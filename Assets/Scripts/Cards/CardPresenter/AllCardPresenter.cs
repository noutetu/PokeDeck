using UnityEngine;
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
    // フィールドとプロパティ
    // ----------------------------------------------------------------------
    private AllCardModel model;                          // 保持するモデル参照

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
    // ----------------------------------------------------------------------
    public void LoadCards(List<CardModel> cards)
    {
        // モデルにデータを設定
        model.SetCards(cards);
        
        // CardDatabaseにもカードを登録して永続化（アプリ再起動時にも使えるように）
        if (CardDatabase.Instance != null)
        {
            foreach (var card in cards)
            {
                CardDatabase.Instance.RegisterCard(card, false); // 保存しないように設定
            }
            
            // すべてのカードを登録した後に1回だけ保存
            if (cards.Count > 0)
            {
                CardDatabase.Instance.SaveCardDatabase();
            }
        }
        
        // 表示用コレクションをクリアして新しいデータを追加
        DisplayedCards.Clear();
        foreach (var card in cards)
        {
            DisplayedCards.Add(card);
        }
        
        // 読み込み完了イベントを発行（Viewが購読して表示を更新）
        OnLoadComplete.OnNext(Unit.Default);
    }

    // ----------------------------------------------------------------------
    // カードデータの追加
    // 既存のデータを保持したまま、新しいデータを追加する
    // @param newCards 追加するカードのリスト
    // ----------------------------------------------------------------------
    public async Task AddCardsAsync(List<CardModel> newCards)
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
            foreach (var card in uniqueNewCards)
            {
                CardDatabase.Instance.RegisterCard(card, false); // 保存しないように設定
            }
            
            // すべてのカードを登録した後に1回だけ保存
            if (uniqueNewCards.Count > 0)
            {
                CardDatabase.Instance.SaveCardDatabase();
            }
        }
        
        // 表示用コレクションに新しいカードを追加
        foreach (var card in uniqueNewCards)
        {
            DisplayedCards.Add(card);
        }
        
        // 追加完了を通知（スクロール位置を保持するモード）
        onCardsAppended.OnNext(Unit.Default);
        
        // 読み込み完了イベントを発行（Viewが購読して表示を更新）
        OnLoadComplete.OnNext(Unit.Default);

        await Task.CompletedTask;
    }
    
    // ----------------------------------------------------------------------
    // カードデータのクリア
    // 表示中のカードをすべて削除する
    // ----------------------------------------------------------------------
    public void ClearCards()
    {
        // 表示用コレクションをクリア
        DisplayedCards.Clear();
        
        // 読み込み完了イベントを発行（Viewが購読して表示を更新）
        OnLoadComplete.OnNext(Unit.Default);
    }
}