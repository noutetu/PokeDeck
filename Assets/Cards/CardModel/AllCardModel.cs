using System;
using System.Collections.Generic;
using UnityEngine;

// ----------------------------------------------------------------------
// 全カードのデータを保持するモデル
// ----------------------------------------------------------------------
[Serializable]
public class AllCardModel
{
    public List<CardModel> cards;

    public void SetCards(List<CardModel> newCards)
    {
        cards = newCards;
    } 
    public List<CardModel> GetAllCards() => cards;
}

