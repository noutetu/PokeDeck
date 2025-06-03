using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Linq.Expressions;

// ----------------------------------------------------------------------
// デッキ一覧パネルの各デッキアイテムを管理するクラス
// ----------------------------------------------------------------------
public class DeckListItem : MonoBehaviour
{
    [SerializeField] private TMP_InputField deckNameInput;      // デッキ名入力フィールド
    [SerializeField] private RawImage cardImage;        // デッキアイコン画像
    [SerializeField] private Button selectButton;       // デッキ選択ボタン
    [SerializeField] private Button deleteButton;       // デッキ削除ボタン
    [SerializeField] private Button copyButton;        // デッキコピー用ボタン

    // パネル
    private GameObject deckListPanel; // 親のデッキ一覧パネル
    private GameObject sampleDeckPanel; // サンプルデッキパネル

    // デッキ選択イベント
    public UnityEvent OnDeckSelected = new UnityEvent();

    private DeckModel currentDeck;  // 現在のデッキ情報 

    // ----------------------------------------------------------------------
    // Unityの初期化処理
    // ----------------------------------------------------------------------
    private void Start()
    {
        // 選択ボタンのイベント設定
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(() =>
            {
                OnDeckSelected.Invoke();
            });
        }

        // デッキ名入力フィールドのイベント設定
        if (deckNameInput != null)
        {
            // デッキ名変更時に保存
            deckNameInput.onEndEdit.AddListener(OnDeckNameChanged);
        }

        // 削除ボタンのイベント設定
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }
        // コピー用ボタンのイベント設定
        if (copyButton != null)
        {
            copyButton.onClick.AddListener(OnCopyButtonClicked);
        }
    }

    // ----------------------------------------------------------------------
    /// デッキ情報を設定
    // ----------------------------------------------------------------------
    public void SetDeckInfo(DeckModel deck)
    {
        currentDeck = deck;

        if (deckNameInput != null && deck != null)
        {
            deckNameInput.text = deck.Name;
        }

        // 最も体力の高いポケモンをアイコンに設定
        SetHighestHPPokemonAsIcon(deck);
    }

    // ----------------------------------------------------------------------
    /// デッキ名変更時の処理
    // ----------------------------------------------------------------------
    private void OnDeckNameChanged(string newName)
    {
        if (currentDeck == null)
            return;
        // 空の場合は何もしない（元の名前を維持）
        if (string.IsNullOrEmpty(newName))
        {
            deckNameInput.text = currentDeck.Name;
            return;
        }

        // 現在のデッキ名と異なる場合のみ保存処理
        if (currentDeck.Name != newName)
        {
            // デッキ名を更新
            string oldName = currentDeck.Name;
            currentDeck.Name = newName;

            // DeckManagerに変更を保存
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.SaveCurrentDeck();

                // フィードバック表示
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowSuccessFeedback($"デッキ名を変更しました: {oldName} → {newName}");
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    /// 最も体力の高いポケモンをアイコンに設定
    // ----------------------------------------------------------------------
    private async void SetHighestHPPokemonAsIcon(DeckModel deck)
    {
        if (cardImage == null || deck == null)
            return;

        CardModel highestHPCard = null;
        int highestHP = 0;

        // デッキ内のすべてのカードをチェック
        foreach (string cardId in deck.CardIds)
        {
            CardModel card = deck.GetCardModel(cardId);

            // ポケモンカードで、HPが最高値のカードを探す
            if (card != null &&
                (card.cardTypeEnum == Enum.CardType.非EX || card.cardTypeEnum == Enum.CardType.EX) &&
                card.hp > highestHP)
            {
                highestHP = card.hp;
                highestHPCard = card;
            }
        }

        // 最も体力の高いポケモンが見つかった場合、そのテクスチャを設定
        if (highestHPCard != null)
        {
            // ImageCacheManagerを使用してキャッシュを確認
            if (ImageCacheManager.Instance != null)
            {
                // キャッシュされている場合は即座に設定
                if (ImageCacheManager.Instance.IsCardTextureCached(highestHPCard))
                {
                    Texture2D cachedTexture = ImageCacheManager.Instance.GetCachedCardTexture(highestHPCard);
                    cardImage.texture = cachedTexture;
                    highestHPCard.imageTexture = cachedTexture;
                }
                else
                {
                    // キャッシュにない場合は非同期で読み込み
                    try
                    {
                        Texture2D texture = await ImageCacheManager.Instance.GetCardTextureAsync(highestHPCard);
                        if (cardImage != null && texture != null) // UI要素がまだ有効かチェック
                        {
                            cardImage.texture = texture;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // エラー時はデフォルトテクスチャを設定
                        if (cardImage != null && ImageCacheManager.Instance != null)
                        {
                            cardImage.texture = ImageCacheManager.Instance.GetDefaultTexture();
                        }
                    }
                }
            }
            else if (highestHPCard.imageTexture != null)
            {
                // ImageCacheManagerが利用できない場合は既存のテクスチャを使用
                cardImage.texture = highestHPCard.imageTexture;
            }
        }
        else
        {
            // デッキにポケモンがない場合はデフォルトテクスチャを設定
            if (ImageCacheManager.Instance != null)
            {
                cardImage.texture = ImageCacheManager.Instance.GetDefaultTexture();
            }
        }
    }

    // ----------------------------------------------------------------------
    /// 削除ボタンクリック時の処理
    // ----------------------------------------------------------------------
    private void OnDeleteButtonClicked()
    {
        if (currentDeck == null)
            return;

        // デッキ名を保持（フィードバック表示用）
        string deckName = currentDeck.Name;

        // DeckManagerを使用してデッキを削除
        if (DeckManager.Instance != null)
        {
            bool success = DeckManager.Instance.DeleteDeck(deckName);

            if (success)
            {
                // フィードバック表示
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowSuccessFeedback($"デッキを削除しました: {deckName}");
                }

                // 親のDeckListPanelを取得して更新を通知
                var deckListPanel = GetComponentInParent<DeckListPanel>();
                if (deckListPanel != null)
                {
                    deckListPanel.RefreshDeckList();
                }
            }
            else
            {
                // 削除失敗時のフィードバック
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowFailureFeedback($"デッキの削除に失敗しました: {deckName}");
                }
            }
        }
    }
    // ----------------------------------------------------------------------
    // コピー用ボタンクリック時の処理
    // ----------------------------------------------------------------------
    private async void OnCopyButtonClicked()
    {
        if (currentDeck == null)
            return;

        // コピー処理中はボタンを無効化
        if (copyButton != null)
            copyButton.interactable = false;

        try
        {
            // デッキ名を保持（フィードバック表示用）
            string originalDeckName = currentDeck.Name;

            // DeckManagerを使用してデッキをコピー（非同期版を使用）
            if (DeckManager.Instance != null)
            {
                // サンプルデッキかどうかを判別
                bool isSampleDeck = DeckManager.Instance.IsSampleDeck(originalDeckName);
                
                // 非同期でコピー処理を実行（画像の読み込みとUI更新も含む）
                DeckModel copiedDeck = await DeckManager.Instance.CopyDeckAsync(originalDeckName);
                
                if (copiedDeck != null)
                {
                    // サンプルデッキからのコピーの場合は、追加のフィードバックメッセージを表示
                    if (isSampleDeck && FeedbackContainer.Instance != null)
                    {
                        FeedbackContainer.Instance.ShowSuccessFeedback($"サンプルデッキ '{originalDeckName}' を通常デッキにコピーしました: '{copiedDeck.Name}'");
                    }
                    // 通常デッキからのコピーの場合は、既存のフィードバックメッセージ（LoadCardTexturesAndRefreshUIAsyncで表示）に任せる
                }
                // デッキ一覧パネルを表示
                if (deckListPanel != null)
                {
                    deckListPanel.gameObject.SetActive(true);
                }
                // サンプルデッキパネルを閉じる
                if (sampleDeckPanel != null)
                {
                    sampleDeckPanel.gameObject.SetActive(false);
                }
            }
        }
        catch (System.Exception ex)
        {
            // エラーハンドリング
            if (FeedbackContainer.Instance != null)
            {
                FeedbackContainer.Instance.ShowFailureFeedback($"デッキのコピー中にエラーが発生しました: {ex.Message}");
            }
        }
        finally
        {
            // 処理完了後、ボタンを再度有効化
            if (copyButton != null)
                copyButton.interactable = true;
        }
    }

    // ----------------------------------------------------------------------
    /// サンプルデッキ用のアクティブ設定
    // ----------------------------------------------------------------------
    public void SetActiveForSampleDeck(GameObject deckListPanel, GameObject sampleDeckPanel)
    {
        this.deckListPanel = deckListPanel;
        this.sampleDeckPanel = sampleDeckPanel;

        deleteButton.gameObject.SetActive(false);
        deckNameInput.readOnly = true;
    }
}