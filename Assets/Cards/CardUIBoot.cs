using UnityEngine.Networking;
using Cysharp.Threading.Tasks;      // UniTaskライブラリ - 非同期処理用
using Newtonsoft.Json;              // JSON変換用ライブラリ
using System.Collections.Generic;
using UnityEngine;
using System.IO;                    // ファイル操作に必要
using System.Collections;           // コルーチン用

// ----------------------------------------------------------------------
// カードUIの初期化と管理を行うクラス
// MVRPパターンの初期化とデータロードを担当する
// ----------------------------------------------------------------------
public class CardUIBoot : MonoBehaviour
{
    // === MVRPパターンの各コンポーネント ===
    [SerializeField] private AllCardView allCardView;   // View: UI表示担当
    private AllCardPresenter presenter;                 // Presenter: ModelとViewの橋渡し役
    private AllCardModel model;                         // Model: データ保持担当
    
    // === 検索関連のコンポーネント ===
    [SerializeField] private GameObject searchPanel;    // 検索パネル
    [SerializeField] private GameObject cardListPanel;  // カードリストパネル
    [SerializeField] private SearchView searchView;     // 検索View（オプション）
    
    // === 遅延読み込み関連 ===
    [SerializeField] private int initialCardCount = 40;   // 初期表示するカード数
    [SerializeField] private int lazyLoadBatchSize = 20;  // 遅延読み込み時のバッチサイズ
    [SerializeField] private float scrollThreshold = 0.8f; // スクロール位置がこの値を超えたら追加読み込み
    
    // === スクロール検知用 ===
    private UnityEngine.UI.ScrollRect scrollRect;
    private List<CardModel> remainingCards = new List<CardModel>();
    private bool isLoadingBatch = false;
    private bool ignoreScrollEvent = false; // フィルター適用後のスクロールイベントを抑止
    
    // === 全カードリスト（検索対象） ===
    private List<CardModel> allCards = new List<CardModel>();
    
    // カードデータを取得するJSONのURL（リモートホスティング）
    private const string jsonUrl = "https://noutetu.github.io/PokeDeckCards/output.json";

    // デフォルトテクスチャの参照
    [SerializeField] private Texture2D defaultCardTexture;    // 画像がない場合のデフォルト画像

   private async void Start()
{
    try
    {
        // 1. JSONファイルを取得し、CardDatabaseに保存
        Debug.Log("① JSONファイル取得・保存を開始します");
        await CardDatabase.WaitForInitializationAsync();
        await LoadJsonAndInitializeAsync();
        Debug.Log("① JSONファイル取得・保存が完了しました");


        // 2. ImageCacheManagerを初期化し、デフォルト画像を設定
        Debug.Log("② 画像の一括プリロードを開始します");
        
        // カード一覧をロード（すべて）
        var allCards = CardDatabase.GetAllCards();
        
        // 進捗フィードバックを表示（初回のみ作成し、以降は更新）
        FeedbackContainer.Instance.ShowProgressFeedback($"画像プリロード: 0/{allCards.Count}枚");
        
        // 並行して画像を読み込むためのタスクリストを作成
        var loadTasks = new List<UniTask>();
        int batchSize = 20; // バッチサイズ（同時読み込み数）
        int processedCount = 0;
        
        // バッチ単位で画像を読み込み
        for (int i = 0; i < allCards.Count; i += batchSize)
        {
            // 現在のバッチのサイズを計算（最後のバッチは少なくなる可能性がある）
            int currentBatchSize = Mathf.Min(batchSize, allCards.Count - i);
            var batchTasks = new List<UniTask>();
            
            // バッチ内のカードの画像を並行して読み込む
            for (int j = 0; j < currentBatchSize; j++)
            {
                if (i + j < allCards.Count)
                {
                    var card = allCards[i + j];
                    if (!string.IsNullOrEmpty(card.imageKey))
                    {
                        batchTasks.Add(ImageCacheManager.Instance.LoadTextureAsync(card.imageKey, card));
                    }
                }
            }
            
            // バッチの画像読み込みを並行実行して完了を待機
            await UniTask.WhenAll(batchTasks);
            processedCount += currentBatchSize;
            Debug.Log($"② バッチ画像読み込み進捗: {processedCount}/{allCards.Count}枚");
            
            // 既存のフィードバックメッセージを更新（新しいメッセージを作成せず）
            FeedbackContainer.Instance.UpdateFeedbackMessage($"画像プリロード: {processedCount}/{allCards.Count}枚");
            
            // UIが応答し続けるために1フレーム待機
            await UniTask.Yield();
        }
        
        // プリロード完了を表示
        FeedbackContainer.Instance.CompleteProgressFeedback("画像プリロード完了", 2.0f);
        Debug.Log("② 画像の一括プリロードが完了しました");
        

        // 3. デッキの復元
        Debug.Log("③ デッキの復元を開始します");
        // DeckManagerがある場合は以下を有効化
        // await DeckManager.Instance.RestoreDeckAsync();
        Debug.Log("③ デッキの復元が完了しました");

        // 4. カード一覧にカードプレハブと画像を生成
        Debug.Log("④ カード一覧の生成・表示を開始します");
        
        // MVRPパターンを初期化
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
                
                // 初期カードをロード
                presenter.LoadCards(initialCards);
                
                Debug.Log($"④ 初期表示：{displayCount}枚のカードを表示（残り{remainingCards.Count}枚）");
            }
            else
            {
                Debug.LogWarning("表示するカードがありません");
            }
        }
        else
        {
            Debug.LogError("AllCardViewが設定されていません");
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
    // SearchRouterのイベントを購読
    // ----------------------------------------------------------------------
    private void SubscribeToSearchEvents()
    {
        if (SearchRouter.Instance != null)
        {
            // 検索結果が適用されたときに呼ばれるイベントハンドラを登録
            SearchRouter.Instance.OnSearchResult += HandleSearchResult;
            Debug.Log("検索結果イベントの購読を開始しました");
        }
        else
        {
            Debug.LogWarning("SearchRouter.Instanceが見つかりません");
        }
    }
    
    // ----------------------------------------------------------------------
    // 検索結果が適用されたときの処理
    // ----------------------------------------------------------------------
    private void HandleSearchResult(List<CardModel> searchResults)
    {
        Debug.Log($"検索結果を受信: {searchResults.Count}枚のカード");
        
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
        Debug.Log($"🔍 フィルタリング結果の表示: 全{total}枚中、初期表示は{displayCount}枚");

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
                Debug.Log("スクロールビューを初期化しました");
            }
            else
            {
                Debug.LogWarning("ScrollRectコンポーネントが見つかりませんでした");
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
        // 残りのカードがなければ何もしない
        if (remainingCards.Count == 0 || isLoadingBatch)
            return;
        
        // 縦スクロール位置が閾値を超えたら追加読み込み
        // 1.0が一番上、 0.0が一番下
        if (position.y < (1.0f - scrollThreshold))
        {
            LoadNextBatchAsync().Forget();
        }
    }
    // ----------------------------------------------------------------------
    // SearchRouterの初期化
    // ----------------------------------------------------------------------
    private void InitializeSearchRouter()
    {
        // SearchRouterのインスタンスにパネル参照を設定
        if (SearchRouter.Instance != null)
        {
            SearchRouter.Instance.SetPanels(searchPanel, cardListPanel);
            
            // 初期状態では検索パネルを非表示に
            if (searchPanel != null)
            {
                searchPanel.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("❌ SearchRouterのインスタンスが取得できませんでした");
        }
    }

    // ----------------------------------------------------------------------
    // JSONデータを非同期で取得し、カード情報を初期化する
    // ----------------------------------------------------------------------
    private async UniTask LoadJsonAndInitializeAsync()
    {
        Debug.Log("🟢 JSON取得開始");

        try
        {
            using var request = UnityWebRequest.Get(jsonUrl);
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var jsonText = request.downloadHandler.text;

                // 取得したJSONをAllCardModelにデシリアライズ
                var loadedModel = JsonConvert.DeserializeObject<AllCardModel>(jsonText);

                Debug.Log("📷 カードデータを設定します");

                // 全カードを保存（検索用）
                allCards = loadedModel.cards;

                // 重要: CardDatabaseにカードデータをキャッシュとして設定
                // これにより、SearchModelがnullを返さないようになる
                CardDatabase.SetCachedCards(loadedModel.cards);
                Debug.Log($"🔄 CardDatabaseにカードデータを設定しました: {loadedModel.cards.Count}枚");
                
                // 検索モデルにカードデータを直接設定（nullエラー対策）
                if (searchView != null)
                {
                    searchView.SetCards(loadedModel.cards);
                }
            }
            else
            {
                Debug.LogError("❌ JSON読み込み失敗: " + request.error);

                // フォールバック: StreamingAssetsからローカルJSONを読み込む
                string localPath = Path.Combine(Application.streamingAssetsPath, "cards.json");
                if (File.Exists(localPath))
                {
                    Debug.Log("🔄 ローカルJSON読み込み: " + localPath);
                    string localJson = File.ReadAllText(localPath);
                    var loadedModel = JsonConvert.DeserializeObject<AllCardModel>(localJson);
                    allCards = loadedModel.cards;
                    CardDatabase.SetCachedCards(loadedModel.cards);
                    Debug.Log($"🔄 CardDatabaseにローカルJSONデータを設定しました: {loadedModel.cards.Count}枚");
                    if (searchView != null)
                    {
                        searchView.SetCards(loadedModel.cards);
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ フォールバック用ローカルJSONが見つかりません: " + localPath);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ JSONロード中に予期せぬエラーが発生しました: " + ex.Message);
            // ここでは例外を再スローせず初期化を継続します
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
            Debug.Log($"🔄 次の{batchSize}枚のカードを読み込みます（残り{remainingCards.Count}枚）");
            
            // バッチを取得
            List<CardModel> nextBatch = remainingCards.GetRange(0, batchSize);
            
            // サブバッチに分割して表示（スクロール時のカクつきを軽減）
            int subBatchSize = 5; // 一度に表示するカード数を小さく
            
            for (int i = 0; i < nextBatch.Count; i += subBatchSize)
            {
                // 残りのカード数からサブバッチサイズを計算
                int count = Mathf.Min(subBatchSize, nextBatch.Count - i);
                
                // サブバッチを抽出
                var subBatch = nextBatch.GetRange(i, count);
                
                // サブバッチを表示
                presenter.AddCards(subBatch);
                
                // UIが更新される時間を確保（1フレーム待機）
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            
            // 残りのリストから削除
            remainingCards.RemoveRange(0, batchSize);
            
            Debug.Log($"🔄 カードを追加表示しました。残り{remainingCards.Count}枚");
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
        if (SearchRouter.Instance != null)
        {
            SearchRouter.Instance.OnSearchResult -= HandleSearchResult;
        }
    }
}