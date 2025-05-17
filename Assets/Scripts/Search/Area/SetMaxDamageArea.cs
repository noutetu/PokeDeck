using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ----------------------------------------------------------------------
// 最大ダメージのフィルタリングを担当するPresenter
// 「以下」「同じ」「以上」のトグルと最大ダメージドロップダウンを管理
// ----------------------------------------------------------------------
public class SetMaxDamageArea : MonoBehaviour, IFilterArea
{
    public enum DamageComparisonType
    {
        None,          // 比較なし（選択していない状態）
        LessOrEqual,   // 以下
        Equal,         // 同じ
        GreaterOrEqual // 以上
    }
    
    [Header("ダメージ比較トグル")]
    [SerializeField] private Toggle lessOrEqualToggle;    // 以下トグル
    [SerializeField] private Toggle equalToggle;          // 同じトグル
    [SerializeField] private Toggle greaterOrEqualToggle; // 以上トグル
    
    [Header("ダメージドロップダウン")]
    [SerializeField] private Dropdown damageDropdown;     // ダメージドロップダウン
    
    // ----------------------------------------------------------------------
    // 選択された比較タイプと値
    // ----------------------------------------------------------------------
    private DamageComparisonType selectedComparisonType = DamageComparisonType.None;
    private int selectedDamage = 0;
    
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
        if (damageDropdown != null)
        {
            // ドロップダウンの項目をクリア
            damageDropdown.ClearOptions();

            // 新しいオプションリストを作成
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>
            {
                // 「指定なし」を最初のオプションとして追加
                new Dropdown.OptionData("指定なし")
            };

            // ダメージ値のオプションを追加（0から200まで10刻み）
            for (int damage = 0; damage <= 200; damage += 10)
            {
                options.Add(new Dropdown.OptionData(damage.ToString()));
            }

            // 作成したオプションリストをドロップダウンに設定
            damageDropdown.AddOptions(options);

            // 初期値は「指定なし」
            damageDropdown.value = 0;
            damageDropdown.RefreshShownValue();

            // 既存のリスナーをクリアして新しいリスナーを設定
            damageDropdown.onValueChanged.RemoveAllListeners();
            damageDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
    }
    
    // ----------------------------------------------------------------------
    // ドロップダウン値変更時の処理
    // ----------------------------------------------------------------------
    private void OnDropdownValueChanged(int index)
    {
        // インデックスの有効性チェック
        if (damageDropdown == null || index < 0 || index >= damageDropdown.options.Count)
        {
            return;
        }
        
        // 選択されたテキストを取得
        string selectedText = damageDropdown.options[index].text;
        
        // 指定なしの処理
        if (selectedText == "指定なし")
        {
            // 前回の値を保存
            bool hadActiveFilter = HasActiveFilters();
            DamageComparisonType prevComparisonType = selectedComparisonType;
            
            // 選択状態をリセット
            selectedDamage = 0;
            selectedComparisonType = DamageComparisonType.None;
            
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

        // ダメージ値をパース
        if (int.TryParse(selectedText, out int damage))
        {
            selectedDamage = damage;

            // トグルを有効化
            SetTogglesInteractable(true);

            // トグルが一つもOnになっていない場合は「同じ」トグルを選択状態にする
            if (selectedComparisonType == DamageComparisonType.None ||
                (!lessOrEqualToggle.isOn && !equalToggle.isOn && !greaterOrEqualToggle.isOn))
            {
                selectedComparisonType = DamageComparisonType.Equal;

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
        // 初期状態ではトグルを無効化（ダメージが選択されていないため）
        SetTogglesInteractable(false);
        
        // トグルをトグルグループに入れるためにトグルグループを作成
        ToggleGroup toggleGroup = gameObject.AddComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = true;  // グループ内のすべてのトグルをオフにできるようにする
        
        // 各トグルをトグルグループに追加
        if (lessOrEqualToggle != null)
        {
            lessOrEqualToggle.group = toggleGroup;
            lessOrEqualToggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, DamageComparisonType.LessOrEqual));
        }
        
        // 「以下」トグルの初期化
        if (equalToggle != null)
        {
            // 「同じ」トグルをトグルグループに追加
            equalToggle.group = toggleGroup;
            equalToggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, DamageComparisonType.Equal));

            // デフォルトで「同じ」トグルを選択状態にしておく（ただし、まだ有効化しない）
            equalToggle.SetIsOnWithoutNotify(true);
            selectedComparisonType = DamageComparisonType.Equal;

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
        
        // 「以上」トグルの初期化
        if (greaterOrEqualToggle != null)
        {
            greaterOrEqualToggle.group = toggleGroup;
            greaterOrEqualToggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, DamageComparisonType.GreaterOrEqual));
        }
    }
    
    // ----------------------------------------------------------------------
    // トグルの状態変更時の処理
    // ----------------------------------------------------------------------
    private void OnToggleValueChanged(bool isOn, DamageComparisonType comparisonType)
    {
        if (isOn)
        {
            // トグルがオンになった場合、比較タイプを設定
            selectedComparisonType = comparisonType;
        }
        else if (selectedComparisonType == comparisonType)
        {
            // 同じトグルがオフになった場合は比較タイプをクリア
            selectedComparisonType = DamageComparisonType.None;
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
    // 現在選択されているダメージ値を取得
    // ----------------------------------------------------------------------
    public int GetSelectedDamage()
    {
        return selectedDamage;
    }
    
    // ----------------------------------------------------------------------
    // 現在選択されている比較タイプを取得
    // ----------------------------------------------------------------------
    public DamageComparisonType GetSelectedComparisonType()
    {
        return selectedComparisonType;
    }
    
    // ----------------------------------------------------------------------
    // ダメージフィルターが有効かどうか
    // ----------------------------------------------------------------------
    public bool HasActiveFilters()
    {
        // 0ダメージも有効なオプションなので、ドロップダウン選択と比較タイプで判断
        return damageDropdown.value > 0 && selectedComparisonType != DamageComparisonType.None;
    }
    
    // ----------------------------------------------------------------------
    // フィルターのリセット
    // ----------------------------------------------------------------------
    public void ResetFilters()
    {
        // ドロップダウンは「指定なし」に戻す
        if (damageDropdown != null)
        {
            // インデックス0が「指定なし」
            damageDropdown.SetValueWithoutNotify(0);
            damageDropdown.RefreshShownValue();
            selectedDamage = 0;
        }
        
        // いったんすべてのトグルをオフにする
        SetAllTogglesOff();
        // 比較タイプをNoneにリセットし、トグルを無効化
        selectedComparisonType = DamageComparisonType.None;
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
            if (damageDropdown.value == 0 || selectedComparisonType == DamageComparisonType.None)
            {
                // フィルター未選択状態を設定（無効化）
                model.SetMaxDamageFilter(0, DamageComparisonType.None);
            }
            else
            {
                // 現在選択されているダメージ条件をモデルに適用
                model.SetMaxDamageFilter(selectedDamage, selectedComparisonType);
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
        if (damageDropdown != null) damageDropdown.onValueChanged.RemoveAllListeners();
    }
}
