using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks; // UniTask用
using System; // Exception用

// ----------------------------------------------------------------------
// シリアライズ用のシンプルなデッキクラス
// DeckModelをJSONで保存するための軽量バージョン
// ----------------------------------------------------------------------
[Serializable]
public class SimplifiedDeck
{
    public string Name;
    public string Memo;
    public List<string> CardIds;
    public List<int> SelectedEnergyTypes; // Enum.PokemonTypeのint値のリスト

    // DeckModelからSimplifiedDeckを作成するコンストラクタ
    public SimplifiedDeck(DeckModel deck)
    {
        Name = deck.Name;
        Memo = deck.Memo;
        CardIds = new List<string>(deck.CardIds);
        
        // エネルギータイプをint値に変換
        SelectedEnergyTypes = new List<int>();
        foreach (var energyType in deck.SelectedEnergyTypes)
        {
            SelectedEnergyTypes.Add((int)energyType);
        }
    }

    // デフォルトコンストラクタ（JSONデシリアライゼーション用）
    public SimplifiedDeck()
    {
        Name = "";
        Memo = "";
        CardIds = new List<string>();
        SelectedEnergyTypes = new List<int>();
    }
}

// ----------------------------------------------------------------------
// デッキの保存・読み込みを管理するクラス
// シングルトンパターンを使用して、アプリケーション全体で一つのインスタンスを共有
// デッキの作成・選択・削除・保存・読み込みを行う
// デッキのカード参照を復元するメソッドも含まれる
// ----------------------------------------------------------------------
public class DeckManager : MonoBehaviour
{
    // シングルトンインスタンス
    private static DeckManager _instance;
    public static DeckManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("DeckManager");
                _instance = go.AddComponent<DeckManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ----------------------------------------------------------------------
    // 現在選択中のデッキ
    // ----------------------------------------------------------------------
    private DeckModel _currentDeck;
    public DeckModel CurrentDeck => _currentDeck;

    // ----------------------------------------------------------------------
    // 保存されているデッキリスト（ユーザー作成デッキ）
    // ----------------------------------------------------------------------
    private List<DeckModel> _savedDecks = new List<DeckModel>();
    public IReadOnlyList<DeckModel> SavedDecks => _savedDecks.AsReadOnly();

    // ----------------------------------------------------------------------
    // サンプルデッキリスト（メモリ専用、保存しない）
    // ----------------------------------------------------------------------
    private List<DeckModel> _sampleDecks = new List<DeckModel>();
    public IReadOnlyList<DeckModel> SampleDecks => _sampleDecks.AsReadOnly();

    // ----------------------------------------------------------------------
    // JSONファイルの保存パス（メインスレッドでキャッシュ）
    // ----------------------------------------------------------------------
    private string _cachedSavePath;
    private string SavePath
    {
        get
        {
            if (string.IsNullOrEmpty(_cachedSavePath))
            {
                _cachedSavePath = Path.Combine(Application.persistentDataPath, "decks.json");
            }
            return _cachedSavePath;
        }
    }

    // ----------------------------------------------------------------------
    // デッキ表示用のパネル参照
    // ----------------------------------------------------------------------
    [SerializeField] private GameObject deckPanel;
    public GameObject DeckPanel => deckPanel;

    // ----------------------------------------------------------------------
    // パネルが表示中かどうかのフラグ
    // ----------------------------------------------------------------------
    private bool _isDeckPanelVisible = false;
    public bool IsDeckPanelVisible => _isDeckPanelVisible;

    // ----------------------------------------------------------------------
    // サンプルデッキ設定（Inspector上で設定可能）
    // ----------------------------------------------------------------------
    [Header("サンプルデッキ設定")]
    [SerializeField] private List<SampleDeckConfig> sampleDecks = new List<SampleDeckConfig>();

    // ----------------------------------------------------------------------
    // Unityの初期化メソッド
    // ----------------------------------------------------------------------
    private async void Awake()
    {
        // シングルトンの設定
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 初期化をUniTaskで実行
        await InitializeAsync();
    }

    // ----------------------------------------------------------------------
    // UniTaskを使ってCardDatabaseの初期化を待機し、DeckManagerを初期化する
    // ----------------------------------------------------------------------
    private async UniTask InitializeAsync()
    {
        try
        {
            // SavePathを事前にキャッシュ（メインスレッドで実行）
            var _ = SavePath; // これによりApplication.persistentDataPathが呼び出される
            
            // 初期化開始フィードバック
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowProgressFeedback("デッキシステムを初期化中...");
            }
            
            // CardDatabaseの初期化完了を待機
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.UpdateFeedbackMessage("カードデータベースの準備中...");
            }
            float startTime = Time.time;
            await CardDatabase.WaitForInitializationAsync();
            float waitTime = Time.time - startTime;

            // カードデータベースが利用可能になったらデッキを読み込む
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.UpdateFeedbackMessage("デッキデータを読み込み中...");
            }
            await LoadSavedDecksAsync();

            // サンプルデッキを非同期で作成（起動時のみ）
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.UpdateFeedbackMessage("サンプルデッキを作成中...");
            }
            await CreateSampleDecksAsync();

            // 先頭のデッキを選択（通常デッキ優先、なければサンプルデッキ）
            SelectInitialDeck();

            // カード参照の復元
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.UpdateFeedbackMessage("カード参照を復元中...");
            }
            await RestoreCardReferencesInDecksAsync();

            // パネルの初期状態は非表示
            if (deckPanel != null)
            {
                deckPanel.SetActive(false);
                _isDeckPanelVisible = false;
            }
            
            // 初期化完了フィードバック
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.CompleteProgressFeedback("デッキシステムの初期化が完了しました", 1.0f);
            }
        }
        catch (System.Exception ex)
        {
            // デッキの初期化中にエラーが発生
            if (FeedbackContainer.Instance != null)
            {
                Debug.LogError($"DeckManagerの初期化中にエラーが発生: {ex.Message}");
                FeedbackContainer.Instance.ShowFailureFeedback("デッキの初期化中にエラーが発生しました。");
            }
        }
    }

    // ----------------------------------------------------------------------
    // 初期デッキを選択（通常デッキ優先、なければ新しい空のデッキを作成）
    // ----------------------------------------------------------------------
    private void SelectInitialDeck()
    {
        if (_savedDecks.Count > 0)
        {
            _currentDeck = _savedDecks[0];
        }
        else
        {
            // ユーザーデッキがない場合は新しい空のデッキを作成
            _currentDeck = CreateNewDeck();
        }
    }

    // ----------------------------------------------------------------------
    // 新しいデッキを作成
    // <param name="name">新しいデッキの名前</param>
    // <returns>作成したデッキ</returns>
    // ----------------------------------------------------------------------
    public DeckModel CreateNewDeck(string name = "")
    {
        _currentDeck = new DeckModel { Name = name };
        return _currentDeck;
    }

    // ----------------------------------------------------------------------
    // 現在のデッキを保存する
    // ----------------------------------------------------------------------
    public async void SaveCurrentDeck()
    {
        // カード0枚のデッキは保存しない
        if (_currentDeck.CardCount == 0)
        {
            // ユーザーにフィードバックを表示
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback("デッキが空です。カードを追加してください。");
            }
            return;
        }
        // カードが20枚より多いデッキは保存しない
        if (_currentDeck.CardCount > DeckModel.MAX_CARDS)
        {
            // ユーザーにフィードバックを表示
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback($"デッキは{DeckModel.MAX_CARDS}枚以下である必要があります。{_currentDeck.CardCount - DeckModel.MAX_CARDS}枚削除してください。");
            }
            return;
        }

        // カードテクスチャのロード処理を開始
        await LoadCardTexturesAndSaveDeckAsync();
    }

    // ----------------------------------------------------------------------
    // カードテクスチャをロードしてデッキを保存するコルーチン
    // ----------------------------------------------------------------------
    private async UniTask LoadCardTexturesAndSaveDeckAsync()
    {
        // ローディングインジケータを表示
        ShowLoadingFeedback("デッキ画像を準備中...");

        // テクスチャ読み込み対象のカードをリストアップ
        List<CardModel> cardsToLoad = GetCardsRequiringTextureLoad();

        // 画像の読み込み処理
        await LoadCardTexturesWithProgressAsync(cardsToLoad);

        // デッキの整理と保存
        PrepareDeckForSaving();
        SaveCurrentDeckToList();
        SaveDecks();

        // 成功フィードバックの表示
        ShowSaveSuccessFeedback(cardsToLoad.Count);
    }

    private void ShowLoadingFeedback(string message)
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowProgressFeedback(message);
        }
    }

    private List<CardModel> GetCardsRequiringTextureLoad()
    {
        List<CardModel> cardsToLoad = new List<CardModel>();
        foreach (string cardId in _currentDeck.CardIds)
        {
            CardModel card = _currentDeck.GetCardModel(cardId);
            if (card != null && card.imageTexture == null && !string.IsNullOrEmpty(card.imageKey))
            {
                cardsToLoad.Add(card);
            }
        }
        return cardsToLoad;
    }

    private async UniTask LoadCardTexturesWithProgressAsync(List<CardModel> cardsToLoad)
    {
        if (cardsToLoad.Count == 0)
            return;

        int loadedCount = 0;
        foreach (CardModel card in cardsToLoad)
        {
            // ImageCacheManagerを使用してテクスチャを読み込む
            if (ImageCacheManager.Instance != null)
            {
                await ImageCacheManager.Instance.GetCardTextureAsync(card);
            }

            loadedCount++;
            // プログレスを更新
            if (FeedbackContainer.Instance != null)
            {
                float progress = (float)loadedCount / cardsToLoad.Count;
                FeedbackContainer.Instance.UpdateFeedbackMessage($"デッキ画像を準備中... ({loadedCount}/{cardsToLoad.Count})");
            }
        }
    }

    // ----------------------------------------------------------------------
    // デッキを保存用に整理
    // ----------------------------------------------------------------------
    private void PrepareDeckForSaving()
    {
        // デッキをID順とカードタイプ順に並べ替え
        _currentDeck.SortCardsByTypeAndID();

        // デッキ保存前にカードモデルキャッシュを構築
        _currentDeck.RestoreCardReferences();
    }

    // ----------------------------------------------------------------------
    // 現在のデッキを保存済みデッキリストに追加または更新
    // ----------------------------------------------------------------------
    private void SaveCurrentDeckToList()
    {
        // 既存のデッキを更新または新規追加
        bool found = false;
        for (int i = 0; i < _savedDecks.Count; i++)
        {
            if (_savedDecks[i].Name == _currentDeck.Name)
            {
                _savedDecks[i] = _currentDeck;
                found = true;
                break;
            }
        }

        if (!found)
        {
            _savedDecks.Add(_currentDeck);
        }
    }

    private void ShowSaveSuccessFeedback(int loadedImageCount)
    {
        if (FeedbackContainer.Instance != null)
        {
            string message = loadedImageCount > 0 
                ? $"デッキ '{_currentDeck.Name}' を保存しました（画像{loadedImageCount}枚を読み込み）"
                : $"デッキ '{_currentDeck.Name}' を保存しました";
            FeedbackContainer.Instance.CompleteProgressFeedback(message, 1.0f);
        }
        Debug.Log($"デッキ '{_currentDeck.Name}' を保存しました");
    }

    // ----------------------------------------------------------------------
    // デッキデータをファイルに保存
    // ----------------------------------------------------------------------
    public void SaveDecks()
    {
        try
        {
            // シリアライズ用のリストを作成
            List<SimplifiedDeck> simplifiedDecks = new List<SimplifiedDeck>();
            foreach (var deck in _savedDecks)
            {
                simplifiedDecks.Add(new SimplifiedDeck(deck));
            }

            // JSONにシリアライズして保存
            string json = JsonConvert.SerializeObject(simplifiedDecks, Formatting.Indented);
            File.WriteAllText(SavePath, json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"デッキの保存中にエラーが発生: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    // 指定名のデッキを削除
    // ----------------------------------------------------------------------
    public bool DeleteDeck(string deckName)
    {
        int index = _savedDecks.FindIndex(d => d.Name == deckName);
        if (index >= 0)
        {
            _savedDecks.RemoveAt(index);
            SaveDecks();
            return true;
        }
        return false;
    }

    // ----------------------------------------------------------------------
    // 指定名のデッキを複製（同期版、内部で非同期版を呼び出す）
    // ----------------------------------------------------------------------
    public DeckModel CopyDeck(string deckName)
    {
        try
        {
            // 非同期メソッドを同期的に実行
            // 注意: メインスレッドでの呼び出しでデッドロックの可能性あり
            // UIスレッドからの呼び出しには適さない
            return CopyDeckAsync(deckName).GetAwaiter().GetResult();
        }
        catch (System.Exception ex)
        {
            // エラーハンドリング
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback($"デッキのコピーに失敗しました: {ex.Message}");
            }
            Debug.LogError($"デッキコピー中にエラーが発生: {ex.Message}");
            return null;
        }
    }

    // ----------------------------------------------------------------------
    // コピーしたデッキのカードテクスチャをロードするUniTask版メソッド
    // ----------------------------------------------------------------------
    private async UniTask LoadCardTexturesForCopiedDeckAsync(DeckModel deck)
    {
        if (deck == null)
            return;

        // テクスチャ読み込み対象のカードをリストアップ
        List<CardModel> cardsToLoad = new List<CardModel>();
        foreach (string cardId in deck.CardIds)
        {
            CardModel card = deck.GetCardModel(cardId);
            if (card != null && card.imageTexture == null && !string.IsNullOrEmpty(card.imageKey))
            {
                cardsToLoad.Add(card);
            }
        }

        // 読み込むカードがなければ終了
        if (cardsToLoad.Count == 0)
            return;

        // 同時に処理するタスクリスト
        List<UniTask> tasks = new List<UniTask>();

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
                }
                catch (Exception ex)
                {
                    Debug.LogError($"画像のロード中にエラーが発生しました: {ex.Message}");
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // 指定名のデッキを選択（通常デッキとサンプルデッキ両方から検索）
    // ----------------------------------------------------------------------
    public bool SelectDeck(string deckName)
    {
        // まず通常デッキから検索
        int index = _savedDecks.FindIndex(d => d.Name == deckName);
        if (index >= 0)
        {
            _currentDeck = _savedDecks[index];
            return true;
        }

        // 通常デッキにない場合はサンプルデッキから検索
        index = _sampleDecks.FindIndex(d => d.Name == deckName);
        if (index >= 0)
        {
            _currentDeck = _sampleDecks[index];
            return true;
        }

        return false;
    }

    // ----------------------------------------------------------------------
    // 現在のデッキがサンプルデッキかどうかを判別
    // ----------------------------------------------------------------------
    public bool IsCurrentDeckSample()
    {
        if (_currentDeck == null)
            return false;
            
        return _sampleDecks.Any(d => d.Name == _currentDeck.Name);
    }

    // ----------------------------------------------------------------------
    // 指定されたデッキがサンプルデッキかどうかを判別
    // ----------------------------------------------------------------------
    public bool IsSampleDeck(string deckName)
    {
        if (string.IsNullOrEmpty(deckName))
            return false;
            
        return _sampleDecks.Any(d => d.Name == deckName);
    }

    // ----------------------------------------------------------------------
    // 保存されているデッキを非同期で読み込み
    // ----------------------------------------------------------------------
    private async UniTask LoadSavedDecksAsync()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                await LoadDecksFromFileAsync();
            }
            else
            {
                CreateEmptyDeckList();
            }

            // 共通の後処理
            FinalizeLoadDecks();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"デッキ読み込み中にエラーが発生: {ex.Message}");
            HandleLoadDecksError();
        }
    }

    // ----------------------------------------------------------------------
    // ファイルからデッキデータを非同期で読み込み
    // ----------------------------------------------------------------------
    private async UniTask LoadDecksFromFileAsync()
    {
        try
        {
            string json = await UniTask.RunOnThreadPool(() => File.ReadAllText(SavePath));
            List<SimplifiedDeck> simplifiedDecks = await UniTask.RunOnThreadPool(() => 
                JsonConvert.DeserializeObject<List<SimplifiedDeck>>(json));

            if (simplifiedDecks != null && simplifiedDecks.Count > 0)
            {
                ConvertSimplifiedDecksToModels(simplifiedDecks);
            }
            else
            {
                CreateEmptyDeckList();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"JSON読み込み中にエラーが発生: {ex.Message}");
            CreateEmptyDeckList();
        }
    }

    // ----------------------------------------------------------------------
    // サンプルデッキを非同期で作成
    // ----------------------------------------------------------------------
    private async UniTask CreateSampleDecksAsync()
    {
        if (CardDatabase.Instance == null)
        {
            Debug.LogWarning("CardDatabaseが利用できません。サンプルデッキの作成をスキップします。");
            return;
        }

        try
        {
            int createdDeckCount = 0;
            int maxRetries = 3;

            // Inspectorで設定された各サンプルデッキを作成
            foreach (var sampleDeck in sampleDecks)
            {
                if (string.IsNullOrEmpty(sampleDeck.deckName))
                {
                    continue;
                }

                DeckModel newDeck = await CreateSampleDeckWithRetryAsync(sampleDeck, maxRetries);
                if (newDeck != null)
                {
                    _sampleDecks.Add(newDeck);
                    createdDeckCount++;
                }
            }

            Debug.Log($"サンプルデッキを{createdDeckCount}個作成しました（メモリ専用）");
            
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.UpdateFeedbackMessage($"サンプルデッキ作成完了: {createdDeckCount}個");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"サンプルデッキ作成中にエラーが発生: {ex.Message}");
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback("サンプルデッキの作成中にエラーが発生しました。");
            }
        }
    }

    // ----------------------------------------------------------------------
    // 再試行機能付きでサンプルデッキを作成
    // ----------------------------------------------------------------------
    private async UniTask<DeckModel> CreateSampleDeckWithRetryAsync(SampleDeckConfig sampleDeck, int maxRetries)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                // 新しいデッキモデルを作成
                DeckModel newDeck = new DeckModel
                {
                    Name = sampleDeck.deckName,
                    Memo = sampleDeck.deckMemo
                };

                // エネルギータイプの設定
                foreach (var energyType in sampleDeck.energyTypes)
                {
                    newDeck.AddSelectedEnergyType(energyType);
                }

                // カード検索条件に基づいてカードを追加
                int addedCards = 0;
                int maxCardsToAdd = Mathf.Min(sampleDeck.maxCards, DeckModel.MAX_CARDS);

                // カードID指定があれば追加
                foreach (string cardId in sampleDeck.specificCardIds)
                {
                    if (addedCards >= maxCardsToAdd) break;

                    var card = await GetCardWithRetryAsync(cardId, 3);
                    if (card != null)
                    {
                        newDeck._AddCardId(card.id);
                        addedCards++;
                    }
                }

                // カードが追加されていればサンプルデッキとして返す
                if (addedCards > 0)
                {
                    // サンプルデッキのカード画像をプリロード
                    await PreloadSampleDeckImagesAsync(newDeck);
                    
                    Debug.Log($"サンプルデッキ '{sampleDeck.deckName}' を作成しました: {addedCards}枚のカード（画像プリロード完了）");
                    return newDeck;
                }
                else
                {
                    Debug.LogWarning($"サンプルデッキ '{sampleDeck.deckName}' にカードを追加できませんでした（試行 {attempt + 1}/{maxRetries}）");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"サンプルデッキ '{sampleDeck.deckName}' の作成でエラー（試行 {attempt + 1}/{maxRetries}）: {ex.Message}");
            }

            // 再試行前に少し待機
            if (attempt < maxRetries - 1)
            {
                await UniTask.Delay(100);
            }
        }

        return null;
    }

    // ----------------------------------------------------------------------
    // サンプルデッキの画像をプリロード
    // ----------------------------------------------------------------------
    private async UniTask PreloadSampleDeckImagesAsync(DeckModel deck)
    {
        if (ImageCacheManager.Instance == null)
        {
            Debug.LogWarning("ImageCacheManagerが利用できません。画像プリロードをスキップします。");
            return;
        }

        try
        {
            // デッキ内のカード参照を復元
            deck.RestoreCardReferences();

            var loadTasks = new List<UniTask>();

            foreach (var cardId in deck.CardIds)
            {
                var cardModel = deck.GetCardModel(cardId);
                if (cardModel != null)
                {
                    // 既にキャッシュされている場合はスキップ
                    if (!ImageCacheManager.Instance.IsCardTextureCached(cardModel))
                    {
                        loadTasks.Add(ImageCacheManager.Instance.GetCardTextureAsync(cardModel));
                    }
                }
            }

            // すべての画像を並列ロード
            if (loadTasks.Count > 0)
            {
                await UniTask.WhenAll(loadTasks);
                Debug.Log($"サンプルデッキ '{deck.Name}' の画像を{loadTasks.Count}枚プリロードしました");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"サンプルデッキ '{deck.Name}' の画像プリロード中にエラー: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    // 再試行機能付きでカードを取得
    // ----------------------------------------------------------------------
    private async UniTask<CardModel> GetCardWithRetryAsync(string cardId, int maxRetries)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                CardModel card = CardDatabase.Instance.GetCard(cardId);
                if (card != null)
                {
                    return card;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"カード取得でエラー（試行 {attempt + 1}/{maxRetries}）: {ex.Message}");
            }

            // 再試行前に少し待機
            if (attempt < maxRetries - 1)
            {
                await UniTask.Delay(50);
            }
        }

        return null;
    }

    // ----------------------------------------------------------------------
    // デッキに含まれるカード参照を非同期で復元
    // ----------------------------------------------------------------------
    private async UniTask RestoreCardReferencesInDecksAsync()
    {
        try
        {
            // 通常デッキのカード参照を復元
            foreach (var deck in _savedDecks)
            {
                await RestoreDeckCardReferencesAsync(deck);
            }

            // サンプルデッキのカード参照を復元
            foreach (var deck in _sampleDecks)
            {
                await RestoreDeckCardReferencesAsync(deck);
            }

            // 現在選択中のデッキも更新
            if (_currentDeck != null)
            {
                await RestoreDeckCardReferencesAsync(_currentDeck);
            }

            Debug.Log("✅ 全デッキのカード参照復元が完了しました");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"カード参照復元中にエラーが発生: {ex.Message}");
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback("カード参照の復元中にエラーが発生しました。");
            }
        }
    }

    // ----------------------------------------------------------------------
    // 特定のデッキのカード参照を非同期で復元
    // ----------------------------------------------------------------------
    private async UniTask RestoreDeckCardReferencesAsync(DeckModel deck)
    {
        if (deck == null) return;

        try
        {
            // デッキのカード参照を復元するメソッドを非同期で呼び出す
            await UniTask.RunOnThreadPool(() =>
            {
                deck.RestoreCardReferences();
                deck.OnAfterDeserialize();
            });
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"デッキ '{deck.Name}' のカード参照復元でエラー: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    // SimplifiedDeckをDeckModelに変換
    // ----------------------------------------------------------------------
    private void ConvertSimplifiedDecksToModels(List<SimplifiedDeck> simplifiedDecks)
    {
        _savedDecks = new List<DeckModel>();

        foreach (var simpleDeck in simplifiedDecks)
        {
            DeckModel newDeck = CreateDeckFromSimplified(simpleDeck);
            _savedDecks.Add(newDeck);
        }

        InitializeLoadedDecks();
    }

    // ----------------------------------------------------------------------
    // SimplifiedDeckから個別のDeckModelを作成
    // ----------------------------------------------------------------------
    private DeckModel CreateDeckFromSimplified(SimplifiedDeck simpleDeck)
    {
        DeckModel newDeck = new DeckModel
        {
            Name = simpleDeck.Name,
            Memo = simpleDeck.Memo ?? ""
        };

        // カードIDを追加
        AddCardIdsToDecк(newDeck, simpleDeck.CardIds);

        // エネルギータイプを復元
        RestoreEnergyTypes(newDeck, simpleDeck.SelectedEnergyTypes);

        return newDeck;
    }

    // ----------------------------------------------------------------------
    // デッキにカードIDを追加
    // ----------------------------------------------------------------------
    private void AddCardIdsToDecк(DeckModel deck, List<string> cardIds)
    {
        foreach (string cardId in cardIds)
        {
            deck._AddCardId(cardId);
        }
    }

    // ----------------------------------------------------------------------
    // エネルギータイプを復元
    // ----------------------------------------------------------------------
    private void RestoreEnergyTypes(DeckModel deck, List<int> energyTypeInts)
    {
        if (energyTypeInts == null || energyTypeInts.Count == 0)
            return;

        try
        {
            foreach (int energyTypeInt in energyTypeInts)
            {
                if (System.Enum.IsDefined(typeof(Enum.PokemonType), energyTypeInt))
                {
                    Enum.PokemonType energyType = (Enum.PokemonType)energyTypeInt;
                    deck.AddSelectedEnergyType(energyType);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"エネルギータイプの復元中にエラーが発生: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    // 空のデッキリストを作成（サンプルデッキ作成条件を削除）
    // ----------------------------------------------------------------------
    private void CreateEmptyDeckList()
    {
        _savedDecks = new List<DeckModel>();
        // サンプルデッキは InitializeAsync で常に作成される
    }

    // ----------------------------------------------------------------------
    // デッキ読み込みエラー時の処理
    // ----------------------------------------------------------------------
    private void HandleLoadDecksError()
    {
        CreateEmptyDeckList();
    }

    // ----------------------------------------------------------------------
    // デッキ読み込み完了後の共通処理
    // ----------------------------------------------------------------------
    private void FinalizeLoadDecks()
    {
        // カードデータベースが準備されているか確認
        EnsureCardDatabaseLoaded();

        // 読み込み後の初期化処理
        InitializeLoadedDecks();
    }

    // ----------------------------------------------------------------------
    // カードデータベースが読み込まれていることを確認
    // ----------------------------------------------------------------------
    private void EnsureCardDatabaseLoaded()
    {
        if (CardDatabase.Instance == null)
        {
            return;
        }

        // デッキから参照されているすべてのカードIDを集める
        HashSet<string> allCardIds = new HashSet<string>();
        foreach (var deck in _savedDecks)
        {
            foreach (var cardId in deck.CardIds)
            {
                allCardIds.Add(cardId);
            }
        }

        // カードデータベースに存在しないカードを特定
        List<string> missingCardIds = new List<string>();
        foreach (var cardId in allCardIds)
        {
            if (CardDatabase.Instance.GetCard(cardId) == null)
            {
                missingCardIds.Add(cardId);
            }
        }
    }

    // ----------------------------------------------------------------------
    /// デッキに含まれるカード参照を復元するメソッド
    /// カードデータベースが更新された後に呼び出すことで、デッキ内のカード参照を最新の状態に更新する
    // ----------------------------------------------------------------------
    public async void RestoreCardReferencesInDecks()
    {
        // 通常デッキのカード参照を復元
        foreach (var deck in _savedDecks)
        {
            RestoreDeckCardReferences(deck);
        }

        // サンプルデッキのカード参照を復元
        foreach (var deck in _sampleDecks)
        {
            RestoreDeckCardReferences(deck);
        }

        // 現在選択中のデッキも更新
        if (_currentDeck != null)
        {
            RestoreDeckCardReferences(_currentDeck);
        }

        // すべてのデッキのカード画像を読み込む
        await LoadCardImagesForAllDecks();
    }

    // TODO　長すぎ
    // ----------------------------------------------------------------------
    // すべてのデッキに含まれるカード画像を読み込む（サンプルデッキも含む）
    // ----------------------------------------------------------------------
    private async UniTask LoadCardImagesForAllDecks()
    {
        // 重複するカードを避けるためのハッシュセット
        var processedCards = new HashSet<string>();
        var tasks = new List<UniTask>();

        // まず現在のデッキの画像を読み込む（優先度高）
        if (_currentDeck != null && _currentDeck.CardIds.Count > 0)
        {
            foreach (var cardId in _currentDeck.CardIds)
            {
                // 重複チェック
                if (!processedCards.Contains(cardId))
                {
                    var cardModel = _currentDeck.GetCardModel(cardId);
                    if (cardModel != null && cardModel.imageTexture == null && !string.IsNullOrEmpty(cardModel.imageKey))
                    {
                        // ImageCacheManagerが存在するか確認
                        if (ImageCacheManager.Instance == null)
                        {
                            continue;
                        }

                        // ImageCacheManagerを使用してカード画像を読み込む
                        tasks.Add(ImageCacheManager.Instance.GetCardTextureAsync(cardModel));
                        processedCards.Add(cardId);
                    }
                }
            }

            // 現在のデッキの画像を優先的に読み込む
            if (tasks.Count > 0)
            {
                try
                {
                    await UniTask.WhenAll(tasks);
                }
                catch (Exception)
                {
                    // エラー処理
                }
            }
        }

        // 次に通常デッキの画像を読み込む
        tasks.Clear();
        int otherDeckCardCount = 0;

        foreach (var deck in _savedDecks)
        {
            // 現在のデッキはスキップ（既に処理済み）
            if (deck == _currentDeck) continue;

            foreach (var cardId in deck.CardIds)
            {
                // 重複チェック
                if (!processedCards.Contains(cardId))
                {
                    var cardModel = deck.GetCardModel(cardId);
                    if (cardModel != null && cardModel.imageTexture == null && !string.IsNullOrEmpty(cardModel.imageKey))
                    {
                        // ImageCacheManagerが存在するか確認
                        if (ImageCacheManager.Instance == null)
                        {
                            continue;
                        }

                        // ImageCacheManagerを使用してカード画像を読み込む
                        tasks.Add(ImageCacheManager.Instance.GetCardTextureAsync(cardModel));
                        processedCards.Add(cardId);
                        otherDeckCardCount++;
                    }
                }
            }
        }

        // サンプルデッキの画像を読み込む
        foreach (var deck in _sampleDecks)
        {
            // 現在のデッキはスキップ（既に処理済み）
            if (deck == _currentDeck) continue;

            foreach (var cardId in deck.CardIds)
            {
                // 重複チェック
                if (!processedCards.Contains(cardId))
                {
                    var cardModel = deck.GetCardModel(cardId);
                    if (cardModel != null && cardModel.imageTexture == null && !string.IsNullOrEmpty(cardModel.imageKey))
                    {
                        // ImageCacheManagerが存在するか確認
                        if (ImageCacheManager.Instance == null)
                        {
                            continue;
                        }

                        // ImageCacheManagerを使用してカード画像を読み込む
                        tasks.Add(ImageCacheManager.Instance.GetCardTextureAsync(cardModel));
                        processedCards.Add(cardId);
                        otherDeckCardCount++;
                    }
                }
            }
        }

        // 他のデッキの画像を読み込む
        if (tasks.Count > 0)
        {
            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch (Exception)
            {
                // エラー処理
            }
        }
    }

    // ----------------------------------------------------------------------
    // 特定のデッキのカード参照を復元
    // ----------------------------------------------------------------------
    private void RestoreDeckCardReferences(DeckModel deck)
    {
        if (deck == null) return;

        // デッキのカード参照を復元するメソッドを呼び出す
        deck.RestoreCardReferences();

        // デッキの状態を再初期化
        deck.OnAfterDeserialize();
    }


    // ----------------------------------------------------------------------
    // デッキパネルを表示
    // ----------------------------------------------------------------------
    public void ShowDeckPanel()
    {
        // デッキパネルが非表示の場合のみ表示
        if (deckPanel != null && !_isDeckPanelVisible)
        {
            deckPanel.SetActive(true);
            _isDeckPanelVisible = true;
        }
    }

    // ----------------------------------------------------------------------
    // デッキパネルを非表示
    // ----------------------------------------------------------------------
    public void HideDeckPanel()
    {
        // デッキパネルが表示中の場合のみ非表示
        if (deckPanel != null && _isDeckPanelVisible)
        {
            deckPanel.SetActive(false);
            _isDeckPanelVisible = false;
        }
    }

    // ----------------------------------------------------------------------
    // デッキを読み込んだ後の処理
    // カード名のカウント等を再構築する
    // ----------------------------------------------------------------------
    private void InitializeLoadedDecks()
    {
        // 各デッキに対して読み込み後の初期化処理
        foreach (var deck in _savedDecks)
        {
            deck.OnAfterDeserialize();
            deck.SortCardsByID(); // 読み込み後にカードをID順に並べ替え
        }

        // 現在のデッキも初期化
        if (_currentDeck != null)
        {
            _currentDeck.OnAfterDeserialize();
            _currentDeck.SortCardsByID(); // 現在のデッキもID順に並べ替え
        }
    }

    // ----------------------------------------------------------------------
    // 画像を読み込み、UIを更新するUniTask版メソッド
    // ----------------------------------------------------------------------
    private async UniTask LoadCardTexturesAndRefreshUIAsync(DeckModel deck, bool showFeedback = true)
    {
        if (deck == null)
            return;
            
        // まず画像をロード
        await LoadCardTexturesForCopiedDeckAsync(deck);
        
        // 画像読み込み完了後にUIを更新
        DeckListPanel deckListPanel = FindFirstObjectByType<DeckListPanel>();
        if (deckListPanel != null)
        {
            deckListPanel.RefreshDeckList();
        }
        
        // フィードバックメッセージ表示（通常デッキからのコピーの場合のみ）
        if (showFeedback && FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowSuccessFeedback($"デッキ '{deck.Name}' のコピーが完了しました");
        }
    }

    // ----------------------------------------------------------------------
    // 指定名のデッキを複製（非同期版）
    // ----------------------------------------------------------------------
    public async UniTask<DeckModel> CopyDeckAsync(string deckName)
    {
        try
        {
            DeckModel sourceDeck = null;
            bool isFromSampleDeck = false;
            
            // まず通常デッキから検索
            int index = _savedDecks.FindIndex(d => d.Name == deckName);
            if (index >= 0)
            {
                sourceDeck = _savedDecks[index];
                isFromSampleDeck = false;
            }
            else
            {
                // 通常デッキにない場合はサンプルデッキから検索
                index = _sampleDecks.FindIndex(d => d.Name == deckName);
                if (index >= 0)
                {
                    sourceDeck = _sampleDecks[index];
                    isFromSampleDeck = true;
                }
            }

            if (sourceDeck != null)
            {
                // 新しいデッキを作成
                DeckModel newDeck = new DeckModel
                {
                    Name = sourceDeck.Name,
                    Memo = sourceDeck.Memo
                };

                // カードをコピー
                foreach (string cardId in sourceDeck.CardIds)
                {
                    newDeck._AddCardId(cardId);
                }

                // エネルギータイプをコピー
                foreach (var energyType in sourceDeck.SelectedEnergyTypes)
                {
                    newDeck.AddSelectedEnergyType(energyType);
                }

                // 重複しない名前を生成
                string newName = newDeck.Name;
                int copyNumber = 2;
                while (_savedDecks.Any(d => d.Name == newName))
                {
                    newName = $"{newDeck.Name}のコピー{copyNumber}";
                    copyNumber++;
                }
                newDeck.Name = newName;

                // 複製したデッキを通常デッキリストに保存（サンプルデッキからのコピーも通常デッキとして保存）
                _savedDecks.Add(newDeck);
                SaveDecks();
                
                // 画像を読み込んでからUIを更新
                // サンプルデッキからのコピーの場合は、DeckListItemで別途フィードバックを表示するため、ここではフィードバックを無効にする
                await LoadCardTexturesAndRefreshUIAsync(newDeck, !isFromSampleDeck);

                // サンプルデッキからのコピーの場合は、通常デッキパネルも更新
                if (isFromSampleDeck)
                {
                    // DeckListPanelの更新を明示的に行う
                    DeckListPanel normalDeckListPanel = FindFirstObjectByType<DeckListPanel>();
                    if (normalDeckListPanel != null)
                    {
                        normalDeckListPanel.RefreshDeckList();
                    }
                }
                
                return newDeck;
            }
        }
        catch (System.Exception ex)
        {
            // エラーハンドリング
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback($"デッキのコピーに失敗しました: {ex.Message}");
            }
        }

        return null;
    }

    // ----------------------------------------------------------------------
    // メモリ管理：サンプルデッキのテクスチャを解放
    // ----------------------------------------------------------------------
    private void ReleaseSampleDeckTextures()
    {
        foreach (var deck in _sampleDecks)
        {
            foreach (var cardId in deck.CardIds)
            {
                var cardModel = deck.GetCardModel(cardId);
                if (cardModel?.imageTexture != null)
                {
                    // テクスチャの解放はImageCacheManagerに委ねる
                    cardModel.imageTexture = null;
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // オブジェクト破棄時の処理
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        ReleaseSampleDeckTextures();
    }

    // ----------------------------------------------------------------------
    // サンプルデッキ設定（Inspector上で設定可能）
    // ----------------------------------------------------------------------
    [System.Serializable]
    public class SampleDeckConfig
    {
        [Tooltip("サンプルデッキの名前")]
        public string deckName = "";

        [Tooltip("サンプルデッキの説明（メモ）")]
        [TextArea(2, 5)]
        public string deckMemo = "";

        [Tooltip("デッキに設定するエネルギータイプ（最大2つ）")]
        public List<Enum.PokemonType> energyTypes = new List<Enum.PokemonType>();

        [Tooltip("デッキに含める特定のカードID")]
        public List<string> specificCardIds = new List<string>();

        [Tooltip("デッキに追加するカードの最大数")]
        [Range(1, 60)]
        public int maxCards = 20;
    }
}