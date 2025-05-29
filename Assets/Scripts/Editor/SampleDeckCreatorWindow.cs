using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace PokeDeck.Editor
{
    /// <summary>
    /// サンプルデッキ作成用のエディターウィンドウ
    /// DeckManagerのSampleDeckConfigを効率的に作成・編集するためのツール
    /// </summary>
    public class SampleDeckCreatorWindow : EditorWindow
    {
        // ----------------------------------------------------------------------
        // フィールド変数
        // ----------------------------------------------------------------------
        private string deckName = "";
        private string deckMemo = "";
        private int maxCards = 20;
        private Vector2 scrollPosition;
        private List<bool> energyTypeSelected = new List<bool>();
        private List<string> cardIds = new List<string>();
        
        // エネルギータイプの表示名とEnum値のマッピング
        private readonly (string displayName, Enum.PokemonType type)[] availableEnergyTypes = new[]
        {
            ("草", Enum.PokemonType.草),
            ("炎", Enum.PokemonType.炎),
            ("水", Enum.PokemonType.水),
            ("雷", Enum.PokemonType.雷),
            ("超", Enum.PokemonType.超),
            ("闘", Enum.PokemonType.闘),
            ("悪", Enum.PokemonType.悪),
            ("鋼", Enum.PokemonType.鋼)
        };

        // ----------------------------------------------------------------------
        // メニューアイテム
        // ----------------------------------------------------------------------
        [MenuItem("PokeDeck/Sample Deck Creator")]
        public static void ShowWindow()
        {
            var window = GetWindow<SampleDeckCreatorWindow>("Sample Deck Creator");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        // ----------------------------------------------------------------------
        // 初期化
        // ----------------------------------------------------------------------
        private void OnEnable()
        {
            // エネルギータイプ選択状態を初期化
            energyTypeSelected = new List<bool>(new bool[availableEnergyTypes.Length]);
            
            // カードID入力フィールドを初期化
            if (cardIds.Count == 0)
            {
                cardIds.Add("");
            }
        }

        // ----------------------------------------------------------------------
        // GUI描画
        // ----------------------------------------------------------------------
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Sample Deck Creator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawBasicInfo();
            EditorGUILayout.Space();

            DrawEnergyTypeSelector();
            EditorGUILayout.Space();

            DrawCardIdList();
            EditorGUILayout.Space();

            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        // ----------------------------------------------------------------------
        // 基本情報セクション
        // ----------------------------------------------------------------------
        private void DrawBasicInfo()
        {
            EditorGUILayout.LabelField("基本情報", EditorStyles.boldLabel);
            
            deckName = EditorGUILayout.TextField("デッキ名", deckName);
            
            EditorGUILayout.LabelField("デッキメモ");
            deckMemo = EditorGUILayout.TextArea(deckMemo, GUILayout.Height(60));
            
            maxCards = EditorGUILayout.IntSlider("最大カード数", maxCards, 1, 60);
        }

        // ----------------------------------------------------------------------
        // エネルギータイプ選択セクション
        // ----------------------------------------------------------------------
        private void DrawEnergyTypeSelector()
        {
            EditorGUILayout.LabelField("エネルギータイプ (最大2つ)", EditorStyles.boldLabel);
            
            int selectedCount = energyTypeSelected.Count(x => x);
            
            for (int i = 0; i < availableEnergyTypes.Length; i++)
            {
                bool wasSelected = energyTypeSelected[i];
                bool isSelected = EditorGUILayout.Toggle(availableEnergyTypes[i].displayName, wasSelected);
                
                // 選択状態が変更された場合の処理
                if (isSelected != wasSelected)
                {
                    if (isSelected && selectedCount >= 2)
                    {
                        // 既に2つ選択されている場合は選択を無効にする
                        EditorUtility.DisplayDialog("選択制限", "エネルギータイプは最大2つまで選択できます。", "OK");
                        continue;
                    }
                    
                    energyTypeSelected[i] = isSelected;
                }
            }
            
            // 選択中のエネルギータイプを表示
            var selectedTypes = GetSelectedEnergyTypes();
            if (selectedTypes.Count > 0)
            {
                EditorGUILayout.LabelField($"選択中: {string.Join(", ", selectedTypes.Select(t => t.ToString()))}");
            }
        }

        // ----------------------------------------------------------------------
        // カードIDリストセクション
        // ----------------------------------------------------------------------
        private void DrawCardIdList()
        {
            EditorGUILayout.LabelField("カードID", EditorStyles.boldLabel);
            
            for (int i = 0; i < cardIds.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                cardIds[i] = EditorGUILayout.TextField($"カード {i + 1}", cardIds[i]);
                
                if (GUILayout.Button("×", GUILayout.Width(20)) && cardIds.Count > 1)
                {
                    cardIds.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("カードIDを追加"))
            {
                cardIds.Add("");
            }
            
            if (GUILayout.Button("空のエントリを削除"))
            {
                cardIds.RemoveAll(string.IsNullOrEmpty);
                if (cardIds.Count == 0)
                {
                    cardIds.Add("");
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // ----------------------------------------------------------------------
        // アクションボタンセクション
        // ----------------------------------------------------------------------
        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("アクション", EditorStyles.boldLabel);
            
            // プレビュー情報
            DrawPreviewInfo();
            
            EditorGUILayout.Space();
            
            // 作成ボタン
            GUI.enabled = !string.IsNullOrEmpty(deckName);
            if (GUILayout.Button("サンプルデッキ設定を作成", GUILayout.Height(30)))
            {
                CreateSampleDeckConfig();
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space();
            
            // クリアボタン
            if (GUILayout.Button("フォームをクリア"))
            {
                ClearForm();
            }
        }

        // ----------------------------------------------------------------------
        // プレビュー情報表示
        // ----------------------------------------------------------------------
        private void DrawPreviewInfo()
        {
            EditorGUILayout.LabelField("プレビュー", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"デッキ名: {(string.IsNullOrEmpty(deckName) ? "未設定" : deckName)}");
            EditorGUILayout.LabelField($"最大カード数: {maxCards}");
            
            var selectedTypes = GetSelectedEnergyTypes();
            EditorGUILayout.LabelField($"エネルギータイプ: {(selectedTypes.Count > 0 ? string.Join(", ", selectedTypes) : "未選択")}");
            
            var validCardIds = cardIds.Where(id => !string.IsNullOrEmpty(id)).ToList();
            EditorGUILayout.LabelField($"カードID数: {validCardIds.Count}");
            EditorGUI.indentLevel--;
        }

        // ----------------------------------------------------------------------
        // 選択されたエネルギータイプを取得
        // ----------------------------------------------------------------------
        private List<Enum.PokemonType> GetSelectedEnergyTypes()
        {
            var selectedTypes = new List<Enum.PokemonType>();
            for (int i = 0; i < energyTypeSelected.Count && i < availableEnergyTypes.Length; i++)
            {
                if (energyTypeSelected[i])
                {
                    selectedTypes.Add(availableEnergyTypes[i].type);
                }
            }
            return selectedTypes;
        }

        // ----------------------------------------------------------------------
        // 有効なカードIDリストを取得
        // ----------------------------------------------------------------------
        private List<string> GetValidCardIds()
        {
            return cardIds.Where(id => !string.IsNullOrEmpty(id.Trim())).Select(id => id.Trim()).ToList();
        }

        // ----------------------------------------------------------------------
        // サンプルデッキ設定を作成
        // ----------------------------------------------------------------------
        private void CreateSampleDeckConfig()
        {
            var deckManager = FindObjectOfType<DeckManager>();
            if (deckManager == null)
            {
                EditorUtility.DisplayDialog("エラー", "シーン内にDeckManagerが見つかりません。", "OK");
                return;
            }

            try
            {
                // SerializedObjectを使用してInspectorの設定を更新
                var serializedObject = new SerializedObject(deckManager);
                var sampleDecksProperty = serializedObject.FindProperty("sampleDecks");
                
                // 新しいサンプルデッキエントリを追加
                sampleDecksProperty.arraySize++;
                var newElement = sampleDecksProperty.GetArrayElementAtIndex(sampleDecksProperty.arraySize - 1);
                
                // 基本情報を設定
                newElement.FindPropertyRelative("deckName").stringValue = deckName;
                newElement.FindPropertyRelative("deckMemo").stringValue = deckMemo;
                newElement.FindPropertyRelative("maxCards").intValue = maxCards;
                
                // エネルギータイプを設定
                var selectedTypes = GetSelectedEnergyTypes();
                var energyTypesProperty = newElement.FindPropertyRelative("energyTypes");
                energyTypesProperty.arraySize = selectedTypes.Count;
                for (int i = 0; i < selectedTypes.Count; i++)
                {
                    energyTypesProperty.GetArrayElementAtIndex(i).intValue = (int)selectedTypes[i];
                }
                
                // カードIDを設定
                var validCardIds = GetValidCardIds();
                var cardIdsProperty = newElement.FindPropertyRelative("specificCardIds");
                cardIdsProperty.arraySize = validCardIds.Count;
                for (int i = 0; i < validCardIds.Count; i++)
                {
                    cardIdsProperty.GetArrayElementAtIndex(i).stringValue = validCardIds[i];
                }
                
                // 変更を適用
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(deckManager);
                
                // 成功メッセージ
                EditorUtility.DisplayDialog("成功", $"サンプルデッキ '{deckName}' を作成しました！", "OK");
                
                // DeckManagerをハイライト表示
                Selection.activeObject = deckManager;
                EditorGUIUtility.PingObject(deckManager);
                
                Debug.Log($"Sample deck '{deckName}' created successfully with {validCardIds.Count} cards and {selectedTypes.Count} energy types.");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("エラー", $"サンプルデッキの作成中にエラーが発生しました: {ex.Message}", "OK");
                Debug.LogError($"Failed to create sample deck: {ex.Message}");
            }
        }

        // ----------------------------------------------------------------------
        // フォームをクリア
        // ----------------------------------------------------------------------
        private void ClearForm()
        {
            if (EditorUtility.DisplayDialog("確認", "フォームの内容をクリアしますか？", "はい", "いいえ"))
            {
                deckName = "";
                deckMemo = "";
                maxCards = 20;
                energyTypeSelected = new List<bool>(new bool[availableEnergyTypes.Length]);
                cardIds = new List<string> { "" };
                
                Debug.Log("Sample deck creator form cleared.");
            }
        }
    }
}
