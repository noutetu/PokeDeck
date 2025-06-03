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
    [SerializeField] private int initialCardCount = 30;
    [SerializeField] private int lazyLoadBatchSize = 20;
    [SerializeField] private float scrollThreshold = 0.7f;

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
            // 初期化開始フィードバック
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowProgressFeedback("アプリを初期化中...");
            }
            
            await InitializeAsync();
            
            // 初期化完了フィードバック
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.CompleteProgressFeedback("アプリの初期化が完了しました", 1.5f);
            }
        }
        catch (System.Exception ex)
        {
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback("アプリの初期化中にエラーが発生しました");
            }
        }
    }

    // ----------------------------------------------------------------------
    // 総合初期化処理
    // ----------------------------------------------------------------------
    private async UniTask InitializeAsync()
    {
        // カードデータ読み込み進捗
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.UpdateFeedbackMessage("カードデータを読み込み中...");
        }
        await LoadCardsData();
        
        // UI初期化進捗
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.UpdateFeedbackMessage("UIを初期化中...");
        }
        InitializeMVRP();
        
        // 画像読み込み進捗
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.UpdateFeedbackMessage("初期画像を読み込み中...");
        }
        await LoadInitialImages();
        
        // イベントハンドラ設定進捗
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.UpdateFeedbackMessage("システムを設定中...");
        }
        SetupEventHandlers();
        InitializeSearchRouter();
    }

    // ----------------------------------------------------------------------
    // カードデータの読み込み
    // ----------------------------------------------------------------------
    private async UniTask LoadCardsData()
    {
        await CardDatabase.WaitForInitializationAsync();
        allCards = await LoadCardsFromRemoteOrLocal();
        CardDatabase.SetCachedCards(allCards);
        
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
            return await cardDataLoader.LoadCardsAsync();
        }
        catch (System.Exception ex)
        {
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback("カードデータの読み込みに失敗しました");
            }
            return new List<CardModel>();
        }
    }



    // ----------------------------------------------------------------------
    // MVRP初期化
    // ----------------------------------------------------------------------
    private void InitializeMVRP()
    {
        model = new AllCardModel();
        presenter = new AllCardPresenter(model);
        scrollRect = allCardView?.GetComponentInChildren<ScrollRect>();
        
        // PresenterとViewを接続
        if (allCardView != null)
        {
            allCardView.BindPresenter(presenter);
        }
    }

    // ----------------------------------------------------------------------
    // 初期画像の読み込み
    // ----------------------------------------------------------------------
    private async UniTask LoadInitialImages()
    {
        if (allCards.Count == 0) return;

        var initialCards = allCards.GetRange(0, Math.Min(initialCardCount, allCards.Count));
        
        // 初期カードを表示
        presenter.LoadCards(initialCards);
        
        // 残りのカードを遅延読み込み用に設定
        if (allCards.Count > initialCardCount)
        {
            remainingCards = allCards.GetRange(initialCardCount, allCards.Count - initialCardCount);
        }

        // 画像の非同期読み込み
        var loadTasks = new List<UniTask>();
        foreach (var card in initialCards)
        {
            if (!string.IsNullOrEmpty(card.imageKey) && card.imageTexture == null)
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
        if (remainingCards.Count == 0 || isLoadingBatch) return;
        
        if (position.y < (1.0f - scrollThreshold))
        {
            LoadNextBatchAsync().Forget();
        }
    }

    // ----------------------------------------------------------------------
    // 検索結果の処理
    // ----------------------------------------------------------------------
    private void OnSearchResult(List<CardModel> searchResults)
    {
        presenter.ClearCards();
        
        int displayCount = Math.Min(initialCardCount, searchResults.Count);
        if (displayCount > 0)
        {
            var initialCards = searchResults.GetRange(0, displayCount);
            presenter.LoadCards(initialCards);
            
            if (searchResults.Count > displayCount)
            {
                remainingCards = searchResults.GetRange(displayCount, searchResults.Count - displayCount);
            }
            else
            {
                remainingCards.Clear();
            }
        }
        
        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = new Vector2(0, 1);
        }
    }

    // ----------------------------------------------------------------------
    // 次のバッチを読み込む
    // ----------------------------------------------------------------------
    private async UniTaskVoid LoadNextBatchAsync()
    {
        if (isLoadingBatch || remainingCards.Count == 0) return;
        
        isLoadingBatch = true;
        
        try
        {
            int batchCount = Math.Min(lazyLoadBatchSize, remainingCards.Count);
            var nextBatch = remainingCards.GetRange(0, batchCount);
            
            // 画像の非同期読み込み
            var loadTasks = new List<UniTask>();
            foreach (var card in nextBatch)
            {
                if (!string.IsNullOrEmpty(card.imageKey) && card.imageTexture == null)
                {
                    loadTasks.Add(ImageCacheManager.Instance.LoadTextureAsync(card.imageKey, card));
                }
            }
            
            if (loadTasks.Count > 0)
            {
                await UniTask.WhenAll(loadTasks);
            }
            
            await presenter.AddCardsAsync(nextBatch);
            remainingCards.RemoveRange(0, batchCount);
        }
        finally
        {
            isLoadingBatch = false;
        }
    }

    // ----------------------------------------------------------------------
    // 検索ナビゲーターの初期化
    // ----------------------------------------------------------------------
    private void InitializeSearchRouter()
    {
        if (SearchNavigator.Instance != null)
        {
            SearchNavigator.Instance.SetPanels(searchPanel, cardListPanel);
            if (searchPanel != null)
            {
                searchPanel.SetActive(false);
            }
        }
    }

    // ----------------------------------------------------------------------
    // 破棄時のクリーンアップ
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        if (SearchNavigator.Instance != null)
        {
            SearchNavigator.Instance.OnSearchResult -= OnSearchResult;
        }
    }
}