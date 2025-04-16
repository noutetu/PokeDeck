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

    void Start()
    {
        Debug.Log("カードビューの初期化");
    }

    public void Setup(CardModel data)
    {
        Debug.Log("カードデータをセットアップ: " + data.name);
        if (cardImage == null)
        {
            Debug.LogError("❌ cardImageがnullだよ！");
            return;
        }
        if (data.imageTexture == null)
        {
            Debug.LogError("❌ imageTextureがnullだよ！");
            return;
        }
        if (data.imageTexture.width == 0 || data.imageTexture.height == 0)
        {
            Debug.LogError("❌ imageTextureのサイズが0だよ！");
            return;
        }
        if (data.imageTexture.width > 0 && data.imageTexture.height > 0)
        {
            Debug.Log("カード画像のサイズ: " + data.imageTexture.width + "x" + data.imageTexture.height);
        }
        else
        {
            Debug.LogError("❌ imageTextureのサイズが不正だよ！");
        }
        cardImage.texture = data.imageTexture;
    }
}

