using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enum;

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
    public string id;                    // カードID（ユニークな識別子）
    public string name;                  // カード名 
    public string cardType;              // カードタイプ（EXやサポート、グッズなど）
    public string evolutionStage;        // 進化段階（たね、1進化、2進化）
    public string pack;                  // カードパック名（最強の遺伝子,幻の島、、、、）
    
    // ----------------------------------------------------------------------
    // ポケモン固有の情報
    // ----------------------------------------------------------------------
    public int hp;                       // HPポイント値
    public string type;                  // ポケモンのタイプ（炎や水など）
    public string weakness;              // 弱点属性（炎や水など）
    public int retreatCost;              // 逃げるためのエネルギーコスト
    public int maxEnergyCost;            // 最大わざエネルギーコスト
    
    // ----------------------------------------------------------------------
    // 技・特性情報
    // ----------------------------------------------------------------------
    public string abilityName;           // 特性名
    public string abilityEffect;         // 特性効果の説明文
    public List<MoveData> moves;         // わざ名やわざ効果といったわざ情報のリスト
    
    // ----------------------------------------------------------------------
    // メタデータ・表示データ
    // ----------------------------------------------------------------------
    public List<string> tags;            // カードの分類タグリスト（回復やドローなど）
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
    // public CardTag tagsEnum;             // 文字列から変換後のカードタグ（ビットフラグ/今後追加の可能性高）
    
    // ----------------------------------------------------------------------
    // Jsonデータ受信後に文字列から列挙型への変換を行うメソッド
    // カードタイプ、進化段階、ポケモンタイプの変換を行うように更新
    // ----------------------------------------------------------------------
    public void ConvertStringDataToEnums()
    {
        ConvertCardType();          // カードタイプの変換
        ConvertEvolutionStage();    // 進化段階の変換（ポケモンカードの場合のみ）
        ConvertPokemonType();       // ポケモンタイプの変換
        ConvertCardPack();          // カードパックの変換
    }

    // ----------------------------------------------------------------------
    // カードパック名を列挙型に変換するメソッド
    // JSON文字列形式のカードパック名を対応する列挙型に変換
    // ----------------------------------------------------------------------
    private void ConvertCardPack()
    {
        if (!string.IsNullOrEmpty(pack))
        {
            packEnum = EnumConverter.ToCardPack(pack);
        }
    }

    // ----------------------------------------------------------------------
    // ポケモンタイプを列挙型に変換するメソッド
    // JSON文字列形式のポケモンタイプを対応する列挙型に変換
    // ----------------------------------------------------------------------
    private void ConvertPokemonType()
    {
        if (!string.IsNullOrEmpty(type))
        {
            typeEnum = EnumConverter.ToPokemonType(type);
        }
    }

    // ----------------------------------------------------------------------
    // 進化段階を列挙型に変換するメソッド
    // ポケモンカードの場合のみ、JSON文字列形式の進化段階を変換
    // ----------------------------------------------------------------------
    private void ConvertEvolutionStage()
    {
        if (cardTypeEnum == CardType.非EX || cardTypeEnum == CardType.EX)
        {
            if (!string.IsNullOrEmpty(evolutionStage))
            {
                evolutionStageEnum = EnumConverter.ToEvolutionStage(evolutionStage);
            }
        }
    }

    // ----------------------------------------------------------------------
    // カードタイプを列挙型に変換するメソッド
    // JSON文字列形式のカードタイプを対応する列挙型に変換
    // ----------------------------------------------------------------------
    private void ConvertCardType()
    {
        if (!string.IsNullOrEmpty(cardType))
        {
            cardTypeEnum = EnumConverter.ToCardType(cardType);
        }
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