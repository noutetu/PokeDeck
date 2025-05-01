using System;
using System.Collections.Generic;
using UnityEngine;

// ----------------------------------------------------------------------
// すべてのフィルターエリアが実装すべきインターフェース
// 将来的に追加されるフィルターエリアの基底となる
// ----------------------------------------------------------------------
public interface IFilterArea
{
    // フィルター条件が変更された時に発火するイベント
    // OKボタンが押されたときにのみ使用する
    event Action OnFilterChanged;
    
    // OKボタンが押されたときに呼び出され、現在のフィルター状態をモデルに適用する
    void ApplyFilterToModel(SearchModel model);
    
    // フィルターをリセット
    void ResetFilters();
}