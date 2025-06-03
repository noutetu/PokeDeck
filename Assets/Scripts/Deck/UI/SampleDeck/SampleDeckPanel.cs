using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// ----------------------------------------------------------------------
// サンプルデッキ一覧パネルを管理するクラス
// DeckManagerからサンプルデッキを取得し、表示する
// ----------------------------------------------------------------------
public class SampleDeckPanel : MonoBehaviour
{
 // ----------------------------------------------------------------------
    // フィールド変数
    // ----------------------------------------------------------------------
    [SerializeField] private Transform contentContainer;    // デッキ一覧のコンテナ
    [SerializeField] private GameObject deckDetailPrefab;   // デッキ詳細アイテムのプレハブ
    [SerializeField] private GameObject deckListPanel;       // デッキパネル
    [SerializeField] private DeckView deckView;       // デッキビュー
    [SerializeField] private Button closeButton;       // 閉じるボタン
    
    private List<GameObject> deckItems = new List<GameObject>();    // デッキアイテムのリスト
    
    // ----------------------------------------------------------------------
    // Unityの初期化メソッド
    // ----------------------------------------------------------------------
    private void OnEnable()
    {
        // パネルが表示されるたびにデッキリストを更新
        RefreshDeckList();
    }
    
    // ----------------------------------------------------------------------
    // Unityの初期化メソッド(初回のみ)
    // ----------------------------------------------------------------------
    private void Start()
    {
        // 閉じるボタンのイベント設定
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => {
                gameObject.SetActive(false);
                
                // デッキパネルを表示
                if (deckListPanel != null)
                {
                    
                    deckListPanel.SetActive(true);
                }
            });
        }
    }
    
    // ----------------------------------------------------------------------
    // デッキ一覧を最新の状態に更新
    // ----------------------------------------------------------------------
    public void RefreshDeckList()
    {
        // 既存のデッキアイテムをクリア
        ClearDeckItems();
        
        // DeckManagerの初期化状態をチェック
        if (DeckManager.Instance == null)
        {
            return;
        }
        
        // サンプルデッキをすべて取得して表示
        var sampleDecks = DeckManager.Instance.SampleDecks;
        
        foreach (var deck in sampleDecks)
        {
            if (deck != null)
            {
                CreateDeckItem(deck);
            }
            else
            {
            }
        }
        
        // サンプルデッキが見つからない場合の警告
        if (sampleDecks.Count == 0)
        {
        }
    }
    
    // ----------------------------------------------------------------------
    // デッキアイテムをすべて削除
    // ----------------------------------------------------------------------
    private void ClearDeckItems()
    {
        foreach (var item in deckItems)
        {
            Destroy(item);
        }
        
        deckItems.Clear();
    }
    
    // ----------------------------------------------------------------------
    // デッキアイテムを生成
    // ----------------------------------------------------------------------
    private void CreateDeckItem(DeckModel deck)
    {
        if (deckDetailPrefab == null || contentContainer == null)
        {
            return;
        }
        
        if (deck == null)
        {
            return;
        }
            
        // デッキアイテムのプレハブを生成
        GameObject deckItem = Instantiate(deckDetailPrefab, contentContainer);
        deckItems.Add(deckItem);
        
        // デッキアイテムコンポーネントを設定
        DeckListItem itemComponent = deckItem.GetComponent<DeckListItem>();
        if (itemComponent != null)
        {
            // デッキ情報を設定
            itemComponent.SetDeckInfo(deck);

            // クリックイベントを設定
            itemComponent.OnDeckSelected.AddListener(() =>
            {
                SelectDeck(deck.Name);
            });
            
            // パネルを渡す(コピーを押したらデッキパネルを開くために)
            itemComponent.SetActiveForSampleDeck(deckListPanel,this.gameObject);
        }
    }

    // ----------------------------------------------------------------------
    // デッキを選択
    // ----------------------------------------------------------------------
    private async void SelectDeck(string deckName)
    {
        // DeckManagerの初期化状態をチェック
        if (DeckManager.Instance == null)
        {
            return;
        }

        // プログレス表示を開始
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.UpdateFeedbackMessage($"サンプルデッキ '{deckName}' を読み込み中...");
        }

        try
        {
            // 指定されたサンプルデッキを選択
            bool success = DeckManager.Instance.SelectDeck(deckName);
            if (!success)
            {
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowFailureFeedback($"サンプルデッキの選択に失敗しました");
                }
                return;
            }


            // デッキパネルに表示を反映（非同期でキャッシュ管理を適切に行う）
            if (deckView != null)
            {
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.UpdateFeedbackMessage("デッキを表示中...");
                }

                await deckView.DisplayDeck(DeckManager.Instance.CurrentDeck);
                
                // 成功フィードバック
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowSuccessFeedback($"サンプルデッキ '{deckName}' を選択しました");
                }
            }

            // サンプルデッキ一覧パネルを閉じる
            gameObject.SetActive(false);

            // 通常のデッキパネルを閉じる
            if (deckListPanel != null)
            {
                deckListPanel.SetActive(false);
            }
            
            // デッキパネルを表示
            if (deckView != null)
            {
                deckView.gameObject.SetActive(true);
            }
        }
        catch (System.Exception ex)
        {
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback($"デッキ表示中にエラーが発生しました");
            }
        }
    }
}