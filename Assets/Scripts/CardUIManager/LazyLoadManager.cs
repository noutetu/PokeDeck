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
    private readonly AllCardPresenter presenter;
    private readonly int initialCardCount;
    private readonly int baseBatchSize;
    private readonly float scrollThreshold;
    
    private List<CardModel> remainingCards = new List<CardModel>();
    private bool isLoadingBatch = false;
    private bool ignoreScrollEvent = false;
    private Vector2 lastPosition = Vector2.zero;
    private float lastScrollTime = 0f;
    private float scrollCooldown = 0.1f;
    private int dynamicBatchSize;
    
    public event Action<List<CardModel>> OnCardsFiltered;
    
    public LazyLoadManager(AllCardPresenter presenter, int initialCardCount = 30, int batchSize = 20, float scrollThreshold = 0.7f)
    {
        this.presenter = presenter;
        this.initialCardCount = initialCardCount;
        this.baseBatchSize = batchSize;
        this.dynamicBatchSize = batchSize;
        this.scrollThreshold = scrollThreshold;
    }
    
    public void InitializeWithCards(List<CardModel> allCards)
    {
        int displayCount = Mathf.Min(initialCardCount, allCards.Count);
        List<CardModel> initialCards = allCards.GetRange(0, displayCount);
        
        if (allCards.Count > displayCount)
        {
            remainingCards = allCards.GetRange(displayCount, allCards.Count - displayCount);
        }
        
        presenter.LoadCards(initialCards);
    }
    
    public void SetFilteredCards(List<CardModel> filteredCards)
    {
        presenter.ClearCards();
        
        int displayCount = Mathf.Min(initialCardCount, filteredCards.Count);
        List<CardModel> initialCards = new List<CardModel>();
        
        if (displayCount > 0)
        {
            initialCards = filteredCards.GetRange(0, displayCount);
        }
        
        presenter.LoadCards(initialCards);
        
        if (filteredCards.Count > displayCount)
        {
            remainingCards = filteredCards.GetRange(displayCount, filteredCards.Count - displayCount);
        }
        else
        {
            remainingCards.Clear();
        }
        
        ignoreScrollEvent = true;
        OnCardsFiltered?.Invoke(filteredCards);
    }
    
    public void OnScrollValueChanged(Vector2 position)
    {
        if (ignoreScrollEvent)
        {
            ignoreScrollEvent = false;
            return;
        }
        
        if (!ShouldProcessScroll(position))
            return;
            
        AdjustBatchSize(position);
        
        if (position.y < (1.0f - scrollThreshold))
        {
            LoadNextBatchAsync().Forget();
        }
    }
    
    private bool ShouldProcessScroll(Vector2 position)
    {
        float currentTime = Time.time;
        if (currentTime - lastScrollTime < scrollCooldown)
            return false;
            
        lastScrollTime = currentTime;
        return remainingCards.Count > 0 && !isLoadingBatch;
    }
    
    private void AdjustBatchSize(Vector2 position)
    {
        float scrollDirection = position.y - lastPosition.y;
        lastPosition = position;
        float scrollSpeed = Mathf.Abs(scrollDirection);
        
        if (scrollSpeed > 0.05f)
        {
            dynamicBatchSize = Mathf.Clamp(dynamicBatchSize + 5, 5, 30);
        }
        else
        {
            dynamicBatchSize = Mathf.Clamp(dynamicBatchSize - 1, 5, 30);
        }
    }
    
    private async UniTaskVoid LoadNextBatchAsync()
    {
        if (isLoadingBatch || remainingCards.Count == 0)
            return;
            
        isLoadingBatch = true;
        
        try
        {
            int batchSize = Mathf.Min(dynamicBatchSize, remainingCards.Count);
            List<CardModel> nextBatch = remainingCards.GetRange(0, batchSize);
            
            await LoadBatchInSubGroups(nextBatch);
            
            remainingCards.RemoveRange(0, batchSize);
        }
        finally
        {
            isLoadingBatch = false;
        }
    }
    
    private async UniTask LoadBatchInSubGroups(List<CardModel> batch)
    {
        int subBatchSize = 3;
        
        for (int i = 0; i < batch.Count; i += subBatchSize)
        {
            int count = Mathf.Min(subBatchSize, batch.Count - i);
            var subBatch = batch.GetRange(i, count);
            
            await PreloadImages(subBatch);
            await presenter.AddCardsAsync(subBatch);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }
    
    private async UniTask PreloadImages(List<CardModel> cards)
    {
        var loadTasks = new List<UniTask>();
        foreach (var card in cards)
        {
            if (!string.IsNullOrEmpty(card.imageKey) && card.imageTexture == null)
            {
                loadTasks.Add(ImageCacheManager.Instance.LoadTextureAsync(card.imageKey, card));
            }
        }
        
        if (loadTasks.Count > 0)
        {
            await UniTask.WhenAll(loadTasks);
        }
    }
}
