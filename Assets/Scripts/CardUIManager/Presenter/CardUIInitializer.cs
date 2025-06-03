using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

// ----------------------------------------------------------------------
// カードUIの初期化を担当するクラス
// MVRPパターンの初期化と初期画像読み込みを管理
// ----------------------------------------------------------------------
public class CardUIInitializer
{
    // ----------------------------------------------------------------------
    // 定数定義
    // ----------------------------------------------------------------------
    private static class Constants
    {
        public const int DEFAULT_BATCH_SIZE = 5;
        public const int DEFAULT_INITIAL_CARD_COUNT = 30;
        public const float PROGRESS_COMPLETE_DELAY_SECONDS = 1.0f;
        
        // フィードバックメッセージ
        public const string MSG_IMAGE_LOAD_PROGRESS_FORMAT = "画像ロード: {0}/{1}枚";
        public const string MSG_INITIAL_IMAGE_LOAD_COMPLETE = "初期画像ロード完了";
        public const string MSG_INITIALIZATION_FAILED = "カードUI初期化に失敗しました";
        public const string MSG_IMAGE_LOAD_FAILED = "画像読み込み中にエラーが発生しました";
        public const string MSG_MVRP_INITIALIZATION_FAILED = "MVRP初期化に失敗しました";
    }
    // ----------------------------------------------------------------------
    // フィールド
    // ----------------------------------------------------------------------
    private readonly AllCardView allCardView;
    private readonly SearchView searchView;
    private readonly int batchSize;
    
    // ----------------------------------------------------------------------
    // 内部コンポーネント
    // ----------------------------------------------------------------------
    private AllCardPresenter presenter;
    private AllCardModel model;
    
    // ----------------------------------------------------------------------
    // プロパティ - 外部からアクセス可能なコンポーネント
    // ----------------------------------------------------------------------
    public AllCardPresenter Presenter => presenter;
    public AllCardModel Model => model;
    
    // ----------------------------------------------------------------------
    // コンストラクタ - 必要なコンポーネントを注入
    // ----------------------------------------------------------------------
    public CardUIInitializer(AllCardView allCardView, SearchView searchView, int batchSize = Constants.DEFAULT_BATCH_SIZE)
    {
        this.allCardView = allCardView;
        this.searchView = searchView;
        this.batchSize = batchSize;
    }
    
    // ----------------------------------------------------------------------
    // 初期化処理の実行
    // MVRPパターンの構築、画像読み込み、検索機能の初期化を順次実行
    // ----------------------------------------------------------------------
    public async UniTask<bool> InitializeAsync(List<CardModel> allCards)
    {
        try
        {
            return await ExecuteInitializationSteps(allCards);
        }
        catch (System.Exception ex)
        {
            return HandleInitializationError(ex);
        }
    }

    // ----------------------------------------------------------------------
    // 初期化ステップの実行
    // ----------------------------------------------------------------------
    private async UniTask<bool> ExecuteInitializationSteps(List<CardModel> allCards)
    {
        // MVRP初期化
        if (!InitializeMVRPSafely())
        {
            return false;
        }
        
        // 初期画像読み込み
        if (!await LoadInitialImagesWithErrorHandling(allCards))
        {
            return false;
        }
        
        // 検索機能初期化
        InitializeSearchViewSafely(allCards);
        
        return true;
    }

    // ----------------------------------------------------------------------
    // 初期化エラーハンドリング
    // ----------------------------------------------------------------------
    private bool HandleInitializationError(System.Exception exception)
    {
        // 詳細エラーログ出力
        Debug.LogError($"CardUIInitializer initialization failed: {exception.Message}");
        Debug.LogException(exception);
        
        // ユーザー向けエラーフィードバック
        ShowFailureFeedback(Constants.MSG_INITIALIZATION_FAILED);
        
        return false;
    }

    // ----------------------------------------------------------------------
    // フィードバック表示ヘルパーメソッド
    // ----------------------------------------------------------------------
    private void ShowProgressFeedback(string message)
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowProgressFeedback(message);
        }
    }

    private void UpdateFeedbackMessage(string message)
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.UpdateFeedbackMessage(message);
        }
    }

    private void CompleteProgressFeedback(string message, float delay)
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.CompleteProgressFeedback(message, delay);
        }
    }

    private void ShowFailureFeedback(string message)
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowFailureFeedback(message);
        }
    }
    
    // ----------------------------------------------------------------------
    // 安全なMVRP初期化
    // ----------------------------------------------------------------------
    private bool InitializeMVRPSafely()
    {
        try
        {
            return ExecuteMVRPInitialization();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"MVRP initialization failed: {ex.Message}");
            Debug.LogException(ex);
            ShowFailureFeedback(Constants.MSG_MVRP_INITIALIZATION_FAILED);
            return false;
        }
    }

    // ----------------------------------------------------------------------
    // MVRP初期化実行
    // ----------------------------------------------------------------------
    private bool ExecuteMVRPInitialization()
    {
        // 前提条件チェック
        if (allCardView == null)
        {
            Debug.LogWarning("AllCardView is null during MVRP initialization");
            return false;
        }
        
        // Model-View-Reactive-Presenterパターンの各コンポーネントを構築
        model = new AllCardModel();
        presenter = new AllCardPresenter(model);
        
        // PresenterとViewをバインド
        allCardView.BindPresenter(presenter);
        
        return true;
    }
    
    // ----------------------------------------------------------------------
    // エラーハンドリング付き初期画像読み込み
    // ----------------------------------------------------------------------
    private async UniTask<bool> LoadInitialImagesWithErrorHandling(List<CardModel> cards)
    {
        try
        {
            await ExecuteInitialImageLoading(cards);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Initial image loading failed: {ex.Message}");
            Debug.LogException(ex);
            ShowFailureFeedback(Constants.MSG_IMAGE_LOAD_FAILED);
            return false;
        }
    }

    // ----------------------------------------------------------------------
    // 初期画像読み込み実行
    // バッチ処理で画像を分割読み込みし、進捗をフィードバック表示
    // ----------------------------------------------------------------------
    private async UniTask ExecuteInitialImageLoading(List<CardModel> cards)
    {
        // 前提条件チェック
        if (cards == null || cards.Count == 0)
        {
            Debug.LogWarning("No cards provided for initial image loading");
            return;
        }
        
        // 初期表示分のカードを決定
        var initialCards = GetInitialCardsForLoading(cards);
        
        // 進捗表示開始
        ShowInitialLoadingProgress(initialCards.Count);
        
        // バッチ処理で画像読み込み実行
        await ProcessImageLoadingBatches(initialCards);
        
        // 完了フィードバック
        CompleteProgressFeedback(Constants.MSG_INITIAL_IMAGE_LOAD_COMPLETE, Constants.PROGRESS_COMPLETE_DELAY_SECONDS);
    }

    // ----------------------------------------------------------------------
    // 初期読み込み対象カード取得
    // ----------------------------------------------------------------------
    private List<CardModel> GetInitialCardsForLoading(List<CardModel> cards)
    {
        int initialCount = Mathf.Min(Constants.DEFAULT_INITIAL_CARD_COUNT, cards.Count);
        return cards.GetRange(0, initialCount);
    }

    // ----------------------------------------------------------------------
    // 初期読み込み進捗表示開始
    // ----------------------------------------------------------------------
    private void ShowInitialLoadingProgress(int totalCount)
    {
        string message = string.Format(Constants.MSG_IMAGE_LOAD_PROGRESS_FORMAT, 0, totalCount);
        ShowProgressFeedback(message);
    }

    // ----------------------------------------------------------------------
    // 画像読み込みバッチ処理
    // ----------------------------------------------------------------------
    private async UniTask ProcessImageLoadingBatches(List<CardModel> initialCards)
    {
        int processedCount = 0;
        
        for (int i = 0; i < initialCards.Count; i += batchSize)
        {
            // 現在のバッチサイズ決定
            int currentBatchSize = Mathf.Min(batchSize, initialCards.Count - i);
            
            // バッチ内のタスク生成と実行
            await ExecuteSingleBatch(initialCards, i, currentBatchSize);
            
            // 進捗更新
            processedCount += currentBatchSize;
            UpdateLoadingProgress(processedCount, initialCards.Count);
            
            // UI更新のためのフレーム待機
            await UniTask.Yield();
        }
    }

    // ----------------------------------------------------------------------
    // 単一バッチの実行
    // ----------------------------------------------------------------------
    private async UniTask ExecuteSingleBatch(List<CardModel> cards, int startIndex, int batchSize)
    {
        var batchTasks = new List<UniTask>();
        
        for (int j = 0; j < batchSize; j++)
        {
            int cardIndex = startIndex + j;
            if (cardIndex < cards.Count)
            {
                var card = cards[cardIndex];
                if (ShouldLoadCardImage(card))
                {
                    batchTasks.Add(ImageCacheManager.Instance.LoadTextureAsync(card.imageKey, card));
                }
            }
        }
        
        if (batchTasks.Count > 0)
        {
            await UniTask.WhenAll(batchTasks);
        }
    }

    // ----------------------------------------------------------------------
    // カード画像読み込み必要性判定
    // ----------------------------------------------------------------------
    private bool ShouldLoadCardImage(CardModel card)
    {
        return !string.IsNullOrEmpty(card.imageKey);
    }

    // ----------------------------------------------------------------------
    // 読み込み進捗更新
    // ----------------------------------------------------------------------
    private void UpdateLoadingProgress(int processedCount, int totalCount)
    {
        string message = string.Format(Constants.MSG_IMAGE_LOAD_PROGRESS_FORMAT, processedCount, totalCount);
        UpdateFeedbackMessage(message);
    }
    
    // ----------------------------------------------------------------------
    // 安全な検索機能初期化
    // ----------------------------------------------------------------------
    private void InitializeSearchViewSafely(List<CardModel> allCards)
    {
        try
        {
            ExecuteSearchViewInitialization(allCards);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SearchView initialization failed: {ex.Message}");
            Debug.LogException(ex);
            // 検索機能の初期化失敗は致命的ではないため、警告レベルで処理
            Debug.LogWarning("Search functionality may not be available");
        }
    }

    // ----------------------------------------------------------------------
    // 検索機能初期化実行
    // SearchViewにカードデータを設定して検索機能を有効化
    // ----------------------------------------------------------------------
    private void ExecuteSearchViewInitialization(List<CardModel> allCards)
    {
        if (searchView != null && allCards != null)
        {
            searchView.SetCards(allCards);
            Debug.Log($"SearchView initialized with {allCards.Count} cards");
        }
        else
        {
            if (searchView == null)
            {
                Debug.LogWarning("SearchView is null during initialization");
            }
            if (allCards == null)
            {
                Debug.LogWarning("AllCards is null during SearchView initialization");
            }
        }
    }
    
    // ----------------------------------------------------------------------
    // ScrollRectコンポーネントの安全な取得
    // 遅延読み込み用にスクロール管理コンポーネントを返す
    // ----------------------------------------------------------------------
    public ScrollRect GetScrollRect()
    {
        try
        {
            var scrollRect = allCardView?.GetComponentInChildren<ScrollRect>();
            
            if (scrollRect == null)
            {
                Debug.LogWarning("ScrollRect component not found in AllCardView hierarchy");
            }
            
            return scrollRect;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to get ScrollRect component: {ex.Message}");
            Debug.LogException(ex);
            return null;
        }
    }
}
