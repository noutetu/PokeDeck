using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// ----------------------------------------------------------------------
// カードデータの読み込みを担当するクラス
// リモートJSONファイルの取得とローカルフォールバック処理を管理
// ----------------------------------------------------------------------
public class CardDataLoader
{
    private const string jsonUrl = "https://noutetu.github.io/PokeDeckCards/output.json";
    
    // ----------------------------------------------------------------------
    // メインのカードデータ読み込み処理
    // リモートJSONの取得を試行し、失敗時はローカルフォールバックを使用
    // ----------------------------------------------------------------------
    public async UniTask<List<CardModel>> LoadCardsAsync()
    {
        try
        {
            // リモートからの読み込み試行
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowProgressFeedback("リモートからカードデータを読み込み中...");
            }
            
            var cards = await LoadFromRemoteAsync();
            if (cards != null && cards.Count > 0)
            {
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.UpdateFeedbackMessage("カードデータベースを初期化中...");
                }
                await InitializeCardDatabase(cards);
                
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.CompleteProgressFeedback($"カードデータ読み込み完了: {cards.Count}枚", 1.0f);
                }
                return cards;
            }
            
            // ローカルフォールバック
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.UpdateFeedbackMessage("ローカルからカードデータを読み込み中...");
            }
            var localCards = await LoadFromLocalFallback();
            
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.CompleteProgressFeedback($"ローカルカードデータ読み込み完了: {localCards.Count}枚", 1.0f);
            }
            return localCards;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ カード読み込みエラー: {ex.Message}");
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback("カードデータの読み込みに失敗しました");
            }
            return new List<CardModel>();
        }
    }
    
    // ----------------------------------------------------------------------
    // リモートJSONファイルからのカードデータ取得
    // UnityWebRequestを使用してHTTP通信でデータを取得
    // ----------------------------------------------------------------------
    private async UniTask<List<CardModel>> LoadFromRemoteAsync()
    {
        using var request = UnityWebRequest.Get(jsonUrl);
        await request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            var jsonText = request.downloadHandler.text;
            var loadedModel = JsonConvert.DeserializeObject<AllCardModel>(jsonText);
            return loadedModel.GetAllCards();
        }
        
        return null;
    }
    
    // ----------------------------------------------------------------------
    // ローカルファイルからのフォールバック読み込み
    // StreamingAssetsフォルダーからカードデータを取得
    // ----------------------------------------------------------------------
    private async UniTask<List<CardModel>> LoadFromLocalFallback()
    {
        string localPath = Path.Combine(Application.streamingAssetsPath, "cards.json");
        if (File.Exists(localPath))
        {
            string localJson = await File.ReadAllTextAsync(localPath);
            var loadedModel = JsonConvert.DeserializeObject<AllCardModel>(localJson);
            return loadedModel.GetAllCards();
        }
        
        return new List<CardModel>();
    }
    
    // ----------------------------------------------------------------------
    // カードデータベースの初期化処理
    // 読み込んだカードデータをキャッシュに設定
    // ----------------------------------------------------------------------
    private async UniTask InitializeCardDatabase(List<CardModel> cards)
    {
        await CardDatabase.WaitForInitializationAsync();
        CardDatabase.SetCachedCards(cards);
    }
}
