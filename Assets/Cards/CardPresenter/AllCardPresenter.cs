using UnityEngine;
using UniRx;
using System.Collections.Generic;
using System.Linq;

// ----------------------------------------------------------------------
// Presenter：データをViewに渡す役割
// MVRPパターンにおいて、ModelとViewの間の通信を担当
// ----------------------------------------------------------------------
public class AllCardPresenter
{
    // ----------------------------------------------------------------------
    // フィールドとプロパティ
    // ----------------------------------------------------------------------
    private AllCardModel model;                          // 保持するモデル参照

    // 表示用のカードデータコレクション（ReactiveCollectionでリアクティブに通知）
    public ReactiveCollection<CardModel> DisplayedCards { get; private set; } = new ReactiveCollection<CardModel>();
    
    // 読み込み完了イベント（Viewが購読してカード表示を更新する）
    public Subject<Unit> OnLoadComplete { get; } = new Subject<Unit>();

    // ----------------------------------------------------------------------
    // コンストラクタ - モデルの注入とアイコン初期化
    // @param model 使用するデータモデル
    // ----------------------------------------------------------------------
    public AllCardPresenter(AllCardModel model)
    {
        this.model = model;
    }

    // ----------------------------------------------------------------------
    // カードデータの一括読み込み
    // 既存のデータをクリアし、新しいデータで置き換える
    // @param cards 読み込むカードのリスト
    // ----------------------------------------------------------------------
    public void LoadCards(List<CardModel> cards)
    {
        // モデルにデータを設定
        model.SetCards(cards);
        
        // CardDatabaseにもカードを登録して永続化（アプリ再起動時にも使えるように）
        if (CardDatabase.Instance != null)
        {
            Debug.Log($"🔄 AllCardPresenter: {cards.Count}枚のカードをCardDatabaseに登録します");
            foreach (var card in cards)
            {
                CardDatabase.Instance.RegisterCard(card);
            }
        }
        else
        {
            Debug.LogWarning("⚠️ AllCardPresenter: CardDatabaseが初期化されていません。カードデータの永続化ができません。");
        }
        
        // 表示用コレクションをクリアして新しいデータを追加
        DisplayedCards.Clear();
        foreach (var card in cards)
        {
            DisplayedCards.Add(card);
        }
        
        // 読み込み完了イベントを発行（Viewが購読して表示を更新）
        OnLoadComplete.OnNext(Unit.Default);
        
        Debug.Log($"✅ AllCardPresenter: {cards.Count}枚のカードを読み込みました");
    }

    // ----------------------------------------------------------------------
    // カードデータの追加
    // 既存のデータを保持したまま、新しいデータを追加する
    // @param newCards 追加するカードのリスト
    // ----------------------------------------------------------------------
    public void AddCards(List<CardModel> newCards)
    {
        // 重複を避けるための処理
        var existingIds = new HashSet<string>(DisplayedCards.Select(c => c.id));
        var uniqueNewCards = newCards.Where(c => !existingIds.Contains(c.id)).ToList();
        
        // モデルにデータを追加
        if (model.cards == null)
        {
            model.cards = new List<CardModel>();
        }
        model.cards.AddRange(uniqueNewCards);
        
        // CardDatabaseにも新しいカードを登録
        if (CardDatabase.Instance != null)
        {
            Debug.Log($"🔄 AllCardPresenter: 新しく{uniqueNewCards.Count}枚のカードをCardDatabaseに登録します");
            foreach (var card in uniqueNewCards)
            {
                CardDatabase.Instance.RegisterCard(card);
            }
        }
        
        // 表示用コレクションに新しいカードを追加
        foreach (var card in uniqueNewCards)
        {
            DisplayedCards.Add(card);
        }
        
        // 読み込み完了イベントを発行（Viewが購読して表示を更新）
        OnLoadComplete.OnNext(Unit.Default);
        
        Debug.Log($"✅ AllCardPresenter: 新しく{uniqueNewCards.Count}枚のカードを追加しました（合計: {DisplayedCards.Count}枚）");
    }
}