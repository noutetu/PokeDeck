using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private TMP_InputField deckMemoInput; // デッキメモ入力フィールド

    [Header("カードアイテム")]
    [SerializeField] private Transform cardContainer; // カードアイテムを配置するコンテナ
    [SerializeField] private GameObject cardItemPrefab; // カードアイテムのプレハブ

    [Header("エネルギーアイテム")]
    [SerializeField] private Transform energyContainer; // エネルギーアイテムを配置するコンテナ
    [SerializeField] private GameObject energyItemPrefab; // エネルギーアイテムのプレハブ

    [Header("ボタンUI")]
    [SerializeField] private Button saveButton; // 保存ボタン
    [SerializeField] private Button newDeckButton; // 新規デッキ作成ボタン
    [SerializeField] private Button openDeckListButton; // デッキ一覧を開くボタン
    [SerializeField] private Button shuffleButton; // シャッフルボタン

    [Header("デッキ一覧パネル")]
    [SerializeField] private GameObject deckListPanel; // デッキ一覧パネル

    [Header("エネルギー選択UI")]
    [SerializeField] private Button inputEnergyButton; // エネルギー選択ボタン
    [SerializeField] private Image energyImage1; // 1つ目のエネルギーアイコン
    [SerializeField] private Image energyImage2; // 2つ目のエネルギーアイコン
    [SerializeField] private SetEnergyPanel setEnergyPanel; // エネルギー選択パネル
    // ----------------------------------------------------------------------
    // プライベート変数
    // ----------------------------------------------------------------------
    private List<GameObject> cardItems = new List<GameObject>(); // デッキ内のカードオブジェクトのリスト
    private List<GameObject> energyItems = new List<GameObject>(); // エネルギービューアイテムのリスト
    private DeckModel currentDeck; // 現在表示中のデッキ

    // ----------------------------------------------------------------------
    // Unityライフサイクルメソッド
    // ----------------------------------------------------------------------
    private void OnEnable()
    {
        // 現在のデッキを取得
        currentDeck = DeckManager.Instance.CurrentDeck;

        // UIイベントを常に初期化（毎回セットアップするように変更）
        SetupUIEvents();

        // UI要素を初期化
        InitializeUI();

        // デッキをUIに表示
        DisplayDeck(currentDeck);
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


        // デッキメモの設定
        if (deckMemoInput != null)
        {
            // 自動改行を有効にする
            TMP_Text textComponent = deckMemoInput.GetComponentInChildren<TMP_Text>();
            textComponent.enableWordWrapping = true;
            // メモの変更時のイベントを設定
            deckMemoInput.onEndEdit.AddListener(OnDeckMemoChanged);
        }

        // 保存ボタンのクリックイベントを設定
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveButtonClicked);

        // 新規デッキ作成ボタンのクリックイベントを設定
        if (newDeckButton != null)
            newDeckButton.onClick.AddListener(OnNewDeckButtonClicked);

        // デッキ一覧を開くボタンのクリックイベントを設定
        if (openDeckListButton != null)
        {
            openDeckListButton.onClick.AddListener(() =>
            {
                if (deckListPanel != null)
                {
                    // 現在のパネルを非表示にし、デッキ一覧パネルを表示
                    gameObject.SetActive(false);
                    deckListPanel.SetActive(true);
                }
            });
        }

        // シャッフルボタンのクリックイベントを設定
        if (shuffleButton != null)
        {
            // 既存のリスナーをすべて削除して重複を防止
            shuffleButton.onClick.RemoveAllListeners();
            shuffleButton.onClick.AddListener(ShuffleDeck);
        }

        // 新しいエネルギー選択UIのセットアップ
        SetupEnergySelectionUI();
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

        // デッキメモを更新
        if (deckMemoInput != null)
            deckMemoInput.text = deck.Memo;

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

        // 新しいエネルギーボタンの画像を更新
        UpdateEnergyButtonImages();
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
        }

        // カード削除ボタンのイベントを設定
        var removeButton = cardItem.GetComponentInChildren<Button>();
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(() =>
            {
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

    // ----------------------------------------------------------------------
    // デッキメモ変更時の処理
    // ----------------------------------------------------------------------
    private void OnDeckMemoChanged(string newMemo)
    {
        // メモを更新（nullチェックは不要、空文字列も許容）
        currentDeck.Memo = newMemo;
    }

    private void OnSaveButtonClicked()
    {
        // エネルギータイプが選択されていない場合は自動選択する
        if (currentDeck.SelectedEnergyTypes.Count == 0)
        {
            currentDeck.AutoSelectEnergyTypes();
            // エネルギータイプが自動選択された場合はUI上のエネルギーアイコンも更新
            UpdateEnergyButtonImages();

            // 自動選択されたことをユーザーに通知
            if (currentDeck.SelectedEnergyTypes.Count > 0 && FeedbackContainer.Instance != null)
            {
                List<string> typeNames = new List<string>();
                foreach (var et in currentDeck.SelectedEnergyTypes)
                {
                    typeNames.Add(et.ToString());
                }
                string energyNames = string.Join("、", typeNames);
                FeedbackContainer.Instance.ShowProgressFeedback($"エネルギータイプが自動選択されました: {energyNames}");
            }
        }

        // 現在のデッキを保存
        DeckManager.Instance.SaveCurrentDeck();
    }

    // ----------------------------------------------------------------------
    // 新規デッキ作成ボタンの処理
    // ----------------------------------------------------------------------
    private void OnNewDeckButtonClicked()
    {
        // 新しいデッキを作成し、UIを更新
        currentDeck = DeckManager.Instance.CreateNewDeck();
        DisplayDeck(currentDeck);
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
    // ----------------------------------------------------------------------
    // エネルギー選択UIのセットアップ
    // ----------------------------------------------------------------------
    private void SetupEnergySelectionUI()
    {
        if (inputEnergyButton != null && setEnergyPanel != null)
        {
            // 既存のリスナーを削除してから新しいリスナーを追加（重複防止）
            inputEnergyButton.onClick.RemoveAllListeners();
            inputEnergyButton.onClick.AddListener(ToggleEnergyPanel);

            // エネルギー選択イベントのリスナー設定
            setEnergyPanel.OnEnergyTypeSelected -= OnEnergyTypesSelected; // 既存のリスナーを削除
            setEnergyPanel.OnEnergyTypeSelected += OnEnergyTypesSelected;

            // 初期状態ではパネルを非表示に
            setEnergyPanel.gameObject.SetActive(false);

        }
    }

    // ----------------------------------------------------------------------
    // エネルギーパネルの表示/非表示を切り替え
    // ----------------------------------------------------------------------
    private void ToggleEnergyPanel()
    {
        bool isActive = setEnergyPanel.gameObject.activeSelf;

        if (isActive)
        {
            // パネルを非表示
            setEnergyPanel.gameObject.SetActive(false);

            // 最終的な画像を更新（トグル選択中にすでに反映済み）
            UpdateEnergyButtonImages();
        }
        else
        {
            // パネルを表示する際に現在のデッキの選択状態を反映
            setEnergyPanel.ShowPanel(currentDeck);

            // 明示的にパネルを表示
            setEnergyPanel.gameObject.SetActive(true);;
        }
    }

    // ----------------------------------------------------------------------
    // エネルギータイプ選択時のコールバック
    // ----------------------------------------------------------------------
    private void OnEnergyTypesSelected(HashSet<Enum.PokemonType> selectedTypes)
    {
        // リアルタイムでボタン画像プレビューを更新
        UpdateEnergyButtonImagesPreview(selectedTypes);

        // 選択内容をデッキモデルに一時的に反映（パネルを閉じたときに正式に保存される）
        currentDeck.ClearSelectedEnergyTypes();
        foreach (var type in selectedTypes)
        {
            currentDeck.AddSelectedEnergyType(type);
        }
    }

    // ----------------------------------------------------------------------
    // エネルギーボタンの画像をプレビュー表示（選択中にリアルタイム更新）
    // ----------------------------------------------------------------------
    private void UpdateEnergyButtonImagesPreview(HashSet<Enum.PokemonType> types)
    {
        if (setEnergyPanel == null)
            return;

        // 最初に両方の画像を非表示に
        if (energyImage1 != null)
        {
            energyImage1.sprite = null;
            energyImage1.enabled = false;
        }

        if (energyImage2 != null)
        {
            energyImage2.sprite = null;
            energyImage2.enabled = false;
        }

        // 選択されたエネルギータイプを配列に変換
        List<Enum.PokemonType> typeList = new List<Enum.PokemonType>(types);

        // 選択されたタイプがあれば画像を設定
        if (typeList.Count > 0 && energyImage1 != null)
        {
            energyImage1.sprite = setEnergyPanel.GetEnergySprite(typeList[0]);
            energyImage1.enabled = true;
        }

        // 2つ目のエネルギータイプがあれば画像を設定
        if (typeList.Count > 1 && energyImage2 != null)
        {
            energyImage2.sprite = setEnergyPanel.GetEnergySprite(typeList[1]);
            energyImage2.enabled = true;
        }
    }

    // ----------------------------------------------------------------------
    // エネルギーボタンの画像を更新
    // ----------------------------------------------------------------------
    private void UpdateEnergyButtonImages()
    {
        if (currentDeck == null || setEnergyPanel == null)
            return;

        // 最初に両方の画像を非表示に
        if (energyImage1 != null)
        {
            energyImage1.sprite = null;
            energyImage1.enabled = false;
        }

        if (energyImage2 != null)
        {
            energyImage2.sprite = null;
            energyImage2.enabled = false;
        }

        // 選択されたエネルギータイプがあれば画像を設定
        var selectedTypes = currentDeck.SelectedEnergyTypes;

        if (selectedTypes.Count > 0 && energyImage1 != null)
        {
            energyImage1.sprite = setEnergyPanel.GetEnergySprite(selectedTypes[0]);
            energyImage1.enabled = true;
        }

        if (selectedTypes.Count > 1 && energyImage2 != null)
        {
            energyImage2.sprite = setEnergyPanel.GetEnergySprite(selectedTypes[1]);
            energyImage2.enabled = true;
        }
    }

    // ----------------------------------------------------------------------
    // エネルギーボタンの画像を更新する（外部からアクセス用）
    // ----------------------------------------------------------------------
    public void UpdateEnergyImages()
    {
        UpdateEnergyButtonImages();
    }

    // シャッフル中を示すフラグ（連続クリックによるエラーを防止）
    private bool isShuffling = false;

    // ----------------------------------------------------------------------
    // デッキシャッフル機能
    // デッキをシャッフル（表示順をランダムに並び替え）します。
    // シャッフル結果はデータとUIの両方に反映します。
    // 最初の5枚の中にたねポケモンが含まれるように調整します。
    // ----------------------------------------------------------------------
    public void ShuffleDeck()
    {
        // シャッフル中の場合は処理をスキップ
        if (isShuffling)
        {
            return;
        }

        // デッキが表示されていない、またはカードがない場合は処理しない
        if (currentDeck == null || cardItems.Count == 0)
        {
            FeedbackContainer.Instance?.ShowFailureFeedback("デッキにカードがありません");
            return;
        }

        // シャッフル開始
        isShuffling = true;

        try
        {
            // GameObject参照とCardModelの両方を保持する一時的なリスト
            List<(GameObject gameObject, CardModel cardModel, string cardId)> cardTriples = new List<(GameObject, CardModel, string)>();

            // 現在のカードアイテムとCardModelとIDのトリプルを作成
            for (int i = 0; i < cardItems.Count && i < currentDeck.CardIds.Count; i++)
            {
                string cardId = currentDeck.CardIds[i];
                CardModel cardModel = currentDeck.GetCardModel(cardId);

                // CardModelが取得できた場合のみトリプルを作成
                if (cardModel != null)
                {
                    cardTriples.Add((cardItems[i], cardModel, cardId));
                }
            }

            // デッキにたねポケモンが含まれていない場合は処理しない
            if (!CheckIfDeckContainsBasicPokemon(cardTriples))
            {
                FeedbackContainer.Instance?.ShowFailureFeedback("デッキにたねポケモンが含まれていません");
                return;
            }

            // 有効なシャッフル結果を得るまで繰り返す
            int maxAttempts = 50; // 最大試行回数を制限
            int attempts = 0;
            bool hasBasicPokemon = false;

            // 最初の5枚にたねポケモンが含まれるまでシャッフルを繰り返す
            while (!hasBasicPokemon && attempts < maxAttempts)
            {
                attempts++;

                // カードトリプルをシャッフル
                ShuffleList(cardTriples);

                // 最初の5枚にたねポケモンが含まれるかチェック
                hasBasicPokemon = CheckForBasicPokemonInFirstN(cardTriples, 5);
            }

            // シャッフル後のカードIDリストを作成（データ更新用）
            List<string> newCardIds = new List<string>();
            foreach (var triple in cardTriples)
            {
                newCardIds.Add(triple.cardId);
            }

            // デッキモデルのカード順序を更新 (データ側の更新)
            currentDeck.UpdateCardOrder(newCardIds);

            // UIのリフレッシュ
            ClearCardContainer();

            // 新しい順序でカードアイテムを再配置
            for (int i = 0; i < cardTriples.Count; i++)
            {
                GameObject cardItem = cardTriples[i].gameObject;
                cardItem.transform.SetParent(cardContainer);
                cardItem.transform.SetSiblingIndex(i);

                // 初期手札の表示設定を更新
                CardView cardView = cardItem.GetComponent<CardView>();
                if (cardView != null)
                {
                    // 最初の5枚のカードのみ初期手札表示をオンにする
                    bool isInitialHand = i < 5;
                    cardView.ToggleInitialHandDisplay(isInitialHand);
                }
            }

            // カードアイテムの参照リストも最新の順序で更新
            cardItems.Clear();
            foreach (var triple in cardTriples)
            {
                cardItems.Add(triple.gameObject);
            }
        }
        finally
        {
            // シャッフル処理完了のフラグをリセット
            isShuffling = false;
        }
    }

    // ----------------------------------------------------------------------
    // カードコンテナの子オブジェクトをすべて一時的に取り外す（親子関係をクリア）
    // ----------------------------------------------------------------------
    private void ClearCardContainer()
    {
        if (cardContainer == null) return;

        // 子オブジェクトのリストを作成（削除中にコレクションが変更されるのを防ぐため）
        List<Transform> children = new List<Transform>();
        foreach (Transform child in cardContainer)
        {
            children.Add(child);
        }

        // 各子オブジェクトを親から切り離す
        foreach (Transform child in children)
        {
            child.SetParent(null);
        }
    }

    // ----------------------------------------------------------------------
    // デッキ内にたねポケモンが含まれているかをチェック
    // ----------------------------------------------------------------------
    private bool CheckIfDeckContainsBasicPokemon(List<(GameObject gameObject, CardModel cardModel, string cardId)> cardTriples)
    {
        if (cardTriples == null || cardTriples.Count == 0)
            return false;

        // たねポケモンが含まれているかをチェック
        foreach (var triple in cardTriples)
        {
            // 各カードのCardModelを取得
            CardModel cardModel = triple.cardModel;
            // CardModelがnullでないことを確認
            // たねポケモンの条件を満たすかチェック
            if (cardModel != null &&

                (cardModel.cardTypeEnum == Enum.CardType.非EX || cardModel.cardTypeEnum == Enum.CardType.EX) &&
                cardModel.evolutionStageEnum == Enum.EvolutionStage.たね)
            {
                return true; // たねポケモン発見
            }
        }

        return false; // たねポケモンなし
    }

    // ----------------------------------------------------------------------
    // 先頭N枚の中にたねポケモンが含まれているかをチェック
    // ----------------------------------------------------------------------
    private bool CheckForBasicPokemonInFirstN(List<(GameObject gameObject, CardModel cardModel, string cardId)> cardTriples, int n)
    {
        if (cardTriples == null || cardTriples.Count == 0 || n <= 0)
            return false;

        // 実際にチェックする枚数（リストの枚数が少ない場合は全枚数）
        int checkCount = Math.Min(n, cardTriples.Count);

        // 先頭N枚をチェック
        for (int i = 0; i < checkCount; i++)
        {
            // 各カードのCardModelを取得
            CardModel cardModel = cardTriples[i].cardModel;
            // CardModelがnullでないことを確認
            // たねポケモンの条件を満たすかチェック
            if (cardModel != null &&
                (cardModel.cardTypeEnum == Enum.CardType.非EX || cardModel.cardTypeEnum == Enum.CardType.EX) &&
                cardModel.evolutionStageEnum == Enum.EvolutionStage.たね)
            {
                return true; // たねポケモン発見
            }
        }

        return false; // たねポケモンなし
    }

    // ----------------------------------------------------------------------
    // リストをランダムにシャッフルするヘルパーメソッド（Fisher-Yatesアルゴリズム）
    // ----------------------------------------------------------------------
    private void ShuffleList<T>(List<T> list)
    {
        System.Random random = new System.Random();
        int n = list.Count;

        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    // ----------------------------------------------------------------------
    // リソース解放処理
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        // イベントのクリーンアップ

        // デッキ名変更イベントを解除
        if (deckNameInput != null)
        {
            deckNameInput.onEndEdit.RemoveListener(OnDeckNameChanged);
        }

        // デッキメモ変更イベントを解除
        if (deckMemoInput != null)
        {
            deckMemoInput.onEndEdit.RemoveListener(OnDeckMemoChanged);
        }

        // 保存ボタンイベントを解除
        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(OnSaveButtonClicked);
        }

        // 新規デッキボタンイベントを解除
        if (newDeckButton != null)
        {
            newDeckButton.onClick.RemoveListener(OnNewDeckButtonClicked);
        }

        // シャッフルボタンイベントを解除
        if (shuffleButton != null)
        {
            shuffleButton.onClick.RemoveListener(ShuffleDeck);
        }

        // エネルギー選択パネルのイベントを解除
        if (setEnergyPanel != null)
        {
            setEnergyPanel.OnEnergyTypeSelected -= OnEnergyTypesSelected;
        }
    }

}
