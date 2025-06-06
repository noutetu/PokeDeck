// Copyright (c) Le Loc Tai <leloctai.com> . All rights reserved. Do not redistribute.

using UnityEngine;

namespace LeTai.Effects
{
public abstract class BlurConfig : ScriptableObject
{
    public abstract float Strength  { get; set; }
    public abstract int   MinExtent { get; }
}
}
