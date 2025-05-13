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
    [SerializeField] private TextMeshProUGUI titleText; // パネルのタイトルテキスト

    // ----------------------------------------------------------------------
    // プライベート変数
    // ----------------------------------------------------------------------
    private HashSet<Enum.PokemonType> selectedTypes = new HashSet<Enum.PokemonType>(); // 現在選択されているタイプ
    private DeckModel deckModel; // 現在編集中のデッキモデル

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
    [SerializeField] private Sprite dragonEnergySprite; // ドラゴンエネルギー
    [SerializeField] private Sprite colorlessEnergySprite; // 無色エネルギー

    // ----------------------------------------------------------------------
    // イベント
    // ----------------------------------------------------------------------
    public event Action<HashSet<Enum.PokemonType>> OnEnergyTypeSelected; // エネルギータイプが選択されたときのイベント

    // ----------------------------------------------------------------------
    // 初期化
    // ----------------------------------------------------------------------
    private void Awake()
    {
        // 最初は非表示にする
        gameObject.SetActive(false);
    }

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
        // エネルギータイプトグルの設定
        if (energyTypeToggles != null)
        {
            for (int i = 0; i < energyTypeToggles.Length; i++)
            {
                // 現在のインデックスを保持
                int index = i;
                if (energyTypeToggles[i] != null)
                {
                    // トグルの値変更イベントをリスナーに追加
                    energyTypeToggles[i].onValueChanged.AddListener((isOn) => OnEnergyTypeToggleChanged(index, isOn));
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // エネルギータイプトグルの値が変更されたとき
    // ----------------------------------------------------------------------
    private void OnEnergyTypeToggleChanged(int index, bool isOn)
    {
        if (index < 0 || index >= System.Enum.GetValues(typeof(Enum.PokemonType)).Length)
            return;

        // ポケモンタイプに変換
        Enum.PokemonType type = (Enum.PokemonType)index;

        // トグルがONになった場合（選択された）
        if (isOn)
        {
            // まだ選択リストに追加されていない場合のみ処理
            if (!selectedTypes.Contains(type))
            {
                // 最大数チェック
                if (selectedTypes.Count < DeckModel.MAX_SELECTED_ENERGIES)
                {
                    selectedTypes.Add(type);
                }
                // 最大数を超える場合はフィードバックを表示してトグルを元に戻す
                else
                {
                    if (FeedbackContainer.Instance != null)
                    {
                        FeedbackContainer.Instance.ShowFailureFeedback(
                            $"最大{DeckModel.MAX_SELECTED_ENERGIES}つまでしか選択できません");
                    }
                    // トグルを元に戻す（ユーザー操作ではなくコードから変更することに注意）
                    // イベントの無限ループを避けるためにイベントを一時的に無効化することが理想的
                    if (energyTypeToggles[index] != null)
                    {
                        energyTypeToggles[index].isOn = false;
                    }
                    return;
                }
            }
        }
        // トグルがOFFになった場合（選択解除された）
        else
        {
            // 選択リストから削除
            selectedTypes.Remove(type);
        }

        // 変更を通知
        OnEnergyTypeSelected?.Invoke(selectedTypes);
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
            }
            
            // トグルの視覚的なフィードバックはToggleコンポーネントが自動的に処理
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

        // 現在の選択状態をクリア
        selectedTypes.Clear();

        // デッキから選択済みエネルギーを読み込み
        foreach (var type in deckModel.SelectedEnergyTypes)
        {
            selectedTypes.Add(type);
        }

        // トグルの選択状態を更新
        UpdateAllToggleSelections();

        // パネルを表示
        gameObject.SetActive(true);
    }

    // ----------------------------------------------------------------------
    // パネルを非表示
    // ----------------------------------------------------------------------
    public void HidePanel()
    {
        // パネルを非表示
        gameObject.SetActive(false);

        // 注意: デッキモデルの更新はDeckView側で行うようにする
        // デッキモデルの更新処理は削除
    }

    // ----------------------------------------------------------------------
    // 選択を全てクリア
    // ----------------------------------------------------------------------
    public void ClearSelection()
    {
        // 選択をクリア
        selectedTypes.Clear();

        // トグルの選択状態を更新
        UpdateAllToggleSelections();

        // 変更を通知
        OnEnergyTypeSelected?.Invoke(selectedTypes);

        // フィードバック表示
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowSuccessFeedback("エネルギー選択をクリアしました");
        }
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
        // 全てのトグルの選択状態をリセット
        for (int i = 0; i < energyTypeToggles.Length; i++)
        {
            Enum.PokemonType type = (Enum.PokemonType)i;
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
            case Enum.PokemonType.ドラゴン: return dragonEnergySprite;
            case Enum.PokemonType.無色: return colorlessEnergySprite;
            default: return null;
        }
    }
}
