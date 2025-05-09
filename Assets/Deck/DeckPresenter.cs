// filepath: /Users/runaki/Desktop/project in Unity/PokeDeck/Assets/Deck/DeckPresenter.cs
using System;
using UnityEngine;
using UniRx;

// ----------------------------------------------------------------------
// デッキ画面のPresenterクラス
// DeckModelとDeckViewの仲介役として、ビジネスロジックを処理する
// ----------------------------------------------------------------------
public class DeckPresenter : MonoBehaviour
{
    [SerializeField] private DeckView view;
    
    // モデル参照
    private DeckModel model;
    
    // 購読解除用のCompositeDisposable
    private CompositeDisposable disposables = new CompositeDisposable();

    private void Awake()
    {
        // Viewコンポーネントの取得（SerializeFieldで指定されていない場合）
        if (view == null)
        {
            view = GetComponent<DeckView>();
            if (view == null)
            {
                Debug.LogError("DeckViewが見つかりません。同じGameObjectにアタッチするか、インスペクタで設定してください。");
                return;
            }
        }
    }

    private void OnEnable()
    {
        // デッキマネージャーから現在のデッキを取得
        model = DeckManager.Instance.CurrentDeck;
        
        // モデルとビューを初期化
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

    /// <summary>
    /// モデルとビューの初期化処理
    /// </summary>
    private void InitializeModelAndView()
    {
        // モデルがnullの場合は処理しない
        if (model == null)
        {
            Debug.LogWarning("DeckModelが設定されていません。");
            return;
        }
        
        // ビューの初期化（モデルの内容を表示）
        view.DisplayDeck(model);
        
        // モデルの変更イベントをサブスクライブ
        SetupModelSubscriptions();
    }

    /// <summary>
    /// モデルの変更イベントをサブスクライブする
    /// </summary>
    private void SetupModelSubscriptions()
    {
        // モデルの変更を監視するReactivePropertyがあれば、それをサブスクライブしてUIを更新
        // 例: モデルのカード変更通知などをサブスクライブ
        
        // モデルのカード追加/削除イベントのサブスクライブ（ReactivePropertyがある場合）
        // 実装例: model.CardsObservable.Subscribe(_ => view.DisplayDeck(model)).AddTo(disposables);
        
        // デッキ名変更イベントのサブスクライブ（ReactivePropertyがある場合）
        // 実装例: model.NameObservable.Subscribe(_ => view.UpdateDeckName(model.Name)).AddTo(disposables);
    }

    /// <summary>
    /// デッキモデルを設定し、ビューを更新する
    /// </summary>
    /// <param name="newModel">新しいデッキモデル</param>
    public void SetModel(DeckModel newModel)
    {
        // 以前の購読を解除
        disposables.Clear();
        
        // 新しいモデルを設定
        model = newModel;
        
        // モデルとビューを初期化
        InitializeModelAndView();
    }
    
    /// <summary>
    /// カードをデッキに追加する
    /// </summary>
    /// <param name="cardId">追加するカードID</param>
    /// <returns>追加に成功したかどうか</returns>
    public bool AddCardToDeck(string cardId)
    {
        if (model == null)
            return false;
            
        bool success = model.AddCard(cardId);
        if (success)
        {
            // ビューを更新
            view.DisplayDeck(model);
            
            // 成功メッセージをフィードバック
            CardModel cardModel = model.GetCardModel(cardId);
            string cardName = cardModel != null ? cardModel.name : "カード";
            
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowSuccessFeedback($"デッキに追加： 「{cardName}」");
            }
        }
        else
        {
            // エラーメッセージをフィードバック
            if (FeedbackContainer.Instance != null)
            {
                // エラーの理由を特定
                string reason = "不明なエラー";
                
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
    
    /// <summary>
    /// カードをデッキから削除する
    /// </summary>
    /// <param name="cardId">削除するカードID</param>
    /// <returns>削除に成功したかどうか</returns>
    public bool RemoveCardFromDeck(string cardId)
    {
        if (model == null)
            return false;
            
        // カード名を取得（フィードバック用）
        CardModel cardModel = model.GetCardModel(cardId);
        string cardName = cardModel != null ? cardModel.name : "カード";
            
        bool success = model.RemoveCard(cardId);
        if (success)
        {
            // ビューを更新
            view.DisplayDeck(model);
            
            // 成功メッセージをフィードバック
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowSuccessFeedback($"デッキから削除： 「{cardName}」");
            }
        }
        
        return success;
    }
    
    /// <summary>
    /// デッキを保存する
    /// </summary>
    public void SaveDeck()
    {
        DeckManager.Instance.SaveCurrentDeck();
    }
    
    /// <summary>
    /// 新しいデッキを作成する
    /// </summary>
    public void CreateNewDeck()
    {
        model = DeckManager.Instance.CreateNewDeck();
        view.DisplayDeck(model);
    }
    
    /// <summary>
    /// デッキ名を変更する
    /// </summary>
    /// <param name="newName">新しいデッキ名</param>
    public void ChangeDeckName(string newName)
    {
        if (model != null && !string.IsNullOrEmpty(newName))
        {
            model.Name = newName;
        }
    }
}