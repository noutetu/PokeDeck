using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PokeDeck
{
    [RequireComponent(typeof(LeTai.TrueShadow.TrueShadowInteractionAnimation))]
    public class CardDoubleClickHandler : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("ダブルクリックと判定される時間間隔（秒）")]
        [SerializeField] private float doubleClickTimeThreshold = 0.5f;
        
        [Header("イベント")]
        [Tooltip("カードが選択された時に呼び出されるイベント")]
        [SerializeField] private UnityEvent onCardSelected;
        
        [Tooltip("カードがデッキに追加される時に呼び出されるイベント")]
        [SerializeField] private UnityEvent onCardAddedToDeck;
        
        // TrueShadowInteractionAnimationへの参照
        private LeTai.TrueShadow.TrueShadowInteractionAnimation shadowAnimation;
        
        // 最後にクリックした時刻
        private float lastClickTime;
        
        // カードが選択状態かどうか
        private bool isCardSelected = false;
        
        // デバッグモード
        [Header("デバッグ")]
        [SerializeField] private bool debugMode = false;
        
        private void Awake()
        {
            // TrueShadowInteractionAnimationコンポーネントの参照を取得
            shadowAnimation = GetComponent<LeTai.TrueShadow.TrueShadowInteractionAnimation>();
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            // 現在の時刻を取得
            float currentTime = Time.time;
            
            if (isCardSelected)
            {
                // カードが既に選択されている場合
                if (currentTime - lastClickTime < doubleClickTimeThreshold)
                {
                    // ダブルクリック時間内なら、デッキに追加
                    AddCardToDeck();
                }
                else
                {
                    // 時間経過後のクリックは再選択として扱う
                    SelectCard();
                }
            }
            else
            {
                // カードが選択されていない場合は選択する
                SelectCard();
            }
            
            // 最後のクリック時刻を更新
            lastClickTime = currentTime;
        }
        
        // カードを選択状態にする
        private void SelectCard()
        {
            isCardSelected = true;
            
            // 選択イベントを発火
            onCardSelected?.Invoke();
            
            if (debugMode)
                Debug.Log($"カード「{gameObject.name}」が選択されました");
        }
        
        // カードをデッキに追加する
        private void AddCardToDeck()
        {
            // デッキ追加イベントを発火
            onCardAddedToDeck?.Invoke();
            
            if (debugMode)
                Debug.Log($"カード「{gameObject.name}」がデッキに追加されました");
            
            // 選択状態をリセット
            ResetCardState();
        }
        
        // カードの状態をリセット（公開メソッド）
        public void ResetCardState()
        {
            isCardSelected = false;
            
            if (debugMode)
                Debug.Log($"カード「{gameObject.name}」の選択状態がリセットされました");
        }
        
        // 現在の選択状態を取得（他のスクリプトから参照用）
        public bool IsSelected()
        {
            return isCardSelected;
        }
        
        // プログラムから強制的に選択状態にする
        public void ForceSelect()
        {
            SelectCard();
        }
        
        // プログラムから強制的にデッキに追加する
        public void ForceAddToDeck()
        {
            AddCardToDeck();
        }
    }
}