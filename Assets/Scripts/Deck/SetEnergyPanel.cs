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
            // 利用可能なエネルギータイプの数
            int availableCount = availableEnergyTypes.Length;
            
            for (int i = 0; i < energyTypeToggles.Length; i++)
            {
                if (energyTypeToggles[i] != null)
                {
                    // 利用可能なタイプかどうかを確認
                    bool isAvailable = i < availableCount;
                    
                    // 利用不可能なエネルギータイプのトグルは非アクティブに
                    energyTypeToggles[i].gameObject.SetActive(isAvailable);
                    
                    if (isAvailable)
                    {
                        // 配列内の対応するインデックスを取得
                        int typeIndex = i;
                        
                        // トグルの値変更イベントをリスナーに追加
                        energyTypeToggles[i].onValueChanged.AddListener((isOn) => 
                            OnEnergyTypeToggleChanged(typeIndex, isOn));
                        
                        // 対応するエネルギー画像を設定
                        if (i < energyTypeImages.Length && energyTypeImages[i] != null)
                        {
                            energyTypeImages[i].sprite = GetEnergySprite(availableEnergyTypes[i]);
                        }
                    }
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // エネルギータイプトグルの値が変更されたとき
    // ----------------------------------------------------------------------
    private void OnEnergyTypeToggleChanged(int index, bool isOn)
    {
        if (index < 0 || index >= availableEnergyTypes.Length)
            return;

        // 対応するポケモンタイプを取得
        Enum.PokemonType type = availableEnergyTypes[index];

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
                    // トグルを元に戻す（イベントを発生させずに）
                    if (energyTypeToggles[index] != null)
                    {
                        energyTypeToggles[index].SetIsOnWithoutNotify(false);
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
