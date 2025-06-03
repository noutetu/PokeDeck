using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

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
    [SerializeField] private TextMeshProUGUI deckCountText;   // デッキカウント
    

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
    [SerializeField] private Button closeEnergyButton; // エネルギー選択パネルを閉じるボタン
    // ----------------------------------------------------------------------
    // プライベート変数
    // ----------------------------------------------------------------------
    private List<GameObject> cardItems = new List<GameObject>(); // デッキ内のカードオブジェクトのリスト
    private List<GameObject> energyItems = new List<GameObject>(); // エネルギービューアイテムのリスト
    private DeckModel currentDeck; // 現在表示中のデッキ
    private bool isReadOnly = false; // 読み取り専用フラグ（サンプルデッキ用）

    // ----------------------------------------------------------------------
    // Unityライフサイクルメソッド
    // ----------------------------------------------------------------------
    private async void OnEnable()
    {
        try
        {
            // 現在のデッキを取得（nullの可能性がある）
            currentDeck = DeckManager.Instance?.CurrentDeck;

            // UIイベントを常に初期化（毎回セットアップするように変更）
            SetupUIEvents();

            // UI要素を初期化
            InitializeUI();

            // デッキをUIに表示（nullでも処理できるように修正済み）
            await DisplayDeck(currentDeck);
            
            // カード枚数を更新
            UpdateCardCount();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DeckView OnEnable中にエラーが発生: {ex.Message}");
            // エラーが発生した場合も基本的な状態を確保
            UpdateCardCount();
        }
    }

    // ----------------------------------------------------------------------
    // UI要素の初期設定
    // ----------------------------------------------------------------------
    private void InitializeUI()
    {
        try
        {
            // サンプルデッキかどうかを判定
            isReadOnly = DeckManager.Instance?.IsCurrentDeckSample() ?? false;
            
            // デッキ名を入力フィールドに設定
            if (deckNameInput != null && currentDeck != null)
            {
                deckNameInput.text = currentDeck.Name ?? "";
            }
            else if (deckNameInput != null)
            {
                deckNameInput.text = "";
            }
            
            // デッキメモを入力フィールドに設定
            if (deckMemoInput != null && currentDeck != null)
            {
                deckMemoInput.text = currentDeck.Memo ?? "";
            }
            else if (deckMemoInput != null)
            {
                deckMemoInput.text = "";
            }
            
            // UIコントロールの有効/無効を設定
            SetUIInteractability();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DeckView InitializeUI中にエラーが発生: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    // UIコントロールの有効/無効を設定（サンプルデッキ対応）
    // ----------------------------------------------------------------------
    private void SetUIInteractability()
    {
        // 保存ボタンのみ無効化（サンプルデッキの場合）
        if (saveButton != null)
            saveButton.interactable = !isReadOnly;
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
            textComponent.textWrappingMode = TMPro.TextWrappingModes.Normal;
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
    public async Task DisplayDeck(DeckModel deck)
    {
        try
        {
            // 現在のデッキを設定
            currentDeck = deck;
            
            // デッキがnullの場合は空の状態を表示
            if (deck == null)
            {
                // UIフィールドを空にクリア
                if (deckNameInput != null)
                {
                    deckNameInput.text = "";
                }
                
                if (deckMemoInput != null)
                {
                    deckMemoInput.text = "";
                }
                
                // 既存のカードアイテムをクリア
                ClearCardItems();
                
                // 既存のエネルギーアイテムをクリア
                ClearEnergyItems();
                
                // カード枚数を更新
                UpdateCardCount();
                
                return;
            }
            
            // サンプルデッキかどうかを再判定
            isReadOnly = DeckManager.Instance?.IsCurrentDeckSample() ?? false;
            
            // UIの有効/無効状態を更新
            SetUIInteractability();

            // デッキ名を更新
            if (deckNameInput != null)
            {
                deckNameInput.text = deck.Name ?? "";
            }

            // デッキメモを更新
            if (deckMemoInput != null)
            {
                deckMemoInput.text = deck.Memo ?? "";
            }

            // カードをタイプ順に並べ替え
            deck.SortCardsByTypeAndID();

            // 既存のカードアイテムをクリア
            ClearCardItems();

            // カードアイテムをすぐに生成（画像は非同期でロード）
            await CreateCardItemsWithAsyncImagesAsync(deck.CardIds);

            // 既存のエネルギーアイテムをクリア
            ClearEnergyItems();

            // 新しいエネルギーアイテムを生成
            if (deck.EnergyRequirements != null)
            {
                foreach (var energyReq in deck.EnergyRequirements)
                {
                    CreateEnergyItem(energyReq);
                }
            }

            // 新しいエネルギーボタンの画像を更新
            UpdateEnergyButtonImages();
            
            // カード枚数を更新
            UpdateCardCount();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DeckView DisplayDeck中にエラーが発生: {ex.Message}");
            // エラーが発生した場合でもカード枚数は更新
            UpdateCardCount();
        }
    }
    // ----------------------------------------------------------------------
    // カードアイテムを最適化された方法で作成（UIを先に表示、画像は非同期）
    // ----------------------------------------------------------------------
    private async Task CreateCardItemsWithAsyncImagesAsync(IReadOnlyList<string> cardIds)
    {
        if (cardItemPrefab == null || cardContainer == null || cardIds == null)
            return;

        // 1. まずすべてのカードアイテムのUIを同期的に作成（デフォルト画像で）
        var cardItemsData = new List<(GameObject item, string cardId, CardModel model)>();

        foreach (var cardId in cardIds)
        {
            var cardModel = currentDeck.GetCardModel(cardId);
            var cardItem = CreateCardItemUIOnly(cardId, cardModel);
            if (cardItem != null)
            {
                cardItemsData.Add((cardItem, cardId, cardModel));
            }
        }

        // 2. 画像を非同期で並列ロード
        if (cardItemsData.Count > 0)
        {
            await LoadCardImagesAsync(cardItemsData);
        }
    }

    // ----------------------------------------------------------------------
    // カードアイテムのUIのみを作成（画像は後で設定）
    // ----------------------------------------------------------------------
    private GameObject CreateCardItemUIOnly(string cardId, CardModel cardModel)
    {
        if (cardItemPrefab == null || cardContainer == null)
            return null;

        // カードアイテムを生成し、リストに追加
        GameObject cardItem = Instantiate(cardItemPrefab, cardContainer);
        cardItems.Add(cardItem);

        // デフォルト画像を設定
        RawImage cardImage = cardItem.GetComponentInChildren<RawImage>();
        if (cardImage != null && ImageCacheManager.Instance != null)
        {
            cardImage.texture = ImageCacheManager.Instance.GetDefaultTexture();
        }

        // カード削除ボタンのイベントを設定
        SetupCardRemoveButton(cardItem, cardId, cardModel);

        return cardItem;
    }

    // ----------------------------------------------------------------------
    // カード削除ボタンの設定
    // ----------------------------------------------------------------------
    private void SetupCardRemoveButton(GameObject cardItem, string cardId, CardModel cardModel)
    {
        var removeButton = cardItem.GetComponentInChildren<Button>();
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(async () =>
            {
                // currentDeckがnullの場合は処理しない
                if (currentDeck == null)
                {
                    if (FeedbackContainer.Instance != null)
                    {
                        FeedbackContainer.Instance.ShowFailureFeedback("デッキが選択されていません");
                    }
                    return;
                }
                
                // サンプルデッキの場合は削除を阻止
                if (isReadOnly)
                {
                    if (FeedbackContainer.Instance != null)
                    {
                        FeedbackContainer.Instance.ShowFailureFeedback("サンプルデッキのカードは削除できません");
                    }
                    return;
                }
                
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
                        int deckSize = currentDeck.CardIds.Count;
                        int maxDeckSize = DeckModel.MAX_CARDS;
                        FeedbackContainer.Instance.ShowSuccessFeedback($"デッキから削除： 「{cardName}」 ({deckSize}/{maxDeckSize})");
                    }
                    
                    // カード枚数表示を更新
                    UpdateCardCount();

                    // デッキを再表示
                    await DisplayDeck(currentDeck);
                }
            });
        }
    }

    // ----------------------------------------------------------------------
    // カード画像を非同期で並列ロード
    // ----------------------------------------------------------------------
    private async Task LoadCardImagesAsync(List<(GameObject item, string cardId, CardModel model)> cardItemsData)
    {
        if (ImageCacheManager.Instance == null)
            return;

        var loadTasks = new List<UniTask>();

        foreach (var (item, cardId, model) in cardItemsData)
        {
            if (model != null)
            {
                // 既にキャッシュされている場合は即座に設定
                if (ImageCacheManager.Instance.IsCardTextureCached(model))
                {
                    var cachedTexture = ImageCacheManager.Instance.GetCachedCardTexture(model);
                    var cardImage = item.GetComponentInChildren<RawImage>();
                    if (cardImage != null && cachedTexture != null)
                    {
                        cardImage.texture = cachedTexture;
                        model.imageTexture = cachedTexture;
                    }
                }
                else
                {
                    // キャッシュにない場合は非同期ロードのタスクに追加
                    loadTasks.Add(LoadSingleCardImageAsync(item, model));
                }
            }
        }

        // すべての画像を並列ロード
        if (loadTasks.Count > 0)
        {
            await UniTask.WhenAll(loadTasks);
        }
    }

    // ----------------------------------------------------------------------
    // 単一カード画像の非同期ロード
    // ----------------------------------------------------------------------
    private async UniTask LoadSingleCardImageAsync(GameObject cardItem, CardModel cardModel)
    {
        try
        {
            if (ImageCacheManager.Instance != null && cardItem != null && cardModel != null)
            {
                var texture = await ImageCacheManager.Instance.GetCardTextureAsync(cardModel);
                
                // UIオブジェクトがまだ有効か確認
                if (cardItem != null)
                {
                    var cardImage = cardItem.GetComponentInChildren<RawImage>();
                    if (cardImage != null && texture != null)
                    {
                        cardImage.texture = texture;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"カード画像ロード中にエラー: {ex.Message}");
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
        try
        {
            // currentDeckがnullの場合は処理しない
            if (currentDeck == null)
                return;
                
            // サンプルデッキの場合は変更を阻止
            if (isReadOnly)
            {
                // 元の名前に戻す
                if (deckNameInput != null)
                {
                    deckNameInput.text = currentDeck.Name ?? "";
                }
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowFailureFeedback("サンプルデッキは変更できません");
                }
                return;
            }
            
            // 新しいデッキ名が空でない場合に更新
            if (!string.IsNullOrEmpty(newName))
            {
                currentDeck.Name = newName;
            }
            else
            {
                // 空の場合は元の名前に戻す
                if (deckNameInput != null)
                {
                    deckNameInput.text = currentDeck.Name ?? "";
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DeckView OnDeckNameChanged中にエラーが発生: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    // デッキメモ変更時の処理
    // ----------------------------------------------------------------------
    private void OnDeckMemoChanged(string newMemo)
    {
        try
        {
            // currentDeckがnullの場合は処理しない
            if (currentDeck == null)
                return;
                
            // サンプルデッキの場合は変更を阻止
            if (isReadOnly)
            {
                // 元のメモに戻す
                if (deckMemoInput != null)
                {
                    deckMemoInput.text = currentDeck.Memo ?? "";
                }
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowFailureFeedback("サンプルデッキは変更できません");
                }
                return;
            }
            
            // メモを更新（nullチェックは不要、空文字列も許容）
            currentDeck.Memo = newMemo ?? "";
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DeckView OnDeckMemoChanged中にエラーが発生: {ex.Message}");
        }
    }

    private void OnSaveButtonClicked()
    {
        try
        {
            // currentDeckがnullの場合は処理しない
            if (currentDeck == null)
            {
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowFailureFeedback("保存するデッキがありません");
                }
                return;
            }
            
            // サンプルデッキの場合は保存を阻止
            if (isReadOnly)
            {
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowFailureFeedback("サンプルデッキは保存できません");
                }
                return;
            }
            
            // デッキ名が設定されているかチェック
            if (string.IsNullOrEmpty(currentDeck.Name) || string.IsNullOrWhiteSpace(currentDeck.Name))
            {
                // デッキ名が設定されていない場合は保存を阻止し、エラーメッセージを表示
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowFailureFeedback("デッキ名を入力してください");
                }
                return; // 保存処理を中断
            }

            // たねポケモンの存在チェック
            if (!CheckIfDeckContainsBasicPokemon(currentDeck))
            {
                // たねポケモンがない場合は保存を阻止し、エラーメッセージを表示
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowFailureFeedback("デッキにたねポケモンが含まれていないため保存できません");
                }
                return; // 保存処理を中断
            }

            // エネルギータイプが選択されていない場合は自動選択する
            if (currentDeck.SelectedEnergyTypes?.Count == 0)
            {
                currentDeck.AutoSelectEnergyTypes();
                // エネルギータイプが自動選択された場合はUI上のエネルギーアイコンも更新
                UpdateEnergyButtonImages();

                // 自動選択されたことをユーザーに通知
                if (currentDeck.SelectedEnergyTypes?.Count > 0 && FeedbackContainer.Instance != null)
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

            // DeckManagerが利用可能かチェック
            if (DeckManager.Instance == null)
            {
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowFailureFeedback("デッキマネージャーが利用できません");
                }
                return;
            }

            // 現在のデッキを保存
            DeckManager.Instance.SaveCurrentDeck();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DeckView OnSaveButtonClicked中にエラーが発生: {ex.Message}");
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback("デッキの保存中にエラーが発生しました");
            }
        }
    }

    // ----------------------------------------------------------------------
    // 新規デッキ作成ボタンの処理
    // ----------------------------------------------------------------------
    private async void OnNewDeckButtonClicked()
    {
        // 新しいデッキを作成し、UIを更新
        currentDeck = DeckManager.Instance.CreateNewDeck();
        
        // 読み取り専用フラグをリセット（新しいデッキは通常のデッキなので）
        isReadOnly = false;
        
        // UIの有効/無効状態を更新
        SetUIInteractability();
        
        await DisplayDeck(currentDeck);
        // フィードバックを送信
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowSuccessFeedback("新しいデッキ");
        }
    }

    // ----------------------------------------------------------------------
    // カードをデッキに追加する公開メソッド
    // ----------------------------------------------------------------------
    public async Task<bool> AddCardToDeck(string cardId)
    {
        // currentDeckがnullの場合は処理しない
        if (currentDeck == null)
        {
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback("デッキが選択されていません");
            }
            return false;
        }
        
        // サンプルデッキの場合はカード追加を阻止
        if (isReadOnly)
        {
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback("サンプルデッキにはカードを追加できません");
            }
            return false;
        }
        
        // カードをデッキに追加
        bool success = currentDeck.AddCard(cardId);
        if (success)
        {
            // デッキを再表示
            await DisplayDeck(currentDeck);
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
            // 既存のリスナーを削除してから新しいリスナーを追加
            inputEnergyButton.onClick.RemoveAllListeners();
            inputEnergyButton.onClick.AddListener(ToggleEnergyPanel);

            // エネルギー選択イベントのリスナー設定
            setEnergyPanel.OnEnergyTypeSelected -= OnEnergyTypesSelected;
            setEnergyPanel.OnEnergyTypeSelected += OnEnergyTypesSelected;

            // 閉じるボタンのイベント設定
            if (closeEnergyButton != null)
            {
                closeEnergyButton.onClick.RemoveAllListeners();
                closeEnergyButton.onClick.AddListener(CloseEnergyPanel);
            }

            // 初期状態を設定
            setEnergyPanel.gameObject.SetActive(false);
            if (closeEnergyButton != null)
            {
                closeEnergyButton.gameObject.SetActive(false);
            }
        }
    }

    // ----------------------------------------------------------------------
    // エネルギーパネルの表示/非表示を切り替え
    // ----------------------------------------------------------------------
    public void ToggleEnergyPanel()
    {   
        // サンプルデッキの場合はパネルを開かない
        if (isReadOnly)
        {
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback("サンプルデッキのエネルギータイプは変更できません");
            }
            return;
        }
        
        // currentDeckがnullの場合は処理を中断
        if (currentDeck == null)
        {
            return;
        }

        if (setEnergyPanel == null)
        {
            return;
        }

        bool isActive = setEnergyPanel.gameObject.activeSelf;

        if (isActive)
        {
            // パネルを非表示
            setEnergyPanel.HidePanel();
            
            // 閉じるボタンも非表示
            if (closeEnergyButton != null)
            {
                closeEnergyButton.gameObject.SetActive(false);
            }
            
            // 最終的な画像を更新
            UpdateEnergyButtonImages();
        }
        else
        {
            // まず閉じるボタンを表示
            if (closeEnergyButton != null)
            {
                closeEnergyButton.gameObject.SetActive(true);
            }
            
            // パネルを表示する際に現在のデッキの選択状態を反映
            if (currentDeck != null)
            {
                setEnergyPanel.ShowPanel(currentDeck);
            }
            else
            {
                setEnergyPanel.ShowPanel(null);
            }
        }
    }

    // ----------------------------------------------------------------------
    // エネルギータイプ選択時のコールバック
    // ----------------------------------------------------------------------
    private void OnEnergyTypesSelected(HashSet<Enum.PokemonType> selectedTypes)
    {
        // currentDeckがnullの場合は処理しない
        if (currentDeck == null)
            return;
            
        // サンプルデッキの場合は変更を阻止
        if (isReadOnly)
        {
            return;
        }
        
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
    // 最初の5枚の中にたねポケモンが含まれるように調整します。
    // ----------------------------------------------------------------------
    public void ShuffleDeck()
    {
        // 前提条件をチェック
        if (!CanPerformShuffle())
            return;

        isShuffling = true;

        try
        {
            // カードデータを準備
            var cardTriples = PrepareCardTriplesForShuffle();
            if (cardTriples == null || cardTriples.Count == 0)
                return;

            // たねポケモンの存在をチェック
            if (!ValidateDeckHasBasicPokemon(cardTriples))
                return;

            // シャッフルを実行
            var shuffledTriples = PerformShuffleWithValidation(cardTriples);

            // データとUIを更新
            UpdateDeckAfterShuffle(shuffledTriples);

            // 成功フィードバックを表示
            FeedbackContainer.Instance?.ShowSuccessFeedback("デッキをシャッフルしました", 0.4f);
        }
        finally
        {
            isShuffling = false;
        }
    }

    // ----------------------------------------------------------------------
    // シャッフル実行可能かチェック
    // ----------------------------------------------------------------------
    private bool CanPerformShuffle()
    {
        // シャッフル中の場合は処理をスキップ
        if (isShuffling)
            return false;

        // デッキが表示されていない、またはカードがない場合は処理しない
        if (currentDeck == null || cardItems.Count == 0)
        {
            FeedbackContainer.Instance?.ShowFailureFeedback("デッキにカードがありません");
            return false;
        }

        return true;
    }

    // ----------------------------------------------------------------------
    // シャッフル用のカードデータを準備
    // ----------------------------------------------------------------------
    private List<(GameObject gameObject, CardModel cardModel, string cardId)> PrepareCardTriplesForShuffle()
    {
        var cardTriples = new List<(GameObject, CardModel, string)>();

        // currentDeckがnullの場合は空のリストを返す
        if (currentDeck == null)
            return cardTriples;

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

        return cardTriples;
    }

    // ----------------------------------------------------------------------
    // デッキにたねポケモンが含まれているかバリデーション
    // ----------------------------------------------------------------------
    private bool ValidateDeckHasBasicPokemon(List<(GameObject gameObject, CardModel cardModel, string cardId)> cardTriples)
    {
        if (!CheckIfDeckContainsBasicPokemon(cardTriples))
        {
            FeedbackContainer.Instance?.ShowFailureFeedback("デッキにたねポケモンが含まれていません");
            return false;
        }
        return true;
    }

    // ----------------------------------------------------------------------
    // バリデーション付きシャッフルを実行
    // ----------------------------------------------------------------------
    private List<(GameObject gameObject, CardModel cardModel, string cardId)> PerformShuffleWithValidation(
        List<(GameObject gameObject, CardModel cardModel, string cardId)> cardTriples)
    {
        const int maxAttempts = 50;
        int attempts = 0;
        bool hasBasicPokemon = false;

        // 最初の5枚にたねポケモンが含まれるまでシャッフルを繰り返す
        while (!hasBasicPokemon && attempts < maxAttempts)
        {
            attempts++;
            ShuffleList(cardTriples);
            hasBasicPokemon = CheckForBasicPokemonInFirstN(cardTriples, 5);
        }

        return cardTriples;
    }

    // ----------------------------------------------------------------------
    // シャッフル後のデータとUIを更新
    // ----------------------------------------------------------------------
    private void UpdateDeckAfterShuffle(List<(GameObject gameObject, CardModel cardModel, string cardId)> cardTriples)
    {
        // データ側の更新
        UpdateDeckModelOrder(cardTriples);

        // UI側の更新
        UpdateUIAfterShuffle(cardTriples);
    }

    // ----------------------------------------------------------------------
    // デッキモデルのカード順序を更新
    // ----------------------------------------------------------------------
    private void UpdateDeckModelOrder(List<(GameObject gameObject, CardModel cardModel, string cardId)> cardTriples)
    {
        // currentDeckがnullの場合は処理しない
        if (currentDeck == null)
            return;
            
        // シャッフル後のカードIDリストを作成
        List<string> newCardIds = new List<string>();
        foreach (var triple in cardTriples)
        {
            newCardIds.Add(triple.cardId);
        }

        // デッキモデルのカード順序を更新
        currentDeck.UpdateCardOrder(newCardIds);
    }

    // ----------------------------------------------------------------------
    // シャッフル後のUI更新
    // ----------------------------------------------------------------------
    private void UpdateUIAfterShuffle(List<(GameObject gameObject, CardModel cardModel, string cardId)> cardTriples)
    {
        // UIのリフレッシュ
        ClearCardContainer();

        // 新しい順序でカードアイテムを再配置
        RearrangeCardItems(cardTriples);

        // カードアイテムの参照リストを更新
        UpdateCardItemsList(cardTriples);

        // レイアウトの更新を強制
        ForceLayoutRebuild();
    }

    // ----------------------------------------------------------------------
    // カードアイテムを新しい順序で再配置
    // ----------------------------------------------------------------------
    private void RearrangeCardItems(List<(GameObject gameObject, CardModel cardModel, string cardId)> cardTriples)
    {
        for (int i = 0; i < cardTriples.Count; i++)
        {
            GameObject cardItem = cardTriples[i].gameObject;
            
            // ワールド位置を維持しないように明示的に指定
            cardItem.transform.SetParent(cardContainer, false);
            cardItem.transform.SetSiblingIndex(i);
            cardItem.transform.localPosition = Vector3.zero;

            // 初期手札の表示設定を更新
            UpdateInitialHandDisplay(cardItem, i < 5);
        }
    }

    // ----------------------------------------------------------------------
    // 初期手札表示の更新
    // ----------------------------------------------------------------------
    private void UpdateInitialHandDisplay(GameObject cardItem, bool isInitialHand)
    {
        CardView cardView = cardItem.GetComponent<CardView>();
        if (cardView != null)
        {
            cardView.ToggleInitialHandDisplay(isInitialHand);
        }
    }

    // ----------------------------------------------------------------------
    // カードアイテムリストを更新
    // ----------------------------------------------------------------------
    private void UpdateCardItemsList(List<(GameObject gameObject, CardModel cardModel, string cardId)> cardTriples)
    {
        cardItems.Clear();
        foreach (var triple in cardTriples)
        {
            cardItems.Add(triple.gameObject);
        }
    }

    // ----------------------------------------------------------------------
    // レイアウトの強制再構築
    // ----------------------------------------------------------------------
    private void ForceLayoutRebuild()
    {
        if (cardContainer is RectTransform rectTransform)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
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

        // 各子オブジェクトを親から切り離す（ワールド位置を維持しない）
        foreach (Transform child in children)
        {
            child.SetParent(null, false);
        }
    }

    // ----------------------------------------------------------------------
    // デッキ内にたねポケモンが含まれているかをチェック（Tripleバージョン - シャッフル用）
    // ----------------------------------------------------------------------
    private bool CheckIfDeckContainsBasicPokemon(List<(GameObject gameObject, CardModel cardModel, string cardId)> cardTriples)
    {
        if (cardTriples == null || cardTriples.Count == 0)
            return false;

        // CardModelのリストを抽出してオーバーロードを呼び出し
        var cardModels = new List<CardModel>();
        foreach (var triple in cardTriples)
        {
            if (triple.cardModel != null)
                cardModels.Add(triple.cardModel);
        }

        return CheckIfDeckContainsBasicPokemon(cardModels);
    }

    // ----------------------------------------------------------------------
    // デッキ内にたねポケモンが含まれているかをチェック（CardModelバージョン - 汎用）
    // ----------------------------------------------------------------------
    private bool CheckIfDeckContainsBasicPokemon(List<CardModel> cardModels)
    {
        if (cardModels == null || cardModels.Count == 0)
            return false;

        // たねポケモンが含まれているかをチェック
        foreach (var cardModel in cardModels)
        {
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
    // デッキ内にたねポケモンが含まれているかをチェック（DeckModelバージョン - 保存時チェック用）
    // ----------------------------------------------------------------------
    private bool CheckIfDeckContainsBasicPokemon(DeckModel deck)
    {
        if (deck == null || deck.CardIds.Count == 0)
            return false;

        // デッキ内の全カードをCardModelリストに変換
        var cardModels = new List<CardModel>();
        foreach (string cardId in deck.CardIds)
        {
            CardModel cardModel = deck.GetCardModel(cardId);
            if (cardModel != null)
                cardModels.Add(cardModel);
        }

        // CardModelバージョンのメソッドを呼び出し
        return CheckIfDeckContainsBasicPokemon(cardModels);
    }

    // ----------------------------------------------------------------------
    // 先頭N枚の中にたねポケモンが含まれているかをチェック（Tripleバージョン - シャッフル用）
    // ----------------------------------------------------------------------
    private bool CheckForBasicPokemonInFirstN(List<(GameObject gameObject, CardModel cardModel, string cardId)> cardTriples, int n)
    {
        if (cardTriples == null || cardTriples.Count == 0 || n <= 0)
            return false;

        // CardModelのリストを抽出してオーバーロードを呼び出し
        var cardModels = new List<CardModel>();
        int checkCount = Math.Min(n, cardTriples.Count);
        
        for (int i = 0; i < checkCount; i++)
        {
            if (cardTriples[i].cardModel != null)
                cardModels.Add(cardTriples[i].cardModel);
        }

        return CheckForBasicPokemonInFirstN(cardModels, cardModels.Count);
    }

    // ----------------------------------------------------------------------
    // 先頭N枚の中にたねポケモンが含まれているかをチェック（CardModelバージョン - 汎用）
    // ----------------------------------------------------------------------
    private bool CheckForBasicPokemonInFirstN(List<CardModel> cardModels, int n)
    {
        if (cardModels == null || cardModels.Count == 0 || n <= 0)
            return false;

        // 実際にチェックする枚数（リストの枚数が少ない場合は全枚数）
        int checkCount = Math.Min(n, cardModels.Count);

        // 先頭N枚をチェック
        for (int i = 0; i < checkCount; i++)
        {
            CardModel cardModel = cardModels[i];
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
    // エネルギーパネルを閉じる
    // ----------------------------------------------------------------------
    private void CloseEnergyPanel()
    {
        // パネルを非表示
        if (setEnergyPanel != null)
        {
            setEnergyPanel.HidePanel();
        }
        
        // 閉じるボタンも非表示
        if (closeEnergyButton != null)
        {
            closeEnergyButton.gameObject.SetActive(false);
        }
        
        // 最終的な画像を更新
        UpdateEnergyButtonImages();
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
        
        // 閉じるボタンのイベントリスナーを解除
        if (closeEnergyButton != null)
        {
            closeEnergyButton.onClick.RemoveAllListeners();
        }
    }

    // ----------------------------------------------------------------------
    // デッキ内のカード枚数を更新して表示
    // ----------------------------------------------------------------------
    private void UpdateCardCount()
    {
        if (deckCountText != null)
        {
            if (currentDeck != null)
            {
                int cardCount = currentDeck.CardCount;
                int maxCards = DeckModel.MAX_CARDS;
                deckCountText.text = $"{cardCount}/{maxCards}";
                
                // カード枚数に応じて色を変更（任意）
                if (cardCount > maxCards)
                {
                    // デッキがオーバーしている場合は赤色で表示
                    deckCountText.color = new Color(1.0f, 0.3f, 0.3f);
                }
                else if (cardCount == maxCards)
                {
                    // デッキが丁度20枚の場合は緑色で表示
                    deckCountText.color = new Color(0.3f, 0.8f, 0.3f);
                }
                else
                {
                    // 通常時は白色で表示
                    deckCountText.color = Color.black;
                }
            }
            else
            {
                // currentDeckがnullの場合は空の表示
                deckCountText.text = "0/20";
                deckCountText.color = Color.gray;
            }
        }
    }
}
