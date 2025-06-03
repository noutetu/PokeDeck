using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

// ----------------------------------------------------------------------
// カードUIの管理を行うクラス - 統合版
// 専門クラスの機能をまとめ、シンプルなコーディネータとして機能する
// ----------------------------------------------------------------------
public class CardUIManager : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // 定数定義
    // ----------------------------------------------------------------------
    private static class Constants
    {
        public const int DEFAULT_INITIAL_CARD_COUNT = 30;
        public const int DEFAULT_LAZY_LOAD_BATCH_SIZE = 20;
        public const float DEFAULT_SCROLL_THRESHOLD = 0.7f;
        public const float INITIALIZATION_COMPLETE_DELAY_SECONDS = 1.5f;
        public const float SCROLL_TOP_POSITION_Y = 1.0f;
        public const float SCROLL_LEFT_POSITION_X = 0.0f;
        
        // フィードバックメッセージ
        public const string MSG_INITIALIZING_APP = "アプリを初期化中...";
        public const string MSG_INITIALIZATION_COMPLETE = "アプリの初期化が完了しました";
        public const string MSG_INITIALIZATION_FAILED = "アプリの初期化中にエラーが発生しました";
        public const string MSG_LOADING_CARD_DATA = "カードデータを読み込み中...";
        public const string MSG_INITIALIZING_UI = "UIを初期化中...";
        public const string MSG_LOADING_INITIAL_IMAGES = "初期画像を読み込み中...";
        public const string MSG_SETTING_UP_SYSTEM = "システムを設定中...";
        public const string MSG_CARD_DATA_LOAD_FAILED = "カードデータの読み込みに失敗しました";
    }
    // ----------------------------------------------------------------------
    // MVRPパターンの各コンポーネント
    // ----------------------------------------------------------------------
    [SerializeField] private AllCardView allCardView;
    
    // ----------------------------------------------------------------------
    // 検索関連のコンポーネント 
    // ----------------------------------------------------------------------
    [SerializeField] private GameObject searchPanel;
    [SerializeField] private GameObject cardListPanel;
    [SerializeField] private SearchView searchView;
    
    // ----------------------------------------------------------------------
    // 設定可能なパラメータ
    // ----------------------------------------------------------------------
    [SerializeField] private int initialCardCount = Constants.DEFAULT_INITIAL_CARD_COUNT;
    [SerializeField] private int lazyLoadBatchSize = Constants.DEFAULT_LAZY_LOAD_BATCH_SIZE;
    [SerializeField] private float scrollThreshold = Constants.DEFAULT_SCROLL_THRESHOLD;

    // ----------------------------------------------------------------------
    // 内部コンポーネント
    // ----------------------------------------------------------------------
    private AllCardPresenter presenter;
    private AllCardModel model;
    private ScrollRect scrollRect;
    
    // ----------------------------------------------------------------------
    // データ管理
    // ----------------------------------------------------------------------
    private List<CardModel> allCards = new List<CardModel>();
    private List<CardModel> remainingCards = new List<CardModel>();
    private bool isLoadingBatch = false;

    // ----------------------------------------------------------------------
    // 初期化メソッド
    // ----------------------------------------------------------------------
    private async void Start()
    {
        try
        {
            await ExecuteInitializationWithFeedback();
        }
        catch (System.Exception ex)
        {
            HandleInitializationError(ex);
        }
    }

    // ----------------------------------------------------------------------
    // フィードバック付き初期化実行
    // ----------------------------------------------------------------------
    private async UniTask ExecuteInitializationWithFeedback()
    {
        // 初期化開始フィードバック
        ShowProgressFeedback(Constants.MSG_INITIALIZING_APP);
        
        // 初期化処理実行
        await InitializeAsync();
        
        // 初期化完了フィードバック
        CompleteProgressFeedback(Constants.MSG_INITIALIZATION_COMPLETE, Constants.INITIALIZATION_COMPLETE_DELAY_SECONDS);
    }

    // ----------------------------------------------------------------------
    // 初期化エラーハンドリング
    // ----------------------------------------------------------------------
    private void HandleInitializationError(System.Exception exception)
    {
        // エラーログ出力（デバッグ用）
        Debug.LogError($"CardUIManager initialization failed: {exception.Message}");
        Debug.LogException(exception);
        
        // ユーザー向けエラーフィードバック
        ShowFailureFeedback(Constants.MSG_INITIALIZATION_FAILED);
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
    // 総合初期化処理
    // ----------------------------------------------------------------------
    private async UniTask InitializeAsync()
    {
        try
        {
            // カードデータ読み込み
            UpdateFeedbackMessage(Constants.MSG_LOADING_CARD_DATA);
            await LoadCardsDataWithErrorHandling();
            
            // UI初期化
            UpdateFeedbackMessage(Constants.MSG_INITIALIZING_UI);
            InitializeMVRPComponents();
            
            // 画像読み込み
            UpdateFeedbackMessage(Constants.MSG_LOADING_INITIAL_IMAGES);
            await LoadInitialImagesWithErrorHandling();
            
            // イベントハンドラ設定
            UpdateFeedbackMessage(Constants.MSG_SETTING_UP_SYSTEM);
            SetupEventHandlers();
            InitializeSearchRouter();
        }
        catch (System.Exception ex)
        {
            // 初期化プロセス内での詳細エラーハンドリング
            Debug.LogError($"Detailed initialization error: {ex.Message}");
            throw; // 上位レベルでの処理のため再スロー
        }
    }

    // ----------------------------------------------------------------------
    // エラーハンドリング付きカードデータ読み込み
    // ----------------------------------------------------------------------
    private async UniTask LoadCardsDataWithErrorHandling()
    {
        try
        {
            await CardDatabase.WaitForInitializationAsync();
            allCards = await LoadCardsFromRemoteOrLocal();
            CardDatabase.SetCachedCards(allCards);
            
            SetCardsToSearchView();
        }
        catch (Exception ex)
        {
            Debug.LogError($"LoadCardsData failed: {ex.Message}");
            allCards = new List<CardModel>(); // フォールバック用空リスト
            throw;
        }
    }

    // ----------------------------------------------------------------------
    // 検索ビューにカード設定
    // ----------------------------------------------------------------------
    private void SetCardsToSearchView()
    {
        if (searchView != null)
        {
            searchView.SetCards(allCards);
        }
    }

    // ----------------------------------------------------------------------
    // リモートまたはローカルからカードデータを読み込み
    // ----------------------------------------------------------------------
    private async UniTask<List<CardModel>> LoadCardsFromRemoteOrLocal()
    {
        try
        {
            var cardDataLoader = new CardDataLoader();
            var loadedCards = await cardDataLoader.LoadCardsAsync();
            
            // 読み込み成功時のバリデーション
            if (loadedCards == null || loadedCards.Count == 0)
            {
                Debug.LogWarning("LoadCardsAsync returned empty or null card list");
                return new List<CardModel>();
            }
            
            return loadedCards;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load cards from remote or local: {ex.Message}");
            Debug.LogException(ex);
            
            ShowFailureFeedback(Constants.MSG_CARD_DATA_LOAD_FAILED);
            return new List<CardModel>();
        }
    }



    // ----------------------------------------------------------------------
    // MVRPコンポーネント初期化
    // ----------------------------------------------------------------------
    private void InitializeMVRPComponents()
    {
        try
        {
            model = new AllCardModel();
            presenter = new AllCardPresenter(model);
            scrollRect = allCardView?.GetComponentInChildren<ScrollRect>();
            
            // PresenterとViewを接続
            BindPresenterToView();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"MVRP initialization failed: {ex.Message}");
            Debug.LogException(ex);
            throw;
        }
    }

    // ----------------------------------------------------------------------
    // PresenterとViewのバインド
    // ----------------------------------------------------------------------
    private void BindPresenterToView()
    {
        if (allCardView != null && presenter != null)
        {
            allCardView.BindPresenter(presenter);
        }
        else
        {
            Debug.LogWarning("AllCardView or Presenter is null during binding");
        }
    }

    // ----------------------------------------------------------------------
    // エラーハンドリング付き初期画像読み込み
    // ----------------------------------------------------------------------
    private async UniTask LoadInitialImagesWithErrorHandling()
    {
        try
        {
            if (allCards.Count == 0)
            {
                Debug.LogWarning("No cards available for initial image loading");
                return;
            }

            await ExecuteInitialImageLoading();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Initial image loading failed: {ex.Message}");
            Debug.LogException(ex);
            // 画像読み込み失敗は致命的ではないため、処理を継続
        }
    }

    // ----------------------------------------------------------------------
    // 初期画像読み込み実行
    // ----------------------------------------------------------------------
    private async UniTask ExecuteInitialImageLoading()
    {
        // 初期表示カードの範囲を決定
        var initialCards = GetInitialCardsRange();
        
        // 初期カードを表示
        LoadCardsToPresenter(initialCards);
        
        // 残りのカードを遅延読み込み用に設定
        SetupRemainingCardsForLazyLoad(initialCards.Count);

        // 画像の非同期読み込み実行
        await LoadTexturesForCards(initialCards);
    }

    // ----------------------------------------------------------------------
    // 初期表示カード範囲取得
    // ----------------------------------------------------------------------
    private List<CardModel> GetInitialCardsRange()
    {
        int cardCount = Math.Min(initialCardCount, allCards.Count);
        return allCards.GetRange(0, cardCount);
    }

    // ----------------------------------------------------------------------
    // Presenterにカード読み込み
    // ----------------------------------------------------------------------
    private void LoadCardsToPresenter(List<CardModel> cards)
    {
        if (presenter != null)
        {
            presenter.LoadCards(cards);
        }
    }

    // ----------------------------------------------------------------------
    // 遅延読み込み用残りカード設定
    // ----------------------------------------------------------------------
    private void SetupRemainingCardsForLazyLoad(int initialCount)
    {
        if (allCards.Count > initialCount)
        {
            remainingCards = allCards.GetRange(initialCount, allCards.Count - initialCount);
        }
        else
        {
            remainingCards.Clear();
        }
    }

    // ----------------------------------------------------------------------
    // カードのテクスチャ読み込み
    // ----------------------------------------------------------------------
    private async UniTask LoadTexturesForCards(List<CardModel> cards)
    {
        var loadTasks = new List<UniTask>();
        
        foreach (var card in cards)
        {
            if (ShouldLoadTexture(card))
            {
                loadTasks.Add(ImageCacheManager.Instance.LoadTextureAsync(card.imageKey, card));
            }
        }
        
        if (loadTasks.Count > 0)
        {
            await UniTask.WhenAll(loadTasks);
        }
    }

    // ----------------------------------------------------------------------
    // テクスチャ読み込み必要性判定
    // ----------------------------------------------------------------------
    private bool ShouldLoadTexture(CardModel card)
    {
        return !string.IsNullOrEmpty(card.imageKey) && card.imageTexture == null;
    }

    // ----------------------------------------------------------------------
    // イベントハンドラの設定
    // ----------------------------------------------------------------------
    private void SetupEventHandlers()
    {
        SetupScrollHandling();
        SetupSearchHandling();
    }

    // ----------------------------------------------------------------------
    // スクロール処理の設定
    // ----------------------------------------------------------------------
    private void SetupScrollHandling()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }
    }

    // ----------------------------------------------------------------------
    // 検索処理の設定
    // ----------------------------------------------------------------------
    private void SetupSearchHandling()
    {
        if (SearchNavigator.Instance != null)
        {
            SearchNavigator.Instance.OnSearchResult += OnSearchResult;
        }
    }

    // ----------------------------------------------------------------------
    // スクロール時の処理
    // ----------------------------------------------------------------------
    private void OnScrollValueChanged(Vector2 position)
    {
        try
        {
            // 遅延読み込み実行条件チェック
            if (!ShouldTriggerLazyLoad(position)) return;
            
            // 非同期バッチ読み込み開始
            LoadNextBatchAsync().Forget();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Scroll handling error: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // 遅延読み込みトリガー判定
    // ----------------------------------------------------------------------
    private bool ShouldTriggerLazyLoad(Vector2 scrollPosition)
    {
        return remainingCards.Count > 0 && 
               !isLoadingBatch && 
               scrollPosition.y < (Constants.SCROLL_TOP_POSITION_Y - scrollThreshold);
    }

    // ----------------------------------------------------------------------
    // 検索結果の処理
    // ----------------------------------------------------------------------
    private void OnSearchResult(List<CardModel> searchResults)
    {
        try
        {
            // 既存表示をクリア
            ClearCurrentCardDisplay();
            
            // 検索結果の表示処理
            ProcessSearchResults(searchResults);
            
            // スクロール位置をトップにリセット
            ResetScrollToTop();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Search result processing error: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // 現在のカード表示をクリア
    // ----------------------------------------------------------------------
    private void ClearCurrentCardDisplay()
    {
        if (presenter != null)
        {
            presenter.ClearCards();
        }
    }

    // ----------------------------------------------------------------------
    // 検索結果の処理
    // ----------------------------------------------------------------------
    private void ProcessSearchResults(List<CardModel> searchResults)
    {
        int displayCount = Math.Min(initialCardCount, searchResults.Count);
        
        if (displayCount > 0)
        {
            var initialCards = searchResults.GetRange(0, displayCount);
            LoadCardsToPresenter(initialCards);
            
            SetupRemainingCardsFromSearchResults(searchResults, displayCount);
        }
    }

    // ----------------------------------------------------------------------
    // 検索結果から残りカード設定
    // ----------------------------------------------------------------------
    private void SetupRemainingCardsFromSearchResults(List<CardModel> searchResults, int displayCount)
    {
        if (searchResults.Count > displayCount)
        {
            remainingCards = searchResults.GetRange(displayCount, searchResults.Count - displayCount);
        }
        else
        {
            remainingCards.Clear();
        }
    }

    // ----------------------------------------------------------------------
    // スクロール位置をトップにリセット
    // ----------------------------------------------------------------------
    private void ResetScrollToTop()
    {
        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = new Vector2(Constants.SCROLL_LEFT_POSITION_X, Constants.SCROLL_TOP_POSITION_Y);
        }
    }

    // ----------------------------------------------------------------------
    // 次のバッチを読み込む
    // ----------------------------------------------------------------------
    private async UniTaskVoid LoadNextBatchAsync()
    {
        // 重複実行防止チェック
        if (isLoadingBatch || remainingCards.Count == 0) return;
        
        isLoadingBatch = true;
        
        try
        {
            await ExecuteBatchLoading();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Batch loading failed: {ex.Message}");
            Debug.LogException(ex);
        }
        finally
        {
            // 読み込み状態を確実にリセット
            isLoadingBatch = false;
        }
    }

    // ----------------------------------------------------------------------
    // バッチ読み込み実行
    // ----------------------------------------------------------------------
    private async UniTask ExecuteBatchLoading()
    {
        // 次のバッチ範囲を決定
        var nextBatch = GetNextBatch();
        
        // 画像の非同期読み込み
        await LoadTexturesForCards(nextBatch);
        
        // Presenterにカード追加
        await AddCardsToPresenter(nextBatch);
        
        // 読み込み完了したカードを残りカードリストから削除
        RemoveProcessedCardsFromRemaining(nextBatch.Count);
    }

    // ----------------------------------------------------------------------
    // 次のバッチ取得
    // ----------------------------------------------------------------------
    private List<CardModel> GetNextBatch()
    {
        int batchCount = Math.Min(lazyLoadBatchSize, remainingCards.Count);
        return remainingCards.GetRange(0, batchCount);
    }

    // ----------------------------------------------------------------------
    // Presenterにカード追加
    // ----------------------------------------------------------------------
    private async UniTask AddCardsToPresenter(List<CardModel> cards)
    {
        if (presenter != null)
        {
            await presenter.AddCardsAsync(cards);
        }
    }

    // ----------------------------------------------------------------------
    // 処理完了カードを残りリストから削除
    // ----------------------------------------------------------------------
    private void RemoveProcessedCardsFromRemaining(int processedCount)
    {
        if (processedCount > 0 && processedCount <= remainingCards.Count)
        {
            remainingCards.RemoveRange(0, processedCount);
        }
    }

    // ----------------------------------------------------------------------
    // 検索ナビゲーターの初期化
    // ----------------------------------------------------------------------
    private void InitializeSearchRouter()
    {
        try
        {
            if (SearchNavigator.Instance != null)
            {
                SetupSearchNavigatorPanels();
                HideSearchPanelInitially();
            }
            else
            {
                Debug.LogWarning("SearchNavigator.Instance is null during initialization");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Search router initialization failed: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // 検索ナビゲーターパネル設定
    // ----------------------------------------------------------------------
    private void SetupSearchNavigatorPanels()
    {
        SearchNavigator.Instance.SetPanels(searchPanel, cardListPanel);
    }

    // ----------------------------------------------------------------------
    // 検索パネルの初期非表示
    // ----------------------------------------------------------------------
    private void HideSearchPanelInitially()
    {
        if (searchPanel != null)
        {
            searchPanel.SetActive(false);
        }
    }

    // ----------------------------------------------------------------------
    // 破棄時のクリーンアップ
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        try
        {
            CleanupEventSubscriptions();
            CleanupScrollHandling();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Cleanup failed during OnDestroy: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // イベント購読のクリーンアップ
    // ----------------------------------------------------------------------
    private void CleanupEventSubscriptions()
    {
        if (SearchNavigator.Instance != null)
        {
            SearchNavigator.Instance.OnSearchResult -= OnSearchResult;
        }
    }

    // ----------------------------------------------------------------------
    // スクロールハンドリングのクリーンアップ
    // ----------------------------------------------------------------------
    private void CleanupScrollHandling()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }
    }
}