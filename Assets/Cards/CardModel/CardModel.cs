using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enum;
using System.Data;

// ----------------------------------------------------------------------
// カードのデータを保持するクラス（Model）
// カードのID・名前・HP・画像URLなどを保持し、
// Presenterからの要求に応じてデータを提供する
// ----------------------------------------------------------------------
public class CardModel
{
    // ----------------------------------------------------------------------
    // カード基本情報
    // ----------------------------------------------------------------------
    private string _id;
    public string id { get => _id; set => _id = value; }
    private string _name;
    public string name
    {
        get => _name;
        set
        {
            _name = value;
        }
    }
    public string cardType;              // カードタイプ（JSON文字列形式）
    public string evolutionStage;        // 進化段階（JSON文字列形式）
    public string pack;                  // カードパック名（JSON文字列形式）
    
    // ----------------------------------------------------------------------
    // ポケモン固有の情報
    // ----------------------------------------------------------------------
    public int hp;                       // HPポイント値
    public string type;                  // ポケモンのタイプ（JSON文字列形式）
    public string weakness;              // 弱点属性（JSON文字列形式）
    public int retreatCost;              // 逃げるためのエネルギーコスト
    
    // ----------------------------------------------------------------------
    // 技・特性情報
    // ----------------------------------------------------------------------
    public string abilityName;           // 特性名
    public string abilityEffect;         // 特性効果の説明文
    public List<MoveData> moves;         // 技データのリスト
    
    // ----------------------------------------------------------------------
    // メタデータ・表示データ
    // ----------------------------------------------------------------------
    public List<string> tags;            // カードの分類タグ（JSON文字列形式）
    public int maxDamage;                // カードが与える最大ダメージ
    public string imageKey;              // カード画像のURL/キー
    public Texture2D imageTexture;       // ロードされたカード画像テクスチャ
    
    // ----------------------------------------------------------------------
    // 変換後の列挙型データ
    // ----------------------------------------------------------------------
    public CardType cardTypeEnum;        // 文字列から変換後のカードタイプ
    public PokemonType typeEnum;         // 文字列から変換後のポケモンタイプ
    public EvolutionStage evolutionStageEnum; // 文字列から変換後の進化段階
    public WeaknessType weaknessEnum;    // 文字列から変換後の弱点タイプ
    public CardPack packEnum;            // 文字列から変換後のカードパック
    public CardTag tagsEnum;             // 文字列から変換後のカードタグ（ビットフラグ）
    
    // ----------------------------------------------------------------------
    // Jsonデータ受信後に文字列から列挙型への変換を行うメソッド
    // カードタイプ、進化段階、ポケモンタイプの変換を行うように更新
    // ----------------------------------------------------------------------
    public void ConvertStringDataToEnums()
    {
        // idの変換処理を削除（idを文字列のまま使用）
        
        // カードタイプの変換
        if (!string.IsNullOrEmpty(cardType))
        {
            cardTypeEnum = EnumConverter.ToCardType(cardType);
        }
        else
        {
        }
        
        // 進化段階の変換（ポケモンカードの場合のみ）
        if (cardTypeEnum == CardType.非EX || cardTypeEnum == CardType.EX)
        {
            // ポケモンカードの場合は進化段階を変換
            if (!string.IsNullOrEmpty(evolutionStage))
            {
                evolutionStageEnum = EnumConverter.ToEvolutionStage(evolutionStage);
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(evolutionStage))
            {
                evolutionStageEnum = EnumConverter.ToEvolutionStage(evolutionStage);
            }
        }
        
        // ポケモンタイプの変換
        if (!string.IsNullOrEmpty(type))
        {
            typeEnum = EnumConverter.ToPokemonType(type); 
        }
        else
        {
        }
        
        // カードパックの変換
        if (!string.IsNullOrEmpty(pack))
        {
            packEnum = EnumConverter.ToCardPack(pack);
        }
        else
        {
        }
        // Uncomment and implement these lines if needed
        // weaknessEnum = EnumConverter.ToWeaknessType(weakness);
        // tagsEnum = EnumConverter.ToCardTags(tags);
        // tagsEnum = EnumConverter.ToCardTags(tags);
        
    }

}

// ----------------------------------------------------------------------
// 技データを保持するクラス
// 技の名前、ダメージ値、効果説明、必要エネルギーを管理
// ----------------------------------------------------------------------
public class MoveData
{
    public string name;                      // 技の名前
    public int damage;                       // 技のダメージ値
    public string effect;                    // 技の効果説明
    public Dictionary<string, int> cost;     // 必要エネルギー（タイプ, 数）
}