using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardTestLogger : MonoBehaviour
{
    [SerializeField] private string testCardId;

    private void Start()
    {
        var card = CardManager.Instance.GetCardById(testCardId);
        if (card != null)
        {
            Debug.Log($"カード名: {card.name}");
            Debug.Log($"HP: {card.hp}");
            Debug.Log($"タイプ: {card.type}");
            Debug.Log($"弱点: {card.weakness}");
            Debug.Log($"リトリートコスト: {card.retreatCost}");
            Debug.Log($"カードタイプ: {card.cardType}");
            Debug.Log($"画像キー: {card.imageKey}");
            Debug.Log($"タグ: {string.Join(", ", card.tags)}");
        }
    }
}

