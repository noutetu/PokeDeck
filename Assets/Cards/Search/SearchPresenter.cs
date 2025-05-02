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
    // 進化段階エリアの登録
    // ----------------------------------------------------------------------
    public void RegisterEvolutionStageArea(SetEvolutionStageArea area)
    {
        evolutionStageArea = area;
        
        // フィルター変更イベントの購読
        if (area != null)
        {
            area.OnFilterChanged += OnFilterChanged;
            Debug.Log("✅ SetEvolutionStageAreaを登録しました");
        }
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
        
        // カードタイプフィルターを適用
        if (cardTypeArea != null)
        {
            cardTypeArea.ApplyFilterToModel(model);
        }
        // 進化段階フィルターを適用
        if (evolutionStageArea != null)
        {
            evolutionStageArea.ApplyFilterToModel(model);
        }
        
        // 他のフィルター適用はコメントアウト
        /*
        // 進化段階フィルターを適用
        if (evolutionStageArea != null)
        {
            evolutionStageArea.ApplyFilterToModel(model);
        }
        
        // ポケモンタイプフィルターを適用
        if (typeArea != null)
        {
            typeArea.ApplyFilterToModel(model);
        }
        
        // カードパックフィルターを適用
        if (cardPackArea != null)
        {
            cardPackArea.ApplyFilterToModel(model);
        }
        
        // HPフィルターを適用
        if (hpArea != null)
        {
            hpArea.ApplyFilterToModel(model);
        }
        
        // 最大ダメージフィルターを適用
        if (maxDamageArea != null)
        {
            maxDamageArea.ApplyFilterToModel(model);
        }
        
            maxEnergyCostArea.ApplyFilterToModel(model);
        }
        */
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
        
        // 他のフィルターリセットはコメントアウト
        /*
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
        */
        
        // モデル側のリセット
        model?.ClearAllFilters();
        
        // 検索結果をクリア（空の結果リストを表示）
        if (view != null)
        {
            view.DisplaySearchResults(new List<CardModel>());
        }
    }
}
