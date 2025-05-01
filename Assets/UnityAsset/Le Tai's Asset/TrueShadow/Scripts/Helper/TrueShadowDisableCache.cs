// Copyright (c) Le Loc Tai <leloctai.com> . All rights reserved. Do not redistribute.

using LeTai.TrueShadow.PluginInterfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LeTai.TrueShadow
{
[AddComponentMenu("UI/True Shadow/True Shadow Disable Cache")]
[ExecuteAlways]
[RequireComponent(typeof(TrueShadow))]
public class TrueShadowDisableCache : MonoBehaviour, ITrueShadowCustomHashProvider
{
    TrueShadow  shadow;
    public bool everyFrame;

    void OnEnable()
    {
        shadow = GetComponent<TrueShadow>();
        Dirty();
    }

    void Update()
    {
        if (everyFrame)
            Dirty();
    }

    void Dirty()
    {
        shadow.CustomHash = Random.Range(int.MinValue, int.MaxValue);
        shadow.SetTextureDirty();
    }

    void OnDisable()
    {
        shadow.CustomHash = 0;
        shadow.SetTextureDirty();
    }
}
}
