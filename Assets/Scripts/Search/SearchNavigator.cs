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
            // 購読者がいるかチェック
            if (OnSearchResult != null)
            {
                int subscriberCount = OnSearchResult.GetInvocationList().Length;
                
                // イベント発火
                OnSearchResult.Invoke(results);
            }
        }
    }
}