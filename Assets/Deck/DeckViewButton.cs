using UnityEngine;
using UnityEngine.UI;

// ----------------------------------------------------------------------
// デッキ表示ボタンの動作を制御するクラス
// ----------------------------------------------------------------------
public class DeckViewButton : MonoBehaviour
{
    // ボタンコンポーネント
    private Button button;
    
    // デッキパネルの参照（Inspector上で設定可能）
    [SerializeField] private GameObject deckPanel;
    [SerializeField] private GameObject deckListPanel; // デッキ一覧パネルの参照を追加

    private void Awake()
    {
        button = GetComponent<Button>();
        
        if (button == null)
        {
            Debug.LogError("ボタンコンポーネントが見つかりません");
            return;
        }
        
        // 起動時にDeckManagerにパネル参照を設定
        if (deckPanel != null)
        {
            DeckManager.Instance.SetDeckPanel(deckPanel);
        }
        
        // ボタンクリック時にデッキパネルを表示/非表示にする
        button.onClick.AddListener(OnDeckButtonClicked);
    }
    
    // ボタンクリック処理
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
    
    // Inspectorからデッキパネルの参照を更新した場合にDeckManagerにも反映
    private void OnValidate()
    {
        if (Application.isPlaying && deckPanel != null)
        {
            DeckManager.Instance.SetDeckPanel(deckPanel);
        }
    }
}