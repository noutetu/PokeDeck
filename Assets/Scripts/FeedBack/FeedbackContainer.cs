using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening; // DOTween追加

// ----------------------------------------------------------------------
// フィードバック表示を管理するコンテナクラス
// 画面上部に固定表示されるフィードバックUIを管理する
// ----------------------------------------------------------------------
public class FeedbackContainer : MonoBehaviour
{
    [Header("フィードバック設定")]
    [SerializeField] private GameObject successPrefab; // 成功時のフィードバック用プレハブ
    [SerializeField] private GameObject failurePrefab; // 失敗時のフィードバック用プレハブ
    [SerializeField] private float displayDuration = 1.5f; // 表示時間（秒）
    [SerializeField] private Vector2 positionOffset = new Vector2(0f, -100f); // 位置オフセット（画面上部からの距離）
    
    [Header("アニメーション設定")]
    [SerializeField] private bool useAnimation = true; // アニメーションを使用するかどうか
    [SerializeField] private float animationDuration = 0.5f; // アニメーション時間（秒）
    [SerializeField] private float floatDistance = 30f; // 浮き上がる距離（ピクセル）
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
        // 表示時間が指定されていない場合はデフォルト値を使用
        float showDuration = duration > 0f ? duration : displayDuration;
        
        // 既存のフィードバックを非表示
        HideCurrentFeedback();
        
        // 成功用のプレハブがある場合はそれを使用
        if (successPrefab != null)
        {
            currentFeedbackInstance = Instantiate(successPrefab, transform);
            SetupFeedbackInstance(currentFeedbackInstance, message, showDuration);
        }
    }
    
    // ----------------------------------------------------------------------
    // 失敗フィードバックメッセージを表示（時間指定可）
    // ----------------------------------------------------------------------
    public void ShowFailureFeedback(string message, float duration = -1f)
    {
        // 表示時間が指定されていない場合はデフォルト値を使用
        float showDuration = duration > 0f ? duration : displayDuration;
        
        // 既存のフィードバックを非表示
        HideCurrentFeedback();
        
        // 失敗用のプレハブがある場合はそれを使用
        if (failurePrefab != null)
        {
            currentFeedbackInstance = Instantiate(failurePrefab, transform);
            SetupFeedbackInstance(currentFeedbackInstance, message, showDuration);
        }
    }
    
    // ----------------------------------------------------------------------
    // フィードバックインスタンスの初期設定
    // ----------------------------------------------------------------------
    private void SetupFeedbackInstance(GameObject instance, string message, float duration)
    {
        if (instance == null) return;
        
        // 位置を調整
        RectTransform rectTransform = instance.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // アンカーを画面上部中央に設定
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            
            // オフセット位置を設定（画面上部からの距離）
            rectTransform.anchoredPosition = positionOffset;
        }
        
        // CanvasGroupの追加（アニメーション用）
        if (instance.GetComponent<CanvasGroup>() == null)
        {
            instance.AddComponent<CanvasGroup>();
        }
        
        // テキストコンポーネントを取得して更新
        TextMeshProUGUI feedbackText = instance.GetComponentInChildren<TextMeshProUGUI>();
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
        
        // フィードバックを表示してアニメーション開始
        instance.SetActive(true);
        
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
        // 実行中のアニメーションがあればキャンセル
        if (currentAnimation != null)
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
        
        // 既に実行中のコルーチンがあればキャンセル
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        
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
        RectTransform rectTransform = instance.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = instance.GetComponent<CanvasGroup>();
        
        if (rectTransform == null || canvasGroup == null) return;
        
        // アニメーション用の初期設定
        Vector2 startPos = positionOffset + new Vector2(0, floatDistance); // 下から登場するように変更
        Vector2 endPos = positionOffset; // 最終位置
        Vector2 exitPos = positionOffset - new Vector2(0, floatDistance); // 上に消えていく位置
        
        // アニメーションの初期状態を設定
        rectTransform.anchoredPosition = startPos; // 下から始まる
        canvasGroup.alpha = 0f;
        
        // DOTweenシーケンスを作成
        currentAnimation = DOTween.Sequence();
        
        // 出現アニメーション（下から上へ）
        currentAnimation.Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, animationDuration * 0.5f));
        currentAnimation.Join(DOTween.To(() => rectTransform.anchoredPosition, 
                                         x => rectTransform.anchoredPosition = x, 
                                         endPos, animationDuration * 0.5f)
                                    .SetEase(appearEase));
        
        // 表示時間の待機
        currentAnimation.AppendInterval(duration - animationDuration);
        
        // 消失アニメーション（上に消えていく）
        currentAnimation.Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, animationDuration * 0.5f));
        currentAnimation.Join(DOTween.To(() => rectTransform.anchoredPosition, 
                                         x => rectTransform.anchoredPosition = x, 
                                         exitPos, animationDuration * 0.5f)
                                    .SetEase(disappearEase));
        
        // アニメーション完了時の処理
        currentAnimation.OnComplete(() => {
            if (instance != null)
            {
                Destroy(instance);
                currentFeedbackInstance = null;
            }
            currentAnimation = null;
        });
        
        // アニメーション開始
        currentAnimation.Play();
    }
    
    // ----------------------------------------------------------------------
    // フィードバックの位置を設定
    // ----------------------------------------------------------------------
    public void SetPosition(Vector2 offset)
    {
        positionOffset = offset;
        
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
    // インスペクターでの値変更に対応
    // ----------------------------------------------------------------------
    private void OnValidate()
    {
        // 実行中にインスペクターで値が変更された場合、位置を更新
        if (Application.isPlaying && currentFeedbackInstance != null)
        {
            RectTransform rectTransform = currentFeedbackInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = positionOffset;
            }
        }
    }

    // ----------------------------------------------------------------------
    // 既存のフィードバックメッセージを更新する（消えずに内容を変更）
    // ----------------------------------------------------------------------
    public void UpdateFeedbackMessage(string newMessage)
    {
        if (currentFeedbackInstance == null) 
        {
            // インスタンスがなければ新規表示
            ShowSuccessFeedback(newMessage);
            return;
        }
        
        // 現在表示中のフィードバックのテキストを更新
        TextMeshProUGUI feedbackText = currentFeedbackInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (feedbackText != null)
        {
            feedbackText.text = newMessage;
        }
        
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
        // 既存のフィードバックを非表示
        HideCurrentFeedback();
        
        // 成功用のプレハブがある場合はそれを使用
        if (successPrefab != null)
        {
            currentFeedbackInstance = Instantiate(successPrefab, transform);
            
            // 位置を調整
            RectTransform rectTransform = currentFeedbackInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 1f);
                rectTransform.anchorMax = new Vector2(0.5f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.anchoredPosition = positionOffset;
            }
            
            // CanvasGroupの追加（アニメーション用）
            CanvasGroup canvasGroup = currentFeedbackInstance.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = currentFeedbackInstance.AddComponent<CanvasGroup>();
            }
            
            // アニメーション無しで表示
            canvasGroup.alpha = 1f;
            
            // テキストコンポーネントを取得して更新
            TextMeshProUGUI feedbackText = currentFeedbackInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (feedbackText != null)
            {
                feedbackText.text = message;
            }
            
            // フィードバックを表示（タイマーなし - UpdateFeedbackMessageで更新する）
            currentFeedbackInstance.SetActive(true);
        }
    }
    
    // ----------------------------------------------------------------------
    // プログレスフィードバックの完了
    // ----------------------------------------------------------------------
    public void CompleteProgressFeedback(string completeMessage = null, float duration = -1f)
    {
        if (currentFeedbackInstance != null)
        {
            // 完了メッセージがあれば更新
            if (!string.IsNullOrEmpty(completeMessage))
            {
                TextMeshProUGUI feedbackText = currentFeedbackInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (feedbackText != null)
                {
                    feedbackText.text = completeMessage;
                }
            }
            
            // 表示時間が指定されていない場合はデフォルト値を使用
            float showDuration = duration > 0f ? duration : displayDuration;
            
            // 消える前に少し表示
            hideCoroutine = StartCoroutine(HideFeedbackAfterDelay(showDuration));
        }
    }
}