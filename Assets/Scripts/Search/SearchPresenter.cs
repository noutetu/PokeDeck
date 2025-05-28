using System.Collections.Generic;
using UnityEngine;

// ----------------------------------------------------------------------
// 検索画面のPresenter
// ViewとModelの橋渡しを行うクラス
// ----------------------------------------------------------------------
public class SearchPresenter
{
    // ----------------------------------------------------------------------
    // フィールド
    // ----------------------------------------------------------------------
    // 検索画面のViewとModel
    private SearchView view;
    private SearchModel model;
    // -------------------------------------------------
    // 各フィルターエリアの参照
    // -------------------------------------------------
    private SetCardTypeArea cardTypeArea;                       // カードタイプフィルターエリア
    private SetEvolutionStageArea evolutionStageArea;           // 進化段階フィルターエリア
    private SetTypeArea typeArea;                               // ポケモンタイプフィルターエリア
    private SetCardPackArea cardPackArea;                       // カードパックフィルターエリア   
    private SetHPArea hpArea;                                   // HPフィルターエリア
    private SetMaxDamageArea maxDamageArea;                     // 最大ダメージフィルターエリア
    private SetMaxEnergyArea maxEnergyCostArea;                 // 最大エネルギーコストフィルターエリア
    private SetRetreatCostArea retreatCostArea;                 // 逃げるコストフィルターエリア
    
    // ----------------------------------------------------------------------
    // コンストラクタ
    // ViewとModelの参照を受け取り、イベントをバインドする
    // ----------------------------------------------------------------------
    public SearchPresenter(SearchView view, SearchModel model)
    {
        this.view = view;
        this.model = model;
        
        // 検索ボタンクリック時のイベントをバインド
        if (view != null)
        {
            view.OnSearchButtonClicked += ApplyAllFiltersToModel;
            view.OnClearButtonClicked += ClearAllFilters;
        }
    }
    
    // ----------------------------------------------------------------------
    // カードタイプエリアの登録
    // ----------------------------------------------------------------------
    public void RegisterCardTypeArea(SetCardTypeArea area)
    {
        cardTypeArea = area;
    }
    
    // ----------------------------------------------------------------------
    // 進化段階エリアの登録
    // ----------------------------------------------------------------------
    public void RegisterEvolutionStageArea(SetEvolutionStageArea area)
    {
        evolutionStageArea = area;
    }
    
    // ----------------------------------------------------------------------
    // ポケモンタイプエリアの登録
    // ----------------------------------------------------------------------
    public void RegisterTypeArea(SetTypeArea area)
    {
        typeArea = area;
    }
    
    // ----------------------------------------------------------------------
    // カードパックエリアの登録 
    // ----------------------------------------------------------------------
    public void RegisterCardPackArea(SetCardPackArea area)
    {
        cardPackArea = area;
    }
    
    // ----------------------------------------------------------------------
    // HPエリアの登録 
    // ----------------------------------------------------------------------
    public void RegisterHPArea(SetHPArea area)
    {
        hpArea = area;
    }
    
    // ----------------------------------------------------------------------
    // 最大ダメージエリアの登録
    // ----------------------------------------------------------------------
    public void RegisterMaxDamageArea(SetMaxDamageArea area)
    {
        maxDamageArea = area;
    }
    
    // ----------------------------------------------------------------------
    // 最大エネルギーコストエリアの登録
    // ----------------------------------------------------------------------
    public void RegisterMaxEnergyCostArea(SetMaxEnergyArea area)
    {
        maxEnergyCostArea = area;
    }
    
    // ----------------------------------------------------------------------
    // 逃げるコストエリアの登録
    // ----------------------------------------------------------------------
    public void RegisterRetreatCostArea(SetRetreatCostArea area)
    {
        retreatCostArea = area;
    }
    
    // ----------------------------------------------------------------------
    // すべてのフィルターをモデルに適用
    // ----------------------------------------------------------------------
    private void ApplyAllFiltersToModel()
    {
        if (model != null)
        {
            // バッチフィルタリングを開始して、個別のフィルター適用時のログ出力や重複処理を防ぐ
            model.BeginBatchFiltering();

            // 各フィルターエリアの設定をモデルに適用
            // カードタイプフィルターを適用
            if (cardTypeArea != null)
                cardTypeArea.ApplyFilterToModel(model);

            // 進化段階フィルターを適用
            if (evolutionStageArea != null)
                evolutionStageArea.ApplyFilterToModel(model);

            // ポケモンタイプフィルターを適用
            if (typeArea != null)
                typeArea.ApplyFilterToModel(model);

            // カードパックフィルターを適用
            if (cardPackArea != null)
                cardPackArea.ApplyFilterToModel(model);

            // HPフィルターを適用
            if (hpArea != null)
                hpArea.ApplyFilterToModel(model);

            // 最大ダメージフィルターを適用
            if (maxDamageArea != null)
                maxDamageArea.ApplyFilterToModel(model);

            // 最大エネルギーコストフィルターを適用
            if (maxEnergyCostArea != null)
                maxEnergyCostArea.ApplyFilterToModel(model);
            
            // 逃げるコストフィルターを適用
            if (retreatCostArea != null)
                retreatCostArea.ApplyFilterToModel(model);
            
            // バッチフィルタリングを終了してフィルター処理を実行（ログは1回だけ出力される）
            model.EndBatchFiltering();
        }
    }
    
    // ----------------------------------------------------------------------
    // すべてのフィルターをクリア
    // ----------------------------------------------------------------------
    private void ClearAllFilters()
    {   
        // カードタイプフィルターをリセット
        if (cardTypeArea != null)
        {
            cardTypeArea.ResetFilters();
        }
        
        // 進化段階フィルターをリセット
        if (evolutionStageArea != null)
        {
            evolutionStageArea.ResetFilters();
        }
        
        // ポケモンタイプフィルターをリセット
        if (typeArea != null)
        {
            typeArea.ResetFilters();
        }
        
        // カードパックフィルターをリセット
        if (cardPackArea != null)
        {
            cardPackArea.ResetFilters();
        }
        
        // HPフィルターをリセット
        if (hpArea != null)
        {
            hpArea.ResetFilters();
        }
        
        // 最大ダメージフィルターをリセット
        if (maxDamageArea != null)
        {
            maxDamageArea.ResetFilters();
        }
        
        // 最大エネルギーコストフィルターをリセット
        if (maxEnergyCostArea != null)
        {
            maxEnergyCostArea.ResetFilters();
        }
        
        // 逃げるコストフィルターをリセット
        if (retreatCostArea != null)
        {
            retreatCostArea.ResetFilters();
        }
        
        // モデル側のリセット
        model?.ClearAllFilters();
        
        // 検索結果をクリア（空の結果リストを表示）
        if (view != null)
        {
            view.DisplaySearchResults(new List<CardModel>());
        }
    }
}