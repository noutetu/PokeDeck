using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class RemoteJsonLoader : MonoBehaviour
{
    // public string jsonUrl = "https://noutetu.github.io/PokeDeckCards/output.json";

    // void Awake()
    // {
    //     StartCoroutine(LoadJsonFromWeb());
    // }

    // IEnumerator LoadJsonFromWeb()
    // {
    //     UnityWebRequest request = UnityWebRequest.Get(jsonUrl);
    //     yield return request.SendWebRequest();

    //     if (request.result == UnityWebRequest.Result.Success)
    //     {
    //         string json = request.downloadHandler.text;
    //         Debug.Log("✅ JSON取得成功！");
    //         Debug.Log(json);

    //         // 🔽 CardManagerに渡してデシリアライズ
    //         if (CardManager.Instance != null)
    //         {
    //             CardManager.Instance.LoadCardDataFromString(json);
    //         }
    //     }
    //     else
    //     {
    //         Debug.LogError("❌ JSON取得失敗: " + request.error);
    //     }
    // }
}
