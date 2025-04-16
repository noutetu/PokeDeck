using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
}

public class MoveData
{
    public string name;
    public int damage;
    public string effect;
    public Dictionary<string, int> cost;
}
