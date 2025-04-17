using UnityEngine;
using System;

public static class EnumHelper
{
    public static Type ParseType(string str)
    {
        if (Enum.TryParse<Type>(str, out var result))
        {
            return result;
        }
        else
        {
            Debug.LogWarning($"❌ タイプの変換に失敗しました: {str}");
            return Type.無色; // デフォルトで安全な値
        }
    }
}

