using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ----------------------------------------------------------------------
// 最大エネルギーコストのフィルタリングを担当するPresenter
// 「以下」「同じ」「以上」のトグルと最大エネルギーコストドロップダウンを管理
// ----------------------------------------------------------------------
public class SetMaxEnergyArea : MonoBehaviour, IFilterArea
{
    public enum EnergyComparisonType
    {
        None,           // 比較なし（選択していない状態）
        LessOrEqual,    // 以下
        Equal,          // 同じ
        GreaterOrEqual  // 以上
    }
    
    [Header("エネルギーコスト比較トグル")]
    [SerializeField] private Toggle lessOrEqualToggle;    // 以下トグル
    [SerializeField] private Toggle equalToggle;          // 同じトグル
    [SerializeField] private Toggle greaterOrEqualToggle; // 以上トグル
    
    [Header("エネルギーコストドロップダウン")]
    [SerializeField] private Dropdown energyCostDropdown;    // エネルギーコストドロップダウン
    
    // ----------------------------------------------------------------------
    // 選択された比較タイプと値
    // ----------------------------------------------------------------------
    private EnergyComparisonType selectedComparisonType = EnergyComparisonType.None;
    private int selectedEnergyCost = 0;
    
    // ----------------------------------------------------------------------
    // フィルター変更時のイベント
    // ----------------------------------------------------------------------
    public event Action OnFilterChanged;
    
    // ----------------------------------------------------------------------   
    // UnityのStartメソッド
    // ----------------------------------------------------------------------
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
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>
            {
                // 「指定なし」を最初のオプションとして追加
                new Dropdown.OptionData("指定なし")
            };

            // エネルギーコスト値のオプションを追加（1から5まで）
            for (int cost = 1; cost <= 5; cost++)
            {
                options.Add(new Dropdown.OptionData(cost.ToString()));
            }

            // 作成したオプションリストをドロップダウンに設定
            energyCostDropdown.AddOptions(options);

            // 初期値は「指定なし」
            energyCostDropdown.value = 0;
            energyCostDropdown.RefreshShownValue();

            // 既存のリスナーをクリアして新しいリスナーを設定
            energyCostDropdown.onValueChanged.RemoveAllListeners();
            energyCostDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
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
            return;
        }
        
        // 選択されたテキストを取得
        string selectedText = energyCostDropdown.options[index].text;
        
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
            }
            return;
        }

        // エネルギーコスト値をパース
        if (int.TryParse(selectedText, out int cost))
        {
            selectedEnergyCost = cost;

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
                }
            }
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
        }

        // 「同じ」トグルが選択されている場合は、他のトグルをオフにする
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
        // ドロップダウンが「指定なし」以外で、比較タイプがNoneでない場合は有効
        return energyCostDropdown.value > 0 && selectedComparisonType != EnergyComparisonType.None;
    }
    
    // ----------------------------------------------------------------------
    // フィルターのリセット
    // ----------------------------------------------------------------------
    public void ResetFilters()
    {
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
        // 比較タイプをNoneにリセットし、トグルを無効化
        selectedComparisonType = EnergyComparisonType.None;
        SetTogglesInteractable(false);
        
        // リセット後にフィルター変更を通知
        OnFilterChanged?.Invoke();
    }
    
    // ----------------------------------------------------------------------
    // OKボタンが押されたときに現在のフィルターをモデルに適用する
    // ----------------------------------------------------------------------
    public void ApplyFilterToModel(SearchModel model)
    {
        if (model != null)
        {
            // ドロップダウンが「指定なし」または比較タイプがNoneの場合はフィルタリングをスキップ
            if (energyCostDropdown.value == 0 || selectedComparisonType == EnergyComparisonType.None)
            {
                // フィルター未選択状態を設定（無効化）
                model.SetMaxEnergyCostFilter(0, EnergyComparisonType.None);
            }
            else
            {
                // 現在選択されているエネルギーコスト条件をモデルに適用
                model.SetMaxEnergyCostFilter(selectedEnergyCost, selectedComparisonType);
            }
        }
    }

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
