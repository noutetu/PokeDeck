using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

// ----------------------------------------------------------------------
// カード1枚分のUI表示（画像のみ）
// ----------------------------------------------------------------------
public class CardView : MonoBehaviour
{
    CardModel data;

    [SerializeField] private RawImage cardImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text retreatCost;
    [Header("詳細まとめオブジェクト")]
    [SerializeField] private GameObject details;
    [Header("Ability")]
    [SerializeField] private GameObject abilityGroup;
    [SerializeField] private TMP_Text abilityNameText;
    [SerializeField] private TMP_Text abilityEffectText;
    [Header("Move1")]
    [SerializeField] private GameObject move1Group;
    [SerializeField] private TMP_Text move1NameText;
    [SerializeField] private TMP_Text move1DamageText;
    [SerializeField] private TMP_Text move1EffectText;
    [Header("Move2")]
    [SerializeField] private GameObject move2Group;
    [SerializeField] private TMP_Text move2NameText;
    [SerializeField] private TMP_Text move2DamageText;
    [SerializeField] private TMP_Text move2EffectText;


    public void Setup(CardModel data)
    {
        this.data = data;
        if (data.cardTypeOnEnum == CardType.EX || data.cardTypeOnEnum == CardType.非EX)
        {
            ViewPokemon();
        }
        else
        {
            ViewOtherCard();
        }

        // detailsの子オブジェクトのレイアウトを再構築する
        LayoutRebuilder.ForceRebuildLayoutImmediate(details.GetComponent<RectTransform>());
    }

    private void ViewPokemon()
    {
        cardImage.texture = data.imageTexture;
        nameText.text = data.name;
        hpText.text = "HP: " + data.hp.ToString();
        retreatCost.text = "逃げ: " + data.retreatCost.ToString();
        abilityNameText.text = data.abilityName;
        abilityEffectText.text = data.abilityEffect;
        
            move1NameText.text = data.moves[0].name;
            move1DamageText.text = data.moves[0].damage.ToString();
            move1EffectText.text = data.moves[0].effect;
        
        if (data.moves.Count > 1)
        {
            move2NameText.text = data.moves[1].name;
            move2DamageText.text = data.moves[1].damage.ToString();
            // move2EffectText.text = data.moves[1].effect;
        }
        else
        {
            move2NameText.text = "";
            move2DamageText.text = "";
            move2EffectText.text = "";
        }
    }
    // ポケモン以外のカードの表示
    public void ViewOtherCard()
    {
        cardImage.texture = data.imageTexture;
        nameText.text = data.name;
        move1EffectText.text = data.moves[0].effect;

        // 画像と名前と効果以外は非表示
        hpText.gameObject.SetActive(false);
        retreatCost.gameObject.SetActive(false);
        abilityNameText.gameObject.SetActive(false);
        abilityEffectText.gameObject.SetActive(false);
        move1NameText.gameObject.SetActive(false);
        move1DamageText.gameObject.SetActive(false);
        move2NameText.gameObject.SetActive(false);
        move2DamageText.gameObject.SetActive(false);
        move2EffectText.gameObject.SetActive(false);
    }
}
