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
    [Header("UI参照")]
    [SerializeField] private TMP_InputField deckNameInput;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardItemPrefab;
    [SerializeField] private Transform energyContainer;
    [SerializeField] private GameObject energyItemPrefab;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button newDeckButton;
    [SerializeField] private Button openDeckListButton; // デッキ一覧を開くボタン
    [SerializeField] private GameObject deckListPanel; // デッキ一覧パネル
    
    // カードビューアイテムのリスト
    private List<GameObject> cardItems = new List<GameObject>();
    // エネルギービューアイテムのリスト
    private List<GameObject> energyItems = new List<GameObject>();

    // 現在表示中のデッキ
    private DeckModel currentDeck;
    
    // イベント初期化フラグ
    private bool eventsInitialized = false;

    private void OnEnable()
    {
        // デッキマネージャーから現在のデッキを取得
        currentDeck = DeckManager.Instance.CurrentDeck;
        
        // UI要素の初期化
        InitializeUI();
        
        // 現在のデッキをUIに表示
        DisplayDeck(currentDeck);
        
        // UIイベントのセットアップ（初回のみ）
        if (!eventsInitialized)
        {
            SetupUIEvents();
            eventsInitialized = true;
        }
    }

    /// <summary>
    /// UI要素の初期設定
    /// </summary>
    private void InitializeUI()
    {
        // デッキ名入力フィールド
        if (deckNameInput != null)
            deckNameInput.text = currentDeck.Name;
        
    }

    /// <summary>
    /// UIイベントの設定
    /// </summary>
    private void SetupUIEvents()
    {
        // デッキ名の変更時
        if (deckNameInput != null)
            deckNameInput.onEndEdit.AddListener(OnDeckNameChanged);
        
        // 保存ボタンのクリック時
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveButtonClicked);
        
        // 新規デッキボタンのクリック時
        if (newDeckButton != null)
            newDeckButton.onClick.AddListener(OnNewDeckButtonClicked);
            
        // デッキ一覧を開くボタンのイベント設定
        if (openDeckListButton != null)
        {
            openDeckListButton.onClick.AddListener(() => {
                if (deckListPanel != null)
                {
                    // 現在のパネルを非表示
                    gameObject.SetActive(false);
                    
                    // デッキリストパネルを表示
                    deckListPanel.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("デッキリストパネルが設定されていません");
                }
            });
        }
    }

    /// <summary>
    /// デッキをUIに表示
    /// </summary>
    /// <param name="deck">表示するデッキ</param>
    public void DisplayDeck(DeckModel deck)
    {
        currentDeck = deck;
        
        // デッキ名を更新
        if (deckNameInput != null)
            deckNameInput.text = deck.Name;
        
        // カードをカードタイプ順に並べ替え（以前はID順）
        deck.SortCardsByTypeAndID();
        
        // カードアイテムをクリア
        ClearCardItems();
        
        // カードアイテムを生成
        foreach (var cardId in deck.CardIds)
        {
            CreateCardItem(cardId);
        }
        
        // エネルギーアイテムをクリア
        ClearEnergyItems();
        
        // エネルギーアイテムを生成
        foreach (var energyReq in deck.EnergyRequirements)
        {
            CreateEnergyItem(energyReq);
        }
    }

    /// <summary>
    /// カードアイテムを作成
    /// </summary>
    /// <param name="cardId">カードID</param>
    private void CreateCardItem(string cardId)
    {
        if (cardItemPrefab == null || cardContainer == null)
            return;
            
        // CardViewItemコンポーネントを持つPrefabを生成
        GameObject cardItem = Instantiate(cardItemPrefab, cardContainer);
        cardItems.Add(cardItem);
        
        // カードモデルを取得
        CardModel cardModel = currentDeck.GetCardModel(cardId);
        
        if (cardModel != null)
        {
            // カード画像の表示
            RawImage cardImage = cardItem.GetComponentInChildren<RawImage>();
            if (cardImage != null && cardModel.imageTexture != null)
            {
                cardImage.texture = cardModel.imageTexture;
            }
            
            // カード名の表示
            TextMeshProUGUI nameText = cardItem.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = cardModel.name;
                
                // 同名カードが複数ある場合は枚数表示
                int sameNameCount = currentDeck.GetSameNameCardCount(cardModel.name);
                if (sameNameCount > 1)
                {
                    nameText.text += $" (x{sameNameCount})";
                }
            }
        }
        else
        {
            // カードモデルが見つからない場合はIDだけ表示
            TextMeshProUGUI textComponent = cardItem.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = $"不明なカード: {cardId}";
            }
        }
        
        // カード削除ボタンのイベント設定
        var removeButton = cardItem.GetComponentInChildren<Button>();
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(() => {
                // カード名を取得してフィードバック表示のために保持
                string cardName = "カード";
                if (cardModel != null && !string.IsNullOrEmpty(cardModel.name))
                {
                    cardName = cardModel.name;
                }
                
                // カード削除実行
                bool success = currentDeck.RemoveCard(cardId);
                
                // 削除成功時にフィードバック表示
                if (success)
                {
                    // フィードバック表示
                    if (FeedbackContainer.Instance != null)
                    {
                        FeedbackContainer.Instance.ShowSuccessFeedback($"デッキから削除： 「{cardName}」");
                    }
                    
                    // デッキの表示を更新
                    DisplayDeck(currentDeck);
                }
            });
        }
    }

    /// <summary>
    /// エネルギーアイテムを作成
    /// </summary>
    /// <param name="energyReq">エネルギー要件</param>
    private void CreateEnergyItem(EnergyRequirement energyReq)
    {
        if (energyItemPrefab == null || energyContainer == null)
            return;
            
        // EnergyViewItemコンポーネントを持つPrefabを生成
        GameObject energyItem = Instantiate(energyItemPrefab, energyContainer);
        energyItems.Add(energyItem);
        
        // TODO: エネルギー情報を表示
        // 仮実装として、単純なテキスト表示を行う
        var textComponent = energyItem.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = $"{energyReq.Type}: {energyReq.Count}";
        }
    }

    /// <summary>
    /// カードアイテムをすべて削除
    /// </summary>
    private void ClearCardItems()
    {
        foreach (var item in cardItems)
        {
            Destroy(item);
        }
        cardItems.Clear();
    }

    /// <summary>
    /// エネルギーアイテムをすべて削除
    /// </summary>
    private void ClearEnergyItems()
    {
        foreach (var item in energyItems)
        {
            Destroy(item);
        }
        energyItems.Clear();
    }

    #region UIイベントハンドラー
    /// <summary>
    /// デッキ名変更時の処理
    /// </summary>
    /// <param name="newName">新しいデッキ名</param>
    private void OnDeckNameChanged(string newName)
    {
        if (!string.IsNullOrEmpty(newName))
        {
            currentDeck.Name = newName;
        }
        else
        {
            // 空の場合は元に戻す
            deckNameInput.text = currentDeck.Name;
        }
    }

    /// <summary>
    /// 保存ボタンクリック時の処理
    /// </summary>
    private void OnSaveButtonClicked()
    {
        // DeckManagerにデッキの保存を任せる（成功/失敗のフィードバックもDeckManagerが表示する）
        DeckManager.Instance.SaveCurrentDeck();
        
        // 注意: ここでのフィードバック表示は削除（DeckManagerが適切なフィードバックを表示するため）
    }

    /// <summary>
    /// 新規デッキボタンクリック時の処理
    private void OnNewDeckButtonClicked()
    {
        currentDeck = DeckManager.Instance.CreateNewDeck();
        DisplayDeck(currentDeck);
    }
    
    /// <summary>
    /// 戻るボタンクリック時の処理
    /// </summary>
    private void OnBackButtonClicked()
    {
        // カードリスト画面に戻る処理
        // 実装例: パネル切り替えやシーン遷移など
        gameObject.SetActive(false);
    }
    #endregion
    
    /// <summary>
    /// カードをデッキに追加する公開メソッド
    /// 他のクラスからこのメソッドを呼び出してカードを追加できるようにする
    /// </summary>
    /// <param name="cardId">追加するカードID</param>
    /// <returns>追加に成功したかどうか</returns>
    public bool AddCardToDeck(string cardId)
    {
        bool success = currentDeck.AddCard(cardId);
        if (success)
        {
            DisplayDeck(currentDeck);
        }
        return success;
    }
}
