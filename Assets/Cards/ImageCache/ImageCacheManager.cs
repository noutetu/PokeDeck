using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System;

// ----------------------------------------------------------------------
// カード画像の読み込みとメモリキャッシュを管理するクラス
// UniTaskを使用した非同期読み込みと、メモリへのキャッシュ機能を提供
// ----------------------------------------------------------------------
public class ImageCacheManager : MonoBehaviour
{
    // シングルトンインスタンス
    private static ImageCacheManager _instance;
    public static ImageCacheManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("ImageCacheManager");
                _instance = go.AddComponent<ImageCacheManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // メモリ内のテクスチャキャッシュ
    private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    
    // デフォルトのテクスチャ
    [SerializeField] private Texture2D defaultTexture;
    private Texture2D _defaultTexture;
    
    // 読み込み中のURLを追跡するためのセット
    private HashSet<string> loadingUrls = new HashSet<string>();
    
    private void Awake()
    {
        // シングルトンの設定
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        
        Debug.Log("🖼️ ImageCacheManagerを初期化しました");
    }
    // ----------------------------------------------------------------------
    // URLからテクスチャを読み込み、キャッシュする
    // @param url 画像のURL
    // @param assignToCard カード画像を設定する対象のCardModel（オプション）
    // @returns 読み込んだテクスチャ
    // ----------------------------------------------------------------------
    public async UniTask<Texture2D> LoadTextureAsync(string url, CardModel assignToCard = null)
    {
        // URLが空の場合はデフォルトテクスチャを返す
        if (string.IsNullOrEmpty(url))
        {
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
            
            // メモリキャッシュをチェック
            if (textureCache.TryGetValue(url, out Texture2D existingTexture))
            {
                if (assignToCard != null)
                {
                    assignToCard.imageTexture = existingTexture;
                }
                
                loadingUrls.Remove(url);
                return existingTexture;
            }
            
            // ネットワークから読み込み
            Debug.Log($"🌐 画像をダウンロードします: {url}");
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                await request.SendWebRequest();
                
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"画像読み込みエラー: {request.error}");
                    loadingUrls.Remove(url);
                    return _defaultTexture;
                }
                
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                
                // テクスチャをメモリキャッシュに追加
                textureCache[url] = texture;
                
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
            Debug.LogError($"画像読み込み中にエラーが発生しました: {ex.Message}");
            loadingUrls.Remove(url);
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
    // デフォルトテクスチャを取得
    // ----------------------------------------------------------------------
    public Texture2D GetDefaultTexture()
    {
        return _defaultTexture;
    }
    
    // デフォルトテクスチャを設定（外部から設定可能）
    public void SetDefaultTexture(Texture2D texture)
    {
        if (texture != null)
        {
            _defaultTexture = texture;
            Debug.Log("デフォルトテクスチャを更新しました");
        }
    }
    
    // ----------------------------------------------------------------------
    // キャッシュの内容をログに出力するデバッグメソッド
    // ----------------------------------------------------------------------
    public void LogCacheContents()
    {
        Debug.Log($"=== ImageCacheManager キャッシュ内容 ===");
        Debug.Log($"メモリキャッシュ数: {textureCache.Count}件");
        
        int index = 0;
        foreach (var entry in textureCache)
        {
            Texture2D texture = entry.Value;
            string dimensions = texture != null ? $"{texture.width}x{texture.height}" : "null";
            Debug.Log($"[{index}] URL: {entry.Key}, テクスチャ: {dimensions}");
            index++;
        }
        
        Debug.Log($"================================");
    }
}