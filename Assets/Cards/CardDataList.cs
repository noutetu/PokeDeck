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
    public string imageKey;
    public List<string> tags;
    public int hp;
    public string type;
    public string weakness;
    public int retreatCost;
    public List<MoveData> moves;
}

[Serializable]
public class MoveData
{
    public string name;
    public int damage;
    public string effect;
    public Dictionary<string, int> cost;
}
