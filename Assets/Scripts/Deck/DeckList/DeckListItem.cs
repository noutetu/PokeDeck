using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

// ----------------------------------------------------------------------
// デッキ一覧パネルの各デッキアイテムを管理するクラス
// ----------------------------------------------------------------------
public class DeckListItem : MonoBehaviour
{
    [SerializeField] private TMP_InputField deckNameInput;      // デッキ名入力フィールド
    [SerializeField] private RawImage cardImage;        // デッキアイコン画像
    [SerializeField] private Button selectButton;       // デッキ選択ボタン
    [SerializeField] private Button deleteButton;       // デッキ削除ボタン
    
    // デッキ選択イベント
    public UnityEvent OnDeckSelected = new UnityEvent();
    
    private DeckModel currentDeck;  // 現在のデッキ情報 

    // ----------------------------------------------------------------------
    // Unityの初期化処理
    // ----------------------------------------------------------------------
    private void Start()
    {
        // 選択ボタンのイベント設定
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(() => {
                OnDeckSelected.Invoke();
            });
        }
        
        // デッキ名入力フィールドのイベント設定
        if (deckNameInput != null)
        {
            // デッキ名変更時に保存
            deckNameInput.onEndEdit.AddListener(OnDeckNameChanged);
        }
        
        // 削除ボタンのイベント設定
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }
    }
    
    // ----------------------------------------------------------------------
    /// デッキ情報を設定
    // ----------------------------------------------------------------------
    public void SetDeckInfo(DeckModel deck)
    {
        currentDeck = deck;
        
        if (deckNameInput != null && deck != null)
        {
            deckNameInput.text = deck.Name;
        }
        
        // 最も体力の高いポケモンをアイコンに設定
        SetHighestHPPokemonAsIcon(deck);
    }
    
    // ----------------------------------------------------------------------
    /// デッキ名変更時の処理
    // ----------------------------------------------------------------------
    private void OnDeckNameChanged(string newName)
    {
        if (currentDeck == null)
            return;
            
        // 空の場合は何もしない（元の名前を維持）
        if (string.IsNullOrEmpty(newName))
        {
            deckNameInput.text = currentDeck.Name;
            return;
        }
            
        // 現在のデッキ名と異なる場合のみ保存処理
        if (currentDeck.Name != newName)
        {
            // デッキ名を更新
            string oldName = currentDeck.Name;
            currentDeck.Name = newName;
            
            // DeckManagerに変更を保存
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.SaveCurrentDeck();
                
                // フィードバック表示
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowSuccessFeedback($"デッキ名を変更しました: {oldName} → {newName}");
                }
            }
        }
    }
    
    // ----------------------------------------------------------------------
    /// 最も体力の高いポケモンをアイコンに設定
    // ----------------------------------------------------------------------
    private void SetHighestHPPokemonAsIcon(DeckModel deck)
    {
        if (cardImage == null || deck == null)
            return;
            
        CardModel highestHPCard = null;
        int highestHP = 0;
        
        // デッキ内のすべてのカードをチェック
        foreach (string cardId in deck.CardIds)
        {
            CardModel card = deck.GetCardModel(cardId);
            
            // ポケモンカードで、HPが最高値のカードを探す
            if (card != null && 
                (card.cardTypeEnum == Enum.CardType.非EX || card.cardTypeEnum == Enum.CardType.EX) &&
                card.hp > highestHP)
            {
                highestHP = card.hp;
                highestHPCard = card;
            }
        }
        
        // 最も体力の高いポケモンが見つかった場合、そのテクスチャを設定
        if (highestHPCard != null && highestHPCard.imageTexture != null)
        {
            cardImage.texture = highestHPCard.imageTexture;
        }
        else
        {
            // デフォルトのテクスチャに設定（必要に応じて）
            // cardImage.texture = defaultTexture;
        }
    }

    // ----------------------------------------------------------------------
    /// 削除ボタンクリック時の処理
    // ----------------------------------------------------------------------
    private void OnDeleteButtonClicked()
    {
        if (currentDeck == null)
            return;
            
        // デッキ名を保持（フィードバック表示用）
        string deckName = currentDeck.Name;
        
        // DeckManagerを使用してデッキを削除
        if (DeckManager.Instance != null)
        {
            bool success = DeckManager.Instance.DeleteDeck(deckName);
            
            if (success)
            {
                // フィードバック表示
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowSuccessFeedback($"デッキを削除しました: {deckName}");
                }
                
                // 親のDeckListPanelを取得して更新を通知
                var deckListPanel = GetComponentInParent<DeckListPanel>();
                if (deckListPanel != null)
                {
                    deckListPanel.RefreshDeckList();
                }
            }
            else
            {
                // 削除失敗時のフィードバック
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowFailureFeedback($"デッキの削除に失敗しました: {deckName}");
                }
            }
        }
    }
}