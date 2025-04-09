using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardTestLogger : MonoBehaviour
{
    [SerializeField] private string testCardId;

    private void Start()
    {
        var card = CardManager.Instance.GetCardById(testCardId);
        // if (card != null)
        // {
        //     Debug.Log($"Card ID: {card.id}");
        //     Debug.Log($"Name: {card.name}");
        //     Debug.Log($"Card Type: {card.cardType}");
        //     Debug.Log($"Evolution Stage: {card.evolutionStage}");
        //     Debug.Log($"Pack: {card.pack}");
        //     Debug.Log($"HP: {card.hp}");
        //     Debug.Log($"Type: {card.type}");
        //     Debug.Log($"Weakness: {card.weakness}");
        //     Debug.Log($"Retreat Cost: {card.retreatCost}");
        //     Debug.Log($"Ability Name: {card.abilityName}");
        //     Debug.Log($"Ability Effect: {card.abilityEffect}");
        //     Debug.Log($"Max Damage: {card.maxDamage}");
        //     Debug.Log($"Image Key: {card.imageKey}");

        //     Debug.Log("Tags:");
        //     foreach (var tag in card.tags)
        //     {
        //         Debug.Log($"- {tag}");
        //     }

        //     Debug.Log("Moves:");
        //     foreach (var move in card.moves)
        //     {
        //         Debug.Log($"Move Name: {move.name}, Damage: {move.damage}, Effect: {move.effect}");
        //         foreach (var cost in move.cost)
        //         {
        //             Debug.Log($"Cost Type: {cost.Key}, Amount: {cost.Value}");
        //         }
        //     }
        // }
        // else
        // {
        //     Debug.LogError("Card not found!");
        // }
    }
}

