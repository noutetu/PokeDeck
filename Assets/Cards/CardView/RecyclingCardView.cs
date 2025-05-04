// using UnityEngine;
// using UnityEngine.UI;
// using System.Collections.Generic;
// using System.Linq;
// using UniRx;
// using Cysharp.Threading.Tasks;

// // ----------------------------------------------------------------------
// // リサイクルビューの実装（効率的なカードリスト表示）
// // 大量のカードを少数のGameObjectで表示するためのコンポーネント
// // ----------------------------------------------------------------------
// public class RecyclingCardView : MonoBehaviour
// {
//     [Header("コンポーネント参照")]
//     [SerializeField] private GameObject cardPrefab;      // カードのプレハブ
//     [SerializeField] private RectTransform contentRect;  // ScrollViewのコンテンツ領域

//     [Header("レイアウト設定")]
//     [SerializeField] private float cardWidth = 200f;     // カードの幅
//     [SerializeField] private float cardHeight = 280f;    // カードの高さ
//     [SerializeField] private float spacingX = 20f;       // カード間の横方向の間隔
//     [SerializeField] private float spacingY = 20f;       // カード間の縦方向の間隔
//     [SerializeField] private float paddingLeft = 20f;    // 左側の余白
//     [SerializeField] private float paddingTop = 20f;     // 上側の余白
//     [SerializeField] private int columnCount = 3;        // 1行あたりのカード数
    
//     [Header("パフォーマンス設定")]
//     [SerializeField] private int bufferItemCount = 6;    // 画面外に確保する余分なカード数
//     [SerializeField] private bool forceMemoryCache = true; // 表示カードを優先的にメモリキャッシュに保持
    
//     [Header("デバッグ")]
//     [SerializeField] private bool showDebugInfo = false; // デバッグ情報を表示するか

//     // 内部データ
//     private List<CardModel> allCardData = new List<CardModel>();  // 全カードデータ参照
//     private List<CardItem> pooledItems = new List<CardItem>();    // 再利用するカードオブジェクト
//     private ScrollRect scrollRect;                                // スクロール制御用
//     private int firstVisibleIndex = 0;                            // 表示中の最初のカードのインデックス
//     private RectTransform viewportRect;                           // Viewportの参照
//     private int visibleRowCount = 0;                              // 画面内に表示可能な行数
//     private int totalVisibleItems = 0;                            // バッファ含む表示可能アイテム数
//     private bool initialized = false;                             // 初期化済みフラグ

//     // ----------------------------------------------------------------------
//     // 初期化処理
//     // ----------------------------------------------------------------------
//     private void Awake()
//     {
//         // ScrollRectの参照を取得
//         scrollRect = GetComponentInParent<ScrollRect>();
//         if (scrollRect == null)
//         {
//             Debug.LogError("❌ RecyclingCardView: ScrollRectコンポーネントが見つかりません");
//             return;
//         }

//         // コンテンツ領域の参照を取得
//         if (contentRect == null)
//         {
//             contentRect = scrollRect.content;
//         }

//         // ビューポート領域の参照を取得
//         viewportRect = scrollRect.viewport;
//         if (viewportRect == null)
//         {
//             viewportRect = scrollRect.transform.parent.GetComponent<RectTransform>();
//         }

//         // スクロールイベントのリスナーを登録
//         scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
//     }

//     // ----------------------------------------------------------------------
//     // 後処理
//     // ----------------------------------------------------------------------
//     private void OnDestroy()
//     {
//         if (scrollRect != null)
//         {
//             scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
//         }
        
//         // プールアイテムのクリーンアップ
//         ClearPool();
//     }

//     // ----------------------------------------------------------------------
//     // リサイクルビューを初期化
//     // ----------------------------------------------------------------------
//     private void Initialize()
//     {
//         if (initialized)
//             return;
            
//         if (scrollRect == null)
//         {
//             Awake(); // コンポーネント参照を取得
//             if (scrollRect == null)
//                 return;
//         }
        
//         // 表示可能な行数を計算
//         CalculateVisibleItemCount();
        
//         // オブジェクトプールを初期化
//         InitializePool();
        
//         initialized = true;
        
//         if (showDebugInfo)
//         {
//             Debug.Log($"🔄 RecyclingCardView: 初期化完了 - " +
//                       $"表示行数: {visibleRowCount}, バッファ含む表示アイテム数: {totalVisibleItems}");
//         }
//     }

//     // ----------------------------------------------------------------------
//     // 表示可能なアイテム数を計算
//     // ----------------------------------------------------------------------
//     private void CalculateVisibleItemCount()
//     {
//         // ビューポートの高さ
//         float viewportHeight = viewportRect.rect.height;
        
//         // 1行の高さ
//         float rowHeight = cardHeight + spacingY;
        
//         // 表示可能な行数（切り上げ）
//         visibleRowCount = Mathf.CeilToInt(viewportHeight / rowHeight) + 1;
        
//         // バッファを含む表示行数
//         int bufferRows = Mathf.CeilToInt((float)bufferItemCount / columnCount);
//         int totalRows = visibleRowCount + bufferRows;
        
//         // 総表示アイテム数
//         totalVisibleItems = totalRows * columnCount;
        
//         if (showDebugInfo)
//         {
//             Debug.Log($"📏 RecyclingCardView: 表示計算 - " +
//                       $"ビューポート高さ: {viewportHeight}, 行高さ: {rowHeight}, " +
//                       $"表示行数: {visibleRowCount}, バッファ行数: {bufferRows}");
//         }
//     }

//     // ----------------------------------------------------------------------
//     // オブジェクトプールの初期化
//     // ----------------------------------------------------------------------
//     private void InitializePool()
//     {
//         // 既存のプールをクリア
//         ClearPool();
        
//         // 必要な数のカードを作成
//         int itemCount = Mathf.Min(totalVisibleItems, allCardData.Count);
        
//         for (int i = 0; i < itemCount; i++)
//         {
//             CreatePoolItem();
//         }
        
//         if (showDebugInfo)
//         {
//             Debug.Log($"🏊 RecyclingCardView: プール初期化 - {itemCount}個のカードアイテムを作成");
//         }
//     }

//     // ----------------------------------------------------------------------
//     // プールアイテムを1つ作成
//     // ----------------------------------------------------------------------
//     private CardItem CreatePoolItem()
//     {
//         GameObject obj = Instantiate(cardPrefab, contentRect);
//         CardItem item = obj.GetComponent<CardItem>();
        
//         if (item == null)
//         {
//             Debug.LogError("❌ RecyclingCardView: カードプレハブにCardItemコンポーネントがありません");
//             Destroy(obj);
//             return null;
//         }
        
//         // 初期状態では非表示
//         obj.SetActive(false);
        
//         pooledItems.Add(item);
//         return item;
//     }

//     // ----------------------------------------------------------------------
//     // プールアイテムのクリーンアップ
//     // ----------------------------------------------------------------------
//     private void ClearPool()
//     {
//         foreach (var item in pooledItems)
//         {
//             if (item != null)
//             {
//                 // リソースを解放
//                 ReleaseCardResources(item);
//                 Destroy(item.gameObject);
//             }
//         }
        
//         pooledItems.Clear();
//     }

//     // ----------------------------------------------------------------------
//     // カードリソースの解放
//     // ----------------------------------------------------------------------
//     private void ReleaseCardResources(CardItem card)
//     {
//         // カードコンポーネントにReleaseResourcesメソッドがあれば呼び出す
//         var releaseMethod = card.GetType().GetMethod("ReleaseResources");
//         if (releaseMethod != null)
//         {
//             releaseMethod.Invoke(card, null);
//         }
//         else
//         {
//             // 標準的なイメージコンポーネントのテクスチャを解放
//             Image image = card.GetComponentInChildren<Image>();
//             if (image != null && image.sprite != null)
//             {
//                 image.sprite = null;
//             }
//         }
//     }

//     // ----------------------------------------------------------------------
//     // 表示するカードデータを設定
//     // @param cards 表示するカードのリスト
//     // ----------------------------------------------------------------------
//     public void SetCardData(List<CardModel> cards)
//     {
//         allCardData = cards ?? new List<CardModel>();
        
//         // コンテンツサイズを更新
//         UpdateContentSize();
        
//         // まだ初期化されていなければ初期化
//         if (!initialized)
//         {
//             Initialize();
//         }
//         else if (pooledItems.Count > allCardData.Count)
//         {
//             // カード数が減った場合、余分なプールアイテムを無効化
//             for (int i = allCardData.Count; i < pooledItems.Count; i++)
//             {
//                 pooledItems[i].gameObject.SetActive(false);
//             }
//         }
//         else if (pooledItems.Count < Mathf.Min(totalVisibleItems, allCardData.Count))
//         {
//             // カード数が増えてプールが足りない場合、追加作成
//             int additionalCount = Mathf.Min(totalVisibleItems, allCardData.Count) - pooledItems.Count;
//             for (int i = 0; i < additionalCount; i++)
//             {
//                 CreatePoolItem();
//             }
//         }
        
//         // 表示位置をリセット
//         firstVisibleIndex = 0;
//         if (scrollRect != null)
//         {
//             scrollRect.normalizedPosition = new Vector2(0, 1); // 一番上にスクロール
//         }
        
//         // カードの表示を更新
//         UpdateVisibleItems();
        
//         if (showDebugInfo)
//         {
//             Debug.Log($"📋 RecyclingCardView: {allCardData.Count}枚のカードデータを設定");
//         }
//     }

//     // ----------------------------------------------------------------------
//     // コンテンツサイズを更新
//     // ----------------------------------------------------------------------
//     private void UpdateContentSize()
//     {
//         // 必要な行数を計算
//         int rowCount = Mathf.CeilToInt((float)allCardData.Count / columnCount);
        
//         // コンテンツの高さを計算
//         float contentHeight = paddingTop + (rowCount * cardHeight) + ((rowCount - 1) * spacingY) + paddingTop;
        
//         // 最低でもビューポートの高さは確保
//         if (viewportRect != null)
//         {
//             contentHeight = Mathf.Max(contentHeight, viewportRect.rect.height);
//         }
        
//         // コンテンツサイズを設定
//         contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, contentHeight);
//     }

//     // ----------------------------------------------------------------------
//     // スクロール位置が変更された時の処理
//     // ----------------------------------------------------------------------
//     private void OnScrollValueChanged(Vector2 normalizedPosition)
//     {
//         if (!initialized || allCardData.Count == 0)
//             return;
            
//         UpdateVisibleItems();
//     }

//     // ----------------------------------------------------------------------
//     // 表示アイテムの更新
//     // ----------------------------------------------------------------------
//     private void UpdateVisibleItems()
//     {
//         if (!initialized || allCardData.Count == 0 || pooledItems.Count == 0)
//             return;
            
//         // スクロール位置から開始インデックスを計算
//         float scrollPosition = 1.0f - scrollRect.normalizedPosition.y; // 0: 上、1: 下
        
//         // 総行数
//         int totalRows = Mathf.CeilToInt((float)allCardData.Count / columnCount);
        
//         // 表示開始行（スクロール位置に基づく）
//         int startRow = Mathf.FloorToInt(scrollPosition * totalRows);
        
//         // バッファを考慮して調整
//         startRow = Mathf.Max(0, startRow - bufferItemCount / columnCount);
        
//         // 表示開始インデックス
//         int newFirstVisibleIndex = startRow * columnCount;
//         newFirstVisibleIndex = Mathf.Clamp(newFirstVisibleIndex, 0, Mathf.Max(0, allCardData.Count - totalVisibleItems));
        
//         // 既に同じインデックスなら更新不要
//         if (newFirstVisibleIndex == firstVisibleIndex)
//             return;
            
//         firstVisibleIndex = newFirstVisibleIndex;
        
//         // 各プールアイテムを更新
//         for (int i = 0; i < pooledItems.Count; i++)
//         {
//             int dataIndex = firstVisibleIndex + i;
//             CardItem cardItem = pooledItems[i];
            
//             if (dataIndex < allCardData.Count)
//             {
//                 // 表示可能なデータがある
//                 CardModel cardData = allCardData[dataIndex];
                
//                 // 位置を計算
//                 int rowIndex = dataIndex / columnCount;
//                 int colIndex = dataIndex % columnCount;
                
//                 float posX = paddingLeft + (colIndex * (cardWidth + spacingX));
//                 float posY = -paddingTop - (rowIndex * (cardHeight + spacingY));
                
//                 RectTransform rt = cardItem.GetComponent<RectTransform>();
//                 rt.anchoredPosition = new Vector2(posX, posY);
//                 rt.sizeDelta = new Vector2(cardWidth, cardHeight);
                
//                 // データが異なる場合のみ更新
//                 bool needsUpdate = !cardItem.gameObject.activeSelf || 
//                                    cardItem.CurrentCard == null || 
//                                    cardItem.CurrentCard.id != cardData.id;
                                   
//                 if (needsUpdate)
//                 {
//                     // 画面内に表示されるのでテクスチャをメモリキャッシュに優先保持
//                     if (forceMemoryCache && !string.IsNullOrEmpty(cardData.imageKey))
//                     {
//                         ImageCacheManager.Instance?.LoadTextureAsync(cardData.imageKey, cardData, true).Forget();
//                     }
                    
//                     // カードデータを設定
//                     cardItem.SetCardData(cardData);
//                 }
                
//                 cardItem.gameObject.SetActive(true);
//             }
//             else
//             {
//                 // 範囲外のカードは非表示
//                 if (cardItem.gameObject.activeSelf)
//                 {
//                     ReleaseCardResources(cardItem);
//                     cardItem.gameObject.SetActive(false);
//                 }
//             }
//         }
        
//         if (showDebugInfo && Time.frameCount % 30 == 0) // フレームごとに出すと多すぎるので間引く
//         {
//             Debug.Log($"🔄 RecyclingCardView: 表示更新 - 開始:{firstVisibleIndex}, 表示数:{pooledItems.Count}");
//         }
//     }
// }