using UnityEngine;

namespace Enum
{
    // ======================================================================
    // EnumConverter
    // Json文字列をEnumに変換するクラス
    // ======================================================================
    public static class EnumConverter
    {
        // ----------------------------------------------------------------------
        // 文字列をCardTypeに変換
        // ----------------------------------------------------------------------
        public static CardType ToCardType(string typeString)
        {
            switch (typeString)
            {
                case "非EX": return CardType.非EX;
                case "EX": return CardType.EX;
                case "サポート": return CardType.サポート;
                case "グッズ": return CardType.グッズ;
                case "ポケモンのどうぐ": return CardType.ポケモンのどうぐ;
                case "化石": return CardType.化石;
                default:
                    Debug.LogError($"❌ 未知のカードタイプ: {typeString}");
                    return default;
            }
        }

        // ----------------------------------------------------------------------
        // 文字列をEvolutionStageに変換
        // ----------------------------------------------------------------------
        public static EvolutionStage ToEvolutionStage(string stageString)
        {
            switch (stageString)
            {
                case "たね": return EvolutionStage.たね;
                case "1進化":
                case "１進化": return EvolutionStage.進化1;
                case "2進化":
                case "２進化": return EvolutionStage.進化2;
                default:
                    Debug.LogError($"❌ 未知の進化段階: {stageString}");
                    return default;
            }
        }

        // ----------------------------------------------------------------------
        // 文字列をPokemonTypeに変換
        // ----------------------------------------------------------------------
        public static PokemonType ToPokemonType(string typeString)
        {

            switch (typeString)
            {
                case "草": return PokemonType.草;
                case "炎": return PokemonType.炎;
                case "水": return PokemonType.水;
                case "雷": return PokemonType.雷;
                case "闘": return PokemonType.闘;
                case "超": return PokemonType.超;
                case "悪": return PokemonType.悪;
                case "鋼": return PokemonType.鋼;
                case "ドラゴン": return PokemonType.ドラゴン;
                case "無色": return PokemonType.無色;
                default:
                    Debug.LogError($"❌ 未知のポケモンタイプ: {typeString}");
                    return default;
            }
        }

        // ----------------------------------------------------------------------
        // 文字列をCardPackに変換
        // ----------------------------------------------------------------------
        public static CardPack ToCardPack(string packString)
        {
            switch (packString)
            {
                case "最強の遺伝子": return CardPack.最強の遺伝子;
                case "幻のいる島": return CardPack.幻のいる島;
                case "時空の激闘": return CardPack.時空の激闘;
                case "超克の光": return CardPack.超克の光;
                case "シャイニングハイ": return CardPack.シャイニングハイ;
                case "双天の守護者": return CardPack.双天の守護者;
                case "PROMO": return CardPack.PROMO;
                default:
                    Debug.LogError($"❌ 未知のカードパック: {packString}");
                    return default;
            }
        }

        // 今後追加の可能性高
        /*
        // ----------------------------------------------------------------------
        // 文字列のリストからCardTagsのフラグを生成
        // ----------------------------------------------------------------------
        public static CardTag ToCardTags(List<string> tagStrings)
        {
            if (tagStrings == null || tagStrings.Count == 0)
                return 0;

            CardTag result = 0;
            
            foreach (var tag in tagStrings)
            {
                switch (tag)
                {
                    // 状態異常
                    case "こんらん": result |= CardTag.こんらん; break;
                    case "やけど": result |= CardTag.やけど; break;
                    case "ねむり": result |= CardTag.ねむり; break;
                    case "まひ": result |= CardTag.まひ; break;
                    case "どく": result |= CardTag.どく; break;
                    // ダメージ関連
                    case "ランダム攻撃": result |= CardTag.ランダム攻撃; break;
                    case "ダメージ減少": result |= CardTag.ダメージ減少; break;
                    case "ダメージ無効化": result |= CardTag.ダメージ無効化; break;
                    case "反撃": result |= CardTag.反撃; break;
                    case "ダメージアップ": result |= CardTag.ダメージアップ; break;
                    case "リベンジ": result |= CardTag.リベンジ; break;
                    case "どくならUP": result |= CardTag.どくならUP; break;
                    case "相手のHPが減っているならUP": result |= CardTag.相手のHPが減っているならUP; break;
                    case "EXならUP": result |= CardTag.EXならUP; break;
                    case "自分のエネルギーが多いならUP": result |= CardTag.自分のエネルギーが多いならUP; break;
                    case "相手のエネルギーが多いならUP": result |= CardTag.相手のエネルギーが多いならUP; break;
                    case "回復": result |= CardTag.回復; break;
                    // エネルギー関連
                    case "エネルギー加速": result |= CardTag.エネルギー加速; break;
                    case "敵エネトラッシュ": result |= CardTag.敵エネトラッシュ; break;
                    case "エネルギー付け替え": result |= CardTag.エネルギー付け替え; break;
                    case "逃げエネ減少": result |= CardTag.逃げエネ減少; break;
                    // ベンチ関連
                    case "ベンチ攻撃":
                    case "ベンチ狙撃": result |= CardTag.ベンチ攻撃; break;
                    case "ベンチに逃げる": result |= CardTag.ベンチに逃げる; break;
                    case "敵ポケ入れ替え": result |= CardTag.敵ポケ入れ替え; break;
                    // 手札/山札関連
                    case "山札チェック": result |= CardTag.山札チェック; break;
                    case "手札入れ替え": result |= CardTag.手札入れ替え; break;
                    case "ドロー": result |= CardTag.ドロー; break;
                    case "サーチ": result |= CardTag.サーチ; break;
                    case "相手手札鑑賞": result |= CardTag.相手手札鑑賞; break;
                    case "味方を手札に戻す": result |= CardTag.味方を手札に戻す; break;
                    case "敵バトルポケ除去": result |= CardTag.敵バトルポケ除去; break;
                    // 妨害
                    case "かげふみ": result |= CardTag.かげふみ; break;
                    case "ワザ封じ": result |= CardTag.ワザ封じ; break;
                    case "サポート封じ": result |= CardTag.サポート封じ; break;
                    // その他
                    case "ワザコピー": result |= CardTag.ワザコピー; break;
                    case "コイン": result |= CardTag.コイン; break;
                    default:
                        Debug.LogWarning($"⚠️ 未知のタグ: {tag}");
                        break;
                }
            }
            
            return result;
        }
        */
    }
}