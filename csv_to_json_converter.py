import pandas as pd
import json

# ----------------------------------------------------------------------
def safe_int(value):
    try:
        return int(value)
    except (ValueError, TypeError):
        return 0

# ----------------------------------------------------------------------
def build_move(row, prefix):
    name = row.get(f"{prefix}Name")
    damage = row.get(f"{prefix}Damage")
    effect = row.get(f"{prefix}Effect")

    # 技名・効果どちらも無ければ技としては無効
    if pd.isna(name) and not isinstance(effect, str):
        return None

    return {
        "name": name if isinstance(name, str) else "",
        "damage": safe_int(damage),
        "effect": effect if isinstance(effect, str) else ""
    }

# ----------------------------------------------------------------------
def convert_csv_to_json(csv_path, output_path):
    df = pd.read_csv(csv_path)
    cards = []

    for _, row in df.iterrows():
        card = {
            "id": str(row["ID"]),
            "name": row["Name"],
            "cardType": row["CardType"] if isinstance(row["CardType"], str) else "",
            "evolutionStage": row["EvolutionStage"] if isinstance(row.get("EvolutionStage"), str) else "",
            "pack": row["Pack"] if isinstance(row.get("Pack"), str) else "",
            "hp": safe_int(row["HP"]),
            "type": row["Type"] if isinstance(row["Type"], str) else "",
            "weakness": row["Weakness"] if isinstance(row["Weakness"], str) else "",
            "retreatCost": safe_int(row["RetreatCost"]),
            "abilityName": row["AbilityName"] if isinstance(row.get("AbilityName"), str) else "",
            "abilityEffect": row["Ability"] if isinstance(row.get("Ability"), str) else "",
            "moves": [],
            "maxDamage": safe_int(row.get("MaxDamage")),
            "maxEnergyCost": safe_int(row.get("MaxEnergyCost")),
            "tags": row["Tags"].split(',') if isinstance(row["Tags"], str) else [],
            "imageKey": row["ImageKey"] if isinstance(row["ImageKey"], str) else ""
        }

        move1 = build_move(row, "Move1")
        move2 = build_move(row, "Move2")
        if move1:
            card["moves"].append(move1)
        if move2:
            card["moves"].append(move2)

        cards.append(card)

    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump({ "cards": cards }, f, ensure_ascii=False, indent=2)

    print(f"✅ JSONファイルを出力しました：{output_path}")

# ----------------------------------------------------------------------
# ▼ 実行部分 ▼
if __name__ == "__main__":
    import sys
    if len(sys.argv) < 2:
        print("❗️CSVファイルをドラッグ＆ドロップしてください")
        sys.exit(1)

    csv_path = sys.argv[1]
    output_path = "output.json"
    convert_csv_to_json(csv_path, output_path)
