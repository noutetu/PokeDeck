using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using System;

// ----------------------------------------------------------------------
// ImageDiskCache クラス
// カード画像のディスクキャッシュを管理するクラス。
// 画像をディスクに保存、読み込み、管理する機能を提供します。
// ----------------------------------------------------------------------
public class ImageDiskCache
{
    // -----------------------------------------------------------------------
    // ディスクキャッシュの設定
    // -----------------------------------------------------------------------
    private string cacheFolderPath; // キャッシュフォルダのパス
    private long maxCacheSize; // 最大キャッシュサイズ（バイト単位）
    private readonly object cacheLock = new object(); // キャッシュ操作のスレッドセーフ制御用ロック

    // -----------------------------------------------------------------------
    // キャッシュメタデータ
    // -----------------------------------------------------------------------
    private Dictionary<string, CacheMetadata> cacheMetadata = new Dictionary<string, CacheMetadata>(); // キャッシュのメタデータを保持
    private string metadataFilePath; // メタデータファイルのパス

    // -----------------------------------------------------------------------
    // ファイルアクセス用の静的ロックオブジェクト
    // -----------------------------------------------------------------------
    private static readonly object fileLock = new object(); // ファイル操作のスレッドセーフ制御用ロック

    // ----------------------------------------------------------------------
    // コンストラクタ
    // @param folderName キャッシュフォルダ名（デフォルト: "ImageCache"）
    // @param maxSizeMB 最大キャッシュサイズ（MB単位、デフォルト: 500MB）
    // キャッシュフォルダを初期化し、メタデータを読み込みます。
    // ----------------------------------------------------------------------
    public ImageDiskCache(string folderName = "ImageCache", long maxSizeMB = 500)
    {
        // キャッシュフォルダのパスを設定
        cacheFolderPath = Path.Combine(Application.persistentDataPath, folderName);
        // メタデータファイルのパスを設定
        metadataFilePath = Path.Combine(cacheFolderPath, "cache_metadata.json");
        // MBをバイトに変換
        maxCacheSize = maxSizeMB * 1024 * 1024; 

        // キャッシュディレクトリを作成
        if (!Directory.Exists(cacheFolderPath))
        {
            Directory.CreateDirectory(cacheFolderPath);
        }

        // メタデータの読み込み
        LoadMetadata();
    }

    // ----------------------------------------------------------------------
    // 現在のキャッシュファイル数を取得
    // @return キャッシュに保存されているファイルの数
    // ----------------------------------------------------------------------
    public int GetFileCount()
    {
        try
        {
            lock (cacheLock)
            {
                return cacheMetadata.Count;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 キャッシュファイル数取得中にエラー: {ex.Message}");
            return 0;
        }
    }

    // ----------------------------------------------------------------------
    // URLからキャッシュキーを生成
    // @param url 画像のURL
    // @return ハッシュ化されたキャッシュキー（MD5を使用）
    // ----------------------------------------------------------------------
    public static string GetKeyFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        // MD5ハッシュを生成
        using (var md5 = MD5.Create())
        {
            // URLをバイト配列に変換
            byte[] inputBytes = Encoding.UTF8.GetBytes(url);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // バイト配列を16進数文字列に変換
            StringBuilder sb = new StringBuilder();
            // 各バイトを2桁の16進数に変換して結合
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }

    // ----------------------------------------------------------------------
    // 画像データをディスクキャッシュに保存
    // @param url 画像のURL
    // @param imageData 画像のバイトデータ
    // @return 保存が成功したかどうか（true: 成功, false: 失敗）
    // キャッシュサイズが最大値を超えないように調整し、画像を保存します。
    // ----------------------------------------------------------------------
    public async UniTask<bool> SaveImageAsync(string url, byte[] imageData)
    {
        // URLと画像データが有効かチェック
        if (string.IsNullOrEmpty(url) || imageData == null || imageData.Length == 0)
        {
            return false;
        }

        try
        {
            // キャッシュキーを生成
            string key = GetKeyFromUrl(url);
            // キャッシュファイルのパスを生成
            string filePath = Path.Combine(cacheFolderPath, key);

            // キャッシュサイズが最大値を超えないようにする
            await EnsureSpaceAvailableAsync(imageData.Length);

            // 画像データをファイルに書き込む
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await fs.WriteAsync(imageData, 0, imageData.Length);
            }

            // メタデータを更新
            lock (cacheLock)
            {
                cacheMetadata[key] = new CacheMetadata
                {
                    Url = url,
                    Key = key,
                    LastAccessed = DateTime.Now,
                    Size = imageData.Length,
                    Created = DateTime.Now
                };
            }

            // メタデータを保存
            await SaveMetadataAsync();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 画像のキャッシュ保存中にエラー: {ex.Message}");
            return false;
        }
    }
    
    // ----------------------------------------------------------------------
    // ディスクキャッシュから画像を読み込み
    // @param url 画像のURL
    // @return 画像のバイトデータ（キャッシュになければnull）
    // キャッシュメタデータを更新します。
    // ----------------------------------------------------------------------
    public async UniTask<byte[]> LoadImageAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        
        try
        {
            // キャッシュキーを生成
            string key = GetKeyFromUrl(url);
            // キャッシュファイルのパスを生成
            string filePath = Path.Combine(cacheFolderPath, key);
            
            if (!File.Exists(filePath))
            {
                return null;
            }
            
            // ファイルからデータを読み込む
            byte[] data = await File.ReadAllBytesAsync(filePath);
            
            // メタデータ更新
            lock (cacheLock)
            {
                if (cacheMetadata.ContainsKey(key))
                {
                    // 最終アクセス日時を更新
                    cacheMetadata[key].LastAccessed = DateTime.Now;
                }
            }
            
            // アクセス時間の変更はバックグラウンドで保存
            SaveMetadataAsync().Forget();
            
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 キャッシュからの画像読み込み中にエラー: {ex.Message}");
            return null;
        }
    }
    
    // ----------------------------------------------------------------------
    // URLがキャッシュされているかチェック
    // @param url 画像のURL
    // @return キャッシュされているかどうか（true: キャッシュあり, false: キャッシュなし）
    // ----------------------------------------------------------------------
    public bool HasCache(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        
        // キャッシュキーを生成
        string key = GetKeyFromUrl(url);
        // キャッシュファイルのパスを生成
        string filePath = Path.Combine(cacheFolderPath, key);

        lock (cacheLock)
        {
            return File.Exists(filePath) && cacheMetadata.ContainsKey(key);
        }
    }
    
    // ----------------------------------------------------------------------
    // キャッシュを削除
    // @param url 画像のURL
    // @return 削除が成功したかどうか（true: 成功, false: 失敗）
    // ----------------------------------------------------------------------
    public bool RemoveCache(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        
        try
        {
            // キャッシュキーを生成
            string key = GetKeyFromUrl(url);
            // キャッシュファイルのパスを生成
            string filePath = Path.Combine(cacheFolderPath, key);
            
            // キャッシュファイルが存在する場合は削除
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            // メタデータから削除
            lock (cacheLock)
            {
                cacheMetadata.Remove(key);
            }
            
            // メタデータを保存
            SaveMetadataAsync().Forget();
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 キャッシュ削除中にエラー: {ex.Message}");
            return false;
        }
    }
    
    // ----------------------------------------------------------------------
    // すべてのキャッシュを削除
    // @return 削除が成功したかどうか（true: 成功, false: 失敗）
    // キャッシュディレクトリ内のすべてのファイルを削除します。
    // ----------------------------------------------------------------------
    public bool ClearAllCache()
    {
        try
        {
            // キャッシュディレクトリ内のすべてのファイルを削除
            if (Directory.Exists(cacheFolderPath))
            {
                // キャッシュディレクトリ内のファイルを取得
                string[] files = Directory.GetFiles(cacheFolderPath);
                // 各ファイルを削除
                foreach (string file in files)
                {
                    File.Delete(file);
                }
            }
            
            // メタデータをクリア
            lock (cacheLock)
            {
                cacheMetadata.Clear();
            }
            
            // メタデータを保存
            SaveMetadataAsync().Forget();
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 キャッシュクリア中にエラー: {ex.Message}");
            return false;
        }
    }
    
    // ----------------------------------------------------------------------
    // 十分なキャッシュ容量を確保する（古いファイルを削除）
    // @param requiredBytes 必要な容量（バイト単位）
    // キャッシュサイズが上限を超えないように古いファイルを削除します。
    // ----------------------------------------------------------------------
    private async UniTask EnsureSpaceAvailableAsync(long requiredBytes)
    {
        try
        {
            long currentSize = GetCurrentCacheSize();
            
            // キャッシュサイズが許容量を超えていないか確認
            if (currentSize + requiredBytes <= maxCacheSize)
            {
                return;
            }
            
            // メタデータの最終アクセス日時でソートしたリスト
            List<KeyValuePair<string, CacheMetadata>> sortedItems;
            lock (cacheLock)
            {
                sortedItems = cacheMetadata.ToList();
            }
            
            // 最終アクセス日時の古い順にソート（LRU）
            sortedItems.Sort((a, b) => a.Value.LastAccessed.CompareTo(b.Value.LastAccessed));
            
            // 必要な容量を確保するまで古いアイテムを削除
            long freedSpace = 0;
            long targetSpace = currentSize + requiredBytes - maxCacheSize + 1024 * 1024; // 1MB余裕を持たせる
            
            foreach (var item in sortedItems)
            {
                // 十分な容量が確保できたら終了
                if (freedSpace >= targetSpace)
                {
                    break;
                }
                
                string key = item.Key;
                string filePath = Path.Combine(cacheFolderPath, key);
                
                if (File.Exists(filePath))
                {
                    long fileSize = new FileInfo(filePath).Length;
                    File.Delete(filePath);
                    freedSpace += fileSize;
                    
                    lock (cacheLock)
                    {
                        cacheMetadata.Remove(key);
                    }
                }
            }
            
            // メタデータを保存
            await SaveMetadataAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 キャッシュ容量確保中にエラー: {ex.Message}");
        }
    }
    
    // ----------------------------------------------------------------------
    // 現在のキャッシュサイズを取得
    // @return 現在のキャッシュサイズ（バイト単位）
    // キャッシュディレクトリ内のファイルサイズを合計します。
    // ----------------------------------------------------------------------
    private long GetCurrentCacheSize()
    {
        try
        {
            if (!Directory.Exists(cacheFolderPath))
            {
                return 0;
            }
            
            string[] files = Directory.GetFiles(cacheFolderPath);
            long size = 0;
            
            foreach (string file in files)
            {
                // .jsonメタデータは除外
                if (file.EndsWith(".json")) continue;
                
                FileInfo fileInfo = new FileInfo(file);
                size += fileInfo.Length;
            }
            
            return size;
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 キャッシュサイズ計算中にエラー: {ex.Message}");
            return 0;
        }
    }
    
    // ----------------------------------------------------------------------
    // 現在のディスクキャッシュサイズをMB単位で取得
    // @return 現在のキャッシュサイズ（MB単位）
    // ----------------------------------------------------------------------
    public float GetCacheSizeMB()
    {
        try
        {
            // 現在のキャッシュサイズを取得
            long sizeInBytes = GetCurrentCacheSize();
            // バイト数をMBに変換
            return sizeInBytes / (1024f * 1024f); 
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 キャッシュサイズ取得中にエラー: {ex.Message}");
            return 0f;
        }
    }

    // ----------------------------------------------------------------------
    // メタデータをJSONに保存
    // キャッシュメタデータをJSON形式で保存します。
    // ----------------------------------------------------------------------
    private async UniTask SaveMetadataAsync()
    {
        try
        {
            // キャッシュディレクトリが存在するか確認
            if (!Directory.Exists(cacheFolderPath))
            {
                // 存在しない場合は作成
                Directory.CreateDirectory(cacheFolderPath);
            }
            
            // JSONデータ作成
            CacheMetadataRoot metadataRoot = new CacheMetadataRoot();
            string json;
            // キャッシュメタデータをJSON形式に変換
            lock (cacheLock)
            {
                // キャッシュメタデータをリストに変換
                metadataRoot.Metadata = new List<CacheMetadata>(cacheMetadata.Values);
                // JSON形式にシリアライズ
                json = JsonUtility.ToJson(metadataRoot, true);
            }
            
            // JSONファイルに書き込み - ファイル書き込み用のロックを使用
            bool success = false;
            int retryCount = 0;
            const int maxRetries = 3;
            
            // リトライ回数を設定
            // 最大リトライ回数までリトライ
            while (!success && retryCount < maxRetries)
            {
                try
                {
                    // ファイルアクセスをロックして排他制御
                    lock (fileLock)
                    {
                        // 同期的に書き込み（UniTaskでの非同期書き込みをやめる）
                        File.WriteAllText(metadataFilePath, json);
                    }
                    success = true;
                }
                catch (IOException ioEx)
                {
                    Debug.LogWarning($"💾 メタデータ保存中にエラー: {ioEx.Message}");
                    // 共有違反やファイルアクセスエラーの場合はリトライ
                    retryCount++;

                    // リトライ前に少し待機
                    await UniTask.Delay(100 * retryCount);
                }
                catch (Exception ex)
                {
                    // その他の例外は再スローして処理を中断
                    throw ex;
                }
            }
            
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 メタデータ保存中にエラー: {ex.Message}");
        }
    }
    
    // ----------------------------------------------------------------------
    // メタデータをJSONから読み込み
    // キャッシュメタデータをJSON形式から読み込みます。
    // ----------------------------------------------------------------------
    private void LoadMetadata()
    {
        try
        {
            // キャッシュディレクトリが存在するか確認
            if (!File.Exists(metadataFilePath))
            {
                // メタデータの初期化と保存
                SaveMetadataAsync().Forget();
                return;
            }
            
            
            // メタデータファイルを読み込む
            string json = File.ReadAllText(metadataFilePath);
            
            // 空のJSONファイルをチェック
            if (string.IsNullOrWhiteSpace(json))
            {
                // メタデータの初期化と保存
                SaveMetadataAsync().Forget();
                return;
            }
            
            // JSONをデシリアライズ
            CacheMetadataRoot metadataRoot = JsonUtility.FromJson<CacheMetadataRoot>(json);
            
            // メタデータがnullでないか確認
            if (metadataRoot != null && metadataRoot.Metadata != null)
            {
                lock (cacheLock)
                {
                    // キャッシュメタデータをクリア
                    cacheMetadata.Clear();
                    // メタデータをキャッシュに追加
                    foreach (var item in metadataRoot.Metadata)
                    {
                        // ファイルが存在する場合のみメタデータとして登録
                        string filePath = Path.Combine(cacheFolderPath, item.Key);
                        if (File.Exists(filePath))
                        {
                            cacheMetadata[item.Key] = item;
                        }
                    }
                }
            }
            else
            {
                // メタデータの初期化と保存
                SaveMetadataAsync().Forget();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"💾 メタデータ読み込み中にエラー: {ex.Message}");
    
            lock (cacheLock)
            {
                // メタデータの初期化
                cacheMetadata.Clear();
            }
            
            // メタデータファイルを初期化
            SaveMetadataAsync().Forget();
        }
    }
    
    // ----------------------------------------------------------------------
    // テクスチャをバイト配列に変換
    // @param texture 変換対象のテクスチャ
    // @return バイト配列（PNG形式）
    // ----------------------------------------------------------------------
    public static byte[] TextureToBytes(Texture2D texture)
    {
        try
        {
            if (texture == null) return null;
            
            // テクスチャをPNG形式でエンコード
            return texture.EncodeToPNG();
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 テクスチャ変換中にエラー: {ex.Message}");
            return null;
        }
    }
    
    // ----------------------------------------------------------------------
    // バイト配列からテクスチャを生成
    // @param bytes バイト配列
    // @return 生成されたテクスチャ（失敗時はnull）
    // ----------------------------------------------------------------------
    public static Texture2D BytesToTexture(byte[] bytes)
    {
        try
        {
            if (bytes == null || bytes.Length == 0) return null;
            
            // テクスチャを生成
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(bytes))
            {
                return texture;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 テクスチャ生成中にエラー: {ex.Message}");
            return null;
        }
    }
}

// ----------------------------------------------------------------------
// キャッシュメタデータ構造体
// キャッシュされた画像のメタデータを保持します。
// ----------------------------------------------------------------------
[Serializable]
public class CacheMetadata
{   
    public string Url;              // 画像のURL
    public string Key;              // キャッシュキー（MD5ハッシュ）
    public DateTime LastAccessed;   // 最終アクセス日時
    public DateTime Created;        // 作成日時
    public long Size;               // 画像サイズ（バイト単位）
    
    // Unity JSONシリアライザ用の変換プロパティ
    public string LastAccessedString
    {
        get => LastAccessed.ToString("o");
        set => LastAccessed = DateTime.Parse(value);
    }
    
    // Unity JSONシリアライザ用の変換プロパティ
    public string CreatedString
    {
        get => Created.ToString("o");
        set => Created = DateTime.Parse(value);
    }
}

// ----------------------------------------------------------------------
// キャッシュメタデータのルートクラス（JSONシリアライズ用）
// メタデータのリストを保持するためのルートクラス。
// ----------------------------------------------------------------------
[Serializable]
public class CacheMetadataRoot
{
    public List<CacheMetadata> Metadata = new List<CacheMetadata>();
}