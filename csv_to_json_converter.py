import pandas as pd
import json

def safe_int(value):
    try:
        return int(value)
    except (ValueError, TypeError):
        return 0

def parse_cost(cost_str):
    if pd.isna(cost_str):
        return {}
    result = {}
    for part in str(cost_str).split(','):
        part = part.strip()
        if ':' in part:
            energy_type, amount = part.split(':')
            try:
                result[energy_type.strip()] = int(amount.strip())
            except ValueError:
                continue
    return result

def build_move(row, prefix):
    name = row.get(f"{prefix}Name")
    damage = row.get(f"{prefix}Damage")
    effect = row.get(f"{prefix}Effect")
    cost = parse_cost(row.get(f"{prefix}Cost"))

    if pd.isna(name):
        return None

    return {
        "name": name,
        "damage": safe_int(damage),
        "effect": effect if isinstance(effect, str) else "",
        "cost": cost
    }

def convert_csv_to_json(csv_path, output_path):
    df = pd.read_csv(csv_path)
    cards = []

    for _, row in df.iterrows():
        card = {
            "id": str(row["ID"]),
            "name": row["Name"],
            "cardType": row["CardType"] if isinstance(row["CardType"], str) else "",
            "imageKey": row["ImageKey"] if isinstance(row["ImageKey"], str) else "",
            "tags": row["Tags"].split(',') if isinstance(row["Tags"], str) else [],
            "hp": safe_int(row["HP"]),
            "type": row["Type"] if isinstance(row["Type"], str) else "",
            "weakness": row["Weakness"] if isinstance(row["Weakness"], str) else "",
            "retreatCost": safe_int(row["RetreatCost"]),
            "moves": []
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

# ▼ ここが「実行部分」 ▼
if __name__ == "__main__":
    import sys
    if len(sys.argv) < 2:
        print("❗️CSVファイルをドラッグ＆ドロップしてください")
        sys.exit(1)

    csv_path = sys.argv[1]
    output_path = "output.json"
    convert_csv_to_json(csv_path, output_path)

