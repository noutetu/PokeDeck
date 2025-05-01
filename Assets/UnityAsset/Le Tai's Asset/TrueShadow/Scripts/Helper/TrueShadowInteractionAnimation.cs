// Copyright (c) Le Loc Tai <leloctai.com> . All rights reserved. Do not redistribute.

// ----------------------------------------------------------------------
// シャドウのインタラクションアニメーション
// UIの状態（選択、ホバー、クリック）に応じてシャドウの見た目を変更するコンポーネント
// ----------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.Mathf;

namespace LeTai.TrueShadow
{
[AddComponentMenu("UI/True Shadow/True Shadow Interaction Animation")]
[RequireComponent(typeof(TrueShadow))]
public class TrueShadowInteractionAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler,
                                 IDeselectHandler, IPointerDownHandler, IPointerUpHandler
{
    // ----------------------------------------------------------------------
    // アニメーション設定
    // ----------------------------------------------------------------------
    public float smoothTime = .05f;

    [Tooltip("Deselect on pointer up")]
    public bool autoDeselect;

    // ----------------------------------------------------------------------
    // シャドウサイズ設定
    // ----------------------------------------------------------------------
    [Header("Size")]
    public float selectedSize = 28;
    public float hoverSize   = 28;
    public float clickedSize = 24;

    // ----------------------------------------------------------------------
    // シャドウ距離設定
    // ----------------------------------------------------------------------
    [Header("Distance")]
    public float selectedDistance = 12;
    public float hoverDistance   = 12;
    public float clickedDistance = 8;

    // ----------------------------------------------------------------------
    // シャドウカラー設定
    // ----------------------------------------------------------------------
    [Header("Color")]
    public Color selectedColor = new Color(0, 0, 0, 0.25f);
    public Color hoverColor   = new Color(0, 0, 0, 0.20f);
    public Color clickedColor = new Color(0, 0, 0, 0.25f);

    // 通常状態の設定値
    float normalSize;
    float normalDistance;
    Color normalColor;
    bool  normalStateAcquired;

    // 現在の状態
    bool isSelected;
    bool isHovered;
    bool isClicked;

    TrueShadow shadow;
    Selectable selectable;

    // 目標とする値
    float targetSize;
    float targetDistance;
    Color targetColor;

    static readonly Color FADED_COLOR = new Color(0.5f, 0.5f, 0.5f, 0.5f);

#if UNITY_EDITOR
    // ----------------------------------------------------------------------
    // エディタ初期化
    // デフォルト値を設定する
    // ----------------------------------------------------------------------
    void Reset()
    {
        shadow = FindTrueShadow();
        if (shadow)
        {
            normalSize          = shadow.Size;
            normalDistance      = shadow.OffsetDistance;
            normalColor         = shadow.Color;
            normalStateAcquired = true;

            // Clicked UI remain selected, which is unwanted. Selected state is probably most useful on console
            // and keyboard nav, the later is rather hard to detect
            bool selectedIsNormal = Input.mousePresent || Input.touchSupported;
            autoDeselect = selectedIsNormal;

            hoverSize    = Round(Min(normalSize * 1.75f, normalSize + 20f));
            selectedSize = selectedIsNormal ? normalSize : hoverSize;
            clickedSize  = Round(Min(normalSize * 1.25f, normalSize + 15f));

            hoverDistance    = Round(Min(normalDistance * 1.5f, normalDistance + 20f));
            selectedDistance = selectedIsNormal ? normalDistance : hoverDistance;
            clickedDistance  = Round(Min(normalDistance * 1.25f, normalDistance + 15f));


            hoverColor    = Color.Lerp(normalColor, FADED_COLOR, .15f);
            selectedColor = selectedIsNormal ? normalColor : hoverColor;
            clickedColor  = Color.Lerp(normalColor, FADED_COLOR, .25f);
        }
    }
#endif

    readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    // ----------------------------------------------------------------------
    // コンポーネント有効化時の初期化
    // ----------------------------------------------------------------------
    void OnEnable()
    {
        shadow     = FindTrueShadow();
        selectable = GetComponent<Selectable>();

        isHovered = false;
        if (Input.mousePresent)
            isHovered = IsOverGameObject(Input.mousePosition);

        if (!isHovered)
        {
            for (var i = 0; i < Input.touchCount; i++)
            {
                isHovered = IsOverGameObject(Input.GetTouch(i).position);
                if (isHovered) break;
            }
        }

        isSelected = !autoDeselect && EventSystem.current.currentSelectedGameObject == gameObject;
        isClicked  = false;


        if (!normalStateAcquired)
        {
            targetSize          = normalSize     = shadow.Size;
            targetDistance      = normalDistance = shadow.OffsetDistance;
            targetColor         = normalColor    = shadow.Color;
            normalStateAcquired = true;
        }

        shadow.Size           = targetSize     = normalSize;
        shadow.OffsetDistance = targetDistance = normalDistance;
    }

    // ----------------------------------------------------------------------
    // TrueShadowコンポーネントを探す
    // ----------------------------------------------------------------------
    TrueShadow FindTrueShadow()
    {
        var shadows = GetComponents<TrueShadow>();
        if (shadows.Length == 0) return null;

        var ishadows = GetComponents<TrueShadowInteractionAnimation>();

        int index = 0;
        for (; index < ishadows.Length; index++)
            if (ishadows[index] == this)
                break;

        return shadows[index];
    }

    // ----------------------------------------------------------------------
    // 状態変更時の処理
    // 現在の状態に応じて目標値を設定する
    // ----------------------------------------------------------------------
    void OnStateChange()
    {
        if (isClicked)
        {
            targetSize     = clickedSize;
            targetDistance = clickedDistance;
            targetColor    = clickedColor;
        }
        else if (isSelected)
        {
            targetSize     = selectedSize;
            targetDistance = selectedDistance;
            targetColor    = selectedColor;
        }
        else if (isHovered)
        {
            targetSize     = hoverSize;
            targetDistance = hoverDistance;
            targetColor    = hoverColor;
        }
        else
        {
            targetSize     = normalSize;
            targetDistance = normalDistance;
            targetColor    = normalColor;
        }
    }


#region AnimationState
    float currentSizeVelocity;
    float currentDistanceVelocity;
    float currentColorRVelocity;
    float currentColorGVelocity;
    float currentColorBVelocity;
    float currentColorAVelocity;
#endregion


    // ----------------------------------------------------------------------
    // アニメーション更新
    // 現在の値から目標値へスムーズに変化させる
    // ----------------------------------------------------------------------
    void Update()
    {
        if (!Approximately(targetSize, shadow.Size))
        {
            shadow.Size = SmoothDamp(shadow.Size,
                                     targetSize,
                                     ref currentSizeVelocity,
                                     smoothTime);
        }

        if (!Approximately(targetDistance, shadow.OffsetDistance))
        {
            shadow.OffsetDistance = SmoothDamp(shadow.OffsetDistance,
                                               targetDistance,
                                               ref currentDistanceVelocity,
                                               smoothTime);
        }

        var shadowColor = shadow.Color;
        if (!Approximately(targetColor.a, shadowColor.a))
        {
            var r = SmoothDamp(shadowColor.r,
                               targetColor.r,
                               ref currentColorRVelocity,
                               smoothTime);
            var g = SmoothDamp(shadowColor.g,
                               targetColor.g,
                               ref currentColorGVelocity,
                               smoothTime);
            var b = SmoothDamp(shadowColor.b,
                               targetColor.b,
                               ref currentColorBVelocity,
                               smoothTime);
            var a = SmoothDamp(shadowColor.a,
                               targetColor.a,
                               ref currentColorAVelocity,
                               smoothTime);

            shadow.Color = new Color(r, g, b, a);
        }
    }

    // ----------------------------------------------------------------------
    // ポインターが要素に入った時の処理
    // ----------------------------------------------------------------------
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        OnStateChange();
    }

    // ----------------------------------------------------------------------
    // ポインターが要素から出た時の処理
    // ----------------------------------------------------------------------
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        OnStateChange();
    }

    // ----------------------------------------------------------------------
    // 要素が選択された時の処理
    // ----------------------------------------------------------------------
    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        OnStateChange();
    }

    // ----------------------------------------------------------------------
    // 要素の選択が解除された時の処理
    // ----------------------------------------------------------------------
    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        OnStateChange();
    }

    // ----------------------------------------------------------------------
    // ポインターが要素を押した時の処理
    // ----------------------------------------------------------------------
    public void OnPointerDown(PointerEventData eventData)
    {
        isClicked = true;
        OnStateChange();
    }

    // ----------------------------------------------------------------------
    // ポインターが要素から離れた時の処理
    // ----------------------------------------------------------------------
    public void OnPointerUp(PointerEventData eventData)
    {
        if (autoDeselect && EventSystem.current.currentSelectedGameObject == gameObject)
            EventSystem.current.SetSelectedGameObject(null);

        isClicked = false;
        OnStateChange();
    }

    // ----------------------------------------------------------------------
    // 指定した位置がこのGameObjectの上にあるか判定
    // ----------------------------------------------------------------------
    bool IsOverGameObject(Vector2 position)
    {
        var pointerData = new PointerEventData(EventSystem.current) { position = position };

        EventSystem.current.RaycastAll(pointerData, raycastResults);

        for (var i = 0; i < raycastResults.Count; i++)
        {
            if (raycastResults[i].gameObject == gameObject)
                return true;
        }

        return false;
    }
}
}
