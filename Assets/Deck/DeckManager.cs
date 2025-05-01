using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.UI; // UIコンポーネント用
using System.Linq;
using System.Collections;
using Cysharp.Threading.Tasks; // UniTask用
using UnityEngine.Networking; // UnityWebRequest用

// ----------------------------------------------------------------------
// デッキの保存・読み込みを管理するクラス
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

    // 現在選択中のデッキ
    private Deck _currentDeck;
    public Deck CurrentDeck => _currentDeck;
    
    // 保存されているデッキリスト
    private List<Deck> _savedDecks = new List<Deck>();
    public IReadOnlyList<Deck> SavedDecks => _savedDecks.AsReadOnly();

    // デッキデータの保存パス
    private string SavePath => Path.Combine(Application.persistentDataPath, "decks.json");

    // デッキ表示用のパネル参照
    [SerializeField] private GameObject deckPanel;
    public GameObject DeckPanel => deckPanel;

    // パネルが表示中かどうかのフラグ
    private bool _isDeckPanelVisible = false;
    public bool IsDeckPanelVisible => _isDeckPanelVisible;

    private void Awake()
    {
        // シングルトンの設定
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // カードデータベースが初期化されるのを待つために、起動順序を調整
        StartCoroutine(InitializeWithDelay());
    }
    
    /// <summary>
    /// カードデータベースの初期化を待ってからデッキを読み込む
    /// </summary>
    private IEnumerator InitializeWithDelay()
    {
        Debug.Log("🔄 DeckManager: カードデータベースの初期化を待機中...");
        
        // カードデータベースが初期化されるのを最大5秒待つ
        float waitTime = 0f;
        float maxWaitTime = 5f;
        
        while (CardDatabase.Instance == null && waitTime < maxWaitTime)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }
        
        if (CardDatabase.Instance == null)
        {
            Debug.LogError("❌ DeckManager: カードデータベースの初期化に失敗しました。デッキのカード情報が正しく復元されない可能性があります。");
        }
        else
        {
            Debug.Log($"✅ DeckManager: カードデータベースの初期化完了 (待機時間: {waitTime:F1}秒)");
            
            // カードデータベースに登録されているカード数をログ出力
            Debug.Log($"📊 CardDatabase: 登録済みカード数: {CardDatabase.GetAllCards().Count}枚");
        }
        
        // カードデータベースが利用可能になったらデッキを読み込む
        LoadDecks();
        
        // 常に新しいデッキを作成
        _currentDeck = new Deck { Name = "新規デッキ" };
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

    /// <summary>
    /// 新しいデッキを作成
    /// </summary>
    /// <param name="name">新しいデッキの名前</param>
    /// <returns>作成したデッキ</returns>
    public Deck CreateNewDeck(string name = "")
    {
        _currentDeck = new Deck { Name = name };
        return _currentDeck;
    }

    /// <summary>
    /// 現在のデッキを保存する
    /// </summary>
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

        // デッキをID順とカードタイプ順に並べ替え（以前はID順のみ）
        _currentDeck.SortCardsByTypeAndID();
        
        // 既存のデッキを更新または新規追加
        bool found = false;
        for (int i = 0; i < _savedDecks.Count; i++)
        {
            if (_savedDecks[i].Name == _currentDeck.Name)
            {
                _savedDecks[i] = CreateSaveDeck(_currentDeck);
                found = true;
                break;
            }
        }

        if (!found)
        {
            _savedDecks.Add(CreateSaveDeck(_currentDeck));
        }

        // 全デッキをJSON形式で保存（シンプルな形式）
        SaveDecks();
        
        // 保存成功のフィードバックを表示
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowSuccessFeedback($"デッキ '{_currentDeck.Name}' を保存しました");
        }
        
        Debug.Log($"デッキ '{_currentDeck.Name}' を保存しました（全{_savedDecks.Count}個）");
    }

    /// <summary>
    /// 保存用のシンプルなデッキ構造を作成
    /// </summary>
    private Deck CreateSaveDeck(Deck sourceDeck)
    {
        // 新しいデッキオブジェクトを作成
        Deck saveDeck = new Deck
        {
            Name = sourceDeck.Name
        };
        
        // カードIDのみをコピー
        foreach (string cardId in sourceDeck.CardIds)
        {
            saveDeck.AddCard(cardId);
        }
        
        return saveDeck;
    }
    
    /// <summary>
    /// エディタ用の一時保存（エネルギー要件のみ更新）
    /// </summary>
    private void UpdateEnergyRequirementsOnly()
    {
        // 現在のデッキのエネルギー要件を更新
        _currentDeck.UpdateEnergyRequirements();
    }

    /// <summary>
    /// 指定名のデッキを削除
    /// </summary>
    /// <param name="deckName">削除するデッキ名</param>
    /// <returns>削除に成功したかどうか</returns>
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
                    _currentDeck = new Deck();
            }
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 指定名のデッキを選択
    /// </summary>
    /// <param name="deckName">選択するデッキ名</param>
    /// <returns>選択に成功したかどうか</returns>
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

    /// <summary>
    /// すべてのデッキをJSONファイルに保存
    /// </summary>
    private void SaveDecks()
    {
        // シンプル化されたデッキデータを作成
        var simplifiedDecks = new List<SimplifiedDeck>();
        
        foreach (var deck in _savedDecks)
        {
            var simpleDeck = new SimplifiedDeck
            {
                Name = deck.Name,
                CardIds = new List<string>(deck.CardIds)
            };
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

    /// <summary>
    /// デッキを読み込んだ後の処理
    /// カード名のカウント等を再構築する
    /// </summary>
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

    /// <summary>
    /// 保存されているデッキを読み込み
    /// </summary>
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
                    _savedDecks = new List<Deck>();
                    
                    foreach (var simpleDeck in simplifiedDecks)
                    {
                        Deck newDeck = new Deck { Name = simpleDeck.Name };
                        
                        // カードIDを追加（カードモデル情報はRestoreCardReferencesで復元）
                        foreach (string cardId in simpleDeck.CardIds)
                        {
                            // シンプルに追加（IDのみ）
                            newDeck._AddCardId(cardId);
                        }
                        
                        _savedDecks.Add(newDeck);
                    }
                    
                    Debug.Log($"デッキデータを簡易形式で読み込みました: {_savedDecks.Count}個のデッキ");
                }
                else
                {
                    // 簡易形式が読み込めなかった場合は新規作成
                    _savedDecks = new List<Deck>();
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
                _savedDecks = new List<Deck>();
            }
        }
        else
        {
            _savedDecks = new List<Deck>();
            Debug.Log("デッキデータが見つかりません。新規作成します。");
        }
    }

    /// <summary>
    /// カードデータベースが読み込まれていることを確認
    /// </summary>
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

    /// <summary>
    /// デッキに含まれるカード参照を復元するメソッド
    /// カードデータベースが更新された後に呼び出すことで、デッキ内のカード参照を最新の状態に更新する
    /// </summary>
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

    /// <summary>
    /// すべてのデッキに含まれるカード画像を読み込む
    /// </summary>
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
                        tasks.Add(DownloadCardImage(cardModel));
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
                        tasks.Add(DownloadCardImage(cardModel));
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

    /// <summary>
    /// カード画像を非同期でダウンロードし設定する
    /// </summary>
    private async UniTask DownloadCardImage(CardModel card)
    {
        if (card == null || string.IsNullOrEmpty(card.imageKey))
            return;

        try
        {
            // WebRequestを使ってテクスチャを取得
            using var request = UnityWebRequestTexture.GetTexture(card.imageKey);
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // ダウンロードしたテクスチャをカードモデルに設定
                card.imageTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Debug.Log($"カード '{card.name}' の画像を読み込みました: {card.imageKey}");
            }
            else
            {
                Debug.LogWarning($"カード '{card.name}' の画像読み込みに失敗しました: {request.error}");
                AssignDefaultTexture(card);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"カード '{card.name}' の画像読み込み中に例外が発生しました: {ex.Message}");
            AssignDefaultTexture(card);
        }
    }

    /// <summary>
    /// カードにデフォルトテクスチャを設定する
    /// </summary>
    private void AssignDefaultTexture(CardModel card)
    {
        if (card == null) return;

        // 空のグレーテクスチャを作成
        var texture = new Texture2D(512, 712, TextureFormat.RGBA32, false);
        // グレーで塗りつぶし
        Color32[] colors = new Color32[texture.width * texture.height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color32(200, 200, 200, 255); // ライトグレー
        }
        texture.SetPixels32(colors);
        texture.Apply();
        
        // カードテクスチャを設定
        card.imageTexture = texture;
        Debug.Log($"カード '{card.name}' にデフォルトテクスチャを設定しました");
    }

    /// <summary>
    /// 特定のデッキのカード参照を復元
    /// </summary>
    /// <param name="deck">復元対象のデッキ</param>
    private void RestoreDeckCardReferences(Deck deck)
    {
        if (deck == null) return;

        // デッキのカード参照を復元するメソッドを呼び出す
        deck.RestoreCardReferences();
        
        // デッキの状態を再初期化
        deck.OnAfterDeserialize();
    }

    /// <summary>
    /// デッキパネルの表示状態を切り替え
    /// </summary>
    public void ToggleDeckPanel()
    {
        if (deckPanel != null)
        {
            _isDeckPanelVisible = !_isDeckPanelVisible;
            deckPanel.SetActive(_isDeckPanelVisible);
            
            // パネル表示時にログを出力
            if (_isDeckPanelVisible)
            {
                Debug.Log("デッキパネルを表示しました");
            }
        }
        else
        {
            Debug.LogWarning("デッキパネルが設定されていません");
        }
    }

    /// <summary>
    /// デッキパネルを表示
    /// </summary>
    public void ShowDeckPanel()
    {
        if (deckPanel != null && !_isDeckPanelVisible)
        {
            deckPanel.SetActive(true);
            _isDeckPanelVisible = true;
            Debug.Log("デッキパネルを表示しました");
        }
    }

    /// <summary>
    /// デッキパネルを非表示
    /// </summary>
    public void HideDeckPanel()
    {
        if (deckPanel != null && _isDeckPanelVisible)
        {
            deckPanel.SetActive(false);
            _isDeckPanelVisible = false;
            Debug.Log("デッキパネルを非表示にしました");
        }
    }
    
    /// <summary>
    /// 実行時にデッキパネル参照を設定するメソッド
    /// </summary>
    /// <param name="panel">デッキパネルのGameObject</param>
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

/// <summary>
/// 保存用の簡易デッキモデル
/// </summary>
[System.Serializable]
public class SimplifiedDeck
{
    public string Name { get; set; }
    public List<string> CardIds { get; set; } = new List<string>();
}