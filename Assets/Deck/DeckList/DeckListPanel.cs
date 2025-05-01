using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ----------------------------------------------------------------------
// デッキ一覧パネルを管理するクラス
// ----------------------------------------------------------------------
public class DeckListPanel : MonoBehaviour
{
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject deckDetailPrefab;
    [SerializeField] private GameObject deckPanel;
    [SerializeField] private DeckView deckView;
    [SerializeField] private Button closeButton;
    
    private List<GameObject> deckItems = new List<GameObject>();
    
    private void OnEnable()
    {
        // パネルが表示されるたびにデッキリストを更新
        RefreshDeckList();
        
        // DeckViewを非表示にする
        if (deckPanel != null)
        {
            deckPanel.SetActive(false);
        }
    }
    
    private void Start()
    {
        // 閉じるボタンのイベント設定
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => {
                gameObject.SetActive(false);
                
                // デッキパネルを表示
                if (deckPanel != null)
                {
                    deckPanel.SetActive(true);
                }
            });
        }
    }
    
    /// <summary>
    /// デッキ一覧を最新の状態に更新
    /// </summary>
    public void RefreshDeckList()
    {
        // 既存のデッキアイテムをクリア
        ClearDeckItems();
        
        // 保存されているデッキをすべて取得
        if (DeckManager.Instance != null)
        {
            foreach (var deck in DeckManager.Instance.SavedDecks)
            {
                CreateDeckItem(deck);
            }
        }
    }
    
    /// <summary>
    /// デッキアイテムをすべて削除
    /// </summary>
    private void ClearDeckItems()
    {
        foreach (var item in deckItems)
        {
            Destroy(item);
        }
        
        deckItems.Clear();
    }
    
    /// <summary>
    /// デッキアイテムを生成
    /// </summary>
    private void CreateDeckItem(Deck deck)
    {
        if (deckDetailPrefab == null || contentContainer == null)
            return;
            
        // デッキアイテムのプレハブを生成
        GameObject deckItem = Instantiate(deckDetailPrefab, contentContainer);
        deckItems.Add(deckItem);
        
        // デッキアイテムコンポーネントを設定
        DeckListItem itemComponent = deckItem.GetComponent<DeckListItem>();
        if (itemComponent != null)
        {
            // デッキ情報を設定
            itemComponent.SetDeckInfo(deck);
            
            // クリックイベントを設定
            itemComponent.OnDeckSelected.AddListener(() => {
                SelectDeck(deck.Name);
            });
        }
    }
    
    /// <summary>
    /// デッキを選択
    /// </summary>
    private void SelectDeck(string deckName)
    {
        // DeckManagerで指定デッキを選択
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.SelectDeck(deckName);
            
            // デッキパネルに表示を反映
            if (deckView != null)
            {
                deckView.DisplayDeck(DeckManager.Instance.CurrentDeck);
            }
            
            // デッキ一覧パネルを閉じる
            gameObject.SetActive(false);
            
            // デッキパネルを表示
            if (deckPanel != null)
            {
                deckPanel.SetActive(true);
            }
        }
    }
}