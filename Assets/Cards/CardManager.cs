using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.UI; // Added to include RawImage
using UnityEngine;
using UnityEngine.Networking;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }
    [SerializeField] RawImage rawimage; 

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
            Debug.Log($"🟢 カード {card.name} を取得しました");
            // 画像を表示する処理
            StartCoroutine(LoadTextureFromWeb(card.imageKey));

            return card;
        }
        Debug.LogWarning($"Card ID {id} not found.");
        return null;
    }

    private IEnumerator LoadTextureFromWeb(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            rawimage.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            Debug.Log("✅ JSON取得成功！");
        }
        else
        {
            Debug.LogError("❌ JSON取得失敗: " + request.error);
        }
    }
}
