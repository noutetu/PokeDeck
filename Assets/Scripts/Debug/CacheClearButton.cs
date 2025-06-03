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
    // ----------------------------------------------------------------------
    // 定数クラス
    // ----------------------------------------------------------------------
    private static class Constants
    {
        // 再起動設定
        public const float DEFAULT_RESTART_DELAY = 1.5f;
        public const bool DEFAULT_RESTART_AFTER_RESET = true;
        
        // ステータスメッセージ
        public const string STATUS_RESETTING = "完全リセット中...";
        public const string STATUS_COMPLETE = "完全リセット完了！";
        public const string STATUS_RELOADING = "再読み込みします...";
        public const string STATUS_READY = "準備完了";
        
        // 時間設定
        public const float STATUS_DISPLAY_DURATION = 2.0f;
    }
    
    [Header("キャッシュクリアボタン")]
    [SerializeField] private Button fullResetButton;
    
    [Header("再起動設定")]
    [SerializeField] private float restartDelay = Constants.DEFAULT_RESTART_DELAY; // 再起動までの待機時間（秒）
    [SerializeField] private bool restartAfterFullReset = Constants.DEFAULT_RESTART_AFTER_RESET; // 完全リセット後に再起動するか
    
    // ----------------------------------------------------------------------
    // 初期化処理
    // ----------------------------------------------------------------------
    private void Start()
    {
        try
        {
            SetupButtonListeners();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CacheClearButton] ボタン初期化中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // ボタンリスナーの設定
    // ----------------------------------------------------------------------
    private void SetupButtonListeners()
    {
        if (fullResetButton != null)
        {
            // ボタンのクリックイベントにFullResetメソッドを登録
            fullResetButton.onClick.AddListener(FullReset);
        }
        else
        {
            Debug.LogWarning("[CacheClearButton] fullResetButtonが設定されていません");
        }
    }
    // ----------------------------------------------------------------------
    // 完全リセット処理
    // ----------------------------------------------------------------------
    public void FullReset()
    {
        try
        {
            ExecuteFullReset();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CacheClearButton] 完全リセット中にエラー: {ex.Message}");
            Debug.LogException(ex);
            SetStatusText("リセット処理中にエラーが発生しました");
        }
    }

    // ----------------------------------------------------------------------
    // 完全リセットの実行
    // ----------------------------------------------------------------------
    private void ExecuteFullReset()
    {
        if (CardDatabase.Instance == null)
        {
            Debug.LogWarning("[CacheClearButton] CardDatabaseのインスタンスがnullです");
            return;
        }

        // キャッシュクリアの処理を実行
        SetStatusText(Constants.STATUS_RESETTING);
        CardDatabase.Instance.FullReset();

        // 完全リセット後の処理
        if (restartAfterFullReset)
        {
            StartCoroutine(RestartScene(Constants.STATUS_RELOADING));
        }
        else
        {
            StartCoroutine(ShowStatus(Constants.STATUS_COMPLETE, Constants.STATUS_DISPLAY_DURATION));
        }
    }

    // ----------------------------------------------------------------------
    // ステータス表示処理
    // ----------------------------------------------------------------------
    private void SetStatusText(string message)
    {
        try
        {
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowSuccessFeedback(message);
            }
            else
            {
                Debug.LogWarning($"[CacheClearButton] FeedbackContainerのインスタンスがnullです: {message}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CacheClearButton] ステータス表示中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }
    
    // ----------------------------------------------------------------------
    // ステータス表示を一定時間後にリセット
    // ----------------------------------------------------------------------
    private IEnumerator ShowStatus(string message, float duration)
    {
        SetStatusText(message);

        yield return new WaitForSeconds(duration);

        SetStatusText(Constants.STATUS_READY);
    }
    
    // ----------------------------------------------------------------------
    // シーン再起動処理
    // ----------------------------------------------------------------------
    private IEnumerator RestartScene(string message)
    {
        SetStatusText(message);
        
        // 指定された遅延時間待機
        yield return new WaitForSeconds(restartDelay);
        
        try
        {
            // 現在のシーンを再読み込み
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CacheClearButton] シーン再起動中にエラー: {ex.Message}");
            Debug.LogException(ex);
            SetStatusText("シーン再読み込み中にエラーが発生しました");
        }
    }
    
    // ----------------------------------------------------------------------
    // クリーンアップ処理
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        try
        {
            CleanupButtonListeners();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CacheClearButton] クリーンアップ中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // ボタンリスナーのクリーンアップ
    // ----------------------------------------------------------------------
    private void CleanupButtonListeners()
    {
        if (fullResetButton != null)
        {
            fullResetButton.onClick.RemoveListener(FullReset);
        }
    }
}