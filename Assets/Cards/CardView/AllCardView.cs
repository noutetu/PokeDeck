using UnityEngine;
using UniRx;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Text;  // 追加: ひらがな・カタカナ変換用

// ----------------------------------------------------------------------
// 複数カードを並べて表示するView（横スクロール）
// Presenterからのデータを受け取り、UIに反映する
// また、検索結果の表示も処理する
// ----------------------------------------------------------------------
public class AllCardView : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Inspector上で設定するコンポーネント
    // ----------------------------------------------------------------------
    [SerializeField] private GameObject cardPrefab;   // カード表示用のプレハブ
    [SerializeField] private Transform contentParent; // カードを配置する親オブジェクト（スクロールビューのコンテンツ領域）
    [SerializeField] private TMP_InputField searchInputField; // テキスト検索用の入力フィールド
    [SerializeField] private Button detailButton;     // 検索パネルを表示するボタン
    
    // 並べ替え用のUI要素
    [SerializeField] private TMP_Dropdown sortDropdown; // 並べ替え選択用ドロップダウン
    [SerializeField] private Button sortButton;       // 並べ替え実行ボタン

    // ----------------------------------------------------------------------
    // プライベートフィールド
    // ----------------------------------------------------------------------
    private AllCardPresenter presenter;
    private string currentSearchText = "";
    private float searchDelay = 0.1f; // 検索遅延時間（秒）
    private float lastSearchTime = 0f; // 最後に検索を実行した時間
    
    // 並べ替えの種類を定義する列挙型
    public enum SortType
    {
        ID,          // ID順（デフォルト）
        Name,        // 名前順
        Type,        // タイプ順
        HP,          // HP順（高い順）
        MaxDamage    // 最大ダメージ順（高い順）
    }
    
    private SortType currentSortType = SortType.ID; // 現在の並べ替え方法
    
    // ----------------------------------------------------------------------
    // Initialize UI
    // ----------------------------------------------------------------------
    private void Start()
    {
        // Detail ボタンがある場合は、クリックイベントを設定
        if (detailButton != null)
        {
            detailButton.onClick.AddListener(OpenSearchPanel);
        }
        
        // 検索入力フィールドの設定
        SetupSearchInputField();
        
        // 並べ替えUI要素の設定
        SetupSortUI();
    }

    // ----------------------------------------------------------------------
    // 検索入力フィールドのセットアップ
    // ----------------------------------------------------------------------
    private void SetupSearchInputField()
    {
        if (searchInputField == null)
        {
            Debug.LogWarning("⚠️ 検索入力フィールドが設定されていません");
            return;
        }

        // 検索入力フィールドの初期化
        searchInputField.text = "";
        
        // テキスト変更時のイベント
        searchInputField.onValueChanged.AddListener((text) => {
            // テキスト変更を即座に保存
            currentSearchText = text;
            // 検索をリクエスト
            RequestSearch();
        });
        
        // Enterキーを押したときの処理
        searchInputField.onEndEdit.AddListener((text) => {
            // 入力完了時に確実に検索実行
            PerformTextSearch(text);
        });
        
        // 検索ボタンの設定
        var searchIcon = searchInputField.transform.Find("Search Button");
        if (searchIcon != null && searchIcon.GetComponent<Button>() != null)
        {
            searchIcon.GetComponent<Button>().onClick.RemoveAllListeners();
            searchIcon.GetComponent<Button>().onClick.AddListener(() => {
                PerformTextSearch(searchInputField.text);
            });
        }
        
        Debug.Log("🔍 InputFieldのリスナー設定完了");
    }
    
    // ----------------------------------------------------------------------
    // 検索リクエスト - 次のフレームで検索を実行するようマーク
    // ----------------------------------------------------------------------
    private void RequestSearch()
    {
        // 次回のUpdateで検索が実行されるように、最終検索時間をリセット
        lastSearchTime = 0;
    }
    
    // ----------------------------------------------------------------------
    // 更新処理（フレーム毎）- 入力の遅延対策
    // ----------------------------------------------------------------------
    private void Update()
    {
        // 検索遅延処理
        if (lastSearchTime <= 0 && !string.IsNullOrEmpty(currentSearchText))
        {
            // 検索を実行
            PerformTextSearch(currentSearchText);
            // 最終検索時間を更新
            lastSearchTime = Time.time;
            Debug.Log($"🔍 Updateで検索実行: '{currentSearchText}'");
        }
    }

    // ひらがな・カタカナを同一視するための文字列正規化
    private string NormalizeJapanese(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            // 全角カタカナ(U+30A1〜U+30F6)をひらがなに変換
            if (ch >= '\u30A1' && ch <= '\u30F6') sb.Append((char)(ch - 0x60));
            else sb.Append(ch);
        }
        return sb.ToString().ToLowerInvariant();
    }

    // ----------------------------------------------------------------------
    // テキスト検索を実行
    // @param searchText 検索テキスト
    // ----------------------------------------------------------------------
    private void PerformTextSearch(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            // 検索テキストが空の場合は全カードを表示
            if (presenter != null)
            {
                RefreshAll(presenter.DisplayedCards);
            }
            return;
        }
        // 正規化後の検索文字列（ひらがな・カタカナを同一視、小文字化）
        var searchNorm = NormalizeJapanese(searchText);
        Debug.Log($"テキスト検索実行(正規化): '{searchNorm}'");
        // 検索対象は常に全カードデータベースから取得
        var allCards = CardDatabase.GetAllCards();
        if (allCards == null || allCards.Count == 0)
        {
            Debug.LogWarning("検索対象のカードがありません");
            return;
        }
        // フィルタリング (カード名と技の効果文のみ対象)
        var results = new List<CardModel>();
        foreach (var card in allCards)
        {
            // カード名マッチ (正規化)
            var nameNorm = NormalizeJapanese(card.name);
            if (nameNorm.Contains(searchNorm))
            {
                results.Add(card);
                continue;
            }
            
            // 技の効果文マッチ (正規化)
            if (card.moves != null)
            {
                foreach (var move in card.moves)
                {
                    var effectNorm = NormalizeJapanese(move.effect);
                    if (effectNorm.Contains(searchNorm))
                    {
                        results.Add(card);
                        break;
                    }
                }
            }
        }
        
        Debug.Log($"検索結果: {results.Count}件");
        if (SearchRouter.Instance != null)
            SearchRouter.Instance.ApplySearchResults(results);
        else
            RefreshAll(new ReactiveCollection<CardModel>(results));
    }

    // ----------------------------------------------------------------------
    // Presenterとの接続設定
    // UniRxを使用してReactiveなデータバインディングを行う
    // @param presenter 接続するPresenter
    // ----------------------------------------------------------------------
    public void BindPresenter(AllCardPresenter presenter)
    {
        this.presenter = presenter;
        
        // Presenterの読み込み完了イベントを購読
        // カードデータが更新されたら表示を更新する
        presenter.OnLoadComplete
            .Subscribe(_ => RefreshAll(presenter.DisplayedCards))
            .AddTo(this); // このコンポーネントが破棄されたら自動的に購読解除
    }

    // ----------------------------------------------------------------------
    // 全カードの表示を更新する
    // 既存のカードをクリアし、新しいカードを追加する
    // @param cards 表示するカードのコレクション
    // ----------------------------------------------------------------------
    private void RefreshAll(ReactiveCollection<CardModel> cards)
    {
        Debug.Log($"カード表示を更新します: {cards.Count}枚");

        // 既存のカードを全て削除
        foreach (Transform child in contentParent)
        {
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // 新しいカードを追加
        foreach (var card in cards)
        {
            AddCard(card);
        }
        
        Debug.Log($"カード表示を完了しました: {cards.Count}枚");
    }

    // ----------------------------------------------------------------------
    // 個別のカードを追加する
    // カードプレハブをインスタンス化し、データを設定する
    // @param card 追加するカードのデータ
    // ----------------------------------------------------------------------
    private void AddCard(CardModel card)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("❌ cardPrefabがnullです");
            return;
        }

        // カードプレハブのインスタンスを生成
        var go = Instantiate(cardPrefab, contentParent);
        
        // CardViewを使わず、直接コンポーネントにアクセス
        var rawImage = go.GetComponentInChildren<RawImage>();
        var nameText = go.GetComponentInChildren<TMP_Text>();
        
        if (rawImage != null && card != null)
        {
            // DeckListItemと同様に直接imageTextureを使用
            if (card.imageTexture != null)
            {
                // 直接CardModelのimageTextureを参照
                rawImage.texture = card.imageTexture;
            }
            else if (ImageCacheManager.Instance != null)
            {
                // imageTextureがnullの場合はデフォルト画像を表示
                rawImage.texture = ImageCacheManager.Instance.GetDefaultTexture();
            }
        }
        
        // カード名の設定
        if (nameText != null && card != null)
        {
            nameText.text = card.name;
        }
        
        // カードオブジェクトにカードデータを紐付け（クリックイベントなどで使用）
        go.name = $"Card_{card.id}_{card.name}";
        
        // CardViewコンポーネントが存在する場合は、データ連携のために設定も行う
        var cardView = go.GetComponent<CardView>();
        if (cardView != null)
        {
            cardView.Setup(card);
        }
    }
    
    // ----------------------------------------------------------------------
    // 検索パネルを開く
    // ----------------------------------------------------------------------
    private void OpenSearchPanel()
    {
        if (SearchRouter.Instance != null)
        {
            SearchRouter.Instance.ShowSearchPanel();
        }
    }
    
    // ----------------------------------------------------------------------
    // 検索結果をカードリストに適用する
    // @param searchResults 検索結果のカードデータリスト
    // ----------------------------------------------------------------------
    public void ApplySearchResults(List<CardModel> searchResults)
    {
        Debug.Log($"🔍 [AllCardView] 検索結果を受信しました: {searchResults?.Count ?? 0}枚のカード");
        
        // 検索結果の内容をサンプル表示（最初の数枚）
        if (searchResults != null && searchResults.Count > 0)
        {
            Debug.Log("🔍 [AllCardView] 検索結果サンプル (最大3枚):");
            for (int i = 0; i < Mathf.Min(3, searchResults.Count); i++)
            {
                var card = searchResults[i];
                Debug.Log($"🔍 [AllCardView] カード{i+1}: ID={card.id}, 名前={card.name}, タイプ={card.cardTypeEnum}, HP={card.hp}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ [AllCardView] 検索結果が0件、またはnullです");
            if (presenter != null)
            {
                // 空の検索結果の場合、表示をクリア
                presenter.ClearCards();
                Debug.Log("🧹 [AllCardView] 検索結果が空のため表示をクリアしました");
            }
            return;
        }
        
        try
        {
            // ReactiveCollectionに変換して表示更新
            var reactiveCards = new ReactiveCollection<CardModel>(searchResults);
            
            if (presenter != null)
            {
                // 既存の表示をクリアしてから新しい検索結果を表示
                presenter.ClearCards();
                RefreshAll(reactiveCards);
                Debug.Log("✅ [AllCardView] 検索結果の表示を更新しました");
            }
            else
            {
                Debug.LogError("❌ [AllCardView] presenterがnullのため検索結果を表示できません");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ [AllCardView] 検索結果の表示中にエラーが発生しました: {ex.Message}");
        }
    }
    
    // ----------------------------------------------------------------------
    // 並べ替えUI要素のセットアップ
    // ----------------------------------------------------------------------
    private void SetupSortUI()
    {
        // ドロップダウンが設定されていない場合はスキップ
        if (sortDropdown == null)
        {
            Debug.LogWarning("⚠️ 並べ替えドロップダウンが設定されていません");
            return;
        }

        // ドロップダウンの選択肢をクリア
        sortDropdown.ClearOptions();
        
        // 並べ替え選択肢の追加
        List<string> options = new List<string>
        {
            "ID順",
            "名前順",
            "タイプ順",
            "HP順（高い順）",
            "最大ダメージ順（高い順）"
        };
        
        sortDropdown.AddOptions(options);
        
        // デフォルト選択値を設定
        sortDropdown.value = (int)currentSortType;
        
        // 選択変更時のイベント
        sortDropdown.onValueChanged.AddListener((index) => {
            currentSortType = (SortType)index;
        });
        
        // 並べ替えボタンのイベント
        if (sortButton != null)
        {
            sortButton.onClick.AddListener(ApplySort);
        }
        
        Debug.Log("🔄 並べ替えUIの設定完了");
    }
    
    // ----------------------------------------------------------------------
    // 現在選択されている方法でカードを並べ替え
    // ----------------------------------------------------------------------
    private void ApplySort()
    {
        // 現在表示中のカードがなければ何もしない
        if (presenter == null || presenter.DisplayedCards.Count == 0)
        {
            return;
        }
        
        // 現在表示中のカードをリストにコピー
        var cardList = new List<CardModel>(presenter.DisplayedCards);
        
        // 選択された方法で並べ替え
        switch (currentSortType)
        {
            case SortType.ID:
                // ID順（整数順）- id型がint型に変更されたため直接比較
                cardList.Sort((a, b) => a.id.CompareTo(b.id));
                break;
                
            case SortType.Name:
                // 名前順（昇順）
                cardList.Sort((a, b) => string.Compare(a.name, b.name));
                break;
                
            case SortType.Type:
                // タイプ順
                cardList.Sort((a, b) => {
                    // まずポケモンタイプで並べ替え
                    int typeCompare = a.typeEnum.CompareTo(b.typeEnum);
                    if (typeCompare != 0) return typeCompare;
                    
                    // タイプが同じなら名前で並べ替え
                    return string.Compare(a.name, b.name);
                });
                break;
                
            case SortType.HP:
                // HP順（降順）
                cardList.Sort((a, b) => b.hp.CompareTo(a.hp));
                break;
                
            case SortType.MaxDamage:
                // 最大ダメージ順（降順）
                cardList.Sort((a, b) => b.maxDamage.CompareTo(a.maxDamage));
                break;
        }
        
        Debug.Log($"🔄 カードを {currentSortType} で並べ替えました");
        
        // 並べ替えた結果を表示
        var reactiveCards = new ReactiveCollection<CardModel>(cardList);
        RefreshAll(reactiveCards);
    }
    
    // ----------------------------------------------------------------------
    // コンポーネント破棄時の処理
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        // ボタンのリスナーを解除
        if (detailButton != null)
        {
            detailButton.onClick.RemoveListener(OpenSearchPanel);
        }
        
        // 検索入力フィールドのリスナーを解除
        if (searchInputField != null)
        {
            searchInputField.onEndEdit.RemoveAllListeners();
            searchInputField.onValueChanged.RemoveAllListeners();
            
            var searchIcon = searchInputField.transform.Find("Search Button");
            if (searchIcon != null && searchIcon.GetComponent<Button>() != null)
            {
                searchIcon.GetComponent<Button>().onClick.RemoveAllListeners();
            }
        }
    }
}
