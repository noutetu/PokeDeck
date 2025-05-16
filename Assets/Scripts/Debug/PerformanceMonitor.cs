using UnityEngine;
using TMPro;
using System.Collections;
using System.Text;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

// ----------------------------------------------------------------------
// アプリケーションのパフォーマンスをモニタリングし、UIに表示するコンポーネント
// このスクリプトは、フレームレート、メモリ使用量、カードの表示枚数などの情報を収集し、
// ユーザーインターフェースに表示します。
// 主な機能:
// - フレームレート(FPS)の計算
// - メモリ使用量の監視
// - カードの表示枚数の監視
// - メモリリークの診断
// - テクスチャやメッシュの統計情報の収集
// - 普段は使用せず、開発中にデバッグ用として使用する
// ----------------------------------------------------------------------
public class PerformanceMonitor : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI statsText;  // 統計情報を表示するテキスト
    [SerializeField] private Image backgroundPanel;     // 背景パネル（透明度調整用）

    [Header("更新間隔")]
    [SerializeField] private float updateInterval = 0.5f;  // 更新間隔（秒）

    [Header("警告閾値")]
    [SerializeField] private float lowFpsThreshold = 24f;      // FPS警告閾値
    [SerializeField] private float highMemoryThreshold = 250f;  // メモリ警告閾値（MB）
    [SerializeField] private int highCardCountThreshold = 200;  // カード枚数警告閾値

    // ----------------------------------------------------------------------
    // パフォーマンス監視用変数
    // ----------------------------------------------------------------------
    private float fps;              // フレームレート   
    private float memoryUsage;      // メモリ使用量（MB）
    private int frameCount;         // フレームカウント
    private float timeElapsed;      // 経過時間
    private AllCardPresenter cardPresenter;     // カードプレゼンターへの参照
    private int displayedCardCount;     // 表示中のカード枚数
    private int totalCardCount;         // 合計カード枚数
    private bool isLoadingCards;      // 読み込み中フラグ    
    private int gcCollections;       // GCコレクション回数

    // ----------------------------------------------------------------------
    // カラー定義
    // ----------------------------------------------------------------------
    private Color normalColor = Color.white;
    private Color warningColor = new Color(1f, 0.5f, 0f); // オレンジ
    private Color dangerColor = Color.red;

    // ----------------------------------------------------------------------
    // メモリ監視の詳細情報
    // ----------------------------------------------------------------------
    private class MemoryDetailData
    {
        public float managedMemory;        // マネージドヒープメモリ
        public float totalAllocatedMemory; // 合計割り当てメモリ
        public int textureCount;           // テクスチャ数
        public float textureMemory;        // テクスチャ推定メモリ
        public int meshCount;              // メッシュ数
        public float[] memoryHistory = new float[60]; // 1分間のメモリ履歴
        public int historyIndex = 0;
    }

    // ----------------------------------------------------------------------
    // メモリ詳細情報
    // ----------------------------------------------------------------------
    private MemoryDetailData memoryDetails = new MemoryDetailData();
    private bool showDetailedMemory = false;        // 詳細メモリ情報表示フラグ
    private float lastGCTime = 0;         // 最後のGC実行時間
    private float memoryLeakRate = 0; // MB/分

    [Header("デバッグ機能")]
    [SerializeField] private bool autoGarbageCollection = false;
    [SerializeField] private float autoGCInterval = 30f; // 秒

    // ----------------------------------------------------------------------
    // 初期化処理
    // ----------------------------------------------------------------------
    private void Start()
    {
        if (statsText == null)
        {
            Debug.LogError("統計テキスト(TextMeshProUGUI)が設定されていません");
            this.enabled = false;
            return;
        }

        // パネルの透明度調整
        if (backgroundPanel != null)
        {
            var panelColor = backgroundPanel.color;
            panelColor.a = 0.8f;
            backgroundPanel.color = panelColor;
        }

        // AllCardPresenterへの参照を取得
        FindCardPresenter();

        // 初期値設定
        fps = 0;
        frameCount = 0;
        timeElapsed = 0;
        displayedCardCount = 0;
        totalCardCount = 0;
        gcCollections = System.GC.CollectionCount(0);

        // メモリ履歴の初期化
        for (int i = 0; i < memoryDetails.memoryHistory.Length; i++)
        {
            memoryDetails.memoryHistory[i] = 0;
        }

        // 定期更新を開始
        StartCoroutine(UpdateStats());

        // 定期的なメモリ詳細収集
        InvokeRepeating("CollectMemoryDetails", 1f, 5f);

        // 自動GCが有効なら開始
        if (autoGarbageCollection)
        {
            InvokeRepeating("ForceGarbageCollection", autoGCInterval, autoGCInterval);
        }
    }

    // ----------------------------------------------------------------------
    // フレームごとの更新処理
    // ----------------------------------------------------------------------
    private void Update()
    {
        // FPS計算用
        timeElapsed += Time.deltaTime;
        frameCount++;
    }

    // ----------------------------------------------------------------------
    // AllCardPresenterへの参照を探して取得
    // ----------------------------------------------------------------------
    private void FindCardPresenter()
    {
        var cardBoot = FindObjectOfType<CardUIManager>();
        if (cardBoot != null)
        {
            // リフレクションを使ってpresenterフィールドにアクセス
            System.Reflection.FieldInfo fieldInfo =
                typeof(CardUIManager).GetField("presenter",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (fieldInfo != null)
            {
                cardPresenter = fieldInfo.GetValue(cardBoot) as AllCardPresenter;
            }
        }
    }

    // ----------------------------------------------------------------------
    // 統計情報を定期的に更新
    // ----------------------------------------------------------------------
    private IEnumerator UpdateStats()
    {
        while (true)
        {
            // 更新間隔を待機
            yield return new WaitForSeconds(updateInterval);

            // FPSを計算
            if (timeElapsed > 0)
            {
                fps = frameCount / timeElapsed;
                frameCount = 0;
                timeElapsed = 0;
            }

            // メモリ使用量を取得 (MB単位)
            memoryUsage = (float)System.GC.GetTotalMemory(false) / (1024 * 1024);

            // メモリ履歴を更新
            memoryDetails.memoryHistory[memoryDetails.historyIndex] = memoryUsage;
            memoryDetails.historyIndex = (memoryDetails.historyIndex + 1) % memoryDetails.memoryHistory.Length;

            // メモリリーク率を計算（60秒間の増加率）
            CalculateMemoryLeakRate();

            // GCの回数を取得
            int currentGC = System.GC.CollectionCount(0);
            int newGCCollections = currentGC - gcCollections;
            gcCollections = currentGC;

            // カード情報を取得
            UpdateCardStats();

            // テキスト生成と表示
            UpdateUIText(newGCCollections);
        }
    }

    // ----------------------------------------------------------------------
    // メモリリーク率を計算（分あたりのMB増加）
    // ----------------------------------------------------------------------
    private void CalculateMemoryLeakRate()
    {
        // 十分なデータがない場合はスキップ
        if (Time.time < 60f) return;

        // 履歴からデータを取得
        float oldestValue = memoryDetails.memoryHistory[(memoryDetails.historyIndex + 1) % memoryDetails.memoryHistory.Length];
        if (oldestValue <= 0) return; // 有効なデータがない

        // リーク率計算（MB/分）
        memoryLeakRate = (memoryUsage - oldestValue) * (60f / memoryDetails.memoryHistory.Length);
    }

    // ----------------------------------------------------------------------
    // メモリ詳細情報を収集
    // ----------------------------------------------------------------------
    private void CollectMemoryDetails()
    {
        // マネージドメモリ
        memoryDetails.managedMemory = (float)System.GC.GetTotalMemory(false) / (1024 * 1024);

        // テクスチャとメッシュのカウント（サンプリング頻度を抑える）
        if (Time.frameCount % 30 == 0)
        {
            CollectResourceStats();
        }
    }

    // ----------------------------------------------------------------------
    // テクスチャなどのリソース統計情報を収集
    // ----------------------------------------------------------------------
    private void CollectResourceStats()
    {
        memoryDetails.textureCount = 0;
        memoryDetails.textureMemory = 0;
        memoryDetails.meshCount = 0;

        // ImageCacheManagerのテクスチャ数を取得
        var imageCacheManager = FindObjectOfType<ImageCacheManager>();
        if (imageCacheManager != null)
        {
            // リフレクションでテクスチャキャッシュにアクセス
            System.Reflection.FieldInfo cacheField =
                typeof(ImageCacheManager).GetField("textureCache",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (cacheField != null)
            {
                var cache = cacheField.GetValue(imageCacheManager) as Dictionary<string, Texture2D>;
                if (cache != null)
                {
                    memoryDetails.textureCount = cache.Count;

                    // テクスチャメモリの推定（各テクスチャのサイズを加算）
                    foreach (var tex in cache.Values)
                    {
                        if (tex != null)
                        {
                            // 推定サイズ計算 (幅×高さ×4バイト÷圧縮率)
                            float texMemory = (tex.width * tex.height * 4f) / (1024f * 1024f); // MB単位

                            // 圧縮テクスチャは少なくサイズを見積もる
                            if (tex.format != TextureFormat.RGBA32)
                            {
                                texMemory *= 0.5f;
                            }

                            memoryDetails.textureMemory += texMemory;
                        }
                    }
                }
            }
        }

        // メッシュカウント（重い処理のため頻度を下げる）
        if (Time.frameCount % 300 == 0) // 300フレームごと
        {
            memoryDetails.meshCount = Resources.FindObjectsOfTypeAll<Mesh>().Length;
        }
    }

    // ----------------------------------------------------------------------
    // カードの統計情報を更新（仮想スクロール対応版）
    // ----------------------------------------------------------------------
    private void UpdateCardStats()
    {
        try
        {
            // 初期値設定
            int actualDisplayedCount = 0;
            int dataModelCardCount = 0;

            // カードプレゼンターから検索/フィルタ後の総カード数を取得
            if (cardPresenter != null)
            {
                // データモデル上の表示対象となるカード総数
                dataModelCardCount = cardPresenter.DisplayedCards?.Count ?? 0;

                // 実際に画面表示されているカード数を取得
                var virtualScroll = FindObjectOfType<SimpleVirtualScroll>();
                if (virtualScroll != null)
                {
                    // リフレクションでアクティブカードの辞書を取得
                    System.Reflection.FieldInfo activeCardsField =
                        typeof(SimpleVirtualScroll).GetField("activeCards",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (activeCardsField != null)
                    {
                        var activeCards = activeCardsField.GetValue(virtualScroll) as Dictionary<int, RectTransform>;
                        actualDisplayedCount = activeCards?.Count ?? 0;
                    }
                }

                // 仮想スクロールから取得できない場合はアクティブなCardViewコンポーネント数をカウント
                if (actualDisplayedCount == 0)
                {
                    // シーン内のアクティブなカードビューを検索
                    actualDisplayedCount = FindObjectsOfType<CardView>(true)
                        .Count(cv => cv.gameObject.activeInHierarchy);
                }

                // カードUIManagerからロード状態を取得
                var cardBoot = FindObjectOfType<CardUIManager>();
                if (cardBoot != null)
                {
                    // remainingCardsフィールドにアクセス
                    System.Reflection.FieldInfo fieldInfo =
                        typeof(CardUIManager).GetField("remainingCards",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (fieldInfo != null)
                    {
                        var remainingCards = fieldInfo.GetValue(cardBoot) as List<CardModel>;
                        int remainingCount = remainingCards?.Count ?? 0;

                        // 残りのカード数も含めて計算
                        totalCardCount = dataModelCardCount;
                    }

                    // isLoadingBatchフィールドにアクセス
                    fieldInfo = typeof(CardUIManager).GetField("isLoadingBatch",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (fieldInfo != null)
                    {
                        isLoadingCards = (bool)fieldInfo.GetValue(cardBoot);
                    }
                }

                // 実際に表示されているカード数を更新
                displayedCardCount = actualDisplayedCount;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"カード統計情報の更新でエラーが発生: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    // UIテキストを更新
    // ----------------------------------------------------------------------
    private void UpdateUIText(int newGCCollections)
    {
        // 文字列ビルダーでテキスト構築
        StringBuilder builder = new StringBuilder();

        // FPS表示（閾値に応じて色を変更）
        AppendColoredText(builder, "FPS: ", normalColor);
        if (fps < lowFpsThreshold)
            AppendColoredText(builder, $"{fps:F1}\n", dangerColor);
        else
            AppendColoredText(builder, $"{fps:F1}\n", normalColor);

        // メモリ使用量表示
        AppendColoredText(builder, "メモリ: ", normalColor);
        if (memoryUsage > highMemoryThreshold)
            AppendColoredText(builder, $"{memoryUsage:F1} MB", dangerColor);
        else
            AppendColoredText(builder, $"{memoryUsage:F1} MB", normalColor);

        // メモリ増加率（リーク監視）
        if (Mathf.Abs(memoryLeakRate) > 1f) // 1MB/分以上の変動
        {
            Color leakColor = memoryLeakRate > 5f ? dangerColor : warningColor;
            AppendColoredText(builder, $" ({memoryLeakRate:+0.0;-0.0} MB/分)\n", leakColor);
        }
        else
        {
            builder.Append("\n");
        }

        // 詳細メモリ情報（フラグが立っている場合）
        if (showDetailedMemory)
        {
            AppendColoredText(builder, "【メモリ詳細】\n", warningColor);

            // テクスチャ情報
            Color texColor = memoryDetails.textureCount > 300 ? dangerColor :
                            (memoryDetails.textureCount > 150 ? warningColor : normalColor);
            AppendColoredText(builder, $"テクスチャ: {memoryDetails.textureCount}個 ", texColor);
            AppendColoredText(builder, $"(約{memoryDetails.textureMemory:F1}MB)\n", texColor);

            // メッシュ情報
            AppendColoredText(builder, $"メッシュ: {memoryDetails.meshCount}個\n", normalColor);

            // GC情報
            AppendColoredText(builder, $"GC総回数: {System.GC.CollectionCount(0)}回\n", normalColor);
        }
        else
        {
            // GC回数表示
            AppendColoredText(builder, "GC発生: ", normalColor);
            if (newGCCollections > 0)
                AppendColoredText(builder, $"{newGCCollections}回\n", warningColor);
            else
                AppendColoredText(builder, "なし\n", normalColor);
        }

        // カード表示枚数（実際に描画されているカード数と総カード数）
        AppendColoredText(builder, "実際表示: ", normalColor);
        if (displayedCardCount > highCardCountThreshold)
            AppendColoredText(builder, $"{displayedCardCount}枚", dangerColor);
        else
            AppendColoredText(builder, $"{displayedCardCount}枚", normalColor);

        // 仮想スクロールの効率性
        float virtualScrollEfficiency = totalCardCount > 0 ?
            100f - ((float)displayedCardCount * 100f / totalCardCount) : 0;

        AppendColoredText(builder, " / 総カード: ", normalColor);
        AppendColoredText(builder, $"{totalCardCount}枚", normalColor);

        // 仮想スクロールの効率を色分け
        Color effColor = virtualScrollEfficiency > 90 ? new Color(0, 0.8f, 0) : // 緑色
                        (virtualScrollEfficiency > 70 ? normalColor : warningColor);
        AppendColoredText(builder, $" (効率: {virtualScrollEfficiency:F0}%)\n", effColor);

        // 読み込み状態表示
        AppendColoredText(builder, "読込状態: ", normalColor);
        if (isLoadingCards)
            AppendColoredText(builder, "読込中\n", warningColor);
        else
            AppendColoredText(builder, "待機中\n", normalColor);

        // OS情報
        AppendColoredText(builder, $"デバイス: {SystemInfo.deviceModel}\n", normalColor);
        AppendColoredText(builder, $"OS: {SystemInfo.operatingSystem}\n", normalColor);

        // テキスト設定
        statsText.text = builder.ToString();
    }

    // ----------------------------------------------------------------------
    // 色付きテキストを追加
    // ----------------------------------------------------------------------
    private void AppendColoredText(StringBuilder builder, string text, Color color)
    {
        // TMProの色タグを使用
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        builder.Append($"<color=#{colorHex}>{text}</color>");
    }

    // ----------------------------------------------------------------------
    // デバッグ情報の表示・非表示を切り替え
    // ----------------------------------------------------------------------
    public void ToggleDisplay()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    // ----------------------------------------------------------------------
    // 詳細メモリ情報の表示/非表示を切り替え
    // ----------------------------------------------------------------------
    public void ToggleDetailedMemory()
    {
        showDetailedMemory = !showDetailedMemory;
    }

    // ----------------------------------------------------------------------
    // 強制的にガベージコレクションを実行
    // ----------------------------------------------------------------------
    public void ForceGarbageCollection()
    {
        if (Time.realtimeSinceStartup - lastGCTime < 5f)
            return; // 短時間での連続実行を防止

        Debug.Log("強制GC実行: リソース解放を試みます");
        System.GC.Collect();
        Resources.UnloadUnusedAssets();
        lastGCTime = Time.realtimeSinceStartup;
    }

    // ----------------------------------------------------------------------
    // メモリリークの診断を実行
    // ----------------------------------------------------------------------
    public void DiagnoseMemoryLeak()
    {
        StartCoroutine(RunMemoryLeakDiagnosis());
    }

    // ----------------------------------------------------------------------
    // メモリリーク診断コルーチン
    // ----------------------------------------------------------------------
    private IEnumerator RunMemoryLeakDiagnosis()
    {
        Debug.Log("=== メモリリーク診断開始 ===");
        Debug.Log($"診断前メモリ使用量: {memoryUsage:F1}MB");

        // 強制GC実行
        ForceGarbageCollection();
        yield return new WaitForSeconds(0.5f);

        // GC後のメモリ測定
        float afterGCMemory = (float)System.GC.GetTotalMemory(false) / (1024 * 1024);
        Debug.Log($"GC後のメモリ使用量: {afterGCMemory:F1}MB (解放量: {memoryUsage - afterGCMemory:F1}MB)");

        // テクスチャ詳細情報
        yield return StartCoroutine(DiagnoseTextures());

        // メモリ監視を30秒間実行
        Debug.Log("30秒間のメモリ使用量監視を開始...");
        float startMemory = afterGCMemory;
        yield return new WaitForSeconds(30f);

        // 最終測定
        memoryUsage = (float)System.GC.GetTotalMemory(false) / (1024 * 1024);
        float leakRate = (memoryUsage - startMemory) * 2f; // MB/分
        Debug.Log($"30秒後のメモリ使用量: {memoryUsage:F1}MB (増加率: {leakRate:F1}MB/分)");

        // リーク判定
        if (leakRate > 5f)
        {
            Debug.LogWarning($"警告: メモリリークの可能性があります。増加率: {leakRate:F1}MB/分");
        }
        else if (leakRate > 0)
        {
            Debug.Log($"軽度のメモリ増加があります: {leakRate:F1}MB/分");
        }
        else
        {
            Debug.Log("メモリリークは検出されませんでした。");
        }

        Debug.Log("=== メモリリーク診断完了 ===");
    }

    // ----------------------------------------------------------------------
    // テクスチャの詳細診断
    // ----------------------------------------------------------------------
    private IEnumerator DiagnoseTextures()
    {
        Debug.Log("テクスチャ詳細診断を実行中...");

        var imageCacheManager = FindObjectOfType<ImageCacheManager>();
        if (imageCacheManager != null)
        {
            // リフレクションでテクスチャキャッシュにアクセス
            System.Reflection.FieldInfo cacheField =
                typeof(ImageCacheManager).GetField("textureCache",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (cacheField != null)
            {
                var cache = cacheField.GetValue(imageCacheManager) as Dictionary<string, Texture2D>;
                if (cache != null)
                {
                    Debug.Log($"キャッシュ内テクスチャ数: {cache.Count}");

                    // 大きなテクスチャをリストアップ
                    int largeTexCount = 0;
                    float totalLargeTexMemory = 0;

                    foreach (var entry in cache.Take(5)) // 最初の5つだけ詳細表示
                    {
                        var tex = entry.Value;
                        if (tex != null)
                        {
                            float texMem = (tex.width * tex.height * 4f) / (1024f * 1024f);
                            Debug.Log($"テクスチャ: {entry.Key}, サイズ: {tex.width}x{tex.height}, 推定メモリ: {texMem:F2}MB");
                        }
                    }

                    // 大きなテクスチャ（1MB以上）をカウント
                    foreach (var tex in cache.Values)
                    {
                        if (tex != null && tex.width * tex.height > 262144) // 512x512以上
                        {
                            float texMem = (tex.width * tex.height * 4f) / (1024f * 1024f);
                            totalLargeTexMemory += texMem;
                            largeTexCount++;
                        }
                    }

                    Debug.Log($"大きなテクスチャ(>1MB): {largeTexCount}個、合計サイズ: {totalLargeTexMemory:F1}MB");
                }
            }
        }

        yield return null;
    }
}