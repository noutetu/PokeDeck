using UnityEngine;
using UniRx;

// ----------------------------------------------------------------------
// デッキ画面のPresenterクラス
// DeckModelとDeckViewの仲介役として、ビジネスロジックを処理する
// ----------------------------------------------------------------------
public class DeckPresenter : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // DeckViewの参照
    // ----------------------------------------------------------------------
    [SerializeField] private DeckView view;

    // ----------------------------------------------------------------------
    // モデル参照
    // ----------------------------------------------------------------------
    private DeckModel model;

    // ----------------------------------------------------------------------
    // 購読解除用のCompositeDisposable
    // ----------------------------------------------------------------------
    private CompositeDisposable disposables = new CompositeDisposable();

    // ----------------------------------------------------------------------
    // Unityライフサイクルメソッド
    // ----------------------------------------------------------------------
    private void Awake()
    {
        // DeckViewコンポーネントを取得
        if (view == null)
        {
            view = GetComponent<DeckView>();
            if (view == null)
            {
                return;
            }
        }
    }

    private void OnEnable()
    {
        // DeckManagerから現在のデッキを取得し、モデルとビューを初期化
        model = DeckManager.Instance.CurrentDeck;
        InitializeModelAndView();
    }

    private void OnDisable()
    {
        // 購読解除
        disposables.Clear();
    }

    private void OnDestroy()
    {
        // 購読解除
        disposables.Dispose();
    }

    // ----------------------------------------------------------------------
    // モデルとビューの初期化処理
    // ----------------------------------------------------------------------
    private void InitializeModelAndView()
    {
        // モデルが設定されていない場合は警告を表示
        if (model == null)
        {
            return;
        }

        // ビューにモデルの内容を表示
        view.DisplayDeck(model);
    }

    // ----------------------------------------------------------------------
    // デッキモデルを設定し、ビューを更新する
    // ----------------------------------------------------------------------
    public void SetModel(DeckModel newModel)
    {
        // 以前の購読を解除
        disposables.Clear();

        // 新しいモデルを設定
        model = newModel;

        // モデルとビューを初期化
        InitializeModelAndView();
    }

    // ----------------------------------------------------------------------
    // カードをデッキに追加する
    // ----------------------------------------------------------------------
    public bool AddCardToDeck(string cardId)
    {
        // モデルが設定されていない場合は処理を中断
        if (model == null)
            return false;

        // モデルにカードを追加
        bool success = model.AddCard(cardId);
        if (success)
        {
            // ビューを更新
            view.DisplayDeck(model);

            // 追加したカードの情報を取得
            CardModel cardModel = model.GetCardModel(cardId);
            string cardName = cardModel != null ? cardModel.name : "カード";

            // 成功メッセージを表示
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowSuccessFeedback($"デッキに追加： 「{cardName}」");
            }
        }
        else
        {
            // エラーメッセージを表示
            if (FeedbackContainer.Instance != null)
            {
                string reason = "不明なエラー";

                // エラーの理由を特定
                if (model.CardCount >= DeckModel.MAX_CARDS)
                {
                    reason = $"デッキは最大{DeckModel.MAX_CARDS}枚までです";
                }
                else
                {
                    CardModel cardModel = model.GetCardModel(cardId);
                    if (cardModel != null)
                    {
                        int sameNameCount = model.GetSameNameCardCount(cardModel.name);
                        if (sameNameCount >= DeckModel.MAX_SAME_NAME_CARDS)
                        {
                            reason = $"同名カードは{DeckModel.MAX_SAME_NAME_CARDS}枚までです";
                        }
                    }
                }

                FeedbackContainer.Instance.ShowFailureFeedback($"デッキに追加できません: {reason}");
            }
        }

        return success;
    }

    // ----------------------------------------------------------------------
    // カードをデッキから削除する
    // ----------------------------------------------------------------------
    public bool RemoveCardFromDeck(string cardId)
    {
        // モデルが設定されていない場合は処理を中断
        if (model == null)
            return false;

        // 削除するカードの情報を取得
        CardModel cardModel = model.GetCardModel(cardId);
        string cardName = cardModel != null ? cardModel.name : "カード";

        // モデルからカードを削除
        bool success = model.RemoveCard(cardId);
        if (success)
        {
            // ビューを更新
            view.DisplayDeck(model);

            // 成功メッセージを表示
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowSuccessFeedback($"デッキから削除： 「{cardName}」");
            }
        }

        return success;
    }

    // ----------------------------------------------------------------------
    // デッキを保存する
    // ----------------------------------------------------------------------
    public void SaveDeck()
    {
        // DeckManagerを通じて現在のデッキを保存
        DeckManager.Instance.SaveCurrentDeck();
    }

    // ----------------------------------------------------------------------
    // 新しいデッキを作成する
    // ----------------------------------------------------------------------
    public void CreateNewDeck()
    {
        // DeckManagerを通じて新しいデッキを作成
        model = DeckManager.Instance.CreateNewDeck();

        // ビューを更新
        view.DisplayDeck(model);
    }

    // ----------------------------------------------------------------------
    // デッキ名を変更する
    // ----------------------------------------------------------------------
    public void ChangeDeckName(string newName)
    {
        // モデルが設定されており、新しい名前が有効な場合に変更を適用
        if (model != null && !string.IsNullOrEmpty(newName))
        {
            model.Name = newName;
        }
    }
}