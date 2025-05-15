using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class UniTaskExtensions
{
    /// <summary>
    /// UniTaskをCoroutineに変換するための拡張メソッド
    /// </summary>
    /// <typeparam name="T">UniTaskの戻り値の型</typeparam>
    /// <param name="task">変換対象のUniTask</param>
    /// <returns>コルーチンとして実行可能なIEnumerator</returns>
    public static IEnumerator ToCoroutine<T>(this UniTask<T> task)
    {
        bool completed = false;
        T result = default;
        
        task.ContinueWith(x => 
        {
            result = x;
            completed = true; 
        }).Forget();
        
        return new WaitUntil(() => completed);
    }
    
    /// <summary>
    /// UniTaskをCoroutineに変換するための拡張メソッド (戻り値なしバージョン)
    /// </summary>
    /// <param name="task">変換対象のUniTask</param>
    /// <returns>コルーチンとして実行可能なIEnumerator</returns>
    public static IEnumerator ToCoroutine(this UniTask task)
    {
        bool completed = false;
        
        task.ContinueWith(() => 
        { 
            completed = true; 
        }).Forget();
        
        return new WaitUntil(() => completed);
    }
}
