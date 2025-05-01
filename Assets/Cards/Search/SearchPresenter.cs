// ----------------------------------------------------------------------
// 検索画面のPresenter
// ViewとModelの橋渡しを行うクラス
// ----------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

public class SearchPresenter
{
    private SearchView view;
    private SearchModel model;
    private SetCardTypeArea cardTypeArea;
    private SetEvolutionStageArea evolutionStageArea;
    private SetTypeArea typeArea;
    private SetCardPackArea cardPackArea;
    private SetHPArea hpArea;
    private SetMaxDamageArea maxDamageArea;
    private SetMaxEnergyArea maxEnergyCostArea;
    
    // ----------------------------------------------------------------------
    // コンストラクタ
    // ViewとModelの参照を受け取り、イベントをバインドする
    // ----------------------------------------------------------------------
    public SearchPresenter(SearchView view, SearchModel model)
    {
        this.view = view;
        this.model = model;
        
        BindViewEvents();
    }
    
    // ----------------------------------------------------------------------
    // カードタイプフィルターエリアを登録
    // ----------------------------------------------------------------------
    public void RegisterCardTypeArea(SetCardTypeArea area)
    {
        this.cardTypeArea = area;
        
        // カードタイプフィルターのイベントを登録
        if (cardTypeArea != null)
        {
            cardTypeArea.OnFilterChanged += () => {
                Debug.Log("🔍 カードタイプフィルター変更を検知");
                model.SetCardTypeFilter(cardTypeArea.GetSelectedCardTypes());
                UpdateView();
            };
        }
    }
    
    // ----------------------------------------------------------------------
    // 進化段階フィルターエリアを登録
    // ----------------------------------------------------------------------
    public void RegisterEvolutionStageArea(SetEvolutionStageArea area)
    {
        this.evolutionStageArea = area;
        
        // 進化段階フィルターのイベントを登録
        if (evolutionStageArea != null)
        {
            evolutionStageArea.OnFilterChanged += () => {
                Debug.Log("🔍 進化段階フィルター変更を検知");
                model.SetEvolutionStageFilter(evolutionStageArea.GetSelectedEvolutionStages());
                UpdateView();
            };
        }
    }
    
    // ----------------------------------------------------------------------
    // ポケモンタイプフィルターエリアを登録
    // ----------------------------------------------------------------------
    public void RegisterTypeArea(SetTypeArea area)
    {
        this.typeArea = area;
        
        // ポケモンタイプフィルターのイベントを登録
        if (typeArea != null)
        {
            typeArea.OnFilterChanged += () => {
                Debug.Log("🔍 ポケモンタイプフィルター変更を検知");
                model.SetPokemonTypeFilter(typeArea.GetSelectedTypes());
                UpdateView();
            };
        }
    }
    
    // ----------------------------------------------------------------------
    // カードパックフィルターエリアを登録
    // ----------------------------------------------------------------------
    public void RegisterCardPackArea(SetCardPackArea area)
    {
        this.cardPackArea = area;
        
        // カードパックフィルターのイベントを登録
        if (cardPackArea != null)
        {
            cardPackArea.OnFilterChanged += () => {
                Debug.Log("🔍 カードパックフィルター変更を検知");
                model.SetCardPackFilter(cardPackArea.GetSelectedCardPacks());
                UpdateView();
            };
        }
    }
    
    // ----------------------------------------------------------------------
    // HPフィルターエリアを登録
    // ----------------------------------------------------------------------
    public void RegisterHPArea(SetHPArea area)
    {
        this.hpArea = area;
        
        // HPフィルターのイベントを登録
        if (hpArea != null)
        {
            hpArea.OnFilterChanged += () => {
                Debug.Log("🔍 HPフィルター変更を検知");
                model.SetHPFilter(hpArea.GetSelectedHP(), hpArea.GetSelectedComparisonType());
                UpdateView();
            };
        }
    }
    
    // ----------------------------------------------------------------------
    // 最大ダメージフィルターエリアを登録
    // ----------------------------------------------------------------------
    public void RegisterMaxDamageArea(SetMaxDamageArea area)
    {
        this.maxDamageArea = area;
        
        // 最大ダメージフィルターのイベントを登録
        if (maxDamageArea != null)
        {
            maxDamageArea.OnFilterChanged += () => {
                Debug.Log("🔍 最大ダメージフィルター変更を検知");
                model.SetMaxDamageFilter(maxDamageArea.GetSelectedDamage(), maxDamageArea.GetSelectedComparisonType());
                UpdateView();
            };
        }
    }

    // ----------------------------------------------------------------------
    // 最大エネルギーコストフィルターエリアを登録
    // ----------------------------------------------------------------------
    public void RegisterMaxEnergyCostArea(SetMaxEnergyArea area)
    {
        this.maxEnergyCostArea = area;
        if (maxEnergyCostArea != null)
        {
            maxEnergyCostArea.OnFilterChanged += () => {
                Debug.Log("🔍 最大エネルギーコストフィルター変更を検知");
                // model.SetMaxEnergyCostFilter(maxEnergyCostArea.GetSelectedEnergyCost(), maxEnergyCostArea.GetSelectedComparisonType());
                UpdateView();
            };
        }
    }
    
    // ----------------------------------------------------------------------
    // Viewのイベントをバインド
    // Viewからの入力イベントを受け取り、Modelの処理につなげる
    // ----------------------------------------------------------------------
    private void BindViewEvents()
    {
        view.OnSearchButtonClicked += PerformSearch;
        view.OnClearButtonClicked += ClearFilters;
    }
    
    // ----------------------------------------------------------------------
    // 検索を実行
    // Modelに検索を依頼し、結果をViewに表示する
    // ----------------------------------------------------------------------
    private void PerformSearch()
    {
        Debug.Log("🔍 検索実行");
        
        // 各フィルターエリアの現在の状態をモデルに適用
        // カードタイプフィルターの適用
        if (cardTypeArea != null)
        {
            cardTypeArea.ApplyFilterToModel(model);
        }
        
        // 進化段階フィルターの適用
        if (evolutionStageArea != null)
        {
            evolutionStageArea.ApplyFilterToModel(model);
        }
        
        // ポケモンタイプフィルターの適用
        if (typeArea != null)
        {
            typeArea.ApplyFilterToModel(model);
        }
        
        // カードパックフィルターの適用
        if (cardPackArea != null)
        {
            cardPackArea.ApplyFilterToModel(model);
        }
        
        // HPフィルターの適用
        if (hpArea != null)
        {
            hpArea.ApplyFilterToModel(model);
        }
        
        // 最大ダメージフィルターの適用
        if (maxDamageArea != null)
        {
            maxDamageArea.ApplyFilterToModel(model);
        }

        // 最大エネルギーコストフィルターの適用
        if (maxEnergyCostArea != null)
        {
            // maxEnergyCostArea.ApplyFilterToModel(model);
        }
        
        // すべてのフィルター条件を適用した後、一度だけフィルタリングを実行
        model.ApplyFilters();
        
        // 検索結果をViewに表示
        UpdateView();
    }
    
    // ----------------------------------------------------------------------
    // フィルターをクリア
    // Modelのフィルターをリセットし、結果を更新する
    // ----------------------------------------------------------------------
    private void ClearFilters()
    {
        Debug.Log("🔍 検索条件クリア");
        model.ClearAllFilters();
        
        // 各フィルターエリアのUIもリセット
        if (cardTypeArea != null)
        {
            Debug.Log("🔄 カードタイプフィルターUIをリセット");
            cardTypeArea.ResetFilters();
        }
        else
        {
            Debug.LogWarning("⚠️ cardTypeArea参照が null のためUIリセットできません");
        }
        
        // 進化段階フィルターUIもリセット
        if (evolutionStageArea != null)
        {
            Debug.Log("🔄 進化段階フィルターUIをリセット");
            evolutionStageArea.ResetFilters();
        }
        else
        {
            Debug.LogWarning("⚠️ evolutionStageArea参照が null のためUIリセットできません");
        }
        
        // ポケモンタイプフィルターUIもリセット
        if (typeArea != null)
        {
            Debug.Log("🔄 ポケモンタイプフィルターUIをリセット");
            typeArea.ResetFilters();
        }
        else
        {
            Debug.LogWarning("⚠️ typeArea参照が null のためUIリセットできません");
        }
        
        // カードパックフィルターUIもリセット
        if (cardPackArea != null)
        {
            Debug.Log("🔄 カードパックフィルターUIをリセット");
            cardPackArea.ResetFilters();
        }
        else
        {
            Debug.LogWarning("⚠️ cardPackArea参照が null のためUIリセットできません");
        }
        
        // HPフィルターUIもリセット
        if (hpArea != null)
        {
            Debug.Log("🔄 HPフィルターUIをリセット");
            hpArea.ResetFilters();
        }
        else
        {
            Debug.LogWarning("⚠️ hpArea参照が null のためUIリセットできません");
        }
        
        // 最大ダメージフィルターUIもリセット
        if (maxDamageArea != null)
        {
            Debug.Log("🔄 最大ダメージフィルターUIをリセット");
            maxDamageArea.ResetFilters();
        }
        else
        {
            Debug.LogWarning("⚠️ maxDamageArea参照が null のためUIリセットできません");
        }

        // 最大エネルギーコストフィルターUIもリセット
        if (maxEnergyCostArea != null)
        {
            Debug.Log("🔄 最大エネルギーコストフィルターUIをリセット");
            maxEnergyCostArea.ResetFilters();
        }
        else
        {
            Debug.LogWarning("⚠️ maxEnergyCostArea参照が null のためUIリセットできません");
        }
        
        // 将来的に他のフィルターエリアが追加された場合、ここに追加する
        
        UpdateView();
    }
    
    // ----------------------------------------------------------------------
    // Viewを更新
    // Modelからデータを取得し、Viewに反映する
    // ----------------------------------------------------------------------
    private void UpdateView()
    {
        List<CardModel> filteredCards = model.GetFilteredCards();
        Debug.Log($"🔍 検索結果: {filteredCards.Count}件");
        view.DisplaySearchResults(filteredCards);
    }
}
