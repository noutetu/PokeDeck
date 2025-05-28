using UnityEngine;

// ----------------------------------------------------------------------
// トグルパネルの表示・非表示を制御するクラス
// 特に機能を持たず、単にGameObjectのactive状態を切り替える時に使用
// チュートリアルテキストの表示など
// ----------------------------------------------------------------------
public class TogglePanel : MonoBehaviour
{
    public GameObject target;

    public void Toggle()
    {
        if (target == null)
        {
            return;
        }
        target.SetActive(!target.activeSelf);
    }
}

