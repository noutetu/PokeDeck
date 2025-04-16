using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;


// ----------------------------------------------------------------------
// カード1枚分のUI表示（画像のみ）
// ----------------------------------------------------------------------
public class CardView : MonoBehaviour
{
    [SerializeField] private RawImage cardImage;

    public void Setup(CardModel data)
    {
        cardImage.texture = data.imageTexture;
    }
}

