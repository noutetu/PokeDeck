using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class TogglePanel : MonoBehaviour
{
    public GameObject target;

    public void Toggle()
    {
        target.SetActive(!target.activeSelf);
    }
}

