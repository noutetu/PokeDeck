using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using Newtonsoft.Json;

// ----------------------------------------------------------------------
// デッキ情報を管理するモデルクラス
// 20枚のカード、デッキ名、必要エネルギーリスト,デッキメモを持つ
// ----------------------------------------------------------------------
[Serializable]
public class DeckModel 
{
    // ----------------------------------------------------------------------
    // 定数クラス
    // ----------------------------------------------------------------------
    private static class Constants
    {
        // デッキ関連制約
        public const int DEFAULT_DECK_SIZE = 20;
        public const int EXTRA_CARDS_BUFFER = 4;
        public const int MAX_TOTAL_CARDS = DEFAULT_DECK_SIZE + EXTRA_CARDS_BUFFER; // 24枚
        public const int MAX_SAME_NAME_CARDS = 2;
        
        // ソート優先度
        public const int SORT_PRIORITY_BASIC_POKEMON = 10;
        public const int SORT_PRIORITY_STAGE1_POKEMON = 20;
        public const int SORT_PRIORITY_STAGE2_POKEMON = 30;
        public const int SORT_PRIORITY_FOSSIL = 40;
        public const int SORT_PRIORITY_GOODS = 50;
        public const int SORT_PRIORITY_TOOL = 60;
        public const int SORT_PRIORITY_SUPPORTER = 70;
        public const int SORT_PRIORITY_OTHER = 800;
        public const int SORT_PRIORITY_INVALID = 999;
        
        // エネルギー選択制約
        public const int MAX_SELECTED_ENERGY_TYPES = 2;
        
        // 進化段階名
        public const string EVOLUTION_BASIC = "たね";
        public const string EVOLUTION_STAGE1 = "1進化";
        public const string EVOLUTION_STAGE2 = "2進化";
        
        // エラーメッセージ
        public const string ERROR_CARD_MODEL_NULL = "カードモデルが取得できませんでした: {0}";
        public const string ERROR_CARD_DATABASE_UNAVAILABLE = "CardDatabaseが利用できません";
        public const string ERROR_CARD_NAME_COUNT_UPDATE = "同名カードカウント更新中にエラー: {0}";
        public const string ERROR_AUTO_ENERGY_SELECT = "エネルギータイプ自動選択中にエラー: {0}";
        public const string ERROR_CARD_REFERENCE_RESTORE = "カード参照復元中にエラー: {0}";
    }

    // ----------------------------------------------------------------------
    // デッキの名前
    // ----------------------------------------------------------------------
    [SerializeField] private string _name = "";
    public string Name
    {
        get => _name;
        set => _name = value;
    }

    // ----------------------------------------------------------------------
    // デッキカードのID一覧（最大20枚）
    // ----------------------------------------------------------------------
    [SerializeField] private List<string> _cardIds = new List<string>();
    public IReadOnlyList<string> CardIds => _cardIds.AsReadOnly();

    // ----------------------------------------------------------------------
    // 必要エネルギーのIDと数量のリスト
    // ----------------------------------------------------------------------
    [SerializeField] private List<EnergyRequirement> _energyRequirements = new List<EnergyRequirement>();
    public IReadOnlyList<EnergyRequirement> EnergyRequirements => _energyRequirements.AsReadOnly();
    
    // ----------------------------------------------------------------------
    // 選択されたエネルギータイプのリスト（最大2つまで）
    // ----------------------------------------------------------------------
    [SerializeField] private List<Enum.PokemonType> _selectedEnergyTypes = new List<Enum.PokemonType>();
    public IReadOnlyList<Enum.PokemonType> SelectedEnergyTypes => _selectedEnergyTypes.AsReadOnly();
    // ----------------------------------------------------------------------
    // 選択可能なエネルギーの最大数
    // ----------------------------------------------------------------------
    public const int MAX_SELECTED_ENERGIES = Constants.MAX_SELECTED_ENERGY_TYPES;

    // ----------------------------------------------------------------------
    // コンストラクタ(通常用)
    // ----------------------------------------------------------------------
    public DeckModel()
    {
        // デフォルトコンストラクタ
    }
    // ----------------------------------------------------------------------
    // コンストラクタ(コピー用)
    // -----------------------------------------------------------------------
    public DeckModel(DeckModel deckModel)
    {
        if (deckModel == null)
            return;

        _name = deckModel._name;
        _cardIds = new List<string>(deckModel._cardIds);
        _energyRequirements = new List<EnergyRequirement>(deckModel._energyRequirements);
        _selectedEnergyTypes = new List<Enum.PokemonType>(deckModel._selectedEnergyTypes);
    }    // ----------------------------------------------------------------------
    // カードの順序を更新する（シャッフル結果をデータと同期させるため）
    // ----------------------------------------------------------------------
    public void UpdateCardOrder(List<string> newCardIds)
    {
        try
        {
            ExecuteSafeCardOrderUpdate(newCardIds);
        }
        catch (Exception ex)
        {
            Debug.LogError($"カード順序更新中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // カード順序更新を安全に実行
    // ----------------------------------------------------------------------
    private void ExecuteSafeCardOrderUpdate(List<string> newCardIds)
    {
        if (newCardIds == null || newCardIds.Count == 0)
            return;

        // 既存のカードIDと新しいカードIDの内容が一致するかの確認
        bool sameContents = _cardIds.Count == newCardIds.Count &&
                           _cardIds.All(id => newCardIds.Contains(id)) &&
                           newCardIds.All(id => _cardIds.Contains(id));
        
        if (!sameContents)
        {
            return;
        }

        // カードIDリストを更新
        _cardIds.Clear();
        _cardIds.AddRange(newCardIds);
    }
    
    // ----------------------------------------------------------------------
    // デッキメモ（ユーザーが自由に記入できるメモ）
    // ----------------------------------------------------------------------
    private string _memo = "";
    public string Memo
    {
        get => _memo;
        set => _memo = value;
    }
    // ----------------------------------------------------------------------
    // 最大カード枚数
    // ----------------------------------------------------------------------
    public const int MAX_CARDS = Constants.DEFAULT_DECK_SIZE;
    
    // ----------------------------------------------------------------------
    // 同名カードの最大枚数
    // ----------------------------------------------------------------------
    public const int MAX_SAME_NAME_CARDS = Constants.MAX_SAME_NAME_CARDS;

    // ----------------------------------------------------------------------
    // デッキが有効かどうかをチェック（20枚以下か）
    // ----------------------------------------------------------------------
    public bool IsValid()
    {
        return _cardIds.Count <= MAX_CARDS;
    }
    
    // ----------------------------------------------------------------------
    // カードID → CardModelのマッピング（参照を保持、JSON化されない）
    // ----------------------------------------------------------------------
    [JsonIgnore]
    private Dictionary<string, CardModel> _cardModels = new Dictionary<string, CardModel>();
    
    // ----------------------------------------------------------------------
    // カード名 → 枚数のマッピング（同名カード制限用）
    // ----------------------------------------------------------------------
    [JsonIgnore]
    private Dictionary<string, int> _cardNameCounts = new Dictionary<string, int>();

    // ----------------------------------------------------------------------
    // 現在のカード枚数を取得
    // ----------------------------------------------------------------------
    public int CardCount => _cardIds.Count;

    // ----------------------------------------------------------------------
    // 同名カードの枚数を取得
    // ----------------------------------------------------------------------
    public int GetSameNameCardCount(string cardName)
    {
        if (string.IsNullOrEmpty(cardName))
            return 0;
            
        if (_cardNameCounts.TryGetValue(cardName, out int count))
        {
            return count;
        }
        return 0;
    }
    
    // ----------------------------------------------------------------------
    // カードIDからCardModelを取得
    // ----------------------------------------------------------------------
    public CardModel GetCardModel(string cardId)
    {
        if (string.IsNullOrEmpty(cardId))
            return null;
            
        // 内部キャッシュから取得
        if (_cardModels.TryGetValue(cardId, out CardModel model))
        {
            return model;
        }
        
        // グローバルデータベースから取得を試みる
        if (CardDatabase.Instance != null)
        {
            model = CardDatabase.Instance.GetCard(cardId);
            if (model != null)
            {
                // 見つかったらキャッシュに追加
                _cardModels[cardId] = model;
                return model;
            }
        }
        
        return null;
    }

    // ----------------------------------------------------------------------
    // カードをデッキに追加（24枚まで追加可能、保存時は20枚以下）
    // ----------------------------------------------------------------------
    public bool AddCard(string cardId)
    {
        try
        {
            return ExecuteSafeCardAddition(cardId);
        }
        catch (Exception ex)
        {
            Debug.LogError($"カード追加中にエラー (CardID: {cardId}): {ex.Message}");
            Debug.LogException(ex);
            return false;
        }
    }

    // ----------------------------------------------------------------------
    // カード追加を安全に実行
    // ----------------------------------------------------------------------
    private bool ExecuteSafeCardAddition(string cardId)
    {
        if (_cardIds.Count >= Constants.MAX_TOTAL_CARDS)
            return false;
        
        // CardDatabaseからカードモデルを取得
        CardModel cardModel = null;
        if (CardDatabase.Instance != null)
        {
            cardModel = CardDatabase.Instance.GetCard(cardId);
        }
        
        // 同名カード制限の検証
        if (!ValidateSameNameCardLimit(cardModel))
            return false;

        // カードIDを追加
        _cardIds.Add(cardId);
        
        // CardModelの参照を保持
        _cardModels[cardId] = cardModel;
        
        // 同名カードのカウント更新
        if (!string.IsNullOrEmpty(cardModel?.name))
        {
            UpdateSameNameCardCount(cardModel.name, true);
        }
        return true;
    }

    // ----------------------------------------------------------------------
    // 同名カード制限を検証
    // ----------------------------------------------------------------------
    private bool ValidateSameNameCardLimit(CardModel cardModel)
    {
        if (!string.IsNullOrEmpty(cardModel?.name))
        {
            int sameNameCount = GetSameNameCardCount(cardModel.name);
            if (sameNameCount >= MAX_SAME_NAME_CARDS)
            {
                return false;
            }
        }
        return true;
    }

    // ----------------------------------------------------------------------
    // 同名カードカウントを更新する（共通処理）
    // @param cardName カード名
    // @param increment true:カウント増加、false:カウント減少
    // ----------------------------------------------------------------------
    private void UpdateSameNameCardCount(string cardName, bool increment)
    {
        try
        {
            if (string.IsNullOrEmpty(cardName))
                return;

            if (increment)
            {
                // カウント増加
                if (_cardNameCounts.ContainsKey(cardName))
                {
                    _cardNameCounts[cardName]++;
                }
                else
                {
                    _cardNameCounts[cardName] = 1;
                }
            }
            else
            {
                // カウント減少
                if (_cardNameCounts.ContainsKey(cardName))
                {
                    _cardNameCounts[cardName]--;
                    if (_cardNameCounts[cardName] <= 0)
                    {
                        _cardNameCounts.Remove(cardName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_CARD_NAME_COUNT_UPDATE, ex.Message));
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // カードモデルをデッキに追加（24枚まで追加可能）
    // ----------------------------------------------------------------------
    public bool AddCard(CardModel card)
    {
        try
        {
            return ExecuteSafeCardModelAddition(card);
        }
        catch (Exception ex)
        {
            Debug.LogError($"カードモデル追加中にエラー (CardID: {card?.id}): {ex.Message}");
            Debug.LogException(ex);
            return false;
        }
    }

    // ----------------------------------------------------------------------
    // カードモデル追加を安全に実行
    // ----------------------------------------------------------------------
    private bool ExecuteSafeCardModelAddition(CardModel card)
    {
        if (card == null || string.IsNullOrEmpty(card.id))
            return false;
        
        if (_cardIds.Count >= Constants.MAX_TOTAL_CARDS)
            return false;
            
        // 同名カードの制限チェック
        if (!string.IsNullOrEmpty(card.name))
        {
            int sameNameCount = GetSameNameCardCount(card.name);
            if (sameNameCount >= MAX_SAME_NAME_CARDS)
            {
                return false;
            }
        }
        
        // カードIDを追加
        _cardIds.Add(card.id);
        
        // CardModelの参照を保持
        _cardModels[card.id] = card;
        
        // CardDatabaseにも登録（グローバルキャッシュ）
        if (CardDatabase.Instance != null)
        {
            CardDatabase.Instance.RegisterCard(card);
        }
        
        // 同名カードのカウント更新
        if (!string.IsNullOrEmpty(card.name))
        {
            UpdateSameNameCardCount(card.name, true);
        }
        return true;
    }

    // ----------------------------------------------------------------------
    // カードをデッキから削除
    // ----------------------------------------------------------------------
    public bool RemoveCard(string cardId)
    {
        // カード名を取得（同名カードカウント更新用）
        string cardName = null;
        CardModel model = GetCardModel(cardId);
        if (model != null)
        {
            cardName = model.name;
        }
        
        // カードIDを削除
        bool result = _cardIds.Remove(cardId);
        // 削除に成功した場合
        if (result)
        {
            // 同名カードカウントの更新
            if (!string.IsNullOrEmpty(cardName))
            {
                UpdateSameNameCardCount(cardName, false);
            }
        }
        return result;
    }

    // ----------------------------------------------------------------------
    // 指定位置のカードをデッキから削除
    // ----------------------------------------------------------------------
    public bool RemoveCardAt(int index)
    {
        if (index < 0 || index >= _cardIds.Count)
            return false;
        
        // インデックスに対応するカードIDを取得
        string cardId = _cardIds[index];
        
        // カード名を取得（同名カードカウント更新用）
        string cardName = null;
        CardModel model = GetCardModel(cardId);
        if (model != null)
        {
            cardName = model.name;
        }

        _cardIds.RemoveAt(index);
        
        // 同名カードカウントの更新
        if (!string.IsNullOrEmpty(cardName))
        {
            UpdateSameNameCardCount(cardName, false);
        }

        return true;
    }

    // ----------------------------------------------------------------------
    // デッキを空にする
    // ----------------------------------------------------------------------
    public void ClearDeck()
    {
        _cardIds.Clear();
        _cardNameCounts.Clear();
        _energyRequirements.Clear();
        _selectedEnergyTypes.Clear();
    }
    
    // ----------------------------------------------------------------------
    // デッキ読み込み後の処理
    // カード名カウントの再構築を行う
    // ----------------------------------------------------------------------
    public void OnAfterDeserialize()
    {
        try
        {
            ExecuteSafeCardNameCountRebuild();
            ExecuteSafeEnergyTypeLogging();
        }
        catch (Exception ex)
        {
            Debug.LogError($"デッキデシリアライゼーション後処理中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // 同名カードカウントの再構築を安全に実行
    // ----------------------------------------------------------------------
    private void ExecuteSafeCardNameCountRebuild()
    {
        try
        {
            RebuildCardNameCounts();
        }
        catch (Exception ex)
        {
            Debug.LogError($"カード名カウント再構築中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // 同名カードカウントを再構築
    // ----------------------------------------------------------------------
    private void RebuildCardNameCounts()
    {
        _cardNameCounts.Clear();
        
        foreach (string cardId in _cardIds)
        {
            ProcessCardForNameCount(cardId);
        }
    }

    // ----------------------------------------------------------------------
    // カードID に対応するカード名カウントを処理
    // ----------------------------------------------------------------------
    private void ProcessCardForNameCount(string cardId)
    {
        try
        {
            CardModel model = GetCardModel(cardId);
            if (model != null && !string.IsNullOrEmpty(model.name))
            {
                UpdateSameNameCardCount(model.name, true);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_CARD_MODEL_NULL, cardId));
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // エネルギータイプ情報のログ出力を安全に実行
    // ----------------------------------------------------------------------
    private void ExecuteSafeEnergyTypeLogging()
    {
        try
        {
            LogSelectedEnergyTypes();
        }
        catch (Exception ex)
        {
            Debug.LogError($"エネルギータイプログ出力中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // 選択されたエネルギータイプの情報をログ出力
    // ----------------------------------------------------------------------
    private void LogSelectedEnergyTypes()
    {
        if (_selectedEnergyTypes != null && _selectedEnergyTypes.Count > 0)
        {
            List<string> typeNames = new List<string>();
            foreach (var et in _selectedEnergyTypes)
            {
                typeNames.Add(et.ToString());
            }
            string energyNames = string.Join(", ", typeNames);
            // ログ出力（デバッグ用）
            Debug.Log($"選択されたエネルギータイプ: {energyNames}");
        }
    }

    // ----------------------------------------------------------------------
    // デッキ内のカードをID順に並べ替える
    // ----------------------------------------------------------------------
    public void SortCardsByID()
    {
        try
        {
            ExecuteSafeCardSortByID();
        }
        catch (Exception ex)
        {
            Debug.LogError($"カードID順ソート中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // カードID順ソートを安全に実行
    // ----------------------------------------------------------------------
    private void ExecuteSafeCardSortByID()
    {
        if (_cardIds == null || _cardIds.Count <= 1)
            return;
            
        // IDの数値部分で並べ替え
        _cardIds.Sort((a, b) => {
            // IDが数値形式であることを前提とした並べ替え
            // 数値に変換できない場合は文字列として比較
            if (int.TryParse(a, out int idA) && int.TryParse(b, out int idB))
            {
                return idA.CompareTo(idB);
            }
            
            // 数値に変換できない場合は文字列として比較
            return string.Compare(a, b);
        });
    }

    // ----------------------------------------------------------------------
    // デッキ内のカード参照を復元する
    // カードIDからCardModelオブジェクトへの参照を再構築する
    // アプリ再起動時やカードデータベース更新時に呼び出す
    // ----------------------------------------------------------------------
    public void RestoreCardReferences()
    {
        try
        {
            ExecuteSafeCardReferenceRestore();
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_CARD_REFERENCE_RESTORE, ex.Message));
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // カード参照復元を安全に実行
    // ----------------------------------------------------------------------
    private void ExecuteSafeCardReferenceRestore()
    {
        _cardModels.Clear();
        
        int restoredCount = 0;
        int missingCount = 0;
        
        foreach (string cardId in _cardIds)
        {
            if (ProcessCardReferenceRestore(cardId))
                restoredCount++;
            else
                missingCount++;
        }
        
        LogCardReferenceRestoreResults(restoredCount, missingCount);
    }

    // ----------------------------------------------------------------------
    // 個別カードの参照復元を処理
    // ----------------------------------------------------------------------
    private bool ProcessCardReferenceRestore(string cardId)
    {
        try
        {
            if (CardDatabase.Instance == null)
            {
                Debug.LogError(Constants.ERROR_CARD_DATABASE_UNAVAILABLE);
                return false;
            }

            CardModel cardModel = CardDatabase.Instance.GetCard(cardId);
            if (cardModel != null)
            {
                _cardModels[cardId] = cardModel;
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"カード参照復元中にエラー (CardID: {cardId}): {ex.Message}");
            Debug.LogException(ex);
        }
        
        return false;
    }

    // ----------------------------------------------------------------------
    // カード参照復元結果をログ出力
    // ----------------------------------------------------------------------
    private void LogCardReferenceRestoreResults(int restoredCount, int missingCount)
    {
        Debug.Log($"カード参照復元完了: 成功 {restoredCount}件, 失敗 {missingCount}件");
        
        if (missingCount > 0)
        {
            Debug.LogWarning($"一部のカード参照を復元できませんでした: {missingCount}件");
        }
    }

    // ----------------------------------------------------------------------
    // カードIDのみをデッキに追加する（内部用）
    // 検証やエネルギー更新を行わずシンプルに追加
    // ----------------------------------------------------------------------
    internal bool _AddCardId(string cardId)
    {
        try
        {
            if (string.IsNullOrEmpty(cardId))
                return false;
                
            _cardIds.Add(cardId);
            
            // CardDatabase.Instanceが利用可能であれば参照を設定
            if (CardDatabase.Instance != null)
            {
                CardModel cardModel = CardDatabase.Instance.GetCard(cardId);
                if (cardModel != null)
                {
                    _cardModels[cardId] = cardModel;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"カードID追加中にエラー (CardID: {cardId}): {ex.Message}");
            Debug.LogException(ex);
            return false;
        }
    }

    // ----------------------------------------------------------------------
    // デッキ内のカードをカードタイプ順、その後ID順に並び替える
    // ----------------------------------------------------------------------
    public void SortCardsByTypeAndID()
    {
        try
        {
            ExecuteSafeCardSortByTypeAndID();
        }
        catch (Exception ex)
        {
            Debug.LogError($"カードタイプ・ID順ソート中にエラー: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // カードタイプ・ID順ソートを安全に実行
    // ----------------------------------------------------------------------
    private void ExecuteSafeCardSortByTypeAndID()
    {
        if (_cardIds == null || _cardIds.Count <= 1)
            return;
            
        // カードIDのリストをクローン
        List<string> sortedCardIds = new List<string>(_cardIds);
        
        // LINQ を使用して、まずカードタイプで並べ替えた後、ID順で並べ替える（順序を変更）
        sortedCardIds = sortedCardIds
            .OrderBy(id => GetCardTypeSortPriority(GetCardModel(id)))  // まずカードタイプ順（優先度が低い順）
            .ThenBy(id => id)  // 次にID順（セット番号順）
            .ToList();
        
        // 並び替えたIDリストで元のリストを置き換え
        _cardIds = sortedCardIds;
    }
    
    // ----------------------------------------------------------------------
    // カードタイプに基づいてソート優先度を返す
    // （数値が小さいほど先頭に表示される）
    // エネルギー、特殊エネルギー、スタジアムは除外
    // ----------------------------------------------------------------------
    private int GetCardTypeSortPriority(CardModel card)
    {
        if (card == null)
            return Constants.SORT_PRIORITY_INVALID;
            
        // 進化段階とカードタイプで優先度を決定
        // 1. まず進化段階で分類
        if (!string.IsNullOrEmpty(card.evolutionStage))
        {
            switch (card.evolutionStage)
            {
                case Constants.EVOLUTION_BASIC:
                    return Constants.SORT_PRIORITY_BASIC_POKEMON;
                case Constants.EVOLUTION_STAGE1:
                    return Constants.SORT_PRIORITY_STAGE1_POKEMON;
                case Constants.EVOLUTION_STAGE2:
                    return Constants.SORT_PRIORITY_STAGE2_POKEMON;
            }
        }
        
        // 2. 進化段階がない場合はカードタイプで判断
        switch (card.cardTypeEnum)
        {
            case Enum.CardType.化石:
                return Constants.SORT_PRIORITY_FOSSIL;
            case Enum.CardType.グッズ:
                return Constants.SORT_PRIORITY_GOODS;
            case Enum.CardType.ポケモンのどうぐ:
                return Constants.SORT_PRIORITY_TOOL;
            case Enum.CardType.サポート:
                return Constants.SORT_PRIORITY_SUPPORTER;
            default:
                return Constants.SORT_PRIORITY_OTHER;
        }
        
    }

    // ----------------------------------------------------------------------
    // エネルギータイプを追加する（最大MAX_SELECTED_ENERGIES個まで）
    // ----------------------------------------------------------------------
    public bool AddSelectedEnergyType(Enum.PokemonType energyType)
    {
        // 既に最大数選択されている場合は追加できない
        if (_selectedEnergyTypes.Count >= MAX_SELECTED_ENERGIES)
            return false;
            
        // 既に同じタイプが選択されている場合は追加しない
        if (_selectedEnergyTypes.Contains(energyType))
            return false;
            
        // エネルギータイプを追加
        // ただし、無色とドラゴンは除外
        _selectedEnergyTypes.Add(energyType);
        return true;
    }
    
    // ----------------------------------------------------------------------
    // エネルギータイプを削除する
    // ----------------------------------------------------------------------
    public bool RemoveSelectedEnergyType(Enum.PokemonType energyType)
    {
        return _selectedEnergyTypes.Remove(energyType);
    }
    
    // ----------------------------------------------------------------------
    // 選択されたエネルギータイプをすべて削除する
    // ----------------------------------------------------------------------
    public void ClearSelectedEnergyTypes()
    {
        _selectedEnergyTypes.Clear();
    }

    // ----------------------------------------------------------------------
    // デッキ内のポケモンタイプを分析し、最も数の多いタイプを
    // 自動的にエネルギータイプとして選択する（最大2つまで）
    // ----------------------------------------------------------------------
    public void AutoSelectEnergyTypes()
    {
        try
        {
            ExecuteSafeAutoEnergySelection();
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_AUTO_ENERGY_SELECT, ex.Message));
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // エネルギータイプ自動選択を安全に実行
    // ----------------------------------------------------------------------
    private void ExecuteSafeAutoEnergySelection()
    {
        // すでにエネルギータイプが選択されている場合は何もしない
        if (_selectedEnergyTypes.Count > 0)
            return;
            
        var validEnergyTypes = GetValidEnergyTypes();
        var typeCount = AnalyzePokemonTypes(validEnergyTypes);
        
        if (typeCount.Count == 0)
            return;
            
        ApplyAutoSelectedEnergyTypes(typeCount);
    }

    // ----------------------------------------------------------------------
    // 選択可能なエネルギータイプを取得
    // ----------------------------------------------------------------------
    private HashSet<Enum.PokemonType> GetValidEnergyTypes()
    {
        return new HashSet<Enum.PokemonType>
        {
            Enum.PokemonType.草,
            Enum.PokemonType.炎,
            Enum.PokemonType.水,
            Enum.PokemonType.雷,
            Enum.PokemonType.超,
            Enum.PokemonType.闘,
            Enum.PokemonType.悪,
            Enum.PokemonType.鋼
        };
    }

    // ----------------------------------------------------------------------
    // デッキ内のポケモンタイプを分析
    // ----------------------------------------------------------------------
    private Dictionary<Enum.PokemonType, int> AnalyzePokemonTypes(HashSet<Enum.PokemonType> validEnergyTypes)
    {
        var typeCount = new Dictionary<Enum.PokemonType, int>();
        
        foreach (string cardId in _cardIds)
        {
            ProcessCardForTypeAnalysis(cardId, validEnergyTypes, typeCount);
        }
        
        return typeCount;
    }

    // ----------------------------------------------------------------------
    // カードのタイプ分析を処理
    // ----------------------------------------------------------------------
    private void ProcessCardForTypeAnalysis(string cardId, HashSet<Enum.PokemonType> validEnergyTypes, Dictionary<Enum.PokemonType, int> typeCount)
    {
        try
        {
            CardModel cardModel = GetCardModel(cardId);
            if (cardModel == null || !IsPokemonCard(cardModel))
                return;

            ProcessPokemonTypeForCount(cardModel, validEnergyTypes, typeCount);
        }
        catch (Exception ex)
        {
            Debug.LogError($"カードタイプ分析中にエラー (CardID: {cardId}): {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // ポケモンカードかどうかを判定
    // ----------------------------------------------------------------------
    private bool IsPokemonCard(CardModel cardModel)
    {
        return cardModel.cardTypeEnum == Enum.CardType.非EX || cardModel.cardTypeEnum == Enum.CardType.EX;
    }

    // ----------------------------------------------------------------------
    // ポケモンタイプをカウントに追加
    // ----------------------------------------------------------------------
    private void ProcessPokemonTypeForCount(CardModel cardModel, HashSet<Enum.PokemonType> validEnergyTypes, Dictionary<Enum.PokemonType, int> typeCount)
    {
        if (cardModel.typeEnum == Enum.PokemonType.ドラゴン)
        {
            // ドラゴンタイプは炎と水のエネルギーを使う傾向が強い
            IncrementTypeCount(typeCount, Enum.PokemonType.炎);
            IncrementTypeCount(typeCount, Enum.PokemonType.水);
        }
        else if (cardModel.typeEnum == Enum.PokemonType.無色)
        {
            // 無色タイプは特に対応するエネルギーがないためカウントしない
        }
        else if (validEnergyTypes.Contains(cardModel.typeEnum))
        {
            IncrementTypeCount(typeCount, cardModel.typeEnum);
        }
    }

    // ----------------------------------------------------------------------
    // タイプカウントを増加
    // ----------------------------------------------------------------------
    private void IncrementTypeCount(Dictionary<Enum.PokemonType, int> typeCount, Enum.PokemonType type)
    {
        if (typeCount.ContainsKey(type))
            typeCount[type]++;
        else
            typeCount[type] = 1;
    }

    // ----------------------------------------------------------------------
    // 自動選択されたエネルギータイプを適用
    // ----------------------------------------------------------------------
    private void ApplyAutoSelectedEnergyTypes(Dictionary<Enum.PokemonType, int> typeCount)
    {
        var sortedTypes = typeCount.OrderByDescending(pair => pair.Value)
                                  .ThenBy(pair => (int)pair.Key)
                                  .Take(Constants.MAX_SELECTED_ENERGY_TYPES)
                                  .Select(pair => pair.Key)
                                  .ToList();
        
        _selectedEnergyTypes.Clear();
        foreach (var type in sortedTypes)
        {
            _selectedEnergyTypes.Add(type);
        }
    }
}

// ----------------------------------------------------------------------
// エネルギー要件を表す構造体
// ----------------------------------------------------------------------
[Serializable]
public struct EnergyRequirement
{
    // エネルギータイプ（例: "草", "炎"など）
    [SerializeField] private string _type;
    public string Type => _type;

    // 必要な数量
    [SerializeField] private int _count;
    public int Count => _count;

    public EnergyRequirement(string type, int count)
    {
        _type = type;
        _count = count;
    }
}
