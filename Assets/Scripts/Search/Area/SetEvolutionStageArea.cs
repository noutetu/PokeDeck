using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Enum;

// ----------------------------------------------------------------------
// 進化段階のフィルタリングを担当するPresenter
// 「たねポケモン」「進化1」「進化2」のトグルを管理
// ----------------------------------------------------------------------
public class SetEvolutionStageArea : MonoBehaviour, IFilterArea
{
    [Header("進化段階トグル")]
    [SerializeField] private Toggle basicToggle;       // たねポケモントグル
    [SerializeField] private Toggle stage1Toggle;      // 進化1トグル
    [SerializeField] private Toggle stage2Toggle;      // 進化2トグル
    
    // ----------------------------------------------------------------------
    // 選択された進化段階を保持するHashSet
    // ----------------------------------------------------------------------
    private HashSet<EvolutionStage> selectedEvolutionStages = new HashSet<EvolutionStage>();
    
    // ----------------------------------------------------------------------
    // フィルター変更時のイベント
    // ----------------------------------------------------------------------
    public event Action OnFilterChanged;
    
    // ----------------------------------------------------------------------
    // UnityのStartメソッド
    // ----------------------------------------------------------------------
    private void Start()
    {
        InitializeToggles();
    }
    
    // ----------------------------------------------------------------------
    // トグルの初期化とイベントリスナー設定
    // ----------------------------------------------------------------------
    private void InitializeToggles()
    {
        // トグルと進化段階のマッピング設定
        SetupToggleListener(basicToggle, EvolutionStage.たね);
        SetupToggleListener(stage1Toggle, EvolutionStage.進化1);
        SetupToggleListener(stage2Toggle, EvolutionStage.進化2);
    }
    
    // ----------------------------------------------------------------------
    // 個別トグルのリスナー設定
    // ----------------------------------------------------------------------
    private void SetupToggleListener(Toggle toggle, EvolutionStage evolutionStage)
    {
        if (toggle == null) return;
        
        toggle.onValueChanged.AddListener((isOn) => {
            if (isOn)
            {
                selectedEvolutionStages.Add(evolutionStage);
            }
            else
            {
                selectedEvolutionStages.Remove(evolutionStage);
            }
        });
    }
    
    // ----------------------------------------------------------------------
    // 現在選択されている進化段階のリストを取得
    // ----------------------------------------------------------------------
    public HashSet<EvolutionStage> GetSelectedEvolutionStages()
    {
        return new HashSet<EvolutionStage>(selectedEvolutionStages);
    }
    
    // ----------------------------------------------------------------------
    // 何かしらの進化段階が選択されているかどうか
    // ----------------------------------------------------------------------
    public bool HasActiveFilters()
    {
        return selectedEvolutionStages.Count > 0;
    }
    
    // ----------------------------------------------------------------------
    // フィルターのリセット
    // ----------------------------------------------------------------------
    public void ResetFilters()
    {
        // 選択状態をクリア
        selectedEvolutionStages.Clear();
        
        // トグルのUIをリセット（イベント発火を防ぐためにリスナー一時停止）
        ResetToggle(basicToggle);
        ResetToggle(stage1Toggle);
        ResetToggle(stage2Toggle);
        
        // リセット後にフィルター変更を通知
        OnFilterChanged?.Invoke();
    }
    
    // ----------------------------------------------------------------------
    // トグルを完全にリセット（状態と色と影の両方）
    // ----------------------------------------------------------------------
    private void ResetToggle(Toggle toggle)
    {
        if (toggle == null) return;
        
        // トグルの状態をリセット（イベント発火なし）
        toggle.SetIsOnWithoutNotify(false);
        
        // SimpleToggleColorコンポーネントを取得して色も更新
        SimpleToggleColor colorComponent = toggle.GetComponent<SimpleToggleColor>();
        if (colorComponent != null)
        {
            colorComponent.UpdateColorState(false);
        }
        
        // TrueShadowToggleInsetコンポーネントを取得して影状態も更新
        TrueShadowToggleInset shadowComponent = toggle.GetComponent<TrueShadowToggleInset>();
        if (shadowComponent != null)
        {
            shadowComponent.UpdateInsetState(false);
        }
    }
    
    // ----------------------------------------------------------------------
    // OKボタンが押されたときに現在のフィルターをモデルに適用する
    // ----------------------------------------------------------------------
    public void ApplyFilterToModel(SearchModel model)
    {
        if (model != null)
        {
            // 現在選択されている進化段階をモデルに適用
            model.SetEvolutionStageFilter(GetSelectedEvolutionStages());
        }
    }
    
    // ----------------------------------------------------------------------
    // OnDestroy時にイベントをクリア
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        if (basicToggle != null) basicToggle.onValueChanged.RemoveAllListeners();
        if (stage1Toggle != null) stage1Toggle.onValueChanged.RemoveAllListeners();
        if (stage2Toggle != null) stage2Toggle.onValueChanged.RemoveAllListeners();
    }
}