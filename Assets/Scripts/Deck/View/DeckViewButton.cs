using UnityEngine;
using UnityEngine.UI;

// ----------------------------------------------------------------------
// デッキ表示ボタンの動作を制御するクラス
// ----------------------------------------------------------------------
public class DeckViewButton : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // ボタンコンポーネント
    // ----------------------------------------------------------------------
    private Button button;

    // ----------------------------------------------------------------------
    // デッキパネルの参照（Inspector上で設定可能）
    // ----------------------------------------------------------------------
    [SerializeField] private GameObject deckPanel; // デッキパネル
    [SerializeField] private GameObject deckListPanel; // デッキ一覧パネル
    [SerializeField] private GameObject SearchPanel; // フィルターパネル
    [SerializeField] private GameObject sampleDeckPanel; // サンプルデッキパネル

    // ----------------------------------------------------------------------
    // Unityライフサイクルメソッド
    // ----------------------------------------------------------------------
    private void Awake()
    {
        // ボタンコンポーネントを取得
        button = GetComponent<Button>();

        if (button == null)
        {
            return;
        }

        // ボタンクリック時のイベントを設定
        button.onClick.AddListener(OnDeckButtonClicked);
    }

    // ----------------------------------------------------------------------
    // ボタンクリック処理
    // ----------------------------------------------------------------------
    private void OnDeckButtonClicked()
    {
        // デッキリストパネルが表示されている場合
        if (deckListPanel != null && deckListPanel.activeSelf)
        {
            // デッキリストパネルを閉じる
            deckListPanel.SetActive(false);

            // デッキパネルも閉じてカード一覧に戻る
            if (deckPanel != null)
            {
                deckPanel.SetActive(false);
            }
            // サンプルデッキパネルが表示されている場合は閉じる 
            if (sampleDeckPanel != null && sampleDeckPanel.activeSelf)
            {
                sampleDeckPanel.SetActive(false);
            }

            // DeckManagerにも状態を伝える
            DeckManager.Instance.HideDeckPanel();

            return; // 処理を終了
        }

        // デッキパネル表示を切り替え
        ToggleDeckPanel();
    }
    
    // ----------------------------------------------------------------------
    // デッキパネルの表示状態を切り替え
    // ----------------------------------------------------------------------
    public void ToggleDeckPanel()
    {
        if (deckPanel != null)
        {
            // デッキパネルの表示状態を切り替え
            deckPanel.SetActive(!deckPanel.activeSelf);


            // フィルターパネルを非表示にする
            if (SearchPanel != null)
            {
                SearchPanel.SetActive(false);
            }
            if(SearchPanel == null)
            {
            }
        }
    }
}