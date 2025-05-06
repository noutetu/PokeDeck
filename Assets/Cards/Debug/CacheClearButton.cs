using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// デバッグ用のキャッシュクリアボタン制御スクリプト
/// </summary>
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
    
    /// <summary>
    /// キャッシュをクリアして再読み込みする
    /// </summary>
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
    
    /// <summary>
    /// カードデータを完全にリセット（ファイル削除含む）
    /// </summary>
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
    
    /// <summary>
    /// ステータステキストを設定
    /// </summary>
    private void SetStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = $"ステータス: {message}";
        }
        Debug.Log($"[CacheClearButton] {message}");
    }
    
    /// <summary>
    /// ステータス表示を一定時間後に元に戻す
    /// </summary>
    private IEnumerator ShowStatus(string message, float duration)
    {
        SetStatusText(message);
        
        yield return new WaitForSeconds(duration);
        
        SetStatusText("準備完了");
    }
    
    /// <summary>
    /// コンポーネント破棄時にイベントをクリア
    /// </summary>
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