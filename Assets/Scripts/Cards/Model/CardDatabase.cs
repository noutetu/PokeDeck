using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;  // UniTask用

// ----------------------------------------------------------------------
// カードデータを一元管理するシングルトンクラス
// このクラスは、ゲーム内で使用される全てのカードデータを一元的に管理します。
// シングルトンパターンを採用しており、アプリケーション全体で唯一のインスタンスとして動作します。
// 主な機能:
// - カードデータのキャッシュ管理（IDや名前での検索が可能）
// - カードデータの永続化（JSON形式で保存・読み込み）
// - 初期化状態の管理とイベント通知
// - デバッグ用のキャッシュクリアやデータリセット機能
// このクラスを利用することで、カードデータの効率的な管理とアクセスが可能になります。
// ----------------------------------------------------------------------

public class CardDatabase : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // シングルトンインスタンス管理
    // ----------------------------------------------------------------------
    private static CardDatabase _instance;
    public static CardDatabase Instance
    {
        get
        {
            // すでにインスタンスが存在する場合はそれを返す
            if (_instance != null)
                return _instance;
                
            // インスタンスがなければシーン内から検索
            _instance = FindFirstObjectByType<CardDatabase>();
            
            // それでも見つからない場合だけ新規作成
            if (_instance == null)
            {
                var go = new GameObject("CardDatabase");
                _instance = go.AddComponent<CardDatabase>();
                DontDestroyOnLoad(go);
            }
            
            return _instance;
        }
    }
    
    // ----------------------------------------------------------------------
    // データキャッシュとストレージ
    // ----------------------------------------------------------------------
    // カードデータのキャッシュ（ID → CardModel）
    private Dictionary<string, CardModel> cardCache = new Dictionary<string, CardModel>();
    
    // カード名のキャッシュ（カード名 → カードIDのリスト）
    private Dictionary<string, List<string>> nameToIdMap = new Dictionary<string, List<string>>();
    
    // カードデータの保存パス
    private string SavePath => Path.Combine(Application.persistentDataPath, Constants.DATABASE_FILENAME);
    
    // ----------------------------------------------------------------------
    // 定数定義
    // ----------------------------------------------------------------------
    private static class Constants
    {
        public const float INITIALIZATION_TIMEOUT_SECONDS = 5f;
        public const float INITIALIZATION_PROGRESS = 0.8f;
        public const string DATABASE_FILENAME = "card_database.json";
        public const string BACKUP_EXTENSION = ".backup";
    }

    // ----------------------------------------------------------------------
    // 初期化と状態管理
    // ----------------------------------------------------------------------

    private bool isInitialized = false;                         // 初期化済みフラグ
    public static event System.Action OnDatabaseInitialized;    // 初期化完了イベント
    
    // ----------------------------------------------------------------------
    // Jsonシリアライズ設定
    // ----------------------------------------------------------------------
    private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        PreserveReferencesHandling = PreserveReferencesHandling.None,
        TypeNameHandling = TypeNameHandling.None,
        ContractResolver = new UnityContractResolver()
    };

    // ----------------------------------------------------------------------
    // Unity ライフサイクルメソッド
    // ----------------------------------------------------------------------
    private void Awake()
    {
        if (!InitializeSingleton())
            return;

        InitializeDatabase();
    }

    // ----------------------------------------------------------------------
    // シングルトンの初期化
    // ----------------------------------------------------------------------
    private bool InitializeSingleton()
    {
        // シングルトンチェック - 複数のインスタンスが作成された場合は破棄する
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return false;
        }
        
        // このインスタンスをシングルトンとして設定
        _instance = this;
        DontDestroyOnLoad(gameObject);
        return true;
    }

    // ----------------------------------------------------------------------
    // データベースの初期化処理
    // ----------------------------------------------------------------------
    private void InitializeDatabase()
    {
        ShowInitializationStartFeedback();
        LoadCardDatabase();
        CompleteInitialization();
        ShowInitializationCompleteFeedback();
    }

    // ----------------------------------------------------------------------
    // 初期化開始フィードバック表示
    // ----------------------------------------------------------------------
    private void ShowInitializationStartFeedback()
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowProgressFeedback("カードデータベースを初期化中...");
        }
    }

    // ----------------------------------------------------------------------
    // 初期化完了処理
    // ----------------------------------------------------------------------
    private void CompleteInitialization()
    {
        isInitialized = true;
        OnDatabaseInitialized?.Invoke();
    }

    // ----------------------------------------------------------------------
    // 初期化完了フィードバック表示
    // ----------------------------------------------------------------------
    private void ShowInitializationCompleteFeedback()
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.CompleteProgressFeedback("カードデータベースの初期化が完了しました", Constants.INITIALIZATION_PROGRESS);
        }
    }
    
    // ----------------------------------------------------------------------
    // カードをキャッシュに追加する
    // ----------------------------------------------------------------------
    public void RegisterCard(CardModel card, bool saveImmediately = false)
    {
        if (card == null)
            return;
            
        RegisterCardInCache(card);
        
        // データベースが変更されたので保存（オプションが有効な場合のみ）
        if (saveImmediately)
        {
            SaveCardDatabase();
        }
    }
    
    // ----------------------------------------------------------------------
    /// 複数のカードをキャッシュに一括登録
    // ----------------------------------------------------------------------
    public static void SetCachedCards(List<CardModel> cards)
    {
        if (cards == null) return;
        
        foreach (var card in cards)
        {
            Instance.RegisterCard(card);
        }
        
        // データベースが変更されたので保存
        Instance.SaveCardDatabase();
    }

    // ----------------------------------------------------------------------
    // カードIDからカードデータを取得
    // ----------------------------------------------------------------------
    public CardModel GetCard(string cardId)
    {
        if (cardCache.TryGetValue(cardId, out CardModel card))
        {
            return card;
        }
        return null;
    }
    
    // ----------------------------------------------------------------------
    /// 全カードを取得
    // ----------------------------------------------------------------------
    public static List<CardModel> GetAllCards()
    {
        List<CardModel> result = new List<CardModel>();

        foreach (var card in Instance.cardCache.Values)
        {
            result.Add(card);
        }

        return result;
    }
    
    // ----------------------------------------------------------------------
    /// カードデータベースをJSONファイルに保存
    // ----------------------------------------------------------------------
    public void SaveCardDatabase()
    {
        try
        {
            if (!CanSaveDatabase())
                return;
            
            var cardsToSave = PrepareCardsForSave();
            SaveCardsToFile(cardsToSave);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"カードデータベースの保存に失敗しました: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    /// 保存可能かチェック
    // ----------------------------------------------------------------------
    private bool CanSaveDatabase()
    {
        // 空のデータベースを保存しないように確認
        return cardCache.Count > 0;
    }

    // ----------------------------------------------------------------------
    /// 保存用カードデータを準備
    // ----------------------------------------------------------------------
    private List<SerializableCardModel> PrepareCardsForSave()
    {
        List<SerializableCardModel> cardsToSave = new List<SerializableCardModel>();
        
        foreach (var card in cardCache.Values)
        {
            var serializableCard = CreateSerializableCard(card);
            cardsToSave.Add(serializableCard);
        }
        
        return cardsToSave;
    }

    // ----------------------------------------------------------------------
    /// SerializableCardModelを作成
    // ----------------------------------------------------------------------
    private SerializableCardModel CreateSerializableCard(CardModel card)
    {
        return new SerializableCardModel
        {
            id = card.id,
            name = card.name,
            cardType = card.cardType,
            evolutionStage = card.evolutionStage,
            pack = card.pack,
            hp = card.hp,
            type = card.type,
            weakness = card.weakness,
            retreatCost = card.retreatCost,
            maxMoveEnergy = card.maxEnergyCost,
            abilityName = card.abilityName,
            abilityEffect = card.abilityEffect,
            moves = card.moves,
            tags = card.tags,
            maxDamage = card.maxDamage,
            imageKey = card.imageKey
            // imageTextureは保存しない（ランタイムデータ）
        };
    }

    // ----------------------------------------------------------------------
    /// ファイルに保存
    // ----------------------------------------------------------------------
    private void SaveCardsToFile(List<SerializableCardModel> cardsToSave)
    {
        string json = JsonConvert.SerializeObject(cardsToSave, Formatting.Indented, jsonSettings);
        File.WriteAllText(SavePath, json);
    }
    
    // ----------------------------------------------------------------------
    /// カードデータベースをJSONファイルから読み込み
    // ----------------------------------------------------------------------
    private void LoadCardDatabase()
    {
        try
        {
            var loadedCards = LoadCardsFromFile();
            if (loadedCards != null && loadedCards.Count > 0)
            {
                PopulateCardCache(loadedCards);
                Debug.Log($"カードデータベースを読み込みました。カード数: {loadedCards.Count}");
            }
            else
            {
                Debug.Log("カードデータファイルが存在しないか、データが空です。");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"カードデータベースの読み込みに失敗しました: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    /// ファイルからカードデータのリストを読み込み
    // ----------------------------------------------------------------------
    private List<SerializableCardModel> LoadCardsFromFile()
    {
        if (!File.Exists(SavePath))
        {
            return null;
        }

        string json = File.ReadAllText(SavePath);

        try
        {
            return JsonConvert.DeserializeObject<List<SerializableCardModel>>(json, jsonSettings);
        }
        catch (JsonException ex)
        {
            Debug.LogError($"JSONファイルの解析に失敗しました: {ex.Message}");
            HandleCorruptedJsonFile();
            return null;
        }
    }

    // ----------------------------------------------------------------------
    /// 破損したJSONファイルのバックアップと削除処理
    // ----------------------------------------------------------------------
    private void HandleCorruptedJsonFile()
    {
        if (File.Exists(SavePath))
        {
            string backupPath = SavePath + Constants.BACKUP_EXTENSION;
            File.Copy(SavePath, backupPath, true);
            File.Delete(SavePath);
        }
    }

    // ----------------------------------------------------------------------
    /// 読み込んだカードデータでキャッシュを更新
    // ----------------------------------------------------------------------
    private void PopulateCardCache(List<SerializableCardModel> loadedCards)
    {
        // キャッシュをクリアして新しいデータで更新
        cardCache.Clear();
        nameToIdMap.Clear();

        // 各カードをCardModelに変換してキャッシュに登録
        foreach (var serializableCard in loadedCards)
        {
            var card = ConvertToCardModel(serializableCard);
            RegisterCardInCache(card);
        }
    }

    // ----------------------------------------------------------------------
    /// SerializableCardModelをCardModelに変換
    // ----------------------------------------------------------------------
    private CardModel ConvertToCardModel(SerializableCardModel serializableCard)
    {
        var card = new CardModel
        {
            id = serializableCard.id,
            name = serializableCard.name,
            cardType = serializableCard.cardType,
            evolutionStage = serializableCard.evolutionStage,
            pack = serializableCard.pack,
            hp = serializableCard.hp,
            type = serializableCard.type,
            weakness = serializableCard.weakness,
            retreatCost = serializableCard.retreatCost,
            maxEnergyCost = serializableCard.maxMoveEnergy,
            abilityName = serializableCard.abilityName,
            abilityEffect = serializableCard.abilityEffect,
            moves = serializableCard.moves,
            tags = serializableCard.tags,
            maxDamage = serializableCard.maxDamage,
            imageKey = serializableCard.imageKey,
            imageTexture = null // テクスチャは別途読み込む
        };

        // 列挙型の変換を実行
        card.ConvertStringDataToEnums();
        return card;
    }

    // ----------------------------------------------------------------------
    /// カードをキャッシュに登録（内部処理用）
    // ----------------------------------------------------------------------
    private void RegisterCardInCache(CardModel card)
    {
        // ID→CardModelのマッピングを更新
        cardCache[card.id] = card;

        // カード名→IDマッピングを更新
        if (!string.IsNullOrEmpty(card.name))
        {
            if (!nameToIdMap.ContainsKey(card.name))
            {
                nameToIdMap[card.name] = new List<string>();
            }

            if (!nameToIdMap[card.name].Contains(card.id))
            {
                nameToIdMap[card.name].Add(card.id);
            }
        }
    }

    // ----------------------------------------------------------------------
    /// カードデータベースの初期化が完了するまで待機するUniTask
    // ----------------------------------------------------------------------
    public static async UniTask WaitForInitializationAsync(float timeoutSeconds = Constants.INITIALIZATION_TIMEOUT_SECONDS)
    {
        if (Instance == null)
        {
            return;
        }
        
        if (Instance.isInitialized)
        {
            // 既に初期化済み
            return;
        }
        
        try
        {
            // 初期化フラグがtrueになるか、タイムアウトするまで待機
            using var cts = new System.Threading.CancellationTokenSource();
            cts.CancelAfterSlim(System.TimeSpan.FromSeconds(timeoutSeconds));
            
            await UniTask.WaitUntil(() => Instance.isInitialized, cancellationToken: cts.Token);
            
        }
        catch (System.OperationCanceledException ex)
        {
            Debug.LogWarning($"カードデータベースの初期化待機がタイムアウトしました: {ex.Message}");
        }
    }
    
    // ----------------------------------------------------------------------
    /// アプリケーション終了時に保存
    // ----------------------------------------------------------------------
    private void OnApplicationQuit()
    {
        SaveCardDatabase();
    }
    
    // ----------------------------------------------------------------------
    /// アプリがバックグラウンドに移行する際の処理
    // ----------------------------------------------------------------------
    private void OnApplicationPause(bool pause)
    {
        if (pause && cardCache.Count > 0) // バックグラウンドに移行かつデータが存在する場合
        {
            SaveCardDatabase();
        }

    }
    
    // ----------------------------------------------------------------------
    // デバッグ用：全てのカードキャッシュを削除してリロードする
    // ----------------------------------------------------------------------
    public void ClearCacheAndReload()
    {
        try
        {
            ClearMemoryCache();
            LoadCardDatabase();
            CompleteInitialization();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"キャッシュのクリアと再読み込みに失敗しました: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    // メモリキャッシュをクリア
    // ----------------------------------------------------------------------
    private void ClearMemoryCache()
    {
        cardCache.Clear();
        nameToIdMap.Clear();
    }

    // ----------------------------------------------------------------------
    // 保存されたカードデータファイルを削除する
    // ----------------------------------------------------------------------
    public bool ClearCardDataFile()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                // ファイルを削除
                File.Delete(SavePath);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"カードデータファイルの削除に失敗しました: {ex.Message}");
            return false;
        }
    }

    // ----------------------------------------------------------------------
    /// 完全にキャッシュをリセット（メモリ＆ファイル）
    // ----------------------------------------------------------------------
    public void FullReset()
    {
        try
        {
            // ファイルキャッシュを削除
            bool fileDeleted = ClearCardDataFile();
            
            // メモリキャッシュをクリア
            ClearMemoryCache();
            
            // 初期化フラグをリセット
            isInitialized = false;
            
            // データの再読み込みは行わない（空の状態を維持）
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"カードデータベースの完全リセットに失敗しました: {ex.Message}");
        }
    }
    
}

// ----------------------------------------------------------------------
/// シリアライズ可能なカードモデル
/// Texture2Dなどのシリアライズ不可能なフィールドを除いたモデル
// ----------------------------------------------------------------------
[System.Serializable]
public class SerializableCardModel
{
    public string id;
    public string name;
    public string cardType;
    public string evolutionStage;
    public string pack;
    public int hp;
    public string type;
    public string weakness;
    public int retreatCost;
    // 最大エネルギーコスト
    public int maxMoveEnergy;
    public string abilityName;
    public string abilityEffect;
    public List<MoveData> moves;
    public List<string> tags;
    public int maxDamage;
    public string imageKey;
    // imageTextureは除外（シリアライズ不可能）
}

// ----------------------------------------------------------------------
/// Unity固有のオブジェクトをシリアライズから除外するContractResolver
// ----------------------------------------------------------------------
public class UnityContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
{
    protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(
        System.Reflection.MemberInfo member, 
        Newtonsoft.Json.MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        
        // Unity固有の型をシリアライズから除外
        if (property.PropertyType == typeof(Texture2D) || 
            property.PropertyType == typeof(Texture) ||
            property.PropertyType == typeof(Object) ||
            property.PropertyType == typeof(Vector2) ||
            property.PropertyType == typeof(Vector3) ||
            property.PropertyType == typeof(Vector4) ||
            property.PropertyType == typeof(Quaternion) ||
            property.PropertyType == typeof(Color) ||
            property.PropertyType == typeof(Rect))
        {
            property.ShouldSerialize = instance => false;
        }
        
        return property;
    }
}