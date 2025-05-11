using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ----------------------------------------------------------------------
// デッキ一覧パネルを管理するクラス
// ----------------------------------------------------------------------
public class DeckListPanel : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // フィールド変数
    // ----------------------------------------------------------------------
    [SerializeField] private Transform contentContainer;    // デッキ一覧のコンテナ
    [SerializeField] private GameObject deckDetailPrefab;   // デッキ詳細アイテムのプレハブ
    [SerializeField] private GameObject deckPanel;       // デッキパネル
    [SerializeField] private DeckView deckView;       // デッキビュー
    [SerializeField] private Button closeButton;       // 閉じるボタン
    
    private List<GameObject> deckItems = new List<GameObject>();    // デッキアイテムのリスト
    
    // ----------------------------------------------------------------------
    // Unityの初期化メソッド
    // ----------------------------------------------------------------------
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
    
    // ----------------------------------------------------------------------
    // Unityの初期化メソッド(初回のみ)
    // ----------------------------------------------------------------------
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
    
    // ----------------------------------------------------------------------
    // デッキ一覧を最新の状態に更新
    // ----------------------------------------------------------------------
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
    
    // ----------------------------------------------------------------------
    // デッキアイテムをすべて削除
    // ----------------------------------------------------------------------
    private void ClearDeckItems()
    {
        foreach (var item in deckItems)
        {
            Destroy(item);
        }
        
        deckItems.Clear();
    }
    
    // ----------------------------------------------------------------------
    // デッキアイテムを生成
    // ----------------------------------------------------------------------
    private void CreateDeckItem(DeckModel deck)
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
    
    // ----------------------------------------------------------------------
    // デッキを選択
    // ----------------------------------------------------------------------
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