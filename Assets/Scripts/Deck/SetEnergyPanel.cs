using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ----------------------------------------------------------------------
// エネルギータイプ選択パネルを管理するクラス
// 最大2つまでのエネルギータイプを選択可能
// ----------------------------------------------------------------------
public class SetEnergyPanel : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // UI参照
    // ----------------------------------------------------------------------
    [Header("UI参照")]
    [SerializeField] private Toggle[] energyTypeToggles; // エネルギータイプのトグル（10種類）
    [SerializeField] private Image[] energyTypeImages; // エネルギータイプのアイコン画像

    // ----------------------------------------------------------------------
    // プライベート変数
    // ----------------------------------------------------------------------
    private HashSet<Enum.PokemonType> selectedTypes = new HashSet<Enum.PokemonType>(); // 現在選択されているタイプ
    private DeckModel deckModel; // 現在編集中のデッキモデル

    // ----------------------------------------------------------------------
    // 選択可能なエネルギータイプ（ドラゴンと無色を除外）
    // ----------------------------------------------------------------------
    private readonly Enum.PokemonType[] availableEnergyTypes = new Enum.PokemonType[]
    {
        Enum.PokemonType.草,
        Enum.PokemonType.炎,
        Enum.PokemonType.水,
        Enum.PokemonType.雷,
        Enum.PokemonType.超,
        Enum.PokemonType.闘,
        Enum.PokemonType.悪,
        Enum.PokemonType.鋼,
    };

    // ----------------------------------------------------------------------
    // エネルギータイプ画像リソース
    // ----------------------------------------------------------------------
    [Header("エネルギー画像")]
    [SerializeField] private Sprite grassEnergySprite; // 草エネルギー
    [SerializeField] private Sprite fireEnergySprite; // 炎エネルギー
    [SerializeField] private Sprite waterEnergySprite; // 水エネルギー
    [SerializeField] private Sprite lightningEnergySprite; // 雷エネルギー
    [SerializeField] private Sprite fightingEnergySprite; // 闘エネルギー
    [SerializeField] private Sprite psychicEnergySprite; // 超エネルギー
    [SerializeField] private Sprite darknessEnergySprite; // 悪エネルギー
    [SerializeField] private Sprite steelEnergySprite; // 鋼エネルギー

    // ----------------------------------------------------------------------
    // イベント
    // ----------------------------------------------------------------------
    public event Action<HashSet<Enum.PokemonType>> OnEnergyTypeSelected; // エネルギータイプが選択されたときのイベント

    // ----------------------------------------------------------------------
    // 開始時
    // ----------------------------------------------------------------------
    private void Start()
    {
        // ボタンイベントの設定
        SetupButtonEvents();
    }

    // ----------------------------------------------------------------------
    // トグルイベントの設定
    // ----------------------------------------------------------------------
    private void SetupButtonEvents()
    {
        if (energyTypeToggles == null) return;

        for (int i = 0; i < energyTypeToggles.Length; i++)
        {
            SetupSingleToggle(i);
        }
    }

    // ----------------------------------------------------------------------
    // 個別のトグル設定
    // ----------------------------------------------------------------------
    private void SetupSingleToggle(int index)
    {
        if (energyTypeToggles[index] == null) return;

        bool isAvailable = index < availableEnergyTypes.Length;
        energyTypeToggles[index].gameObject.SetActive(isAvailable);

        if (!isAvailable) return;

        // イベントリスナー設定
        int capturedIndex = index; // クロージャ対策
        energyTypeToggles[index].onValueChanged.AddListener(isOn => 
            OnEnergyTypeToggleChanged(capturedIndex, isOn));

        // エネルギー画像設定
        SetEnergyImage(index);
    }

    // ----------------------------------------------------------------------
    // エネルギー画像設定
    // ----------------------------------------------------------------------
    private void SetEnergyImage(int index)
    {
        if (index >= energyTypeImages.Length || energyTypeImages[index] == null) return;
        
        energyTypeImages[index].sprite = GetEnergySprite(availableEnergyTypes[index]);
    }

    // ----------------------------------------------------------------------
    // エネルギータイプトグルの値が変更されたとき
    // ----------------------------------------------------------------------
    private void OnEnergyTypeToggleChanged(int index, bool isOn)
    {
        if (!IsValidIndex(index)) return;

        Enum.PokemonType type = availableEnergyTypes[index];

        if (isOn)
        {
            HandleToggleOn(type, index);
        }
        else
        {
            HandleToggleOff(type);
        }

        // 視覚的状態を更新
        UpdateToggleVisualState(energyTypeToggles[index], isOn);

        // 変更を通知
        OnEnergyTypeSelected?.Invoke(selectedTypes);
    }

    // ----------------------------------------------------------------------
    // インデックスの有効性をチェック
    // ----------------------------------------------------------------------
    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < availableEnergyTypes.Length;
    }

    // ----------------------------------------------------------------------
    // トグルON時の処理
    // ----------------------------------------------------------------------
    private void HandleToggleOn(Enum.PokemonType type, int index)
    {
        if (selectedTypes.Contains(type)) return;

        if (selectedTypes.Count < DeckModel.MAX_SELECTED_ENERGIES)
        {
            selectedTypes.Add(type);
        }
        else
        {
            ShowMaxSelectionError();
            ResetToggle(index);
        }
    }

    // ----------------------------------------------------------------------
    // トグルOFF時の処理
    // ----------------------------------------------------------------------
    private void HandleToggleOff(Enum.PokemonType type)
    {
        selectedTypes.Remove(type);
    }

    // ----------------------------------------------------------------------
    // 最大選択数エラー表示
    // ----------------------------------------------------------------------
    private void ShowMaxSelectionError()
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowFailureFeedback(
                $"最大{DeckModel.MAX_SELECTED_ENERGIES}つまでしか選択できません");
        }
    }

    // ----------------------------------------------------------------------
    // トグルをリセット（状態と視覚的状態の両方）
    // ----------------------------------------------------------------------
    private void ResetToggle(int index)
    {
        if (energyTypeToggles[index] != null)
        {
            Toggle toggle = energyTypeToggles[index];
            toggle.SetIsOnWithoutNotify(false);
            UpdateToggleVisualState(toggle, false);
        }
    }

    // ----------------------------------------------------------------------
    // トグルの視覚的状態を明示的に更新
    // ----------------------------------------------------------------------
    private void UpdateToggleVisualState(Toggle toggle, bool isOn)
    {
        if (toggle == null) return;

        // SimpleToggleColorコンポーネントを取得して色を更新
        SimpleToggleColor colorComponent = toggle.GetComponent<SimpleToggleColor>();
        if (colorComponent != null)
        {
            colorComponent.UpdateColorState(isOn);
        }

        // TrueShadowToggleInsetコンポーネントを取得して影状態を更新
        TrueShadowToggleInset shadowComponent = toggle.GetComponent<TrueShadowToggleInset>();
        if (shadowComponent != null)
        {
            shadowComponent.UpdateInsetState(isOn);
        }
    }

    // ----------------------------------------------------------------------
    // トグル選択状態の更新
    // ----------------------------------------------------------------------
    private void UpdateToggleSelection(int index, bool isSelected)
    {
        // トグルの状態を更新（イベントを発生させない方法で）
        if (index >= 0 && index < energyTypeToggles.Length && energyTypeToggles[index] != null)
        {
            Toggle toggle = energyTypeToggles[index];

            // 現在の値と異なる場合のみ更新してイベントの無限ループを避ける
            if (toggle.isOn != isSelected)
            {
                // イベントを一時的に無効化するのが理想的だが、
                // ここでは単純に値のみを更新する
                toggle.SetIsOnWithoutNotify(isSelected);
                
                // 視覚的状態も明示的に更新
                UpdateToggleVisualState(toggle, isSelected);
            }
        }
    }

    // ----------------------------------------------------------------------
    // パネルを表示
    // ----------------------------------------------------------------------
    public void ShowPanel(DeckModel deck)
    {
        // デッキモデルの設定
        deckModel = deck;
        if (deckModel == null)
            return;

        // パネルを表示
        gameObject.SetActive(true);

        // 現在の選択状態をクリア
        selectedTypes.Clear();
        // トグルの選択状態を初期化
        for (int i = 0; i < energyTypeToggles.Length; i++)
        {
            ResetToggle(i);
        }

        // デッキから選択済みエネルギーを読み込み
            foreach (var type in deckModel.SelectedEnergyTypes)
            {
                selectedTypes.Add(type);
            }

        // トグルの選択状態を更新
        UpdateAllToggleSelections();
    }

    // ----------------------------------------------------------------------
    // パネルを非表示
    // ----------------------------------------------------------------------
    public void HidePanel()
    {
        // パネルを非表示
        gameObject.SetActive(false);
    }

    // ----------------------------------------------------------------------
    // 現在選択されているエネルギータイプを取得
    // ----------------------------------------------------------------------
    public HashSet<Enum.PokemonType> GetSelectedTypes()
    {
        // 現在選択されているエネルギータイプのコピーを返す
        return new HashSet<Enum.PokemonType>(selectedTypes);
    }

    // ----------------------------------------------------------------------
    // 全てのトグルの選択状態を更新
    // ----------------------------------------------------------------------
    private void UpdateAllToggleSelections()
    {
        // 全てのトグルの選択状態を更新
        for (int i = 0; i < availableEnergyTypes.Length && i < energyTypeToggles.Length; i++)
        {
            Enum.PokemonType type = availableEnergyTypes[i];
            UpdateToggleSelection(i, selectedTypes.Contains(type));
        }
    }

    // ----------------------------------------------------------------------
    // エネルギータイプに対応するスプライトを取得
    // ----------------------------------------------------------------------
    public Sprite GetEnergySprite(Enum.PokemonType type)
    {
        switch (type)
        {
            case Enum.PokemonType.草: return grassEnergySprite;
            case Enum.PokemonType.炎: return fireEnergySprite;
            case Enum.PokemonType.水: return waterEnergySprite;
            case Enum.PokemonType.雷: return lightningEnergySprite;
            case Enum.PokemonType.闘: return fightingEnergySprite;
            case Enum.PokemonType.超: return psychicEnergySprite;
            case Enum.PokemonType.悪: return darknessEnergySprite;
            case Enum.PokemonType.鋼: return steelEnergySprite;
            default: return null;
        }
    }
}