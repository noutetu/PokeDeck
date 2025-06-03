using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening; // DOTween追加
using System;

// ----------------------------------------------------------------------
// フィードバック表示を管理するコンテナクラス
// 画面上部に固定表示されるフィードバックUIを管理する
// ----------------------------------------------------------------------
public class FeedbackContainer : MonoBehaviour
{
    // 定数クラス
    private static class Constants
    {
        // 時間設定
        public const float DEFAULT_DISPLAY_DURATION = 1.5f;
        public const float DEFAULT_ANIMATION_DURATION = 0.5f;
        
        // 位置設定
        public const float DEFAULT_POSITION_OFFSET_X = 0f;
        public const float DEFAULT_POSITION_OFFSET_Y = -100f;
        public const float DEFAULT_FLOAT_DISTANCE = 30f;
        
        // アンカー設定
        public const float ANCHOR_MIN_X = 0.5f;
        public const float ANCHOR_MIN_Y = 1f;
        public const float ANCHOR_MAX_X = 0.5f;
        public const float ANCHOR_MAX_Y = 1f;
        public const float PIVOT_X = 0.5f;
        public const float PIVOT_Y = 1f;
        
        // アニメーション設定
        public const float ALPHA_VISIBLE = 1f;
        public const float ALPHA_INVISIBLE = 0f;
        public const float ANIMATION_DURATION_RATIO = 0.5f;
    }

    [Header("フィードバック設定")]
    [SerializeField] private GameObject successPrefab; // 成功時のフィードバック用プレハブ
    [SerializeField] private GameObject failurePrefab; // 失敗時のフィードバック用プレハブ
    [SerializeField] private float displayDuration = Constants.DEFAULT_DISPLAY_DURATION; // 表示時間（秒）
    [SerializeField] private Vector2 positionOffset = new Vector2(Constants.DEFAULT_POSITION_OFFSET_X, Constants.DEFAULT_POSITION_OFFSET_Y); // 位置オフセット（画面上部からの距離）
    
    [Header("アニメーション設定")]
    [SerializeField] private bool useAnimation = true; // アニメーションを使用するかどうか
    [SerializeField] private float animationDuration = Constants.DEFAULT_ANIMATION_DURATION; // アニメーション時間（秒）
    [SerializeField] private float floatDistance = Constants.DEFAULT_FLOAT_DISTANCE; // 浮き上がる距離（ピクセル）
    [SerializeField] private Ease appearEase = Ease.OutBack; // 出現時のイージング
    [SerializeField] private Ease disappearEase = Ease.InBack; // 消失時のイージング

    // -------------------------------------------------
    // フィードバックインスタンス
    // -------------------------------------------------
    private GameObject currentFeedbackInstance;
    private Coroutine hideCoroutine;
    private Sequence currentAnimation;
    
    // シングルトンパターン
    private static FeedbackContainer _instance;
    public static FeedbackContainer Instance => _instance;
    
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
            Debug.LogError($"[FeedbackContainer] シングルトン初期化中にエラーが発生しました: {ex.Message}");
            Debug.LogException(ex);
        }
    }
    
    // ----------------------------------------------------------------------
    // シングルトンのセットアップ
    // ----------------------------------------------------------------------
    private void SetupSingleton()
    {
        // シングルトンの設定
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }
    
    // ----------------------------------------------------------------------
    // 成功フィードバックメッセージを表示（時間指定可）
    // ----------------------------------------------------------------------
    public void ShowSuccessFeedback(string message, float duration = -1f)
    {
        try
        {
            if (successPrefab == null)
            {
                Debug.LogWarning("[FeedbackContainer] 成功用プレハブが設定されていません");
                return;
            }
            
            ShowFeedback(successPrefab, message, duration);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FeedbackContainer] 成功フィードバック表示中にエラーが発生しました: {ex.Message}");
            Debug.LogException(ex);
        }
    }
    
    // ----------------------------------------------------------------------
    // 失敗フィードバックメッセージを表示（時間指定可）
    // ----------------------------------------------------------------------
    public void ShowFailureFeedback(string message, float duration = -1f)
    {
        try
        {
            if (failurePrefab == null)
            {
                Debug.LogWarning("[FeedbackContainer] 失敗用プレハブが設定されていません");
                return;
            }
            
            ShowFeedback(failurePrefab, message, duration);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FeedbackContainer] 失敗フィードバック表示中にエラーが発生しました: {ex.Message}");
            Debug.LogException(ex);
        }
    }
    
    // ----------------------------------------------------------------------
    // 共通フィードバック表示処理
    // ----------------------------------------------------------------------
    private void ShowFeedback(GameObject prefab, string message, float duration = -1f)
    {
        // 表示時間が指定されていない場合はデフォルト値を使用
        float showDuration = duration > 0f ? duration : displayDuration;
        
        // 既存のフィードバックを非表示
        HideCurrentFeedback();
        
        // プレハブをインスタンス化
        currentFeedbackInstance = Instantiate(prefab, transform);
        SetupFeedbackInstance(currentFeedbackInstance, message, showDuration);
    }
    
    // ----------------------------------------------------------------------
    // フィードバックインスタンスの初期設定
    // ----------------------------------------------------------------------
    private void SetupFeedbackInstance(GameObject instance, string message, float duration)
    {
        try
        {
            if (instance == null) return;
            
            SetupRectTransform(instance);
            SetupCanvasGroup(instance);
            SetupFeedbackText(instance, message);
            
            // フィードバックを表示してアニメーション開始
            instance.SetActive(true);
            
            StartFeedbackAnimation(instance, duration);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FeedbackContainer] フィードバックインスタンスの初期設定中にエラーが発生しました: {ex.Message}");
            Debug.LogException(ex);
        }
    }
    
    // ----------------------------------------------------------------------
    // RectTransformの設定
    // ----------------------------------------------------------------------
    private void SetupRectTransform(GameObject instance)
    {
        RectTransform rectTransform = instance.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // アンカーを画面上部中央に設定
            rectTransform.anchorMin = new Vector2(Constants.ANCHOR_MIN_X, Constants.ANCHOR_MIN_Y);
            rectTransform.anchorMax = new Vector2(Constants.ANCHOR_MAX_X, Constants.ANCHOR_MAX_Y);
            rectTransform.pivot = new Vector2(Constants.PIVOT_X, Constants.PIVOT_Y);
            
            // オフセット位置を設定（画面上部からの距離）
            rectTransform.anchoredPosition = positionOffset;
        }
    }
    
    // ----------------------------------------------------------------------
    // CanvasGroupの設定
    // ----------------------------------------------------------------------
    private void SetupCanvasGroup(GameObject instance)
    {
        // CanvasGroupの追加（アニメーション用）
        if (instance.GetComponent<CanvasGroup>() == null)
        {
            instance.AddComponent<CanvasGroup>();
        }
    }
    
    // ----------------------------------------------------------------------
    // フィードバックテキストの設定
    // ----------------------------------------------------------------------
    private void SetupFeedbackText(GameObject instance, string message)
    {
        // テキストコンポーネントを取得して更新
        TextMeshProUGUI feedbackText = instance.GetComponentInChildren<TextMeshProUGUI>();
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }
    
    // ----------------------------------------------------------------------
    // フィードバックアニメーションの開始
    // ----------------------------------------------------------------------
    private void StartFeedbackAnimation(GameObject instance, float duration)
    {
        if (useAnimation)
        {
            PlayShowAnimation(instance, duration);
        }
        else
        {
            // アニメーションなしの場合は単純に一定時間後に非表示
            hideCoroutine = StartCoroutine(HideFeedbackAfterDelay(duration));
        }
    }
    
    // ----------------------------------------------------------------------
    // 現在表示中のフィードバックを非表示にする
    // ----------------------------------------------------------------------
    private void HideCurrentFeedback()
    {
        try
        {
            CleanupCurrentAnimation();
            CleanupCurrentCoroutine();
            CleanupCurrentInstance();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FeedbackContainer] フィードバックの非表示処理中にエラーが発生しました: {ex.Message}");
            Debug.LogException(ex);
        }
    }
    
    // ----------------------------------------------------------------------
    // 現在のアニメーションをクリーンアップ
    // ----------------------------------------------------------------------
    private void CleanupCurrentAnimation()
    {
        // 実行中のアニメーションがあればキャンセル
        if (currentAnimation != null)
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
    }
    
    // ----------------------------------------------------------------------
    // 現在のコルーチンをクリーンアップ
    // ----------------------------------------------------------------------
    private void CleanupCurrentCoroutine()
    {
        // 既に実行中のコルーチンがあればキャンセル
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }
    
    // ----------------------------------------------------------------------
    // 現在のインスタンスをクリーンアップ
    // ----------------------------------------------------------------------
    private void CleanupCurrentInstance()
    {
        // 既存のフィードバックがあれば破棄
        if (currentFeedbackInstance != null)
        {
            Destroy(currentFeedbackInstance);
            currentFeedbackInstance = null;
        }
    }
    
    // ----------------------------------------------------------------------
    // 表示アニメーションを再生
    // ----------------------------------------------------------------------
    private void PlayShowAnimation(GameObject instance, float duration)
    {
        try
        {
            CanvasGroup canvasGroup = instance.GetComponent<CanvasGroup>();
            RectTransform rectTransform = instance.GetComponent<RectTransform>();

            if (rectTransform == null || canvasGroup == null) return;
            
            // アニメーションの設定を準備
            AnimationSettings settings = PrepareAnimationSettings();
            
            // アニメーションの初期状態を設定
            InitializeAnimationState(rectTransform, canvasGroup, settings);
            
            // DOTweenシーケンスを作成し実行
            CreateAndPlayAnimation(rectTransform, canvasGroup, settings, instance, duration);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FeedbackContainer] アニメーション再生中にエラーが発生しました: {ex.Message}");
            Debug.LogException(ex);
        }
    }
    
    // ----------------------------------------------------------------------
    // アニメーション設定を準備
    // ----------------------------------------------------------------------
    private AnimationSettings PrepareAnimationSettings()
    {
        return new AnimationSettings
        {
            StartPosition = positionOffset + new Vector2(0, floatDistance), // 下から登場
            EndPosition = positionOffset, // 最終位置
            ExitPosition = positionOffset - new Vector2(0, floatDistance) // 上に消えていく位置
        };
    }
    
    // ----------------------------------------------------------------------
    // アニメーション初期状態を設定
    // ----------------------------------------------------------------------
    private void InitializeAnimationState(RectTransform rectTransform, CanvasGroup canvasGroup, AnimationSettings settings)
    {
        rectTransform.anchoredPosition = settings.StartPosition; // 下から始まる
        canvasGroup.alpha = Constants.ALPHA_INVISIBLE; // 透明から開始
    }
    
    // ----------------------------------------------------------------------
    // アニメーションを作成して再生
    // ----------------------------------------------------------------------
    private void CreateAndPlayAnimation(RectTransform rectTransform, CanvasGroup canvasGroup, 
                                       AnimationSettings settings, GameObject instance, float duration)
    {
        float halfAnimDuration = animationDuration * Constants.ANIMATION_DURATION_RATIO;
        
        // DOTweenシーケンスを作成
        currentAnimation = DOTween.Sequence();
        
        // 出現アニメーション（下から上へ）
        AppendAppearAnimation(rectTransform, canvasGroup, settings, halfAnimDuration);
        
        // 表示時間の待機
        currentAnimation.AppendInterval(duration - animationDuration);
        
        // 消失アニメーション（上に消えていく）
        AppendDisappearAnimation(rectTransform, canvasGroup, settings, halfAnimDuration);
        
        // アニメーション完了時の処理
        SetupAnimationCompletionCallback(instance);
        
        // アニメーション開始
        currentAnimation.Play();
    }
    
    // ----------------------------------------------------------------------
    // 出現アニメーションの追加
    // ----------------------------------------------------------------------
    private void AppendAppearAnimation(RectTransform rectTransform, CanvasGroup canvasGroup, 
                                     AnimationSettings settings, float duration)
    {
        currentAnimation.Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 
                                        Constants.ALPHA_VISIBLE, duration));
        
        currentAnimation.Join(DOTween.To(() => rectTransform.anchoredPosition, 
                                      x => rectTransform.anchoredPosition = x, 
                                      settings.EndPosition, duration)
                            .SetEase(appearEase));
    }
    
    // ----------------------------------------------------------------------
    // 消失アニメーションの追加
    // ----------------------------------------------------------------------
    private void AppendDisappearAnimation(RectTransform rectTransform, CanvasGroup canvasGroup, 
                                        AnimationSettings settings, float duration)
    {
        currentAnimation.Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 
                                        Constants.ALPHA_INVISIBLE, duration));
        
        currentAnimation.Join(DOTween.To(() => rectTransform.anchoredPosition, 
                                      x => rectTransform.anchoredPosition = x, 
                                      settings.ExitPosition, duration)
                            .SetEase(disappearEase));
    }
    
    // ----------------------------------------------------------------------
    // アニメーション完了時のコールバック設定
    // ----------------------------------------------------------------------
    private void SetupAnimationCompletionCallback(GameObject instance)
    {
        currentAnimation.OnComplete(() => {
            if (instance != null)
            {
                Destroy(instance);
                currentFeedbackInstance = null;
            }
            currentAnimation = null;
        });
    }
    
    // ----------------------------------------------------------------------
    // アニメーション設定構造体
    // ----------------------------------------------------------------------
    private struct AnimationSettings
    {
        public Vector2 StartPosition;
        public Vector2 EndPosition;
        public Vector2 ExitPosition;
    }
    
    // ----------------------------------------------------------------------
    // フィードバックの位置を設定
    // ----------------------------------------------------------------------
    public void SetPosition(Vector2 offset)
    {
        try
        {
            positionOffset = offset;
            UpdateCurrentInstancePosition();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FeedbackContainer] フィードバック位置の設定中にエラーが発生しました: {ex.Message}");
            Debug.LogException(ex);
        }
    }
    
    // ----------------------------------------------------------------------
    // 現在のインスタンスの位置を更新
    // ----------------------------------------------------------------------
    private void UpdateCurrentInstancePosition()
    {
        // 既にインスタンスが存在する場合は位置を更新
        if (currentFeedbackInstance != null)
        {
            RectTransform rectTransform = currentFeedbackInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = positionOffset;
            }
        }
    }
    
    // ----------------------------------------------------------------------
    // 一定時間後にフィードバックを非表示にするコルーチン
    // ----------------------------------------------------------------------
    private IEnumerator HideFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideCurrentFeedback();
        hideCoroutine = null;
    }
    
    // ----------------------------------------------------------------------
    // 既存のフィードバックメッセージを更新する（消えずに内容を変更）
    // ----------------------------------------------------------------------
    public void UpdateFeedbackMessage(string newMessage)
    {
        try
        {
            if (currentFeedbackInstance == null)
            {
                // インスタンスがなければ新規表示
                ShowSuccessFeedback(newMessage);
                return;
            }

            UpdateFeedbackText(newMessage);
            ResetFeedbackAnimation();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FeedbackContainer] メッセージ更新中にエラーが発生しました: {ex.Message}");
            Debug.LogException(ex);
        }
    }
    
    // ----------------------------------------------------------------------
    // フィードバックテキストの更新
    // ----------------------------------------------------------------------
    private void UpdateFeedbackText(string newMessage)
    {
        // 現在表示中のフィードバックのテキストを更新
        TextMeshProUGUI feedbackText = currentFeedbackInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (feedbackText != null)
        {
            feedbackText.text = newMessage;
        }
    }
    
    // ----------------------------------------------------------------------
    // フィードバックアニメーションのリセット
    // ----------------------------------------------------------------------
    private void ResetFeedbackAnimation()
    {
        // アニメーション中なら中断
        if (currentAnimation != null)
        {
            currentAnimation.Kill(false); // 現在のアニメーションを終了（消さない）
            currentAnimation = null;
        }

        // コルーチンをリセット（非表示タイマーを延長）
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        // 新しい表示時間を設定（デフォルト時間）
        hideCoroutine = StartCoroutine(HideFeedbackAfterDelay(displayDuration));
    }
    
    // ----------------------------------------------------------------------
    // プログレスフィードバック表示（更新可能な長時間表示）
    // ----------------------------------------------------------------------
    public void ShowProgressFeedback(string message)
    {
        try
        {
            if (successPrefab == null)
            {
                Debug.LogWarning("[FeedbackContainer] 成功用プレハブが設定されていません");
                return;
            }
            
            // 既存のフィードバックを非表示
            HideCurrentFeedback();
            
            SetupProgressFeedback(message);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FeedbackContainer] プログレスフィードバック表示中にエラーが発生しました: {ex.Message}");
            Debug.LogException(ex);
        }
    }
    
    // ----------------------------------------------------------------------
    // プログレスフィードバックのセットアップ
    // ----------------------------------------------------------------------
    private void SetupProgressFeedback(string message)
    {
        currentFeedbackInstance = Instantiate(successPrefab, transform);
        
        SetupProgressRectTransform(currentFeedbackInstance);
        SetupProgressCanvasGroup(currentFeedbackInstance);
        SetupProgressText(currentFeedbackInstance, message);
        
        // フィードバックを表示（タイマーなし - UpdateFeedbackMessageで更新する）
        currentFeedbackInstance.SetActive(true);
    }
    
    // ----------------------------------------------------------------------
    // プログレスフィードバックのRectTransform設定
    // ----------------------------------------------------------------------
    private void SetupProgressRectTransform(GameObject instance)
    {
        RectTransform rectTransform = instance.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(Constants.ANCHOR_MIN_X, Constants.ANCHOR_MIN_Y);
            rectTransform.anchorMax = new Vector2(Constants.ANCHOR_MAX_X, Constants.ANCHOR_MAX_Y);
            rectTransform.pivot = new Vector2(Constants.PIVOT_X, Constants.PIVOT_Y);
            rectTransform.anchoredPosition = positionOffset;
        }
    }
    
    // ----------------------------------------------------------------------
    // プログレスフィードバックのCanvasGroup設定
    // ----------------------------------------------------------------------
    private void SetupProgressCanvasGroup(GameObject instance)
    {
        CanvasGroup canvasGroup = instance.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = instance.AddComponent<CanvasGroup>();
        }
        
        // アニメーション無しで表示
        canvasGroup.alpha = Constants.ALPHA_VISIBLE;
    }
    
    // ----------------------------------------------------------------------
    // プログレスフィードバックのテキスト設定
    // ----------------------------------------------------------------------
    private void SetupProgressText(GameObject instance, string message)
    {
        TextMeshProUGUI feedbackText = instance.GetComponentInChildren<TextMeshProUGUI>();
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }
    
    // ----------------------------------------------------------------------
    // プログレスフィードバックの完了
    // ----------------------------------------------------------------------
    public void CompleteProgressFeedback(string completeMessage = null, float duration = -1f)
    {
        try
        {
            if (currentFeedbackInstance == null) return;
            
            // 完了メッセージがあれば更新
            if (!string.IsNullOrEmpty(completeMessage))
            {
                UpdateFeedbackText(completeMessage);
            }
            
            // 表示時間が指定されていない場合はデフォルト値を使用
            float showDuration = duration > 0f ? duration : displayDuration;
            
            // 消える前に少し表示
            hideCoroutine = StartCoroutine(HideFeedbackAfterDelay(showDuration));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FeedbackContainer] プログレスフィードバック完了処理中にエラーが発生しました: {ex.Message}");
            Debug.LogException(ex);
        }
    }
}