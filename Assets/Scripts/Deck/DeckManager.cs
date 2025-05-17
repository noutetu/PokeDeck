using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks; // UniTask用

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
    // 保存されているデッキリスト
    // ----------------------------------------------------------------------
    private List<DeckModel> _savedDecks = new List<DeckModel>();
    public IReadOnlyList<DeckModel> SavedDecks => _savedDecks.AsReadOnly();

    // ----------------------------------------------------------------------
    // デッキデータの保存パス
    // ----------------------------------------------------------------------
    private string SavePath => Path.Combine(Application.persistentDataPath, "decks.json");

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
    [SerializeField] private bool createSampleDecksWhenEmpty = true;
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
            // CardDatabaseの初期化完了を待機
            float startTime = Time.time;
            await CardDatabase.WaitForInitializationAsync();
            float waitTime = Time.time - startTime;

            // カードデータベースが利用可能になったらデッキを読み込む
            LoadDecks();

            // 先頭のデッキを選択
            _currentDeck = _savedDecks[0];

            // カード参照の復元
            RestoreCardReferencesInDecks();

            // パネルの初期状態は非表示
            if (deckPanel != null)
            {
                deckPanel.SetActive(false);
                _isDeckPanelVisible = false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ DeckManager初期化中にエラーが発生しました: {ex.Message}");
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
    public void SaveCurrentDeck()
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
        StartCoroutine(LoadCardTexturesAndSaveDeck());
    }

    // TODO: 処理が長い
    // ----------------------------------------------------------------------
    // カードテクスチャをロードしてデッキを保存するコルーチン
    // ----------------------------------------------------------------------
    private IEnumerator LoadCardTexturesAndSaveDeck()
    {
        // ローディングインジケータを表示
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowSuccessFeedback("デッキ画像を準備中...");
        }

        // テクスチャ読み込み対象のカードをリストアップ
        List<CardModel> cardsToLoad = new List<CardModel>();
        foreach (string cardId in _currentDeck.CardIds)
        {
            CardModel card = _currentDeck.GetCardModel(cardId);
            if (card != null && card.imageTexture == null && !string.IsNullOrEmpty(card.imageKey))
            {
                cardsToLoad.Add(card);
            }
        }

        // 実際の読み込み処理
        int loadedCount = 0;

        // ImageCacheManagerを使って画像をロード
        if (ImageCacheManager.Instance != null)
        {
            foreach (CardModel card in cardsToLoad)
            {
                // 進捗状況を表示
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowSuccessFeedback($"画像を準備中... ({loadedCount}/{cardsToLoad.Count})");
                }

                // ImageCacheManagerを使用してカード画像を読み込む
                yield return ImageCacheManager.Instance.GetCardTextureAsync(card).ToCoroutine();

                loadedCount++;
            }
        }

        // デッキをID順とカードタイプ順に並べ替え
        _currentDeck.SortCardsByTypeAndID();

        // デッキ保存前にカードモデルキャッシュを構築
        _currentDeck.RestoreCardReferences();

        // 既存のデッキを更新または新規追加
        bool found = false;
        for (int i = 0; i < _savedDecks.Count; i++)
        {
            if (_savedDecks[i].Name == _currentDeck.Name)
            {
                _savedDecks[i] = _currentDeck; // 現在のデッキ参照をそのまま使用
                found = true;
                break;
            }
        }

        if (!found)
        {
            _savedDecks.Add(_currentDeck); // 現在のデッキ参照をそのまま追加
        }

        // 全デッキをJSON形式で保存（シンプルな形式）
        SaveDecks();

        // 保存成功のフィードバックを表示
        if (FeedbackContainer.Instance != null)
        {
            string energyInfo = "";
            var energyTypes = _currentDeck.SelectedEnergyTypes;
            if (energyTypes.Count > 0)
            {
                List<string> typeNames = new List<string>();
                foreach (var et in energyTypes)
                {
                    typeNames.Add(et.ToString());
                }
                energyInfo = $"（エネルギー: {string.Join(", ", typeNames)}）";
            }

            if (cardsToLoad.Count > 0)
            {
                FeedbackContainer.Instance.ShowSuccessFeedback($"画像の準備が完了しました。デッキ '{_currentDeck.Name}' を保存しました{energyInfo}");
            }
            else
            {
                FeedbackContainer.Instance.ShowSuccessFeedback($"デッキ '{_currentDeck.Name}' を保存しました{energyInfo}");
            }
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

            // 現在のデッキが削除されたら新しいデッキを選択
            if (_currentDeck.Name == deckName)
            {
                if (_savedDecks.Count > 0)
                    _currentDeck = _savedDecks[0];
                else
                    _currentDeck = new DeckModel();
            }
            return true;
        }

        return false;
    }

    // ----------------------------------------------------------------------
    // 指定名のデッキを選択
    // ----------------------------------------------------------------------
    public bool SelectDeck(string deckName)
    {
        int index = _savedDecks.FindIndex(d => d.Name == deckName);
        if (index >= 0)
        {
            _currentDeck = _savedDecks[index];
            return true;
        }

        return false;
    }

    // ----------------------------------------------------------------------
    // すべてのデッキをJSONファイルに保存
    // ----------------------------------------------------------------------
    private void SaveDecks()
    {
        // シンプル化されたデッキデータを作成
        var simplifiedDecks = new List<SimplifiedDeck>();

        // 各デッキをシンプルな形式に変換
        foreach (var deck in _savedDecks)
        {
            // コンストラクタを使用してデッキを保存
            var simpleDeck = new SimplifiedDeck
            {
                Name = deck.Name,
                CardIds = new List<string>(deck.CardIds),
                SelectedEnergyTypes = new List<int>(),
                Memo = deck.Memo
            };

            // 選択されたエネルギータイプをintに変換して保存
            foreach (var energyType in deck.SelectedEnergyTypes)
            {
                simpleDeck.SelectedEnergyTypes.Add((int)energyType);
            }

            simplifiedDecks.Add(simpleDeck);
        }

        // JSONシリアライズ設定
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // JSONに変換して保存
        string json = JsonConvert.SerializeObject(simplifiedDecks, settings);
        File.WriteAllText(SavePath, json);
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
    // 保存されているデッキを読み込み
    // ----------------------------------------------------------------------
    private void LoadDecks()
    {
        // デッキデータの保存パスを確認
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);

                // まず簡易版としてデシリアライズを試みる
                List<SimplifiedDeck> simplifiedDecks = JsonConvert.DeserializeObject<List<SimplifiedDeck>>(json);

                if (simplifiedDecks != null && simplifiedDecks.Count > 0)
                {
                    // 簡易版が成功したら、正式なDeckオブジェクトに変換
                    _savedDecks = new List<DeckModel>();

                    foreach (var simpleDeck in simplifiedDecks)
                    {
                        DeckModel newDeck = new DeckModel
                        {
                            Name = simpleDeck.Name,
                            Memo = simpleDeck.Memo ?? "" // nullの場合は空文字列を設定
                        };

                        // カードIDを追加（カードモデル情報はRestoreCardReferencesで復元）
                        foreach (string cardId in simpleDeck.CardIds)
                        {
                            // シンプルに追加（IDのみ）
                            newDeck._AddCardId(cardId);
                        }

                        // エネルギータイプを復元（存在する場合）
                        if (simpleDeck.SelectedEnergyTypes != null && simpleDeck.SelectedEnergyTypes.Count > 0)
                        {
                            try
                            {
                                foreach (int energyTypeInt in simpleDeck.SelectedEnergyTypes)
                                {
                                    // 有効な範囲のエネルギータイプかチェック
                                    if (System.Enum.IsDefined(typeof(Enum.PokemonType), energyTypeInt))
                                    {
                                        // intからPokemonType enumに変換して追加
                                        Enum.PokemonType energyType = (Enum.PokemonType)energyTypeInt;
                                        newDeck.AddSelectedEnergyType(energyType);
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"エネルギータイプの復元中にエラーが発生しました: {ex.Message}");
                                // エラーが発生しても処理を続行
                            }
                        }

                        _savedDecks.Add(newDeck);
                    }
                }
                else
                {
                    // 簡易形式が読み込めなかった場合は新規作成
                    _savedDecks = new List<DeckModel>();

                    // デッキがなければサンプルデッキを作成
                    if (createSampleDecksWhenEmpty)
                    {
                        CreateSampleDecks();
                    }
                }

                // カードデータベースが準備されているか確認
                EnsureCardDatabaseLoaded();

                // 読み込み後の初期化処理
                InitializeLoadedDecks();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"デッキの読み込み中にエラーが発生しました: {e.Message}");
                _savedDecks = new List<DeckModel>();

                // エラーの場合もサンプルデッキを作成
                if (createSampleDecksWhenEmpty)
                {
                    CreateSampleDecks();
                }
            }
        }
        else
        {
            _savedDecks = new List<DeckModel>();

            // ファイルがない場合はサンプルデッキを作成
            if (createSampleDecksWhenEmpty)
            {
                CreateSampleDecks();
            }
        }
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
        // 全デッキのカード参照を復元
        foreach (var deck in _savedDecks)
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

    // ----------------------------------------------------------------------
    // すべてのデッキに含まれるカード画像を読み込む
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
                        // ImageCacheManagerを使用してカード画像を読み込む
                        tasks.Add(ImageCacheManager.Instance.GetCardTextureAsync(cardModel));
                        processedCards.Add(cardId);
                    }
                }
            }

            // 現在のデッキの画像を優先的に読み込む
            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }
        }

        // 次に他のすべてのデッキの画像を読み込む
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
            await UniTask.WhenAll(tasks);
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
    // デッキパネルの表示状態を切り替え
    // ----------------------------------------------------------------------
    public void ToggleDeckPanel()
    {
        if (deckPanel != null)
        {
            // デッキパネルの表示状態を切り替え
            _isDeckPanelVisible = !_isDeckPanelVisible;
            deckPanel.SetActive(_isDeckPanelVisible);
        }
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
    // 実行時にデッキパネル参照を設定するメソッド
    // ----------------------------------------------------------------------
    public void SetDeckPanel(GameObject panel)
    {
        deckPanel = panel;
        if (deckPanel != null)
        {
            deckPanel.SetActive(false);
            _isDeckPanelVisible = false;
        }
    }

    // ----------------------------------------------------------------------
    // Inspectorで設定されたサンプルデッキを作成
    // ----------------------------------------------------------------------
    private void CreateSampleDecks()
    {
        if (CardDatabase.Instance == null)
        {
            return;
        }
        try
        {
            int createdDeckCount = 0;

            // Inspectorで設定された各サンプルデッキを作成
            foreach (var sampleDeck in sampleDecks)
            {
                if (string.IsNullOrEmpty(sampleDeck.deckName))
                {
                    continue;
                }

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
                    if (addedCards < maxCardsToAdd)
                    {
                        CardModel card = CardDatabase.Instance.GetCard(cardId);
                        if (card != null)
                        {
                            newDeck._AddCardId(card.id);
                            addedCards++;
                        }
                    }
                }

                // カードが追加されたらデッキを保存
                if (addedCards > 0)
                {
                    _savedDecks.Add(newDeck);
                    createdDeckCount++;
                }
            }

            // 作成したデッキがあれば保存して最初のデッキを選択
            if (createdDeckCount > 0)
            {
                SaveDecks();
                _currentDeck = _savedDecks[0]; // 最初のサンプルデッキを現在のデッキに設定
                RestoreCardReferencesInDecks(); // カード参照を復元
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"サンプルデッキ作成中にエラーが発生しました: {ex.Message}");
        }
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

    // ----------------------------------------------------------------------
    // 保存用の簡易デッキモデル
    // ----------------------------------------------------------------------
    [System.Serializable]
    public class SimplifiedDeck
    {
        public string Name { get; set; }
        public List<string> CardIds { get; set; } = new List<string>();
        public List<int> SelectedEnergyTypes { get; set; } = new List<int>(); // 選択されたエネルギータイプ（int型で保存）
        public string Memo { get; set; } = ""; // デッキメモ
    }
}