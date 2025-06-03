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
        
        // ImageCacheManagerを使用してキャッシュを確認
        if (ImageCacheManager.Instance != null && ImageCacheManager.Instance.IsCardTextureCached(data))
        {
            // キャッシュにある場合は即座に表示
            Texture2D cachedTexture = ImageCacheManager.Instance.GetCachedCardTexture(data);
            if (cardImage != null && cachedTexture != null)
            {
                cardImage.texture = cachedTexture;
                data.imageTexture = cachedTexture;
            }
        }
        else if (data.imageTexture != null && cardImage != null)
        {
            // CardModelに保存されているテクスチャを直接表示
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
        
        try
        {
            // ImageCacheManagerを使用して画像を読み込み
            Texture2D texture = await ImageCacheManager.Instance.GetCardTextureAsync(data);
            
            // UI要素がまだ有効かチェック
            if (cardImage != null && texture != null)
            {
                cardImage.texture = texture;
                data.imageTexture = texture;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"カード画像の読み込みでエラー: {ex.Message}");
            // エラー時はデフォルトテクスチャまたはプレースホルダーを設定
            if (cardImage != null)
            {
                if (ImageCacheManager.Instance != null)
                {
                    cardImage.texture = ImageCacheManager.Instance.GetDefaultTexture();
                }
                else
                {
                    SetPlaceholderImage();
                }
            }
        }
        finally
        {
            isImageLoading = false;
        }
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
    
    // ----------------------------------------------------------------------
    // デッキにカードを追加する処理
    // ----------------------------------------------------------------------
    private void AddCardToDeck()
    {
        if (!ValidateBasicRequirements())
            return;

        // CardDatabaseに登録
        RegisterCardToDatabase();

        // バリデーション実行
        var validationResult = ValidateCardAddition();
        if (!validationResult.IsValid)
        {
            ShowFailureFeedback(validationResult.ErrorMessage);
            return;
        }

        // デッキに追加実行
        ExecuteCardAddition();
    }

    // ----------------------------------------------------------------------
    // 基本要件のバリデーション
    // ----------------------------------------------------------------------
    private bool ValidateBasicRequirements()
    {
        return data != null && DeckManager.Instance != null;
    }

    // ----------------------------------------------------------------------
    // CardDatabaseへの登録
    // ----------------------------------------------------------------------
    private void RegisterCardToDatabase()
    {
        if (CardDatabase.Instance != null)
        {
            CardDatabase.Instance.RegisterCard(data);
        }
    }

    // ----------------------------------------------------------------------
    // カード追加のバリデーション
    // ----------------------------------------------------------------------
    private (bool IsValid, string ErrorMessage) ValidateCardAddition()
    {
        // 同名カード上限チェック
        if (!string.IsNullOrEmpty(data.name))
        {
            int sameNameCount = DeckManager.Instance.CurrentDeck.GetSameNameCardCount(data.name);
            if (sameNameCount >= DeckModel.MAX_SAME_NAME_CARDS)
            {
                return (false, $"{SAME_CARD_LIMIT_TEXT}（{DeckModel.MAX_SAME_NAME_CARDS}枚）");
            }
        }

        // デッキ上限チェック
        if (DeckManager.Instance.CurrentDeck.CardCount >= DeckModel.MAX_CARDS + 4)
        {
            return (false, "デッキは24枚まで追加可能です");
        }

        return (true, string.Empty);
    }

    // ----------------------------------------------------------------------
    // カード追加の実行
    // ----------------------------------------------------------------------
    private void ExecuteCardAddition()
    {
        // カードがCardDatabaseに存在するか確認・登録
        CardModel dbCard = EnsureCardInDatabase();

        // デッキに追加実行
        bool success = DeckManager.Instance.CurrentDeck.AddCard(data);

        // 結果に応じてフィードバック表示
        if (success)
        {
            ShowAdditionSuccessFeedback();
        }
        else
        {
            ShowAdditionFailureFeedback();
        }
    }

    // ----------------------------------------------------------------------
    // カードがデータベースに存在することを保証
    // ----------------------------------------------------------------------
    private CardModel EnsureCardInDatabase()
    {
        if (CardDatabase.Instance == null)
            return data;

        CardModel dbCard = CardDatabase.Instance.GetCard(data.id);
        if (dbCard == null)
        {
            CardDatabase.Instance.RegisterCard(data);
            dbCard = data;
        }
        return dbCard;
    }

    // ----------------------------------------------------------------------
    // 追加成功時のフィードバック表示
    // ----------------------------------------------------------------------
    private void ShowAdditionSuccessFeedback()
    {
        int deckSize = DeckManager.Instance.CurrentDeck.CardCount;
        int maxDeckSize = DeckModel.MAX_CARDS;
        string message = $"「{data.name}」をデッキに追加しました！\n" +
                         $"デッキサイズ: {deckSize}/{maxDeckSize}";
        ShowSuccessFeedback(message);
    }

    // ----------------------------------------------------------------------
    // 追加失敗時のフィードバック表示
    // ----------------------------------------------------------------------
    private void ShowAdditionFailureFeedback()
    {
        string failureReason = DetermineFailureReason();
        ShowFailureFeedback(failureReason);
    }

    // ----------------------------------------------------------------------
    // 失敗理由の特定
    // ----------------------------------------------------------------------
    private string DetermineFailureReason()
    {
        if (DeckManager.Instance.CurrentDeck.CardCount >= DeckModel.MAX_CARDS + 4)
        {
            return "デッキ上限（24枚）";
        }
        
        if (DeckManager.Instance.CurrentDeck.GetSameNameCardCount(data.name) >= DeckModel.MAX_SAME_NAME_CARDS)
        {
            return $"同名上限（{DeckModel.MAX_SAME_NAME_CARDS}枚）";
        }
        
        return "追加失敗";
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