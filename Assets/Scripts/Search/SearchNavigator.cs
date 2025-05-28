using System;
using System.Collections.Generic;
using UnityEngine;

// ----------------------------------------------------------------------
// 検索関連のパネル表示/非表示を管理するシングルトンクラス
// パネル間のナビゲーションや検索結果の伝達を担当
// ----------------------------------------------------------------------
public class SearchNavigator : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // シングルトンパターンの実装
    // ----------------------------------------------------------------------
    private static SearchNavigator _instance;
    
    public static SearchNavigator Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject routerObj = new GameObject("SearchRouter");
                _instance = routerObj.AddComponent<SearchNavigator>();
                DontDestroyOnLoad(routerObj);
            }
            
            return _instance;
        }
    }
    
    // ----------------------------------------------------------------------
    // Awakeメソッド - シングルトンの初期化
    // ----------------------------------------------------------------------
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // ----------------------------------------------------------------------
    // パネル参照
    // ----------------------------------------------------------------------
    [SerializeField] private GameObject searchPanel;         // 検索入力パネル
    [SerializeField] private GameObject cardListPanel;       // カードリストパネル
    
    // ----------------------------------------------------------------------
    // 検索結果イベント - カードリストに検索結果を通知するためのイベント
    // ----------------------------------------------------------------------
    public event Action<List<CardModel>> OnSearchResult;
    
    // 最後に適用された検索結果
    private List<CardModel> lastResults = new List<CardModel>();
    
    // ----------------------------------------------------------------------
    // パネル参照を設定
    // @param search 検索パネル
    // @param cardList カードリストパネル
    // ----------------------------------------------------------------------
    public void SetPanels(GameObject search, GameObject cardList)
    {
        searchPanel = search;
        cardListPanel = cardList;
        
        // 初期状態では検索パネルを非表示に
        if (searchPanel) searchPanel.SetActive(false);
        if (cardListPanel && !cardListPanel.activeSelf) cardListPanel.SetActive(true);
    }
    
    // ----------------------------------------------------------------------
    // 検索パネルを表示
    // ----------------------------------------------------------------------
    public void ShowSearchPanel()
    {
        if (searchPanel == null || searchPanel.activeSelf) return;
        
        searchPanel.SetActive(true);
    }
    
    // ----------------------------------------------------------------------
    // 検索パネルを非表示
    // ----------------------------------------------------------------------
    public void HideSearchPanel()
    {
        if (searchPanel == null || !searchPanel.activeSelf) return;
        
        searchPanel.SetActive(false);
    }
    
    // ----------------------------------------------------------------------
    // 検索結果をカードリストに反映
    // @param results 検索結果のカードリスト
    // ----------------------------------------------------------------------
    public void ApplySearchResults(List<CardModel> results)
    {
        
        if (results != null)
        {
            // 検索結果を保存
            lastResults = new List<CardModel>(results);
            
            // 購読者がいるかチェック
            if (OnSearchResult != null)
            {
                int subscriberCount = OnSearchResult.GetInvocationList().Length;
                
                // イベント発火
                OnSearchResult.Invoke(results);
            }
        }
    }

    // ----------------------------------------------------------------------
    // 検索のキャンセル
    // パネルを閉じるが、初回のみ全カードを表示
    // ----------------------------------------------------------------------
    public void CancelSearch()
    {
        // パネルを非表示
        HideSearchPanel();
        
        // 初回キャンセル時（lastResultsが空）には全カードを表示
        if (lastResults.Count == 0)
        {
            // 全カードをデータベースから取得
            List<CardModel> allCards = CardDatabase.GetAllCards();
            if (allCards != null && allCards.Count > 0)
            {
                // 結果を保存
                lastResults = new List<CardModel>(allCards);
                
                // 全カードを表示
                OnSearchResult?.Invoke(allCards);
            }
        }
    }
}