using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;
using System;

// ----------------------------------------------------------------------
// デッキのファイル入出力を専門に行うクラス
// DeckManagerから分離してコードの単一責任原則を守る
// ----------------------------------------------------------------------
public class DeckFileManager
{
    private readonly string _savePath;

    // ----------------------------------------------------------------------
    // セーブパスの公開プロパティ
    // ----------------------------------------------------------------------
    public string SavePath => _savePath;

    // ----------------------------------------------------------------------
    // コンストラクタ
    // ----------------------------------------------------------------------
    public DeckFileManager()
    {
        _savePath = Path.Combine(Application.persistentDataPath, "decks.json");
    }

    // ----------------------------------------------------------------------
    // デッキを同期でファイルに保存
    // ----------------------------------------------------------------------
    public void SaveDecks(IReadOnlyList<DeckModel> decksToSave)
    {
        try
        {
            if (decksToSave == null)
            {
                Debug.LogWarning("保存するデッキリストがnullです");
                return;
            }

            // シリアライズ用のリストを作成
            List<SimplifiedDeck> simplifiedDecks = new List<SimplifiedDeck>();
            foreach (var deck in decksToSave)
            {
                if (deck != null)
                {
                    simplifiedDecks.Add(new SimplifiedDeck(deck));
                }
            }

            // JSONにシリアライズして保存
            string json = JsonConvert.SerializeObject(simplifiedDecks, Formatting.Indented);
            File.WriteAllText(_savePath, json);

            Debug.Log($"デッキの保存が完了: {simplifiedDecks.Count}個のデッキ");
        }
        catch (Exception ex)
        {
            Debug.LogError($"デッキの保存中にエラーが発生: {ex.Message}");
            throw;
        }
    }

    // ----------------------------------------------------------------------
    // デッキを非同期でファイルに保存
    // ----------------------------------------------------------------------
    public async UniTask SaveDecksAsync(IReadOnlyList<DeckModel> decksToSave)
    {
        try
        {
            if (decksToSave == null)
            {
                Debug.LogWarning("保存するデッキリストがnullです");
                return;
            }

            // シリアライズ処理を別スレッドで実行
            var simplifiedDecks = await UniTask.RunOnThreadPool(() =>
            {
                var result = new List<SimplifiedDeck>();
                foreach (var deck in decksToSave)
                {
                    if (deck != null)
                    {
                        result.Add(new SimplifiedDeck(deck));
                    }
                }
                return result;
            });

            // JSON変換とファイル書き込みを別スレッドで実行
            await UniTask.RunOnThreadPool(() =>
            {
                string json = JsonConvert.SerializeObject(simplifiedDecks, Formatting.Indented);
                File.WriteAllText(_savePath, json);
            });

            Debug.Log($"デッキの非同期保存が完了: {simplifiedDecks.Count}個のデッキ");
        }
        catch (Exception ex)
        {
            Debug.LogError($"デッキの非同期保存中にエラーが発生: {ex.Message}");
            throw;
        }
    }

    // ----------------------------------------------------------------------
    // デッキを非同期でファイルから読み込み
    // ----------------------------------------------------------------------
    public async UniTask<List<DeckModel>> LoadDecksAsync()
    {
        try
        {
            if (!File.Exists(_savePath))
            {
                Debug.Log("デッキファイルが存在しません。空のリストを返します。");
                return new List<DeckModel>();
            }

            // ファイル読み込みとJSON解析を別スレッドで実行
            var simplifiedDecks = await UniTask.RunOnThreadPool(() =>
            {
                string json = File.ReadAllText(_savePath);
                return JsonConvert.DeserializeObject<List<SimplifiedDeck>>(json);
            });

            if (simplifiedDecks == null || simplifiedDecks.Count == 0)
            {
                Debug.Log("デッキファイルが空またはnullです。空のリストを返します。");
                return new List<DeckModel>();
            }

            // SimplifiedDeckをDeckModelに変換
            var loadedDecks = ConvertSimplifiedDecksToModels(simplifiedDecks);
            
            Debug.Log($"デッキの読み込みが完了: {loadedDecks.Count}個のデッキ");
            return loadedDecks;
        }
        catch (Exception ex)
        {
            Debug.LogError($"デッキの読み込み中にエラーが発生: {ex.Message}");
            throw;
        }
    }

    // ----------------------------------------------------------------------
    // 保存ファイルが存在するかチェック
    // ----------------------------------------------------------------------
    public bool DoesSaveFileExist()
    {
        return File.Exists(_savePath);
    }

    // ----------------------------------------------------------------------
    // 保存ファイルのパスを取得
    // ----------------------------------------------------------------------
    public string GetSavePath()
    {
        return _savePath;
    }

    // ----------------------------------------------------------------------
    // 保存ファイルのサイズを取得（バイト）
    // ----------------------------------------------------------------------
    public long GetSaveFileSize()
    {
        try
        {
            if (File.Exists(_savePath))
            {
                return new FileInfo(_savePath).Length;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ファイルサイズの取得中にエラー: {ex.Message}");
            return 0;
        }
    }

    // ----------------------------------------------------------------------
    // 保存ファイルを削除
    // ----------------------------------------------------------------------
    public bool DeleteSaveFile()
    {
        try
        {
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
                Debug.Log("デッキファイルを削除しました");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ファイル削除中にエラー: {ex.Message}");
            return false;
        }
    }

    // ----------------------------------------------------------------------
    // SimplifiedDeckリストをDeckModelリストに変換
    // ----------------------------------------------------------------------
    private List<DeckModel> ConvertSimplifiedDecksToModels(List<SimplifiedDeck> simplifiedDecks)
    {
        var deckModels = new List<DeckModel>();
        
        foreach (var simpleDeck in simplifiedDecks)
        {
            try
            {
                DeckModel newDeck = CreateDeckFromSimplified(simpleDeck);
                if (newDeck != null)
                {
                    deckModels.Add(newDeck);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"デッキ変換中にエラー (名前: {simpleDeck?.Name}): {ex.Message}");
                // 変換に失敗したデッキはスキップして続行
                continue;
            }
        }

        // 読み込み後の初期化処理
        InitializeLoadedDecks(deckModels);
        
        return deckModels;
    }

    // ----------------------------------------------------------------------
    // SimplifiedDeckから個別のDeckModelを作成
    // ----------------------------------------------------------------------
    private DeckModel CreateDeckFromSimplified(SimplifiedDeck simpleDeck)
    {
        if (simpleDeck == null)
        {
            Debug.LogWarning("SimplifiedDeckがnullです");
            return null;
        }

        var deck = new DeckModel 
        { 
            Name = simpleDeck.Name ?? "",
            Memo = simpleDeck.Memo ?? ""
        };

        // カードIDを追加
        if (simpleDeck.CardIds != null)
        {
            AddCardIdsToDecк(deck, simpleDeck.CardIds);
        }

        // エネルギータイプを復元
        if (simpleDeck.SelectedEnergyTypes != null)
        {
            RestoreEnergyTypes(deck, simpleDeck.SelectedEnergyTypes);
        }

        return deck;
    }

    // ----------------------------------------------------------------------
    // デッキにカードIDを追加
    // ----------------------------------------------------------------------
    private void AddCardIdsToDecк(DeckModel deck, List<string> cardIds)
    {
        if (deck == null || cardIds == null) return;

        foreach (string cardId in cardIds)
        {
            if (!string.IsNullOrEmpty(cardId))
            {
                deck._AddCardId(cardId);
            }
        }
    }

    // ----------------------------------------------------------------------
    // エネルギータイプを復元
    // ----------------------------------------------------------------------
    private void RestoreEnergyTypes(DeckModel deck, List<int> energyTypeInts)
    {
        if (deck == null || energyTypeInts == null) return;

        foreach (int energyTypeInt in energyTypeInts)
        {
            // int値をenumにキャスト
            if (System.Enum.IsDefined(typeof(Enum.PokemonType), energyTypeInt))
            {
                deck.SelectedEnergyTypes.Add((Enum.PokemonType)energyTypeInt);
            }
            else
            {
                Debug.LogWarning($"無効なエネルギータイプ値: {energyTypeInt}");
            }
        }
    }

    // ----------------------------------------------------------------------
    // 読み込み後のデッキ初期化処理
    // ----------------------------------------------------------------------
    private void InitializeLoadedDecks(List<DeckModel> decks)
    {
        if (decks == null) return;

        // 各デッキに対して読み込み後の初期化処理
        foreach (var deck in decks)
        {
            if (deck != null)
            {
                try
                {
                    deck.OnAfterDeserialize();
                    deck.SortCardsByID(); // 読み込み後にカードをID順に並べ替え
                }
                catch (Exception ex)
                {
                    Debug.LogError($"デッキ初期化中にエラー (名前: {deck.Name}): {ex.Message}");
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // バックアップファイルを作成
    // ----------------------------------------------------------------------
    public bool CreateBackup()
    {
        try
        {
            if (!File.Exists(_savePath))
            {
                Debug.LogWarning("バックアップ対象のファイルが存在しません");
                return false;
            }

            string backupPath = _savePath + ".backup";
            File.Copy(_savePath, backupPath, true);
            
            Debug.Log($"バックアップを作成しました: {backupPath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"バックアップ作成中にエラー: {ex.Message}");
            return false;
        }
    }

    // ----------------------------------------------------------------------
    // バックアップファイルから復元
    // ----------------------------------------------------------------------
    public bool RestoreFromBackup()
    {
        try
        {
            string backupPath = _savePath + ".backup";
            if (!File.Exists(backupPath))
            {
                Debug.LogWarning("バックアップファイルが存在しません");
                return false;
            }

            File.Copy(backupPath, _savePath, true);
            
            Debug.Log("バックアップから復元しました");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"バックアップ復元中にエラー: {ex.Message}");
            return false;
        }
    }
}
