using System;
using System.Collections.Generic;

[Serializable]
public class CardDataList
{
    public List<CardData> cards;
}

[Serializable]
public class CardData
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
}

[Serializable]
public class MoveData
{
    public string name;
    public int damage;
    public string effect;
    public Dictionary<string, int> cost;
}
