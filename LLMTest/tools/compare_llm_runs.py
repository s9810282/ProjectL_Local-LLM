"""run_llm_compare.py 실행 여러 개의 LLM 선택을 원래 후보 ID로 나란히 비교한다(순서 불변·반복 일관성 확인).

사용법:
  python LLMTest/tools/compare_llm_runs.py <실행 폴더> <실행 폴더> [...]
첫 폴더를 기준으로, 같은 상황·지시문에서 원래 ID 선택이 모든 실행에서 같은지 표시한다.
"""
import argparse
import json
import sys
from collections import Counter
from pathlib import Path


def load(run_dir):
    run = json.loads((Path(run_dir) / "run.json").read_text(encoding="utf-8"))
    rows = {(r["situation"], r["policy"]): r for r in run["rows"]}
    order = run.get("candidateOrder", {"order": "file"})
    label = run.get("promptVariant", "basic") + "/" + order["order"] + (f":{order['seed']}" if "seed" in order else "")
    return label, rows


def main():
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("runs", nargs="+", type=Path)
    args = parser.parse_args()
    runs = [load(path) for path in args.runs]
    keys = sorted(set().union(*(rows for _, rows in runs)))
    print("상황 지시문 | " + " | ".join(f"{i + 1}:{label}" for i, (label, _) in enumerate(runs)) + " | 같음")
    stable = Counter()
    total = Counter()
    for key in keys:
        cells = []
        choices = set()
        for _, rows in runs:
            row = rows.get(key)
            if row is None or row["status"] != "ok":
                cells.append("-" if row is None else row["status"])
                choices.add(None)
                continue
            mark = "O" if row["match_expected"] else "X"
            cells.append(f"{row['llm_choice']}({row['llm_alias']},{mark})")
            choices.add(row["llm_choice"])
        same = len(choices) == 1 and None not in choices
        total[key[1]] += 1
        stable[key[1]] += same
        print(f"{key[0]} {key[1]:7} | " + " | ".join(cells) + f" | {'같음' if same else '다름'}")
    print()
    for policy in sorted(total):
        print(f"[{policy}] 모든 실행에서 같은 선택 {stable[policy]}/{total[policy]}")
    for i, (label, rows) in enumerate(runs):
        positions = Counter(r["llm_alias"] for r in rows.values() if r["status"] == "ok" and r["candidates"] > 1)
        expected = sum(1 for r in rows.values() if r["status"] == "ok" and r["candidates"] > 1 and r["match_expected"])
        print(f"{i + 1}:{label} 고른 위치(후보 2개 이상) {dict(sorted(positions.items()))} · 기대 일치 {expected}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
