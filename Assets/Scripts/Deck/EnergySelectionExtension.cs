using UnityEngine;
using UnityEngine.UI;

// ----------------------------------------------------------------------
// DeckViewのエネルギー選択UI機能を拡張するクラス
// ----------------------------------------------------------------------
public static class EnergySelectionExtension
{
    // ----------------------------------------------------------------------
    // エネルギー選択UI機能をセットアップする
    // DeckView.SetupUIEventsメソッド内から呼び出す
    // ----------------------------------------------------------------------
    public static void SetupEnergySelectionUI(
        DeckView deckView, 
        Button inputEnergyButton, 
        GameObject setEnergyPanelObj,
        DeckModel currentDeck)
    {
        if (inputEnergyButton == null || setEnergyPanelObj == null || currentDeck == null)
            return;
            
        // パネルのコンポーネント取得
        SetEnergyPanel setEnergyPanel = setEnergyPanelObj.GetComponent<SetEnergyPanel>();
        if (setEnergyPanel == null)
            return;
            
        // パネルにエネルギー選択イベントを追加
        setEnergyPanel.OnEnergyTypeSelected += (selectedTypes) => {
            // DeckViewクラスの公開メソッドを使用してエネルギー画像を更新
            deckView.UpdateEnergyImages();
        };
        
        // ボタンクリックでパネルを表示
        inputEnergyButton.onClick.AddListener(() => {
            setEnergyPanel.ShowPanel(currentDeck);
        });
    }
    
    // ----------------------------------------------------------------------
    // エネルギー画像を更新する（DeckViewクラスにメソッド追加）
    // ----------------------------------------------------------------------
    public static void UpdateEnergyImages(
        Image energyImage1, 
        Image energyImage2, 
        SetEnergyPanel setEnergyPanel,
        DeckModel currentDeck)
    {
        if (energyImage1 == null || energyImage2 == null || 
            setEnergyPanel == null || currentDeck == null)
            return;
            
        // まず両方の画像をクリア
        energyImage1.sprite = null;
        energyImage1.enabled = false;
        energyImage2.sprite = null;
        energyImage2.enabled = false;
        
        // 選択されたエネルギータイプを取得
        var selectedTypes = currentDeck.SelectedEnergyTypes;
        
        // 選択されたタイプがあれば画像を設定
        if (selectedTypes.Count > 0)
        {
            // 1つ目のエネルギー
            energyImage1.sprite = setEnergyPanel.GetEnergySprite(selectedTypes[0]);
            energyImage1.enabled = true;
            
            // 2つ目のエネルギー（存在する場合）
            if (selectedTypes.Count > 1)
            {
                energyImage2.sprite = setEnergyPanel.GetEnergySprite(selectedTypes[1]);
                energyImage2.enabled = true;
            }
        }
    }
}
