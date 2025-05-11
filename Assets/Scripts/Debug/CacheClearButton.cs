using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// ----------------------------------------------------------------------
// デバッグ用のキャッシュクリアボタン制御スクリプト
// このスクリプトは、キャッシュのクリアや完全リセットを行うボタンの制御を担当します。
// 主な機能:
// - キャッシュクリアボタンのクリックイベント処理
// - 完全リセットボタンのクリックイベント処理
// - ステータス表示の更新と一定時間後のリセット
// ----------------------------------------------------------------------
public class CacheClearButton : MonoBehaviour
{
    [SerializeField] private Button clearCacheButton;
    [SerializeField] private Button fullResetButton;
    [SerializeField] private TMP_Text statusText;
    
    private void Start()
    {
        // ボタンイベントを設定
        if (clearCacheButton != null)
        {
            clearCacheButton.onClick.AddListener(ClearCacheAndReload);
        }
        
        if (fullResetButton != null)
        {
            fullResetButton.onClick.AddListener(FullReset);
        }
        
        if (statusText != null)
        {
            statusText.text = "ステータス: 準備完了";
        }
    }
    
    // ----------------------------------------------------------------------
    // キャッシュクリア処理
    // ----------------------------------------------------------------------
    public void ClearCacheAndReload()
    {
        if (CardDatabase.Instance != null)
        {
            SetStatusText("キャッシュクリア中...");
            CardDatabase.Instance.ClearCacheAndReload();
            StartCoroutine(ShowStatus("キャッシュクリア完了！", 2f));
        }
        else
        {
            SetStatusText("エラー: CardDatabaseが見つかりません");
        }
    }
    
    // ----------------------------------------------------------------------
    // 完全リセット処理
    // ----------------------------------------------------------------------
    public void FullReset()
    {
        if (CardDatabase.Instance != null)
        {
            SetStatusText("完全リセット中...");
            CardDatabase.Instance.FullReset();
            StartCoroutine(ShowStatus("完全リセット完了！", 2f));
        }
        else
        {
            SetStatusText("エラー: CardDatabaseが見つかりません");
        }
    }
    
    // ----------------------------------------------------------------------
    // ステータス表示処理
    // ----------------------------------------------------------------------
    private void SetStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = $"ステータス: {message}";
        }
        Debug.Log($"[CacheClearButton] {message}");
    }
    
    private IEnumerator ShowStatus(string message, float duration)
    {
        SetStatusText(message);
        
        yield return new WaitForSeconds(duration);
        
        SetStatusText("準備完了");
    }
    
    // ----------------------------------------------------------------------
    // クリーンアップ処理
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        if (clearCacheButton != null)
        {
            clearCacheButton.onClick.RemoveListener(ClearCacheAndReload);
        }
        
        if (fullResetButton != null)
        {
            fullResetButton.onClick.RemoveListener(FullReset);
        }
    }
}