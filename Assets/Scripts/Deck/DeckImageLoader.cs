using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

// ----------------------------------------------------------------------
// デッキの画像読み込み処理を専門に行うクラス
// DeckManagerから分離してコードの単一責任原則を守る
// ----------------------------------------------------------------------
public class DeckImageLoader
{
    // ----------------------------------------------------------------------
    // すべてのデッキに含まれるカード画像を読み込む（サンプルデッキも含む）
    // ----------------------------------------------------------------------
    public static async UniTask LoadCardImagesForAllDecksAsync(
        DeckModel currentDeck, 
        IReadOnlyList<DeckModel> savedDecks, 
        IReadOnlyList<DeckModel> sampleDecks)
    {
        // 重複するカードを避けるためのハッシュセット
        var processedCards = new HashSet<string>();
        var tasks = new List<UniTask>();

        // 現在のデッキの画像を優先的に読み込む
        await LoadCurrentDeckImagesAsync(currentDeck, processedCards, tasks);

        // 通常デッキの画像を読み込む
        await LoadSavedDecksImagesAsync(savedDecks, currentDeck, processedCards, tasks);

        // サンプルデッキの画像を読み込む
        await LoadSampleDecksImagesAsync(sampleDecks, currentDeck, processedCards, tasks);
    }

    // ----------------------------------------------------------------------
    // 現在のデッキの画像を優先的に読み込む
    // ----------------------------------------------------------------------
    private static async UniTask LoadCurrentDeckImagesAsync(
        DeckModel currentDeck, 
        HashSet<string> processedCards, 
        List<UniTask> tasks)
    {
        if (currentDeck?.CardIds?.Count > 0)
        {
            tasks.Clear();
            
            foreach (var cardId in currentDeck.CardIds)
            {
                if (ShouldLoadCardImage(cardId, currentDeck, processedCards))
                {
                    var cardModel = currentDeck.GetCardModel(cardId);
                    if (IsValidForImageLoad(cardModel))
                    {
                        tasks.Add(ImageCacheManager.Instance.GetCardTextureAsync(cardModel));
                        processedCards.Add(cardId);
                    }
                }
            }

            if (tasks.Count > 0)
            {
                await ExecuteImageLoadTasks(tasks, "現在のデッキ");
            }
        }
    }

    // ----------------------------------------------------------------------
    // 保存済みデッキの画像を読み込む
    // ----------------------------------------------------------------------
    private static async UniTask LoadSavedDecksImagesAsync(
        IReadOnlyList<DeckModel> savedDecks, 
        DeckModel currentDeck, 
        HashSet<string> processedCards, 
        List<UniTask> tasks)
    {
        tasks.Clear();

        foreach (var deck in savedDecks)
        {
            if (deck == currentDeck) continue; // 現在のデッキはスキップ

            LoadDeckImages(deck, processedCards, tasks);
        }

        if (tasks.Count > 0)
        {
            await ExecuteImageLoadTasks(tasks, "保存済みデッキ");
        }
    }

    // ----------------------------------------------------------------------
    // サンプルデッキの画像を読み込む
    // ----------------------------------------------------------------------
    private static async UniTask LoadSampleDecksImagesAsync(
        IReadOnlyList<DeckModel> sampleDecks, 
        DeckModel currentDeck, 
        HashSet<string> processedCards, 
        List<UniTask> tasks)
    {
        tasks.Clear();

        foreach (var deck in sampleDecks)
        {
            if (deck == currentDeck) continue; // 現在のデッキはスキップ

            LoadDeckImages(deck, processedCards, tasks);
        }

        if (tasks.Count > 0)
        {
            await ExecuteImageLoadTasks(tasks, "サンプルデッキ");
        }
    }

    // ----------------------------------------------------------------------
    // 単一デッキの画像読み込みタスクを準備
    // ----------------------------------------------------------------------
    private static void LoadDeckImages(DeckModel deck, HashSet<string> processedCards, List<UniTask> tasks)
    {
        foreach (var cardId in deck.CardIds)
        {
            if (ShouldLoadCardImage(cardId, deck, processedCards))
            {
                var cardModel = deck.GetCardModel(cardId);
                if (IsValidForImageLoad(cardModel))
                {
                    tasks.Add(ImageCacheManager.Instance.GetCardTextureAsync(cardModel));
                    processedCards.Add(cardId);
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // カード画像を読み込むべきかチェック
    // ----------------------------------------------------------------------
    private static bool ShouldLoadCardImage(string cardId, DeckModel deck, HashSet<string> processedCards)
    {
        return !processedCards.Contains(cardId);
    }

    // ----------------------------------------------------------------------
    // カードモデルが画像読み込みに有効かチェック
    // ----------------------------------------------------------------------
    private static bool IsValidForImageLoad(CardModel cardModel)
    {
        return cardModel != null && 
               cardModel.imageTexture == null && 
               !string.IsNullOrEmpty(cardModel.imageKey) && 
               ImageCacheManager.Instance != null;
    }

    // ----------------------------------------------------------------------
    // 画像読み込みタスクを実行
    // ----------------------------------------------------------------------
    private static async UniTask ExecuteImageLoadTasks(List<UniTask> tasks, string deckType)
    {
        try
        {
            await UniTask.WhenAll(tasks);
            Debug.Log($"{deckType}の画像読み込み完了: {tasks.Count}枚");
        }
        catch (Exception ex)
        {
            Debug.LogError($"{deckType}の画像読み込み中にエラー: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    // コピーしたデッキのカードテクスチャをロード
    // ----------------------------------------------------------------------
    public static async UniTask LoadCardTexturesForCopiedDeckAsync(DeckModel deck)
    {
        if (deck?.CardIds == null || deck.CardIds.Count == 0)
            return;

        // テクスチャが未読み込みのカードを抽出
        var cardsToLoad = new List<CardModel>();
        foreach (var cardId in deck.CardIds)
        {
            var cardModel = deck.GetCardModel(cardId);
            if (IsValidForImageLoad(cardModel))
            {
                cardsToLoad.Add(cardModel);
            }
        }

        if (cardsToLoad.Count == 0)
            return;

        // 同時に処理するタスクリスト
        var tasks = new List<UniTask>();

        // ImageCacheManagerを使用してテクスチャを非同期で読み込む
        if (ImageCacheManager.Instance != null)
        {
            foreach (CardModel card in cardsToLoad)
            {
                tasks.Add(ImageCacheManager.Instance.GetCardTextureAsync(card));
            }

            if (tasks.Count > 0)
            {
                try
                {
                    await UniTask.WhenAll(tasks);
                    Debug.Log($"コピーデッキの画像読み込み完了: {tasks.Count}枚");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"コピーデッキの画像読み込み中にエラー: {ex.Message}");
                }
            }
        }
    }
}
