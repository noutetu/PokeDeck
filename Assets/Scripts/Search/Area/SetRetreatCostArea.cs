using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Enum;

// ----------------------------------------------------------------------
// 逃げるコストのフィルタリングを担当するPresenter
// 「以下」「同じ」「以上」のトグルとコストドロップダウンを管理
// ----------------------------------------------------------------------
public class SetRetreatCostArea : MonoBehaviour, IFilterArea
{
    public enum RetreatComparisonType
    {
        None,          // 比較なし（選択していない状態）
        LessOrEqual,   // 以下
        Equal,         // 同じ
        GreaterOrEqual // 以上
    }
    
    [Header("逃げるコスト比較トグル")]
    [SerializeField] private Toggle lessOrEqualToggle;    // 以下トグル
    [SerializeField] private Toggle equalToggle;          // 同じトグル
    [SerializeField] private Toggle greaterOrEqualToggle; // 以上トグル
    
    [Header("逃げるコストドロップダウン")]
    [SerializeField] private Dropdown retreatCostDropdown;    // コストドロップダウン
    
    // ----------------------------------------------------------------------
    // 選択された比較タイプとコスト値
    // ----------------------------------------------------------------------
    private RetreatComparisonType selectedComparisonType = RetreatComparisonType.None;
    private int selectedRetreatCost = 0;
    
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
        if (retreatCostDropdown != null)
        {
            // ドロップダウンの項目をクリア
            retreatCostDropdown.ClearOptions();
            
            // 新しいオプションリストを作成
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            
            // 「指定なし」を最初のオプションとして追加
            options.Add(new Dropdown.OptionData("指定なし"));
            
            // コスト値のオプションを追加（0から4まで）
            for (int cost = 0; cost <= 4; cost++)
            {
                options.Add(new Dropdown.OptionData(cost.ToString()));
            }
            
            // 作成したオプションリストをドロップダウンに設定
            retreatCostDropdown.AddOptions(options);
            
            // 初期値は「指定なし」
            retreatCostDropdown.value = 0;
            retreatCostDropdown.RefreshShownValue();
            
            // 既存のリスナーをクリアして新しいリスナーを設定
            retreatCostDropdown.onValueChanged.RemoveAllListeners();
            retreatCostDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            
        }
        else
        {
            Debug.LogError("逃げるコストドロップダウンがnullです。Inspectorで設定してください。");
        }
    }
    
    // ----------------------------------------------------------------------
    // ドロップダウン値変更時の処理
    // ----------------------------------------------------------------------
    private void OnDropdownValueChanged(int index)
    {
        // インデックスの有効性チェック
        if (retreatCostDropdown == null || index < 0 || index >= retreatCostDropdown.options.Count)
        {
            Debug.LogError($"無効なドロップダウンインデックス: {index}");
            return;
        }
        
        // 選択されたテキストを取得
        string selectedText = retreatCostDropdown.options[index].text;
        Debug.Log($"選択された逃げるコスト値: インデックス={index}, テキスト={selectedText}");
        
        // 指定なしの処理
        if (selectedText == "指定なし")
        {
            // 前回の値を保存
            bool hadActiveFilter = HasActiveFilters();
            RetreatComparisonType prevComparisonType = selectedComparisonType;
            
            // 選択状態をリセット
            selectedRetreatCost = 0;
            // 特殊値の1ではなく正しくNoneを設定
            selectedComparisonType = RetreatComparisonType.None;
            
            // トグルを無効化して全てオフに
            SetTogglesInteractable(false);
            SetAllTogglesOff();
            
            // OKボタンを押すまではフィルタリングを実行しない
            Debug.Log($"逃げるコストフィルターをクリア（前回のアクティブ状態: {hadActiveFilter}, 比較タイプ: {prevComparisonType}）");
            return;
        }
        
        // コスト値をパース
        if (int.TryParse(selectedText, out int cost))
        {
            selectedRetreatCost = cost;
            Debug.Log($"逃げるコスト値を設定: {selectedRetreatCost}");
            
            // トグルを有効化
            SetTogglesInteractable(true);
            
            // トグルが一つもOnになっていない場合は「同じ」トグルを選択状態にする
            if (selectedComparisonType == RetreatComparisonType.None || 
                (!lessOrEqualToggle.isOn && !equalToggle.isOn && !greaterOrEqualToggle.isOn))
            {
                selectedComparisonType = RetreatComparisonType.Equal;
                
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
            
            // 比較タイプが選択されている場合はフィルター更新のログを出すが、
            // OKボタンを押すまではフィルタリングを実行しない
            if (selectedComparisonType != RetreatComparisonType.None)
            {
                Debug.Log($"逃げるコストフィルター更新: コスト={selectedRetreatCost}, 比較={selectedComparisonType}");
            }
        }
        else
        {
            Debug.LogError($"コスト値のパースに失敗: {selectedText}");
        }
    }
    
    // ----------------------------------------------------------------------
    // トグルの初期化とイベントリスナー設定
    // ----------------------------------------------------------------------
    private void InitializeToggles()
    {
        // 初期状態ではトグルを無効化（コストが選択されていないため）
        SetTogglesInteractable(false);
        
        // トグルをトグルグループに入れるためにトグルグループを作成
        ToggleGroup toggleGroup = gameObject.AddComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = true;  // グループ内のすべてのトグルをオフにできるようにする
        
        // 各トグルをトグルグループに追加
        if (lessOrEqualToggle != null)
        {
            lessOrEqualToggle.group = toggleGroup;
            lessOrEqualToggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, RetreatComparisonType.LessOrEqual));
        }
        
        if (equalToggle != null)
        {
            equalToggle.group = toggleGroup;
            equalToggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, RetreatComparisonType.Equal));
            
            // デフォルトで「同じ」トグルを選択状態にしておく（ただし、まだ有効化しない）
            equalToggle.SetIsOnWithoutNotify(true);
            selectedComparisonType = RetreatComparisonType.Equal;
            Debug.Log("🔍 「同じ」トグルをデフォルトで選択状態に設定");
        }
        
        if (greaterOrEqualToggle != null)
        {
            greaterOrEqualToggle.group = toggleGroup;
            greaterOrEqualToggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, RetreatComparisonType.GreaterOrEqual));
        }
    }
    
    // ----------------------------------------------------------------------
    // トグルの状態変更時の処理
    // ----------------------------------------------------------------------
    private void OnToggleValueChanged(bool isOn, RetreatComparisonType comparisonType)
    {
        if (isOn)
        {
            // トグルがオンになった場合、比較タイプを設定
            selectedComparisonType = comparisonType;
        }
        else if (selectedComparisonType == comparisonType)
        {
            // 同じトグルがオフになった場合は比較タイプをクリア
            selectedComparisonType = RetreatComparisonType.None;
        }
        
        // コスト値が選択されている場合はログを出すが、
        // OKボタンを押すまではフィルタリングを実行しない
        if (selectedRetreatCost >= 0)
        {
            Debug.Log($"逃げるコスト比較タイプフィルター変更: {comparisonType} → {isOn}, 選択値: {selectedRetreatCost}");
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
        
        Debug.Log("🔄 すべてのトグルの状態、色、影をオフに更新しました");
    }
    
    // ----------------------------------------------------------------------
    // 現在選択されている逃げるコスト値を取得
    // ----------------------------------------------------------------------
    public int GetSelectedRetreatCost()
    {
        return selectedRetreatCost;
    }
    
    // ----------------------------------------------------------------------
    // 現在選択されている比較タイプを取得
    // ----------------------------------------------------------------------
    public RetreatComparisonType GetSelectedComparisonType()
    {
        return selectedComparisonType;
    }
    
    // ----------------------------------------------------------------------
    // コストフィルターが有効かどうか
    // ----------------------------------------------------------------------
    public bool HasActiveFilters()
    {
        // ドロップダウンが「指定なし」以外で、比較タイプがNoneでない場合は有効
        return retreatCostDropdown.value > 0 && selectedComparisonType != RetreatComparisonType.None;
    }
    
    // ----------------------------------------------------------------------
    // フィルターのリセット
    // ----------------------------------------------------------------------
    public void ResetFilters()
    {
        Debug.Log("📋 逃げるコストフィルターをリセット開始");
        
        // ドロップダウンは「指定なし」に戻す
        if (retreatCostDropdown != null)
        {
            // インデックス0が「指定なし」
            retreatCostDropdown.SetValueWithoutNotify(0);
            retreatCostDropdown.RefreshShownValue();
            selectedRetreatCost = 0;
        }
        
        // いったんすべてのトグルをオフにする
        SetAllTogglesOff();
        // 比較タイプをNoneにリセットし、トグルを無効化
        selectedComparisonType = RetreatComparisonType.None;
        SetTogglesInteractable(false);
        
        Debug.Log("✅ 逃げるコストフィルターのリセット完了");
        
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
    public void ApplyFilterToModel(SearchModel model)
    {
        if (model != null)
        {
            // ドロップダウンが「指定なし」または比較タイプがNoneの場合はフィルタリングをスキップ
            if (retreatCostDropdown.value == 0 || selectedComparisonType == RetreatComparisonType.None)
            {
                Debug.Log($"🔍 逃げるコストフィルターは無効なのでスキップします（ドロップダウン値={retreatCostDropdown.value}, 比較タイプ={selectedComparisonType}）");
                // フィルター未選択状態を設定（無効化）
                model.SetRetreatCostFilter(0, RetreatComparisonType.None);
            }
            else
            {
                // 現在選択されているコスト条件をモデルに適用
                model.SetRetreatCostFilter(selectedRetreatCost, selectedComparisonType);
                Debug.Log($"🔍 逃げるコストフィルターをモデルに適用: コスト={selectedRetreatCost}, 比較タイプ={selectedComparisonType}");
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
        if (retreatCostDropdown != null) retreatCostDropdown.onValueChanged.RemoveAllListeners();
    }
}
