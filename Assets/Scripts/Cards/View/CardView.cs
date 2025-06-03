using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // UIイベント検出のため追加

// ----------------------------------------------------------------------
// カード1枚分のUI表示を担当するクラス
// カードモデルのデータをUIコンポーネントに反映し、適切な表示形式を選択する
// ----------------------------------------------------------------------
public class CardView : MonoBehaviour, IPointerClickHandler
{
    // ----------------------------------------------------------------------
    // 定数クラス
    // ----------------------------------------------------------------------
    private static class Constants
    {
        public const float DOUBLE_CLICK_THRESHOLD_SECONDS = 1f;
        public const string ADD_SUCCESS_TEXT = "デッキに追加！";
        public const string SAME_CARD_LIMIT_TEXT = "同名カード上限";
        public const string DECK_FULL_MESSAGE = "デッキは24枚まで追加可能です";
        public const string ADDITION_FAILED_TEXT = "追加失敗";
        public const string DECK_FULL_REASON = "デッキ上限（24枚）";
        public const string SAME_NAME_LIMIT_REASON_FORMAT = "同名上限（{0}枚）";
        public const string CARD_ADDED_MESSAGE_FORMAT = "「{0}」をデッキに追加しました！\nデッキサイズ: {1}/{2}";
        public const byte PLACEHOLDER_GRAY_VALUE = 200;
        public const byte PLACEHOLDER_ALPHA_VALUE = 255;
        public const int PLACEHOLDER_TEXTURE_SIZE = 2;
    }
    // ----------------------------------------------------------------------
    // カードの表示に使用するデータモデル
    // ----------------------------------------------------------------------
    private CardModel data;

    // ----------------------------------------------------------------------
    // UI表示用コンポーネント - Inspector上で設定する
    // ----------------------------------------------------------------------
    // 基本情報表示用コンポーネント
    [SerializeField] private RawImage cardImage;        // カード画像表示用
    [SerializeField] private Button cardButton;         // クリックイベント用ボタン
    [SerializeField] private GameObject initialHandSign; // 初手表示用テキスト

    // ----------------------------------------------------------------------
    // 画像読み込み状態の管理
    // ----------------------------------------------------------------------
    private bool isImageLoading = false;

    // ----------------------------------------------------------------------
    // ダブルクリック検出用変数
    // ----------------------------------------------------------------------
    private float lastClickTime;

    // ----------------------------------------------------------------------
    // Awakeメソッド - 初期化処理
    // ボタンコンポーネントがなければ追加
    // ボタンコンポーネントがある場合は、クリックイベントを登録
    // クリックイベントはIPointerClickHandlerインターフェースを実装している
    // ----------------------------------------------------------------------
    private void Awake()
    {
        EnsureCardButtonExists();
    }
    
    // ----------------------------------------------------------------------
    // カードボタンコンポーネントの確保
    // ----------------------------------------------------------------------
    private void EnsureCardButtonExists()
    {
        if (cardButton == null)
        {
            cardButton = GetComponent<Button>();
            if (cardButton == null)
            {
                cardButton = gameObject.AddComponent<Button>();
            }
        }
    }

    // ----------------------------------------------------------------------
    // カードデータを設定し、適切な表示形式でUIを更新する
    // data 表示するカードデータ
    // ----------------------------------------------------------------------
    public void SetImage(CardModel data)
    {
        this.data = data;
        
        // データが無効な場合は早期リターン
        if (data == null)
        {
            return;
        }
        
        // 1. キャッシュから画像読み込みを試行
        if (TryLoadFromCache())
        {
            return;
        }
        
        // 2. CardModelから既存テクスチャ読み込みを試行
        if (TryLoadFromCardModel())
        {
            return;
        }
        
        // 3. 画像が見つからない場合のフォールバック処理
        HandleMissingTexture();
    }
    
    // ----------------------------------------------------------------------
    // キャッシュからの画像読み込み試行
    // ----------------------------------------------------------------------
    private bool TryLoadFromCache()
    {
        // ImageCacheManagerの存在確認とキャッシュ状態チェック
        if (ImageCacheManager.Instance != null && ImageCacheManager.Instance.IsCardTextureCached(data))
        {
            // キャッシュからテクスチャを取得
            Texture2D cachedTexture = ImageCacheManager.Instance.GetCachedCardTexture(data);
            
            // テクスチャが有効な場合にUIに適用
            if (cardImage != null && cachedTexture != null)
            {
                cardImage.texture = cachedTexture;
                data.imageTexture = cachedTexture; // CardModelにも保存
                return true;
            }
        }
        return false;
    }
    
    // ----------------------------------------------------------------------
    // CardModelからの画像読み込み試行
    // ----------------------------------------------------------------------
    private bool TryLoadFromCardModel()
    {
        if (data.imageTexture != null && cardImage != null)
        {
            cardImage.texture = data.imageTexture;
            return true;
        }
        return false;
    }
    
    // ----------------------------------------------------------------------
    // 画像がない場合の処理
    // ----------------------------------------------------------------------
    private void HandleMissingTexture()
    {
        if (cardImage != null)
        {
            SetPlaceholderImage();
            LoadImageAsync();
        }
    }

    // ----------------------------------------------------------------------
    // 画像を非同期で読み込み、完了後ステータスを非表示化
    // ----------------------------------------------------------------------
    private async void LoadImageAsync()
    {
        // 重複読み込み防止チェック
        if (data == null || isImageLoading) return;
        
        // 読み込み状態フラグをセット
        isImageLoading = true;
        
        try
        {
            // ImageCacheManagerを使用して非同期で画像を読み込み
            Texture2D texture = await ImageCacheManager.Instance.GetCardTextureAsync(data);
            
            // UI要素の生存確認（非同期処理中にオブジェクトが破棄される可能性）
            if (cardImage != null && texture != null)
            {
                cardImage.texture = texture;
                data.imageTexture = texture; // 次回アクセス用にキャッシュ
            }
        }
        catch (System.Exception)
        {
            // エラー時のフォールバック処理
            HandleImageLoadError();
        }
        finally
        {
            // 読み込み状態フラグをリセット（例外発生時も確実に実行）
            isImageLoading = false;
        }
    }
    
    // ----------------------------------------------------------------------
    // 画像読み込みエラー時の処理
    // ----------------------------------------------------------------------
    private void HandleImageLoadError()
    {
        if (cardImage != null)
        {
            if (ImageCacheManager.Instance != null)
            {
                cardImage.texture = ImageCacheManager.Instance.GetDefaultTexture();
            }
            else
            {
                SetPlaceholderImage();
            }
        }
    }

    // ----------------------------------------------------------------------
    // プレースホルダー画像を設定
    // デフォルトのグレーテクスチャを生成
    // ----------------------------------------------------------------------
    private void SetPlaceholderImage()
    {
        if (TrySetDefaultTexture())
        {
            return;
        }
        
        CreateAndSetGrayTexture();
    }
    
    // ----------------------------------------------------------------------
    // デフォルトテクスチャの設定試行
    // ----------------------------------------------------------------------
    private bool TrySetDefaultTexture()
    {
        if (ImageCacheManager.Instance != null && ImageCacheManager.Instance.GetDefaultTexture() != null)
        {
            cardImage.texture = ImageCacheManager.Instance.GetDefaultTexture();
            return true;
        }
        return false;
    }
    
    // ----------------------------------------------------------------------
    // グレーテクスチャの生成と設定
    // ----------------------------------------------------------------------
    private void CreateAndSetGrayTexture()
    {
        // 最小サイズのテクスチャを作成（メモリ効率重視）
        // 2x2ピクセルで十分（UIでスケーリングされるため）
        var texture = new Texture2D(Constants.PLACEHOLDER_TEXTURE_SIZE, Constants.PLACEHOLDER_TEXTURE_SIZE);
        
        // 4ピクセル分の色配列を準備（2x2=4ピクセル）
        Color32[] colors = new Color32[4];
        for (int i = 0; i < 4; i++)
        {
            // 全ピクセルを同一のグレー色で塗りつぶし
            // Color32は8bit（0-255）形式でメモリ効率が良い
            colors[i] = new Color32(Constants.PLACEHOLDER_GRAY_VALUE, Constants.PLACEHOLDER_GRAY_VALUE, 
                                   Constants.PLACEHOLDER_GRAY_VALUE, Constants.PLACEHOLDER_ALPHA_VALUE);
        }
        
        // テクスチャに色を適用してGPUに送信
        // SetPixels32() → Apply() の順序が重要
        texture.SetPixels32(colors);
        texture.Apply(); // GPU側メモリに反映
        
        // UIコンポーネントに設定
        cardImage.texture = texture;
    }
    
    // ----------------------------------------------------------------------
    // クリックイベント処理 - ダブルクリックを検出してデッキに追加
    // ----------------------------------------------------------------------
    public void OnPointerClick(PointerEventData eventData)
    {
        // 前回クリックからの経過時間を計算（Unity標準の時間システムを使用）
        float timeSinceLastClick = Time.time - lastClickTime;
        
        // ダブルクリックの判定（デフォルト0.3秒以内の連続クリック）
        // 一般的なOSのダブルクリック間隔に合わせた設定
        if (timeSinceLastClick < Constants.DOUBLE_CLICK_THRESHOLD_SECONDS)
        {
            // デッキ追加処理を実行（バリデーション付き）
            AddCardToDeck();
        }
        
        // 今回のクリック時刻を記録（次回のダブルクリック判定用）
        // Time.time は起動からの経過時間（秒）
        lastClickTime = Time.time;
    }
    
    // ----------------------------------------------------------------------
    // デッキにカードを追加する処理
    // ----------------------------------------------------------------------
    private void AddCardToDeck()
    {
        // 1. 基本的な前提条件をチェック（null チェック等）
        if (!ValidateBasicRequirements())
            return;

        // 2. CardDatabaseにカードを事前登録（参照整合性確保）
        RegisterCardToDatabase();

        // 3. デッキ追加可能性をバリデーション（上限チェック等）
        var validationResult = ValidateCardAddition();
        if (!validationResult.IsValid)
        {
            // バリデーション失敗時はエラーメッセージを表示して終了
            ShowFailureFeedback(validationResult.ErrorMessage);
            return;
        }

        // 4. 実際のデッキ追加処理を実行
        ExecuteCardAddition();
    }

    // ----------------------------------------------------------------------
    // 基本要件のバリデーション
    // ----------------------------------------------------------------------
    private bool ValidateBasicRequirements()
    {
        return data != null && DeckManager.Instance != null;
    }

    // ----------------------------------------------------------------------
    // CardDatabaseへの登録
    // ----------------------------------------------------------------------
    private void RegisterCardToDatabase()
    {
        if (CardDatabase.Instance != null)
        {
            CardDatabase.Instance.RegisterCard(data);
        }
    }

    // ----------------------------------------------------------------------
    // カード追加のバリデーション
    // ----------------------------------------------------------------------
    private (bool IsValid, string ErrorMessage) ValidateCardAddition()
    {
        // 同名カード上限チェック（ポケモンカードゲームのルール準拠）
        if (!string.IsNullOrEmpty(data.name))
        {
            // 現在のデッキ内の同名カード数を取得
            // GetSameNameCardCount() は大文字小文字を区別しない比較を実行
            int sameNameCount = DeckManager.Instance.CurrentDeck.GetSameNameCardCount(data.name);
            
            // 上限に達している場合はエラー（通常は4枚まで）
            if (sameNameCount >= DeckModel.MAX_SAME_NAME_CARDS)
            {
                return (false, $"{Constants.SAME_CARD_LIMIT_TEXT}（{DeckModel.MAX_SAME_NAME_CARDS}枚）");
            }
        }

        // デッキ総枚数上限チェック（標準60枚 + 予備4枚 = 64枚）
        // 予備枠は編集時の一時的な状態を許容するためのバッファ
        if (DeckManager.Instance.CurrentDeck.CardCount >= DeckModel.MAX_CARDS + 4)
        {
            return (false, Constants.DECK_FULL_MESSAGE);
        }

        // すべてのバリデーションを通過
        return (true, string.Empty);
    }

    // ----------------------------------------------------------------------
    // カード追加の実行
    // ----------------------------------------------------------------------
    private void ExecuteCardAddition()
    {
        // カードがCardDatabaseに存在するか確認・登録
        // データベース整合性を保つため事前に登録処理を実行
        CardModel dbCard = EnsureCardInDatabase();

        // デッキに追加実行（内部でDeckModelの制約チェックも実行される）
        bool success = DeckManager.Instance.CurrentDeck.AddCard(data);

        // 結果に応じてフィードバック表示
        // ユーザーに操作結果を視覚的に通知
        if (success)
        {
            ShowAdditionSuccessFeedback();
        }
        else
        {
            ShowAdditionFailureFeedback();
        }
    }

    // ----------------------------------------------------------------------
    // カードがデータベースに存在することを保証
    // ----------------------------------------------------------------------
    private CardModel EnsureCardInDatabase()
    {
        if (CardDatabase.Instance == null)
            return data;

        CardModel dbCard = CardDatabase.Instance.GetCard(data.id);
        if (dbCard == null)
        {
            CardDatabase.Instance.RegisterCard(data);
            dbCard = data;
        }
        return dbCard;
    }

    // ----------------------------------------------------------------------
    // 追加成功時のフィードバック表示
    // ----------------------------------------------------------------------
    private void ShowAdditionSuccessFeedback()
    {
        int deckSize = DeckManager.Instance.CurrentDeck.CardCount;
        int maxDeckSize = DeckModel.MAX_CARDS;
        string message = string.Format(Constants.CARD_ADDED_MESSAGE_FORMAT, data.name, deckSize, maxDeckSize);
        ShowFeedback(message, true);
    }

    // ----------------------------------------------------------------------
    // 追加失敗時のフィードバック表示
    // ----------------------------------------------------------------------
    private void ShowAdditionFailureFeedback()
    {
        string failureReason = DetermineFailureReason();
        ShowFeedback(failureReason, false);
    }

    // ----------------------------------------------------------------------
    // 失敗理由の特定
    // ----------------------------------------------------------------------
    private string DetermineFailureReason()
    {
        // デッキ総枚数制限チェック（最高優先度）
        if (DeckManager.Instance.CurrentDeck.CardCount >= DeckModel.MAX_CARDS + 4)
        {
            return Constants.DECK_FULL_REASON;
        }
        
        // 同名カード制限チェック（ポケモンカードの基本ルール）
        if (DeckManager.Instance.CurrentDeck.GetSameNameCardCount(data.name) >= DeckModel.MAX_SAME_NAME_CARDS)
        {
            return string.Format(Constants.SAME_NAME_LIMIT_REASON_FORMAT, DeckModel.MAX_SAME_NAME_CARDS);
        }
        
        // 上記以外の原因による失敗（システムエラー等）
        return Constants.ADDITION_FAILED_TEXT;
    }

    // ----------------------------------------------------------------------
    // 初期手札表示のオンオフを切り替える
    // ----------------------------------------------------------------------
    public void ToggleInitialHandDisplay(bool isVisible)
    {
        if (initialHandSign != null)
        {
            initialHandSign.gameObject.SetActive(isVisible);
        }
    }
    
    // ----------------------------------------------------------------------
    // フィードバック表示の統合メソッド
    // ----------------------------------------------------------------------
    private void ShowFeedback(string message, bool isSuccess)
    {
        if (FeedbackContainer.Instance != null)
        {
            if (isSuccess)
            {
                FeedbackContainer.Instance.ShowSuccessFeedback(message);
            }
            else
            {
                FeedbackContainer.Instance.ShowFailureFeedback(message);
            }
        }
    }
    
    // ----------------------------------------------------------------------
    // 成功フィードバックメッセージを表示（レガシー互換性）
    // ----------------------------------------------------------------------
    private void ShowSuccessFeedback(string message)
    {
        ShowFeedback(message, true);
    }
    
    // ----------------------------------------------------------------------
    // 失敗フィードバックメッセージを表示（レガシー互換性）
    // ----------------------------------------------------------------------
    private void ShowFailureFeedback(string message)
    {
        ShowFeedback(message, false);
    }
}