using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Enum;

// ----------------------------------------------------------------------
// ポケモンタイプのフィルタリングを担当するPresenter
// 「草」「炎」「水」「雷」「闘」「超」「悪」「鋼」「ドラゴン」「無色」のトグルを管理
// ----------------------------------------------------------------------
public class SetTypeArea : MonoBehaviour, IFilterArea
{
    [Header("ポケモンタイプトグル")]
    [SerializeField] private Toggle grassToggle;       // 草タイプトグル
    [SerializeField] private Toggle fireToggle;        // 炎タイプトグル
    [SerializeField] private Toggle waterToggle;       // 水タイプトグル
    [SerializeField] private Toggle lightningToggle;   // 雷タイプトグル
    [SerializeField] private Toggle fightingToggle;    // 闘タイプトグル
    [SerializeField] private Toggle psychicToggle;     // 超タイプトグル
    [SerializeField] private Toggle darknessToggle;    // 悪タイプトグル
    [SerializeField] private Toggle steelToggle;       // 鋼タイプトグル
    [SerializeField] private Toggle dragonToggle;      // ドラゴンタイプトグル
    [SerializeField] private Toggle colorlessToggle;   // 無色タイプトグル
    
    // ----------------------------------------------------------------------
    // 選択されたポケモンタイプを保持するHashSet
    // ----------------------------------------------------------------------
    private HashSet<PokemonType> selectedTypes = new HashSet<PokemonType>();
    
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
        // トグルとポケモンタイプのマッピング設定
        SetupToggleListener(grassToggle, PokemonType.草);
        SetupToggleListener(fireToggle, PokemonType.炎);
        SetupToggleListener(waterToggle, PokemonType.水);
        SetupToggleListener(lightningToggle, PokemonType.雷);
        SetupToggleListener(fightingToggle, PokemonType.闘);
        SetupToggleListener(psychicToggle, PokemonType.超);
        SetupToggleListener(darknessToggle, PokemonType.悪);
        SetupToggleListener(steelToggle, PokemonType.鋼);
        SetupToggleListener(dragonToggle, PokemonType.ドラゴン);
        SetupToggleListener(colorlessToggle, PokemonType.無色);
    }
    
    // ----------------------------------------------------------------------
    // 個別トグルのリスナー設定
    // ----------------------------------------------------------------------
    private void SetupToggleListener(Toggle toggle, PokemonType pokemonType)
    {
        if (toggle == null) return;
        
        toggle.onValueChanged.AddListener((isOn) => {
            if (isOn)
            {
                selectedTypes.Add(pokemonType);
            }
            else
            {
                selectedTypes.Remove(pokemonType);
            }
        });
    }
    
    // ----------------------------------------------------------------------
    // OKボタンが押されたときに現在のフィルターをモデルに適用する
    // ----------------------------------------------------------------------
    public void ApplyFilterToModel(SearchModel model)
    {
        if (model != null)
        {
            // 現在選択されているポケモンタイプをモデルに適用
            HashSet<PokemonType> types = GetSelectedTypes();
            model.SetPokemonTypeFilter(types);
        }
    }
    
    // ----------------------------------------------------------------------
    // 現在選択されているポケモンタイプのリストを取得
    // ----------------------------------------------------------------------
    public HashSet<PokemonType> GetSelectedTypes()
    {
        // トグルの現在の状態を直接チェックして、選択状態を確実に取得
        HashSet<PokemonType> result = new HashSet<PokemonType>();
        
        // 各トグルの状態を確認
        CheckAndAddType(grassToggle, PokemonType.草, result);
        CheckAndAddType(fireToggle, PokemonType.炎, result);
        CheckAndAddType(waterToggle, PokemonType.水, result);
        CheckAndAddType(lightningToggle, PokemonType.雷, result);
        CheckAndAddType(fightingToggle, PokemonType.闘, result);
        CheckAndAddType(psychicToggle, PokemonType.超, result);
        CheckAndAddType(darknessToggle, PokemonType.悪, result);
        CheckAndAddType(steelToggle, PokemonType.鋼, result);
        CheckAndAddType(dragonToggle, PokemonType.ドラゴン, result);
        CheckAndAddType(colorlessToggle, PokemonType.無色, result);
        
        // 内部状態ではなくUI状態に基づいた結果を返す
        return result;
    }
    
    // ----------------------------------------------------------------------
    // トグル状態をチェックしてHashSetに追加するヘルパーメソッド
    // ----------------------------------------------------------------------
    private void CheckAndAddType(Toggle toggle, PokemonType type, HashSet<PokemonType> set)
    {
        if (toggle != null && toggle.isOn)
        {
            set.Add(type);
        }
    }
    
    // ----------------------------------------------------------------------
    // 何かしらのポケモンタイプが選択されているかどうか
    // ----------------------------------------------------------------------
    public bool HasActiveFilters()
    {
        return selectedTypes.Count > 0;
    }
    
    // ----------------------------------------------------------------------
    // フィルターのリセット
    // ----------------------------------------------------------------------
    public void ResetFilters()
    {
        // 選択状態をクリア
        selectedTypes.Clear();
        
        // トグルのUIをリセット（イベント発火を防ぐためにリスナー一時停止）
        ResetToggle(grassToggle);
        ResetToggle(fireToggle);
        ResetToggle(waterToggle);
        ResetToggle(lightningToggle);
        ResetToggle(fightingToggle);
        ResetToggle(psychicToggle);
        ResetToggle(darknessToggle);
        ResetToggle(steelToggle);
        ResetToggle(dragonToggle);
        ResetToggle(colorlessToggle);
        
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
    // OnDestroy時にイベントをクリア
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        if (grassToggle != null) grassToggle.onValueChanged.RemoveAllListeners();
        if (fireToggle != null) fireToggle.onValueChanged.RemoveAllListeners();
        if (waterToggle != null) waterToggle.onValueChanged.RemoveAllListeners();
        if (lightningToggle != null) lightningToggle.onValueChanged.RemoveAllListeners();
        if (fightingToggle != null) fightingToggle.onValueChanged.RemoveAllListeners();
        if (psychicToggle != null) psychicToggle.onValueChanged.RemoveAllListeners();
        if (darknessToggle != null) darknessToggle.onValueChanged.RemoveAllListeners();
        if (steelToggle != null) steelToggle.onValueChanged.RemoveAllListeners();
        if (dragonToggle != null) dragonToggle.onValueChanged.RemoveAllListeners();
        if (colorlessToggle != null) colorlessToggle.onValueChanged.RemoveAllListeners();
    }
}