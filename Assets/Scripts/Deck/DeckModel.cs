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
    public const int MAX_SELECTED_ENERGIES = 2;

    // ----------------------------------------------------------------------
    // カードの順序を更新する（シャッフル結果をデータと同期させるため）
    // ----------------------------------------------------------------------
    public void UpdateCardOrder(List<string> newCardIds)
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
    public const int MAX_CARDS = 20;
    
    // ----------------------------------------------------------------------
    // 同名カードの最大枚数
    // ----------------------------------------------------------------------
    public const int MAX_SAME_NAME_CARDS = 2;

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
        if (_cardIds.Count >= MAX_CARDS + 4)
            return false;
        
        // CardDatabaseからカードモデルを取得
        CardModel cardModel = null;
        if (CardDatabase.Instance != null)
        {
            cardModel = CardDatabase.Instance.GetCard(cardId);
        }
        
        // 同名カードの制限チェック
        if (!string.IsNullOrEmpty(cardModel.name))
        {
            int sameNameCount = GetSameNameCardCount(cardModel.name);
            if (sameNameCount >= MAX_SAME_NAME_CARDS)
            {
                return false;
            }
        }

        // カードIDを追加
        _cardIds.Add(cardId);
        
        // CardModelの参照を保持
        _cardModels[cardId] = cardModel;
        
        // 同名カードのカウント更新
        if (!string.IsNullOrEmpty(cardModel.name))
        {
            if (_cardNameCounts.ContainsKey(cardModel.name))
            {
                _cardNameCounts[cardModel.name]++;
            }
            else
            {
                _cardNameCounts[cardModel.name] = 1;
            }
        }
        return true;
    }

    // ----------------------------------------------------------------------
    // カードモデルをデッキに追加（24枚まで追加可能）
    // ----------------------------------------------------------------------
    public bool AddCard(CardModel card)
    {
        if (card == null || string.IsNullOrEmpty(card.id))
            return false;
        
        if (_cardIds.Count >= MAX_CARDS + 4)
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
            if (_cardNameCounts.ContainsKey(card.name))
            {
                _cardNameCounts[card.name]++;
            }
            else
            {
                _cardNameCounts[card.name] = 1;
            }
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
            if (!string.IsNullOrEmpty(cardName) && _cardNameCounts.ContainsKey(cardName))
            {
                _cardNameCounts[cardName]--;
                if (_cardNameCounts[cardName] <= 0)
                {
                    _cardNameCounts.Remove(cardName);
                }
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
        if (!string.IsNullOrEmpty(cardName) && _cardNameCounts.ContainsKey(cardName))
        {
            _cardNameCounts[cardName]--;
            if (_cardNameCounts[cardName] <= 0)
            {
                _cardNameCounts.Remove(cardName);
            }
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
        // 同名カードカウントを再構築
        _cardNameCounts.Clear();
        foreach (string cardId in _cardIds)
        {
            // カードモデルを取得
            CardModel model = GetCardModel(cardId);
            // カードモデルが存在する場合、カード名をカウント
            if (model != null && !string.IsNullOrEmpty(model.name))
            {
                // カード名のカウントを更新
                if (_cardNameCounts.ContainsKey(model.name))
                {
                    _cardNameCounts[model.name]++;
                }
                else
                {
                    _cardNameCounts[model.name] = 1;
                }
            }
        }

        // 選択されたエネルギータイプの情報をログ出力
        if (_selectedEnergyTypes != null && _selectedEnergyTypes.Count > 0)
        {
            List<string> typeNames = new List<string>();
            foreach (var et in _selectedEnergyTypes)
            {
                typeNames.Add(et.ToString());
            }
            string energyNames = string.Join(", ", typeNames);
        }
    }

    // ----------------------------------------------------------------------
    // デッキ内のカードをID順に並べ替える
    // ----------------------------------------------------------------------
    public void SortCardsByID()
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
        // CardModelsコレクションをクリア
        _cardModels.Clear();

        // カードIDからCardModelオブジェクトへの参照を再構築
        int restoredCount = 0;
        int missingCount = 0;

        foreach (string cardId in _cardIds)
        {
            // CardDatabaseからカードモデルを取得
            CardModel cardModel = CardDatabase.Instance.GetCard(cardId);
            if (cardModel != null)
            {
                // カードモデルの参照を保存
                _cardModels[cardId] = cardModel;
                restoredCount++;
            }
            else
            {
                missingCount++;
            }
        }
    }

    // ----------------------------------------------------------------------
    // カードIDのみをデッキに追加する（内部用）
    // 検証やエネルギー更新を行わずシンプルに追加
    // ----------------------------------------------------------------------
    internal bool _AddCardId(string cardId)
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

    // ----------------------------------------------------------------------
    // デッキ内のカードをカードタイプ順、その後ID順に並び替える
    // ----------------------------------------------------------------------
    public void SortCardsByTypeAndID()
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
            return 999; // 無効なカードは最後尾
            
        // 進化段階とカードタイプで優先度を決定
        // 1. まず進化段階で分類
        if (!string.IsNullOrEmpty(card.evolutionStage))
        {
            switch (card.evolutionStage)
            {
                case "たね":
                    return 10; // たねポケモン
                case "1進化":
                    return 20; // 1進化ポケモン
                case "2進化":
                    return 30; // 2進化ポケモン
            }
        }
        
        // 2. 進化段階がない場合はカードタイプで判断
        switch (card.cardTypeEnum)
        {
            case Enum.CardType.化石:
                return 40;
            case Enum.CardType.グッズ:
                return 50;
            case Enum.CardType.ポケモンのどうぐ:
                return 60;
            case Enum.CardType.サポート:
                return 70;
            default:
                return 800; // その他のカードは後方に配置
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
        // すでにエネルギータイプが選択されている場合は何もしない
        if (_selectedEnergyTypes.Count > 0)
            return;
            
        // 実際に選択可能なエネルギータイプ（ドラゴンと無色を除外）
        HashSet<Enum.PokemonType> validEnergyTypes = new HashSet<Enum.PokemonType>
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
            
        // ポケモンタイプの出現回数をカウントする辞書
        Dictionary<Enum.PokemonType, int> typeCount = new Dictionary<Enum.PokemonType, int>();
        
        // デッキ内の各カードについて処理
        foreach (string cardId in _cardIds)
        {
            // カードモデルを取得
            CardModel cardModel = GetCardModel(cardId);
            if (cardModel != null && 
                (cardModel.cardTypeEnum == Enum.CardType.非EX || cardModel.cardTypeEnum == Enum.CardType.EX))
            {
                if (cardModel.typeEnum == Enum.PokemonType.ドラゴン)
                {
                    // ドラゴンタイプのポケモンは炎と水のエネルギーを使う傾向が強いため、
                    // これらのタイプをカウントに追加
                    if (typeCount.ContainsKey(Enum.PokemonType.炎))
                        typeCount[Enum.PokemonType.炎]++;
                    else
                        typeCount[Enum.PokemonType.炎] = 1;
                        
                    if (typeCount.ContainsKey(Enum.PokemonType.水))
                        typeCount[Enum.PokemonType.水]++;
                    else
                        typeCount[Enum.PokemonType.水] = 1;
                }
                else if (cardModel.typeEnum == Enum.PokemonType.無色)
                {
                    // 無色タイプのポケモンは特に対応するエネルギーがないためカウントしない
                    // 必要に応じて、デッキ内の他のカードのタイプに基づいて選択する
                }
                else if (validEnergyTypes.Contains(cardModel.typeEnum))
                {
                    // 選択可能なエネルギータイプの場合、カウントを追加
                    if (typeCount.ContainsKey(cardModel.typeEnum))
                        typeCount[cardModel.typeEnum]++;
                    else
                        typeCount[cardModel.typeEnum] = 1;
                }
            }
        }
        
        // タイプのカウントがない場合は終了
        if (typeCount.Count == 0)
            return;
            
        // タイプの出現回数で降順にソート（同じ数の場合はEnum値の小さい順）
        var sortedTypes = typeCount.OrderByDescending(pair => pair.Value)
                                  .ThenBy(pair => (int)pair.Key)
                                  .Take(MAX_SELECTED_ENERGIES)
                                  .Select(pair => pair.Key)
                                  .ToList();
        
        // 選択されたタイプを設定
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
