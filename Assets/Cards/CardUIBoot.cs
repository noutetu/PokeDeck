using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

public class CardUIBoot : MonoBehaviour
{
    [SerializeField] private AllCardView allCardView;   // View
    private AllCardPresenter presenter;                 // Presenter
    private AllCardModel model;                         // Model

    private const string jsonUrl = "https://noutetu.github.io/PokeDeckCards/output.json";

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

            var loadedModel = JsonConvert.DeserializeObject<AllCardModel>(jsonText);
            SetCardData(loadedModel.cards);

            Debug.Log("📷 画像の読み込み開始");

            // ✅ 10枚ずつ画像読み込み＆表示
            await SetImagesInBatches(loadedModel.cards, 10);

            Debug.Log("✅ すべてのカード表示完了");
        }
        else
        {
            Debug.LogError("❌ JSON読み込み失敗: " + request.error);
        }
    }
    private async UniTask SetImagesInBatches(List<CardModel> cards, int batchSize)
    {
        int total = cards.Count;

        for (int i = 0; i < total; i += batchSize)
        {
            // 🔸 今のバッチ分だけ取り出す
            var batch = cards.GetRange(i, Mathf.Min(batchSize, total - i));

            // 🔸 画像読み込み
            var tasks = new List<UniTask>();
            foreach (var card in batch)
            {
                tasks.Add(DownloadAndAssignImage(card));
            }
            await UniTask.WhenAll(tasks);

            // 🔸 表示に追加
            presenter.AddCards(batch); // ← AddCardsをPresenterに追加してね！

            Debug.Log($"🟢 表示バッチ完了: {i + batch.Count}/{total}");
        }
    }



    private void SetCardData(List<CardModel> cards)
    {
        foreach (var card in cards)
        {
            card.SetCardType(card.cardType);
        }
    }

    // ✅ WhenAllによる並列画像ロード
    private async UniTask SetImages(List<CardModel> cards)
    {
        var tasks = new List<UniTask>();
        int total = cards.Count;
        int completed = 0;

        foreach (var card in cards)
        {
            tasks.Add(UniTask.Create(async () =>
            {
                await DownloadAndAssignImage(card);
                completed++;
                Debug.Log($"📦 画像ロード進捗: {completed}/{total} ({(completed * 100 / total)}%)");
            }));
        }

        await UniTask.WhenAll(tasks);
    }


    // 1枚の画像を読み込む処理
    private async UniTask DownloadAndAssignImage(CardModel card)
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