using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// ----------------------------------------------------------------------
// SimpleVirtualScroll クラス
// シンプルな仮想スクロールを実装するクラス。
// カードの動的な表示と非表示を管理し、パフォーマンスを最適化します。
// ----------------------------------------------------------------------
public class SimpleVirtualScroll : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // 定数定義
    // ----------------------------------------------------------------------
    private static class Constants
    {
        // デフォルト値
        public const int DEFAULT_POOL_SIZE = 20;
        public const int DEFAULT_COLUMNS_COUNT = 2;
        public const float DEFAULT_PADDING_LEFT = 25f;
        public const float DEFAULT_PADDING_TOP = 20f;
        public const float DEFAULT_CELL_WIDTH = 440f;
        public const float DEFAULT_CELL_HEIGHT = 660f;
        public const float DEFAULT_SPACING_X = 25f;
        public const float DEFAULT_SPACING_Y = 80f;
        
        // パフォーマンス設定
        public const int SCROLL_BUFFER_ROWS = 2;
        public const int POOL_EXPANSION_MULTIPLIER = 2;
        public const float MIN_CONTENT_HEIGHT_OFFSET = 1f;
        
        // スクロール位置
        public const float SCROLL_TOP_NORMALIZED_X = 0f;
        public const float SCROLL_TOP_NORMALIZED_Y = 1f;
        
        // アンカー設定
        public const float ANCHOR_TOP_LEFT_MIN = 0f;
        public const float ANCHOR_TOP_LEFT_MAX = 0f;
        public const float PIVOT_TOP_LEFT = 0f;
    }
    // ----------------------------------------------------------------------
    // 基本設定
    // UIコンポーネントやカードプレハブの設定を行います。
    // ----------------------------------------------------------------------
    [Header("基本設定")]
    [SerializeField] private ScrollRect scrollRect;   // UIのScrollRect
    [SerializeField] private RectTransform content;   // スクロール内のコンテンツ領域
    [SerializeField] private CardView cardPrefab;     // カードのプレハブ
    
    // ----------------------------------------------------------------------
    // パフォーマンス設定
    // ----------------------------------------------------------------------
    [Header("パフォーマンス設定")]
    [SerializeField] private int poolSize = Constants.DEFAULT_POOL_SIZE;       // 再利用するカード数
    
    // ----------------------------------------------------------------------
    // GridLayout設定
    // カードのレイアウトに関する設定を行います。
    // ----------------------------------------------------------------------
    [Header("GridLayout設定")]
    [SerializeField] private float paddingLeft = Constants.DEFAULT_PADDING_LEFT;
    [SerializeField] private float paddingTop = Constants.DEFAULT_PADDING_TOP;
    [SerializeField] private float cellWidth = Constants.DEFAULT_CELL_WIDTH;
    [SerializeField] private float cellHeight = Constants.DEFAULT_CELL_HEIGHT;
    [SerializeField] private float spacingX = Constants.DEFAULT_SPACING_X;
    [SerializeField] private float spacingY = Constants.DEFAULT_SPACING_Y;
    [SerializeField] private int columnsCount = Constants.DEFAULT_COLUMNS_COUNT;
    
    // カードデータ
    private List<CardModel> allCards = new List<CardModel>();
    
    // プールされたカードビュー
    private List<CardView> cardPool = new List<CardView>();
    
    // カード表示位置の管理
    private float cardHeight;                       // 1枚のカードの高さ
    private float viewportHeight;                   // 表示領域の高さ
    private Dictionary<int, RectTransform> activeCards = new Dictionary<int, RectTransform>();
    
    // ----------------------------------------------------------------------
    // SetGridSettings メソッド
    // GridLayoutの設定を反映します。
    // @param paddingLeft 左の余白
    // @param paddingTop 上の余白
    // @param cellWidth セルの幅
    // @param cellHeight セルの高さ
    // @param spacingX セル間の水平間隔
    // @param spacingY セル間の垂直間隔
    // @param columnsCount 列数
    // ----------------------------------------------------------------------
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
    }
    
    // ----------------------------------------------------------------------
    // Start メソッド
    // 初期化処理を行います。
    // 必要なコンポーネントのチェック、GridLayoutの無効化、
    // スクロールイベントの登録、カードプールの初期化を行います。
    // ----------------------------------------------------------------------
    private void Start()
    {
        try
        {
            ExecuteSafeInitialization();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SimpleVirtualScroll: 初期化中にエラーが発生しました");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeInitialization メソッド
    // 安全な初期化処理を実行します。
    // ----------------------------------------------------------------------
    private void ExecuteSafeInitialization()
    {
        if (!ValidateRequiredComponents())
        {
            return;
        }

        SetupGridLayoutConfiguration();
        SetupScrollPosition();
        SetupViewportConfiguration();
        SetupScrollEventHandlers();
        CleanupExistingCards();
        InitializeCardPool();
    }

    // ----------------------------------------------------------------------
    // ValidateRequiredComponents メソッド
    // 必要なコンポーネントの存在を検証します。
    // @return 全て存在する場合true
    // ----------------------------------------------------------------------
    private bool ValidateRequiredComponents()
    {
        if (scrollRect == null)
        {
            Debug.LogError("SimpleVirtualScroll: ScrollRectが設定されていません");
            return false;
        }

        if (content == null)
        {
            Debug.LogError("SimpleVirtualScroll: Contentが設定されていません");
            return false;
        }

        if (cardPrefab == null)
        {
            Debug.LogError("SimpleVirtualScroll: CardPrefabが設定されていません");
            return false;
        }

        return true;
    }

    // ----------------------------------------------------------------------
    // SetupGridLayoutConfiguration メソッド
    // GridLayoutの設定を行います。
    // ----------------------------------------------------------------------
    private void SetupGridLayoutConfiguration()
    {
        GridLayoutGroup gridLayout = content.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            ProcessExistingGridLayout(gridLayout);
        }
        else
        {
            // GridLayoutGroupがない場合はデフォルト値を使用
            SetGridSettings(paddingLeft, paddingTop, cellWidth, cellHeight, spacingX, spacingY, columnsCount);
        }
    }

    // ----------------------------------------------------------------------
    // ProcessExistingGridLayout メソッド
    // 既存のGridLayoutGroupの設定を処理します。
    // @param gridLayout 処理対象のGridLayoutGroup
    // ----------------------------------------------------------------------
    private void ProcessExistingGridLayout(GridLayoutGroup gridLayout)
    {
        // GridLayoutGroupの設定を反映
        float paddingLeft = gridLayout.padding.left;
        float paddingTop = gridLayout.padding.top;
        float cellWidth = gridLayout.cellSize.x;
        float cellHeight = gridLayout.cellSize.y;
        float spacingX = gridLayout.spacing.x;
        float spacingY = gridLayout.spacing.y;
        int columnsCount = Constants.DEFAULT_COLUMNS_COUNT;

        if (gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            columnsCount = gridLayout.constraintCount;
        }

        // この情報をSimpleVirtualScrollの設計に組み込む
        SetGridSettings(paddingLeft, paddingTop, cellWidth, cellHeight, spacingX, spacingY, columnsCount);

        // GridLayoutGroupを無効化
        gridLayout.enabled = false;
    }

    // ----------------------------------------------------------------------
    // SetupScrollPosition メソッド
    // スクロール位置を初期化します。
    // ----------------------------------------------------------------------
    private void SetupScrollPosition()
    {
        // スクロール位置をリセットして、一番上から表示
        content.anchoredPosition = Vector2.zero;
        scrollRect.normalizedPosition = new Vector2(Constants.SCROLL_TOP_NORMALIZED_X, Constants.SCROLL_TOP_NORMALIZED_Y);
    }

    // ----------------------------------------------------------------------
    // SetupViewportConfiguration メソッド
    // ビューポートの設定を行います。
    // ----------------------------------------------------------------------
    private void SetupViewportConfiguration()
    {
        // 表示領域の高さを取得
        viewportHeight = scrollRect.viewport.rect.height;
    }

    // ----------------------------------------------------------------------
    // SetupScrollEventHandlers メソッド
    // スクロールイベントハンドラーを設定します。
    // ----------------------------------------------------------------------
    private void SetupScrollEventHandlers()
    {
        // スクロールイベントを登録
        scrollRect.onValueChanged.AddListener(OnScroll);
    }

    // ----------------------------------------------------------------------
    // CleanupExistingCards メソッド
    // 既存のカードをクリーンアップします。
    // ----------------------------------------------------------------------
    private void CleanupExistingCards()
    {
        // 起動時に既存のカードを全て削除して確実にクリーンな状態で開始
        foreach (Transform child in content)
        {
            if (child.GetComponent<CardView>() != null)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    // ----------------------------------------------------------------------
    // InitializeCardPool メソッド
    // カードプールを初期化します。
    // プール内のカードを生成し、非アクティブ状態に設定します。
    // ----------------------------------------------------------------------
    private void InitializeCardPool()
    {
        try
        {
            ExecuteSafeCardPoolInitialization();
            Debug.Log($"SimpleVirtualScroll: カードプール初期化完了。プールサイズ: {poolSize}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SimpleVirtualScroll: カードプール初期化中にエラーが発生しました");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeCardPoolInitialization メソッド
    // 安全なカードプール初期化処理を実行します。
    // ----------------------------------------------------------------------
    private void ExecuteSafeCardPoolInitialization()
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
    
    // ----------------------------------------------------------------------
    // SetCards メソッド
    // 全カードデータを設定します。
    // @param cards カードデータのリスト
    // コンテンツの高さを計算し、表示範囲を更新します。
    // ----------------------------------------------------------------------
    public void SetCards(List<CardModel> cards)
    {
        try
        {
            ExecuteSafeSetCards(cards);
            Debug.Log($"SimpleVirtualScroll: カード設定完了。カード数: {cards?.Count ?? 0}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SimpleVirtualScroll: カード設定中にエラーが発生しました");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeSetCards メソッド
    // 安全なカード設定処理を実行します。
    // @param cards カードデータのリスト
    // ----------------------------------------------------------------------
    private void ExecuteSafeSetCards(List<CardModel> cards)
    {
        if (cards == null)
        {
            Debug.LogWarning("SimpleVirtualScroll: カードデータがnullです");
            cards = new List<CardModel>();
        }

        DeactivateAllActiveCards();
        UpdateCardData(cards);
        RecalculateContentHeight(cards);
        UpdateVisibleCards();
    }

    // ----------------------------------------------------------------------
    // DeactivateAllActiveCards メソッド
    // 全てのアクティブカードを非アクティブ化します。
    // ----------------------------------------------------------------------
    private void DeactivateAllActiveCards()
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
    }

    // ----------------------------------------------------------------------
    // UpdateCardData メソッド
    // カードデータを更新します。
    // @param cards 新しいカードデータのリスト
    // ----------------------------------------------------------------------
    private void UpdateCardData(List<CardModel> cards)
    {
        allCards = cards;
    }

    // ----------------------------------------------------------------------
    // RecalculateContentHeight メソッド
    // コンテンツの高さを再計算します。
    // @param cards カードデータのリスト
    // ----------------------------------------------------------------------
    private void RecalculateContentHeight(List<CardModel> cards)
    {
        // 行数を計算
        int rowCount = Mathf.CeilToInt((float)cards.Count / columnsCount);

        // コンテンツの高さを計算
        float contentHeight = CalculateContentHeight(rowCount);

        // 最低でもビューポートの高さ以上にする（スクロールを可能にするため）
        contentHeight = Mathf.Max(contentHeight, viewportHeight + Constants.MIN_CONTENT_HEIGHT_OFFSET);

        // コンテンツのサイズを調整
        content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
    }

    // ----------------------------------------------------------------------
    // CalculateContentHeight メソッド
    // 指定された行数でコンテンツの高さを計算します。
    // @param rowCount 行数
    // @return 計算されたコンテンツの高さ
    // ----------------------------------------------------------------------
    private float CalculateContentHeight(int rowCount)
    {
        return paddingTop + (rowCount * cellHeight) + ((rowCount - 1) * spacingY);
    }

    // ----------------------------------------------------------------------
    // OnScroll メソッド
    // スクロール時の処理を行います。
    // 表示範囲内のカードを更新します。
    // @param normalizedPosition スクロール位置の正規化された値
    // ----------------------------------------------------------------------
    private void OnScroll(Vector2 normalizedPosition)
    {
        try
        {
            UpdateVisibleCards();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SimpleVirtualScroll: スクロール処理中にエラーが発生しました");
            Debug.LogException(ex);
        }
    }
    
    // TODO ここ長すぎ
    // ----------------------------------------------------------------------
    // UpdateVisibleCards メソッド
    // 表示されているカードを更新します。
    // スクロール位置に基づいて、表示範囲内のカードをアクティブ化し、
    // 範囲外のカードを非アクティブ化します。
    // ----------------------------------------------------------------------
    private void UpdateVisibleCards()
    {
        try
        {
            if (allCards == null || allCards.Count == 0)
            {
                Debug.LogWarning("SimpleVirtualScroll: カードデータが空です");
                return;
            }

            ExecuteSafeUpdateVisibleCards();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SimpleVirtualScroll: 表示カード更新中にエラーが発生しました");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeUpdateVisibleCards メソッド
    // 安全な表示カード更新処理を実行します。
    // ----------------------------------------------------------------------
    private void ExecuteSafeUpdateVisibleCards()
    {
        var scrollData = CalculateScrollViewRange();
        RemoveCardsOutsideRange(scrollData.startIndex, scrollData.endIndex);
        DisplayCardsInRange(scrollData.startIndex, scrollData.endIndex);
    }

    // ----------------------------------------------------------------------
    // CalculateScrollViewRange メソッド
    // スクロール表示範囲を計算します。
    // @return 開始・終了インデックスを含むデータ
    // ----------------------------------------------------------------------
    private (int startIndex, int endIndex) CalculateScrollViewRange()
    {
        // 現在のスクロール位置
        float scrollPosition = content.anchoredPosition.y;

        // 表示範囲の先頭と末尾の行インデックスを計算
        int startRow = Mathf.FloorToInt(scrollPosition / (cellHeight + spacingY));
        int endRow = Mathf.CeilToInt((scrollPosition + viewportHeight) / (cellHeight + spacingY));

        // バッファを追加（スクロールの先読み）
        startRow = Mathf.Max(0, startRow - Constants.SCROLL_BUFFER_ROWS);
        endRow = Mathf.Min(Mathf.CeilToInt((float)allCards.Count / columnsCount), endRow + 1);

        // 行と列からインデックス範囲を計算
        int startIndex = startRow * columnsCount;
        int endIndex = Mathf.Min(allCards.Count - 1, (endRow + 1) * columnsCount - 1);

        return (startIndex, endIndex);
    }

    // ----------------------------------------------------------------------
    // RemoveCardsOutsideRange メソッド
    // 表示範囲外のカードを非アクティブ化します。
    // @param startIndex 表示開始インデックス
    // @param endIndex 表示終了インデックス
    // ----------------------------------------------------------------------
    private void RemoveCardsOutsideRange(int startIndex, int endIndex)
    {
        List<int> removeIndices = new List<int>();
        
        foreach (var kvp in activeCards)
        {
            if (ProcessCardForRemoval(kvp, startIndex, endIndex))
            {
                removeIndices.Add(kvp.Key);
            }
        }

        // 削除対象のインデックスを実際に削除
        foreach (int index in removeIndices)
        {
            activeCards.Remove(index);
        }
    }

    // ----------------------------------------------------------------------
    // ProcessCardForRemoval メソッド
    // カードが削除対象かどうかを判定し、必要に応じて非アクティブ化します。
    // @param kvp アクティブカードのキーバリューペア
    // @param startIndex 表示開始インデックス
    // @param endIndex 表示終了インデックス
    // @return 削除が必要な場合true
    // ----------------------------------------------------------------------
    private bool ProcessCardForRemoval(KeyValuePair<int, RectTransform> kvp, int startIndex, int endIndex)
    {
        // RectTransformが無効になっていないか確認
        if (kvp.Value == null || !kvp.Value.gameObject)
        {
            Debug.LogWarning($"SimpleVirtualScroll: 無効なカードが検出されました。Index: {kvp.Key}");
            return true;
        }

        if (kvp.Key < startIndex || kvp.Key > endIndex)
        {
            try
            {
                // プールに戻す
                kvp.Value.gameObject.SetActive(false);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SimpleVirtualScroll: カードを非アクティブ化中にエラーが発生しました。Index: {kvp.Key}");
                Debug.LogException(ex);
                return true;
            }
        }

        return false;
    }

    // ----------------------------------------------------------------------
    // DisplayCardsInRange メソッド
    // 表示範囲内のカードを表示します。
    // @param startIndex 表示開始インデックス
    // @param endIndex 表示終了インデックス
    // ----------------------------------------------------------------------
    private void DisplayCardsInRange(int startIndex, int endIndex)
    {
        for (int i = startIndex; i <= endIndex; i++)
        {
            if (i < 0 || i >= allCards.Count)
                continue;

            if (!activeCards.ContainsKey(i))
            {
                ProcessCardDisplay(i);
            }
        }
    }

    // ----------------------------------------------------------------------
    // ProcessCardDisplay メソッド
    // 単一のカードの表示処理を実行します。
    // @param cardIndex カードのインデックス
    // ----------------------------------------------------------------------
    private void ProcessCardDisplay(int cardIndex)
    {
        try
        {
            CardView card = GetCardFromPool();
            if (card != null)
            {
                SetupCardDisplayPosition(card, cardIndex);
                SetupCardData(card, cardIndex);
                ActivateCardDisplay(card, cardIndex);
            }
            else
            {
                Debug.LogWarning($"SimpleVirtualScroll: プールからカードを取得できませんでした。Index: {cardIndex}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SimpleVirtualScroll: カード表示中にエラーが発生しました。Index: {cardIndex}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // SetupCardDisplayPosition メソッド
    // カードの表示位置を設定します。
    // @param card 設定対象のカードビュー
    // @param cardIndex カードのインデックス
    // ----------------------------------------------------------------------
    private void SetupCardDisplayPosition(CardView card, int cardIndex)
    {
        RectTransform rectTransform = card.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError($"SimpleVirtualScroll: RectTransformが見つかりません。Index: {cardIndex}");
            return;
        }

        // 行と列を計算
        int row = cardIndex / columnsCount;
        int col = cardIndex % columnsCount;

        // グリッドレイアウトに合わせた位置を設定
        float xPos = paddingLeft + col * (cellWidth + spacingX);
        float yPos = paddingTop + row * (cellHeight + spacingY);

        // カードのRectTransformの設定
        rectTransform.anchorMin = new Vector2(Constants.ANCHOR_TOP_LEFT_MIN, 1);
        rectTransform.anchorMax = new Vector2(Constants.ANCHOR_TOP_LEFT_MAX, 1);
        rectTransform.pivot = new Vector2(Constants.PIVOT_TOP_LEFT, 1);

        // 位置を設定（Unityの座標系に合わせて調整）
        rectTransform.anchoredPosition = new Vector2(xPos, -yPos);

        // サイズを設定
        rectTransform.sizeDelta = new Vector2(cellWidth, cellHeight);
    }

    // ----------------------------------------------------------------------
    // SetupCardData メソッド
    // カードにデータを設定します。
    // @param card 設定対象のカードビュー
    // @param cardIndex カードのインデックス
    // ----------------------------------------------------------------------
    private void SetupCardData(CardView card, int cardIndex)
    {
        if (cardIndex >= 0 && cardIndex < allCards.Count)
        {
            card.SetImage(allCards[cardIndex]);
        }
        else
        {
            Debug.LogError($"SimpleVirtualScroll: 無効なカードインデックス。Index: {cardIndex}, Count: {allCards.Count}");
        }
    }

    // ----------------------------------------------------------------------
    // ActivateCardDisplay メソッド
    // カードをアクティブ化してアクティブリストに追加します。
    // @param card アクティブ化するカードビュー
    // @param cardIndex カードのインデックス
    // ----------------------------------------------------------------------
    private void ActivateCardDisplay(CardView card, int cardIndex)
    {
        RectTransform rectTransform = card.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            card.gameObject.SetActive(true);
            activeCards.Add(cardIndex, rectTransform);
        }
        else
        {
            Debug.LogError($"SimpleVirtualScroll: RectTransformの取得に失敗しました。Index: {cardIndex}");
        }
    }
    
    // TODO ここ長すぎ
    // ----------------------------------------------------------------------
    // GetCardFromPool メソッド
    // プールから非アクティブなカードを取得します。
    // 必要に応じてプールを拡張します。
    // @return 利用可能なカードビュー
    // ----------------------------------------------------------------------
    private CardView GetCardFromPool()
    {
        try
        {
            return ExecuteSafeGetCardFromPool();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SimpleVirtualScroll: カードプールからの取得中にエラーが発生しました");
            Debug.LogException(ex);
            return null;
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeGetCardFromPool メソッド
    // 安全なカードプール取得処理を実行します。
    // @return 利用可能なカードビュー
    // ----------------------------------------------------------------------
    private CardView ExecuteSafeGetCardFromPool()
    {
        OptimizeCardPool();
        CardView availableCard = FindAvailableCardInPool();
        
        if (availableCard != null)
        {
            return availableCard;
        }

        CleanupInvalidPoolReferences();
        return CreateNewPoolCard();
    }

    // ----------------------------------------------------------------------
    // OptimizeCardPool メソッド
    // プールのサイズを最適化します。
    // ----------------------------------------------------------------------
    private void OptimizeCardPool()
    {
        if (cardPool.Count <= poolSize * Constants.POOL_EXPANSION_MULTIPLIER)
        {
            return;
        }

        int removeCount = 0;
        for (int i = cardPool.Count - 1; i >= poolSize; i--)
        {
            if (TryRemoveInactivePoolCard(i))
            {
                removeCount++;
                if (cardPool.Count <= poolSize)
                {
                    break;
                }
            }
        }

        if (removeCount > 0)
        {
            Debug.Log($"SimpleVirtualScroll: プールを最適化しました。削除数: {removeCount}");
        }
    }

    // ----------------------------------------------------------------------
    // TryRemoveInactivePoolCard メソッド
    // 指定されたインデックスの非アクティブカードを削除を試行します。
    // @param index 削除対象のインデックス
    // @return 削除成功時true
    // ----------------------------------------------------------------------
    private bool TryRemoveInactivePoolCard(int index)
    {
        if (index < 0 || index >= cardPool.Count)
        {
            return false;
        }

        var card = cardPool[index];
        if (card != null && card.gameObject && !card.gameObject.activeInHierarchy)
        {
            Destroy(card.gameObject);
            cardPool.RemoveAt(index);
            return true;
        }

        return false;
    }

    // ----------------------------------------------------------------------
    // FindAvailableCardInPool メソッド
    // プール内から利用可能なカードを検索します。
    // @return 利用可能なカードビュー、見つからない場合null
    // ----------------------------------------------------------------------
    private CardView FindAvailableCardInPool()
    {
        for (int i = 0; i < cardPool.Count; i++)
        {
            var card = cardPool[i];
            
            if (!IsValidPoolCard(card))
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

        return null;
    }

    // ----------------------------------------------------------------------
    // IsValidPoolCard メソッド
    // プールカードが有効かどうかを判定します。
    // @param card 判定対象のカードビュー
    // @return 有効な場合true
    // ----------------------------------------------------------------------
    private bool IsValidPoolCard(CardView card)
    {
        return card != null && card.gameObject;
    }

    // ----------------------------------------------------------------------
    // CleanupInvalidPoolReferences メソッド
    // プール内の無効な参照をクリーンアップします。
    // ----------------------------------------------------------------------
    private void CleanupInvalidPoolReferences()
    {
        int beforeCount = cardPool.Count;
        cardPool.RemoveAll(card => !IsValidPoolCard(card));
        int afterCount = cardPool.Count;

        if (beforeCount != afterCount)
        {
            Debug.Log($"SimpleVirtualScroll: 無効な参照をクリーンアップしました。削除数: {beforeCount - afterCount}");
        }
    }

    // ----------------------------------------------------------------------
    // CreateNewPoolCard メソッド
    // 新しいプールカードを作成します。
    // @return 新しく作成されたカードビュー
    // ----------------------------------------------------------------------
    private CardView CreateNewPoolCard()
    {
        if (cardPrefab == null)
        {
            Debug.LogError("SimpleVirtualScroll: カードプレハブが設定されていません");
            return null;
        }

        if (content == null)
        {
            Debug.LogError("SimpleVirtualScroll: コンテンツが設定されていません");
            return null;
        }

        Debug.Log("SimpleVirtualScroll: プールを動的に拡張しています");
        CardView newCard = Instantiate(cardPrefab, content);
        cardPool.Add(newCard);
        return newCard;
    }
    
    // ----------------------------------------------------------------------
    // OnDestroy メソッド
    // コンポーネント破棄時の処理を行います。
    // スクロールイベントのリスナーを解除します。
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        try
        {
            ExecuteSafeDestroy();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SimpleVirtualScroll: 破棄処理中にエラーが発生しました");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeDestroy メソッド
    // 安全な破棄処理を実行します。
    // ----------------------------------------------------------------------
    private void ExecuteSafeDestroy()
    {
        CleanupScrollEventHandlers();
        CleanupActiveCards();
        CleanupCardPool();
    }

    // ----------------------------------------------------------------------
    // CleanupScrollEventHandlers メソッド
    // スクロールイベントハンドラーをクリーンアップします。
    // ----------------------------------------------------------------------
    private void CleanupScrollEventHandlers()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScroll);
        }
    }

    // ----------------------------------------------------------------------
    // CleanupActiveCards メソッド
    // アクティブカードをクリーンアップします。
    // ----------------------------------------------------------------------
    private void CleanupActiveCards()
    {
        activeCards.Clear();
    }

    // ----------------------------------------------------------------------
    // CleanupCardPool メソッド
    // カードプールをクリーンアップします。
    // ----------------------------------------------------------------------
    private void CleanupCardPool()
    {
        cardPool.Clear();
    }
}