using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Security.Cryptography;
using UnityEngine.EventSystems; // UIイベント検出のため追加

// ----------------------------------------------------------------------
// カード1枚分のUI表示を担当するクラス
// カードモデルのデータをUIコンポーネントに反映し、適切な表示形式を選択する
// ----------------------------------------------------------------------
public class CardView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI loadingText; // ロード中ステータス表示用
    // カードの表示に使用するデータモデル
    private CardModel data;

    // ----------------------------------------------------------------------
    // UI表示用コンポーネント - Inspector上で設定する
    // ----------------------------------------------------------------------
    // 基本情報表示用コンポーネント
    [SerializeField] private RawImage cardImage;        // カード画像表示用
    [SerializeField] private Button cardButton;         // クリックイベント用ボタン
    
    // 画像読み込み状態の管理
    private bool isImageLoading = false;
    
    // ダブルクリック検出用変数
    private float lastClickTime;
    private float doubleClickTimeThreshold = 0.3f; // ダブルクリック判定の時間間隔（秒）
    
    // フィードバックテキスト表示用定数
    private const string ADD_SUCCESS_TEXT = "デッキに追加！";
    private const string ADD_FAILED_TEXT = "デッキが一杯です";
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
    public void Setup(CardModel data)
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
            loadingText?.gameObject.SetActive(false);
        }
        else if (cardImage != null)
        {
            // テクスチャがない場合はプレースホルダーを表示
            SetPlaceholderImage();
            // 読み込みステータスを表示
            if (loadingText != null)
            {
                loadingText.text = "読み込み中...";
                loadingText.gameObject.SetActive(true);
            }
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
        // ロード完了後ステータス非表示
        if (loadingText != null)
            loadingText.gameObject.SetActive(false);
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
    // ポケモンカードの表示処理
    // HP、タイプ、特性、技などポケモン特有の情報を表示
    // ----------------------------------------------------------------------
    private void ViewImage()
    {
        // 基本情報の設定
        if (data.imageTexture != null)
        {
            cardImage.texture = data.imageTexture;
        }
        else
        {
            SetPlaceholderImage();
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
        if (data != null && DeckManager.Instance != null)
        {
            // カードデータのデバッグ情報を出力
            Debug.Log($"⭐ カード追加: name={data.name}, id={data.id}, idString={data.id}");
            
            // データの整合性チェック
            if (string.IsNullOrEmpty(data.id))
            {
                Debug.LogError($"⭐ カードID文字列が空です: name={data.name}, id={data.id}");
            }
            
            // CardDatabaseに登録してグローバルキャッシュに追加
            if (CardDatabase.Instance != null)
            {
                CardDatabase.Instance.RegisterCard(data);
                Debug.Log($"⭐ CardDatabaseに登録: name={data.name}");
            }
            else 
            {
                Debug.LogError("⭐ CardDatabase.Instanceがnullです - カードをデータベースに登録できません");
            }
            
            // 同名カードが上限に達しているか確認
            if (!string.IsNullOrEmpty(data.name))
            {
                int sameNameCount = DeckManager.Instance.CurrentDeck.GetSameNameCardCount(data.name);
                Debug.Log($"⭐ 同名カード数: {sameNameCount}枚, カード名: {data.name}");
                
                if (sameNameCount >= DeckModel.MAX_SAME_NAME_CARDS)
                {
                    Debug.LogWarning($"同名カード「{data.name}」は{DeckModel.MAX_SAME_NAME_CARDS}枚までしか追加できません");
                    ShowFailureFeedback($"{SAME_CARD_LIMIT_TEXT}（{DeckModel.MAX_SAME_NAME_CARDS}枚）");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("⭐ カード名が空です");
                // カード名が空の場合、IDを名前として使用
                data.name = $"ID:{data.id}のカード";
                Debug.Log($"⭐ カード名を設定: {data.name}");
            }                // 現在のデッキが最大枚数に達しているか確認
            if (DeckManager.Instance.CurrentDeck.CardCount >= DeckModel.MAX_CARDS + 4)
            {
                Debug.LogWarning($"デッキが最大枚数(24枚)に達しています");
                ShowFailureFeedback("デッキは24枚まで追加可能");
                return;
            }
            
            // カードがCardDatabaseに存在するか確認
            CardModel dbCard = null;
            if (CardDatabase.Instance != null)
            {
                dbCard = CardDatabase.Instance.GetCard(data.id);
                if (dbCard == null)
                {
                    Debug.LogWarning($"⭐ CardDatabaseにカード(id={data.id})が存在しません。再登録します。");
                    CardDatabase.Instance.RegisterCard(data);
                    dbCard = data; // 現在のデータを使用
                }
            }
            
            // 現在のデッキにカードを追加
            bool success = DeckManager.Instance.CurrentDeck.AddCard(data);
            
            if (success)
            {
                Debug.Log($"⭐ カード '{data.name}' をデッキに追加しました, id={data.id}");
                string feedbackMessage = $"{ADD_SUCCESS_TEXT} 「{data.name}」";
                Debug.Log($"⭐ フィードバックメッセージ: {feedbackMessage}");
                ShowSuccessFeedback(feedbackMessage);
                
                // エネルギー要件のみ更新（デッキは保存しない）
                DeckManager.Instance.CurrentDeck.UpdateEnergyRequirements();
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
        else
        {
            // データまたはDeckManagerがnullの場合
            if (data == null)
            {
                Debug.LogError("⭐ カードデータ(data)がnullです");
            }
            if (DeckManager.Instance == null)
            {
                Debug.LogError("⭐ DeckManager.Instanceがnullです");
            }
            
            Debug.LogWarning("カードをデッキに追加できません：データが不足しています");
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
        else
        {
            Debug.LogWarning("⭐ FeedbackContainerが見つかりません。シーン上にFeedbackContainerオブジェクトを配置してください。");
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
        else
        {
            Debug.LogWarning("⭐ FeedbackContainerが見つかりません。シーン上にFeedbackContainerオブジェクトを配置してください。");
        }
    }
}