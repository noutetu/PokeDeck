using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    private Dictionary<string, CardData> cardDict;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadCardDataFromString(string json)
    {
        CardDataList cardList = JsonUtility.FromJson<CardDataList>(json);
        cardDict = cardList.cards.ToDictionary(card => card.id, card => card);
        Debug.Log($"🟢 {cardDict.Count}枚のカードを読み込みました");
    }


    public CardData GetCardById(string id)
    {
        if (cardDict != null && cardDict.TryGetValue(id, out var card))
        {
            return card;
        }
        Debug.LogWarning($"Card ID {id} not found.");
        return null;
    }
}
