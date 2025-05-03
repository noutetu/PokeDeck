// SimpleToggleColor.cs
using UnityEngine;
using UnityEngine.UI;

public class SimpleToggleColor : MonoBehaviour
{
    public Color onColor = new Color(0.29f, 1, 0.26f);   // オン状態の色
    public Color offColor = new Color(0.93f, 0.3f, 0.23f); // オフ状態の色
    public Graphic targetGraphic;   // 色を変更する対象のグラフィック

    private void Start()
    {
        // トグルコンポーネントの取得と初期設定
        Toggle toggle = GetComponent<Toggle>();
        if (toggle != null && targetGraphic != null)
        {
            // 初期状態の色を設定
            targetGraphic.color = toggle.isOn ? onColor : offColor;
            
            // トグルの状態変化時のイベント登録
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
    }

    // トグル状態変化時の処理
    public void OnToggleValueChanged(bool isOn)
    {
        if (targetGraphic != null)
        {
            // 状態に応じて色を変更
            targetGraphic.color = isOn ? onColor : offColor;
        }
    }
    
    // トグルの色を強制的に更新するパブリックメソッド
    // SetToggleWithoutNotifyを使用してトグルの状態を変更した場合に使用
    public void UpdateColorState(bool isOn)
    {
        if (targetGraphic != null)
        {
            targetGraphic.color = isOn ? onColor : offColor;
        }
    }

    private void OnDestroy()
    {
        // イベントリスナーの解除
        Toggle toggle = GetComponent<Toggle>();
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }
}