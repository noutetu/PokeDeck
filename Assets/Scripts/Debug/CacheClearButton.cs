using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

// ----------------------------------------------------------------------
// キャッシュクリアボタン制御スクリプト
// このスクリプトは、キャッシュのクリアや完全リセットを行うボタンの制御を担当します。
// 主な機能:
// - キャッシュクリアボタンのクリックイベント処理
// - 完全リセットボタンのクリックイベント処理
// - ステータス表示の更新と一定時間後のリセット
// - キャッシュクリア後のシーン再読み込み処理
// ----------------------------------------------------------------------
public class CacheClearButton : MonoBehaviour
{
    [Header("キャッシュクリアボタン")]
    [SerializeField] private Button fullResetButton;
    
    [Header("再起動設定")]
    [SerializeField] private float restartDelay = 1.5f; // 再起動までの待機時間（秒）
    [SerializeField] private bool restartAfterFullReset = true; // 完全リセット後に再起動するか
    
    // ----------------------------------------------------------------------
    // 初期化処理
    // ----------------------------------------------------------------------
    private void Start()
    {
        if (fullResetButton != null)
        {
            // ボタンのクリックイベントにFullResetメソッドを登録
            fullResetButton.onClick.AddListener(FullReset);
        }
    }
    // ----------------------------------------------------------------------
    // 完全リセット処理
    // ----------------------------------------------------------------------
    public void FullReset()
    {
        if (CardDatabase.Instance != null)
        {
            // キャッシュクリアの処理を実行
            SetStatusText("完全リセット中...");
            CardDatabase.Instance.FullReset();

            // 完全リセット後の処理
            if (restartAfterFullReset)
            {
                StartCoroutine(RestartScene("再読み込みします..."));
            }
            else
            {
                StartCoroutine(ShowStatus("完全リセット完了！", 2f));
            }
        }
    }

    // ----------------------------------------------------------------------
    // ステータス表示処理
    // ----------------------------------------------------------------------
    private void SetStatusText(string message)
    {
        FeedbackContainer.Instance?.ShowSuccessFeedback(message);
    }
    
    // ----------------------------------------------------------------------
    // ステータス表示を一定時間後にリセット
    // ----------------------------------------------------------------------
    private IEnumerator ShowStatus(string message, float duration)
    {
        SetStatusText(message);

        yield return new WaitForSeconds(duration);

        SetStatusText("準備完了");
    }
    
    // ----------------------------------------------------------------------
    // シーン再起動処理
    // ----------------------------------------------------------------------
    private IEnumerator RestartScene(string message)
    {
        SetStatusText(message);
        
        // 指定された遅延時間待機
        yield return new WaitForSeconds(restartDelay);
        
        // 現在のシーンを再読み込み
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
    
    // ----------------------------------------------------------------------
    // クリーンアップ処理
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        if (fullResetButton != null)
        {
            fullResetButton.onClick.RemoveListener(FullReset);
        }
    }
}