using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

