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
    // 定数クラス
    // ----------------------------------------------------------------------
    private static class Constants
    {
        // デッキタイプ
        public const string DECK_TYPE_CURRENT = "現在のデッキ";
        public const string DECK_TYPE_SAVED = "保存済みデッキ";
        public const string DECK_TYPE_SAMPLE = "サンプルデッキ";
        public const string DECK_TYPE_COPIED = "コピーデッキ";
        
        // エラーメッセージ
        public const string ERROR_PARALLEL_LOAD = "カード画像の並列ロードに失敗しました: {0}";
        public const string ERROR_COPIED_DECK_LOAD = "コピーデッキのカード画像ロードに失敗しました: {0}";
        
        // ログメッセージ
        public const string LOG_LOADING_DECK_IMAGES = "デッキ画像読み込み: {0}種類 - {1}枚のカード";
    }
    // ----------------------------------------------------------------------
    // すべてのデッキに含まれるカード画像を読み込む（サンプルデッキも含む）
    // @param currentDeck 現在のデッキモデル
    // @param savedDecks 保存済みデッキリスト
    // @param sampleDecks サンプルデッキリスト
    // @returns 非同期タスク
    // ----------------------------------------------------------------------
    public static async UniTask LoadCardImagesForAllDecksAsync(
        DeckModel currentDeck, 
        IReadOnlyList<DeckModel> savedDecks, 
        IReadOnlyList<DeckModel> sampleDecks)
    {
        try
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
            
            Debug.Log($"全デッキの画像読み込み完了: {processedCards.Count}枚のユニークカード");
        }
        catch (Exception ex)
        {
            Debug.LogError($"すべてのデッキ画像読み込み中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // 現在のデッキの画像を優先的に読み込む
    // @param currentDeck 現在のデッキモデル
    // @param processedCards 既に処理済みのカードIDセット
    // @param tasks 読み込みタスクリスト
    // @returns 非同期タスク
    // ----------------------------------------------------------------------
    private static async UniTask LoadCurrentDeckImagesAsync(
        DeckModel currentDeck,
        HashSet<string> processedCards,
        List<UniTask> tasks)
    {
        try
        {
            if (currentDeck?.CardIds == null || currentDeck.CardIds.Count == 0)
            {
                return;
            }

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
                Debug.Log($"{Constants.LOG_LOADING_DECK_IMAGES}".Replace("{0}", Constants.DECK_TYPE_CURRENT).Replace("{1}", tasks.Count.ToString()));
                await ExecuteImageLoadTasks(tasks, Constants.DECK_TYPE_CURRENT);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"現在のデッキ画像読み込み中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // 保存済みデッキの画像を読み込む
    // @param savedDecks 保存済みデッキリスト
    // @param currentDeck 現在のデッキモデル
    // @param processedCards 既に処理済みのカードIDセット
    // @param tasks 読み込みタスクリスト
    // @returns 非同期タスク
    // ----------------------------------------------------------------------
    private static async UniTask LoadSavedDecksImagesAsync(
        IReadOnlyList<DeckModel> savedDecks,
        DeckModel currentDeck,
        HashSet<string> processedCards,
        List<UniTask> tasks)
    {
        try
        {
            if (savedDecks == null || savedDecks.Count == 0)
            {
                return;
            }

            tasks.Clear();

            foreach (var deck in savedDecks)
            {
                if (deck == currentDeck) continue; // 現在のデッキはスキップ

                LoadDeckImages(deck, processedCards, tasks);
            }

            if (tasks.Count > 0)
            {
                Debug.Log($"{Constants.LOG_LOADING_DECK_IMAGES}".Replace("{0}", Constants.DECK_TYPE_SAVED).Replace("{1}", tasks.Count.ToString()));
                await ExecuteImageLoadTasks(tasks, Constants.DECK_TYPE_SAVED);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"保存済みデッキ画像読み込み中にエラー: {ex.Message}");
            Debug.LogException(ex);
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
        try
        {
            if (sampleDecks == null || sampleDecks.Count == 0)
            {
                return;
            }
            
            tasks.Clear();

            foreach (var deck in sampleDecks)
            {
                if (deck == currentDeck) continue; // 現在のデッキはスキップ

                LoadDeckImages(deck, processedCards, tasks);
            }

            if (tasks.Count > 0)
            {
                Debug.Log($"{Constants.LOG_LOADING_DECK_IMAGES}".Replace("{0}", Constants.DECK_TYPE_SAMPLE).Replace("{1}", tasks.Count.ToString()));
                await ExecuteImageLoadTasks(tasks, Constants.DECK_TYPE_SAMPLE);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"サンプルデッキ画像読み込み中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // 単一デッキの画像読み込みタスクを準備
    // @param deck 対象デッキモデル
    // @param processedCards 既に処理済みのカードIDセット
    // @param tasks 読み込みタスクリスト
    // ----------------------------------------------------------------------
    private static void LoadDeckImages(DeckModel deck, HashSet<string> processedCards, List<UniTask> tasks)
    {
        if (deck?.CardIds == null)
        {
            return;
        }
        
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
    // @param cardId カードID
    // @param deck 対象デッキモデル
    // @param processedCards 既に処理済みのカードIDセット
    // @returns 読み込むべきならtrue
    // ----------------------------------------------------------------------
    private static bool ShouldLoadCardImage(string cardId, DeckModel deck, HashSet<string> processedCards)
    {
        return !string.IsNullOrEmpty(cardId) && 
               !processedCards.Contains(cardId);
    }

    // ----------------------------------------------------------------------
    // カードモデルが画像読み込みに有効かチェック
    // @param cardModel 対象カードモデル
    // @returns 読み込み有効ならtrue
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
    // @param tasks 読み込みタスクのリスト
    // @param deckType デッキタイプ（ログ出力用）
    // @returns 非同期タスク
    // ----------------------------------------------------------------------
    private static async UniTask ExecuteImageLoadTasks(List<UniTask> tasks, string deckType)
    {
        if (tasks == null || tasks.Count == 0)
        {
            return;
        }
        
        // すべてのタスクを並列に実行
        try
        {
            await UniTask.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            string errorMessage = string.Format(Constants.ERROR_PARALLEL_LOAD, ex.Message);
            Debug.LogError(errorMessage);
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // コピーしたデッキのカードテクスチャをロード
    // @param deck コピーしたデッキモデル
    // @returns 非同期タスク
    // ----------------------------------------------------------------------
    public static async UniTask LoadCardTexturesForCopiedDeckAsync(DeckModel deck)
    {
        try
        {
            if (deck?.CardIds == null || deck.CardIds.Count == 0)
            {
                return;
            }

            // テクスチャが未読み込みのカードを抽出
            var cardsToLoad = ExtractUnloadedCards(deck);
            if (cardsToLoad.Count == 0)
            {
                return;
            }

            // 同時に処理するタスクリスト
            var tasks = CreateImageLoadTasks(cardsToLoad);
            
            // すべてのタスクを並列に実行
            if (tasks.Count > 0)
            {
                Debug.Log($"{Constants.LOG_LOADING_DECK_IMAGES}".Replace("{0}", Constants.DECK_TYPE_COPIED).Replace("{1}", tasks.Count.ToString()));
                await ExecuteImageLoadTasks(tasks, Constants.DECK_TYPE_COPIED);
            }
        }
        catch (Exception ex)
        {
            string errorMessage = string.Format(Constants.ERROR_COPIED_DECK_LOAD, ex.Message);
            Debug.LogError(errorMessage);
            Debug.LogException(ex);
        }
    }
    
    // ----------------------------------------------------------------------
    // 未読み込みカードの抽出
    // @param deck 対象デッキモデル
    // @returns 未読み込みカードのリスト
    // ----------------------------------------------------------------------
    private static List<CardModel> ExtractUnloadedCards(DeckModel deck)
    {
        var cardsToLoad = new List<CardModel>();
        
        foreach (var cardId in deck.CardIds)
        {
            var cardModel = deck.GetCardModel(cardId);
            if (IsValidForImageLoad(cardModel))
            {
                cardsToLoad.Add(cardModel);
            }
        }
        
        return cardsToLoad;
    }
    
    // ----------------------------------------------------------------------
    // 画像読み込みタスクの作成
    // @param cardsToLoad 読み込み対象カードリスト
    // @returns 画像読み込みタスクのリスト
    // ----------------------------------------------------------------------
    private static List<UniTask> CreateImageLoadTasks(List<CardModel> cardsToLoad)
    {
        var tasks = new List<UniTask>();
        
        if (ImageCacheManager.Instance != null)
        {
            foreach (CardModel card in cardsToLoad)
            {
                tasks.Add(ImageCacheManager.Instance.GetCardTextureAsync(card));
            }
        }
        else
        {
            Debug.LogWarning("ImageCacheManagerのインスタンスがnullです");
        }
        
        return tasks;
    }
}
