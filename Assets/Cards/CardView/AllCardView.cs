using UnityEngine;
using UniRx;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

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
        
        // SearchRouterが存在する場合は、検索結果イベントを購読
        if (SearchRouter.Instance != null)
        {
            SearchRouter.Instance.OnSearchResult += ApplySearchResults;
        }
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
        
        Debug.Log($"テキスト検索実行: '{searchText}'");
        
        // CardDatabaseから全カードを取得
        var allCards = CardDatabase.GetAllCards();
        if (allCards == null || allCards.Count == 0)
        {
            Debug.LogWarning("検索対象のカードがありません");
            return;
        }
        
        // 検索テキストを小文字に変換（大文字小文字区別なし）
        string searchLower = searchText.ToLower();
        
        // フィルタリング
        var results = new List<CardModel>();
        foreach (var card in allCards)
        {
            // カード名の検索
            if (card.name != null && card.name.ToLower().Contains(searchLower))
            {
                results.Add(card);
                continue;
            }
            
            // 技の効果のテキスト検索（技が存在する場合）- 技名は検索対象から除外
            if (card.moves != null)
            {
                bool found = false;
                foreach (var move in card.moves)
                {
                    // 技の効果テキストのみを検索対象とする（技名は除外）
                    if (move.effect != null && move.effect.ToLower().Contains(searchLower))
                    {
                        found = true;
                        break;
                    }
                }
                
                if (found)
                {
                    results.Add(card);
                }
            }
        }
        
        Debug.Log($"検索結果: {results.Count}件");
        
        // 検索結果を表示
        ApplySearchResults(results);
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
        // 既存のカードを全て削除
        foreach (Transform child in contentParent)
        {
            // エディットモードとプレイモードで適切な破棄メソッドを使用
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // 新しいカードを1枚ずつ追加
        foreach (var card in cards)
        {
            AddCard(card);
        }
    }

    // ----------------------------------------------------------------------
    // 個別のカードを追加する
    // カードプレハブをインスタンス化し、データを設定する
    // @param card 追加するカードのデータ
    // ----------------------------------------------------------------------
    private void AddCard(CardModel card)
    {
        // プレハブの存在チェック
        if (cardPrefab == null)
        {
            Debug.LogError("❌ cardPrefabがnullだよ！");
            return;
        }

        // プレハブをインスタンス化（親オブジェクトを指定）
        var go = Instantiate(cardPrefab, contentParent);
        var view = go.GetComponent<CardView>();

        // CardViewコンポーネントの存在チェック
        if (view == null)
        {
            Debug.LogError("❌ CardViewがプレハブにアタッチされてないよ！");
            return;
        }

        // カードデータを設定
        view.Setup(card);
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
        // ReactiveCollectionに変換して表示更新
        var reactiveCards = new ReactiveCollection<CardModel>(searchResults);
        RefreshAll(reactiveCards);
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
                // ID順（数値の場合は数値順、そうでなければ文字列順）
                cardList.Sort((a, b) => {
                    if (int.TryParse(a.id, out int idA) && int.TryParse(b.id, out int idB))
                    {
                        return idA.CompareTo(idB);
                    }
                    return string.Compare(a.id, b.id);
                });
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
        // イベント購読を解除
        if (SearchRouter.Instance != null)
        {
            SearchRouter.Instance.OnSearchResult -= ApplySearchResults;
        }
        
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
