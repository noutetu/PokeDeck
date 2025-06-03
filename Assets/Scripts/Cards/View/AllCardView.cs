using UnityEngine;
using UniRx;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;  // LINQ拡張メソッド用
using TMPro;        // TextMeshPro用

// ----------------------------------------------------------------------
// 複数カードを並べて表示するView（縦スクロール）
// Presenterからのデータを受け取り、UIに反映する
// ----------------------------------------------------------------------
public class AllCardView : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Inspector上で設定するコンポーネント
    // ----------------------------------------------------------------------
    [SerializeField] private GameObject cardPrefab;   // カード表示用のプレハブ
    [SerializeField] private Transform contentParent; // カードを配置する親オブジェクト（スクロールビューのコンテンツ領域）
    [SerializeField] private Button showFilterButton;     // フィルタリングパネルを表示するボタン
    [SerializeField] private SimpleVirtualScroll virtualScroll; // スクロールビューのコンポーネント
    [SerializeField] private TMP_InputField searchInputField; // テキスト検索用の入力フィールド

    // ----------------------------------------------------------------------
    // プライベートフィールド
    // ----------------------------------------------------------------------
    private AllCardPresenter presenter;
    private SearchModel searchModel; // SearchModelへの参照

    // ----------------------------------------------------------------------
    // UIの初期化処理
    // ここでカードプレハブや親オブジェクトの設定を行う
    // 仮想スクロールの初期化も行う
    // 既存のカードをクリーンアップしてから新しいカードを追加する
    // ----------------------------------------------------------------------
    private void Start()
    {
        // まず既存のカードをすべて削除して確実にクリーンな状態にする
        foreach (Transform child in contentParent)
        {
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
        }
        
        // フィルタリング表示ボタンがある場合は、クリックイベントを設定
        if (showFilterButton != null)
        {
            showFilterButton.onClick.AddListener(OpenSearchPanel);
        }

        // 仮想スクロールが設定されているか確認
        if (virtualScroll == null)
        {
            // エディタで設定されていない場合は、同じGameObjectについているコンポーネントを探す
            virtualScroll = GetComponent<SimpleVirtualScroll>();
        }
        
        // SearchModelのインスタンスを取得
        InitializeSearchModel();
    }
    
    // ----------------------------------------------------------------------
    // SearchModelの初期化
    // ----------------------------------------------------------------------
    private void InitializeSearchModel()
    {
        // シングルトンインスタンスを取得
        searchModel = SearchModel.Instance;
        if (searchModel == null)
        {
            // インスタンスがない場合は探す
            searchModel = FindFirstObjectByType<SearchModel>();
            if (searchModel == null)
            {
                return;
            }
        }
        
        // テキスト検索用の入力フィールドがある場合、SearchModelと連携
        SetupSearchInputField();
    }
    
    // ----------------------------------------------------------------------
    // 検索入力フィールドのセットアップ
    // ----------------------------------------------------------------------
    private void SetupSearchInputField()
    {
        if (searchInputField == null || searchModel == null)
        {
            return;
        }

        // 検索入力フィールドのイベントを設定
        searchInputField.onValueChanged.AddListener((text) =>
        {
            // SearchModelに検索テキストを設定
            searchModel.SetSearchText(text);
        });

        // Enterキーを押したときの処理
        searchInputField.onEndEdit.AddListener((text) =>
        {
            // 検索実行
            // searchModel.PerformTextSearch(text); // 古い呼び出しをコメントアウト
            searchModel.ExecuteSearchAndFilters(); // 新しい呼び出しに変更
        });
        
        // 検索ボタンの設定
        var searchIcon = searchInputField.transform.Find("Search Button");
        if (searchIcon != null && searchIcon.GetComponent<Button>() != null)
        {
            searchIcon.GetComponent<Button>().onClick.RemoveAllListeners();
            searchIcon.GetComponent<Button>().onClick.AddListener(() =>
            {
                // searchModel.PerformTextSearch(searchInputField.text); // 古い呼び出しをコメントアウト
                searchModel.ExecuteSearchAndFilters(); // 新しい呼び出しに変更
            });
        }
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
            .Subscribe(_ => {
                RefreshAll(presenter.DisplayedCards);
                InitializeVirtualScroll(); // 仮想スクロールも初期化
            })
            .AddTo(this); // このコンポーネントが破棄されたら自動的に購読解除
            
        // SearchNavigatorの検索結果イベントを購読
        if (SearchNavigator.Instance != null)
        {
            // 既存のイベントハンドラーを削除（複数回呼ばれる可能性があるため）
            SearchNavigator.Instance.OnSearchResult -= OnSearchResultUpdated;
            // 新しいイベントハンドラーを追加
            SearchNavigator.Instance.OnSearchResult += OnSearchResultUpdated;
        }
    }
    
    // ----------------------------------------------------------------------
    // 検索結果が更新されたときの処理
    // @param cards 検索結果のカードリスト
    // ----------------------------------------------------------------------
    private void OnSearchResultUpdated(List<CardModel> cards)
    {
        if (presenter != null && cards != null)
        {
            // 検索結果をプレゼンターに設定して表示を更新
            presenter.UpdateDisplayedCards(cards);
            RefreshAll(presenter.DisplayedCards);
            InitializeVirtualScroll();
        }
    }

    // ----------------------------------------------------------------------
    // 仮想スクロールの初期化
    // ----------------------------------------------------------------------
    private void InitializeVirtualScroll()
    {
        if (virtualScroll != null && presenter != null && presenter.DisplayedCards != null)
        {
            // PresnterのReactiveCollectionをリストに変換して渡す
            List<CardModel> currentCards = new List<CardModel>(presenter.DisplayedCards);
            virtualScroll.SetCards(currentCards);
        }
    }

    // ----------------------------------------------------------------------
    // 全カードの表示を更新する
    // 既存のカードをクリアし、新しいカードを追加する
    // @param cards 表示するカードのコレクション
    // ----------------------------------------------------------------------
    private void RefreshAll(ReactiveCollection<CardModel> cards)
    {
        if (virtualScroll != null)
        {
            List<CardModel> cardList = cards.ToList();
            virtualScroll.SetCards(cardList);
        }
    }

    // ----------------------------------------------------------------------
    // 検索パネルを開く
    // ----------------------------------------------------------------------
    private void OpenSearchPanel()
    {
        if (SearchNavigator.Instance != null)
        {
            SearchNavigator.Instance.ShowSearchPanel();
        }
    }
    
    // ----------------------------------------------------------------------
    // コンポーネント破棄時の処理
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        // ボタンのリスナーを解除
        if (showFilterButton != null)
        {
            showFilterButton.onClick.RemoveListener(OpenSearchPanel);
        }
        
        // 検索入力フィールドのリスナーを解除
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.RemoveAllListeners();
            searchInputField.onEndEdit.RemoveAllListeners();
            
            // 検索ボタンのリスナーも解除
            var searchIcon = searchInputField.transform.Find("Search Button");
            if (searchIcon != null && searchIcon.GetComponent<Button>() != null)
            {
                searchIcon.GetComponent<Button>().onClick.RemoveAllListeners();
            }
        }
        
        // SearchNavigatorのイベント購読解除
        if (SearchNavigator.Instance != null)
        {
            SearchNavigator.Instance.OnSearchResult -= OnSearchResultUpdated;
        }
    }
}