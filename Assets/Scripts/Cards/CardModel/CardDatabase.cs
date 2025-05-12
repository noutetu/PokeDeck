using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
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
            _instance = FindObjectOfType<CardDatabase>();
            
            // それでも見つからない場合だけ新規作成
            if (_instance == null)
            {
                var go = new GameObject("CardDatabase");
                _instance = go.AddComponent<CardDatabase>();
                DontDestroyOnLoad(go);
                Debug.Log("CardDatabaseの新規インスタンスを作成しました");
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
    private string SavePath => Path.Combine(Application.persistentDataPath, "card_database.json");
    
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
        // シングルトンチェック - 複数のインスタンスが作成された場合は破棄する
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"複数のCardDatabaseインスタンスが検出されました。重複を破棄します: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        // このインスタンスをシングルトンとして設定
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 保存されたカードデータを読み込み
        LoadCardDatabase();
        
        // 初期化完了フラグを設定
        isInitialized = true;
        
        // 初期化完了イベント発火
        OnDatabaseInitialized?.Invoke();
        
        Debug.Log($"✅ CardDatabase初期化完了: {cardCache.Count}枚のカードを読み込みました");
    }
    
    // ----------------------------------------------------------------------
    // カードをキャッシュに追加する
    // ----------------------------------------------------------------------
    public void RegisterCard(CardModel card, bool saveImmediately = false)
    {
        if (card == null)
            return;
            
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
        
        Debug.Log($"CardDatabaseに{cards.Count}枚のカードを登録しました");
        
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
    /// カード名からカードIDのリストを取得
    // ----------------------------------------------------------------------
    public List<string> GetCardIdsByName(string cardName)
    {
        List<string> result = new List<string>();
        foreach (var pair in cardCache)
        {
            if (pair.Value.name == cardName)
            {
                result.Add(pair.Key);
            }
        }
        return result;
    }
    
    // ----------------------------------------------------------------------
    /// 名前から同名カードのID文字列リストを取得（互換性維持用）
    // ----------------------------------------------------------------------
    public List<string> GetCardIdStringsByName(string cardName)
    {
        List<string> result = new List<string>();
        List<string> numericIds = GetCardIdsByName(cardName);
        
        foreach (string id in numericIds)
        {
            CardModel card = GetCard(id);
            if (card != null && !string.IsNullOrEmpty(card.id))
            {
                result.Add(card.id);
            }
        }
        
        return result;
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
            // 空のデータベースを保存しないように確認
            if (cardCache.Count == 0)
            {
                Debug.LogWarning("カードデータベースが空のため保存をスキップします");
                return;
            }
            
            // カードデータのリストに変換（JSONシリアライズ用に準備）
            List<SerializableCardModel> cardsToSave = new List<SerializableCardModel>();
            
            foreach (var card in cardCache.Values)
            {
                // シリアライズ可能なモデルに変換
                var serializableCard = new SerializableCardModel
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
                    // 最大エネルギーコストを追加
                    maxMoveEnergy = card.maxEnergyCost,
                    abilityName = card.abilityName,
                    abilityEffect = card.abilityEffect,
                    moves = card.moves,
                    tags = card.tags,
                    maxDamage = card.maxDamage,
                    imageKey = card.imageKey
                    // imageTextureは保存しない（ランタイムデータ）
                };
                
                cardsToSave.Add(serializableCard);
            }
            
            // JSONに変換して保存
            string json = JsonConvert.SerializeObject(cardsToSave, Formatting.Indented, jsonSettings);
            File.WriteAllText(SavePath, json);
            
            Debug.Log($"カードデータベースを保存しました: {cardsToSave.Count}枚のカード, 保存先: {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"カードデータベースの保存中にエラーが発生しました: {e.Message}");
            Debug.LogException(e);
        }
    }
    
    // ----------------------------------------------------------------------
    /// カードデータベースをJSONファイルから読み込み
    // ----------------------------------------------------------------------
    private void LoadCardDatabase()
    {
        try
        {
            // ファイルが存在するか確認
            if (!File.Exists(SavePath))
            {
                Debug.Log("カードデータベースのファイルが見つかりません。新規作成します。");
                return;
            }
            
            // ファイルからJSONを読み込み
            string json = File.ReadAllText(SavePath);
            
            try
            {
                // JSONからシリアライズ可能なカードデータリストに変換
                List<SerializableCardModel> loadedCards = JsonConvert.DeserializeObject<List<SerializableCardModel>>(json, jsonSettings);
                
                if (loadedCards != null && loadedCards.Count > 0)
                {
                    // キャッシュをクリアして新しいデータで更新
                    cardCache.Clear();
                    nameToIdMap.Clear();
                    
                    // 各カードをCardModelに変換してキャッシュに登録
                    foreach (var serializableCard in loadedCards)
                    {
                        // CardModelに変換
                        CardModel card = new CardModel
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
                        
                        // カードをキャッシュに登録
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
                    
                    Debug.Log($"カードデータベースを読み込みました: {loadedCards.Count}枚のカード");
                }
                else
                {
                    Debug.LogWarning("カードデータベースの読み込みに失敗または空のデータベースでした");
                }
            }
            catch (JsonException jsonEx)
            {
                Debug.LogError($"JSONのデシリアライズエラー: {jsonEx.Message}");
                // JSONの形式が無効な場合は、ファイルを削除して新規作成することも検討
                if (File.Exists(SavePath))
                {
                    string backupPath = SavePath + ".backup";
                    Debug.LogWarning($"無効なJSONファイルをバックアップ: {backupPath}");
                    File.Copy(SavePath, backupPath, true);
                    File.Delete(SavePath);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"カードデータベースの読み込み中にエラーが発生しました: {e.Message}");
            Debug.LogException(e);
        }
    }

    // ----------------------------------------------------------------------
    /// カードデータベースの初期化が完了するまで待機するUniTask
    // ----------------------------------------------------------------------
    public static async UniTask WaitForInitializationAsync(float timeoutSeconds = 5f)
    {
        if (Instance == null)
        {
            Debug.LogWarning("CardDatabase.Instanceがnullです。初期化を待機できません。");
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
            
            Debug.Log($"✅ CardDatabase初期化完了を確認しました（UniTask待機）");
        }
        catch (System.OperationCanceledException)
        {
            Debug.LogWarning($"⚠️ CardDatabase初期化待機がタイムアウトしました ({timeoutSeconds}秒)");
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
            Debug.Log("アプリがバックグラウンドに移行: データベースを保存します");
            SaveCardDatabase();
        }

    }
    
    // ----------------------------------------------------------------------
    //  デバッグ用：全てのカードキャッシュを削除してリロードする
    // ----------------------------------------------------------------------
    public void ClearCacheAndReload()
    {
        Debug.Log("🧹 カードデータベースのキャッシュをクリアして再読み込みします...");
        
        try
        {
            // メモリキャッシュをクリア
            cardCache.Clear();
            nameToIdMap.Clear();
            
            Debug.Log("✅ メモリキャッシュをクリアしました");
            
            // データベースを再読み込み
            LoadCardDatabase();
            
            // 初期化完了フラグを更新
            isInitialized = true;
            
            // 初期化完了イベントを発火
            OnDatabaseInitialized?.Invoke();
            
            Debug.Log($"✅ CardDatabase再読み込み完了: {cardCache.Count}枚のカード");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"💾 キャッシュクリア中にエラーが発生しました: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    /// デバッグ用：保存されたカードデータファイルを削除する
    // ----------------------------------------------------------------------
    public bool ClearCardDataFile()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                // ファイルを削除
                File.Delete(SavePath);
                Debug.Log($"✅ カードデータファイルを削除しました: {SavePath}");
                return true;
            }
            else
            {
                Debug.Log("削除するカードデータファイルが見つかりませんでした");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ カードデータファイル削除中にエラーが発生しました: {ex.Message}");
            return false;
        }
    }

    // ----------------------------------------------------------------------
    /// デバッグ用：完全にキャッシュをリセット（メモリ＆ファイル）
    // ----------------------------------------------------------------------
    public void FullReset()
    {
        Debug.Log("🔄 カードデータベースの完全リセットを開始...");
        
        try
        {
            // ファイルキャッシュを削除
            bool fileDeleted = ClearCardDataFile();
            
            // メモリキャッシュをクリア
            cardCache.Clear();
            nameToIdMap.Clear();
            
            Debug.Log($"✅ カードデータベースの完全リセット完了 (ファイル削除: {(fileDeleted ? "成功" : "不要")})");
            
            // 初期化フラグをリセット
            isInitialized = false;
            
            // データの再読み込みは行わない（空の状態を維持）
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ カードデータベースのリセット中にエラーが発生しました: {ex.Message}");
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
    public string idString; // 互換性維持用の元のID文字列
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
            property.PropertyType == typeof(UnityEngine.Object) ||
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