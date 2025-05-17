using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq; 

// ----------------------------------------------------------------------
// カード検索画面のView
// ユーザーからの入力を受け取り、検索条件の設定と結果表示を行う
// ----------------------------------------------------------------------
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
    [SerializeField] private SetRetreatCostArea retreatCostArea; // 逃げるコストフィルターエリア

    [Header("結果プレビュー表示UI")]
    [SerializeField] private Transform cardContainer;          // 検索結果表示用コンテナ
    [SerializeField] private GameObject cardPrefab;            // カードプレハブ
    [SerializeField] private ScrollRect scrollRect;            // スクロール領域（オプション）

    // ----------------------------------------------------------------------
    // MVP管理用
    // ----------------------------------------------------------------------
    private SearchPresenter presenter;
    private SearchModel model;

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
        // UIの初期化
        InitializeUI();
        // リスナーの設定
        SetupListeners();
        // MVPのセットアップ
        SetupMVP();

        // 起動時にすべてのフィルターをリセット
        ResetUI();

        // モデルの状態も初期化（クリアボタンはUIのみリセットするため）
        if (model != null)
        {
            model.ClearAllFilters();
        }
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

        // カードタイプフィルターエリアの登録
        if (cardTypeArea != null)
        {
            presenter.RegisterCardTypeArea(cardTypeArea);
        }

        // 進化段階フィルターエリアの登録
        if (evolutionStageArea != null)
        {
            presenter.RegisterEvolutionStageArea(evolutionStageArea);
        }

        // 以下のフィルターエリア登録
        if (typeArea != null)
        {
            presenter.RegisterTypeArea(typeArea);
        }

        // カードパックフィルターエリアの登録
        if (cardPackArea != null)
        {
            presenter.RegisterCardPackArea(cardPackArea);
        }

        // HPフィルターエリアの登録
        if (hpArea != null)
        {
            presenter.RegisterHPArea(hpArea);
        }
        // 最大ダメージフィルターエリアの登録
        if (maxDamageArea != null)
        {
            presenter.RegisterMaxDamageArea(maxDamageArea);
        }
        // 最大エネルギーコストフィルターエリアの登録
        if (maxEnergyCostArea != null)
        {
            presenter.RegisterMaxEnergyCostArea(maxEnergyCostArea);
        }
        // 逃げるコストフィルターエリアの登録
        if (retreatCostArea != null)
        {
            presenter.RegisterRetreatCostArea(retreatCostArea);
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
            // モデルにカードデータを設定
            model.SetCards(cards);
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
            applyButton.onClick.AddListener(() =>
            {
                // 検索を実行
                OnSearchButtonClicked?.Invoke();
                // 検索結果をメインのカードリストに適用して検索パネルを閉じる
                ApplySearchResults();
            });
        }

        // クリアボタンクリック（フィルタリングリセット）
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(() =>
            {
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
        if (SearchNavigator.Instance != null)
        {
            // カードタイプフィルターを取得
            var selectedCardTypes = cardTypeArea != null ? cardTypeArea.GetSelectedCardTypes().ToList() : new List<Enum.CardType>();
            // 進化段階フィルターを取得
            var selectedEvolutionStages = evolutionStageArea != null ? evolutionStageArea.GetSelectedEvolutionStages().ToList() : new List<Enum.EvolutionStage>();
            // ポケモンタイプフィルターを取得
            var selectedTypes = typeArea != null ? typeArea.GetSelectedTypes().ToList() : new List<Enum.PokemonType>();
            // カードパックフィルターを取得
            var selectedCardPacks = cardPackArea != null ? cardPackArea.GetSelectedCardPacks().ToList() : new List<Enum.CardPack>();

            // HPフィルター用のmin/maxを計算
            int minHP, maxHP;
            if (hpArea != null)
            {
                var hpVal = hpArea.GetSelectedHP();
                var cmp = hpArea.GetSelectedComparisonType();
                switch (cmp)
                {
                    case SetHPArea.HPComparisonType.LessOrEqual: minHP = 0; maxHP = hpVal; break;
                    case SetHPArea.HPComparisonType.Equal: minHP = hpVal; maxHP = hpVal; break;
                    case SetHPArea.HPComparisonType.GreaterOrEqual: minHP = hpVal; maxHP = int.MaxValue; break;
                    default: minHP = 0; maxHP = int.MaxValue; break;
                }
            }
            else { minHP = 0; maxHP = int.MaxValue; }

            // ダメージフィルター用のmin/maxを計算
            int minMaxDamage, maxMaxDamage;
            if (maxDamageArea != null)
            {
                var dmg = maxDamageArea.GetSelectedDamage();
                var cmpD = maxDamageArea.GetSelectedComparisonType();
                switch (cmpD)
                {
                    case SetMaxDamageArea.DamageComparisonType.LessOrEqual: minMaxDamage = 0; maxMaxDamage = dmg; break;
                    case SetMaxDamageArea.DamageComparisonType.Equal: minMaxDamage = dmg; maxMaxDamage = dmg; break;
                    case SetMaxDamageArea.DamageComparisonType.GreaterOrEqual: minMaxDamage = dmg; maxMaxDamage = int.MaxValue; break;
                    default: minMaxDamage = 0; maxMaxDamage = int.MaxValue; break;
                }
            }
            else { minMaxDamage = 0; maxMaxDamage = int.MaxValue; }

            // エネルギーフィルターの範囲を計算
            int minEnergyCost, maxEnergyCost;
            if (maxEnergyCostArea != null)
            {
                var cost = maxEnergyCostArea.GetSelectedEnergyCost();
                var cmp = maxEnergyCostArea.GetSelectedComparisonType();
                switch (cmp)
                {
                    case SetMaxEnergyArea.EnergyComparisonType.LessOrEqual: minEnergyCost = 0; maxEnergyCost = cost; break;
                    case SetMaxEnergyArea.EnergyComparisonType.Equal: minEnergyCost = cost; maxEnergyCost = cost; break;
                    case SetMaxEnergyArea.EnergyComparisonType.GreaterOrEqual: minEnergyCost = cost; maxEnergyCost = int.MaxValue; break;
                    default: minEnergyCost = 0; maxEnergyCost = int.MaxValue; break;
                }
            }
            else { minEnergyCost = 0; maxEnergyCost = int.MaxValue; }

            // 逃げるコストフィルターの範囲を計算
            int minRetreatCost, maxRetreatCost;
            if (retreatCostArea != null)
            {
                var cost = retreatCostArea.GetSelectedRetreatCost();
                var cmp = retreatCostArea.GetSelectedComparisonType();
                switch (cmp)
                {
                    case SetRetreatCostArea.RetreatComparisonType.LessOrEqual: minRetreatCost = 0; maxRetreatCost = cost; break;
                    case SetRetreatCostArea.RetreatComparisonType.Equal: minRetreatCost = cost; maxRetreatCost = cost; break;
                    case SetRetreatCostArea.RetreatComparisonType.GreaterOrEqual: minRetreatCost = cost; maxRetreatCost = int.MaxValue; break;
                    default: minRetreatCost = 0; maxRetreatCost = int.MaxValue; break;
                }
            }
            else { minRetreatCost = 0; maxRetreatCost = int.MaxValue; }

            // 検索実行
            if (model != null)
            {
                var results = model.Search(
                    selectedCardTypes,
                    selectedEvolutionStages,
                    selectedTypes,
                    selectedCardPacks,
                    minHP, maxHP,
                    minMaxDamage, maxMaxDamage,
                    minEnergyCost, maxEnergyCost,
                    minRetreatCost, maxRetreatCost
                );
                SearchNavigator.Instance.ApplySearchResults(results);
                CloseSearchPanel();
            }
        }
    }

    // ----------------------------------------------------------------------
    // 検索パネルを閉じる
    // ----------------------------------------------------------------------
    private void CloseSearchPanel()
    {
        if (SearchNavigator.Instance != null)
        {
            SearchNavigator.Instance.HideSearchPanel();
        }
    }

    // ----------------------------------------------------------------------
    // UI要素のリセット
    // ----------------------------------------------------------------------
    public void ResetUI()
    {
        // カードコンテナの中身をクリア
        ClearCardContainer();
        // フィルターUIをリセット（カードタイプと進化段階）
        if (cardTypeArea != null) cardTypeArea.ResetFilters();
        if (evolutionStageArea != null) evolutionStageArea.ResetFilters();
        if (typeArea != null) typeArea.ResetFilters();
        if (cardPackArea != null) cardPackArea.ResetFilters();
        if (hpArea != null) hpArea.ResetFilters();
        if (maxDamageArea != null) maxDamageArea.ResetFilters();
        if (maxEnergyCostArea != null) maxEnergyCostArea.ResetFilters();
        if (retreatCostArea != null) retreatCostArea.ResetFilters();
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
        // カードコンテナのクリア
        ClearCardContainer();

        // カードプレハブのNullチェック
        if (cardPrefab == null || cardContainer == null)
        {
            return;
        }

        // カードの表示
        foreach (var card in cards)
        {
            // カードプレハブのインスタンス化
            GameObject cardObj = Instantiate(cardPrefab, cardContainer);
            // カードの画像を設定
            CardView cardView = cardObj.GetComponent<CardView>();

            if (cardView != null)
            {
                cardView.SetImage(card);
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