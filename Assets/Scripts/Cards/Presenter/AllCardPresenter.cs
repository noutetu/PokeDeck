using UniRx;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

// ----------------------------------------------------------------------
// Presenter：データをViewに渡す役割
// このクラスは、MVRP（Model-View-Reactive-Presenter）パターンにおいて、
// Model（データ層）とView（表示層）の間の通信を担当します。
// 主に以下の責務を持ちます：
// - Modelからデータを取得し、Viewに渡す
// - Viewの更新をトリガーするイベントを発行する
// - データの永続化や追加・削除などの操作を管理する
// このクラスは、カードデータを管理するためのPresenterとして機能し、
// ReactiveCollectionやUniRxを活用してリアクティブなデータ更新を実現します。
// ----------------------------------------------------------------------
public class AllCardPresenter
{
    // ----------------------------------------------------------------------
    // 定数クラス
    // ----------------------------------------------------------------------
    private static class Constants
    {
        public const bool SHOULD_NOT_SAVE_IMMEDIATELY = false;
    }

    // ----------------------------------------------------------------------
    // フィールドとプロパティ
    // ----------------------------------------------------------------------
    private AllCardModel model;

    // 表示用のカードデータコレクション（ReactiveCollectionでリアクティブに通知）
    public ReactiveCollection<CardModel> DisplayedCards { get; private set; } = new ReactiveCollection<CardModel>();
    
    // 読み込み完了イベント（Viewが購読してカード表示を更新する）
    public Subject<Unit> OnLoadComplete { get; } = new Subject<Unit>();

    // カード追加時のイベント通知（追加の場合はスクロール位置を保持）
    private Subject<Unit> onCardsAppended = new Subject<Unit>();

    // カード追加時のイベント（購読用）
    public IObservable<Unit> OnCardsAppended => onCardsAppended;

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
        model.SetCards(cards);
        RegisterCardsToDatabase(cards);
        RefreshDisplayedCards(cards);
    }

    // ----------------------------------------------------------------------
    // カードデータの追加
    // 既存のデータを保持したまま、新しいデータを追加する
    // @param newCards 追加するカードのリスト
    // @returns 非同期タスク
    // ----------------------------------------------------------------------
    public async Task AddCardsAsync(List<CardModel> newCards)
    {
        var uniqueNewCards = GetUniqueCards(newCards);
        AddCardsToModel(uniqueNewCards);
        RegisterCardsToDatabase(uniqueNewCards);
        AddCardsToDisplayCollection(uniqueNewCards);
        NotifyCardsAppended();

        await Task.CompletedTask;
    }
    
    // ----------------------------------------------------------------------
    // カードデータのクリア
    // 表示中のカードをすべて削除する
    // ----------------------------------------------------------------------
    public void ClearCards()
    {
        ClearDisplayedCardsAndNotify();
    }
    
    // ----------------------------------------------------------------------
    // 検索結果に基づいて表示カードを更新
    // @param cards 表示するカードのリスト
    // ----------------------------------------------------------------------
    public void UpdateDisplayedCards(List<CardModel> cards)
    {
        RefreshDisplayedCards(cards);
    }

    // ----------------------------------------------------------------------
    // プライベートヘルパーメソッド
    // ----------------------------------------------------------------------

    // ----------------------------------------------------------------------
    // CardDatabaseにカードを登録し、必要に応じて保存する
    // @param cardsToRegister 登録するカードのリスト
    // ----------------------------------------------------------------------
    private void RegisterCardsToDatabase(List<CardModel> cardsToRegister)
    {
        if (CardDatabase.Instance == null || cardsToRegister.Count == 0)
            return;

        foreach (var card in cardsToRegister)
        {
            CardDatabase.Instance.RegisterCard(card, Constants.SHOULD_NOT_SAVE_IMMEDIATELY);
        }
        
        CardDatabase.Instance.SaveCardDatabase();
    }

    // ----------------------------------------------------------------------
    // 重複を除いたユニークなカードのリストを取得する
    // @param newCards 新しいカードのリスト
    // @returns 重複を除いたカードのリスト
    // ----------------------------------------------------------------------
    private List<CardModel> GetUniqueCards(List<CardModel> newCards)
    {
        var existingCardIds = new HashSet<string>(DisplayedCards.Select(c => c.id));
        return newCards.Where(c => !existingCardIds.Contains(c.id)).ToList();
    }

    // ----------------------------------------------------------------------
    // カードをモデルに追加する
    // @param cardsToAdd 追加するカードのリスト
    // ----------------------------------------------------------------------
    private void AddCardsToModel(List<CardModel> cardsToAdd)
    {
        if (model.cards == null)
        {
            model.cards = new List<CardModel>();
        }
        model.cards.AddRange(cardsToAdd);
    }

    // ----------------------------------------------------------------------
    // カードを表示用コレクションに追加する
    // @param cardsToAdd 追加するカードのリスト
    // ----------------------------------------------------------------------
    private void AddCardsToDisplayCollection(List<CardModel> cardsToAdd)
    {
        foreach (var card in cardsToAdd)
        {
            DisplayedCards.Add(card);
        }
    }

    // ----------------------------------------------------------------------
    // カード追加完了の通知を行う
    // ----------------------------------------------------------------------
    private void NotifyCardsAppended()
    {
        onCardsAppended.OnNext(Unit.Default);
        OnLoadComplete.OnNext(Unit.Default);
    }

    // ----------------------------------------------------------------------
    // 表示用カードコレクションを更新し、完了イベントを発行する
    // @param cards 表示するカードのリスト
    // ----------------------------------------------------------------------
    private void RefreshDisplayedCards(List<CardModel> cards)
    {
        DisplayedCards.Clear();
        
        foreach (var card in cards)
        {
            DisplayedCards.Add(card);
        }
        
        OnLoadComplete.OnNext(Unit.Default);
    }

    // ----------------------------------------------------------------------
    // 表示用カードコレクションをクリアし、完了イベントを発行する
    // ----------------------------------------------------------------------
    private void ClearDisplayedCardsAndNotify()
    {
        DisplayedCards.Clear();
        OnLoadComplete.OnNext(Unit.Default);
    }
}