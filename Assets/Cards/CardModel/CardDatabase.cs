using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

// ----------------------------------------------------------------------
// カードデータを一元管理するシングルトンクラス
// 全カードのキャッシュとアクセスを提供する
// ----------------------------------------------------------------------
public class CardDatabase : MonoBehaviour
{
    private static CardDatabase _instance;
    public static CardDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CardDatabase");
                _instance = go.AddComponent<CardDatabase>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    // カードデータのキャッシュ（ID → CardModel）
    private Dictionary<string, CardModel> cardCache = new Dictionary<string, CardModel>();
    
    // カード名のキャッシュ（カード名 → カードIDのリスト）
    private Dictionary<string, List<string>> nameToIdMap = new Dictionary<string, List<string>>();
    
    // カードデータの保存パス
    private string SavePath => Path.Combine(Application.persistentDataPath, "card_database.json");
    
    // 初期化済みフラグ
    private bool isInitialized = false;
    
    // Jsonシリアライズ設定
    private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        PreserveReferencesHandling = PreserveReferencesHandling.None,
        TypeNameHandling = TypeNameHandling.None,
        ContractResolver = new UnityContractResolver()
    };
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 保存されたカードデータを読み込み
        LoadCardDatabase();
        
        Debug.Log($"✅ CardDatabase初期化完了: {cardCache.Count}枚のカードを読み込みました");
        isInitialized = true;
    }
    
    /// <summary>
    /// カードをキャッシュに追加する
    /// </summary>
    public void RegisterCard(CardModel card)
    {
        if (card == null || string.IsNullOrEmpty(card.id))
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
        
        // データベースが変更されたので保存
        SaveCardDatabase();
    }
    
    /// <summary>
    /// 複数のカードをキャッシュに一括登録
    /// </summary>
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
    
    /// <summary>
    /// IDからカードを取得する
    /// </summary>
    public CardModel GetCard(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return null;
        
        if (cardCache.TryGetValue(cardId, out CardModel card))
        {
            return card;
        }
        
        Debug.LogWarning($"カードID:{cardId}がデータベースに見つかりません");
        return null;
    }
    
    /// <summary>
    /// 名前から同名カードのIDリストを取得
    /// </summary>
    public List<string> GetCardIdsByName(string cardName)
    {
        if (string.IsNullOrEmpty(cardName)) return new List<string>();
        
        if (nameToIdMap.TryGetValue(cardName, out List<string> ids))
        {
            return new List<string>(ids);
        }
        
        return new List<string>();
    }
    
    /// <summary>
    /// 全カードを取得
    /// </summary>
    public static List<CardModel> GetAllCards()
    {
        List<CardModel> result = new List<CardModel>();
        
        foreach (var card in Instance.cardCache.Values)
        {
            result.Add(card);
        }
        
        return result;
    }
    
    /// <summary>
    /// カードデータベースをJSONファイルに保存
    /// </summary>
    private void SaveCardDatabase()
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
    
    /// <summary>
    /// カードデータベースをJSONファイルから読み込み
    /// </summary>
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
                        if (!string.IsNullOrEmpty(card.id))
                        {
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
    
    /// <summary>
    /// カードデータベースが初期化されているかどうかを確認
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }
    
    /// <summary>
    /// アプリケーション終了時に保存
    /// </summary>
    private void OnApplicationQuit()
    {
        SaveCardDatabase();
    }
}

/// <summary>
/// シリアライズ可能なカードモデル
/// Texture2Dなどのシリアライズ不可能なフィールドを除いたモデル
/// </summary>
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
    public string abilityName;
    public string abilityEffect;
    public List<MoveData> moves;
    public List<string> tags;
    public int maxDamage;
    public string imageKey;
    // imageTextureは除外（シリアライズ不可能）
}

/// <summary>
/// Unity固有のオブジェクトをシリアライズから除外するContractResolver
/// </summary>
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