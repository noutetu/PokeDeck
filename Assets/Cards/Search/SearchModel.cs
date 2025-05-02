// ----------------------------------------------------------------------
// カード検索のモデルクラス
// 検索条件の管理とフィルタリング処理を担当する
// ----------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Enum;

public class SearchModel
{
    // ----------------------------------------------------------------------
    // カードデータ管理用の変数
    // ----------------------------------------------------------------------
    private List<CardModel> allCards = new List<CardModel>();
    private List<CardModel> filteredCards = new List<CardModel>();
    private List<CardModel> cardList = null; // 外部から設定されたカードリスト
    
    // ----------------------------------------------------------------------
    // 検索条件
    // ----------------------------------------------------------------------
    private string searchText = "";
    private HashSet<CardType> selectedCardTypes = new HashSet<CardType>();
    private HashSet<EvolutionStage> selectedEvolutionStages = new HashSet<EvolutionStage>();
    private HashSet<PokemonType> selectedPokemonTypes = new HashSet<PokemonType>();
    private HashSet<CardPack> selectedCardPacks = new HashSet<CardPack>();
    // HP関連のフィルター条件を追加
    private int selectedHP = 0;
    private SetHPArea.HPComparisonType selectedHPComparisonType = SetHPArea.HPComparisonType.None;
    // 最大ダメージ関連のフィルター条件を追加
    private int selectedMaxDamage = 0;
    private SetMaxDamageArea.DamageComparisonType selectedMaxDamageComparisonType = SetMaxDamageArea.DamageComparisonType.None;

    // ----------------------------------------------------------------------
    // コンストラクタ
    // ----------------------------------------------------------------------
    public SearchModel()
    {
        Initialize();
    }

    // ----------------------------------------------------------------------
    // モデルの初期化
    // ----------------------------------------------------------------------
    public void Initialize()
    {
        LoadCards();
    }
    
    // ----------------------------------------------------------------------
    // カードデータをロード
    // ----------------------------------------------------------------------
    private void LoadCards()
    {
        // データベースやファイルからカードデータを読み込む
        var cards = CardDatabase.GetAllCards();
        
        // nullチェックを追加
        if (cards != null)
        {
            allCards = cards;
            
            // カードのEnum変換を確認・実行
            int convertedCount = 0;
            foreach (var card in allCards)
            {
                // カードタイプが有効かつEnum変換が行われていない場合
                if (!string.IsNullOrEmpty(card.cardType))
                {
                    // 初回変換または再変換を行う
                    card.ConvertStringDataToEnums();
                    convertedCount++;
                }
                else if (string.IsNullOrEmpty(card.cardType))
                {
                    // カードタイプが空の場合は「不明」などの特別な処理が必要
                    Debug.LogWarning($"⚠️ カードタイプが未設定: {card.name}");
                }
            }
            // カードタイプの分布を集計して出力（問題分析用）
            Dictionary<CardType, int> typeDistribution = new Dictionary<CardType, int>();
            foreach (var card in allCards)
            {
                if (!typeDistribution.ContainsKey(card.cardTypeEnum))
                {
                    typeDistribution[card.cardTypeEnum] = 0;
                }
                typeDistribution[card.cardTypeEnum]++;
            }
            // 進化段階の分布も集計
            Dictionary<EvolutionStage, int> stageDistribution = new Dictionary<EvolutionStage, int>();
            foreach (var card in allCards)
            {
                // カードタイプが「非EX」または「EX」のみ進化段階がある
                if (card.cardTypeEnum == CardType.非EX || card.cardTypeEnum == CardType.EX)
                {
                    if (!stageDistribution.ContainsKey(card.evolutionStageEnum))
                    {
                        stageDistribution[card.evolutionStageEnum] = 0;
                    }
                    stageDistribution[card.evolutionStageEnum]++;
                }
            }
            
            // ポケモンタイプの分布も集計
            Dictionary<PokemonType, int> pokemonTypeDistribution = new Dictionary<PokemonType, int>();
            foreach (var card in allCards)
            {
                // カードタイプが「非EX」または「EX」のみポケモンタイプがある
                if ((card.cardTypeEnum == CardType.非EX || card.cardTypeEnum == CardType.EX) && 
                    !string.IsNullOrEmpty(card.type))
                {
                    if (!pokemonTypeDistribution.ContainsKey(card.typeEnum))
                    {
                        pokemonTypeDistribution[card.typeEnum] = 0;
                    }
                    pokemonTypeDistribution[card.typeEnum]++;
                }
            }
            // カードの詳細をログに出力（最初の5枚のみ）
            if (allCards.Count > 0)
            {
                for (int i = 0; i < Mathf.Min(5, allCards.Count); i++)
                {
                    var card = allCards[i];
                }
            }
        }
        else
        {
            // CardDatabaseからカードを取得できない場合、空のリストを作成
            allCards = new List<CardModel>();
            Debug.LogWarning("⚠️ CardDatabase.GetAllCards()がnullを返しました。カードデータが読み込まれていない可能性があります。");
        }
        
        // 初期状態では全カードを表示
        filteredCards = new List<CardModel>(allCards);
    }
    
    // ----------------------------------------------------------------------
    // カードデータを外部から設定する
    // CardUIBootなどから読み込み済みのカードデータを設定できるようにする
    // @param cards 設定するカードデータ
    // ----------------------------------------------------------------------
    public void SetCards(List<CardModel> cards)
    {
        if (cards != null)
        {
            allCards = new List<CardModel>(cards);
            
            // Enumの変換を確認・実行
            int convertedCount = 0;
            foreach (var card in allCards)
            {
                // カードタイプ・進化段階の列挙型変換が行われていない場合は変換を実行
                if ((card.cardTypeEnum == 0 && !string.IsNullOrEmpty(card.cardType)) ||
                    (card.cardTypeEnum == CardType.非EX || card.cardTypeEnum == CardType.EX) && 
                    (!string.IsNullOrEmpty(card.evolutionStage) || !string.IsNullOrEmpty(card.type)))
                {
                    card.ConvertStringDataToEnums();
                    convertedCount++;
                }
            }
            // カードの詳細をログに出力（最初の5枚のみ）
            if (allCards.Count > 0)
            {
                for (int i = 0; i < Mathf.Min(5, allCards.Count); i++)
                {
                    var card = allCards[i];
                }
            }
            
            // 検索条件も初期化
            ClearAllFilters();
        }
        else
        {
            Debug.LogError("❌ SetCards()にnullが渡されました");
        }
    }
    
    // ----------------------------------------------------------------------
    // テキスト検索フィルターを設定
    // @param text 検索テキスト
    // ----------------------------------------------------------------------
    public void SetSearchText(string text)
    {
        searchText = text;
        ApplyFilters();
    }
    
    // ----------------------------------------------------------------------
    // カードタイプフィルターを設定
    // @param cardTypes 検索するカードタイプのセット
    // ----------------------------------------------------------------------
    public void SetCardTypeFilter(HashSet<CardType> cardTypes)
    {
        selectedCardTypes = new HashSet<CardType>(cardTypes);
        ApplyFilters();
    }
    
    // ----------------------------------------------------------------------
    // 進化段階フィルターを設定
    // @param evolutionStages 検索する進化段階のセット
    // ----------------------------------------------------------------------
    public void SetEvolutionStageFilter(HashSet<EvolutionStage> evolutionStages)
    {
        selectedEvolutionStages = new HashSet<EvolutionStage>(evolutionStages);
        ApplyFilters();
    }
    
    // ----------------------------------------------------------------------
    // ポケモンタイプフィルターを設定
    // @param pokemonTypes 検索するポケモンタイプのセット
    // ----------------------------------------------------------------------
    public void SetPokemonTypeFilter(HashSet<PokemonType> pokemonTypes)
    {
        selectedPokemonTypes = new HashSet<PokemonType>(pokemonTypes);
        ApplyFilters();
    }
    
    // ----------------------------------------------------------------------
    // カードパックフィルターを設定
    // @param cardPacks 植物するカードパックのセット
    // ----------------------------------------------------------------------
    public void SetCardPackFilter(HashSet<CardPack> cardPacks)
    {
        selectedCardPacks = new HashSet<CardPack>(cardPacks);
        ApplyFilters();
    }
    
    // ----------------------------------------------------------------------
    // HPフィルターを設定
    // @param hp 検索するHP値
    // @param comparisonType 比較タイプ（以下、同じ、以上のいずれか）
    // ----------------------------------------------------------------------
    public void SetHPFilter(int hp, SetHPArea.HPComparisonType comparisonType)
    {
        selectedHP = hp;
        selectedHPComparisonType = comparisonType;
        ApplyFilters();
    }
    
    // ----------------------------------------------------------------------
    // 最大ダメージフィルターを設定
    // @param damage 検索する最大ダメージ値
    // @param comparisonType 比較タイプ（以下、同じ、以上のいずれか）
    // ----------------------------------------------------------------------
    public void SetMaxDamageFilter(int damage, SetMaxDamageArea.DamageComparisonType comparisonType)
    {
        selectedMaxDamage = damage;
        selectedMaxDamageComparisonType = comparisonType;
        ApplyFilters();
    }
    
    // ----------------------------------------------------------------------
    // すべてのフィルターをクリア
    // ----------------------------------------------------------------------
    public void ClearAllFilters()
    {
        searchText = "";
        selectedCardTypes.Clear();
        selectedEvolutionStages.Clear();
        selectedPokemonTypes.Clear();
        selectedCardPacks.Clear();
        filteredCards = new List<CardModel>(allCards);
    }
    
    // ----------------------------------------------------------------------
    // フィルタリングを適用
    // ----------------------------------------------------------------------
    public void ApplyFilters()
    {
        // 最初に全カードをベースにする
        filteredCards = new List<CardModel>(allCards);
        
        // テキスト検索フィルター適用
        ApplyTextFilter();
        
        // カードタイプフィルター適用
        ApplyCardTypeFilter();
        
        // 進化段階フィルター適用
        ApplyEvolutionStageFilter();
        
        // ポケモンタイプフィルター適用
        ApplyPokemonTypeFilter();
        
        // カードパックフィルター適用
        ApplyCardPackFilter();
        
        // HPフィルター適用
        ApplyHPFilter();
        
        // 最大ダメージフィルター適用
        ApplyMaxDamageFilter();
        
        Debug.Log($"🔍 全フィルター適用後のカード数: {filteredCards.Count}件");
    }
    
    // ----------------------------------------------------------------------
    // テキスト検索フィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyTextFilter()
    {
        if (string.IsNullOrWhiteSpace(searchText)) return;
        
        // 検索テキストを小文字に変換して検索（大文字小文字を区別しない）
        string searchLower = searchText.ToLower();
        
        filteredCards = filteredCards.Where(card => 
            // カード名の検索
            (card.name != null && card.name.ToLower().Contains(searchLower)) ||
            
            // 技名の検索（技が存在する場合）
            (card.moves != null && card.moves.Any(move => 
                move.name != null && move.name.ToLower().Contains(searchLower))) ||
            
            // 技の効果の検索（技が存在する場合）
            (card.moves != null && card.moves.Any(move => 
                move.effect != null && move.effect.ToLower().Contains(searchLower)))
        ).ToList();
        
    }
    
    // ----------------------------------------------------------------------
    // カードタイプフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyCardTypeFilter()
    {
        // カードタイプフィルターが設定されていない場合はスキップ
        if (selectedCardTypes.Count == 0) return;
        
        
        // デバッグ情報：フィルタリング前のカードタイプの分布を出力
        Dictionary<CardType, int> typeDistribution = new Dictionary<CardType, int>();
        foreach (var card in filteredCards)
        {
            if (!typeDistribution.ContainsKey(card.cardTypeEnum))
            {
                typeDistribution[card.cardTypeEnum] = 0;
            }
            typeDistribution[card.cardTypeEnum]++;
        }
        // カードタイプによるフィルタリング
        filteredCards = filteredCards.Where(card => 
            selectedCardTypes.Contains(card.cardTypeEnum)
        ).ToList();
        
        
        // デバッグ情報：フィルタリング後のカードタイプの分布を出力
        typeDistribution.Clear();
        foreach (var card in filteredCards)
        {
            if (!typeDistribution.ContainsKey(card.cardTypeEnum))
            {
                typeDistribution[card.cardTypeEnum] = 0;
            }
            typeDistribution[card.cardTypeEnum]++;
        }
        
    }
    
    // ----------------------------------------------------------------------
    // 進化段階フィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyEvolutionStageFilter()
    {
        // 進化段階フィルターが設定されていない場合はスキップ
        if (selectedEvolutionStages.Count == 0) return;
        
        // デバッグ情報：フィルタリング前の進化段階の分布を出力
        Dictionary<EvolutionStage, int> stageDistribution = new Dictionary<EvolutionStage, int>();
        foreach (var card in filteredCards)
        {
            // ポケモンカードのみ進化段階がある
            if (card.cardTypeEnum == CardType.非EX || card.cardTypeEnum == CardType.EX)
            {
                if (!stageDistribution.ContainsKey(card.evolutionStageEnum))
                {
                    stageDistribution[card.evolutionStageEnum] = 0;
                }
                stageDistribution[card.evolutionStageEnum]++;
            }
        }
        
        // 進化段階によるフィルタリング
        // 修正：ポケモンカードのみ対象とし、選択された進化段階に合致するもののみ表示
        filteredCards = filteredCards.Where(card => 
            // 選択された進化段階に合致するポケモンカードのみを表示
            ((card.cardTypeEnum == CardType.非EX || card.cardTypeEnum == CardType.EX) && 
             !string.IsNullOrEmpty(card.evolutionStage) &&
             selectedEvolutionStages.Contains(card.evolutionStageEnum))
        ).ToList();
        
        // デバッグ情報：フィルタリング後の進化段階の分布を出力
        stageDistribution.Clear();
        foreach (var card in filteredCards)
        {
            if (card.cardTypeEnum == CardType.非EX || card.cardTypeEnum == CardType.EX)
            {
                if (!stageDistribution.ContainsKey(card.evolutionStageEnum))
                {
                    stageDistribution[card.evolutionStageEnum] = 0;
                }
                stageDistribution[card.evolutionStageEnum]++;
            }
        }
    }
    
    // ----------------------------------------------------------------------
    // ポケモンタイプフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyPokemonTypeFilter()
    {
        // ポケモンタイプフィルターが設定されていない場合はスキップ
        if (selectedPokemonTypes.Count == 0) return;
        
        // デバッグ情報：フィルタリング前のポケモンタイプの分布を出力
        Dictionary<PokemonType, int> typeDistribution = new Dictionary<PokemonType, int>();
        foreach (var card in filteredCards)
        {
            // ポケモンカードのみポケモンタイプがある
            if ((card.cardTypeEnum == CardType.非EX || card.cardTypeEnum == CardType.EX) &&
                !string.IsNullOrEmpty(card.type))
            {
                if (!typeDistribution.ContainsKey(card.typeEnum))
                {
                    typeDistribution[card.typeEnum] = 0;
                }
                typeDistribution[card.typeEnum]++;
            }
        }
        
        
        // カードタイプ分布も出力（どれくらいポケモン以外のカードが含まれているか）
        Dictionary<CardType, int> cardTypeDistribution = new Dictionary<CardType, int>();
        foreach (var card in filteredCards)
        {
            if (!cardTypeDistribution.ContainsKey(card.cardTypeEnum))
            {
                cardTypeDistribution[card.cardTypeEnum] = 0;
            }
            cardTypeDistribution[card.cardTypeEnum]++;
        }
        
        // ポケモンタイプによるフィルタリング（ポケモンカードのみを対象に）
        filteredCards = filteredCards.Where(card => 
            // ポケモンカード（非EXまたはEX）かつポケモンタイプが設定されており、選択されたタイプに一致するもののみを表示
            (card.cardTypeEnum == CardType.非EX || card.cardTypeEnum == CardType.EX) && 
            !string.IsNullOrEmpty(card.type) &&
            selectedPokemonTypes.Contains(card.typeEnum)
        ).ToList();
        
        // デバッグ情報：フィルタリング後のポケモンタイプの分布を出力
        typeDistribution.Clear();
        foreach (var card in filteredCards)
        {
            if ((card.cardTypeEnum == CardType.非EX || card.cardTypeEnum == CardType.EX) &&
                !string.IsNullOrEmpty(card.type))
            {
                if (!typeDistribution.ContainsKey(card.typeEnum))
                {
                    typeDistribution[card.typeEnum] = 0;
                }
                typeDistribution[card.typeEnum]++;
            }
        }
        // フィルタリング後のカードタイプ分布も出力
        cardTypeDistribution.Clear();
        foreach (var card in filteredCards)
        {
            if (!cardTypeDistribution.ContainsKey(card.cardTypeEnum))
            {
                cardTypeDistribution[card.cardTypeEnum] = 0;
            }
            cardTypeDistribution[card.cardTypeEnum]++;
        }
    }
    
    // ----------------------------------------------------------------------
    // カードパックフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyCardPackFilter()
    {
        // カードパックフィルターが設定されていない場合はスキップ
        if (selectedCardPacks.Count == 0) return;
        
        // デバッグ情報：フィルタリング前のカードパックの分布を出力
        Dictionary<CardPack, int> packDistribution = new Dictionary<CardPack, int>();
        foreach (var card in filteredCards)
        {
            if (!packDistribution.ContainsKey(card.packEnum))
            {
                packDistribution[card.packEnum] = 0;
            }
            packDistribution[card.packEnum]++;
        }
        
        // カードパックによるフィルタリング
        // パック情報が設定されているカードのみを対象にする
        filteredCards = filteredCards.Where(card => 
            !string.IsNullOrEmpty(card.pack) && selectedCardPacks.Contains(card.packEnum)
        ).ToList();
        
        // デバッグ情報：フィルタリング後のカードパックの分布を出力
        packDistribution.Clear();
        foreach (var card in filteredCards)
        {
            if (!packDistribution.ContainsKey(card.packEnum))
            {
                packDistribution[card.packEnum] = 0;
            }
            packDistribution[card.packEnum]++;
        }
    }
    
    // ----------------------------------------------------------------------
    // HPフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyHPFilter()
    {
        // HP比較タイプが設定されていないか、HP値が0（指定なし）の場合はスキップ
        if (selectedHPComparisonType == SetHPArea.HPComparisonType.None || selectedHP <= 0)
        {
            Debug.Log("🔍 HPフィルターはスキップします（比較タイプ: " + selectedHPComparisonType + ", HP値: " + selectedHP + "）");
            return;
        }
        
        Debug.Log($"🔍 HPフィルター適用: {selectedHP}HP, 比較タイプ: {selectedHPComparisonType}");
        
        // フィルタリング前のカードタイプの分布を出力
        Dictionary<CardType, int> typeDistribution = new Dictionary<CardType, int>();
        foreach (var card in filteredCards)
        {
            if (!typeDistribution.ContainsKey(card.cardTypeEnum))
            {
                typeDistribution[card.cardTypeEnum] = 0;
            }
            typeDistribution[card.cardTypeEnum]++;
        }
        Debug.Log("🔍 フィルタリング前のカードタイプ分布:");
        foreach (var kv in typeDistribution)
        {
            Debug.Log($"  🔍 {kv.Key}: {kv.Value}枚");
        }
        
        // フィルタリング前のHPを持つカードの数を確認
        int cardsWithHpCount = filteredCards.Count(card => card.hp > 0);
        Debug.Log($"🔍 フィルタリング前のHPを持つカード数: {cardsWithHpCount}枚");
        
        // HPによるフィルタリング - 修正版（化石も含めてHPを持つカードを対象に）
        filteredCards = filteredCards.Where(card => 
        {
            // HPが0以下の場合はフィルタリングの対象外（HPを持たないカード）
            if (card.hp <= 0)
                return false;
            
            // 各比較タイプに応じたフィルタリング
            switch (selectedHPComparisonType)
            {
                case SetHPArea.HPComparisonType.LessOrEqual:
                    return card.hp <= selectedHP;
                case SetHPArea.HPComparisonType.Equal:
                    return card.hp == selectedHP;
                case SetHPArea.HPComparisonType.GreaterOrEqual:
                    return card.hp >= selectedHP;
                default:
                    return false;
            }
        }).ToList();
        
        Debug.Log($"🔍 HPフィルター結果: {filteredCards.Count}件");
        
        // フィルタリング後のカードタイプの分布を出力
        typeDistribution.Clear();
        foreach (var card in filteredCards)
        {
            if (!typeDistribution.ContainsKey(card.cardTypeEnum))
            {
                typeDistribution[card.cardTypeEnum] = 0;
            }
            typeDistribution[card.cardTypeEnum]++;
        }
        Debug.Log("🔍 フィルタリング後のカードタイプ分布:");
        foreach (var kv in typeDistribution)
        {
            Debug.Log($"  🔍 {kv.Key}: {kv.Value}枚");
        }
        
        // フィルタリング後のHPを持つカードの数を確認
        cardsWithHpCount = filteredCards.Count(card => card.hp > 0);
        Debug.Log($"🔍 フィルタリング後のHPを持つカード数: {cardsWithHpCount}枚");
        
        // HPの分布を出力
        Dictionary<int, int> hpDistribution = new Dictionary<int, int>();
        foreach (var card in filteredCards)
        {
            if (card.hp > 0)
            {
                if (!hpDistribution.ContainsKey(card.hp))
                {
                    hpDistribution[card.hp] = 0;
                }
                hpDistribution[card.hp]++;
            }
        }
        
        Debug.Log("🔍 フィルタリング後のHP分布:");
        foreach (var kv in hpDistribution.OrderBy(kv => kv.Key))
        {
            Debug.Log($"  🔍 {kv.Key}HP: {kv.Value}枚");
        }
    }
    
    // ----------------------------------------------------------------------
    // 最大ダメージフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyMaxDamageFilter()
    {
        // 最大ダメージ比較タイプが未設定（None）の場合はフィルタリングをスキップ
        // これは「指定なし」が選択されている場合
        if (selectedMaxDamageComparisonType == SetMaxDamageArea.DamageComparisonType.None)
        {
            Debug.Log("🔍 最大ダメージフィルター: 「指定なし」が選択されているため、スキップします");
            return;
        }
        
        Debug.Log($"🔍 最大ダメージフィルター適用: {selectedMaxDamage}ダメージ, 比較タイプ={selectedMaxDamageComparisonType}");
        
        // フィルタリング前の最大ダメージを持つカードの数を確認
        int cardsWithMaxDamageCount = filteredCards.Count(card => card.maxDamage > 0);
        Debug.Log($"🔍 フィルタリング前の最大ダメージを持つカード数: {cardsWithMaxDamageCount}枚");
        
        // 最大ダメージによるフィルタリング（0ダメージも有効な選択肢として含める）
        filteredCards = filteredCards.Where(card => 
        {
            // 各比較タイプに応じたフィルタリング
            switch (selectedMaxDamageComparisonType)
            {
                case SetMaxDamageArea.DamageComparisonType.LessOrEqual:
                    return card.maxDamage <= selectedMaxDamage;
                case SetMaxDamageArea.DamageComparisonType.Equal:
                    return card.maxDamage == selectedMaxDamage;
                case SetMaxDamageArea.DamageComparisonType.GreaterOrEqual:
                    return card.maxDamage >= selectedMaxDamage;
                default:
                    return false;
            }
        }).ToList();
        
        Debug.Log($"🔍 最大ダメージフィルター結果: {filteredCards.Count}件");
        
        // フィルタリング後の最大ダメージを持つカードの数を確認
        cardsWithMaxDamageCount = filteredCards.Count(card => card.maxDamage > 0);
        Debug.Log($"🔍 フィルタリング後の最大ダメージを持つカード数: {cardsWithMaxDamageCount}枚");
        
        // 最大ダメージの分布を出力
        Dictionary<int, int> damageDistribution = new Dictionary<int, int>();
        foreach (var card in filteredCards)
        {
            if (!damageDistribution.ContainsKey(card.maxDamage))
            {
                damageDistribution[card.maxDamage] = 0;
            }
            damageDistribution[card.maxDamage]++;
        }
        
        Debug.Log("🔍 フィルタリング後の最大ダメージ分布:");
        foreach (var kv in damageDistribution.OrderBy(kv => kv.Key))
        {
            Debug.Log($"  🔍 {kv.Key}ダメージ: {kv.Value}枚");
        }
    }
    
    // ----------------------------------------------------------------------
    // 現在のフィルタリング結果を取得
    // @return フィルタリングされたカードリスト
    // ----------------------------------------------------------------------
    public List<CardModel> GetFilteredCards()
    {
        return new List<CardModel>(filteredCards);
    }

    public List<CardModel> Search(
        List<CardType> cardTypes,
        List<EvolutionStage> evolutionStages,
        List<PokemonType> types,
        List<CardPack> cardPacks,
        int minHP,
        int maxHP,
        int minMaxDamage,
        int maxMaxDamage,
        int minEnergyCost,
        int maxEnergyCost
    )
    {
        Debug.Log("🔍 [SearchModel] 検索開始");
        
        // 検索条件の有無をチェック
        bool hasCardTypeFilter = cardTypes != null && cardTypes.Count > 0;
        bool hasEvolutionStageFilter = evolutionStages != null && evolutionStages.Count > 0;
        bool hasTypeFilter = types != null && types.Count > 0;
        bool hasCardPackFilter = cardPacks != null && cardPacks.Count > 0;
        bool hasHPFilter = minHP > 0 || maxHP < 999;
        bool hasMaxDamageFilter = minMaxDamage > 0 || maxMaxDamage < 999;
        bool hasEnergyCostFilter = minEnergyCost > 0 || maxEnergyCost < 999;
        
        // 適用するフィルター条件をログ出力
        Debug.Log($"🔍 [SearchModel] フィルター条件: カードタイプ({hasCardTypeFilter}), 進化段階({hasEvolutionStageFilter}), タイプ({hasTypeFilter}), カードパック({hasCardPackFilter}), HP({hasHPFilter}), 最大ダメージ({hasMaxDamageFilter}), エネルギーコスト({hasEnergyCostFilter})");
        
        // CardDatabaseまたはcardListからカードを取得
        List<CardModel> allCards = null;
        
        // カードリストが直接設定されている場合はそれを使用
        if (cardList != null && cardList.Count > 0)
        {
            allCards = cardList;
            Debug.Log($"🔍 [SearchModel] cardListから{allCards.Count}枚のカードを検索対象として使用します");
        }
        // それ以外の場合はCardDatabaseから取得
        else if (CardDatabase.Instance != null)
        {
            allCards = CardDatabase.GetAllCards();
            if (allCards != null)
            {
                Debug.Log($"🔍 [SearchModel] CardDatabaseから{allCards.Count}枚のカードを取得しました");
            }
            else
            {
                Debug.LogError("❌ [SearchModel] CardDatabaseからカードを取得できませんでした");
                return new List<CardModel>();
            }
        }
        else
        {
            Debug.LogError("❌ [SearchModel] カードリストとCardDatabaseの両方が利用できません");
            return new List<CardModel>();
        }

        // フィルターの適用
        var filteredCards = allCards.Where(card => {
            // フィルター条件がない場合は全カード表示（OR条件）
            bool matchCardType = !hasCardTypeFilter || cardTypes.Contains(card.cardTypeEnum);
            bool matchEvolutionStage = !hasEvolutionStageFilter || evolutionStages.Contains(card.evolutionStageEnum);
            bool matchType = !hasTypeFilter || types.Contains(card.typeEnum);
            bool matchCardPack = !hasCardPackFilter || cardPacks.Contains(card.packEnum);
            bool matchHP = card.hp >= minHP && card.hp <= maxHP;
            bool matchMaxDamage = card.maxDamage >= minMaxDamage && card.maxDamage <= maxMaxDamage;

            // すべての条件にマッチするか（AND条件）
            return matchCardType && matchEvolutionStage && matchType && matchCardPack && matchHP && matchMaxDamage;
        }).ToList();
        
        Debug.Log($"🔍 [SearchModel] 検索結果: 全{allCards.Count}枚のカードから{filteredCards.Count}枚が条件に一致しました");
        
        return filteredCards;
    }
}
