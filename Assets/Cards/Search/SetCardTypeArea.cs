using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Enum;

// ----------------------------------------------------------------------
// カードタイプのフィルタリングを担当するPresenter
// 「非EX」「EX」「サポート」「グッズ」「化石」「ポケモンの道具」のトグルを管理
// ----------------------------------------------------------------------
public class SetCardTypeArea : MonoBehaviour, IFilterArea
{
    [Header("カードタイプトグル")]
    [SerializeField] private Toggle nonEXToggle;
    [SerializeField] private Toggle exToggle;
    [SerializeField] private Toggle supportToggle;
    [SerializeField] private Toggle itemToggle;
    [SerializeField] private Toggle fossilToggle;
    [SerializeField] private Toggle pokemonToolToggle;
    
    // 選択されたカードタイプを保持するHashSet
    private HashSet<CardType> selectedCardTypes = new HashSet<CardType>();
    
    // フィルター変更時のイベント
    public event Action OnFilterChanged;
    
    private void Start()
    {
        InitializeToggles();
    }
    
    // ----------------------------------------------------------------------
    // トグルの初期化とイベントリスナー設定
    // ----------------------------------------------------------------------
    private void InitializeToggles()
    {
        // トグルとカードタイプのマッピング設定
        SetupToggleListener(nonEXToggle, CardType.非EX);
        SetupToggleListener(exToggle, CardType.EX);
        SetupToggleListener(supportToggle, CardType.サポート);
        SetupToggleListener(itemToggle, CardType.グッズ);
        SetupToggleListener(fossilToggle, CardType.化石);
        SetupToggleListener(pokemonToolToggle, CardType.ポケモンのどうぐ);
    }
    
    // ----------------------------------------------------------------------
    // 個別トグルのリスナー設定
    // ----------------------------------------------------------------------
    private void SetupToggleListener(Toggle toggle, CardType cardType)
    {
        if (toggle == null) return;
        
        toggle.onValueChanged.AddListener((isOn) => {
            if (isOn)
            {
                selectedCardTypes.Add(cardType);
            }
            else
            {
                selectedCardTypes.Remove(cardType);
            }
            
            // OKボタンを押すまでフィルタリングを実行しないため、イベント発火を削除
            // OnFilterChanged?.Invoke();
            Debug.Log($"カードタイプフィルター変更: {cardType} → {isOn}, 選択数: {selectedCardTypes.Count}");
        });
    }
    
    // ----------------------------------------------------------------------
    // 現在選択されているカードタイプのリストを取得
    // ----------------------------------------------------------------------
    public HashSet<CardType> GetSelectedCardTypes()
    {
        return new HashSet<CardType>(selectedCardTypes);
    }
    
    // ----------------------------------------------------------------------
    // OKボタンが押されたときに現在のフィルターをモデルに適用する
    // ----------------------------------------------------------------------
    public void ApplyFilterToModel(SearchModel model)
    {
        if (model != null)
        {
            // 現在選択されているカードタイプをモデルに適用
            model.SetCardTypeFilter(GetSelectedCardTypes());
            Debug.Log($"🔍 カードタイプフィルターをモデルに適用: {selectedCardTypes.Count}個のタイプ");
        }
    }
    
    // ----------------------------------------------------------------------
    // 何かしらのカードタイプが選択されているかどうか
    // ----------------------------------------------------------------------
    public bool HasActiveFilters()
    {
        return selectedCardTypes.Count > 0;
    }
    
    // ----------------------------------------------------------------------
    // フィルターのリセット
    // ----------------------------------------------------------------------
    public void ResetFilters()
    {
        Debug.Log("📋 カードタイプフィルターをリセット開始");
        
        // 選択状態をクリア
        selectedCardTypes.Clear();
        
        // トグルのUIをリセット（イベント発火を防ぐためにリスナー一時停止）
        ResetToggle(nonEXToggle);
        ResetToggle(exToggle);
        ResetToggle(supportToggle);
        ResetToggle(itemToggle);
        ResetToggle(fossilToggle);
        ResetToggle(pokemonToolToggle);
        
        Debug.Log("✅ カードタイプフィルターのリセット完了");
        
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
        
        Debug.Log($"🔄 トグル '{toggle.name}' の状態、色、影をリセットしました");
    }
    
    // ----------------------------------------------------------------------
    // イベント発火せずにトグル状態を変更
    // ----------------------------------------------------------------------
    private void SetToggleWithoutNotify(Toggle toggle, bool value)
    {
        if (toggle == null) return;
        
        toggle.SetIsOnWithoutNotify(value);
    }
    
    // ----------------------------------------------------------------------
    // OnDestroy時にイベントをクリア
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        if (nonEXToggle != null) nonEXToggle.onValueChanged.RemoveAllListeners();
        if (exToggle != null) exToggle.onValueChanged.RemoveAllListeners();
        if (supportToggle != null) supportToggle.onValueChanged.RemoveAllListeners();
        if (itemToggle != null) itemToggle.onValueChanged.RemoveAllListeners();
        if (fossilToggle != null) fossilToggle.onValueChanged.RemoveAllListeners();
        if (pokemonToolToggle != null) pokemonToolToggle.onValueChanged.RemoveAllListeners();
    }
}