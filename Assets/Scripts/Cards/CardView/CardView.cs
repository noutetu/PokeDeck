using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // UIイベント検出のため追加

// ----------------------------------------------------------------------
// カード1枚分のUI表示を担当するクラス
// カードモデルのデータをUIコンポーネントに反映し、適切な表示形式を選択する
// ----------------------------------------------------------------------
public class CardView : MonoBehaviour, IPointerClickHandler
{
    // ----------------------------------------------------------------------
    // カードの表示に使用するデータモデル
    // ----------------------------------------------------------------------
    private CardModel data;

    // ----------------------------------------------------------------------
    // UI表示用コンポーネント - Inspector上で設定する
    // ----------------------------------------------------------------------
    // 基本情報表示用コンポーネント
    [SerializeField] private RawImage cardImage;        // カード画像表示用
    [SerializeField] private Button cardButton;         // クリックイベント用ボタン
    [SerializeField] private GameObject initialHandSign; // 初手表示用テキスト

    // ----------------------------------------------------------------------
    // 画像読み込み状態の管理
    // ----------------------------------------------------------------------
    private bool isImageLoading = false;

    // ----------------------------------------------------------------------
    // ダブルクリック検出用変数
    // ----------------------------------------------------------------------
    private float lastClickTime;
    private float doubleClickTimeThreshold = 1f; // ダブルクリック判定の時間間隔（秒）

    // ----------------------------------------------------------------------
    // フィードバックテキスト表示用定数
    // ----------------------------------------------------------------------
    private const string ADD_SUCCESS_TEXT = "デッキに追加！";
    private const string SAME_CARD_LIMIT_TEXT = "同名カード上限";

    // ----------------------------------------------------------------------
    // Awakeメソッド - 初期化処理
    // ボタンコンポーネントがなければ追加
    // ボタンコンポーネントがある場合は、クリックイベントを登録
    // クリックイベントはIPointerClickHandlerインターフェースを実装している
    // ----------------------------------------------------------------------
    private void Awake()
    {
        // ボタンがなければ追加
        if (cardButton == null)
        {
            cardButton = GetComponent<Button>();
            if (cardButton == null)
            {
                cardButton = gameObject.AddComponent<Button>();
            }
        }
    }

    // ----------------------------------------------------------------------
    // カードデータを設定し、適切な表示形式でUIを更新する
    // @param data 表示するカードデータ
    // ----------------------------------------------------------------------
    public void SetImage(CardModel data)
    {
        this.data = data;
        
        if (data == null)
        {
            return;
        }
        
        // CardModelに保存されているテクスチャを直接表示
        if (data.imageTexture != null && cardImage != null)
        {
            cardImage.texture = data.imageTexture;
        }
        else if (cardImage != null)
        {
            // テクスチャがない場合はプレースホルダーを表示
            SetPlaceholderImage();
            // 画像ロード開始
            LoadImageAsync();
        }
    }

    // ----------------------------------------------------------------------
    // 画像を非同期で読み込み、完了後ステータスを非表示化
    // ----------------------------------------------------------------------
    private async void LoadImageAsync()
    {
        if (data == null || isImageLoading) return;
        isImageLoading = true;
        Texture2D texture = await ImageCacheManager.Instance.GetCardTextureAsync(data);
        data.imageTexture = texture;
        if (cardImage != null)
            cardImage.texture = texture;
        isImageLoading = false;
    }

    // ----------------------------------------------------------------------
    // プレースホルダー画像を設定
    // デフォルトのグレーテクスチャを生成
    // ----------------------------------------------------------------------
    private void SetPlaceholderImage()
    {
        if (ImageCacheManager.Instance != null && ImageCacheManager.Instance.GetDefaultTexture() != null)
        {
            cardImage.texture = ImageCacheManager.Instance.GetDefaultTexture();
        }
        else
        {
            // デフォルトのグレーテクスチャを生成
            var texture = new Texture2D(2, 2);
            Color32[] colors = new Color32[4];
            for (int i = 0; i < 4; i++)
            {
                colors[i] = new Color32(200, 200, 200, 255); // ライトグレー
            }
            texture.SetPixels32(colors);
            texture.Apply();
            cardImage.texture = texture;
        }
    }
    
    // ----------------------------------------------------------------------
    // クリックイベント処理 - ダブルクリックを検出してデッキに追加
    // ----------------------------------------------------------------------
    public void OnPointerClick(PointerEventData eventData)
    {
        float timeSinceLastClick = Time.time - lastClickTime;
        
        // ダブルクリック検出
        if (timeSinceLastClick < doubleClickTimeThreshold)
        {
            // ダブルクリック処理 - デッキに追加
            AddCardToDeck();
        }
        
        lastClickTime = Time.time;
    }
    
    // TODO 処理が長い
    // ----------------------------------------------------------------------
    // デッキにカードを追加する処理
    // ----------------------------------------------------------------------
    private void AddCardToDeck()
    {
        // カードデータがnullでないことを確認
        // DeckManagerがnullでないことを確認
        if (data != null && DeckManager.Instance != null)
        {
            // カードデータのデバッグ情報を出力
            Debug.Log($"⭐ カード追加: name={data.name}, id={data.id}, idString={data.id}");

            // CardDatabaseに登録してグローバルキャッシュに追加
            if (CardDatabase.Instance != null)
            {
                CardDatabase.Instance.RegisterCard(data);
            }

            // 同名カードが上限に達しているか確認
            if (!string.IsNullOrEmpty(data.name))
            {
                // カード名が空でない場合、同名カードの枚数を確認
                int sameNameCount = DeckManager.Instance.CurrentDeck.GetSameNameCardCount(data.name);

                // 同名カードの枚数が上限に達している場合、追加を拒否
                if (sameNameCount >= DeckModel.MAX_SAME_NAME_CARDS)
                {
                    ShowFailureFeedback($"{SAME_CARD_LIMIT_TEXT}（{DeckModel.MAX_SAME_NAME_CARDS}枚）");
                    return;
                }
            }

            // 現在のデッキが最大枚数に達しているか確認
            if (DeckManager.Instance.CurrentDeck.CardCount >= DeckModel.MAX_CARDS + 4)
            {
                ShowFailureFeedback("デッキは24枚まで追加可能です");
                return;
            }

            // カードがCardDatabaseに存在するか確認
            CardModel dbCard = null;
            if (CardDatabase.Instance != null)
            {
                dbCard = CardDatabase.Instance.GetCard(data.id);

                // カードがデータベースに存在しない場合、登録する
                if (dbCard == null)
                {
                    CardDatabase.Instance.RegisterCard(data);
                    dbCard = data; // 現在のデータを使用
                }
            }

            // 現在のデッキにカードを追加
            bool success = DeckManager.Instance.CurrentDeck.AddCard(data);

            // 追加できた場合
            if (success)
            {
                // メッセージを表示
                string feedbackMessage = $"{ADD_SUCCESS_TEXT} 「{data.name}」";
                ShowSuccessFeedback(feedbackMessage);
            }
            else
            {
                Debug.LogWarning($"カード '{data.name}' をデッキに追加できませんでした");
                // 失敗の詳細理由を特定して表示
                string failureReason = "追加失敗";

                if (DeckManager.Instance.CurrentDeck.CardCount >= DeckModel.MAX_CARDS + 4)
                {
                    failureReason = "デッキ上限（24枚）";
                }
                else if (DeckManager.Instance.CurrentDeck.GetSameNameCardCount(data.name) >= DeckModel.MAX_SAME_NAME_CARDS)
                {
                    failureReason = $"同名上限（{DeckModel.MAX_SAME_NAME_CARDS}枚）";
                }
                else if (dbCard == null)
                {
                    failureReason = "カードデータ不正";
                }

                ShowFailureFeedback(failureReason);
            }
        }
    }

    // ----------------------------------------------------------------------
    // 初期手札表示のオンオフを切り替える
    // ----------------------------------------------------------------------
    public void ToggleInitialHandDisplay(bool isVisible)
    {
        if (initialHandSign != null)
        {
            initialHandSign.gameObject.SetActive(isVisible);
        }
    }
    // ----------------------------------------------------------------------
    // 成功フィードバックメッセージを表示
    // ----------------------------------------------------------------------
    private void ShowSuccessFeedback(string message)
    {
        // FeedbackContainerを使用して画面上部に表示
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowSuccessFeedback(message);
        }
    }
    
    // ----------------------------------------------------------------------
    // 失敗フィードバックメッセージを表示
    // ----------------------------------------------------------------------
    private void ShowFailureFeedback(string message)
    {
        // FeedbackContainerを使用して画面上部に表示
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowFailureFeedback(message);
        }
    }
}