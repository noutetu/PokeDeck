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

    // Enumと対応するアイコンの辞書
    public Dictionary<Type, Sprite> typeIcons = new Dictionary<Type, Sprite>
    {
        { Type.草, Resources.Load<Sprite>("Icons/Grass") },
        { Type.炎, Resources.Load<Sprite>("Icons/Fire") },
        { Type.水, Resources.Load<Sprite>("Icons/Water") },
        { Type.雷, Resources.Load<Sprite>("Icons/Electric") },
        { Type.闘, Resources.Load<Sprite>("Icons/Fighting") },
        { Type.超, Resources.Load<Sprite>("Icons/Psychic") },
        { Type.悪, Resources.Load<Sprite>("Icons/Dark") },
        { Type.鋼, Resources.Load<Sprite>("Icons/Steel") },
        { Type.ドラゴン, Resources.Load<Sprite>("Icons/Dragon") },
        { Type.無色, Resources.Load<Sprite>("Icons/Colorless") }
    };

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
    public void AddCards(List<CardModel> newCards)
    {
        foreach (var card in newCards)
        {
            DisplayedCards.Add(card);
        }
        OnLoadComplete.OnNext(Unit.Default); // 読み込み完了イベントを発行
    }

}