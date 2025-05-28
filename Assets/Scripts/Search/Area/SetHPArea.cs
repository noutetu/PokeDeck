using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ----------------------------------------------------------------------
// HPのフィルタリングを担当するPresenter
// 「以下」「同じ」「以上」のトグルとHPドロップダウンを管理
// ----------------------------------------------------------------------
public class SetHPArea : MonoBehaviour, IFilterArea
{
    public enum HPComparisonType
    {
        None,       // 比較なし（選択していない状態）
        LessOrEqual,   // 以下
        Equal,         // 同じ
        GreaterOrEqual // 以上
    }
    
    [Header("HP比較トグル")]
    [SerializeField] private Toggle lessOrEqualToggle;    // 以下トグル
    [SerializeField] private Toggle equalToggle;          // 同じトグル
    [SerializeField] private Toggle greaterOrEqualToggle; // 以上トグル
    
    [Header("HPドロップダウン")]
    [SerializeField] private Dropdown hpDropdown;    // HPドロップダウン
    
    // ----------------------------------------------------------------------
    // 選択されたHP比較タイプとHP値
    // ----------------------------------------------------------------------
    private HPComparisonType selectedComparisonType = HPComparisonType.None;
    private int selectedHP = 0;
    
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
        if (hpDropdown != null)
        {
            // ドロップダウンの項目をクリア
            hpDropdown.ClearOptions();
            
            // 新しいオプションリストを作成
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>
            {
                // 「指定なし」を最初のオプションとして追加
                new Dropdown.OptionData("指定なし")
            };
            
            // HP値のオプションを追加（30から200まで10刻み）
            for (int hp = 30; hp <= 200; hp += 10)
            {
                options.Add(new Dropdown.OptionData(hp.ToString()));
            }
            
            // 作成したオプションリストをドロップダウンに設定
            hpDropdown.AddOptions(options);
            
            // 初期値は「指定なし」
            hpDropdown.value = 0;
            hpDropdown.RefreshShownValue();
            
            // 既存のリスナーをクリアして新しいリスナーを設定
            hpDropdown.onValueChanged.RemoveAllListeners();
            hpDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            
        }
    }
    
    // ----------------------------------------------------------------------
    // ドロップダウン値変更時の処理
    // ----------------------------------------------------------------------
    private void OnDropdownValueChanged(int index)
    {
        // インデックスの有効性チェック
        if (hpDropdown == null || index < 0 || index >= hpDropdown.options.Count)
        {
            return;
        }
        
        // 選択されたテキストを取得
        string selectedText = hpDropdown.options[index].text;
        
        // 指定なしの処理
        if (selectedText == "指定なし")
        {
            // 選択状態をリセット
            selectedHP = 0;
            selectedComparisonType = HPComparisonType.None;
            
            // トグルを無効化して全てオフに
            SetTogglesInteractable(false);
            SetAllTogglesOff();
            return;
        }
        
        // HP値をパース
        if (int.TryParse(selectedText, out int hp))
        {
            selectedHP = hp;
            
            // トグルを有効化
            SetTogglesInteractable(true);
            
            // トグルが一つもOnになっていない場合は「同じ」トグルを選択状態にする
            if (selectedComparisonType == HPComparisonType.None || 
                (!lessOrEqualToggle.isOn && !equalToggle.isOn && !greaterOrEqualToggle.isOn))
            {
                selectedComparisonType = HPComparisonType.Equal;
                
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
        // 初期状態ではトグルを無効化（HPが選択されていないため）
        SetTogglesInteractable(false);
        
        // トグルをトグルグループに入れるためにトグルグループを作成
        ToggleGroup toggleGroup = gameObject.AddComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = true;  // グループ内のすべてのトグルをオフにできるようにする
        
        // 各トグルをトグルグループに追加
        if (lessOrEqualToggle != null)
        {
            lessOrEqualToggle.group = toggleGroup;
            lessOrEqualToggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, HPComparisonType.LessOrEqual));
        }
        
        // 「同じ」トグルはデフォルトで選択状態にするが、まだ有効化しない
        if (equalToggle != null)
        {
            equalToggle.group = toggleGroup;
            equalToggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, HPComparisonType.Equal));

            // デフォルトで「同じ」トグルを選択状態にしておく（ただし、まだ有効化しない）
            equalToggle.SetIsOnWithoutNotify(true);
            selectedComparisonType = HPComparisonType.Equal;
        }
        
        // 「以上」トグルをトグルグループに追加
        if (greaterOrEqualToggle != null)
        {
            greaterOrEqualToggle.group = toggleGroup;
            greaterOrEqualToggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, HPComparisonType.GreaterOrEqual));
        }
    }
    
    // ----------------------------------------------------------------------
    // トグルの状態変更時の処理
    // ----------------------------------------------------------------------
    private void OnToggleValueChanged(bool isOn, HPComparisonType comparisonType)
    {
        if (isOn)
        {
            // トグルがオンになった場合、比較タイプを設定
            selectedComparisonType = comparisonType;
        }
        else if (selectedComparisonType == comparisonType)
        {
            // 同じトグルがオフになった場合は比較タイプをクリア
            selectedComparisonType = HPComparisonType.None;
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
    // 現在選択されているHP条件を取得
    // ----------------------------------------------------------------------
    public int GetSelectedHP()
    {
        return selectedHP;
    }
    
    // ----------------------------------------------------------------------
    // 現在選択されている比較タイプを取得
    // ----------------------------------------------------------------------
    public HPComparisonType GetSelectedComparisonType()
    {
        return selectedComparisonType;
    }
    
    // ----------------------------------------------------------------------
    // HPフィルターが有効かどうか
    // ----------------------------------------------------------------------
    public bool HasActiveFilters()
    {
        // ドロップダウンが「指定なし」以外で、比較タイプがNoneでない場合は有効
        return hpDropdown.value > 0 && selectedComparisonType != HPComparisonType.None;
    }
    
    // ----------------------------------------------------------------------
    // フィルターのリセット
    // ----------------------------------------------------------------------
    public void ResetFilters()
    {
        // ドロップダウンは「指定なし」に戻す
        if (hpDropdown != null)
        {
            // インデックス0が「指定なし」
            hpDropdown.SetValueWithoutNotify(0);
            hpDropdown.RefreshShownValue();
            selectedHP = 0;
        }
        
        // いったんすべてのトグルをオフにする
        SetAllTogglesOff();
        // 比較タイプをNoneにリセットし、トグルを無効化
        selectedComparisonType = HPComparisonType.None;
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
            if (hpDropdown.value == 0 || selectedComparisonType == HPComparisonType.None)
            {
                // フィルター未選択状態を設定（無効化）
                model.SetHPFilter(0, HPComparisonType.None);
            }
            else
            {
                // 現在選択されているHP条件をモデルに適用
                model.SetHPFilter(selectedHP, selectedComparisonType);
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
        if (hpDropdown != null) hpDropdown.onValueChanged.RemoveAllListeners();
    }
}