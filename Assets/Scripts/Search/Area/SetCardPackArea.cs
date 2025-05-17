using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Enum;

// ----------------------------------------------------------------------
// カードパックのフィルタリングを担当するPresenter
// ----------------------------------------------------------------------
public class SetCardPackArea : MonoBehaviour, IFilterArea
{
    [Header("カードパックトグル")]
    [SerializeField] private Toggle saikyo_Toggle;          // 最強の遺伝子トグル
    [SerializeField] private Toggle maboroshi_Toggle;       // 幻のいる島トグル
    [SerializeField] private Toggle jikuu_Toggle;           // 時空の激闘トグル
    [SerializeField] private Toggle choukoku_Toggle;        // 超克の光トグル
    [SerializeField] private Toggle shiningHigh_Toggle;     // シャイニングハイトグル
    [SerializeField] private Toggle souten_Toggle;          // 双天の守護者トグル
    [SerializeField] private Toggle promo_Toggle;           // PROMOトグル
    
    // ----------------------------------------------------------------------
    // 選択されたカードパックを保持するHashSet
    // ----------------------------------------------------------------------
    private HashSet<CardPack> selectedCardPacks = new HashSet<CardPack>();
    
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
        // トグルとカードパックのマッピング設定
        SetupToggleListener(saikyo_Toggle, CardPack.最強の遺伝子);
        SetupToggleListener(maboroshi_Toggle, CardPack.幻のいる島);
        SetupToggleListener(jikuu_Toggle, CardPack.時空の激闘);
        SetupToggleListener(choukoku_Toggle, CardPack.超克の光);
        SetupToggleListener(shiningHigh_Toggle, CardPack.シャイニングハイ);
        SetupToggleListener(souten_Toggle, CardPack.双天の守護者);
        SetupToggleListener(promo_Toggle, CardPack.PROMO);
    }
    
    // ----------------------------------------------------------------------
    // 個別トグルのリスナー設定
    // ----------------------------------------------------------------------
    private void SetupToggleListener(Toggle toggle, CardPack cardPack)
    {
        if (toggle == null) return;
        
        toggle.onValueChanged.AddListener((isOn) => {
            if (isOn)
            {
                selectedCardPacks.Add(cardPack);
            }
            else
            {
                selectedCardPacks.Remove(cardPack);
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
            // 現在選択されているカードパックをモデルに適用
            model.SetCardPackFilter(GetSelectedCardPacks());
        }
    }
    
    // ----------------------------------------------------------------------
    // 現在選択されているカードパックのリストを取得
    // ----------------------------------------------------------------------
    public HashSet<CardPack> GetSelectedCardPacks()
    {
        return new HashSet<CardPack>(selectedCardPacks);
    }
    
    // ----------------------------------------------------------------------
    // 何かしらのカードパックが選択されているかどうか
    // ----------------------------------------------------------------------
    public bool HasActiveFilters()
    {
        return selectedCardPacks.Count > 0;
    }
    
    // ----------------------------------------------------------------------
    // フィルターのリセット
    // ----------------------------------------------------------------------
    public void ResetFilters()
    {   
        // 選択状態をクリア
        selectedCardPacks.Clear();
        
        // トグルのUIをリセット（イベント発火を防ぐためにリスナー一時停止）
        ResetToggle(saikyo_Toggle);
        ResetToggle(maboroshi_Toggle);
        ResetToggle(jikuu_Toggle);
        ResetToggle(choukoku_Toggle);
        ResetToggle(shiningHigh_Toggle);
        ResetToggle(souten_Toggle);
        ResetToggle(promo_Toggle);
        
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
        if (saikyo_Toggle != null) saikyo_Toggle.onValueChanged.RemoveAllListeners();
        if (maboroshi_Toggle != null) maboroshi_Toggle.onValueChanged.RemoveAllListeners();
        if (jikuu_Toggle != null) jikuu_Toggle.onValueChanged.RemoveAllListeners();
        if (choukoku_Toggle != null) choukoku_Toggle.onValueChanged.RemoveAllListeners();
        if (shiningHigh_Toggle != null) shiningHigh_Toggle.onValueChanged.RemoveAllListeners();
        if (souten_Toggle != null) souten_Toggle.onValueChanged.RemoveAllListeners();
        if (promo_Toggle != null) promo_Toggle.onValueChanged.RemoveAllListeners();
    }
}