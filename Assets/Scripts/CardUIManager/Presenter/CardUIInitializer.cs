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
    public CardUIInitializer(AllCardView allCardView, SearchView searchView, int batchSize = 5)
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
            InitializeMVRP();
            await LoadInitialImages(allCards);
            InitializeSearchView(allCards);
            return true;
        }
        catch (System.Exception ex)
        {
            return false;
        }
    }
    
    // ----------------------------------------------------------------------
    // MVRPパターンの初期化
    // Model-View-Reactive-Presenterパターンの各コンポーネントを構築
    // ----------------------------------------------------------------------
    private void InitializeMVRP()
    {
        if (allCardView == null) return;
        
        model = new AllCardModel();
        presenter = new AllCardPresenter(model);
        allCardView.BindPresenter(presenter);
    }
    
    // ----------------------------------------------------------------------
    // 初期画像の非同期読み込み
    // バッチ処理で画像を分割読み込みし、進捗をフィードバック表示
    // ----------------------------------------------------------------------
    private async UniTask LoadInitialImages(List<CardModel> cards)
    {
        if (cards == null || cards.Count == 0) return;
        
        // 初期表示分のカードのみ画像をロード
        int initialCount = Mathf.Min(30, cards.Count); // 最初の30枚のみ
        var initialCards = cards.GetRange(0, initialCount);
        
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowProgressFeedback($"画像ロード: 0/{initialCards.Count}枚");
        }
        
        int processedCount = 0;
        
        for (int i = 0; i < initialCards.Count; i += batchSize)
        {
            int currentBatchSize = Mathf.Min(batchSize, initialCards.Count - i);
            var batchTasks = new List<UniTask>();
            
            for (int j = 0; j < currentBatchSize; j++)
            {
                if (i + j < initialCards.Count)
                {
                    var card = initialCards[i + j];
                    if (!string.IsNullOrEmpty(card.imageKey))
                    {
                        batchTasks.Add(ImageCacheManager.Instance.LoadTextureAsync(card.imageKey, card));
                    }
                }
            }
            
            await UniTask.WhenAll(batchTasks);
            processedCount += currentBatchSize;
            
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.UpdateFeedbackMessage($"画像ロード: {processedCount}/{initialCards.Count}枚");
            }
            await UniTask.Yield();
        }
        
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.CompleteProgressFeedback("初期画像ロード完了", 1.0f);
        }
    }
    
    // ----------------------------------------------------------------------
    // 検索機能の初期化
    // SearchViewにカードデータを設定して検索機能を有効化
    // ----------------------------------------------------------------------
    private void InitializeSearchView(List<CardModel> allCards)
    {
        if (searchView != null)
        {
            searchView.SetCards(allCards);
        }
    }
    
    // ----------------------------------------------------------------------
    // ScrollRectコンポーネントの取得
    // 遅延読み込み用にスクロール管理コンポーネントを返す
    // ----------------------------------------------------------------------
    public ScrollRect GetScrollRect()
    {
        return allCardView?.GetComponentInChildren<ScrollRect>();
    }
}
