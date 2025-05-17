// TrueShadowToggleInset.cs
using UnityEngine;
using UnityEngine.UI;
using LeTai.TrueShadow;

[AddComponentMenu("UI/True Shadow/True Shadow Toggle Inset")]
[RequireComponent(typeof(TrueShadow))]
public class TrueShadowToggleInset : MonoBehaviour
{
    private TrueShadow[] shadows;
    private float[] normalOpacity;
    private Toggle toggle;
    
    private void OnEnable()
    {
        // TrueShadowコンポーネントを取得
        shadows = GetComponents<TrueShadow>();
        normalOpacity = new float[shadows.Length];
        
        // 初期状態の不透明度を記録
        for (int i = 0; i < shadows.Length; i++)
        {
            normalOpacity[i] = shadows[i].Color.a;
        }
        
        // トグルコンポーネントの取得と初期設定
        toggle = GetComponent<Toggle>();
        if (toggle != null)
        {
            // 初期状態の設定
            ApplyInsetState(toggle.isOn);
            
            // トグルの状態変化時のイベント登録
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
    }
    
    // トグル状態変化時の処理
    public void OnToggleValueChanged(bool isOn)
    {
        ApplyInsetState(isOn);
    }
    
    // 影のへこみ状態を適用
    private void ApplyInsetState(bool isInset)
    {
        if (shadows == null || shadows.Length == 0) return;
        
        for (int i = 0; i < shadows.Length; i++)
        {
            // Insetプロパティを切り替え
            shadows[i].Inset = isInset;
            
            // オプション：不透明度も調整したい場合
            var color = shadows[i].Color;
            color.a = isInset ? normalOpacity[i] * 0.8f : normalOpacity[i];
            shadows[i].Color = color;
        }
    }
    
    // 影の状態を強制的に更新するパブリックメソッド
    // トグルのリセット処理などで使用
    public void UpdateInsetState(bool isOn)
    {
        ApplyInsetState(isOn);
    }
    
    private void OnDestroy()
    {
        // イベントリスナーの解除
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }
}