using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Enum;
using System.Text; // ひらがな・カタカナ変換用
using TMPro;  // TMP_InputField用
using UnityEngine.UI; // Buttonコンポーネント用

// ----------------------------------------------------------------------
// カード検索のモデルクラス
// 検索条件の管理とフィルタリング処理を担当する
// ----------------------------------------------------------------------
public class SearchModel : MonoBehaviour
{
    // シングルトンインスタンス
    public static SearchModel Instance { get; private set; }

    // 検索フィールドへの参照
    [SerializeField] private TMP_InputField searchInputField;

    // ----------------------------------------------------------------------
    // 遅延検索用定数・変数
    // ----------------------------------------------------------------------
    private const float SEARCH_DELAY = 0.3f; // 検索実行までの遅延時間（秒）
    private const int MIN_SEARCH_LENGTH = 2; // 最小検索文字数
    private float currentSearchDelayTimer = 0f; // 遅延タイマー
    private bool searchIsDue = false; // 検索が要求されているか
    private string lastExecutedSearchText = ""; // 最後に実行した検索テキスト（重複実行防止用）

    // ----------------------------------------------------------------------
    // カードデータ管理用の変数
    // ----------------------------------------------------------------------
    private List<CardModel> allCards = new List<CardModel>();
    private List<CardModel> filteredCards = new List<CardModel>();
    private List<CardModel> cardList = null; // 外部から設定されたカードリスト

    // ----------------------------------------------------------------------
    // 検索条件
    // ----------------------------------------------------------------------

    // 検索テキスト
    private string searchText = "";

    // 最後に検索を実行した時間（テキスト検索の遅延用）
    // private float lastSearchTime = 0f; // 遅延検索メカニズム変更のためコメントアウトまたは削除

    // バッチフィルタリング中かどうか
    private bool isBatchFiltering = false;

    // フィルター条件
    private HashSet<CardType> selectedCardTypes = new HashSet<CardType>();
    private HashSet<EvolutionStage> selectedEvolutionStages = new HashSet<EvolutionStage>();
    private HashSet<PokemonType> selectedPokemonTypes = new HashSet<PokemonType>();
    private HashSet<CardPack> selectedCardPacks = new HashSet<CardPack>();

    // HP関連のフィルター
    private int selectedHP = 0;
    private SetHPArea.HPComparisonType selectedHPComparisonType = SetHPArea.HPComparisonType.None;

    // 最大ダメージ関連のフィルター
    private int selectedMaxDamage = 0;
    private SetMaxDamageArea.DamageComparisonType selectedMaxDamageComparisonType = SetMaxDamageArea.DamageComparisonType.None;

    // 最大エネルギーコスト関連のフィルター
    private int selectedMaxEnergyCost = 0;
    private SetMaxEnergyArea.EnergyComparisonType selectedMaxEnergyCostComparisonType = SetMaxEnergyArea.EnergyComparisonType.None;

    // 逃げるコスト関連のフィルター
    private int selectedRetreatCost = 0;
    private SetRetreatCostArea.RetreatComparisonType selectedRetreatCostComparisonType = SetRetreatCostArea.RetreatComparisonType.None;

    // ----------------------------------------------------------------------
    // MonoBehaviourのライフサイクル
    // ----------------------------------------------------------------------
    private void Awake()
    {
        // シングルトンの初期化
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // InputFieldの設定
        SetupSearchInputField();

        // モデルの初期化
        Initialize();
    }

    // ----------------------------------------------------------------------
    // 検索入力フィールドのセットアップ
    // ----------------------------------------------------------------------
    private void SetupSearchInputField()
    {
        if (searchInputField == null)
        {
            Debug.LogWarning("🔍 SearchModel: searchInputFieldが設定されていません。");
            return;
        }

        // 検索入力フィールドの初期化
        searchInputField.text = searchText; // 初期テキストを反映

        // テキスト変更時のイベント
        searchInputField.onValueChanged.AddListener((text) =>
        {
            // テキスト変更を即座に保存
            this.searchText = text;
            // 検索をリクエスト（遅延実行）
            RequestSearch();
        });

        // Enterキーを押したときや入力完了時の処理
        searchInputField.onEndEdit.AddListener((text) =>
        {
            // 入力完了時にsearchTextを更新し、即座に検索実行
            this.searchText = text;
            ExecuteSearchAndFilters();
            lastExecutedSearchText = text; // 重複実行防止のため記録
            searchIsDue = false; // 遅延検索キューをキャンセル
            currentSearchDelayTimer = 0f;
        });

        Debug.Log("🔍 SearchModel: InputFieldのリスナー設定完了");
    }

    // ----------------------------------------------------------------------
    // 更新処理（フレーム毎）- 入力の遅延対策
    // ----------------------------------------------------------------------
    private void Update()
    {
        if (searchIsDue)
        {
            currentSearchDelayTimer -= Time.deltaTime;
            if (currentSearchDelayTimer <= 0f)
            {
                // 重複実行防止: 前回と同じ検索テキストの場合はスキップ
                if (searchText != lastExecutedSearchText)
                {
                    ExecuteSearchAndFilters();
                    lastExecutedSearchText = searchText;
                }
                searchIsDue = false;
            }
        }
    }

    // ----------------------------------------------------------------------
    // 検索テキストを設定
    // @param text 検索テキスト
    // ----------------------------------------------------------------------
    public void SetSearchText(string text)
    {
        // 同じテキストの場合は処理をスキップ（パフォーマンス向上）
        if (searchText == text)
        {
            return;
        }
        
        searchText = text;
        
        // 最小文字数チェック：空文字またはMIN_SEARCH_LENGTH未満の場合
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < MIN_SEARCH_LENGTH)
        {
            // 最小文字数未満の場合は全カードを表示（フィルターのみ適用）
            RequestSearchWithoutTextFilter();
        }
        else
        {
            // 最小文字数以上の場合は通常の検索をリクエスト
            RequestSearch();
        }
    }

    // ----------------------------------------------------------------------
    // 検索リクエスト - 遅延後に検索を実行するようマーク
    // ----------------------------------------------------------------------
    public void RequestSearch() // publicに変更して外部からも呼び出せるように
    {
        // 既に同じ検索がスケジュールされている場合はタイマーを更新するだけ
        searchIsDue = true;
        currentSearchDelayTimer = SEARCH_DELAY;
    }

    // ----------------------------------------------------------------------
    // テキストフィルターなしの検索リクエスト（最小文字数未満の場合）
    // ----------------------------------------------------------------------
    private void RequestSearchWithoutTextFilter()
    {
        // テキスト検索をスキップして他のフィルターのみを適用
        ExecuteSearchAndFilters(); // 既存のメソッドを使用（テキストが空の場合は自動的にテキストフィルターはスキップされる）
    }

    // ----------------------------------------------------------------------
    // ひらがな・カタカナを同一視するための文字列正規化
    // -----------------------------------------------------------------------
    private string NormalizeJapanese(string input)
    {
        // 入力がnullまたは空の場合は空文字を返す
        if (string.IsNullOrEmpty(input)) return "";
        var sb = new StringBuilder(input.Length);
        // 文字列を1文字ずつ処理
        foreach (var ch in input)
        {
            // 全角カタカナ(U+30A1〜U+30F6)をひらがなに変換
            if (ch >= '\u30A1' && ch <= '\u30F6') sb.Append((char)(ch - 0x60));
            else sb.Append(ch);
        }
        return sb.ToString().ToLowerInvariant();
    }

    // ----------------------------------------------------------------------
    // コンポーネント破棄時の処理
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        // 検索入力フィールドのリスナーを解除
        if (searchInputField != null)
        {
            searchInputField.onEndEdit.RemoveAllListeners();
            searchInputField.onValueChanged.RemoveAllListeners();

            var searchIcon = searchInputField.transform.Find("Search Button");
            if (searchIcon != null && searchIcon.GetComponent<Button>() != null)
            {
                searchIcon.GetComponent<Button>().onClick.RemoveAllListeners();
            }
        }

        // シングルトンインスタンスの解除
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ----------------------------------------------------------------------
    // コンストラクタ
    // ----------------------------------------------------------------------
    // MonoBehaviourベースに変更したため、コンストラクタは不要になります

    // ----------------------------------------------------------------------
    // フィルタリングの一括処理を開始
    // -----------------------------------------------------------------------
    public void BeginBatchFiltering()
    {
        isBatchFiltering = true;
    }

    // ----------------------------------------------------------------------
    // フィルタリングの一括処理を終了して適用
    // ----------------------------------------------------------------------
    public void EndBatchFiltering()
    {
        isBatchFiltering = false;
        RequestSearch(); // バッチ処理完了後に検索をリクエスト
    }

    // ----------------------------------------------------------------------
    // モデルの初期化
    // ----------------------------------------------------------------------
    public void Initialize()
    {
        LoadCards();
        RequestSearch(); // 初期表示のために検索をリクエスト
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
    // CardUIManagerなどから読み込み済みのカードデータを設定できるようにする
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
            // 検索条件も初期化
            ClearAllFilters();
        }
        else
        {
            Debug.LogError("❌ SetCards()にnullが渡されました");
        }
    }

    // ----------------------------------------------------------------------
    // カードタイプフィルターを設定
    // @param cardTypes 検索するカードタイプのセット
    // ----------------------------------------------------------------------
    public void SetCardTypeFilter(HashSet<CardType> cardTypes)
    {
        selectedCardTypes = new HashSet<CardType>(cardTypes);
        if (!isBatchFiltering) RequestSearch();
    }

    // ----------------------------------------------------------------------
    // 進化段階フィルターを設定
    // @param evolutionStages 検索する進化段階のセット
    // ----------------------------------------------------------------------
    public void SetEvolutionStageFilter(HashSet<EvolutionStage> evolutionStages)
    {
        selectedEvolutionStages = new HashSet<EvolutionStage>(evolutionStages);
        if (!isBatchFiltering) RequestSearch();
    }

    // ----------------------------------------------------------------------
    // ポケモンタイプフィル
    // @param pokemonTypes 検索するポケモンタイプのセット
    // ----------------------------------------------------------------------
    public void SetPokemonTypeFilter(HashSet<PokemonType> pokemonTypes)
    {
        selectedPokemonTypes = new HashSet<PokemonType>(pokemonTypes);
        if (!isBatchFiltering) RequestSearch();
    }

    // ----------------------------------------------------------------------
    // カードパックフィルターを設定
    // @param cardPacks 植物するカードパックのセット
    // ----------------------------------------------------------------------
    public void SetCardPackFilter(HashSet<CardPack> cardPacks)
    {
        selectedCardPacks = new HashSet<CardPack>(cardPacks);
        if (!isBatchFiltering) RequestSearch();
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
        if (!isBatchFiltering) RequestSearch();
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
        if (!isBatchFiltering) RequestSearch();
    }
    // ----------------------------------------------------------------------
    // 最大エネルギーコストフィルターを設定
    // ----------------------------------------------------------------------
    public void SetMaxEnergyCostFilter(int cost, SetMaxEnergyArea.EnergyComparisonType comparisonType)
    {
        selectedMaxEnergyCost = cost;
        selectedMaxEnergyCostComparisonType = comparisonType;
        if (!isBatchFiltering) RequestSearch();
    }
    // ----------------------------------------------------------------------
    // 逃げるコストフィルターを設定
    // @param cost 検索する逃げるコスト値
    // @param comparisonType 比較タイプ（以下、同じ、以上のいずれか）
    // ----------------------------------------------------------------------
    public void SetRetreatCostFilter(int cost, SetRetreatCostArea.RetreatComparisonType comparisonType)
    {
        selectedRetreatCost = cost;
        selectedRetreatCostComparisonType = comparisonType;
        if (!isBatchFiltering) RequestSearch();
    }

    // ----------------------------------------------------------------------
    // すべてのフィルターをクリア
    // ----------------------------------------------------------------------
    public void ClearAllFilters()
    {
        searchText = "";
        lastExecutedSearchText = ""; // 重複実行防止変数もリセット
        if (searchInputField != null)
        {
            searchInputField.text = ""; // 紐づけられたInputFieldもクリア
        }
        selectedCardTypes.Clear();
        selectedEvolutionStages.Clear();
        selectedPokemonTypes.Clear();
        selectedCardPacks.Clear();
        // filteredCards = new List<CardModel>(allCards); // ExecuteSearchAndFiltersが処理するので不要

        // HPフィルターをリセット
        selectedHP = 0;
        selectedHPComparisonType = SetHPArea.HPComparisonType.None;

        // 最大ダメージフィルターをリセット
        selectedMaxDamage = 0;
        selectedMaxDamageComparisonType = SetMaxDamageArea.DamageComparisonType.None;

        // エネルギーコストフィルターをリセット
        selectedMaxEnergyCost = 0;
        selectedMaxEnergyCostComparisonType = SetMaxEnergyArea.EnergyComparisonType.None;

        // 逃げるコストフィルターをリセット
        selectedRetreatCost = 0;
        selectedRetreatCostComparisonType = SetRetreatCostArea.RetreatComparisonType.None;

        RequestSearch(); // 表示を更新
    }

    // ----------------------------------------------------------------------
    // フィルタリングと検索を適用 (旧 ApplyFilters)
    // ----------------------------------------------------------------------
    public void ExecuteSearchAndFilters()
    {
        // 最初に全カードをベースにする
        if (allCards == null)
        {
            Debug.LogWarning("🔍 ExecuteSearchAndFilters: allCards is null. LoadCards might not have run or failed.");
            allCards = new List<CardModel>(); // NullGuard
        }
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

        // 最大エネルギーコストフィルター適用
        ApplyMaxEnergyCostFilter();

        // 逃げるコストフィルター適用
        ApplyRetreatCostFilter();

        // もし全カード表示でない場合は、フィルタリング件数をフィードバック
        if (filteredCards.Count != allCards.Count)
        {
            // フィードバックを表示
            FeedbackContainer.Instance.ShowSuccessFeedback($" 検索結果: {filteredCards.Count}件", 0.8f);
        }

        // 最後に実行した検索テキストを記録（重複実行防止）
        lastExecutedSearchText = searchText;

        // 検索結果をUIに反映
        if (SearchNavigator.Instance != null)
        {
            SearchNavigator.Instance.ApplySearchResults(filteredCards);
        }
        else
        {
            Debug.LogWarning("🔍 SearchNavigator.Instance が null のため、検索結果を適用できません。");
        }
    }

    // ----------------------------------------------------------------------
    // テキスト検索フィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyTextFilter()
    {
        if (string.IsNullOrWhiteSpace(searchText)) return;

        // 検索テキストを正規化
        string searchNorm = NormalizeJapanese(searchText);
        
        filteredCards = filteredCards.Where(card =>
        {
            // カード名マッチ (正規化)
            if (card.name != null && NormalizeJapanese(card.name).Contains(searchNorm))
            {
                return true;
            }
            
            // 特性の効果文マッチ (正規化)
            if (!string.IsNullOrEmpty(card.abilityEffect) && 
                NormalizeJapanese(card.abilityEffect).Contains(searchNorm))
            {
                return true;
            }

            // 技マッチ (正規化) - 技が存在する場合
            if (card.moves != null)
            {
                foreach (var move in card.moves)
                {
                    // 技の効果文のみマッチ (正規化)
                    if (move.effect != null && NormalizeJapanese(move.effect).Contains(searchNorm))
                    {
                        return true;
                    }
                }
            }
            return false;
        }).ToList();
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

        // 修正：ポケモンカードのみ対象とし、選択された進化段階に合致するもののみ表示
        filteredCards = filteredCards.Where(card =>
            // 選択された進化段階に合致するポケモンカードのみを表示
            (card.cardTypeEnum == CardType.非EX || card.cardTypeEnum == CardType.EX) &&
             !string.IsNullOrEmpty(card.evolutionStage) &&
             selectedEvolutionStages.Contains(card.evolutionStageEnum)
        ).ToList();
    }

    // ----------------------------------------------------------------------
    // ポケモンタイプフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyPokemonTypeFilter()
    {
        // ポケモンタイプフィルターが設定されていない場合はスキップ
        if (selectedPokemonTypes.Count == 0) return;

        // ポケモンタイプによるフィルタリング（ポケモンカードのみを対象に）
        filteredCards = filteredCards.Where(card =>
            // ポケモンカード（非EXまたはEX）かつポケモンタイプが設定されており、選択されたタイプに一致するもののみを表示
            (card.cardTypeEnum == CardType.非EX || card.cardTypeEnum == CardType.EX) &&
            !string.IsNullOrEmpty(card.type) &&
            selectedPokemonTypes.Contains(card.typeEnum)
        ).ToList();

    }

    // ----------------------------------------------------------------------
    // カードパックフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyCardPackFilter()
    {
        // カードパックフィルターが設定されていない場合はスキップ
        if (selectedCardPacks.Count == 0) return;

        // カードパックによるフィルタリング
        // パック情報が設定されているカードのみを対象にする
        filteredCards = filteredCards.Where(card =>
            !string.IsNullOrEmpty(card.pack) && selectedCardPacks.Contains(card.packEnum)
        ).ToList();
    }

    // ----------------------------------------------------------------------
    // HPフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyHPFilter()
    {
        // HP比較タイプが設定されていないか、HP値が0（指定なし）の場合はスキップ
        if (selectedHPComparisonType == SetHPArea.HPComparisonType.None || selectedHP <= 0)
        {
            return;
        }

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
            return;
        }

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
    }
    // ----------------------------------------------------------------------
    // 最大エネルギーコストフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyMaxEnergyCostFilter()
    {
        // EnergyComparisonType.None(指定なし)の場合はスキップ
        if (selectedMaxEnergyCostComparisonType == SetMaxEnergyArea.EnergyComparisonType.None)
        {
            return;
        }

        // CardModel.maxEnergyCost を直接比較
        filteredCards = filteredCards.Where(card =>
        {
            int energy = card.maxEnergyCost;
            bool matches = false;

            switch (selectedMaxEnergyCostComparisonType)
            {
                case SetMaxEnergyArea.EnergyComparisonType.LessOrEqual:
                    matches = energy <= selectedMaxEnergyCost;
                    break;
                case SetMaxEnergyArea.EnergyComparisonType.Equal:
                    matches = energy == selectedMaxEnergyCost;
                    break;
                case SetMaxEnergyArea.EnergyComparisonType.GreaterOrEqual:
                    matches = energy >= selectedMaxEnergyCost;
                    break;
                default:
                    matches = true; // フィルタリングしない
                    break;
            }

            return matches;
        }).ToList();
    }

    // ----------------------------------------------------------------------
    // 逃げるコストフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyRetreatCostFilter()
    {
        // RetreatComparisonType.None(指定なし)の場合はスキップ
        if (selectedRetreatCostComparisonType == SetRetreatCostArea.RetreatComparisonType.None)
        {
            return;
        }

        // 逃げるコストフィルター適用
        filteredCards = filteredCards.Where(card =>
        {
            // ポケモンカードのみを対象とする
            if (card.cardTypeEnum != CardType.非EX && card.cardTypeEnum != CardType.EX)
            {
                return false; // ポケモンカードでなければフィルター対象外
            }

            // 各比較タイプに応じたフィルタリング
            switch (selectedRetreatCostComparisonType)
            {
                case SetRetreatCostArea.RetreatComparisonType.LessOrEqual:
                    return card.retreatCost <= selectedRetreatCost;
                case SetRetreatCostArea.RetreatComparisonType.Equal:
                    return card.retreatCost == selectedRetreatCost;
                case SetRetreatCostArea.RetreatComparisonType.GreaterOrEqual:
                    return card.retreatCost >= selectedRetreatCost;
                default:
                    return true; // フィルタリングしない
            }
        }).ToList();
    }

    // ----------------------------------------------------------------------
    // 現在のフィルタリング結果を取得
    // @return フィルタリングされたカードリスト
    // ----------------------------------------------------------------------
    public List<CardModel> GetFilteredCards()
    {
        return new List<CardModel>(filteredCards);
    }

    // ----------------------------------------------------------------------
    // 現在のフィルタリング条件を取得

    // ----------------------------------------------------------------------
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
        int maxEnergyCost,
        int minRetreatCost,
        int maxRetreatCost
    )
    {

        // 検索条件の有無を適切にチェック
        bool hasCardTypeFilter = cardTypes != null && cardTypes.Count > 0;
        bool hasEvolutionStageFilter = evolutionStages != null && evolutionStages.Count > 0;
        bool hasTypeFilter = types != null && types.Count > 0;
        bool hasCardPackFilter = cardPacks != null && cardPacks.Count > 0;

        // システムのデフォルト値定義
        const int minDefaultHP = 30;        // 最小HP（デフォルト）
        const int maxDefaultHP = 200;       // 最大HP（デフォルト）
        const int minDefaultDamage = 0;     // 最小最大ダメージ（デフォルト）
        const int maxDefaultDamage = 200;   // 最大最大ダメージ（デフォルト）
        const int minDefaultEnergyCost = 0; // 最小エネルギーコスト（デフォルト）
        const int maxDefaultEnergyCost = 5; // 最大エネルギーコスト（デフォルト）
        const int minDefaultRetreatCost = 0;// 最小逃げるコスト（デフォルト）
        const int maxDefaultRetreatCost = 4;// 最大逃げるコスト（デフォルト）

        // フィルターが明示的に設定されている場合のみtrue（デフォルト値と異なる場合）
        bool hasHPFilter = minHP > minDefaultHP || maxHP < maxDefaultHP;
        bool hasMaxDamageFilter = minMaxDamage > minDefaultDamage || maxMaxDamage < maxDefaultDamage;
        bool hasEnergyCostFilter = minEnergyCost > minDefaultEnergyCost || maxEnergyCost < maxDefaultEnergyCost;
        bool hasRetreatCostFilter = minRetreatCost > minDefaultRetreatCost || maxRetreatCost < maxDefaultRetreatCost;

        // カードリストが直接設定されている場合はそれを使用
        List<CardModel> allCards = null;
        if (cardList != null && cardList.Count > 0)
        {
            allCards = cardList;
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
                return new List<CardModel>();
            }
        }
        else
        {
            return new List<CardModel>();
        }

        // フィルターの適用
        var filteredCards = allCards.Where(card =>
        {
            // フィルター条件がない場合は全カード表示（条件は AND 条件で、フィルターがなければ自動的に true）
            bool matchCardType = !hasCardTypeFilter || cardTypes.Contains(card.cardTypeEnum);
            bool matchEvolutionStage = !hasEvolutionStageFilter || evolutionStages.Contains(card.evolutionStageEnum);
            bool matchType = !hasTypeFilter || types.Contains(card.typeEnum);
            bool matchCardPack = !hasCardPackFilter || cardPacks.Contains(card.packEnum);

            // 数値フィルターの条件判定を修正
            bool matchHP = !hasHPFilter || (card.hp >= minHP && card.hp <= maxHP);
            bool matchMaxDamage = !hasMaxDamageFilter || (card.maxDamage >= minMaxDamage && card.maxDamage <= maxMaxDamage);
            bool matchEnergyCost = !hasEnergyCostFilter || (card.maxEnergyCost >= minEnergyCost && card.maxEnergyCost <= maxEnergyCost);
            bool matchRetreatCost = !hasRetreatCostFilter || (card.retreatCost >= minRetreatCost && card.retreatCost <= maxRetreatCost);

            // すべての条件にマッチするか（AND条件）
            return matchCardType && matchEvolutionStage && matchType && matchCardPack
                && matchHP && matchMaxDamage && matchEnergyCost && matchRetreatCost;
        }).ToList();

        return filteredCards;
    }

    // ----------------------------------------------------------------------
    // テキスト検索と現在のフィルターを組み合わせて実行
    // @param searchText 検索テキスト
    // ----------------------------------------------------------------------
    public void PerformTextSearchAndFilter(string searchText)
    {
        // 検索テキストを保存
        this.searchText = searchText;

        // 現在のフィルター条件に基づいて結果を取得
        ExecuteSearchAndFilters(); // filteredCardsフィールドが更新される
        List<CardModel> currentlyFilteredCards = new List<CardModel>(this.filteredCards);

        // 検索テキストが空の場合は、フィルターのみの結果を表示
        if (string.IsNullOrWhiteSpace(searchText))
        {
            if (SearchNavigator.Instance != null)
            {
                SearchNavigator.Instance.ApplySearchResults(currentlyFilteredCards);
            }
            return;
        }

        // テキスト検索とフィルター結果を組み合わせる
        string searchNorm = NormalizeJapanese(searchText);
        var results = new List<CardModel>();

        foreach (var card in currentlyFilteredCards) // ExecuteSearchAndFiltersによって更新されたfilteredCardsを使用
        {
            // カード名マッチ (正規化)
            var nameNorm = NormalizeJapanese(card.name);
            if (nameNorm.Contains(searchNorm))
            {
                results.Add(card);
                continue;
            }
            
            // 特性の効果文マッチ (正規化)
            if (!string.IsNullOrEmpty(card.abilityEffect))
            {
                var abilityEffectNorm = NormalizeJapanese(card.abilityEffect);
                if (abilityEffectNorm.Contains(searchNorm))
                {
                    results.Add(card);
                    continue;
                }
            }

            // 技の効果文マッチ (正規化)
            if (card.moves != null)
            {
                bool found = false;
                foreach (var move in card.moves)
                {
                    var effectNorm = NormalizeJapanese(move.effect);
                    if (effectNorm.Contains(searchNorm))
                    {
                        results.Add(card);
                        found = true;
                        break;
                    }
                }
                if (found) continue;
            }
        }

        // 最終的な結果を表示
        if (SearchNavigator.Instance != null)
        {
            SearchNavigator.Instance.ApplySearchResults(results);
        }
    }
}