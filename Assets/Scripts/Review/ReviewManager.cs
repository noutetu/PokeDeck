using UnityEngine;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

// ----------------------------------------------------------------------
// App Storeレビュー依頼を管理するクラス
// 累計使用時間を測定し、適切なタイミングでレビュー依頼を表示する
// ----------------------------------------------------------------------
public class ReviewManager : MonoBehaviour
{
    // シングルトンインスタンス
    private static ReviewManager _instance;
    public static ReviewManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("ReviewManager");
                _instance = go.AddComponent<ReviewManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ----------------------------------------------------------------------
    // レビュー依頼のタイミング設定
    // ----------------------------------------------------------------------
    private const float REVIEW_TIME_1 = 7200f;   // 2時間（初回レビュー依頼）
    private const float REVIEW_TIME_2 = 36000f;  // 10時間（2回目レビュー依頼）

    // PlayerPrefsキー
    private const string PLAYTIME_KEY = "playtime";
    private const string REVIEW1_KEY = "review1";
    private const string REVIEW2_KEY = "review2";

    // アプリがアクティブかどうか
    private bool _isAppActive = true;

    // ----------------------------------------------------------------------
    // Unity初期化
    // ----------------------------------------------------------------------
    private void Awake()
    {
        // シングルトン保証
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[ReviewManager] 初期化完了 - 現在の累計使用時間: {GetPlaytime():F1}秒");
    }

    // ----------------------------------------------------------------------
    // 毎フレーム使用時間を更新
    // ----------------------------------------------------------------------
    private void Update()
    {
        // アプリがアクティブな時のみ時間を加算
        if (_isAppActive)
        {
            float currentPlaytime = PlayerPrefs.GetFloat(PLAYTIME_KEY, 0f);
            currentPlaytime += Time.unscaledDeltaTime;
            PlayerPrefs.SetFloat(PLAYTIME_KEY, currentPlaytime);
        }
    }

    // ----------------------------------------------------------------------
    // アプリがフォーカスを得た/失った時の処理
    // ----------------------------------------------------------------------
    private void OnApplicationFocus(bool hasFocus)
    {
        _isAppActive = hasFocus;
        Debug.Log($"[ReviewManager] アプリフォーカス変更: {hasFocus}");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        _isAppActive = !pauseStatus;
        Debug.Log($"[ReviewManager] アプリポーズ変更: {pauseStatus}");
    }

    // ----------------------------------------------------------------------
    // レビュー依頼を試行
    // ----------------------------------------------------------------------
    public void TryRequestReview()
    {
#if UNITY_IOS
        float playtime = GetPlaytime();
        bool review1Shown = PlayerPrefs.GetInt(REVIEW1_KEY, 0) == 1;
        bool review2Shown = PlayerPrefs.GetInt(REVIEW2_KEY, 0) == 1;

        Debug.Log($"[ReviewManager] レビュー依頼チェック - 使用時間: {playtime:F1}秒, review1: {review1Shown}, review2: {review2Shown}");

        // 初回レビュー依頼（2時間後）
        if (playtime >= REVIEW_TIME_1 && !review1Shown)
        {
            Debug.Log("[ReviewManager] 初回レビュー依頼を表示");
            Device.RequestStoreReview();
            PlayerPrefs.SetInt(REVIEW1_KEY, 1);
            PlayerPrefs.Save();
            
            // フィードバック表示（オプション）
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowInfoFeedback("ご利用ありがとうございます！");
            }
        }
        // 2回目レビュー依頼（10時間後）
        else if (playtime >= REVIEW_TIME_2 && !review2Shown)
        {
            Debug.Log("[ReviewManager] 2回目レビュー依頼を表示");
            Device.RequestStoreReview();
            PlayerPrefs.SetInt(REVIEW2_KEY, 1);
            PlayerPrefs.Save();
            
            // フィードバック表示（オプション）
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowInfoFeedback("いつもご利用いただき、ありがとうございます！");
            }
        }
#else
        Debug.Log("[ReviewManager] レビュー依頼はiOSプラットフォームでのみ動作します");
#endif
    }

    // ----------------------------------------------------------------------
    // 現在の累計使用時間を取得（秒）
    // ----------------------------------------------------------------------
    public float GetPlaytime()
    {
        return PlayerPrefs.GetFloat(PLAYTIME_KEY, 0f);
    }

    // ----------------------------------------------------------------------
    // 使用時間を時間:分:秒形式で取得
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
    // ----------------------------------------------------------------------
    public bool IsReview1Shown()
    {
        return PlayerPrefs.GetInt(REVIEW1_KEY, 0) == 1;
    }

    public bool IsReview2Shown()
    {
        return PlayerPrefs.GetInt(REVIEW2_KEY, 0) == 1;
    }

    // ----------------------------------------------------------------------
    // デバッグ用：データをリセット
    // ----------------------------------------------------------------------
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public void ResetReviewData()
    {
        PlayerPrefs.DeleteKey(PLAYTIME_KEY);
        PlayerPrefs.DeleteKey(REVIEW1_KEY);
        PlayerPrefs.DeleteKey(REVIEW2_KEY);
        PlayerPrefs.Save();
        Debug.Log("[ReviewManager] レビューデータをリセットしました");
    }

    // ----------------------------------------------------------------------
    // デバッグ用：使用時間を強制設定
    // ----------------------------------------------------------------------
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public void SetPlaytimeForTesting(float seconds)
    {
        PlayerPrefs.SetFloat(PLAYTIME_KEY, seconds);
        PlayerPrefs.Save();
        Debug.Log($"[ReviewManager] テスト用使用時間を設定: {seconds}秒");
    }

    // ----------------------------------------------------------------------
    // デバッグ情報を表示
    // ----------------------------------------------------------------------
    public void LogDebugInfo()
    {
        Debug.Log($"[ReviewManager] === デバッグ情報 ===");
        Debug.Log($"累計使用時間: {GetPlaytimeFormatted()} ({GetPlaytime():F1}秒)");
        Debug.Log($"初回レビュー表示済み: {IsReview1Shown()}");
        Debug.Log($"2回目レビュー表示済み: {IsReview2Shown()}");
        Debug.Log($"次回レビュー条件: {(GetPlaytime() < REVIEW_TIME_1 ? $"{REVIEW_TIME_1 - GetPlaytime():F0}秒後" : GetPlaytime() < REVIEW_TIME_2 ? $"{REVIEW_TIME_2 - GetPlaytime():F0}秒後" : "すべて完了")}");
    }
}
