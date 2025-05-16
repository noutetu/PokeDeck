using UnityEngine;
using UniRx;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Text;  // 追加: ひらがな・カタカナ変換用
using System.Linq;  // LINQ拡張メソッド用

// ----------------------------------------------------------------------
// 複数カードを並べて表示するView（縦スクロール）
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
    [SerializeField] private Button showFilterButton;     // フィルタリングパネルを表示するボタン
    [SerializeField] private SimpleVirtualScroll virtualScroll; // スクロールビューのコンポーネント

    // ----------------------------------------------------------------------
    // プライベートフィールド
    // ----------------------------------------------------------------------
    private AllCardPresenter presenter;
    private string currentSearchText = "";
    private float lastSearchTime = 0f; // 最後に検索を実行した時間

    // ----------------------------------------------------------------------
    // UIの初期化処理
    // ここでカードプレハブや親オブジェクトの設定を行う
    // また、検索入力フィールドや並べ替えUIの初期化も行う
    // さらに、仮想スクロールの初期化も行う
    // 既存のカードをクリーンアップしてから新しいカードを追加する
    // これにより、UIが常に最新の状態で表示されるようにする
    // さらに、検索ボタンのイベントリスナーも設定する
    // これにより、ユーザーがボタンをクリックしたときに適切な処理が実行されるようにする
    // また、検索入力フィールドの初期化も行い、ユーザーが入力したテキストに基づいて検索を実行する
    // 検索入力フィールドのテキスト変更時やEnterキー押下時に検索を実行するように設定する
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

        // 検索入力フィールドの設定
        SetupSearchInputField();

        // 仮想スクロールが設定されているか確認
        if (virtualScroll == null)
        {
            // エディタで設定されていない場合は、同じGameObjectについているコンポーネントを探す
            virtualScroll = GetComponent<SimpleVirtualScroll>();
        }
    }

    // ----------------------------------------------------------------------
    // 検索入力フィールドのセットアップ
    // ----------------------------------------------------------------------
    private void SetupSearchInputField()
    {
        if (searchInputField == null)
        {
            return;
        }

        // 検索入力フィールドの初期化
        searchInputField.text = "";

        // テキスト変更時のイベント
        searchInputField.onValueChanged.AddListener((text) =>
        {
            // テキスト変更を即座に保存
            currentSearchText = text;
            // 検索をリクエスト
            RequestSearch();
        });

        // Enterキーを押したときの処理
        searchInputField.onEndEdit.AddListener((text) =>
        {
            // 入力完了時に確実に検索実行
            PerformTextSearch(text);
        });

        // 検索ボタンの設定
        var searchIcon = searchInputField.transform.Find("Search Button");
        if (searchIcon != null && searchIcon.GetComponent<Button>() != null)
        {
            searchIcon.GetComponent<Button>().onClick.RemoveAllListeners();
            searchIcon.GetComponent<Button>().onClick.AddListener(() =>
            {
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
        // 最終検索時間が0以下の場合、または検索テキストが空でない場合に検索を実行
        if (lastSearchTime <= 0 && !string.IsNullOrEmpty(currentSearchText))
        {
            // 検索を実行
            PerformTextSearch(currentSearchText);
            // 最終検索時間を更新
            lastSearchTime = Time.time;
        }
    }

    // ----------------------------------------------------------------------
    // ひらがな・カタカナを同一視するための文字列正規化
    // -----------------------------------------------------------------------
    private string NormalizeJapanese(string input)
    {
        // 入力がnullまたは空の場合は空文字を返す
        if (string.IsNullOrEmpty(input)) return "";
        var sb = new StringBuilder(input.Length);
        // 文字列を1文字ずつ処理
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
        string searchNorm = NormalizeJapanese(searchText);
        // 検索対象は常に全カードデータベースから取得
        List<CardModel> allCards = CardDatabase.GetAllCards();
        if (allCards == null || allCards.Count == 0)
        {
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
        // 検索結果を表示
        if (SearchNavigator.Instance != null)
            SearchNavigator.Instance.ApplySearchResults(results);
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
            .Subscribe(_ => {
                RefreshAll(presenter.DisplayedCards);
                InitializeVirtualScroll(); // 仮想スクロールも初期化
            })
            .AddTo(this); // このコンポーネントが破棄されたら自動的に購読解除
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