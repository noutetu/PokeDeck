using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

// ----------------------------------------------------------------------
// 遅延読み込み機能を管理するクラス
// スクロール検知と動的バッチ読み込みを担当
// ----------------------------------------------------------------------
public class LazyLoadManager
{
    // ----------------------------------------------------------------------
    // 定数定義
    // ----------------------------------------------------------------------
    private static class Constants
    {
        // デフォルト値
        public const int DEFAULT_INITIAL_CARD_COUNT = 30;
        public const int DEFAULT_BATCH_SIZE = 20;
        public const float DEFAULT_SCROLL_THRESHOLD = 0.7f;
        public const float DEFAULT_SCROLL_COOLDOWN = 0.1f;
        public const int DEFAULT_SUB_BATCH_SIZE = 3;
        
        // 動的バッチサイズ制御
        public const int MIN_BATCH_SIZE = 5;
        public const int MAX_BATCH_SIZE = 30;
        public const int BATCH_SIZE_INCREMENT = 5;
        public const int BATCH_SIZE_DECREMENT = 1;
        public const float FAST_SCROLL_THRESHOLD = 0.05f;
        
        // スクロール制御
        public const float SCROLL_BOTTOM_THRESHOLD = 1.0f;
    }
    
    // ----------------------------------------------------------------------
    // フィールド定義
    // ----------------------------------------------------------------------
    private readonly AllCardPresenter presenter;
    private readonly int initialCardCount;
    private readonly int baseBatchSize;
    private readonly float scrollThreshold;
    
    private List<CardModel> remainingCards = new List<CardModel>();
    private bool isLoadingBatch = false;
    private bool ignoreScrollEvent = false;
    private Vector2 lastPosition = Vector2.zero;
    private float lastScrollTime = 0f;
    private float scrollCooldown = Constants.DEFAULT_SCROLL_COOLDOWN;
    private int dynamicBatchSize; // readonly削除
    
    public event Action<List<CardModel>> OnCardsFiltered;
    
    public LazyLoadManager(AllCardPresenter presenter, int initialCardCount = Constants.DEFAULT_INITIAL_CARD_COUNT, 
                          int batchSize = Constants.DEFAULT_BATCH_SIZE, float scrollThreshold = Constants.DEFAULT_SCROLL_THRESHOLD)
    {
        try
        {
            // readonly フィールドの割り当てはコンストラクタ内で直接実行
            ValidateInitializationParameters(presenter, initialCardCount, batchSize, scrollThreshold);
            
            this.presenter = presenter;
            this.initialCardCount = initialCardCount;
            this.baseBatchSize = batchSize;
            this.scrollThreshold = scrollThreshold;
            
            // 非readonly フィールドの初期化
            this.dynamicBatchSize = batchSize;
            InitializeInternalState();
            
            Debug.Log($"LazyLoadManager: 初期化完了 - InitialCount: {initialCardCount}, BatchSize: {batchSize}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LazyLoadManager: 初期化中にエラーが発生しました");
            Debug.LogException(ex);
            
            // readonly フィールドのデフォルト値設定（コンストラクタ内のみ可能）
            this.presenter = null;
            this.initialCardCount = Constants.DEFAULT_INITIAL_CARD_COUNT;
            this.baseBatchSize = Constants.DEFAULT_BATCH_SIZE;
            this.scrollThreshold = Constants.DEFAULT_SCROLL_THRESHOLD;
            this.dynamicBatchSize = Constants.DEFAULT_BATCH_SIZE;
            InitializeInternalState();
        }
    }

    // ----------------------------------------------------------------------
    // ValidateInitializationParameters メソッド
    // 初期化パラメータを検証します。
    // ----------------------------------------------------------------------
    private void ValidateInitializationParameters(AllCardPresenter presenter, int initialCardCount, int batchSize, float scrollThreshold)
    {
        if (presenter == null)
        {
            throw new ArgumentNullException(nameof(presenter), "LazyLoadManager: Presenterが指定されていません");
        }

        if (initialCardCount <= 0)
        {
            throw new ArgumentException($"LazyLoadManager: 無効な初期カード数: {initialCardCount}", nameof(initialCardCount));
        }

        if (batchSize <= 0)
        {
            throw new ArgumentException($"LazyLoadManager: 無効なバッチサイズ: {batchSize}", nameof(batchSize));
        }

        if (scrollThreshold < 0 || scrollThreshold > 1)
        {
            throw new ArgumentException($"LazyLoadManager: 無効なスクロール閾値: {scrollThreshold}", nameof(scrollThreshold));
        }
    }

    // ----------------------------------------------------------------------
    // InitializeInternalState メソッド
    // 内部状態を初期化します。
    // ----------------------------------------------------------------------
    private void InitializeInternalState()
    {
        remainingCards = new List<CardModel>();
        isLoadingBatch = false;
        ignoreScrollEvent = false;
        lastPosition = Vector2.zero;
        lastScrollTime = 0f;
        scrollCooldown = Constants.DEFAULT_SCROLL_COOLDOWN;
    }
    
    // ----------------------------------------------------------------------
    // InitializeWithCards メソッド
    // 全カードデータで初期化を行います。
    // @param allCards 全カードデータのリスト
    // ----------------------------------------------------------------------
    public void InitializeWithCards(List<CardModel> allCards)
    {
        try
        {
            ExecuteSafeCardInitialization(allCards);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LazyLoadManager: カード初期化中にエラーが発生しました");
            Debug.LogException(ex);
            HandleInitializationFailure();
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeCardInitialization メソッド
    // 安全なカード初期化処理を実行します。
    // @param allCards 全カードデータのリスト
    // ----------------------------------------------------------------------
    private void ExecuteSafeCardInitialization(List<CardModel> allCards)
    {
        ValidateCardData(allCards);
        var cardDistribution = CalculateCardDistribution(allCards);
        LoadInitialCards(cardDistribution.initialCards);
        SetupRemainingCards(cardDistribution.remainingCards);
        
        Debug.Log($"LazyLoadManager: カード初期化完了 - 初期表示: {cardDistribution.initialCards.Count}枚, 残り: {cardDistribution.remainingCards.Count}枚");
    }

    // ----------------------------------------------------------------------
    // ValidateCardData メソッド
    // カードデータの妥当性を検証します。
    // @param allCards 検証対象のカードデータ
    // ----------------------------------------------------------------------
    private void ValidateCardData(List<CardModel> allCards)
    {
        if (allCards == null)
        {
            throw new ArgumentNullException(nameof(allCards), "LazyLoadManager: カードデータがnullです");
        }

        if (presenter == null)
        {
            throw new InvalidOperationException("LazyLoadManager: Presenterが初期化されていません");
        }
    }

    // ----------------------------------------------------------------------
    // CalculateCardDistribution メソッド
    // カードの配分を計算します。
    // @param allCards 全カードデータ
    // @return 初期カードと残りカードの配分
    // ----------------------------------------------------------------------
    private (List<CardModel> initialCards, List<CardModel> remainingCards) CalculateCardDistribution(List<CardModel> allCards)
    {
        int displayCount = Mathf.Min(initialCardCount, allCards.Count);
        List<CardModel> initialCards = allCards.GetRange(0, displayCount);
        
        List<CardModel> remainingCards = new List<CardModel>();
        if (allCards.Count > displayCount)
        {
            remainingCards = allCards.GetRange(displayCount, allCards.Count - displayCount);
        }
        
        return (initialCards, remainingCards);
    }

    // ----------------------------------------------------------------------
    // LoadInitialCards メソッド
    // 初期カードを読み込みます。
    // @param initialCards 初期表示するカードデータ
    // ----------------------------------------------------------------------
    private void LoadInitialCards(List<CardModel> initialCards)
    {
        if (initialCards.Count > 0)
        {
            presenter.LoadCards(initialCards);
        }
        else
        {
            Debug.LogWarning("LazyLoadManager: 初期表示するカードがありません");
        }
    }

    // ----------------------------------------------------------------------
    // SetupRemainingCards メソッド
    // 残りカードを設定します。
    // @param cards 残りカードデータ
    // ----------------------------------------------------------------------
    private void SetupRemainingCards(List<CardModel> cards)
    {
        remainingCards = cards;
    }

    // ----------------------------------------------------------------------
    // HandleInitializationFailure メソッド
    // 初期化失敗時の処理を行います。
    // ----------------------------------------------------------------------
    private void HandleInitializationFailure()
    {
        remainingCards = new List<CardModel>();
        Debug.LogWarning("LazyLoadManager: 初期化に失敗しました。空の状態で継続します");
    }
    
    // ----------------------------------------------------------------------
    // SetFilteredCards メソッド
    // フィルターされたカードを設定し、遅延読み込み用に準備します。
    // @param filteredCards フィルターされたカードデータのリスト
    // ----------------------------------------------------------------------
    public void SetFilteredCards(List<CardModel> filteredCards)
    {
        try
        {
            ExecuteSafeFilteredCardSetup(filteredCards);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LazyLoadManager: フィルターカード設定中にエラーが発生しました");
            Debug.LogException(ex);
            HandleFilterSetupFailure();
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeFilteredCardSetup メソッド
    // 安全なフィルターカード設定処理を実行します。
    // @param filteredCards フィルターされたカードデータ
    // ----------------------------------------------------------------------
    private void ExecuteSafeFilteredCardSetup(List<CardModel> filteredCards)
    {
        ValidateFilteredCards(filteredCards);
        ClearCurrentCardDisplay();
        
        var cardDistribution = CalculateFilteredCardDistribution(filteredCards);
        LoadFilteredInitialCards(cardDistribution.initialCards);
        SetupFilteredRemainingCards(cardDistribution.remainingCards);
        FinalizeFilteredCardSetup(filteredCards);
    }

    // ----------------------------------------------------------------------
    // ValidateFilteredCards メソッド
    // フィルターされたカードデータの妥当性を検証します。
    // @param filteredCards 検証対象のカードデータ
    // ----------------------------------------------------------------------
    private void ValidateFilteredCards(List<CardModel> filteredCards)
    {
        if (filteredCards == null)
        {
            throw new ArgumentNullException(nameof(filteredCards), "LazyLoadManager: フィルターされたカードデータがnullです");
        }

        if (presenter == null)
        {
            throw new InvalidOperationException("LazyLoadManager: Presenterが初期化されていません");
        }
    }

    // ----------------------------------------------------------------------
    // ClearCurrentCardDisplay メソッド
    // 現在の表示カードをクリアします。
    // ----------------------------------------------------------------------
    private void ClearCurrentCardDisplay()
    {
        presenter.ClearCards();
    }

    // ----------------------------------------------------------------------
    // CalculateFilteredCardDistribution メソッド
    // フィルターされたカードの配分を計算します。
    // @param filteredCards フィルターされたカードデータ
    // @return 初期表示カードと残りカードの配分
    // ----------------------------------------------------------------------
    private (List<CardModel> initialCards, List<CardModel> remainingCards) CalculateFilteredCardDistribution(List<CardModel> filteredCards)
    {
        int displayCount = Mathf.Min(initialCardCount, filteredCards.Count);
        List<CardModel> initialCards = new List<CardModel>();
        
        if (displayCount > 0)
        {
            initialCards = filteredCards.GetRange(0, displayCount);
        }
        
        List<CardModel> remainingCards = new List<CardModel>();
        if (filteredCards.Count > displayCount)
        {
            remainingCards = filteredCards.GetRange(displayCount, filteredCards.Count - displayCount);
        }
        
        return (initialCards, remainingCards);
    }

    // ----------------------------------------------------------------------
    // LoadFilteredInitialCards メソッド
    // フィルターされた初期カードを読み込みます。
    // @param initialCards 初期表示するカードデータ
    // ----------------------------------------------------------------------
    private void LoadFilteredInitialCards(List<CardModel> initialCards)
    {
        presenter.LoadCards(initialCards);
    }

    // ----------------------------------------------------------------------
    // SetupFilteredRemainingCards メソッド
    // フィルターされた残りカードを設定します。
    // @param cards 残りカードデータ
    // ----------------------------------------------------------------------
    private void SetupFilteredRemainingCards(List<CardModel> cards)
    {
        if (cards.Count > 0)
        {
            remainingCards = cards;
        }
        else
        {
            remainingCards.Clear();
        }
    }

    // ----------------------------------------------------------------------
    // FinalizeFilteredCardSetup メソッド
    // フィルターされたカード設定を完了します。
    // @param filteredCards 元のフィルターされたカードデータ
    // ----------------------------------------------------------------------
    private void FinalizeFilteredCardSetup(List<CardModel> filteredCards)
    {
        ignoreScrollEvent = true;
        OnCardsFiltered?.Invoke(filteredCards);
    }

    // ----------------------------------------------------------------------
    // HandleFilterSetupFailure メソッド
    // フィルター設定失敗時の処理を行います。
    // ----------------------------------------------------------------------
    private void HandleFilterSetupFailure()
    {
        remainingCards.Clear();
        ignoreScrollEvent = true;
        Debug.LogWarning("LazyLoadManager: フィルター設定に失敗しました。空の状態で継続します");
    }
    
    // ----------------------------------------------------------------------
    // OnScrollValueChanged メソッド
    // スクロール値変更時の処理を行います。
    // @param position 現在のスクロール位置
    // ----------------------------------------------------------------------
    public void OnScrollValueChanged(Vector2 position)
    {
        try
        {
            ExecuteSafeScrollProcessing(position);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LazyLoadManager: スクロール処理中にエラーが発生しました");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeScrollProcessing メソッド
    // 安全なスクロール処理を実行します。
    // @param position スクロール位置
    // ----------------------------------------------------------------------
    private void ExecuteSafeScrollProcessing(Vector2 position)
    {
        if (ShouldIgnoreScrollEvent())
        {
            return;
        }
        
        if (!ShouldProcessScroll(position))
        {
            return;
        }
            
        AdjustBatchSize(position);
        
        if (ShouldLoadNextBatch(position))
        {
            LoadNextBatchAsync().Forget();
        }
    }

    // ----------------------------------------------------------------------
    // ShouldIgnoreScrollEvent メソッド
    // スクロールイベントを無視すべきかを判定します。
    // @return 無視すべき場合はtrue
    // ----------------------------------------------------------------------
    private bool ShouldIgnoreScrollEvent()
    {
        if (ignoreScrollEvent)
        {
            ignoreScrollEvent = false;
            return true;
        }
        return false;
    }

    // ----------------------------------------------------------------------
    // ShouldLoadNextBatch メソッド
    // 次のバッチを読み込むべきかを判定します。
    // @param position 現在のスクロール位置
    // @return 読み込むべき場合はtrue
    // ----------------------------------------------------------------------
    private bool ShouldLoadNextBatch(Vector2 position)
    {
        return position.y < (Constants.SCROLL_BOTTOM_THRESHOLD - scrollThreshold);
    }
    
    // ----------------------------------------------------------------------
    // ShouldProcessScroll メソッド
    // スクロール処理を実行すべきかを判定します。
    // @param position 現在のスクロール位置
    // @return 処理すべき場合はtrue
    // ----------------------------------------------------------------------
    private bool ShouldProcessScroll(Vector2 position)
    {
        try
        {
            return ExecuteSafeScrollValidation(position);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LazyLoadManager: スクロール判定中にエラーが発生しました");
            Debug.LogException(ex);
            return false;
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeScrollValidation メソッド
    // 安全なスクロール判定処理を実行します。
    // @param position スクロール位置
    // @return 処理すべき場合はtrue
    // ----------------------------------------------------------------------
    private bool ExecuteSafeScrollValidation(Vector2 position)
    {
        if (!ValidateScrollCooldown())
        {
            return false;
        }
            
        UpdateScrollTime();
        return ValidateScrollConditions();
    }

    // ----------------------------------------------------------------------
    // ValidateScrollCooldown メソッド
    // スクロールのクールダウン時間を検証します。
    // @return クールダウンが完了している場合はtrue
    // ----------------------------------------------------------------------
    private bool ValidateScrollCooldown()
    {
        float currentTime = Time.time;
        return currentTime - lastScrollTime >= scrollCooldown;
    }

    // ----------------------------------------------------------------------
    // UpdateScrollTime メソッド
    // 最後のスクロール時間を更新します。
    // ----------------------------------------------------------------------
    private void UpdateScrollTime()
    {
        lastScrollTime = Time.time;
    }

    // ----------------------------------------------------------------------
    // ValidateScrollConditions メソッド
    // スクロール処理の条件を検証します。
    // @return 条件を満たしている場合はtrue
    // ----------------------------------------------------------------------
    private bool ValidateScrollConditions()
    {
        return remainingCards.Count > 0 && !isLoadingBatch;
    }
    
    // ----------------------------------------------------------------------
    // AdjustBatchSize メソッド
    // スクロール速度に基づいてバッチサイズを動的に調整します。
    // @param position 現在のスクロール位置
    // ----------------------------------------------------------------------
    private void AdjustBatchSize(Vector2 position)
    {
        try
        {
            ExecuteSafeBatchSizeAdjustment(position);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LazyLoadManager: バッチサイズ調整中にエラーが発生しました");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeBatchSizeAdjustment メソッド
    // 安全なバッチサイズ調整処理を実行します。
    // @param position スクロール位置
    // ----------------------------------------------------------------------
    private void ExecuteSafeBatchSizeAdjustment(Vector2 position)
    {
        float scrollSpeed = CalculateScrollSpeed(position);
        UpdateLastPosition(position);
        
        if (IsFastScrolling(scrollSpeed))
        {
            IncreaseBatchSize();
        }
        else
        {
            DecreaseBatchSize();
        }
    }

    // ----------------------------------------------------------------------
    // CalculateScrollSpeed メソッド
    // スクロール速度を計算します。
    // @param position 現在のスクロール位置
    // @return スクロール速度
    // ----------------------------------------------------------------------
    private float CalculateScrollSpeed(Vector2 position)
    {
        float scrollDirection = position.y - lastPosition.y;
        return Mathf.Abs(scrollDirection);
    }

    // ----------------------------------------------------------------------
    // UpdateLastPosition メソッド
    // 最後のスクロール位置を更新します。
    // @param position 現在のスクロール位置
    // ----------------------------------------------------------------------
    private void UpdateLastPosition(Vector2 position)
    {
        lastPosition = position;
    }

    // ----------------------------------------------------------------------
    // IsFastScrolling メソッド
    // 高速スクロール中かを判定します。
    // @param scrollSpeed スクロール速度
    // @return 高速スクロール中の場合はtrue
    // ----------------------------------------------------------------------
    private bool IsFastScrolling(float scrollSpeed)
    {
        return scrollSpeed > Constants.FAST_SCROLL_THRESHOLD;
    }

    // ----------------------------------------------------------------------
    // IncreaseBatchSize メソッド
    // バッチサイズを増加させます。
    // ----------------------------------------------------------------------
    private void IncreaseBatchSize()
    {
        dynamicBatchSize = Mathf.Clamp(dynamicBatchSize + Constants.BATCH_SIZE_INCREMENT, 
                                      Constants.MIN_BATCH_SIZE, Constants.MAX_BATCH_SIZE);
    }

    // ----------------------------------------------------------------------
    // DecreaseBatchSize メソッド
    // バッチサイズを減少させます。
    // ----------------------------------------------------------------------
    private void DecreaseBatchSize()
    {
        dynamicBatchSize = Mathf.Clamp(dynamicBatchSize - Constants.BATCH_SIZE_DECREMENT, 
                                      Constants.MIN_BATCH_SIZE, Constants.MAX_BATCH_SIZE);
    }
    
    // ----------------------------------------------------------------------
    // LoadNextBatchAsync メソッド
    // 次のバッチを非同期で読み込みます。
    // ----------------------------------------------------------------------
    private async UniTaskVoid LoadNextBatchAsync()
    {
        try
        {
            await ExecuteSafeBatchLoading();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LazyLoadManager: バッチ読み込み中にエラーが発生しました");
            Debug.LogException(ex);
            HandleBatchLoadingFailure();
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeBatchLoading メソッド
    // 安全なバッチ読み込み処理を実行します。
    // ----------------------------------------------------------------------
    private async UniTask ExecuteSafeBatchLoading()
    {
        if (!ValidateBatchLoadingConditions())
        {
            return;
        }
            
        SetBatchLoadingState(true);
        
        try
        {
            var batchData = PrepareBatchData();
            await LoadBatchInSubGroups(batchData.batch);
            UpdateRemainingCardsAfterLoad(batchData.batchSize);
            
            Debug.Log($"LazyLoadManager: バッチ読み込み完了 - {batchData.batchSize}枚読み込み、残り{remainingCards.Count}枚");
        }
        finally
        {
            SetBatchLoadingState(false);
        }
    }

    // ----------------------------------------------------------------------
    // ValidateBatchLoadingConditions メソッド
    // バッチ読み込みの条件を検証します。
    // @return 読み込み可能な場合はtrue
    // ----------------------------------------------------------------------
    private bool ValidateBatchLoadingConditions()
    {
        return !isLoadingBatch && remainingCards.Count > 0;
    }

    // ----------------------------------------------------------------------
    // SetBatchLoadingState メソッド
    // バッチ読み込み状態を設定します。
    // @param isLoading 読み込み中の場合はtrue
    // ----------------------------------------------------------------------
    private void SetBatchLoadingState(bool isLoading)
    {
        isLoadingBatch = isLoading;
    }

    // ----------------------------------------------------------------------
    // PrepareBatchData メソッド
    // バッチデータを準備します。
    // @return バッチサイズとバッチデータ
    // ----------------------------------------------------------------------
    private (int batchSize, List<CardModel> batch) PrepareBatchData()
    {
        int batchSize = Mathf.Min(dynamicBatchSize, remainingCards.Count);
        List<CardModel> batch = remainingCards.GetRange(0, batchSize);
        return (batchSize, batch);
    }

    // ----------------------------------------------------------------------
    // UpdateRemainingCardsAfterLoad メソッド
    // 読み込み後に残りカードを更新します。
    // @param loadedBatchSize 読み込まれたバッチサイズ
    // ----------------------------------------------------------------------
    private void UpdateRemainingCardsAfterLoad(int loadedBatchSize)
    {
        remainingCards.RemoveRange(0, loadedBatchSize);
    }

    // ----------------------------------------------------------------------
    // HandleBatchLoadingFailure メソッド
    // バッチ読み込み失敗時の処理を行います。
    // ----------------------------------------------------------------------
    private void HandleBatchLoadingFailure()
    {
        SetBatchLoadingState(false);
        Debug.LogWarning("LazyLoadManager: バッチ読み込みに失敗しました");
    }
    
    // ----------------------------------------------------------------------
    // LoadBatchInSubGroups メソッド
    // バッチをサブグループに分けて読み込みます。
    // @param batch 読み込み対象のバッチデータ
    // ----------------------------------------------------------------------
    private async UniTask LoadBatchInSubGroups(List<CardModel> batch)
    {
        try
        {
            await ExecuteSafeSubGroupLoading(batch);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LazyLoadManager: サブグループ読み込み中にエラーが発生しました");
            Debug.LogException(ex);
            throw; // 上位レベルでハンドリングするために再スロー
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeSubGroupLoading メソッド
    // 安全なサブグループ読み込み処理を実行します。
    // @param batch 読み込み対象のバッチデータ
    // ----------------------------------------------------------------------
    private async UniTask ExecuteSafeSubGroupLoading(List<CardModel> batch)
    {
        ValidateSubGroupBatch(batch);
        
        for (int i = 0; i < batch.Count; i += Constants.DEFAULT_SUB_BATCH_SIZE)
        {
            var subBatch = CreateSubBatch(batch, i);
            await ProcessSubBatch(subBatch);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }

    // ----------------------------------------------------------------------
    // ValidateSubGroupBatch メソッド
    // サブグループバッチの妥当性を検証します。
    // @param batch 検証対象のバッチデータ
    // ----------------------------------------------------------------------
    private void ValidateSubGroupBatch(List<CardModel> batch)
    {
        if (batch == null)
        {
            throw new ArgumentNullException(nameof(batch), "LazyLoadManager: バッチデータがnullです");
        }

        if (batch.Count == 0)
        {
            throw new ArgumentException("LazyLoadManager: バッチデータが空です", nameof(batch));
        }
    }

    // ----------------------------------------------------------------------
    // CreateSubBatch メソッド
    // サブバッチを作成します。
    // @param batch 元のバッチデータ
    // @param startIndex 開始インデックス
    // @return サブバッチデータ
    // ----------------------------------------------------------------------
    private List<CardModel> CreateSubBatch(List<CardModel> batch, int startIndex)
    {
        int count = Mathf.Min(Constants.DEFAULT_SUB_BATCH_SIZE, batch.Count - startIndex);
        return batch.GetRange(startIndex, count);
    }

    // ----------------------------------------------------------------------
    // ProcessSubBatch メソッド
    // サブバッチを処理します。
    // @param subBatch 処理対象のサブバッチデータ
    // ----------------------------------------------------------------------
    private async UniTask ProcessSubBatch(List<CardModel> subBatch)
    {
        await PreloadImages(subBatch);
        await presenter.AddCardsAsync(subBatch);
    }
    
    // ----------------------------------------------------------------------
    // PreloadImages メソッド
    // カードの画像を事前読み込みします。
    // @param cards 事前読み込み対象のカードデータ
    // ----------------------------------------------------------------------
    private async UniTask PreloadImages(List<CardModel> cards)
    {
        try
        {
            await ExecuteSafeImagePreloading(cards);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LazyLoadManager: 画像事前読み込み中にエラーが発生しました");
            Debug.LogException(ex);
            // 画像読み込みエラーは処理を継続
        }
    }

    // ----------------------------------------------------------------------
    // ExecuteSafeImagePreloading メソッド
    // 安全な画像事前読み込み処理を実行します。
    // @param cards 事前読み込み対象のカードデータ
    // ----------------------------------------------------------------------
    private async UniTask ExecuteSafeImagePreloading(List<CardModel> cards)
    {
        ValidatePreloadCards(cards);
        
        var loadTasks = CollectImageLoadTasks(cards);
        
        if (loadTasks.Count > 0)
        {
            await UniTask.WhenAll(loadTasks);
            Debug.Log($"LazyLoadManager: {loadTasks.Count}枚の画像を事前読み込みしました");
        }
    }

    // ----------------------------------------------------------------------
    // ValidatePreloadCards メソッド
    // 事前読み込みカードの妥当性を検証します。
    // @param cards 検証対象のカードデータ
    // ----------------------------------------------------------------------
    private void ValidatePreloadCards(List<CardModel> cards)
    {
        if (cards == null)
        {
            throw new ArgumentNullException(nameof(cards), "LazyLoadManager: カードデータがnullです");
        }
    }

    // ----------------------------------------------------------------------
    // CollectImageLoadTasks メソッド
    // 画像読み込みタスクを収集します。
    // @param cards カードデータ
    // @return 画像読み込みタスクのリスト
    // ----------------------------------------------------------------------
    private List<UniTask> CollectImageLoadTasks(List<CardModel> cards)
    {
        var loadTasks = new List<UniTask>();
        
        foreach (var card in cards)
        {
            if (ShouldPreloadCardImage(card))
            {
                loadTasks.Add(ImageCacheManager.Instance.LoadTextureAsync(card.imageKey, card));
            }
        }
        
        return loadTasks;
    }

    // ----------------------------------------------------------------------
    // ShouldPreloadCardImage メソッド
    // カードの画像を事前読み込みすべきかを判定します。
    // @param card 判定対象のカードデータ
    // @return 事前読み込みすべき場合はtrue
    // ----------------------------------------------------------------------
    private bool ShouldPreloadCardImage(CardModel card)
    {
        return !string.IsNullOrEmpty(card.imageKey) && card.imageTexture == null;
    }
}
