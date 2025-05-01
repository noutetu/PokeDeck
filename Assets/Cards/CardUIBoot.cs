using UnityEngine.Networking;
using Cysharp.Threading.Tasks;      // UniTaskライブラリ - 非同期処理用
using Newtonsoft.Json;              // JSON変換用ライブラリ
using System.Collections.Generic;
using UnityEngine;

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
    
    // カードデータを取得するJSONのURL（リモートホスティング）
    private const string jsonUrl = "https://noutetu.github.io/PokeDeckCards/output.json";

    // デフォルトテクスチャの参照
    [SerializeField] private Texture2D defaultCardTexture;    // 画像がない場合のデフォルト画像

    // ----------------------------------------------------------------------
    // 初期化処理 - MVRPコンポーネント作成とデータロード開始
    // ----------------------------------------------------------------------
    private async void Start()
    {
        // MVRPパターンの初期化
        model = new AllCardModel();                  // モデル作成
        presenter = new AllCardPresenter(model);     // プレゼンター作成（モデルを注入）
        allCardView.BindPresenter(presenter);        // ビューとプレゼンターを接続（購読設定）

        // SearchRouterの初期化（パネル参照の設定）
        InitializeSearchRouter();
        
        // JSON取得とカード情報のロード開始
        await LoadJsonAndInitializeAsync();
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

        // WebRequestを使ってJSONデータを取得
        using var request = UnityWebRequest.Get(jsonUrl);
        await request.SendWebRequest();  // 非同期でリクエスト送信

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("🟢 JSON取得成功");
            var jsonText = request.downloadHandler.text;

            // 取得したJSONをAllCardModelにデシリアライズ
            var loadedModel = JsonConvert.DeserializeObject<AllCardModel>(jsonText);

            Debug.Log("📷 画像の読み込み開始");

            // バッチ処理で画像を読み込み（パフォーマンス最適化）
            // 10枚ずつ画像読み込み＆表示することで、一度に多数の通信を発生させない
            await SetImagesInBatches(loadedModel.cards, 50);

            // 重要: CardDatabaseにカードデータをキャッシュとして設定
            // これにより、SearchModelがnullを返さないようになる
            CardDatabase.SetCachedCards(loadedModel.cards);
            Debug.Log($"🔄 CardDatabaseにカードデータを設定しました: {loadedModel.cards.Count}枚");
            
            // 検索モデルにカードデータを直接設定（nullエラー対策）
            if (searchView != null)
            {
                searchView.SetCards(loadedModel.cards);
            }
            
            Debug.Log("✅ すべてのカード表示完了");
        }
        else
        {
            Debug.LogError("❌ JSON読み込み失敗: " + request.error);
        }
    }

    // ----------------------------------------------------------------------
    // カードデータのバッチ処理による画像ロードと表示
    // 指定数ごとに画像を読み込み、UIに表示する
    // @param cards 処理対象のカードリスト
    // @param batchSize 一度に処理するカード数
    // ----------------------------------------------------------------------
    private async UniTask SetImagesInBatches(List<CardModel> cards, int batchSize)
    {
        int total = cards.Count;

        for (int i = 0; i < total; i += batchSize)
        {
            // 現在のバッチ分だけカードを取り出す
            // 残りが少ない場合は、残り全部を取得
            var batch = cards.GetRange(i, Mathf.Min(batchSize, total - i));

            // バッチ内の全カードの画像を並列で読み込む
            var tasks = new List<UniTask>();
            foreach (var card in batch)
            {
                tasks.Add(DownloadAndAssignImage(card));
            }
            await UniTask.WhenAll(tasks);  // すべての画像読み込みが完了するまで待機

            // バッチ内のカードをUIに表示追加
            presenter.AddCards(batch);
        }
    }
    // ----------------------------------------------------------------------
    // 1枚のカード画像を非同期でダウンロードし、カードモデルに設定する
    // @param card 画像を設定するカードモデル
    // ----------------------------------------------------------------------
    private async UniTask DownloadAndAssignImage(CardModel card)
    {
        // カード名が無効な場合はスキップ
        if (string.IsNullOrEmpty(card.name) || card.name == "NaN")
        {
            Debug.LogWarning($"⚠️ 無効なカード名のため画像ダウンロードをスキップ: 「{card.name}」, URL: {card.imageKey}");
            AssignDefaultTexture(card);
            return;
        }

        // URLが空の場合はデフォルト画像を使用
        if (string.IsNullOrEmpty(card.imageKey))
        {
            Debug.LogWarning($"⚠️ 画像URLが空のため、デフォルト画像を使用: カード名「{card.name}」");
            AssignDefaultTexture(card);
            return;
        }

        try
        {
            // WebRequestを使ってテクスチャを取得
            using var request = UnityWebRequestTexture.GetTexture(card.imageKey);
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // ダウンロードしたテクスチャをカードモデルに設定
                card.imageTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            }
            else
            {
                // エラー時にはカード名とURLを両方表示
                Debug.LogError($"❌ 画像読み込み失敗: カード名「{card.name}」, URL: {card.imageKey}, エラー: {request.error}");
                
                // エラーの詳細情報も出力（デバッグに役立つ）
                if (request.responseCode == 404)
                {
                    Debug.LogWarning($"⚠️ 画像が見つかりません（404 Not Found）: カード名「{card.name}」");
                }
                
                // エラー時はデフォルト画像を使用
                AssignDefaultTexture(card);
            }
        }
        catch (System.Exception ex)
        {
            // 例外発生時もカード名を表示
            Debug.LogError($"❌ 画像読み込み中に例外が発生: カード名「{card.name}」, URL: {card.imageKey}, 例外: {ex.Message}");
            
            // 例外発生時もデフォルト画像を使用
            AssignDefaultTexture(card);
        }
    }

    // ----------------------------------------------------------------------
    // カードにデフォルトテクスチャを設定する
    // @param card テクスチャを設定するカード
    // ----------------------------------------------------------------------
    private void AssignDefaultTexture(CardModel card)
    {
        // デフォルトテクスチャが設定されていればそれを使用
        if (defaultCardTexture != null)
        {
            card.imageTexture = defaultCardTexture;
        }
        else
        {
            // デフォルトテクスチャが未設定の場合は空のテクスチャを作成
            var texture = new Texture2D(512, 712, TextureFormat.RGBA32, false);
            // グレーで塗りつぶし
            Color32[] colors = new Color32[texture.width * texture.height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color32(200, 200, 200, 255); // ライトグレー
            }
            texture.SetPixels32(colors);
            texture.Apply();
            
            // カード名をテクスチャに描画（簡易的な方法）
            card.imageTexture = texture;
            
            Debug.LogWarning($"⚠️ デフォルトテクスチャが未設定のため、グレーのテクスチャを使用: カード名「{card.name}」");
        }
    }
}