"""플레이 성격 지표 짝 비교·판정과 스테이지별 값. 설계 docs/2026-11-18-enemy-playstyle-consistency-design.md §5,
점검 docs/2026-11-18-enemy-playstyle-knobs-design.md §7(F1·F3).

사용:
  python -m sim.playstyle_compare compare <기준 playstyle 폴더> <새 값 폴더> [...]
  python -m sim.playstyle_compare stages <playstyle 폴더> [--md <파일>]

- 같은 판은 하나로 센다(F1): 조합(스테이지 × 상대) 안에서 기록이 같은 판은 씨앗이 가장 작은 것만 남긴다.
  짝 비교(드러남)는 기준·새 값이 **둘 다** 같은 짝을 하나로, 절대 기준·자연스러움·하한은 **새 값만으로** 같은 판을 하나로
  센다(2026-11-19 검토 M3: 짝 기준으로 세면 새 값의 같은 판이 여러 번 들어간다).
- 스테이지·상대 묶음은 검정하지 않고 "모은 값이 성격 쪽인가"만 본다. 모든 스테이지·모든 상대가 성격 쪽이어야 "늘 그러함".
- 하한(F3)은 승률이 아니라 판이 망가지지 않았나: 시간 초과 비율과 적이 한 번도 공격하지 않은 판. 승률은 보고만 한다.
- 절대 기준(F2, 2026-11-18 사용자 결정): 성격의 기준 지표가 **모든 스테이지에서** 정한 선을 만족해야 통과.
  안정 운영형은 피할 수 있었는데 노출 C2v ≤ 0.25(2026-11-19 사용자 결정, 손잡이 설계 §13.10·§14: 피할 칸이 없던 노출은
  스테이지 생김새이지 AI의 선택이 아니다).
- 자연스러움(2026-11-19 사용자 결정): 성격과 상관없이 모든 스테이지에서 제자리 D2 < 0.1,
  칠 수 없을 때 접근 D3 ≥ 0.75(싸움을 피하지 않는다, §12), 칠 수 있는데 안 침 D1 ≤ 0.15(공격을 피하지 않는다, §14).
- 하한의 "한 번도 공격 안 한 판"은 같은 씨앗의 기준에도 똑같이 있는 판이면 세지 않는다(§14: 판·상대의 생김새).
  판정 = 드러남(기준과 짝 비교) + 절대 기준 + 자연스러움 + 하한. 스테이지·상대별 "기준보다 성격 쪽" 수는 참고로만 낸다.
  판정 뒤에는 통과 후보의 판 셋을 턴마다 따라가 본 기록을 문서에 붙여야 통과로 적는다(§12, 도구 밖).
"""

from __future__ import annotations

import argparse
import csv
import math
import sys
from collections import defaultdict
from pathlib import Path

if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
    __package__ = "sim"

from .playstyle import FEW_ATTACKS, METRICS, ROLE_PARTS, STREAK_TURNS  # noqa: E402

# 드러남을 보는 지표. C의 잃은 적은 판 길이에 끌리지 않는 "적 턴당 잃은 적"(Q-PK6 A).
# 안정 운영형의 노출은 공격하지 않은 유닛의 것(2026-11-19 사용자: 공격해서 노출되는 것은 어쩔 수 없다).
PERSONAS = {"A 역할 정적형": ("Astar",), "B 집단 돌입형": ("B1", "B3"), "C 안정 운영형": ("C1t", "C2n")}
# 절대 기준: 성격 → (지표, 선). 방향은 METRICS(클수록 = 선 이상, 작을수록 = 선 이하). A는 아직 정하지 않았다.
# 안정 운영형은 공격하지 않은 유닛 가운데 잡히지 않는 칸으로 갈 수 있었던 유닛의 노출(C2v)을 본다(§14).
ABSOLUTE = {"B 집단 돌입형": ("B3", 0.6), "C 안정 운영형": ("C2v", 0.25)}
# 칠 수 없을 때 접근 D3의 선(None이면 값만 내고 판정하지 않는다). 0.75 = 지금 적(기준·행동 규칙 붙인 기준, 씨앗 0~7·8~15)의
# 가장 나쁜 스테이지 값(P22 0.78~0.81)보다 조금 아래: 지금 적만큼은 다가가야 한다. 후보 값의 스테이지 값을 보기 전에 정했다(손잡이 설계 §12.2).
D3_LINE = 0.75
# 칠 수 있는데 안 침 D1의 선. 0.15 = 새 기준(행동 규칙 + 고친 판 값)의 가장 나쁜 스테이지(0.10)보다 조금 위(§14).
D1_LINE = 0.15
# 자연스러움(성격 공통): 모든 스테이지에서 (지표, 선, 선과 같으면 못 미침인가).
# 제자리 D2 < 0.1(2026-11-19 사용자). 칠 수 없을 때 접근 D3 ≥ 선(싸움을 피하지 않는다, 손잡이 설계 §12).
# 칠 수 있는데 안 침 D1 ≤ 선(공격을 피하지 않는다, §14).
NATURAL = (("D2", 0.1, True), ("D3", D3_LINE, False), ("D1", D1_LINE, False))
TIMEOUT_CAP = 0.10       # 하한: 시간 초과 비율(F3)
ROLE_NAMES = [name for parts in ROLE_PARTS.values() for name in parts]


def load(folder: Path) -> dict:
    rows = {}
    with open(folder / "battles.csv", encoding="utf-8-sig") as f:
        for r in csv.DictReader(f):
            key = (r["stage"], int(r["seed"]), r["player"])
            if key in rows:
                raise SystemExit(f"같은 판이 두 번: {key} ({folder})")
            rows[key] = r
    return rows


def distinct_keys(keys: list, *tables: dict) -> list:
    """조합(스테이지 × 상대) 안에서 모든 표의 판 열쇠(game)가 같은 것은 씨앗이 가장 작은 것 하나만(F1)."""
    seen, kept = set(), []
    for key in sorted(keys):
        mark = (key[0], key[2]) + tuple(table[key]["game"] for table in tables)
        if mark not in seen:
            seen.add(mark)
            kept.append(key)
    return kept


# ---------------------------------------------------------------- 값
def value(row: dict, name: str) -> float | None:
    """판 하나의 지표 값. 분모가 0이면 None."""
    if name == "Astar":
        return float(row["Astar"]) if row["Astar"] not in ("", "None") else None
    den = float(row[f"{name}_den"])
    return float(row[f"{name}_num"]) / den if den else None


def pooled(rows: list[dict], name: str) -> float | None:
    """여러 판을 모은 값(분자 합 ÷ 분모 합). A*는 모은 역할 지표로 다시 계산한다(판마다의 평균이 아님)."""
    if name == "Astar":
        scores = []
        for parts in ROLE_PARTS.values():
            values = []
            for part in parts:
                v = pooled(rows, part)
                if v is not None:
                    values.append(v if METRICS[part][0] > 0 else 1 - v)
            if values:
                scores.append(sum(values) / len(values))
        return sum(scores) / len(scores) if scores else None
    num = sum(float(r[f"{name}_num"]) for r in rows)
    den = sum(float(r[f"{name}_den"]) for r in rows)
    return num / den if den else None


def sign_p(a: int, b: int) -> float:
    n = a + b
    if n == 0:
        return 1.0
    tail = sum(math.comb(n, i) for i in range(min(a, b) + 1)) / 2 ** n
    return min(1.0, 2 * tail)


# ---------------------------------------------------------------- 짝 비교
def pairs(base: dict, new: dict, keys: list, name: str) -> tuple[int, int, int]:
    direction = METRICS[name][0]
    toward = away = same = 0
    for k in keys:
        a, b = value(base[k], name), value(new[k], name)
        if a is None or b is None:
            continue
        diff = direction * (b - a)
        if diff > 1e-12:
            toward += 1
        elif diff < -1e-12:
            away += 1
        else:
            same += 1
    return toward, away, same


def grouped(base: dict, new: dict, keys: list, name: str, index: int) -> tuple[int, int, list]:
    """묶음(스테이지 index 0, 상대 index 2)마다 모은 값이 성격 쪽인 묶음 수, 묶음 수, 성격 쪽이 아닌 묶음 이름."""
    direction = METRICS[name][0]
    groups = defaultdict(list)
    for k in keys:
        groups[k[index]].append(k)
    toward, against = 0, []
    for label, ks in sorted(groups.items()):
        a = pooled([base[k] for k in ks], name)
        b = pooled([new[k] for k in ks], name)
        if a is not None and b is not None and direction * (b - a) > 1e-12:
            toward += 1
        else:
            against.append(label.replace("Combat_LD_", ""))
    return toward, len(groups), against


def absolute(rows: dict, keys: list, name: str, line: float, strict: bool = False) -> tuple[int, int, list]:
    """스테이지마다 모은 값이 선을 만족하는 스테이지 수, 스테이지 수, 못 미친 스테이지 [(이름, 값)].
    strict면 선과 같은 값은 못 미친 것으로 본다("미만")."""
    direction = METRICS[name][0]
    groups = defaultdict(list)
    for k in keys:
        groups[k[0]].append(k)
    met, missed = 0, []
    for label, ks in sorted(groups.items()):
        v = pooled([rows[k] for k in ks], name)
        gap = None if v is None else direction * (v - line)
        if gap is not None and (gap > 1e-12 if strict else gap >= -1e-12):
            met += 1
        else:
            missed.append((label.replace("Combat_LD_", ""), v))
    return met, len(groups), missed


def stage_values(rows: dict, keys: list, name: str) -> list:
    """스테이지마다 모은 값 [(이름, 값)]."""
    groups = defaultdict(list)
    for k in keys:
        groups[k[0]].append(k)
    return [(label.replace("Combat_LD_", ""), pooled([rows[k] for k in ks], name)) for label, ks in sorted(groups.items())]


def never_attacked(row: dict) -> bool:
    """적 턴이 있었는데 적이 한 번도 공격하지 않은 판."""
    return int(row["turns"]) > 0 and row["turns"] == row["idle_turns"]


def broken(rows: dict, keys: list) -> tuple[float, int, float, float, float, float]:
    """(시간 초과 비율, 적이 한 번도 공격하지 않은 판 수, 적 승률, 공격 없는 적 턴 비율, 거의 안 싸우고 진 판 비율,
    공격을 계속 피한 판 비율).
    거의 안 싸우고 진 판 = 플레이어가 이겼고 적이 판 전체에서 공격한 유닛 턴이 FEW_ATTACKS 이하(보고만).
    공격을 계속 피한 판 = 한 유닛이 STREAK_TURNS턴 이상 이어서 칠 수 있는데 안 친 판(보고만)."""
    if not keys:
        return 0.0, 0, 0.0, 0.0, 0.0, 0.0
    timeout = sum(rows[k]["outcome"] == "timeout" for k in keys) / len(keys)
    never = sum(never_attacked(rows[k]) for k in keys)
    win = sum(rows[k]["outcome"] == "enemy_win" for k in keys) / len(keys)
    turns = sum(int(rows[k]["turns"]) for k in keys)
    idle = sum(int(rows[k]["idle_turns"]) for k in keys) / turns if turns else 0.0
    meek = sum(rows[k]["outcome"] == "player_win" and int(rows[k]["attacks"]) <= FEW_ATTACKS for k in keys) / len(keys)
    streaky = sum(int(rows[k]["streak"]) >= STREAK_TURNS for k in keys) / len(keys)
    return timeout, never, win, idle, meek, streaky


def compare(base: dict, new: dict) -> dict:
    shared = sorted(set(base) & set(new))
    keys = distinct_keys(shared, base, new)          # 드러남: 기준·새 값이 둘 다 같은 짝은 하나로
    own = distinct_keys(shared, new)                 # 절대 기준·자연스러움·하한: 새 값만으로 같은 판은 하나로(M3)
    result = {"pairs": len(shared), "distinct": len(keys), "own": len(own), "metrics": {}, "personas": {}}
    for name in METRICS:
        t, w, s = pairs(base, new, keys, name)
        result["metrics"][name] = dict(base=pooled([base[k] for k in keys], name),
                                       new=pooled([new[k] for k in keys], name),
                                       toward=t, away=w, same=s, p=sign_p(t, w),
                                       stages=grouped(base, new, keys, name, 0),
                                       players=grouped(base, new, keys, name, 2))
    result["base_broken"] = broken(base, distinct_keys(shared, base))
    result["new_broken"] = broken(new, own)
    timeout = result["new_broken"][0]
    # 한 번도 공격 안 한 판은 같은 판(스테이지·씨앗·상대)의 기준에도 그랬으면 세지 않는다: 판·상대의 생김새다.
    result["fresh_never"] = sum(never_attacked(new[k]) and not never_attacked(base[k]) for k in own)
    floor = timeout <= TIMEOUT_CAP and result["fresh_never"] == 0
    # 자연스러움: 조건마다 (지표, 선, 선과 같으면 못 미침인가, 만족한 스테이지 수·스테이지 수·못 미친 곳).
    # 선이 None이면 판정하지 않고 값만 낸다.
    result["natural"] = [(name, line, strict, absolute(new, own, name, line, strict) if line is not None
                          else (None, None, stage_values(new, own, name)))
                         for name, line, strict in NATURAL]
    is_natural = all(line is None or met[0] == met[1] for _, line, _, met in result["natural"])
    for persona, names in PERSONAS.items():
        metrics = [result["metrics"][n] for n in names]
        shown = all(m["toward"] > m["away"] and m["p"] < 0.05 for m in metrics)
        steady = all(m["stages"][0] == m["stages"][1] and m["players"][0] == m["players"][1] for m in metrics)
        line = ABSOLUTE.get(persona)
        if line is None:
            met, passed = None, None        # 절대 기준 미정: 판정하지 않는다
        else:
            met = absolute(new, own, *line)
            passed = shown and met[0] == met[1] and floor and is_natural
        result["personas"][persona] = dict(shown=shown, steady=steady, floor=floor, absolute=met, passed=passed)
    return result


def fmt(v) -> str:
    return "-" if v is None else f"{v:.3f}"


def print_compare(folder: Path, res: dict) -> None:
    tb, nb, wb, ib, mb, sb = res["base_broken"]
    tn, nn, wn, in_, mn, sn = res["new_broken"]
    print(f"== {folder.name}  (짝 {res['pairs']} 중 서로 다른 짝 {res['distinct']}, 새 값의 서로 다른 판 {res['own']})")
    print(f"  하한: 시간 초과 {tb:.3f} → {tn:.3f}, 한 번도 공격 안 한 판 {nb} → {nn}(기준에 없던 판 {res['fresh_never']})  "
          f"| 보고만: 적 승률 {wb:.3f} → {wn:.3f}, 공격 없는 적 턴 {ib:.3f} → {in_:.3f}, "
          f"거의 안 싸우고 진 판 {mb:.3f} → {mn:.3f}, 공격을 계속 피한 판({STREAK_TURNS}턴 이상) {sb:.3f} → {sn:.3f}")
    for name, line, strict, met in res["natural"]:
        sign = ((">" if METRICS[name][0] > 0 else "<") if strict else ("≥" if METRICS[name][0] > 0 else "≤"))
        if line is None:
            values = [v for _, v in met[2] if v is not None]
            low = f"스테이지 최소 {min(values):.3f}, 최대 {max(values):.3f}" if values else "값 없음"
            print(f"  자연스러움: {METRICS[name][1]} {name} — 선 미정({low})")
            continue
        missed = ", ".join(f"{label} {fmt(v)}" for label, v in met[2]) or "없음"
        print(f"  자연스러움: {METRICS[name][1]} {name} {sign} {line} 스테이지 {met[0]}/{met[1]}  못 미친 곳: {missed}")
    for persona, verdict in res["personas"].items():
        met = verdict["absolute"]
        if met is None:
            print(f"  {persona}: 절대 기준 미정 (드러남 {verdict['shown']}, 하한 {verdict['floor']})")
            continue
        name, line = ABSOLUTE[persona]
        sign = "≥" if METRICS[name][0] > 0 else "≤"
        missed = ", ".join(f"{label} {fmt(v)}" for label, v in met[2]) or "없음"
        mark = "통과" if verdict["passed"] else "아님"
        print(f"  {persona}: {mark} (드러남 {verdict['shown']}, 절대 기준 {name} {sign} {line} 스테이지 {met[0]}/{met[1]}, "
              f"하한 {verdict['floor']})  못 미친 곳: {missed}")
    for name, m in res["metrics"].items():
        direction = "↑" if METRICS[name][0] > 0 else "↓"
        against = ",".join(m["stages"][2]) if m["stages"][2] else "없음"
        print(f"    {name:5} {METRICS[name][1]:20}{direction}  {fmt(m['base'])} → {fmt(m['new'])}  "
              f"짝 {m['toward']}/{m['away']}/{m['same']} (p {m['p']:.3f})  "
              f"스테이지 {m['stages'][0]}/{m['stages'][1]} 상대 {m['players'][0]}/{m['players'][1]}  반대·같음: {against}")


# ---------------------------------------------------------------- 스테이지별 값
STAGE_COLUMNS = ("Astar", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8",
                 "B1", "B2", "B3", "B4", "B5", "C1", "C1t", "C2", "C2n", "C2a", "C2v", "C3", "C4", "D1", "D2", "D3")


def stage_table(rows: dict) -> tuple[list[str], list[list]]:
    """스테이지마다(같은 판은 하나로) 지표를 모은 값. 마지막 세 줄은 전체·최소·최대."""
    keys = distinct_keys(list(rows), rows)
    groups = defaultdict(list)
    for k in keys:
        groups[k[0]].append(k)
    header = ["스테이지", "판"] + list(STAGE_COLUMNS)
    table = []
    for stage, ks in sorted(groups.items()):
        table.append([stage.replace("Combat_LD_", ""), len(ks)] + [pooled([rows[k] for k in ks], n) for n in STAGE_COLUMNS])
    body = [row[2:] for row in table]
    table.append(["전체", len(keys)] + [pooled([rows[k] for k in keys], n) for n in STAGE_COLUMNS])
    for label, pick in (("최소", min), ("최대", max)):
        table.append([label, ""] + [pick((r[i] for r in body if r[i] is not None), default=None)
                                   for i in range(len(STAGE_COLUMNS))])
    return header, table


def markdown(header: list[str], table: list[list]) -> str:
    def cell(v):
        if v is None:
            return "-"
        return f"{v:.2f}" if isinstance(v, float) else str(v)
    lines = ["| " + " | ".join(header) + " |", "|" + "---|" * len(header)]
    lines += ["| " + " | ".join(cell(v) for v in row) + " |" for row in table]
    return "\n".join(lines)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="플레이 성격 지표 짝 비교·스테이지별 값")
    sub = parser.add_subparsers(dest="command", required=True)
    cmp_parser = sub.add_parser("compare")
    cmp_parser.add_argument("base", type=Path)
    cmp_parser.add_argument("new", type=Path, nargs="+")
    stage_parser = sub.add_parser("stages")
    stage_parser.add_argument("folder", type=Path)
    stage_parser.add_argument("--md", type=Path, default=None)
    args = parser.parse_args(argv)
    if args.command == "compare":
        base = load(args.base)
        for folder in args.new:
            print_compare(folder, compare(base, load(folder)))
    else:
        text = markdown(*stage_table(load(args.folder)))
        print(text)
        if args.md:
            args.md.write_text(text + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
