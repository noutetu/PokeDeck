using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Enum;

// ----------------------------------------------------------------------
// 最大エネルギーコストのフィルタリングを担当するPresenter
// 「以下」「同じ」「以上」のトグルと最大エネルギーコストドロップダウンを管理
// ----------------------------------------------------------------------
public class SetMaxEnergyArea : MonoBehaviour
{
    public enum EnergyComparisonType
    {
        None,          // 比較なし（選択していない状態）
        LessOrEqual,   // 以下
        Equal,         // 同じ
        GreaterOrEqual // 以上
    }
    
    [Header("エネルギーコスト比較トグル")]
    [SerializeField] private Toggle lessOrEqualToggle;    // 以下トグル
    [SerializeField] private Toggle equalToggle;          // 同じトグル
    [SerializeField] private Toggle greaterOrEqualToggle; // 以上トグル
    
    [Header("エネルギーコストドロップダウン")]
    [SerializeField] private Dropdown energyCostDropdown;    // エネルギーコストドロップダウン
    
    // 選択された比較タイプと値
    private EnergyComparisonType selectedComparisonType = EnergyComparisonType.None;
    private int selectedEnergyCost = 0;
    
    // フィルター変更時のイベント
    public event Action OnFilterChanged;
    
    private void Start()
    {
        InitializeDropdown();
        InitializeToggles();
    }
    
    // ----------------------------------------------------------------------
    // ドロップダウンの初期化とイベントリスナー設定
    // ----------------------------------------------------------------------
    private void InitializeDropdown()
    {
        if (energyCostDropdown != null)
        {
            // ドロップダウンの項目をクリア
            energyCostDropdown.ClearOptions();
            
            // 新しいオプションリストを作成
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            
            // 「指定なし」を最初のオプションとして追加
            options.Add(new Dropdown.OptionData("指定なし"));
            
            // エネルギーコスト値のオプションを追加（0から200まで10刻み）
            for (int energyCost = 0; energyCost <= 4; energyCost += 1)
            {
                options.Add(new Dropdown.OptionData(energyCost.ToString()));
            }
            
            // 作成したオプションリストをドロップダウンに設定
            energyCostDropdown.AddOptions(options);
            
            // 初期値は「指定なし」
            energyCostDropdown.value = 0;
            energyCostDropdown.RefreshShownValue();
            
            // 既存のリスナーをクリアして新しいリスナーを設定
            energyCostDropdown.onValueChanged.RemoveAllListeners();
            energyCostDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            
            // デバッグ用にオプション一覧を出力
            Debug.Log($"エネルギーコストドロップダウンの初期化完了: オプション数={energyCostDropdown.options.Count}");
        }
        else
        {
            Debug.LogError("エネルギーコストドロップダウンがnullです。Inspectorで設定してください。");
        }
    }
    
    // ----------------------------------------------------------------------
    // ドロップダウン値変更時の処理
    // ----------------------------------------------------------------------
    private void OnDropdownValueChanged(int index)
    {
        // インデックスの有効性チェック
        if (energyCostDropdown == null || index < 0 || index >= energyCostDropdown.options.Count)
        {
            Debug.LogError($"無効なドロップダウンインデックス: {index}");
            return;
        }
        
        // 選択されたテキストを取得
        string selectedText = energyCostDropdown.options[index].text;
        Debug.Log($"選択されたエネルギーコストドロップダウン値: インデックス={index}, テキスト={selectedText}");
        
        // 指定なしの処理
        if (selectedText == "指定なし")
        {
            // 前回の値を保存
            bool hadActiveFilter = HasActiveFilters();
            EnergyComparisonType prevComparisonType = selectedComparisonType;
            
            // 選択状態をリセット
            selectedEnergyCost = 0;
            selectedComparisonType = EnergyComparisonType.None;
            
            // トグルを無効化して全てオフに
            SetTogglesInteractable(false);
            SetAllTogglesOff();
            
            // フィルター変更を通知（前回アクティブだった場合のみ）
            if (hadActiveFilter)
            {
                OnFilterChanged?.Invoke();
                Debug.Log($"エネルギーコストフィルターをクリア（前回のアクティブ状態: {hadActiveFilter}, 比較タイプ: {prevComparisonType}）");
            }
            return;
        }
        
        // エネルギーコスト値をパース
        if (int.TryParse(selectedText, out int energyCost))
        {
            selectedEnergyCost = energyCost;
            Debug.Log($"エネルギーコスト値を設定: {selectedEnergyCost}");
            
            // トグルを有効化
            SetTogglesInteractable(true);
            
            // トグルが一つもOnになっていない場合は「同じ」トグルを選択状態にする
            if (selectedComparisonType == EnergyComparisonType.None || 
                (!lessOrEqualToggle.isOn && !equalToggle.isOn && !greaterOrEqualToggle.isOn))
            {
                selectedComparisonType = EnergyComparisonType.Equal;
                
                // 「同じ」トグルをオンにする
                if (equalToggle != null)
                {
                    equalToggle.SetIsOnWithoutNotify(true);
                    
                    // SimpleToggleColorコンポーネントを更新
                    SimpleToggleColor colorComponent = equalToggle.GetComponent<SimpleToggleColor>();
                    if (colorComponent != null)
                    {
                        colorComponent.UpdateColorState(true);
                    }
                    
                    // TrueShadowToggleInsetコンポーネントを更新
                    TrueShadowToggleInset shadowComponent = equalToggle.GetComponent<TrueShadowToggleInset>();
                    if (shadowComponent != null)
                    {
                        shadowComponent.UpdateInsetState(true);
                    }
                    
                    Debug.Log("🔍 トグルが選択されていなかったため、「同じ」トグルをデフォルトで選択しました");
                }
            }
            
            // 比較タイプが選択されている場合はフィルター更新（オプションOKボタン対応）
            if (selectedComparisonType != EnergyComparisonType.None)
            {
                // OKボタンを押すまではフィルタリングを実行しない
                // OnFilterChanged?.Invoke();
                Debug.Log($"エネルギーコストフィルター更新: エネルギーコスト={selectedEnergyCost}, 比較={selectedComparisonType}");
            }
        }
        else
        {
            Debug.LogError($"エネルギーコスト値のパースに失敗: {selectedText}");
        }
    }
    
    // ----------------------------------------------------------------------
    // トグルの初期化とイベントリスナー設定
    // ----------------------------------------------------------------------
    private void InitializeToggles()
    {
        // 初期状態ではトグルを無効化（エネルギーコストが選択されていないため）
        SetTogglesInteractable(false);
        
        // トグルをトグルグループに入れるためにトグルグループを作成
        ToggleGroup toggleGroup = gameObject.AddComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = true;  // グループ内のすべてのトグルをオフにできるようにする
        
        // 各トグルをトグルグループに追加
        if (lessOrEqualToggle != null)
        {
            lessOrEqualToggle.group = toggleGroup;
            lessOrEqualToggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, EnergyComparisonType.LessOrEqual));
        }
        
        if (equalToggle != null)
        {
            equalToggle.group = toggleGroup;
            equalToggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, EnergyComparisonType.Equal));
            
            // デフォルトで「同じ」トグルを選択状態にしておく（ただし、まだ有効化しない）
            equalToggle.SetIsOnWithoutNotify(true);
            selectedComparisonType = EnergyComparisonType.Equal;
            
            // SimpleToggleColorコンポーネントも更新して視覚的に選択状態にする
            SimpleToggleColor colorComponent = equalToggle.GetComponent<SimpleToggleColor>();
            if (colorComponent != null)
            {
                colorComponent.UpdateColorState(true);
            }
            
            // TrueShadowToggleInsetコンポーネントも更新
            TrueShadowToggleInset shadowComponent = equalToggle.GetComponent<TrueShadowToggleInset>();
            if (shadowComponent != null)
            {
                shadowComponent.UpdateInsetState(true);
            }
            
            Debug.Log("🔍 「同じ」トグルをデフォルトで選択状態に設定（視覚状態も更新）");
        }
        
        if (greaterOrEqualToggle != null)
        {
            greaterOrEqualToggle.group = toggleGroup;
            greaterOrEqualToggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, EnergyComparisonType.GreaterOrEqual));
        }
    }
    
    // ----------------------------------------------------------------------
    // トグルの状態変更時の処理
    // ----------------------------------------------------------------------
    private void OnToggleValueChanged(bool isOn, EnergyComparisonType comparisonType)
    {
        if (isOn)
        {
            // トグルがオンになった場合、比較タイプを設定
            selectedComparisonType = comparisonType;
        }
        else if (selectedComparisonType == comparisonType)
        {
            // 同じトグルがオフになった場合は比較タイプをクリア
            selectedComparisonType = EnergyComparisonType.None;
        }
        
        // フィルター変更を通知（エネルギーコスト値が選択されている場合のみ）
        // 0エネルギーコストも有効なオプションなので、ドロップダウンの選択インデックスで判断
        if (energyCostDropdown.value > 0)
        {
            // OKボタンを押すまではフィルタリングを実行しない
            // OnFilterChanged?.Invoke();
            Debug.Log($"エネルギーコスト比較タイプフィルター変更: {comparisonType} → {isOn}, 選択値: {selectedEnergyCost}");
        }
    }
    
    // ----------------------------------------------------------------------
    // すべてのトグルをオフに設定（色と影の状態も更新）
    // ----------------------------------------------------------------------
    private void SetAllTogglesOff()
    {
        // 「以下」トグルをオフにする
        if (lessOrEqualToggle != null)
        {
            lessOrEqualToggle.SetIsOnWithoutNotify(false);
            
            // SimpleToggleColorも更新
            SimpleToggleColor colorComponent = lessOrEqualToggle.GetComponent<SimpleToggleColor>();
            if (colorComponent != null)
            {
                colorComponent.UpdateColorState(false);
            }
            
            // TrueShadowToggleInsetも更新
            TrueShadowToggleInset shadowComponent = lessOrEqualToggle.GetComponent<TrueShadowToggleInset>();
            if (shadowComponent != null)
            {
                shadowComponent.UpdateInsetState(false);
            }
        }
        
        // 「同じ」トグルをオフにする
        if (equalToggle != null)
        {
            equalToggle.SetIsOnWithoutNotify(false);
            
            // SimpleToggleColorも更新
            SimpleToggleColor colorComponent = equalToggle.GetComponent<SimpleToggleColor>();
            if (colorComponent != null)
            {
                colorComponent.UpdateColorState(false);
            }
            
            // TrueShadowToggleInsetも更新
            TrueShadowToggleInset shadowComponent = equalToggle.GetComponent<TrueShadowToggleInset>();
            if (shadowComponent != null)
            {
                shadowComponent.UpdateInsetState(false);
            }
        }
        
        // 「以上」トグルをオフにする
        if (greaterOrEqualToggle != null)
        {
            greaterOrEqualToggle.SetIsOnWithoutNotify(false);
            
            // SimpleToggleColorも更新
            SimpleToggleColor colorComponent = greaterOrEqualToggle.GetComponent<SimpleToggleColor>();
            if (colorComponent != null)
            {
                colorComponent.UpdateColorState(false);
            }
            
            // TrueShadowToggleInsetも更新
            TrueShadowToggleInset shadowComponent = greaterOrEqualToggle.GetComponent<TrueShadowToggleInset>();
            if (shadowComponent != null)
            {
                shadowComponent.UpdateInsetState(false);
            }
        }
        
        Debug.Log("🔄 すべてのトグルの状態、色、影をオフに更新しました");
    }
    
    // ----------------------------------------------------------------------
    // すべてのトグルの有効/無効を設定
    // ----------------------------------------------------------------------
    private void SetTogglesInteractable(bool interactable)
    {
        if (lessOrEqualToggle != null) lessOrEqualToggle.interactable = interactable;
        if (equalToggle != null) equalToggle.interactable = interactable;
        if (greaterOrEqualToggle != null) greaterOrEqualToggle.interactable = interactable;
    }
    
    // ----------------------------------------------------------------------
    // 現在選択されているエネルギーコスト値を取得
    // ----------------------------------------------------------------------
    public int GetSelectedEnergyCost()
    {
        return selectedEnergyCost;
    }
    
    // ----------------------------------------------------------------------
    // 現在選択されている比較タイプを取得
    // ----------------------------------------------------------------------
    public EnergyComparisonType GetSelectedComparisonType()
    {
        return selectedComparisonType;
    }
    
    // ----------------------------------------------------------------------
    // エネルギーコストフィルターが有効かどうか
    // ----------------------------------------------------------------------
    public bool HasActiveFilters()
    {
        // 0エネルギーコストも有効なオプションなので、ドロップダウン選択と比較タイプで判断
        return energyCostDropdown.value > 0 && selectedComparisonType != EnergyComparisonType.None;
    }
    
    // ----------------------------------------------------------------------
    // フィルターのリセット
    // ----------------------------------------------------------------------
    public void ResetFilters()
    {
        Debug.Log("📋 エネルギーコストフィルターをリセット開始");
        
        // ドロップダウンは「指定なし」に戻す
        if (energyCostDropdown != null)
        {
            // インデックス0が「指定なし」
            energyCostDropdown.SetValueWithoutNotify(0);
            energyCostDropdown.RefreshShownValue();
            selectedEnergyCost = 0;
        }
        
        // いったんすべてのトグルをオフにする
        SetAllTogglesOff();
        
        // 「同じ」トグルを選択状態にする
        if (equalToggle != null)
        {
            // 「同じ」トグルをオンにする
            equalToggle.SetIsOnWithoutNotify(true);
            selectedComparisonType = EnergyComparisonType.Equal;
            
            // SimpleToggleColorコンポーネントを更新
            SimpleToggleColor colorComponent = equalToggle.GetComponent<SimpleToggleColor>();
            if (colorComponent != null)
            {
                colorComponent.UpdateColorState(true);
            }
            
            // TrueShadowToggleInsetコンポーネントを更新
            TrueShadowToggleInset shadowComponent = equalToggle.GetComponent<TrueShadowToggleInset>();
            if (shadowComponent != null)
            {
                shadowComponent.UpdateInsetState(true);
            }
            
            Debug.Log("🔍 「同じ」トグルをデフォルト選択状態に戻しました");
        }
        
        // トグルを無効化
        SetTogglesInteractable(false);
        
        Debug.Log("✅ エネルギーコストフィルターのリセット完了");
        
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
    // OKボタンが押されたときに現在のフィルターをモデルに適用する
    // ----------------------------------------------------------------------
    // public void ApplyFilterToModel(SearchModel model)
    // {
    //     if (model != null)
    //     {
    //         // ドロップダウンが「指定なし」の場合は、ComparisonTypeがNoneになっているはず
    //         if (energyCostDropdown.value == 0 || selectedComparisonType == EnergyComparisonType.None)
    //         {
    //             // 「指定なし」の場合、明示的にNoneとしてモデルに設定
    //             model.SetMaxEnergyCostFilter(0, EnergyComparisonType.None);
    //             Debug.Log("🔍 エネルギーコストフィルター: 「指定なし」をモデルに適用");
    //         }
    //         else
    //         {
    //             // 通常の条件設定（数値が選択されている場合）
    //             model.SetMaxEnergyCostFilter(selectedEnergyCost, selectedComparisonType);
    //             Debug.Log($"🔍 エネルギーコストフィルターをモデルに適用: コスト={selectedEnergyCost}, 比較タイプ={selectedComparisonType}");
    //         }
    //     }
    // }

    // ----------------------------------------------------------------------
    // OnDestroy時にイベントをクリア
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        if (lessOrEqualToggle != null) lessOrEqualToggle.onValueChanged.RemoveAllListeners();
        if (equalToggle != null) equalToggle.onValueChanged.RemoveAllListeners();
        if (greaterOrEqualToggle != null) greaterOrEqualToggle.onValueChanged.RemoveAllListeners();
        if (energyCostDropdown != null) energyCostDropdown.onValueChanged.RemoveAllListeners();
    }
}
