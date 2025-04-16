using UnityEngine;
using UniRx;
using System.Collections.Generic;

// ----------------------------------------------------------------------
// 複数カードを並べて表示するView（横スクロール）
// ----------------------------------------------------------------------
public class AllCardView : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform contentParent;

    public void BindPresenter(AllCardPresenter presenter)
    {
        presenter.OnLoadComplete
            .Subscribe(_ => RefreshAll(presenter.DisplayedCards))
            .AddTo(this);
    }


    private void RefreshAll(ReactiveCollection<CardModel> cards)
    {
        Debug.Log("カードのリフレッシュ");
        Debug.Log("カードの数: " + cards.Count);
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var card in cards)
        {
            AddCard(card);
        }
    }

    private void AddCard(CardModel card)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("❌ cardPrefabがnullだよ！");
            return;
        }

        var go = Instantiate(cardPrefab, contentParent);
        var view = go.GetComponent<CardView>();

        if (view == null)
        {
            Debug.LogError("❌ CardViewがプレハブにアタッチされてないよ！");
            return;
        }

        Debug.Log("カードをインスタンス化: " + card.name);
        view.Setup(card);
    }

}