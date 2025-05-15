using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
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
        Debug.Log("🔄 DeckManager: 初期化を開始します");
        
        try
        {
            // CardDatabaseの初期化完了を待機
            float startTime = Time.time;
            await CardDatabase.WaitForInitializationAsync();
            float waitTime = Time.time - startTime;
            
            // CardDatabaseが正しく初期化されているか確認
            if (CardDatabase.Instance != null)
            {
                Debug.Log($"✅ DeckManager: カードデータベースの初期化完了 (待機時間: {waitTime:F1}秒)");
                
                // カードデータベースに登録されているカード数をログ出力
                Debug.Log($"📊 CardDatabase: 登録済みカード数: {CardDatabase.GetAllCards().Count}枚");
            }
            else
            {
                Debug.LogError("❌ DeckManager: カードデータベースの初期化に失敗しました。デッキのカード情報が正しく復元されない可能性があります。");
            }
            
            // カードデータベースが利用可能になったらデッキを読み込む
            LoadDecks();
            
            // 常に新しいデッキを作成
            _currentDeck = new DeckModel { Name = "新規デッキ" };
            Debug.Log("📝 DeckManager: 新規デッキを作成しました");
            
            // 既存のデッキが存在する場合はカード参照を復元
            if (_savedDecks.Count > 0)
            {
                // カード参照の復元
                RestoreCardReferencesInDecks();
                Debug.Log($"既存デッキ {_savedDecks.Count}個のカード参照を復元しました");
            }
            
            Debug.Log($"✅ DeckManager初期化完了: {_savedDecks.Count}個のデッキが読み込まれ、新規デッキが作成されました");

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
            Debug.Log($"デッキ '{_currentDeck.Name}' にカードが含まれていないため保存をスキップします");
            
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
            Debug.Log($"デッキ '{_currentDeck.Name}' のカード数が{_currentDeck.CardCount}枚で、{DeckModel.MAX_CARDS}枚を超えているため保存できません");
            
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
    
    // ----------------------------------------------------------------------
    // カードテクスチャをロードしてデッキを保存するコルーチン
    // ----------------------------------------------------------------------
    private IEnumerator LoadCardTexturesAndSaveDeck()
    {
        // ロード開始ログ
        Debug.Log($"デッキ '{_currentDeck.Name}' のカード画像読み込みを開始...");
        
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
        
        // 読み込み対象カード数をログ出力
        Debug.Log($"読み込み対象カード: {cardsToLoad.Count}枚");
        
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
                Debug.Log($"カード画像を読み込みました: {card.name} ({loadedCount}/{cardsToLoad.Count})");
            }
        }
        else
        {
            Debug.LogError("ImageCacheManagerが見つかりません。カード画像を読み込めませんでした。");
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
        
        Debug.Log($"デッキ '{_currentDeck.Name}' の保存が完了しました（読み込んだ画像: {loadedCount}枚）");
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
            Debug.Log($"デッキ '{deckName}' を選択しました");
            return true;
        }
        
        Debug.LogWarning($"デッキ '{deckName}' が見つかりません");
        return false;
    }

    // ----------------------------------------------------------------------
    // すべてのデッキをJSONファイルに保存
    // ----------------------------------------------------------------------
    private void SaveDecks()
    {
        // シンプル化されたデッキデータを作成
        var simplifiedDecks = new List<SimplifiedDeck>();
        
        foreach (var deck in _savedDecks)
        {
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
        
        // JSONシリアライズ設定（きれいに整形）
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        
        // JSONに変換して保存
        string json = JsonConvert.SerializeObject(simplifiedDecks, settings);
        File.WriteAllText(SavePath, json);
        Debug.Log($"デッキデータを保存しました: {SavePath}");
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
                        DeckModel newDeck = new DeckModel { 
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
                                    else
                                    {
                                        Debug.LogWarning($"無効なエネルギータイプ値: {energyTypeInt}、スキップします");
                                    }
                                }
                                
                                Debug.Log($"デッキ '{newDeck.Name}' からエネルギータイプを {newDeck.SelectedEnergyTypes.Count} 個復元しました");
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"エネルギータイプの復元中にエラーが発生しました: {ex.Message}");
                                // エラーが発生しても処理を続行
                            }
                        }
                        
                        _savedDecks.Add(newDeck);
                    }
                    
                    Debug.Log($"デッキデータを簡易形式で読み込みました: {_savedDecks.Count}個のデッキ");
                }
                else
                {
                    // 簡易形式が読み込めなかった場合は新規作成
                    _savedDecks = new List<DeckModel>();
                    Debug.LogWarning("デッキデータを読み込めませんでした。新規作成します。");
                }
                
                // カードデータベースが準備されているか確認
                EnsureCardDatabaseLoaded();
                
                // 読み込み後の初期化処理
                InitializeLoadedDecks();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"デッキデータの読み込み中にエラーが発生しました: {e.Message}");
                _savedDecks = new List<DeckModel>();
            }
        }
        else
        {
            _savedDecks = new List<DeckModel>();
            Debug.Log("デッキデータが見つかりません。新規作成します。");
        }
    }

    // ----------------------------------------------------------------------
    // カードデータベースが読み込まれていることを確認
    // ----------------------------------------------------------------------
    private void EnsureCardDatabaseLoaded()
    {
        if (CardDatabase.Instance == null)
        {
            Debug.LogWarning("CardDatabaseインスタンスが初期化されていません。カード参照の復元が遅延される場合があります。");
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

        if (missingCardIds.Count > 0)
        {
            Debug.LogWarning($"カードデータベースに存在しないカードが {missingCardIds.Count} 個あります。これらのカードはデッキに表示されない可能性があります。");
            
            // ここでAllCardModelやAllCardPresenterと連携して、不足しているカードを読み込む処理を追加することも可能
        }
    }

    // ----------------------------------------------------------------------
    /// デッキに含まれるカード参照を復元するメソッド
    /// カードデータベースが更新された後に呼び出すことで、デッキ内のカード参照を最新の状態に更新する
    // ----------------------------------------------------------------------
    public async void RestoreCardReferencesInDecks()
    {
        if (CardDatabase.Instance == null)
        {
            Debug.LogError("CardDatabaseが利用できないため、カード参照を復元できません。");
            return;
        }

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

        Debug.Log("すべてのデッキのカード参照とカード画像を復元しました。");
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
                Debug.Log($"現在のデッキ '{_currentDeck.Name}' のカード画像 {tasks.Count}枚を読み込み中...");
                await UniTask.WhenAll(tasks);
                Debug.Log($"現在のデッキ '{_currentDeck.Name}' のカード画像の読み込みが完了しました。");
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
            Debug.Log($"他のデッキのカード画像 {tasks.Count}枚を読み込み中...");
            await UniTask.WhenAll(tasks);
            Debug.Log($"他のデッキのカード画像の読み込みが完了しました。");
        }
        
        Debug.Log($"すべてのデッキのカード画像 合計{processedCards.Count}枚の読み込みが完了しました。");
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
            _isDeckPanelVisible = !_isDeckPanelVisible;
            deckPanel.SetActive(_isDeckPanelVisible);
        }
        else
        {
            Debug.LogWarning("デッキパネルが設定されていません");
        }
    }

    // ----------------------------------------------------------------------
    // デッキパネルを表示
    // ----------------------------------------------------------------------
    public void ShowDeckPanel()
    {
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