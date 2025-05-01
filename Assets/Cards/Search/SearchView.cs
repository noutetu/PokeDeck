// ----------------------------------------------------------------------
// カード検索画面のView
// ユーザーからの入力を受け取り、検索条件の設定と結果表示を行う
// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SearchView : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Inspector上で設定するコンポーネント
    // ----------------------------------------------------------------------
    [Header("検索入力UI")]
    [SerializeField] private Button applyButton;               // OKボタン（決定）
    [SerializeField] private Button cancelButton;              // 閉じるボタン
    [SerializeField] private Button clearButton;               // フィルタリングリセットボタン
    
    [Header("フィルターエリア")]
    [SerializeField] private SetCardTypeArea cardTypeArea;     // カードタイプフィルターエリア
    [SerializeField] private SetEvolutionStageArea evolutionStageArea; // 進化段階フィルターエリア
    [SerializeField] private SetTypeArea typeArea;             // ポケモンタイプフィルターエリア
    [SerializeField] private SetCardPackArea cardPackArea;     // カードパックフィルターエリア
    [SerializeField] private SetHPArea hpArea;                 // HPフィルターエリア
    [SerializeField] private SetMaxDamageArea maxDamageArea;   // 最大ダメージフィルターエリア
    [SerializeField] private SetMaxEnergyArea maxEnergyCostArea; // 最大エネルギーコストフィルターエリア
    
    [Header("結果プレビュー表示UI")]
    [SerializeField] private Transform cardContainer;          // 検索結果表示用コンテナ
    [SerializeField] private GameObject cardPrefab;            // カードプレハブ
    [SerializeField] private ScrollRect scrollRect;            // スクロール領域（オプション）

    // ----------------------------------------------------------------------
    // MVP管理用
    // ----------------------------------------------------------------------
    private SearchPresenter presenter;
    private SearchModel model;
    private List<CardModel> currentResults = new List<CardModel>();
    
    // ----------------------------------------------------------------------
    // イベント
    // ----------------------------------------------------------------------
    public event Action OnSearchButtonClicked;
    public event Action OnClearButtonClicked;
    
    // ----------------------------------------------------------------------
    // 初期化処理
    // ----------------------------------------------------------------------
    private void Start()
    {
        InitializeUI();
        SetupListeners();
        SetupMVP();
    }

    // ----------------------------------------------------------------------
    // UI初期化処理
    // ----------------------------------------------------------------------
    private void InitializeUI()
    {
        // スクロール位置のリセット
        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = new Vector2(0, 1);
        }
    }
    
    // ----------------------------------------------------------------------
    // MVP構造のセットアップ
    // ----------------------------------------------------------------------
    private void SetupMVP()
    {
        // モデルの作成
        model = new SearchModel();
        
        // プレゼンターの作成とビューへの接続
        presenter = new SearchPresenter(this, model);
        
        // フィルターエリアの登録
        if (cardTypeArea != null)
        {
            presenter.RegisterCardTypeArea(cardTypeArea);
        }
        else
        {
            Debug.LogWarning("⚠️ SetCardTypeAreaコンポーネントが設定されていません");
        }
        
        // 進化段階フィルターエリアの登録
        if (evolutionStageArea != null)
        {
            presenter.RegisterEvolutionStageArea(evolutionStageArea);
        }
        else
        {
            Debug.LogWarning("⚠️ SetEvolutionStageAreaコンポーネントが設定されていません");
        }
        
        // ポケモンタイプフィルターエリアの登録
        if (typeArea != null)
        {
            presenter.RegisterTypeArea(typeArea);
        }
        else
        {
            Debug.LogWarning("⚠️ SetTypeAreaコンポーネントが設定されていません");
        }
        
        // カードパックフィルターエリアの登録
        if (cardPackArea != null)
        {
            presenter.RegisterCardPackArea(cardPackArea);
        }
        else
        {
            Debug.LogWarning("⚠️ SetCardPackAreaコンポーネントが設定されていません");
        }

        // HPフィルターエリアの登録
        if (hpArea != null)
        {
            presenter.RegisterHPArea(hpArea);
        }
        else
        {
            Debug.LogWarning("⚠️ SetHPAreaコンポーネントが設定されていません");
        }
        
        // 最大ダメージフィルターエリアの登録
        if (maxDamageArea != null)
        {
            presenter.RegisterMaxDamageArea(maxDamageArea);
        }
        else
        {
            Debug.LogWarning("⚠️ SetMaxDamageAreaコンポーネントが設定されていません");
        }

        // 最大エネルギーコストフィルターエリアの登録
        if (maxEnergyCostArea != null)
        {
            // presenter.RegisterMaxEnergyCostArea(maxEnergyCostArea);
        }
        else
        {
            Debug.LogWarning("⚠️ SetMaxEnergyAreaコンポーネントが設定されていません");
        }
    }
    
    // ----------------------------------------------------------------------
    // カードデータを外部から設定（エラー対策）
    // ----------------------------------------------------------------------
    public void SetCards(List<CardModel> cards)
    {
        if (model == null)
        {
            // modelがnullの場合は初期化する
            SetupMVP();
        }
        
        if (model != null)
        {
            model.SetCards(cards);
        }
        else
        {
            Debug.LogWarning("⚠️ SearchModelがnullです。SetCards()が失敗しました。");
        }
    }
    
    // ----------------------------------------------------------------------
    // リスナーの設定
    // ----------------------------------------------------------------------
    private void SetupListeners()
    {
        // OKボタン（決定）クリック - 検索実行と結果の適用
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(() => {
                // 検索を実行
                OnSearchButtonClicked?.Invoke();
                // 検索結果をメインのカードリストに適用して検索パネルを閉じる
                ApplySearchResults();
            });
        }
        
        // クリアボタンクリック（フィルタリングリセット）
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(() => {
                OnClearButtonClicked?.Invoke();
                ResetUI();
            });
        }
        
        // 閉じるボタンクリック
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CloseSearchPanel);
        }
    }
    
    // ----------------------------------------------------------------------
    // 検索結果を適用して検索パネルを閉じる
    // ----------------------------------------------------------------------
    public void ApplySearchResults()
    {
        if (SearchRouter.Instance != null)
        {
            SearchRouter.Instance.ApplySearchResults(currentResults);
        }
    }
    
    // ----------------------------------------------------------------------
    // 検索パネルを閉じる
    // ----------------------------------------------------------------------
    private void CloseSearchPanel()
    {
        if (SearchRouter.Instance != null)
        {
            SearchRouter.Instance.HideSearchPanel();
        }
    }
    
    // ----------------------------------------------------------------------
    // UI要素のリセット
    // ----------------------------------------------------------------------
    public void ResetUI()
    {
        // カードコンテナの中身をクリア
        ClearCardContainer();
    }
    
    // ----------------------------------------------------------------------
    // カードコンテナをクリア
    // ----------------------------------------------------------------------
    private void ClearCardContainer()
    {
        if (cardContainer == null) return;
        
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }
    }
    
    // ----------------------------------------------------------------------
    // 検索結果の表示
    // @param cards 表示するカードデータのリスト
    // ----------------------------------------------------------------------
    public void DisplaySearchResults(List<CardModel> cards)
    {
        // 現在の結果を保存
        currentResults = cards;
        
        // カードコンテナのクリア
        ClearCardContainer();
        
        // カードプレハブのNullチェック
        if (cardPrefab == null || cardContainer == null)
        {
            Debug.LogWarning("カードプレハブまたはコンテナが設定されていません。");
            return;
        }
        
        // カードの表示
        foreach (var card in cards)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardContainer);
            CardView cardView = cardObj.GetComponent<CardView>();
            
            if (cardView != null)
            {
                cardView.Setup(card);
            }
        }
        
        // スクロール位置のリセット
        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = new Vector2(0, 1);
        }
    }
    
    // ----------------------------------------------------------------------
    // コンポーネント破棄時の処理
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        // イベントリスナーの解除
        if (clearButton != null)
        {
            clearButton.onClick.RemoveAllListeners();
        }
        
        if (applyButton != null)
        {
            applyButton.onClick.RemoveAllListeners();
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
        }
    }
}
