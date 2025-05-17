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

        // 起動時にDeckManagerにデッキパネルの参照を設定
        if (deckPanel != null)
        {
            DeckManager.Instance.SetDeckPanel(deckPanel);
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

            // DeckManagerにも状態を伝える
            DeckManager.Instance.HideDeckPanel();

            return; // 処理を終了
        }

        // 通常のデッキパネル表示切替
        DeckManager.Instance.ToggleDeckPanel();
    }
}