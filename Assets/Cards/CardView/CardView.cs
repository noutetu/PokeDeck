using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using System.Security.Cryptography;
using UnityEngine.EventSystems; // UIイベント検出のため追加

// ----------------------------------------------------------------------
// カード1枚分のUI表示を担当するクラス
// カードモデルのデータをUIコンポーネントに反映し、適切な表示形式を選択する
// ----------------------------------------------------------------------
public class CardView : MonoBehaviour, IPointerClickHandler
{
    // カードの表示に使用するデータモデル
    private CardModel data;

    // ----------------------------------------------------------------------
    // UI表示用コンポーネント - Inspector上で設定する
    // ----------------------------------------------------------------------
    // 基本情報表示用コンポーネント
    [SerializeField] private RawImage cardImage;        // カード画像表示用
    [SerializeField] private Button cardButton;         // クリックイベント用ボタン
    
    // ダブルクリック検出用変数
    private float lastClickTime;
    private float doubleClickTimeThreshold = 0.3f; // ダブルクリック判定の時間間隔（秒）
    
    // フィードバックテキスト表示用定数
    private const string ADD_SUCCESS_TEXT = "デッキに追加！";
    private const string ADD_FAILED_TEXT = "デッキが一杯です";
    private const string SAME_CARD_LIMIT_TEXT = "同名カード上限";

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
        ViewImage();
    }
    
    // ----------------------------------------------------------------------
    // ポケモンカードの表示処理
    // HP、タイプ、特性、技などポケモン特有の情報を表示
    // ----------------------------------------------------------------------
    private void ViewImage()
    {
        // 基本情報の設定
        cardImage.texture = data.imageTexture;
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
            Debug.Log($"⭐ カード追加: name={data.name}, id={data.id}");
            
            // CardDatabaseに登録してグローバルキャッシュに追加
            if (CardDatabase.Instance != null)
            {
                CardDatabase.Instance.RegisterCard(data);
                Debug.Log($"⭐ CardDatabaseに登録: name={data.name}");
            }
            
            // 同名カードが上限に達しているか確認
            if (!string.IsNullOrEmpty(data.name))
            {
                int sameNameCount = DeckManager.Instance.CurrentDeck.GetSameNameCardCount(data.name);
                Debug.Log($"⭐ 同名カード数: {sameNameCount}枚, カード名: {data.name}");
                
                if (sameNameCount >= Deck.MAX_SAME_NAME_CARDS)
                {
                    Debug.LogWarning($"同名カード「{data.name}」は{Deck.MAX_SAME_NAME_CARDS}枚までしか追加できません");
                    ShowFailureFeedback($"{SAME_CARD_LIMIT_TEXT}（{Deck.MAX_SAME_NAME_CARDS}枚）");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("⭐ カード名が空です");
            }
            
            // 現在のデッキが最大枚数に達しているか確認
            if (DeckManager.Instance.CurrentDeck.CardCount >= Deck.MAX_CARDS)
            {
                Debug.LogWarning($"デッキが最大枚数({Deck.MAX_CARDS}枚)に達しています");
                ShowFailureFeedback(ADD_FAILED_TEXT);
                return;
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
                ShowFailureFeedback("追加失敗");
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
        Debug.Log($"⭐ ShowSuccessFeedback呼び出し: message='{message}'");
        
        // FeedbackContainerを使用して画面上部に表示
        if (FeedbackContainer.Instance != null)
        {
            Debug.Log($"⭐ FeedbackContainer.Instance.ShowSuccessFeedback呼び出し");
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
        Debug.Log($"⭐ ShowFailureFeedback呼び出し: message='{message}'");
        
        // FeedbackContainerを使用して画面上部に表示
        if (FeedbackContainer.Instance != null)
        {
            Debug.Log($"⭐ FeedbackContainer.Instance.ShowFailureFeedback呼び出し");
            FeedbackContainer.Instance.ShowFailureFeedback(message);
        }
        else
        {
            Debug.LogWarning("⭐ FeedbackContainerが見つかりません。シーン上にFeedbackContainerオブジェクトを配置してください。");
        }
    }
}