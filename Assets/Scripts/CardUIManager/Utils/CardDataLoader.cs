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
    // ----------------------------------------------------------------------
    // 定数定義
    // ----------------------------------------------------------------------
    private static class Constants
    {
        // ネットワーク設定
        public const string REMOTE_JSON_URL = "https://noutetu.github.io/PokeDeckCards/output.json";
        public const string LOCAL_FALLBACK_FILENAME = "cards.json";
        
        // フィードバックメッセージ
        public const string MSG_LOADING_REMOTE = "リモートからカードデータを読み込み中...";
        public const string MSG_INITIALIZING_DATABASE = "カードデータベースを初期化中...";
        public const string MSG_LOADING_LOCAL = "ローカルからカードデータを読み込み中...";
        public const string MSG_LOADING_COMPLETE = "カードデータ読み込み完了: {0}枚";
        public const string MSG_LOCAL_COMPLETE = "ローカルカードデータ読み込み完了: {0}枚";
        public const string MSG_LOADING_FAILED = "カードデータの読み込みに失敗しました";
        
        // 進捗値
        public const float PROGRESS_COMPLETE = 1.0f;
    }
    
    // ----------------------------------------------------------------------
    // メインのカードデータ読み込み処理
    // リモートJSONの取得を試行し、失敗時はローカルフォールバックを使用
    // ----------------------------------------------------------------------
    public async UniTask<List<CardModel>> LoadCardsAsync()
    {
        try
        {
            return await ExecuteSafeLoadCardsAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("CardDataLoader: カードデータ読み込み中にエラーが発生しました");
            Debug.LogException(ex);
            
            DisplayFailureFeedback();
            return new List<CardModel>();
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeLoadCardsAsync メソッド
    // 安全なカードデータ読み込み処理を実行します。
    // ----------------------------------------------------------------------
    private async UniTask<List<CardModel>> ExecuteSafeLoadCardsAsync()
    {
        var remoteCards = await TryLoadFromRemote();
        if (remoteCards != null && remoteCards.Count > 0)
        {
            return remoteCards;
        }

        return await LoadFromLocalWithFeedback();
    }

    // ----------------------------------------------------------------------
    // TryLoadFromRemote メソッド
    // リモートからのカードデータ読み込みを試行します。
    // @return 成功時はカードリスト、失敗時はnull
    // ----------------------------------------------------------------------
    private async UniTask<List<CardModel>> TryLoadFromRemote()
    {
        try
        {
            DisplayRemoteLoadingFeedback();
            var cards = await LoadFromRemoteAsync();
            
            if (cards != null && cards.Count > 0)
            {
                await ProcessSuccessfulRemoteLoad(cards);
                return cards;
            }
            
            Debug.LogWarning("CardDataLoader: リモートからのデータ取得に失敗しました");
            return null;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("CardDataLoader: リモート読み込み中にエラーが発生しました");
            Debug.LogException(ex);
            return null;
        }
    }

    // ----------------------------------------------------------------------
    // ProcessSuccessfulRemoteLoad メソッド
    // リモート読み込み成功時の処理を実行します。
    // @param cards 読み込まれたカードデータ
    // ----------------------------------------------------------------------
    private async UniTask ProcessSuccessfulRemoteLoad(List<CardModel> cards)
    {
        DisplayDatabaseInitializingFeedback();
        await InitializeCardDatabase(cards);
        DisplayRemoteLoadCompleteFeedback(cards.Count);
    }

    // ----------------------------------------------------------------------
    // LoadFromLocalWithFeedback メソッド
    // ローカルファイルからの読み込みをフィードバック付きで実行します。
    // @return カードデータのリスト
    // ----------------------------------------------------------------------
    private async UniTask<List<CardModel>> LoadFromLocalWithFeedback()
    {
        try
        {
            DisplayLocalLoadingFeedback();
            var localCards = await LoadFromLocalFallback();
            DisplayLocalLoadCompleteFeedback(localCards.Count);
            return localCards;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("CardDataLoader: ローカル読み込み中にエラーが発生しました");
            Debug.LogException(ex);
            return new List<CardModel>();
        }
    }
    
    // ----------------------------------------------------------------------
    // リモートJSONファイルからのカードデータ取得
    // UnityWebRequestを使用してHTTP通信でデータを取得
    // ----------------------------------------------------------------------
    private async UniTask<List<CardModel>> LoadFromRemoteAsync()
    {
        try
        {
            return await ExecuteSafeRemoteLoad();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("CardDataLoader: リモート通信中にエラーが発生しました");
            Debug.LogException(ex);
            return null;
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeRemoteLoad メソッド
    // 安全なリモートデータ読み込みを実行します。
    // @return カードデータのリスト、失敗時はnull
    // ----------------------------------------------------------------------
    private async UniTask<List<CardModel>> ExecuteSafeRemoteLoad()
    {
        using var request = UnityWebRequest.Get(Constants.REMOTE_JSON_URL);
        await request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            return ProcessRemoteResponse(request.downloadHandler.text);
        }
        
        Debug.LogWarning($"CardDataLoader: リモート読み込み失敗 - {request.error}");
        return null;
    }

    // ----------------------------------------------------------------------
    // ProcessRemoteResponse メソッド
    // リモートレスポンスを処理してカードデータに変換します。
    // @param jsonText JSONテキストデータ
    // @return カードデータのリスト
    // ----------------------------------------------------------------------
    private List<CardModel> ProcessRemoteResponse(string jsonText)
    {
        try
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogWarning("CardDataLoader: 空のJSONデータを受信しました");
                return null;
            }

            var loadedModel = JsonConvert.DeserializeObject<AllCardModel>(jsonText);
            if (loadedModel == null)
            {
                Debug.LogWarning("CardDataLoader: JSONデシリアライゼーションに失敗しました");
                return null;
            }

            return loadedModel.GetAllCards();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("CardDataLoader: JSONデータ処理中にエラーが発生しました");
            Debug.LogException(ex);
            return null;
        }
    }
    
    // ----------------------------------------------------------------------
    // ローカルファイルからのフォールバック読み込み
    // StreamingAssetsフォルダーからカードデータを取得
    // ----------------------------------------------------------------------
    private async UniTask<List<CardModel>> LoadFromLocalFallback()
    {
        try
        {
            return await ExecuteSafeLocalLoad();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("CardDataLoader: ローカルファイル読み込み中にエラーが発生しました");
            Debug.LogException(ex);
            return new List<CardModel>();
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeLocalLoad メソッド
    // 安全なローカルファイル読み込みを実行します。
    // @return カードデータのリスト
    // ----------------------------------------------------------------------
    private async UniTask<List<CardModel>> ExecuteSafeLocalLoad()
    {
        string localPath = GetLocalFilePath();
        
        if (!ValidateLocalFile(localPath))
        {
            Debug.LogWarning($"CardDataLoader: ローカルファイルが見つかりません: {localPath}");
            return new List<CardModel>();
        }

        string localJson = await File.ReadAllTextAsync(localPath);
        return ProcessLocalJsonData(localJson);
    }

    // ----------------------------------------------------------------------
    // GetLocalFilePath メソッド
    // ローカルファイルのパスを取得します。
    // @return ファイルパス
    // ----------------------------------------------------------------------
    private string GetLocalFilePath()
    {
        return Path.Combine(Application.streamingAssetsPath, Constants.LOCAL_FALLBACK_FILENAME);
    }

    // ----------------------------------------------------------------------
    // ValidateLocalFile メソッド
    // ローカルファイルの存在を検証します。
    // @param filePath ファイルパス
    // @return ファイルが存在する場合true
    // ----------------------------------------------------------------------
    private bool ValidateLocalFile(string filePath)
    {
        return File.Exists(filePath);
    }

    // ----------------------------------------------------------------------
    // ProcessLocalJsonData メソッド
    // ローカルJSONデータを処理してカードデータに変換します。
    // @param jsonData JSONデータ
    // @return カードデータのリスト
    // ----------------------------------------------------------------------
    private List<CardModel> ProcessLocalJsonData(string jsonData)
    {
        try
        {
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogWarning("CardDataLoader: 空のローカルJSONデータです");
                return new List<CardModel>();
            }

            var loadedModel = JsonConvert.DeserializeObject<AllCardModel>(jsonData);
            if (loadedModel == null)
            {
                Debug.LogWarning("CardDataLoader: ローカルJSONデシリアライゼーションに失敗しました");
                return new List<CardModel>();
            }

            return loadedModel.GetAllCards() ?? new List<CardModel>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("CardDataLoader: ローカルJSONデータ処理中にエラーが発生しました");
            Debug.LogException(ex);
            return new List<CardModel>();
        }
    }
    
    // ----------------------------------------------------------------------
    // カードデータベースの初期化処理
    // 読み込んだカードデータをキャッシュに設定
    // ----------------------------------------------------------------------
    private async UniTask InitializeCardDatabase(List<CardModel> cards)
    {
        try
        {
            await ExecuteSafeDatabaseInitialization(cards);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("CardDataLoader: データベース初期化中にエラーが発生しました");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeDatabaseInitialization メソッド
    // 安全なデータベース初期化を実行します。
    // @param cards 初期化するカードデータ
    // ----------------------------------------------------------------------
    private async UniTask ExecuteSafeDatabaseInitialization(List<CardModel> cards)
    {
        if (cards == null || cards.Count == 0)
        {
            Debug.LogWarning("CardDataLoader: 初期化するカードデータが空です");
            return;
        }

        await CardDatabase.WaitForInitializationAsync();
        CardDatabase.SetCachedCards(cards);
        
        Debug.Log($"CardDataLoader: データベース初期化完了 - {cards.Count}枚のカードを登録");
    }

    // ----------------------------------------------------------------------
    // フィードバック表示ヘルパーメソッド群
    // ----------------------------------------------------------------------

    // ----------------------------------------------------------------------
    // DisplayRemoteLoadingFeedback メソッド
    // リモート読み込み開始のフィードバックを表示します。
    // ----------------------------------------------------------------------
    private void DisplayRemoteLoadingFeedback()
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowProgressFeedback(Constants.MSG_LOADING_REMOTE);
        }
    }

    // ----------------------------------------------------------------------
    // DisplayDatabaseInitializingFeedback メソッド
    // データベース初期化中のフィードバックを表示します。
    // ----------------------------------------------------------------------
    private void DisplayDatabaseInitializingFeedback()
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.UpdateFeedbackMessage(Constants.MSG_INITIALIZING_DATABASE);
        }
    }

    // ----------------------------------------------------------------------
    // DisplayRemoteLoadCompleteFeedback メソッド
    // リモート読み込み完了のフィードバックを表示します。
    // @param cardCount 読み込まれたカード数
    // ----------------------------------------------------------------------
    private void DisplayRemoteLoadCompleteFeedback(int cardCount)
    {
        if (FeedbackContainer.Instance != null)
        {
            string message = string.Format(Constants.MSG_LOADING_COMPLETE, cardCount);
            FeedbackContainer.Instance.CompleteProgressFeedback(message, Constants.PROGRESS_COMPLETE);
        }
    }

    // ----------------------------------------------------------------------
    // DisplayLocalLoadingFeedback メソッド
    // ローカル読み込み開始のフィードバックを表示します。
    // ----------------------------------------------------------------------
    private void DisplayLocalLoadingFeedback()
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.UpdateFeedbackMessage(Constants.MSG_LOADING_LOCAL);
        }
    }

    // ----------------------------------------------------------------------
    // DisplayLocalLoadCompleteFeedback メソッド
    // ローカル読み込み完了のフィードバックを表示します。
    // @param cardCount 読み込まれたカード数
    // ----------------------------------------------------------------------
    private void DisplayLocalLoadCompleteFeedback(int cardCount)
    {
        if (FeedbackContainer.Instance != null)
        {
            string message = string.Format(Constants.MSG_LOCAL_COMPLETE, cardCount);
            FeedbackContainer.Instance.CompleteProgressFeedback(message, Constants.PROGRESS_COMPLETE);
        }
    }

    // ----------------------------------------------------------------------
    // DisplayFailureFeedback メソッド
    // 読み込み失敗のフィードバックを表示します。
    // ----------------------------------------------------------------------
    private void DisplayFailureFeedback()
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowFailureFeedback(Constants.MSG_LOADING_FAILED);
        }
    }
}
