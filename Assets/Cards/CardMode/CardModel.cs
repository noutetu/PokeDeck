using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ----------------------------------------------------------------------
// カードのデータを保持するクラス（Model）
// カードのID・名前・HP・画像URLなどを保持し、
// Presenterからの要求に応じてデータを提供する
// ----------------------------------------------------------------------
public class CardModel
{
    public string id;
    public string name;
    public string cardType;
    public string evolutionStage;
    public string pack;
    public int hp;
    public string type;
    public string weakness;
    public int retreatCost;
    public string abilityName;
    public string abilityEffect;
    public List<MoveData> moves;
    public List<string> tags;
    public int maxDamage;
    public string imageKey;
    public Texture2D imageTexture;
    public CardType cardTypeOnEnum;
    
    public void SetCardType(string type)
    {
        switch (type)
        {
            case "非EX":
                cardTypeOnEnum = CardType.非EX;
                break;
            case "EX":
                cardTypeOnEnum = CardType.EX;
                break;
            case "サポート":
                cardTypeOnEnum = CardType.サポート;
                break;
            case "グッズ":
                cardTypeOnEnum = CardType.グッズ;
                break;
            case "ポケモンの道具":
                cardTypeOnEnum = CardType.ポケモンの道具;
                break;
            case "グッズ(化石)":
                cardTypeOnEnum = CardType.化石;
                break;
            default:
                Debug.LogError("❌ カードタイプが不明: " + type);
                break;
        }
    }
}

public class MoveData
{
    public string name;
    public int damage;
    public string effect;
    public Dictionary<string, int> cost;
}
public enum CardType
{
    非EX,
    EX,
    サポート,
    グッズ,
    ポケモンの道具,
    化石,
}
public enum Type
{
    草,
    炎,
    水,
    雷,
    闘,
    超,
    悪,
    鋼,
    ドラゴン,
    無色,
}
