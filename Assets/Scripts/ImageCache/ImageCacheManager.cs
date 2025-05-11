using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System;

// ----------------------------------------------------------------------
// カード画像の読み込みとキャッシュを管理するクラス
// メモリキャッシュとディスクキャッシュの階層的管理を行い、
// UniTaskを使用した非同期読み込みと効率的なキャッシング機能を提供
// ----------------------------------------------------------------------
public class ImageCacheManager : MonoBehaviour
{
    // シングルトンインスタンス
    private static ImageCacheManager _instance;
    public static ImageCacheManager Instance
    {
        get
        {
            return _instance;
        }
    }

    // -------------------------------------------------
    // メモリ内のテクスチャキャッシュ
    // -------------------------------------------------
    private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    
    // -------------------------------------------------
    // ディスクキャッシュ
    // -------------------------------------------------
    private ImageDiskCache diskCache;
    
    // -------------------------------------------------
    // デフォルトのテクスチャ
    // -------------------------------------------------
    [SerializeField] private Texture2D defaultTexture;
    private Texture2D _defaultTexture;
    
    // -------------------------------------------------
    // 設定項目
    // -------------------------------------------------
    [SerializeField] private int maxCacheSizeMB = 500; // ディスクキャッシュの最大サイズ (MB)
    [SerializeField] private bool useMemoryCache = true; // メモリキャッシュを使用するか
    [SerializeField] private bool useDiskCache = true; // ディスクキャッシュを使用するか
    
    // -------------------------------------------------
    // メモリ管理設定
    // -------------------------------------------------
    [SerializeField] private int maxMemoryCacheCount = 1000; // 最大100テクスチャをメモリに保持

    // -------------------------------------------------
    // 読み込み中のURLを追跡するためのセット
    // -------------------------------------------------
    private HashSet<string> loadingUrls = new HashSet<string>();
    
    // -------------------------------------------------
    // キャッシュパフォーマンス統計情報
    // -------------------------------------------------
    private int memoryCacheHitCount = 0;    // メモリキャッシュからの読み込み回数
    private int diskCacheHitCount = 0;       // ディスクキャッシュからの読み込み回数
    private int networkLoadCount = 0;        // ネットワークからの読み込み回数
    private int totalLoadCount = 0;          // 総読み込み試行回数
    
    // -------------------------------------------------
    // デバッグログ設定
    // -------------------------------------------------
    [SerializeField] private bool enableDetailedLog = true;  // 詳細ログを有効化するか
    [SerializeField] private bool logStatisticsOnLoad = true; // ロード完了時に統計情報を出力するか
    
    // -------------------------------------------------
    // UnityのAwakeメソッド
    // シングルトンパターンを実装し、ディスクキャッシュを初期化
    // -------------------------------------------------
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _defaultTexture = defaultTexture;
            
            // ディスクキャッシュの初期化
            if (useDiskCache)
            {
                diskCache = new ImageDiskCache("ImageCache", maxCacheSizeMB);
                Debug.Log($"🖼️ ディスクキャッシュを初期化しました（最大サイズ: {maxCacheSizeMB}MB）");
            }
            
            Debug.Log("🖼️ ImageCacheManagerを初期化しました");
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    // ----------------------------------------------------------------------
    // URLからテクスチャを読み込み、キャッシュする
    // @param url 画像のURL
    // @param assignToCard カード画像を設定する対象のCardModel（オプション）
    // @returns 読み込んだテクスチャ
    // ----------------------------------------------------------------------
    public async UniTask<Texture2D> LoadTextureAsync(string url, CardModel assignToCard = null)
    {
        // 総読み込み試行回数をカウント
        totalLoadCount++;
        
        // URLが空の場合はデフォルトテクスチャを返す
        if (string.IsNullOrEmpty(url))
        {
            if (enableDetailedLog)
                Debug.LogWarning("URLが空のため、デフォルトテクスチャを返します");
            
            if (assignToCard != null)
            {
                assignToCard.imageTexture = _defaultTexture;
            }
            return _defaultTexture;
        }
        
        // 読み込み中のURLなら回避（重複読み込み防止）
        if (loadingUrls.Contains(url))
        {
            // 読み込み完了を待機
            while (loadingUrls.Contains(url))
            {
                await UniTask.Yield();
            }
            
            // 読み込み完了後にキャッシュにあるか確認
            if (textureCache.TryGetValue(url, out Texture2D cachedTexture))
            {
                if (assignToCard != null)
                {
                    assignToCard.imageTexture = cachedTexture;
                }
                return cachedTexture;
            }
        }
        
        try
        {
            // 読み込み中としてマーク
            loadingUrls.Add(url);
            
            Texture2D texture = null;
            
            // 1. メモリキャッシュをチェック
            if (useMemoryCache && textureCache.TryGetValue(url, out texture))
            {
                // メモリキャッシュヒットのカウント増加
                memoryCacheHitCount++;
                
                if (assignToCard != null)
                {
                    assignToCard.imageTexture = texture;
                }
                
                loadingUrls.Remove(url);
                
                return texture;
            }
            
            // 2. ディスクキャッシュをチェック
            if (useDiskCache && diskCache != null)
            {
                byte[] imageData = await diskCache.LoadImageAsync(url);
                if (imageData != null)
                {
                    texture = ImageDiskCache.BytesToTexture(imageData);
                    if (texture != null)
                    {
                        // ディスクキャッシュヒットのカウント増加
                        diskCacheHitCount++;
                        
                        // メモリキャッシュにも保存
                        if (useMemoryCache)
                        {
                            textureCache[url] = texture;
                        }
                        
                        if (assignToCard != null)
                        {
                            assignToCard.imageTexture = texture;
                        }
                        
                        loadingUrls.Remove(url);
                        
                        return texture;
                    }
                }
            }
            
            // 3. ネットワークからダウンロード
            // ネットワーク読み込みのカウント増加
            networkLoadCount++;
            
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                await request.SendWebRequest();
                
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"画像読み込みエラー: {request.error}, URL: {url}");
                    loadingUrls.Remove(url);
                    
                    if (assignToCard != null)
                    {
                        assignToCard.imageTexture = _defaultTexture;
                    }
                    
                    return _defaultTexture;
                }
                
                texture = DownloadHandlerTexture.GetContent(request);
                
                // ダウンロードしたテクスチャをディスクキャッシュに保存
                if (useDiskCache && diskCache != null && texture != null)
                {
                    byte[] textureBytes = ImageDiskCache.TextureToBytes(texture);
                    if (textureBytes != null)
                    {
                        await diskCache.SaveImageAsync(url, textureBytes);
                    }
                }
                
                // メモリキャッシュにも保存
                if (useMemoryCache && texture != null)
                {
                    textureCache[url] = texture;
                }
                
                if (assignToCard != null)
                {
                    assignToCard.imageTexture = texture;
                }
                
                loadingUrls.Remove(url);
                
                return texture;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"画像読み込み中にエラーが発生しました: {ex.Message}, URL: {url}");
            loadingUrls.Remove(url);
            
            if (assignToCard != null)
            {
                assignToCard.imageTexture = _defaultTexture;
            }
            
            return _defaultTexture;
        }
    }
    
    // ----------------------------------------------------------------------
    // カードのテクスチャを取得または読み込む（UniTask版）
    // ----------------------------------------------------------------------
    public async UniTask<Texture2D> GetCardTextureAsync(CardModel card)
    {
        if (card == null)
            return _defaultTexture;
            
        // すでにテクスチャが設定されている場合はそれを返す
        if (card.imageTexture != null)
            return card.imageTexture;
            
        // URLが空の場合はデフォルトを返す
        if (string.IsNullOrEmpty(card.imageKey))
        {
            card.imageTexture = _defaultTexture;
            return _defaultTexture;
        }
        
        // 画像を読み込んでカードに設定
        var texture = await LoadTextureAsync(card.imageKey, card);
        return texture;
    }

// ----------------------------------------------------------------------
// キャッシュ統計情報を出力
// ----------------------------------------------------------------------
public void LogCacheStatistics()
{
    if (totalLoadCount == 0) return;
    
    // 各ソース別の割合を計算
    float memoryCachePercent = (float)memoryCacheHitCount / totalLoadCount * 100;
    float diskCachePercent = (float)diskCacheHitCount / totalLoadCount * 100;
    float networkPercent = (float)networkLoadCount / totalLoadCount * 100;
    
    // キャッシュヒット率を計算
    float cacheHitRate = (float)(memoryCacheHitCount + diskCacheHitCount) / totalLoadCount * 100;
    
    // ディスクキャッシュのサイズ情報を取得
    float diskCacheSizeMB = 0;
    int diskCacheFileCount = 0;
    if (useDiskCache && diskCache != null)
    {
        diskCacheSizeMB = diskCache.GetCacheSizeMB();
        diskCacheFileCount = diskCache.GetFileCount();
    }
    
    // 詳細な統計情報をログに出力
    Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Debug.Log("📊 画像キャッシュ詳細 📊");
    Debug.Log($"📈 画像読み込み合計: {totalLoadCount}回");
    Debug.Log($"📊 メモリキャッシュ: {memoryCacheHitCount}回 ({memoryCachePercent:F1}%)");
    Debug.Log($"📊 ディスクキャッシュ: {diskCacheHitCount}回 ({diskCachePercent:F1}%)");
    Debug.Log($"📊 ネットワークロード: {networkLoadCount}回 ({networkPercent:F1}%)");
    Debug.Log($"📊 キャッシュヒット率: {cacheHitRate:F1}%");
    Debug.Log($"💾 メモリキャッシュエントリ数: {textureCache.Count}個");
    Debug.Log($"💾 ディスクキャッシュサイズ: {diskCacheSizeMB:F2}MB / {maxCacheSizeMB}MB ({diskCacheSizeMB / maxCacheSizeMB * 100:F1}%)");
    Debug.Log($"💾 ディスクキャッシュファイル数: {diskCacheFileCount}個");
    Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
}
    
    // ----------------------------------------------------------------------
    // 統計情報をリセット
    // ----------------------------------------------------------------------
    public void ResetStatistics()
    {
        memoryCacheHitCount = 0;
        diskCacheHitCount = 0;
        networkLoadCount = 0;
        totalLoadCount = 0;
        Debug.Log("📊 画像読み込み統計情報をリセットしました");
    }
    
    // ----------------------------------------------------------------------
    // キャッシュ管理
    // ----------------------------------------------------------------------
    
    // キャッシュをクリア（メモリとディスク両方）
    public void ClearAllCache()
    {
        // メモリキャッシュをクリア
        textureCache.Clear();
        
        // ディスクキャッシュをクリア
        if (useDiskCache && diskCache != null)
        {
            diskCache.ClearAllCache();
        }
        
        Debug.Log("🖼️ すべてのキャッシュをクリアしました");
    }
    
    // メモリキャッシュのみクリア
    public void ClearMemoryCache()
    {
        textureCache.Clear();
        Debug.Log("🖼️ メモリキャッシュをクリアしました");
    }
    
    // 特定のURLのキャッシュを削除
    public void RemoveCache(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        
        // メモリキャッシュから削除
        if (textureCache.ContainsKey(url))
        {
            textureCache.Remove(url);
        }
        
        // ディスクキャッシュから削除
        if (useDiskCache && diskCache != null)
        {
            diskCache.RemoveCache(url);
        }
    }
    
    // ----------------------------------------------------------------------
    // デフォルトテクスチャ管理
    // ----------------------------------------------------------------------
    public Texture2D GetDefaultTexture()
    {
        return _defaultTexture;
    }
    
    // ---------------------------------------------------
    // デフォルトテクスチャを設定（外部から設定可能）
    // ---------------------------------------------------
    public void SetDefaultTexture(Texture2D texture)
    {
        if (texture != null)
        {
            _defaultTexture = texture;
            Debug.Log("デフォルトテクスチャを更新しました");
        }
    }
    
    // ----------------------------------------------------------------------
    // アプリケーション終了時やバックグラウンド移行時のキャッシュ管理
    // ----------------------------------------------------------------------
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Debug.Log("🖼️ アプリケーションがバックグラウンドに移行: キャッシュメタデータを保存します");
            // バックグラウンド移行時に統計情報を出力
            LogCacheStatistics();
        }
    }
}