using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using System;

// ----------------------------------------------------------------------
// デッキ画面のUIを管理するクラス
// ----------------------------------------------------------------------
public class DeckView : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // UI参照
    // ----------------------------------------------------------------------
    [Header("UI参照")]
    [SerializeField] private TMP_InputField deckNameInput; // デッキ名入力フィールド
    [SerializeField] private Transform cardContainer; // カードアイテムを配置するコンテナ
    [SerializeField] private GameObject cardItemPrefab; // カードアイテムのプレハブ
    [SerializeField] private Transform energyContainer; // エネルギーアイテムを配置するコンテナ
    [SerializeField] private GameObject energyItemPrefab; // エネルギーアイテムのプレハブ
    [SerializeField] private Button saveButton; // 保存ボタン
    [SerializeField] private Button newDeckButton; // 新規デッキ作成ボタン
    [SerializeField] private Button openDeckListButton; // デッキ一覧を開くボタン
    [SerializeField] private GameObject deckListPanel; // デッキ一覧パネル

    // ----------------------------------------------------------------------
    // プライベート変数
    // ----------------------------------------------------------------------
    private List<GameObject> cardItems = new List<GameObject>(); // カードビューアイテムのリスト
    private List<GameObject> energyItems = new List<GameObject>(); // エネルギービューアイテムのリスト
    private DeckModel currentDeck; // 現在表示中のデッキ
    private bool eventsInitialized = false; // イベント初期化フラグ

    // ----------------------------------------------------------------------
    // Unityライフサイクルメソッド
    // ----------------------------------------------------------------------
    private void OnEnable()
    {
        // 現在のデッキを取得
        currentDeck = DeckManager.Instance.CurrentDeck;

        // UI要素を初期化
        InitializeUI();

        // デッキをUIに表示
        DisplayDeck(currentDeck);

        // UIイベントを初期化（初回のみ）
        if (!eventsInitialized)
        {
            SetupUIEvents();
            eventsInitialized = true;
        }
    }

    // ----------------------------------------------------------------------
    // UI要素の初期設定
    // ----------------------------------------------------------------------
    private void InitializeUI()
    {
        // デッキ名を入力フィールドに設定
        if (deckNameInput != null)
            deckNameInput.text = currentDeck.Name;
    }

    // ----------------------------------------------------------------------
    // UIイベントの設定
    // ----------------------------------------------------------------------
    private void SetupUIEvents()
    {
        // デッキ名変更時のイベントを設定
        if (deckNameInput != null)
            deckNameInput.onEndEdit.AddListener(OnDeckNameChanged);

        // 保存ボタンのクリックイベントを設定
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveButtonClicked);

        // 新規デッキ作成ボタンのクリックイベントを設定
        if (newDeckButton != null)
            newDeckButton.onClick.AddListener(OnNewDeckButtonClicked);

        // デッキ一覧を開くボタンのクリックイベントを設定
        if (openDeckListButton != null)
        {
            openDeckListButton.onClick.AddListener(() => {
                if (deckListPanel != null)
                {
                    // 現在のパネルを非表示にし、デッキ一覧パネルを表示
                    gameObject.SetActive(false);
                    deckListPanel.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("デッキリストパネルが設定されていません");
                }
            });
        }
    }

    // ----------------------------------------------------------------------
    // デッキをUIに表示
    // ----------------------------------------------------------------------
    public void DisplayDeck(DeckModel deck)
    {
        // 現在のデッキを設定
        currentDeck = deck;

        // デッキ名を更新
        if (deckNameInput != null)
            deckNameInput.text = deck.Name;

        // カードをタイプ順に並べ替え
        deck.SortCardsByTypeAndID();

        // 既存のカードアイテムをクリア
        ClearCardItems();

        // 新しいカードアイテムを生成
        foreach (var cardId in deck.CardIds)
        {
            CreateCardItem(cardId);
        }

        // 既存のエネルギーアイテムをクリア
        ClearEnergyItems();

        // 新しいエネルギーアイテムを生成
        foreach (var energyReq in deck.EnergyRequirements)
        {
            CreateEnergyItem(energyReq);
        }
    }

    // ----------------------------------------------------------------------
    // カードアイテムを作成
    // ----------------------------------------------------------------------
    private void CreateCardItem(string cardId)
    {
        // 必要なプレハブやコンテナが設定されていない場合は処理を中断
        if (cardItemPrefab == null || cardContainer == null)
            return;

        // カードアイテムを生成し、リストに追加
        GameObject cardItem = Instantiate(cardItemPrefab, cardContainer);
        cardItems.Add(cardItem);

        // カードモデルを取得
        CardModel cardModel = currentDeck.GetCardModel(cardId);

        if (cardModel != null)
        {
            // カード画像を設定
            RawImage cardImage = cardItem.GetComponentInChildren<RawImage>();
            if (cardImage != null && cardModel.imageTexture != null)
            {
                cardImage.texture = cardModel.imageTexture;
            }

            // カード名を設定
            TextMeshProUGUI nameText = cardItem.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = cardModel.name;

                // 同名カードが複数ある場合は枚数を表示
                int sameNameCount = currentDeck.GetSameNameCardCount(cardModel.name);
                if (sameNameCount > 1)
                {
                    nameText.text += $" (x{sameNameCount})";
                }
            }
        }
        else
        {
            // カードモデルが見つからない場合はIDを表示
            TextMeshProUGUI textComponent = cardItem.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = $"不明なカード: {cardId}";
            }
        }

        // カード削除ボタンのイベントを設定
        var removeButton = cardItem.GetComponentInChildren<Button>();
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(() => {
                // カード名を取得
                string cardName = "カード";
                if (cardModel != null && !string.IsNullOrEmpty(cardModel.name))
                {
                    cardName = cardModel.name;
                }

                // カードをデッキから削除
                bool success = currentDeck.RemoveCard(cardId);

                if (success)
                {
                    // 成功メッセージを表示
                    if (FeedbackContainer.Instance != null)
                    {
                        FeedbackContainer.Instance.ShowSuccessFeedback($"デッキから削除： 「{cardName}」");
                    }

                    // デッキを再表示
                    DisplayDeck(currentDeck);
                }
            });
        }
    }

    // ----------------------------------------------------------------------
    // エネルギーアイテムを作成
    // ----------------------------------------------------------------------
    private void CreateEnergyItem(EnergyRequirement energyReq)
    {
        // 必要なプレハブやコンテナが設定されていない場合は処理を中断
        if (energyItemPrefab == null || energyContainer == null)
            return;

        // エネルギーアイテムを生成し、リストに追加
        GameObject energyItem = Instantiate(energyItemPrefab, energyContainer);
        energyItems.Add(energyItem);

        // エネルギー情報を表示
        var textComponent = energyItem.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = $"{energyReq.Type}: {energyReq.Count}";
        }
    }

    // ----------------------------------------------------------------------
    // カードアイテムをすべて削除
    // ----------------------------------------------------------------------
    private void ClearCardItems()
    {
        // 既存のカードアイテムを破棄
        foreach (var item in cardItems)
        {
            Destroy(item);
        }
        cardItems.Clear();
    }

    // ----------------------------------------------------------------------
    // エネルギーアイテムをすべて削除
    // ----------------------------------------------------------------------
    private void ClearEnergyItems()
    {
        // 既存のエネルギーアイテムを破棄
        foreach (var item in energyItems)
        {
            Destroy(item);
        }
        energyItems.Clear();
    }

    // ----------------------------------------------------------------------
    // UIイベントハンドラー
    // ----------------------------------------------------------------------
    private void OnDeckNameChanged(string newName)
    {
        // 新しいデッキ名が空でない場合に更新
        if (!string.IsNullOrEmpty(newName))
        {
            currentDeck.Name = newName;
        }
        else
        {
            // 空の場合は元の名前に戻す
            deckNameInput.text = currentDeck.Name;
        }
    }

    private void OnSaveButtonClicked()
    {
        // 現在のデッキを保存
        DeckManager.Instance.SaveCurrentDeck();
    }

    private void OnNewDeckButtonClicked()
    {
        // 新しいデッキを作成し、UIを更新
        currentDeck = DeckManager.Instance.CreateNewDeck();
        DisplayDeck(currentDeck);
    }

    private void OnBackButtonClicked()
    {
        // 現在の画面を非表示
        gameObject.SetActive(false);
    }

    // ----------------------------------------------------------------------
    // カードをデッキに追加する公開メソッド
    // ----------------------------------------------------------------------
    public bool AddCardToDeck(string cardId)
    {
        // カードをデッキに追加
        bool success = currentDeck.AddCard(cardId);
        if (success)
        {
            // デッキを再表示
            DisplayDeck(currentDeck);
        }
        return success;
    }
}
