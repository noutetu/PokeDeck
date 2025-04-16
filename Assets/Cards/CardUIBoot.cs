using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json; 
using System.Collections.Generic;

// ----------------------------------------------------------------------
public class CardUIBoot : MonoBehaviour
{
    [SerializeField] private AllCardView allCardView;   // View
    private AllCardPresenter presenter;                 // Presenter
    private AllCardModel model;                         // Model
    // ============================
    private const string jsonUrl = "https://noutetu.github.io/PokeDeckCards/output.json";
    // ============================
    private async void Start()
    {
        model = new AllCardModel();
        presenter = new AllCardPresenter(model);
        allCardView.BindPresenter(presenter);

        await LoadJsonAndInitializeAsync();
    }

    private async UniTask LoadJsonAndInitializeAsync()
    {
        Debug.Log("🟢 JSON取得開始");

        using var request = UnityWebRequest.Get(jsonUrl);
        await request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("🟢 JSON取得成功");
            var jsonText = request.downloadHandler.text;

            // ✅ JsonUtility → Newtonsoft.Json に変更！
            var loadedModel = JsonConvert.DeserializeObject<AllCardModel>(jsonText);
            Debug.Log("画像の読み込み開始");
            // 画像の読み込みを非同期で行う
            await LoadImages(loadedModel.cards);
            Debug.Log("画像の読み込み完了");

            Debug.Log($"📦 読み込んだカード数: {loadedModel.cards.Count}");

            presenter.LoadCards(loadedModel.cards);
        }
        else
        {
            Debug.LogError("❌ JSON読み込み失敗: " + request.error);
        }
    }
    private async UniTask LoadImages(List<CardModel> cards)
    {
        foreach (var card in cards)
        {
            var request = UnityWebRequestTexture.GetTexture(card.imageKey);
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                card.imageTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            }
            else
            {
                Debug.LogError($"❌ 画像読み込み失敗: {card.imageKey}");
            }
        }
    }

}
