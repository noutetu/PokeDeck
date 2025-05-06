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
    [SerializeField] private SetRetreatCostArea retreatCostArea;  // 逃げるコストフィルターエリア
    
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
        
        // フィルター変更イベントの購読（自動プレビュー用）
        if (area != null)
        {
            // プレビュー検索を無効化するため購読を行わない
            // area.OnFilterChanged += OnFilterChanged;
            Debug.Log("✅ SetCardTypeAreaを登録しました");
        }
    }
    
    // ----------------------------------------------------------------------
    // 進化段階エリアの登録 - 現在はコメントアウトして無効化
    // ----------------------------------------------------------------------
    public void RegisterEvolutionStageArea(SetEvolutionStageArea area)
    {
        evolutionStageArea = area;
        
        // フィルター変更イベントの購読（自動プレビュー用）
        /*
        if (area != null)
        {
            area.OnFilterChanged += OnFilterChanged;
            Debug.Log("✅ SetEvolutionStageAreaを登録しました");
        }
        */
    }
    
    // ----------------------------------------------------------------------
    // ポケモンタイプエリアの登録 - 現在はコメントアウトして無効化
    // ----------------------------------------------------------------------
    public void RegisterTypeArea(SetTypeArea area)
    {
        typeArea = area;
        
        // フィルター変更イベントの購読（自動プレビュー用）
        /*
        if (area != null)
        {
            area.OnFilterChanged += OnFilterChanged;
            Debug.Log("✅ SetTypeAreaを登録しました");
        }
        */
    }
    
    // ----------------------------------------------------------------------
    // カードパックエリアの登録 - 現在はコメントアウトして無効化
    // ----------------------------------------------------------------------
    public void RegisterCardPackArea(SetCardPackArea area)
    {
        cardPackArea = area;
        
        // フィルター変更イベントの購読（自動プレビュー用）
        /*
        if (area != null)
        {
            area.OnFilterChanged += OnFilterChanged;
            Debug.Log("✅ SetCardPackAreaを登録しました");
        }
        */
    }
    
    // ----------------------------------------------------------------------
    // HPエリアの登録 - 現在はコメントアウトして無効化
    // ----------------------------------------------------------------------
    public void RegisterHPArea(SetHPArea area)
    {
        hpArea = area;
        
        // フィルター変更イベントの購読（自動プレビュー用）
        /*
        if (area != null)
        {
            area.OnFilterChanged += OnFilterChanged;
            Debug.Log("✅ SetHPAreaを登録しました");
        }
        */
    }
    
    // ----------------------------------------------------------------------
    // 最大ダメージエリアの登録 - 現在はコメントアウトして無効化
    // ----------------------------------------------------------------------
    public void RegisterMaxDamageArea(SetMaxDamageArea area)
    {
        maxDamageArea = area;
        
        // フィルター変更イベントの購読（自動プレビュー用）
        /*
        if (area != null)
        {
            area.OnFilterChanged += OnFilterChanged;
            Debug.Log("✅ SetMaxDamageAreaを登録しました");
        }
        */
    }
    
    // ----------------------------------------------------------------------
    // 最大エネルギーコストエリアの登録 - 現在はコメントアウトして無効化
    // ----------------------------------------------------------------------
    public void RegisterMaxEnergyCostArea(SetMaxEnergyArea area)
    {
        maxEnergyCostArea = area;
        
        // フィルター変更イベントの購読（自動プレビュー用）
        /*
        if (area != null)
        {
            area.OnFilterChanged += OnFilterChanged;
            Debug.Log("✅ SetMaxEnergyCostAreaを登録しました");
        }
        */
    }
    
    // ----------------------------------------------------------------------
    // 逃げるコストエリアの登録
    // ----------------------------------------------------------------------
    public void RegisterRetreatCostArea(SetRetreatCostArea area)
    {
        retreatCostArea = area;
        
        // フィルター変更イベントの購読（自動プレビュー用）
        /*
        if (area != null)
        {
            area.OnFilterChanged += OnFilterChanged;
            Debug.Log("✅ SetRetreatCostAreaを登録しました");
        }
        */
    }
    
    // ----------------------------------------------------------------------
    // フィルター変更時の処理
    // ----------------------------------------------------------------------
    private void OnFilterChanged()
    {
        // 変更があった場合はすべてのフィルターをモデルに適用
        ApplyAllFiltersToModel();
        
        // プレビュー検索は無効化
        // PerformPreviewSearch();
    }
    
    // ----------------------------------------------------------------------
    // すべてのフィルターをモデルに適用
    // ----------------------------------------------------------------------
    private void ApplyAllFiltersToModel()
    {
        Debug.Log("🔍 検索フィルターをモデルに適用");
        
        if (model != null)
        {
            // バッチフィルタリングを開始して、個別のフィルター適用時のログ出力や重複処理を防ぐ
            model.BeginBatchFiltering();
            
            // 各フィルターエリアの設定をモデルに適用
            if (cardTypeArea != null)
                cardTypeArea.ApplyFilterToModel(model);
    
            if (evolutionStageArea != null)
                evolutionStageArea.ApplyFilterToModel(model);
    
            if (typeArea != null)
                typeArea.ApplyFilterToModel(model);
    
            if (cardPackArea != null)
                cardPackArea.ApplyFilterToModel(model);
    
            if (hpArea != null)
                hpArea.ApplyFilterToModel(model);
    
            if (maxDamageArea != null)
                maxDamageArea.ApplyFilterToModel(model);
    
            if (maxEnergyCostArea != null)
                maxEnergyCostArea.ApplyFilterToModel(model);
                
            // 逃げるコストフィルターを適用
            if (retreatCostArea != null)
                retreatCostArea.ApplyFilterToModel(model);
            
            // バッチフィルタリングを終了してフィルター処理を実行（ログは1回だけ出力される）
            model.EndBatchFiltering();
        }
        else
        {
            Debug.LogError("❌ SearchModelがnullです");
        }
    }
    
    // ----------------------------------------------------------------------
    // プレビュー検索の実行 - 無効化
    // ----------------------------------------------------------------------
    // private void PerformPreviewSearch()
    // {
    //     // プレビュー検索は無効化されています
    // }
    
    // ----------------------------------------------------------------------
    // すべてのフィルターをクリア
    // ----------------------------------------------------------------------
    private void ClearAllFilters()
    {
        Debug.Log("🧹 すべてのフィルターをリセットします");
        
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