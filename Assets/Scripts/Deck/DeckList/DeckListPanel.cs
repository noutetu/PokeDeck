using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// ----------------------------------------------------------------------
// デッキ一覧パネルを管理するクラス
// ----------------------------------------------------------------------
public class DeckListPanel : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // フィールド変数
    // ----------------------------------------------------------------------
    [SerializeField] private Transform contentContainer;    // デッキ一覧のコンテナ
    [SerializeField] private GameObject deckDetailPrefab;   // デッキ詳細アイテムのプレハブ
    [SerializeField] private GameObject deckPanel;       // デッキパネル
    [SerializeField] private DeckView deckView;       // デッキビュー
    [SerializeField] private Button closeButton;       // 閉じるボタン
    [SerializeField] private Button toSampleDeckListButton; // サンプルデッキ一覧へ移動ボタン
    [SerializeField] private SampleDeckPanel sampleDeckPanel; // サンプルデッキパネル

    [Header("NoDeckMessage")]
    [SerializeField] private GameObject noDeckMessage; // デッキがない場合のメッセージ

    private List<GameObject> deckItems = new List<GameObject>();    // デッキアイテムのリスト

    // ----------------------------------------------------------------------
    // Unityの初期化メソッド
    // ----------------------------------------------------------------------
    private void OnEnable()
    {
        // パネルが表示されるたびにデッキリストを更新
        RefreshDeckList();

        // DeckViewを非表示にする
        if (deckPanel != null)
        {
            deckPanel.SetActive(false);
        }
    }

    // TODO: ボタンの購読解除が不明
    // ----------------------------------------------------------------------
    // Unityの初期化メソッド(初回のみ)
    // ----------------------------------------------------------------------
    private void Start()
    {
        // 閉じるボタンのイベント設定
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);

                // デッキパネルを表示
                if (deckPanel != null)
                {
                    deckPanel.SetActive(true);
                }
            });
        }
        // サンプルデッキ一覧へ移動ボタンのイベント設定
        if (toSampleDeckListButton != null)
        {
            toSampleDeckListButton.onClick.AddListener(GoToSampleDeckList);
        }
    }

    // ----------------------------------------------------------------------
    // デッキ一覧を最新の状態に更新
    // ----------------------------------------------------------------------
    public void RefreshDeckList()
    {
        // 既存のデッキアイテムをクリア
        ClearDeckItems();

        // デッキが一つもない場合はメッセージを表示
        if (DeckManager.Instance == null || DeckManager.Instance.SavedDecks.Count == 0)
        {
            if (noDeckMessage != null)
            {
                noDeckMessage.SetActive(true);
            }
            return;
        }
        else
        {
            if (noDeckMessage != null)
            {
                noDeckMessage.SetActive(false);
            }
        }
        // 保存されているデッキをすべて取得
        if (DeckManager.Instance != null)
        {
            foreach (var deck in DeckManager.Instance.SavedDecks)
            {
                CreateDeckItem(deck);
            }
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
            return;

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
        }
    }

    // ----------------------------------------------------------------------
    // デッキを選択
    // ----------------------------------------------------------------------
    private async void SelectDeck(string deckName)
    {
        // DeckManagerで指定デッキを選択
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.SelectDeck(deckName);

            // デッキパネルに表示を反映（非同期でキャッシュ管理を適切に行う）
            if (deckView != null)
            {
                await deckView.DisplayDeck(DeckManager.Instance.CurrentDeck);
            }

            // デッキ一覧パネルを閉じる
            gameObject.SetActive(false);

            // デッキパネルを表示
            if (deckPanel != null)
            {
                deckPanel.SetActive(true);
            }
        }
    }

    // ----------------------------------------------------------------------
    // サンプルデッキ一覧へ移動
    // ----------------------------------------------------------------------   
    public void GoToSampleDeckList()
    {
        if (sampleDeckPanel != null)
        {
            sampleDeckPanel.gameObject.SetActive(true);
        }

        // デッキ一覧パネルを閉じる
        gameObject.SetActive(false);
    }
}