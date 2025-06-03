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
        // レビュータイミング設定
        public const float DEFAULT_REVIEW_TIME_1 = 7200f;   // 2時間
        public const float DEFAULT_REVIEW_TIME_2 = 36000f;  // 10時間
        
        // PlayerPrefsキー
        public const string DEFAULT_PLAYTIME_KEY = "playtime";
        public const string DEFAULT_REVIEW1_KEY = "review1";
        public const string DEFAULT_REVIEW2_KEY = "review2";
        
        // フィードバックメッセージ
        public const string DEFAULT_FEEDBACK_MSG_1 = "ご利用ありがとうございます！";
        public const string DEFAULT_FEEDBACK_MSG_2 = "いつもご利用いただき、ありがとうございます！";
        
        // 時間計算関連
        public const float SECONDS_PER_HOUR = 3600f;
        public const float SECONDS_PER_MINUTE = 60f;
        public const string TIME_FORMAT = "{0:D2}:{1:D2}:{2:D2}";
        
        // プレイヤープレフス値
        public const int REVIEW_SHOWN_VALUE = 1;
        public const int REVIEW_NOT_SHOWN_VALUE = 0;
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
    public void TryRequestReviewIfNeeded()
    {
#if UNITY_IOS
        try
        {
            ProcessReviewRequest();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ReviewManager] レビュー依頼処理中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
#endif
    }
    
    // レビュー依頼の内部処理ロジック
    // @summary 使用時間に基づいて適切なレビュー依頼を表示
    private void ProcessReviewRequest()
    {
#if UNITY_IOS
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
            PlayerPrefs.SetInt(reviewKey, Constants.REVIEW_SHOWN_VALUE);
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
        TimeComponents components = CalculateTimeComponents(totalSeconds);
        return FormatTimeComponents(components);
    }
    
    // 秒数から時・分・秒の構成要素を計算
    // @param totalSeconds 総秒数
    // @returns 時間コンポーネント構造体
    private TimeComponents CalculateTimeComponents(float totalSeconds)
    {
        TimeComponents result;
        result.hours = Mathf.FloorToInt(totalSeconds / Constants.SECONDS_PER_HOUR);
        result.minutes = Mathf.FloorToInt((totalSeconds % Constants.SECONDS_PER_HOUR) / Constants.SECONDS_PER_MINUTE);
        result.seconds = Mathf.FloorToInt(totalSeconds % Constants.SECONDS_PER_MINUTE);
        return result;
    }
    
    // 時間コンポーネントをフォーマット
    // @param components 時間コンポーネント
    // @returns フォーマット済み文字列 (HH:MM:SS)
    private string FormatTimeComponents(TimeComponents components)
    {
        return $"{components.hours:D2}:{components.minutes:D2}:{components.seconds:D2}";
    }
    
    // 時間コンポーネント構造体
    private struct TimeComponents
    {
        public int hours;
        public int minutes;
        public int seconds;
    }

    // ----------------------------------------------------------------------
    // レビュー依頼状態を取得
    // @returns 初回レビュー依頼済みか
    // ----------------------------------------------------------------------
    public bool IsReview1Shown()
    {
        return IsReviewShown(review1Key);
    }

    // @returns 2回目レビュー依頼済みか
    public bool IsReview2Shown()
    {
        return IsReviewShown(review2Key);
    }
    
    // 特定のレビュー依頼が表示済みかどうかを確認
    // @param reviewKey 確認するレビューキー
    // @returns レビュー依頼済みかどうか
    private bool IsReviewShown(string reviewKey)
    {
        return PlayerPrefs.GetInt(reviewKey, Constants.REVIEW_NOT_SHOWN_VALUE) == Constants.REVIEW_SHOWN_VALUE;
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
