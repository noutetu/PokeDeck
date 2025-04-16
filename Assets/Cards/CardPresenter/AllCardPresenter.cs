using UnityEngine;
using UniRx;
using System.Collections.Generic;


// ----------------------------------------------------------------------
// Presenter：データをViewに渡す役割
// ----------------------------------------------------------------------
public class AllCardPresenter
{
    private AllCardModel model;
    public ReactiveCollection<CardModel> DisplayedCards { get; private set; } = new ReactiveCollection<CardModel>();
    // 読み込み完了イベント
public Subject<Unit> OnLoadComplete { get; } = new Subject<Unit>();

    public AllCardPresenter(AllCardModel model)
    {
        this.model = model;
    }

    public void LoadCards(List<CardModel> cards)
    {
        model.SetCards(cards);
        DisplayedCards.Clear();
        foreach (var card in cards)
        {
            DisplayedCards.Add(card);
        }
        OnLoadComplete.OnNext(Unit.Default); // 読み込み完了イベントを発行
    }
}