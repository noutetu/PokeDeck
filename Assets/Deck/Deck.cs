using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using Newtonsoft.Json;

// ----------------------------------------------------------------------
// デッキ情報を管理するモデルクラス
// 20枚のカード、デッキ名、必要エネルギーリストを持つ
// ----------------------------------------------------------------------
[Serializable]
public class Deck 
{
    // デッキの名前
    [SerializeField] private string _name = "";
    public string Name
    {
        get => _name;
        set => _name = value;
    }

    // デッキカードのID一覧（最大20枚）
    [SerializeField] private List<string> _cardIds = new List<string>();
    public IReadOnlyList<string> CardIds => _cardIds.AsReadOnly();

    // 必要エネルギーのIDと数量のリスト
    [SerializeField] private List<EnergyRequirement> _energyRequirements = new List<EnergyRequirement>();
    public IReadOnlyList<EnergyRequirement> EnergyRequirements => _energyRequirements.AsReadOnly();

    // 最大カード枚数
    public const int MAX_CARDS = 20;
    
    // 同名カードの最大枚数
    public const int MAX_SAME_NAME_CARDS = 2;
    
    // カードID → CardModelのマッピング（参照を保持、JSON化されない）
    [JsonIgnore]
    private Dictionary<string, CardModel> _cardModels = new Dictionary<string, CardModel>();
    
    // カード名 → 枚数のマッピング（同名カード制限用）
    [JsonIgnore]
    private Dictionary<string, int> _cardNameCounts = new Dictionary<string, int>();

    /// <summary>
    /// 現在のカード枚数を取得
    /// </summary>
    public int CardCount => _cardIds.Count;

    /// <summary>
    /// 同名カードの枚数を取得
    /// </summary>
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
    
    /// <summary>
    /// カードIDからCardModelを取得
    /// </summary>
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

    /// <summary>
    /// カードをデッキに追加（20枚制限あり）
    /// </summary>
    /// <param name="cardId">追加するカードID</param>
    /// <returns>追加に成功したかどうか</returns>
    public bool AddCard(string cardId)
    {
        if (_cardIds.Count >= MAX_CARDS)
            return false;
        
        // CardDatabaseからカードモデルを取得
        CardModel cardModel = null;
        if (CardDatabase.Instance != null)
        {
            cardModel = CardDatabase.Instance.GetCard(cardId);
        }
        
        if (cardModel == null)
        {
            Debug.LogWarning($"カードID:{cardId}のモデルが見つかりません");
            return false;
        }
        
        // 同名カードの制限チェック
        if (!string.IsNullOrEmpty(cardModel.name))
        {
            int sameNameCount = GetSameNameCardCount(cardModel.name);
            if (sameNameCount >= MAX_SAME_NAME_CARDS)
            {
                Debug.LogWarning($"同名カード「{cardModel.name}」は{MAX_SAME_NAME_CARDS}枚までしか追加できません");
                return false;
            }
        }

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
        
        UpdateEnergyRequirements();
        return true;
    }

    /// <summary>
    /// カードモデルをデッキに追加（20枚制限あり）
    /// </summary>
    /// <param name="card">追加するカードモデル</param>
    /// <returns>追加に成功したかどうか</returns>
    public bool AddCard(CardModel card)
    {
        if (card == null || string.IsNullOrEmpty(card.id))
            return false;
        
        if (_cardIds.Count >= MAX_CARDS)
            return false;
            
        // 同名カードの制限チェック
        if (!string.IsNullOrEmpty(card.name))
        {
            int sameNameCount = GetSameNameCardCount(card.name);
            if (sameNameCount >= MAX_SAME_NAME_CARDS)
            {
                Debug.LogWarning($"同名カード「{card.name}」は{MAX_SAME_NAME_CARDS}枚までしか追加できません");
                return false;
            }
        }
        
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
        
        UpdateEnergyRequirements();
        return true;
    }

    /// <summary>
    /// カードをデッキから削除
    /// </summary>
    /// <param name="cardId">削除するカードID</param>
    /// <returns>削除に成功したかどうか</returns>
    public bool RemoveCard(string cardId)
    {
        // カード名を取得（同名カードカウント更新用）
        string cardName = null;
        CardModel model = GetCardModel(cardId);
        if (model != null)
        {
            cardName = model.name;
        }
        
        bool result = _cardIds.Remove(cardId);
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
            
            // キャッシュから削除（参照は残しておいてもよい）
            // _cardModels.Remove(cardId);
            
            UpdateEnergyRequirements();
        }
        return result;
    }

    /// <summary>
    /// 指定位置のカードをデッキから削除
    /// </summary>
    /// <param name="index">削除するカードのインデックス</param>
    /// <returns>削除に成功したかどうか</returns>
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
        
        UpdateEnergyRequirements();
        return true;
    }

    /// <summary>
    /// デッキを空にする
    /// </summary>
    public void ClearDeck()
    {
        _cardIds.Clear();
        _cardNameCounts.Clear();
        _energyRequirements.Clear();
        // _cardModels.Clear(); // キャッシュはクリアしなくてもよい
    }

    /// <summary>
    /// 必要エネルギーリストを更新
    /// </summary>
    public void UpdateEnergyRequirements()
    {
        // エネルギー要件をクリア
        _energyRequirements.Clear();
        
        // カードデータベースから各カードの必要エネルギーを取得して集計
        Dictionary<string, int> energyCounts = new Dictionary<string, int>();
        
        foreach (string cardId in _cardIds)
        {
            // カードモデルを取得
            CardModel card = GetCardModel(cardId);
            if (card != null && card.moves != null)
            {
                // 各技のエネルギー要件を集計
                foreach (var move in card.moves)
                {
                    if (move.cost != null)
                    {
                        foreach (var energy in move.cost)
                        {
                            string energyType = energy.Key;
                            int count = energy.Value;
                            
                            if (energyCounts.ContainsKey(energyType))
                                energyCounts[energyType] += count;
                            else
                                energyCounts[energyType] = count;
                        }
                    }
                }
            }
        }
        
        // 集計結果をリストに変換
        foreach (var pair in energyCounts)
        {
            _energyRequirements.Add(new EnergyRequirement(pair.Key, pair.Value));
        }
    }

    /// <summary>
    /// デッキが有効かどうかをチェック（20枚ちょうどか）
    /// </summary>
    /// <returns>デッキが有効かどうか</returns>
    public bool IsValid()
    {
        return _cardIds.Count == MAX_CARDS;
    }
    
    /// <summary>
    /// デッキ読み込み後の処理
    /// カード名カウントの再構築を行う
    /// </summary>
    public void OnAfterDeserialize()
    {
        // 同名カードカウントを再構築
        _cardNameCounts.Clear();
        foreach (string cardId in _cardIds)
        {
            CardModel model = GetCardModel(cardId);
            if (model != null && !string.IsNullOrEmpty(model.name))
            {
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
    }

    /// <summary>
    /// デッキ内のカードをID順に並べ替える
    /// </summary>
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
        
        Debug.Log($"デッキ「{_name}」のカードをID順に並べ替えました：{_cardIds.Count}枚");
    }

    /// <summary>
    /// デッキ内のカード参照を復元する
    /// カードIDからCardModelオブジェクトへの参照を再構築する
    /// アプリ再起動時やカードデータベース更新時に呼び出す
    /// </summary>
    public void RestoreCardReferences()
    {
        // CardModelsコレクションをクリア
        _cardModels.Clear();
        
        if (CardDatabase.Instance == null)
        {
            Debug.LogWarning("CardDatabaseが利用できないため、カード参照を復元できません。デッキ表示が正しく行われない場合があります。");
            return;
        }
        
        // カードIDからCardModelオブジェクトへの参照を再構築
        foreach (string cardId in _cardIds)
        {
            CardModel cardModel = CardDatabase.Instance.GetCard(cardId);
            if (cardModel != null)
            {
                // カードモデルの参照を保存
                _cardModels[cardId] = cardModel;
                Debug.Log($"カードIDの参照を復元しました: {cardId}, カード名: {cardModel.name}");
            }
            else
            {
                Debug.LogWarning($"カードID {cardId} に対応するカードモデルが見つかりません。このカードはデッキに表示されない可能性があります。");
            }
        }
        
        Debug.Log($"デッキ '{Name}' のカード参照を復元しました: {_cardModels.Count}/{_cardIds.Count} 枚");
    }

    /// <summary>
    /// カードIDのみをデッキに追加する（内部用）
    /// 検証やエネルギー更新を行わずシンプルに追加
    /// </summary>
    /// <param name="cardId">追加するカードID</param>
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

    /// <summary>
    /// デッキ内のカードをカードタイプ順、その後ID順に並び替える
    /// エネルギー、特殊エネルギー、スタジアムは除外してリストの最後に配置
    /// </summary>
    public void SortCardsByTypeAndID()
    {
        if (_cardIds == null || _cardIds.Count <= 1)
            return;
            
        // ソート前のカード情報をログ
        Debug.Log($"デッキ「{_name}」のソート前: {_cardIds.Count}枚");
        LogCardOrder("ソート前", _cardIds);
            
        // カードIDのリストをクローン
        List<string> sortedCardIds = new List<string>(_cardIds);
        
        // LINQ を使用して、まずカードタイプで並べ替えた後、ID順で並べ替える（順序を変更）
        sortedCardIds = sortedCardIds
            .OrderBy(id => GetCardTypeSortPriority(GetCardModel(id)))  // まずカードタイプ順（優先度が低い順）
            .ThenBy(id => id)  // 次にID順（セット番号順）
            .ToList();
        
        // 並び替えたIDリストで元のリストを置き換え
        _cardIds = sortedCardIds;
        
        // ソート後のカード情報をログ
        LogCardOrder("ソート後", _cardIds);
        
        Debug.Log($"デッキ「{_name}」のカードをカードタイプ順→ID順に並び替えました：{_cardIds.Count}枚");
    }
    
    /// <summary>
    /// カードの並び順をログに出力
    /// </summary>
    private void LogCardOrder(string prefix, List<string> cardIds)
    {
        if (cardIds == null || cardIds.Count == 0)
            return;
            
        Debug.Log($"===== {prefix} カード一覧 =====");
        
        for (int i = 0; i < cardIds.Count; i++)
        {
            string cardId = cardIds[i];
            CardModel card = GetCardModel(cardId);
            
            if (card != null)
            {
                string cardType = "";
                string evolutionStage = "";
                int priority = GetCardTypeSortPriority(card);
                
                // カードタイプまたは進化段階を文字列で表現
                if (!string.IsNullOrEmpty(card.evolutionStage))
                {
                    evolutionStage = $"進化段階: {card.evolutionStage}";
                }
                
                cardType = $"タイプ: {card.cardType}";
                
                Debug.Log($"[{i+1:D2}] ID: {cardId}, 名前: {card.name}, {evolutionStage} {cardType}, 優先度: {priority}");
            }
            else
            {
                Debug.Log($"[{i+1:D2}] ID: {cardId}, カードモデルなし");
            }
        }
        
        Debug.Log($"===== {prefix} カード一覧終了 =====");
    }

    /// <summary>
    /// カードタイプに基づいてソート優先度を返す
    /// （数値が小さいほど先頭に表示される）
    /// エネルギー、特殊エネルギー、スタジアムは除外
    /// </summary>
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
}

/// <summary>
/// エネルギー要件を表す構造体
/// </summary>
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
