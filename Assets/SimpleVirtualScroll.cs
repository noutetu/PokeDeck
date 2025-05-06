using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// シンプルな仮想スクロール実装
/// </summary>
public class SimpleVirtualScroll : MonoBehaviour
{
    [Header("基本設定")]
    [SerializeField] private ScrollRect scrollRect;   // UIのScrollRect
    [SerializeField] private RectTransform content;   // スクロール内のコンテンツ領域
    [SerializeField] private CardView cardPrefab;     // カードのプレハブ
    
    [Header("パフォーマンス設定")]
    [SerializeField] private int poolSize = 20;       // 再利用するカード数
    [SerializeField] private float bufferZone = 50f;  // 事前読み込み範囲
    
    // GridLayout設定用の変数を追加
    [Header("GridLayout設定")]
    [SerializeField] private float paddingLeft = 25f;
    [SerializeField] private float paddingTop = 20f;
    [SerializeField] private float cellWidth = 440f;
    [SerializeField] private float cellHeight = 660f;
    [SerializeField] private float spacingX = 25f;
    [SerializeField] private float spacingY = 80f;
    [SerializeField] private int columnsCount = 2;
    
    // カードデータ
    private List<CardModel> allCards = new List<CardModel>();
    
    // プールされたカードビュー
    private List<CardView> cardPool = new List<CardView>();
    
    // カード表示位置の管理
    private float cardHeight;                       // 1枚のカードの高さ
    private float viewportHeight;                   // 表示領域の高さ
    private Dictionary<int, RectTransform> activeCards = new Dictionary<int, RectTransform>();
    
    // GridLayout設定を反映するメソッド
    private void SetGridSettings(float paddingLeft, float paddingTop, float cellWidth, float cellHeight, 
                               float spacingX, float spacingY, int columnsCount)
    {
        this.paddingLeft = paddingLeft;
        this.paddingTop = paddingTop;
        this.cellWidth = cellWidth;
        this.cellHeight = cellHeight;
        this.spacingX = spacingX;
        this.spacingY = spacingY;
        this.columnsCount = columnsCount;
        
        // カード高さはセルの高さとして設定（スペーシングは別途計算）
        this.cardHeight = cellHeight;
        
        Debug.Log($"グリッド設定を適用: セルサイズ={cellWidth}x{cellHeight}, スペーシング={spacingX}x{spacingY}, 列数={columnsCount}");
    }
    
    private void Start()
    {
        if (scrollRect == null || content == null || cardPrefab == null)
        {
            Debug.LogError("SimpleVirtualScroll: 必要なコンポーネントが設定されていません");
            return;
        }
        
        // GridLayoutGroupを無効化
        GridLayoutGroup gridLayout = content.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            // 設定を記録してから無効化
            Debug.LogWarning("SimpleVirtualScrollと競合するGridLayoutGroupを無効化します");
            
            // GridLayoutGroupの設定を反映
            float paddingLeft = gridLayout.padding.left;
            float paddingTop = gridLayout.padding.top;
            float cellWidth = gridLayout.cellSize.x;
            float cellHeight = gridLayout.cellSize.y;
            float spacingX = gridLayout.spacing.x;
            float spacingY = gridLayout.spacing.y;
            int columnsCount = 2; // FixedColumnCount制約の場合
            
            if (gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                columnsCount = gridLayout.constraintCount;
            }
            
            // この情報をSimpleVirtualScrollの設計に組み込む
            SetGridSettings(paddingLeft, paddingTop, cellWidth, cellHeight, spacingX, spacingY, columnsCount);
            
            // GridLayoutGroupを無効化
            gridLayout.enabled = false;
        }
        else
        {
            // GridLayoutGroupがない場合はデフォルト値を使用
            SetGridSettings(25f, 20f, 440f, 660f, 25f, 80f, 2);
        }
        
        // ContentSizeFitterがあれば無効化
        ContentSizeFitter sizeFitter = content.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null)
        {
            Debug.LogWarning("SimpleVirtualScrollと競合するContentSizeFitterを無効化します");
            sizeFitter.enabled = false;
        }
        
        // コンテンツのアンカー設定を上部固定に変更
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1); // 上部中央を基準に

        // ======================================================
        // jfaksjfaskfjkasfjkajfa;lf
        // ======================================================
        // スクロール位置をリセットして、一番上から表示
        content.anchoredPosition = Vector2.zero;
        scrollRect.normalizedPosition = new Vector2(0, 1);
        
        Debug.Log("コンテンツのアンカー設定を上部固定に変更しました");
        
        // カードの高さは既にSetGridSettings内で設定済み
        
        // 表示領域の高さを取得
        viewportHeight = scrollRect.viewport.rect.height;
        
        // スクロールイベントを登録
        scrollRect.onValueChanged.AddListener(OnScroll);
        
        // 起動時に既存のカードを全て削除して確実にクリーンな状態で開始
        foreach (Transform child in content)
        {
            if (child.GetComponent<CardView>() != null)
            {
                Destroy(child.gameObject);
            }
        }
        
        // カードプールを初期化
        InitializeCardPool();
        
        // デバッグ情報を定期的にログ出力
        StartCoroutine(DebugPoolStatus());
    }
    
    /// <summary>
    /// カードプールの初期化
    /// </summary>
    private void InitializeCardPool()
    {
        // 既存のカードをクリア
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        
        cardPool.Clear();
        
        // 新しいカードをプールに追加
        for (int i = 0; i < poolSize; i++)
        {
            CardView card = Instantiate(cardPrefab, content);
            card.gameObject.SetActive(false);
            cardPool.Add(card);
        }
    }
    
    /// <summary>
    /// 全カードデータを設定
    /// </summary>
    public void SetCards(List<CardModel> cards)
    {
        // データを保存する前に、既存のアクティブカードを全て非アクティブに
        foreach (var kvp in activeCards)
        {
            if (kvp.Value != null && kvp.Value.gameObject)
            {
                kvp.Value.gameObject.SetActive(false);
            }
        }
        
        // アクティブカードを完全にクリア
        activeCards.Clear();
        
        // データを保存
        allCards = cards;
        
        // 行数を計算
        int rowCount = Mathf.CeilToInt((float)cards.Count / columnsCount);
        
        // コンテンツの高さを計算 - 完全に書き直し
        float contentHeight = 0;
        
        // デバッグ出力で高さ計算の要素を確認
        Debug.Log($"高さ計算要素: paddingTop={paddingTop}, cellHeight={cellHeight}, " +
                  $"spacingY={spacingY}, rowCount={rowCount}");
              
        // 単純な計算式に変更
        contentHeight = paddingTop + (rowCount * cellHeight) + ((rowCount - 1) * spacingY);
        
        // 最低でもビューポートの高さ以上にする（スクロールを可能にするため）
        contentHeight = Mathf.Max(contentHeight, viewportHeight + 1);
        
        // コンテンツのサイズを調整
        content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
        
        // アンカーを上に固定（Start()で設定済みだが念のため）
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        
        // スクロール位置をリセットして、一番上から表示
        // content.anchoredPosition = Vector2.zero;
        // scrollRect.normalizedPosition = new Vector2(0, 1);
        
        Debug.Log($"【修正版】コンテンツサイズを設定: 幅={content.sizeDelta.x}, 高さ={contentHeight} " +
                  $"(カード数: {cards.Count}, 行数: {rowCount}, 列数: {columnsCount})");
        
        // 初期表示を更新
        UpdateVisibleCards();
    }
    
    /// <summary>
    /// スクロール時の処理
    /// </summary>
    private void OnScroll(Vector2 normalizedPosition)
    {
        // スクロール位置のデバッグ出力
        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"スクロール情報: 位置Y={content.anchoredPosition.y}, " +
                      $"コンテンツ高さ={content.sizeDelta.y}, ビューポート高さ={viewportHeight}");
        }
        
        UpdateVisibleCards();
    }
    
    /// <summary>
    /// 表示されているカードを更新
    /// </summary>
    private void UpdateVisibleCards()
    {
        if (allCards == null || allCards.Count == 0)
            return;
            
        // 現在のスクロール位置
        float scrollPosition = content.anchoredPosition.y;
        
        // 表示範囲の先頭と末尾の行インデックスを計算
        int startRow = Mathf.FloorToInt(scrollPosition / (cellHeight + spacingY));
        int endRow = Mathf.CeilToInt((scrollPosition + viewportHeight) / (cellHeight + spacingY));
        
        // バッファを追加（スクロールの先読み）
        startRow = Mathf.Max(0, startRow - 1); // 1行分のバッファ
        endRow = Mathf.Min(Mathf.CeilToInt((float)allCards.Count / columnsCount), endRow + 1); // 1行分のバッファ
        
        // 行と列からインデックス範囲を計算
        int startIndex = startRow * columnsCount;
        int endIndex = Mathf.Min(allCards.Count - 1, (endRow + 1) * columnsCount - 1);
        
        // 表示範囲外のカードを非アクティブに
        List<int> removeIndices = new List<int>();
        foreach (var kvp in activeCards)
        {
            // RectTransformが無効になっていないか確認
            if (kvp.Value == null || !kvp.Value.gameObject)
            {
                // すでに破棄されている場合はリストに追加
                removeIndices.Add(kvp.Key);
                continue;
            }

            if (kvp.Key < startIndex || kvp.Key > endIndex)
            {
                try
                {
                    // プールに戻す
                    kvp.Value.gameObject.SetActive(false);
                    RemoveFromActiveCards(kvp.Key);
                    removeIndices.Add(kvp.Key);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"カードの非アクティブ化でエラー発生: {e.Message}");
                    removeIndices.Add(kvp.Key);
                }
            }
        }
        
        // 削除対象のインデックスを実際に削除
        foreach (int index in removeIndices)
        {
            activeCards.Remove(index);
        }
        
        // 表示範囲内のカードを表示
        for (int i = startIndex; i <= endIndex; i++)
        {
            if (i < 0 || i >= allCards.Count)
                continue;
                
            if (!activeCards.ContainsKey(i))
            {
                try
                {
                    // プールから取得してデータを設定
                    CardView card = GetCardFromPool();
                    if (card != null)
                    {
                        RectTransform rectTransform = card.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            // 行と列を計算
                            int row = i / columnsCount;
                            int col = i % columnsCount;
                            
                            // グリッドレイアウトに合わせた位置を設定
                            float xPos = paddingLeft + col * (cellWidth + spacingX);
                            float yPos = paddingTop + row * (cellHeight + spacingY);
                            
                            // カードのRectTransformの設定
                            rectTransform.anchorMin = new Vector2(0, 1);
                            rectTransform.anchorMax = new Vector2(0, 1);
                            rectTransform.pivot = new Vector2(0, 1); // 左上を基準点に
                            
                            // 位置を設定（Unityの座標系に合わせて調整）
                            rectTransform.anchoredPosition = new Vector2(xPos, -yPos);
                            
                            // サイズを設定
                            rectTransform.sizeDelta = new Vector2(cellWidth, cellHeight);
                            
                            // データを設定（Setupメソッドを使用）
                            card.Setup(allCards[i]);
                            
                            // アクティブにする
                            card.gameObject.SetActive(true);
                            
                            // アクティブリストに追加
                            activeCards.Add(i, rectTransform);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"カード表示でエラー発生: {e.Message}");
                }
            }
        }
        
        // デバッグ情報のログ頻度を減らす（重要な変更の時だけ）
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"表示カード数: {activeCards.Count}, 範囲: {startIndex}-{endIndex}, 行: {startRow}-{endRow}");
        }
    }
    
    /// <summary>
    /// プールからカードを取得
    /// </summary>
    private CardView GetCardFromPool()
    {
        // プールを最適化するためにまず不要なカードを消す
        if (cardPool.Count > poolSize * 2)
        {
            Debug.LogWarning($"プールが大きすぎます({cardPool.Count})。クリーンアップを実行します。");
            
            // 非アクティブなカードを削除して適切なサイズに戻す
            int removeCount = 0;
            for (int i = cardPool.Count - 1; i >= poolSize; i--)
            {
                if (i < cardPool.Count && cardPool[i] != null && !cardPool[i].gameObject.activeInHierarchy)
                {
                    Destroy(cardPool[i].gameObject);
                    cardPool.RemoveAt(i);
                    removeCount++;
                    
                    if (cardPool.Count <= poolSize)
                        break;
                }
            }
            
            Debug.Log($"プールクリーンアップ完了: {removeCount}枚のカードを削除しました");
        }

        // 非アクティブなカードを探す
        for (int i = 0; i < cardPool.Count; i++)
        {
            var card = cardPool[i];
            // nullチェックと破棄済みチェックを追加
            if (card == null || !card.gameObject)
            {
                cardPool.RemoveAt(i);
                i--;
                continue;
            }
            
            if (!card.gameObject.activeInHierarchy)
            {
                return card;
            }
        }
        
        // クリーンアップ - 破棄済みの参照を削除
        cardPool.RemoveAll(card => card == null || !card.gameObject);
        
        // プールの情報を詳細出力（問題の診断用）
        int activeCount = 0;
        foreach (var card in cardPool)
        {
            if (card.gameObject.activeInHierarchy)
                activeCount++;
        }
        
        // 全て使用中の場合は新しく作成
        Debug.LogWarning($"SimpleVirtualScroll: プールサイズ({poolSize})が不足しています。" + 
                         $"現在のプール数:{cardPool.Count}, 使用中:{activeCount}, " + 
                         $"表示範囲内のカード数:{activeCards.Count}, 全カード数:{allCards.Count}");
        
        // 動的にプールを拡張（警告は出すが機能は維持する）
        CardView newCard = Instantiate(cardPrefab, content);
        cardPool.Add(newCard);
        return newCard;
    }
    
    /// <summary>
    /// アクティブカードからの削除ヘルパー
    /// </summary>
    private void RemoveFromActiveCards(int index)
    {
        // このメソッドは将来的に拡張可能
    }
    
    /// <summary>
    /// コンポーネント破棄時の処理
    /// </summary>
    private void OnDestroy()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScroll);
        }
    }
    
    /// <summary>
    /// デバッグ用：現在表示中のカード数と全カード数を表示
    /// </summary>
    public string GetDebugInfo()
    {
        return $"表示中: {activeCards.Count}/{allCards.Count} (プール: {poolSize})";
    }

    /// <summary>
    /// 定期的なプール状況のデバッグ出力
    /// </summary>
    private IEnumerator DebugPoolStatus()
    {
        while (enabled)
        {
            yield return new WaitForSeconds(10f);
            
            int activeCards = 0;
            foreach (var card in cardPool)
            {
                if (card != null && card.gameObject && card.gameObject.activeInHierarchy)
                    activeCards++;
            }
            
            Debug.Log($"【プール状況】 プールサイズ:{poolSize}, プール内:{cardPool.Count}, " +
                     $"アクティブ:{activeCards}, 表示中:{this.activeCards.Count}, 全カード:{allCards.Count}");
        }
    }
}