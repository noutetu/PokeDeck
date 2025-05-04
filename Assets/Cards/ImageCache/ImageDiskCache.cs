using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using System;

// ----------------------------------------------------------------------
// カード画像のディスクキャッシュを管理するクラス
// 画像をディスクに保存・読み込み・管理する機能を提供
// ----------------------------------------------------------------------
public class ImageDiskCache
{
    // ディスクキャッシュの設定
    private string cacheFolderPath;
    private long maxCacheSize; // バイト単位
    private readonly object cacheLock = new object();
    
    // キャッシュメタデータ
    private Dictionary<string, CacheMetadata> cacheMetadata = new Dictionary<string, CacheMetadata>();
    private string metadataFilePath;
    
    // ファイルアクセス用の静的ロックオブジェクト
    private static readonly object fileLock = new object();
    
    // ----------------------------------------------------------------------
    // コンストラクタ
    // @param folderName キャッシュフォルダ名
    // @param maxSizeMB 最大キャッシュサイズ（MB単位）
    // ----------------------------------------------------------------------
    public ImageDiskCache(string folderName = "ImageCache", long maxSizeMB = 500)
    {
        cacheFolderPath = Path.Combine(Application.persistentDataPath, folderName);
        metadataFilePath = Path.Combine(cacheFolderPath, "cache_metadata.json");
        maxCacheSize = maxSizeMB * 1024 * 1024; // MBをバイトに変換
        
        // キャッシュディレクトリを作成
        if (!Directory.Exists(cacheFolderPath))
        {
            Directory.CreateDirectory(cacheFolderPath);
            Debug.Log($"💾 新しいキャッシュディレクトリを作成しました: {cacheFolderPath}");
        }
        
        // メタデータの読み込み
        LoadMetadata();
        
        Debug.Log($"💾 ImageDiskCacheを初期化: キャッシュパス={cacheFolderPath}, 最大サイズ={maxSizeMB}MB");
    }// ----------------------------------------------------------------------
// 現在のキャッシュファイル数を取得
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
    // @return ハッシュ化されたキャッシュキー
    // ----------------------------------------------------------------------
    public static string GetKeyFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        
        using (var md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(url);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            
            StringBuilder sb = new StringBuilder();
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
    // @return 保存が成功したかどうか
    // ----------------------------------------------------------------------
    public async UniTask<bool> SaveImageAsync(string url, byte[] imageData)
    {
        if (string.IsNullOrEmpty(url) || imageData == null || imageData.Length == 0)
        {
            Debug.LogWarning("💾 無効な画像データのためキャッシュを保存できません");
            return false;
        }
        
        try
        {
            string key = GetKeyFromUrl(url);
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
    // ----------------------------------------------------------------------
    public async UniTask<byte[]> LoadImageAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        
        try
        {
            string key = GetKeyFromUrl(url);
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
    // @return キャッシュされているかどうか
    // ----------------------------------------------------------------------
    public bool HasCache(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        
        string key = GetKeyFromUrl(url);
        string filePath = Path.Combine(cacheFolderPath, key);
        
        lock (cacheLock)
        {
            return File.Exists(filePath) && cacheMetadata.ContainsKey(key);
        }
    }
    
    // ----------------------------------------------------------------------
    // キャッシュを削除
    // @param url 画像のURL
    // @return 削除が成功したかどうか
    // ----------------------------------------------------------------------
    public bool RemoveCache(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        
        try
        {
            string key = GetKeyFromUrl(url);
            string filePath = Path.Combine(cacheFolderPath, key);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
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
    // @return 削除が成功したかどうか
    // ----------------------------------------------------------------------
    public bool ClearAllCache()
    {
        try
        {
            // キャッシュディレクトリ内のすべてのファイルを削除
            if (Directory.Exists(cacheFolderPath))
            {
                string[] files = Directory.GetFiles(cacheFolderPath);
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
            
            Debug.Log("💾 すべてのキャッシュを削除しました");
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
    // @param requiredBytes 必要な容量（バイト）
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
            
            Debug.Log($"💾 キャッシュ容量が上限に近づいています: {currentSize / (1024 * 1024)}MB / {maxCacheSize / (1024 * 1024)}MB");
            
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
                    
                    Debug.Log($"💾 キャッシュを削除しました: {item.Value.Url}, サイズ: {fileSize / 1024}KB");
                }
            }
            
            // メタデータを保存
            await SaveMetadataAsync();
            
            Debug.Log($"💾 キャッシュクリーンアップ完了: {freedSpace / (1024 * 1024)}MB解放");
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 キャッシュ容量確保中にエラー: {ex.Message}");
        }
    }
    
    // ----------------------------------------------------------------------
    // 現在のキャッシュサイズを取得
    // @return 現在のキャッシュサイズ（バイト）
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
    // ----------------------------------------------------------------------
    public float GetCacheSizeMB()
    {
        try
        {
            long sizeInBytes = GetCurrentCacheSize();
            return sizeInBytes / (1024f * 1024f); // バイト数をMBに変換
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 キャッシュサイズ取得中にエラー: {ex.Message}");
            return 0f;
        }
    }

    // ----------------------------------------------------------------------
    // メタデータをJSONに保存
    // ----------------------------------------------------------------------
    private async UniTask SaveMetadataAsync()
    {
        try
        {
            // キャッシュディレクトリが存在するか確認
            if (!Directory.Exists(cacheFolderPath))
            {
                Directory.CreateDirectory(cacheFolderPath);
                Debug.Log($"💾 キャッシュディレクトリを作成しました: {cacheFolderPath}");
            }
            
            // JSONデータ作成
            CacheMetadataRoot metadataRoot = new CacheMetadataRoot();
            string json;
            
            lock (cacheLock)
            {
                metadataRoot.Metadata = new List<CacheMetadata>(cacheMetadata.Values);
                json = JsonUtility.ToJson(metadataRoot, true);
            }
            
            // JSONファイルに書き込み - ファイル書き込み用のロックを使用
            bool success = false;
            int retryCount = 0;
            const int maxRetries = 3;
            
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
                    // 共有違反やファイルアクセスエラーの場合はリトライ
                    retryCount++;
                    Debug.LogWarning($"💾 メタデータ保存中にIOエラー: {ioEx.Message}, リトライ {retryCount}/{maxRetries}");
                    
                    // リトライ前に少し待機
                    await UniTask.Delay(100 * retryCount);
                }
                catch (Exception ex)
                {
                    // その他の例外は再スローして処理を中断
                    throw ex;
                }
            }
            
            if (success)
            {
                Debug.Log($"💾 キャッシュメタデータを保存しました: {metadataRoot.Metadata.Count}個のエントリ");
            }
            else
            {
                Debug.LogError($"💾 メタデータ保存に{maxRetries}回失敗しました");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"💾 メタデータ保存中にエラー: {ex.Message}");
        }
    }
    
    // ----------------------------------------------------------------------
    // メタデータをJSONから読み込み
    // ----------------------------------------------------------------------
    private void LoadMetadata()
    {
        try
        {
            if (!File.Exists(metadataFilePath))
            {
                Debug.Log("💾 キャッシュメタデータファイルがありません。新規作成します。");
                // メタデータの初期化と保存
                SaveMetadataAsync().Forget();
                return;
            }
            
            string json = File.ReadAllText(metadataFilePath);
            
            // 空のJSONファイルをチェック
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.Log("💾 キャッシュメタデータファイルが空です。新規作成します。");
                // メタデータの初期化と保存
                SaveMetadataAsync().Forget();
                return;
            }
            
            CacheMetadataRoot metadataRoot = JsonUtility.FromJson<CacheMetadataRoot>(json);
            
            if (metadataRoot != null && metadataRoot.Metadata != null)
            {
                lock (cacheLock)
                {
                    cacheMetadata.Clear();
                    
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
                
                Debug.Log($"💾 キャッシュメタデータを読み込みました: {cacheMetadata.Count}個のエントリ");
            }
            else
            {
                Debug.LogWarning("💾 キャッシュメタデータの読み込みに失敗しました");
                // メタデータの初期化と保存
                SaveMetadataAsync().Forget();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"💾 メタデータ読み込み中にエラー: {ex.Message}");
            
            // エラー発生時は新しいメタデータを作成
            lock (cacheLock)
            {
                cacheMetadata.Clear();
            }
            
            // メタデータファイルを初期化
            SaveMetadataAsync().Forget();
        }
    }
    
    // ----------------------------------------------------------------------
    // テクスチャをバイト配列に変換
    // @param texture 変換対象のテクスチャ
    // @return バイト配列
    // ----------------------------------------------------------------------
    public static byte[] TextureToBytes(Texture2D texture)
    {
        try
        {
            if (texture == null) return null;
            
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
    // @return 生成されたテクスチャ
    // ----------------------------------------------------------------------
    public static Texture2D BytesToTexture(byte[] bytes)
    {
        try
        {
            if (bytes == null || bytes.Length == 0) return null;
            
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
// ----------------------------------------------------------------------
[Serializable]
public class CacheMetadata
{
    public string Url;
    public string Key;
    public DateTime LastAccessed;
    public DateTime Created;
    public long Size;
    
    // Unity JSONシリアライザ用の変換プロパティ
    public string LastAccessedString
    {
        get => LastAccessed.ToString("o");
        set => LastAccessed = DateTime.Parse(value);
    }
    
    public string CreatedString
    {
        get => Created.ToString("o");
        set => Created = DateTime.Parse(value);
    }
}

// ----------------------------------------------------------------------
// キャッシュメタデータのルートクラス（JSONシリアライズ用）
// ----------------------------------------------------------------------
[Serializable]
public class CacheMetadataRoot
{
    public List<CacheMetadata> Metadata = new List<CacheMetadata>();
}