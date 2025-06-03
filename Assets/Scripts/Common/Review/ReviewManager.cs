using UnityEngine;
#if UNITY_IOS
using UnityEngine.iOS;
#endif
using System;

// ----------------------------------------------------------------------
// App Storeレビュー依頼を管理するクラス
// 累計使用時間を測定し、適切なタイミングでレビュー依頼を表示する
// ----------------------------------------------------------------------
public class ReviewManager : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // 定数クラス
    // ----------------------------------------------------------------------
    private static class Constants
    {
        public const float DEFAULT_REVIEW_TIME_1 = 7200f;   // 2時間
        public const float DEFAULT_REVIEW_TIME_2 = 36000f;  // 10時間
        public const string DEFAULT_PLAYTIME_KEY = "playtime";
        public const string DEFAULT_REVIEW1_KEY = "review1";
        public const string DEFAULT_REVIEW2_KEY = "review2";
        public const string DEFAULT_FEEDBACK_MSG_1 = "ご利用ありがとうございます！";
        public const string DEFAULT_FEEDBACK_MSG_2 = "いつもご利用いただき、ありがとうございます！";
    }

    [Header("レビュー依頼タイミング（秒）")]
    [SerializeField] private float reviewTime1 = Constants.DEFAULT_REVIEW_TIME_1;
    [SerializeField] private float reviewTime2 = Constants.DEFAULT_REVIEW_TIME_2;

    [Header("PlayerPrefsキー")]
    [SerializeField] private string playtimeKey = Constants.DEFAULT_PLAYTIME_KEY;
    [SerializeField] private string review1Key = Constants.DEFAULT_REVIEW1_KEY;
    [SerializeField] private string review2Key = Constants.DEFAULT_REVIEW2_KEY;

    [Header("フィードバックメッセージ")]
    [SerializeField] private string feedbackMsg1 = Constants.DEFAULT_FEEDBACK_MSG_1;
    [SerializeField] private string feedbackMsg2 = Constants.DEFAULT_FEEDBACK_MSG_2;

    // シングルトンインスタンス
    private static ReviewManager _instance;
    public static ReviewManager Instance => _instance;

    // アプリがアクティブかどうか
    private bool _isAppActive = true;

    // ----------------------------------------------------------------------
    // Awakeメソッド（シングルトンの初期化）
    // ----------------------------------------------------------------------
    private void Awake()
    {
        try
        {
            SetupSingleton();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ReviewManager] シングルトン初期化中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // シングルトンのセットアップ
    // ----------------------------------------------------------------------
    private void SetupSingleton()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ----------------------------------------------------------------------
    // 毎フレーム使用時間を更新
    // ----------------------------------------------------------------------
    private void Update()
    {
        try
        {
            UpdatePlaytimeIfActive();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ReviewManager] 使用時間更新中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    private void UpdatePlaytimeIfActive()
    {
        if (_isAppActive)
        {
            float currentPlaytime = PlayerPrefs.GetFloat(playtimeKey, 0f);
            currentPlaytime += Time.unscaledDeltaTime;
            PlayerPrefs.SetFloat(playtimeKey, currentPlaytime);
        }
    }

    // ----------------------------------------------------------------------
    // アプリがフォーカスを得た/失った時の処理
    // ----------------------------------------------------------------------
    private void OnApplicationFocus(bool hasFocus)
    {
        _isAppActive = hasFocus;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        _isAppActive = !pauseStatus;
    }

    // ----------------------------------------------------------------------
    // レビュー依頼を必要に応じて実行
    // ----------------------------------------------------------------------
    // レビュー依頼を必要に応じて実行
    public void TryRequestReviewIfNeeded()
    {
#if UNITY_IOS
        try
        {
            float playtime = GetPlaytime();
            bool review1Shown = IsReview1Shown();
            bool review2Shown = IsReview2Shown();

            if (ShouldRequestFirstReview(playtime, review1Shown))
            {
                RequestReview(review1Key, feedbackMsg1);
            }
            else if (ShouldRequestSecondReview(playtime, review2Shown))
            {
                RequestReview(review2Key, feedbackMsg2);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ReviewManager] レビュー依頼処理中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
#endif
    }

    // @param playtime 現在の累計使用時間
    // @param review1Shown 初回レビュー依頼済みか
    // @returns 初回レビュー依頼すべきか
    private bool ShouldRequestFirstReview(float playtime, bool review1Shown)
    {
        return playtime >= reviewTime1 && !review1Shown;
    }

    // @param playtime 現在の累計使用時間
    // @param review2Shown 2回目レビュー依頼済みか
    // @returns 2回目レビュー依頼すべきか
    private bool ShouldRequestSecondReview(float playtime, bool review2Shown)
    {
        return playtime >= reviewTime2 && !review2Shown;
    }

    // @param reviewKey PlayerPrefsに保存するレビュー依頼済みキー
    // @param feedbackMsg フィードバック表示メッセージ
    private void RequestReview(string reviewKey, string feedbackMsg)
    {
#if UNITY_IOS
        try
        {
            Device.RequestStoreReview();
            PlayerPrefs.SetInt(reviewKey, 1);
            PlayerPrefs.Save();
            ShowFeedback(feedbackMsg);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ReviewManager] レビュー依頼実行中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
#endif
    }

    // @param message フィードバック表示メッセージ
    private void ShowFeedback(string message)
    {
        try
        {
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowSuccessFeedback(message);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ReviewManager] フィードバック表示中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // 現在の累計使用時間を取得（秒）
    // @returns 累計使用時間（秒）
    // ----------------------------------------------------------------------
    public float GetPlaytime()
    {
        return PlayerPrefs.GetFloat(playtimeKey, 0f);
    }

    // ----------------------------------------------------------------------
    // 使用時間を時間:分:秒形式で取得
    // @returns フォーマット済みの使用時間文字列
    // ----------------------------------------------------------------------
    public string GetPlaytimeFormatted()
    {
        float totalSeconds = GetPlaytime();
        int hours = Mathf.FloorToInt(totalSeconds / 3600f);
        int minutes = Mathf.FloorToInt((totalSeconds % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }

    // ----------------------------------------------------------------------
    // レビュー依頼状態を取得
    // @returns 初回レビュー依頼済みか
    // ----------------------------------------------------------------------
    public bool IsReview1Shown()
    {
        return PlayerPrefs.GetInt(review1Key, 0) == 1;
    }

    // @returns 2回目レビュー依頼済みか
    public bool IsReview2Shown()
    {
        return PlayerPrefs.GetInt(review2Key, 0) == 1;
    }

    // ----------------------------------------------------------------------
    // デバッグ用：データをリセット
    // ----------------------------------------------------------------------
#if DEVELOPMENT_BUILD
    // @summary レビュー関連データをリセット
    public void ResetReviewData()
    {
        try
        {
            PlayerPrefs.DeleteKey(playtimeKey);
            PlayerPrefs.DeleteKey(review1Key);
            PlayerPrefs.DeleteKey(review2Key);
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ReviewManager] データリセット中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // @summary 使用時間を強制設定
    // @param seconds 設定する秒数
    public void SetPlaytimeForTesting(float seconds)
    {
        try
        {
            PlayerPrefs.SetFloat(playtimeKey, seconds);
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ReviewManager] テスト用使用時間設定中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }
#endif

    // ----------------------------------------------------------------------
    // デバッグ情報を表示
    // ----------------------------------------------------------------------
    public void LogDebugInfo()
    {
        Debug.Log($"[ReviewManager] Playtime: {GetPlaytime()}秒, Review1: {IsReview1Shown()}, Review2: {IsReview2Shown()}");
    }
}
