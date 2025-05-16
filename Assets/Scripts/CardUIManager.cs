using UnityEngine.Networking;
using Cysharp.Threading.Tasks;      // UniTaskライブラリ - 非同期処理用
using Newtonsoft.Json;              // JSON変換用ライブラリ
using System.Collections.Generic;
using UnityEngine;
using System.IO;                    // ファイル操作に必要

// ----------------------------------------------------------------------
// カードUIの初期化と管理を行うクラス
// MVRPパターンの初期化とデータロードを担当する
// 検索機能の初期化と遅延読み込みを行う
// 検索結果を受け取って表示する
// ----------------------------------------------------------------------
public class CardUIManager : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // MVRPパターンの各コンポーネント
    // ----------------------------------------------------------------------
    [SerializeField] private AllCardView allCardView;   // View: UI表示担当
    private AllCardPresenter presenter;                 // Presenter: ModelとViewの橋渡し役
    private AllCardModel model;                         // Model: データ保持担当

    // ----------------------------------------------------------------------
    // 検索関連のコンポーネント 
    // ----------------------------------------------------------------------
    [SerializeField] private GameObject searchPanel;    // 検索パネル
    [SerializeField] private GameObject cardListPanel;  // カードリストパネル
    [SerializeField] private SearchView searchView;     // 検索View（オプション）

    // ----------------------------------------------------------------------
    // 遅延読み込み関連 
    // ----------------------------------------------------------------------
    [SerializeField] private int initialCardCount = 30;   // 初期表示するカード数
    [SerializeField] private int lazyLoadBatchSize = 20;  // 遅延読み込み時のバッチサイズ
    [SerializeField] private float scrollThreshold = 0.7f; // スクロール位置がこの値を超えたら追加読み込み
    int batchSize = 5; // バッチサイズ（同時読み込み数）

    // ----------------------------------------------------------------------
    // スクロール検知用
    // ---------------------------------------------------------------------- 
    private UnityEngine.UI.ScrollRect scrollRect;
    private List<CardModel> remainingCards = new List<CardModel>();
    private bool isLoadingBatch = false;
    private bool ignoreScrollEvent = false; // フィルター適用後のスクロールイベントを抑止
    private Vector2 lastPosition = Vector2.zero; // 前回のスクロール位置
    private float lastScrollTime = 0f; // 最後にスクロールした時間
    private float scrollCooldown = 0.1f; // スクロール処理の最小間隔（秒）

    // ----------------------------------------------------------------------
    // 全カードリスト（検索対象）
    // ---------------------------------------------------------------------- 
    private List<CardModel> allCards = new List<CardModel>();

    // ----------------------------------------------------------------------
    // カードデータを取得するJSONのURL（リモートホスティング）
    // ----------------------------------------------------------------------
    private const string jsonUrl = "https://noutetu.github.io/PokeDeckCards/output.json";
    // ----------------------------------------------------------------------
    [SerializeField] private Texture2D defaultCardTexture;    // 画像がない場合のデフォルト画像

    // ----------------------------------------------------------------------
    // 初期化処理
    // ----------------------------------------------------------------------
    private async void Start()
    {
        try
        {
            // -------------------------------------------------
            // 1. JSONファイルを取得し、CardDatabaseに保存
            // -------------------------------------------------
            await CardDatabase.WaitForInitializationAsync();
            await LoadJsonAndInitializeAsync();

            // カード一覧をロード（すべて）
            var allCards = CardDatabase.GetAllCards();

            // -------------------------------------------------
            // 2. MVRPパターンを初期化
            // -------------------------------------------------
            if (allCardView != null)
            {
                // モデル・プレゼンターの作成
                model = new AllCardModel();
                presenter = new AllCardPresenter(model);

                // プレゼンターとビューの接続
                allCardView.BindPresenter(presenter);

                if (allCards != null && allCards.Count > 0)
                {
                    // 初期表示用カード数を算出
                    int displayCount = Mathf.Min(initialCardCount, allCards.Count);
                    List<CardModel> initialCards = allCards.GetRange(0, displayCount);

                    // 残りのカードは遅延ロード用に保存
                    if (allCards.Count > displayCount)
                    {
                        remainingCards = allCards.GetRange(displayCount, allCards.Count - displayCount);
                    }

                    // -------------------------------------------------
                    // 3. 初期表示のカードのみ画像をロード
                    // -------------------------------------------------
                    // 進捗フィードバックを表示
                    FeedbackContainer.Instance.ShowProgressFeedback($"画像ロード: 0/{initialCards.Count}枚");

                    int processedCount = 0;

                    // 初期表示カードの画像のみバッチ単位でロード
                    for (int i = 0; i < initialCards.Count; i += batchSize)
                    {
                        // 現在のバッチのサイズを計算
                        int currentBatchSize = Mathf.Min(batchSize, initialCards.Count - i);
                        var batchTasks = new List<UniTask>();

                        // バッチ内のカードの画像を並行して読み込む
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

                        // バッチの画像読み込みを並行実行して完了を待機
                        await UniTask.WhenAll(batchTasks);
                        processedCount += currentBatchSize;

                        // 既存のフィードバックメッセージを更新
                        FeedbackContainer.Instance.UpdateFeedbackMessage($"画像ロード: {processedCount}/{initialCards.Count}枚");

                        // UIが応答し続けるために1フレーム待機
                        await UniTask.Yield();
                    }

                    // 初期ロード完了を表示
                    FeedbackContainer.Instance.CompleteProgressFeedback("初期画像ロード完了", 1.0f);

                    // 初期カードをロード
                    presenter.LoadCards(initialCards);
                }
            }

            // スクロールビューの初期化
            InitializeScrollView();

            // 検索イベントの購読
            SubscribeToSearchEvents();

            // SearchRouterの初期化
            InitializeSearchRouter();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ 初期化中にエラーが発生しました: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    // SearchNavigatorのイベントを購読
    // ----------------------------------------------------------------------
    private void SubscribeToSearchEvents()
    {
        if (SearchNavigator.Instance != null)
        {
            // 検索結果が適用されたときに呼ばれるイベントハンドラを登録
            SearchNavigator.Instance.OnSearchResult += HandleSearchResult;
        }
    }

    // ----------------------------------------------------------------------
    // 検索結果が適用されたときの処理
    // ----------------------------------------------------------------------
    private void HandleSearchResult(List<CardModel> searchResults)
    {
        // 現在表示されているカードをすべてクリア
        presenter.ClearCards();

        // 検索結果の初期カードのみを表示し、残りを遅延読み込み用に保存
        DisplayFilteredCards(searchResults);
    }

    // ----------------------------------------------------------------------
    // フィルタリングされたカードを表示
    // ----------------------------------------------------------------------
    private void DisplayFilteredCards(List<CardModel> filteredCards)
    {
        int total = filteredCards.Count;
        // 初期表示数を調整
        int displayCount = Mathf.Min(initialCardCount, total);

        // 初期表示用カードを読み込む
        List<CardModel> initialCards = new List<CardModel>();
        if (displayCount > 0)
        {
            initialCards = filteredCards.GetRange(0, displayCount);
        }
        presenter.LoadCards(initialCards);

        // 残りのカードを遅延読み込みリストに設定
        if (total > displayCount)
        {
            remainingCards = filteredCards.GetRange(displayCount, total - displayCount);
        }
        else
        {
            remainingCards.Clear();
        }

        // スクロール位置をリセット
        if (scrollRect != null)
        {
            ignoreScrollEvent = true;
            scrollRect.normalizedPosition = new Vector2(0, 1);
        }
    }

    // ----------------------------------------------------------------------
    // スクロールビューの参照を取得し、スクロールイベントを登録
    // ----------------------------------------------------------------------
    private void InitializeScrollView()
    {
        if (allCardView != null)
        {
            // AllCardViewからScrollRectコンポーネントを取得
            scrollRect = allCardView.GetComponentInChildren<UnityEngine.UI.ScrollRect>();

            if (scrollRect != null)
            {
                // スクロールイベントにリスナーを登録
                scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
                // 初回の誤発火を無視
                ignoreScrollEvent = true;
            }
        }
    }

    // ----------------------------------------------------------------------
    // スクロール時に呼ばれるイベントハンドラ
    // ----------------------------------------------------------------------
    private void OnScrollValueChanged(Vector2 position)
    {
        if (ignoreScrollEvent)
        {
            // フィルタ適用時のプログラム操作によるスクロールを無視
            ignoreScrollEvent = false;
            return;
        }
        
        // スクロール頻度を制限（パフォーマンス最適化）
        float currentTime = Time.time;
        if (currentTime - lastScrollTime < scrollCooldown)
        {
            return; // 前回のスクロールからの経過時間が短い場合はスキップ
        }
        lastScrollTime = currentTime;
        
        // 残りのカードがなければ何もしない
        if (remainingCards.Count == 0 || isLoadingBatch)
            return;
            
        // スクロール方向を検出
        float scrollDirection = position.y - lastPosition.y;
        lastPosition = position;
        
        // スクロール速度が速い場合は多めにロード
        float scrollSpeed = Mathf.Abs(scrollDirection);
        if (scrollSpeed > 0.05f)
        {
            // 高速スクロール時は大きなバッチサイズを使用
            lazyLoadBatchSize = Mathf.Clamp(lazyLoadBatchSize + 5, 5, 30);
        }
        else
        {
            // 低速スクロール時は小さなバッチサイズを使用
            lazyLoadBatchSize = Mathf.Clamp(lazyLoadBatchSize - 1, 5, 30);
        }

        // 縦スクロール位置が閾値を超えたら追加読み込み
        // 1.0が一番上、 0.0が一番下
        if (position.y < (1.0f - scrollThreshold))
        {
            LoadNextBatchAsync().Forget();
        }
    }

    // ----------------------------------------------------------------------
    // SearchNavigatorの初期化
    // ----------------------------------------------------------------------
    private void InitializeSearchRouter()
    {
        // SearchRouterのインスタンスにパネル参照を設定
        if (SearchNavigator.Instance != null)
        {
            SearchNavigator.Instance.SetPanels(searchPanel, cardListPanel);

            // 初期状態では検索パネルを非表示に
            if (searchPanel != null)
            {
                searchPanel.SetActive(false);
            }
        }
    }

    // ----------------------------------------------------------------------
    // JSONデータを非同期で取得し、カード情報を初期化する
    // ----------------------------------------------------------------------
    private async UniTask LoadJsonAndInitializeAsync()
    {
        try
        {
            // JSONファイルをリモートから取得
            using var request = UnityWebRequest.Get(jsonUrl);
            await request.SendWebRequest();

            // リクエストが成功した場合
            if (request.result == UnityWebRequest.Result.Success)
            {
                // JSONデータを取得
                var jsonText = request.downloadHandler.text;

                // 取得したJSONをAllCardModelにデシリアライズ
                var loadedModel = JsonConvert.DeserializeObject<AllCardModel>(jsonText);

                // 全カードを保存（検索用）
                allCards = loadedModel.GetAllCards();

                // 重要: CardDatabaseにカードデータをキャッシュとして設定
                // これにより、SearchModelがnullを返さないようになる
                CardDatabase.SetCachedCards(loadedModel.GetAllCards());

                // 検索モデルにカードデータを直接設定（nullエラー対策）
                if (searchView != null)
                {
                    searchView.SetCards(loadedModel.GetAllCards());
                }
            }
            else
            {
                // フォールバック: StreamingAssetsからローカルJSONを読み込む
                string localPath = Path.Combine(Application.streamingAssetsPath, "cards.json");
                if (File.Exists(localPath))
                {
                    string localJson = File.ReadAllText(localPath);
                    var loadedModel = JsonConvert.DeserializeObject<AllCardModel>(localJson);
                    allCards = loadedModel.GetAllCards();
                    CardDatabase.SetCachedCards(loadedModel.GetAllCards());
                    if (searchView != null)
                    {
                        searchView.SetCards(loadedModel.GetAllCards());
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ JSONロード中に予期せぬエラーが発生しました: " + ex.Message);
        }
    }

    // ----------------------------------------------------------------------
    // 次のバッチを読み込む - 分割ロード処理を追加
    // ----------------------------------------------------------------------
    private async UniTaskVoid LoadNextBatchAsync()
    {
        // 読み込み中フラグをセット
        if (isLoadingBatch || remainingCards.Count == 0)
            return;

        isLoadingBatch = true;

        try
        {
            // 次のバッチサイズを決定
            int batchSize = Mathf.Min(lazyLoadBatchSize, remainingCards.Count);

            // バッチを取得
            List<CardModel> nextBatch = remainingCards.GetRange(0, batchSize);

            // サブバッチに分割して表示（スクロール時のカクつきを軽減）
            int subBatchSize = 3; // 一度に表示するカード数を小さく

            for (int i = 0; i < nextBatch.Count; i += subBatchSize)
            {
                // 残りのカード数からサブバッチサイズを計算
                int count = Mathf.Min(subBatchSize, nextBatch.Count - i);

                // サブバッチを抽出
                var subBatch = nextBatch.GetRange(i, count);
                
                // サブバッチの画像をプリロード
                var loadTasks = new List<UniTask>();
                foreach (var card in subBatch)
                {
                    if (!string.IsNullOrEmpty(card.imageKey) && card.imageTexture == null)
                    {
                        loadTasks.Add(ImageCacheManager.Instance.LoadTextureAsync(card.imageKey, card));
                    }
                }
                
                // 画像読み込みを待機
                if (loadTasks.Count > 0)
                {
                    await UniTask.WhenAll(loadTasks);
                }

                // サブバッチを表示
                await presenter.AddCardsAsync(subBatch);

                // UIが更新される時間を確保（1フレーム待機）
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            // 残りのリストから削除
            remainingCards.RemoveRange(0, batchSize);
        }
        finally
        {
            // フラグをリセット
            isLoadingBatch = false;
        }
    }

    // ----------------------------------------------------------------------
    // コンポーネント破棄時のクリーンアップ
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        // SearchRouterイベントの購読解除
        if (SearchNavigator.Instance != null)
        {
            SearchNavigator.Instance.OnSearchResult -= HandleSearchResult;
        }
    }
}