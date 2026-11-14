"""적 메인 노림 지표. 설계 docs/2026-11-14-enemy-main-focus-metrics-design.md.

저장된 전투 기록(run.py --log)을 처음부터 다시 돌려, 적 턴마다 메인에 대해
- 기회(M1): 턴 시작 판에서 무엇이 가능했나(메인을 줄일 수 있는 적, 혼자 최대 피해, 이번 턴 처치 가능)
- 결과(M2): 그 턴에 실제로 무엇을 했나(메인이 잃은 양, 메인 사망, 메인을 친 적, 닿았던 적이 대신 한 것)
를 나란히 세고 분류(M3)한다. 판단이 아니라 사실 계산이다(엔진에 물어본다). 엔진·정책 코드는 바꾸지 않는다.
또 공격하지 않은 적 유닛이 메인 쪽으로 걸었나(idle_* 열, near2_end 열)를 센다
(설계 docs/2026-11-14-planner-goal-pressure-design.md §1.1, Q-G6).

사용:
  python LLMTest/sim/main_focus.py <결과 폴더> [<결과 폴더> ...] [--jobs 5]   지표를 세고 표를 낸다
  python LLMTest/sim/main_focus.py <결과 폴더> ... --replay-only              다시 돌린 기록이 저장된 것과 같은지만
결과: LLMTest/results/main_focus/<UTC>/ (turns.csv, summary.json, mismatch.json, run.json)
"""

from __future__ import annotations

import argparse
import csv
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
import json
from multiprocessing import Pool
from pathlib import Path
import random
import statistics
import sys
import time

if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
    __package__ = "sim"

from .analysis import attack_results, distance_field  # noqa: E402
from .assets import GameAssets  # noqa: E402
from .planner import prediction_seed, signature  # noqa: E402
from .player import deployment  # noqa: E402
from .rules import Attack, EndTurn, Move, RuleError, apply, can_attack, start_battle  # noqa: E402
from .state import BattleState, new_battle  # noqa: E402
from .targeting import Side  # noqa: E402

RESULTS = Path(__file__).resolve().parents[1] / "results" / "main_focus"
KILL_BEAM = 16          # 처치 확인 탐색의 빔 폭(설계 P3)

# 분류(설계 M3). 기회 축 × 결과 축의 칸 이름.
NO_CHANCE = "no_chance"                         # 닿음 없음, 안 침
MISSED_ATTACK = "missed_attack"                 # 칠 수 있었는데 안 침(처치는 못 잡음)
SHORT = "short"                                 # 쳤지만 모자람(처치는 못 잡음)
MISSED_ATTACK_UNCONFIRMED = "missed_attack_unconfirmed"
SHORT_UNCONFIRMED = "short_unconfirmed"
MISSED_KILL = "missed_kill"                     # 잡을 수 있었는데(확인) 못 잡음
KILL = "kill"                                   # 잡음
OUTSIDE_HIT = "outside_hit"                     # 닿음 없음이었는데 메인이 피해를 입음(추정 밖)
OUTSIDE_KILL = "outside_kill"                   # 처치 못 잡음 추정이었는데 잡음(추정 밖)
CATEGORIES = (NO_CHANCE, MISSED_ATTACK, MISSED_ATTACK_UNCONFIRMED, SHORT, SHORT_UNCONFIRMED, MISSED_KILL, KILL,
              OUTSIDE_HIT, OUTSIDE_KILL)


# ---------------------------------------------------------------- 메인이 잃은 양
def toughness(state: BattleState, uid: int) -> int:
    """메인의 HP + 보호막(죽었으면 0). 메인이 잃은 양은 이 값의 차이다(설계 P2)."""
    unit = state.units[uid]
    return unit.hp + unit.shield if unit.alive else 0


def prediction_root(state: BattleState) -> BattleState:
    """계획 AI 예측과 같은 씨앗으로 난수를 맞춘 복사본(적 AI가 볼 수 있었던 것과 같은 조건, 설계 §3)."""
    root = state.clone()
    root.rng = random.Random(prediction_seed(state, Side.ENEMY))
    return root


# ---------------------------------------------------------------- M1 기회
@dataclass
class Opportunity:
    reach: dict[int, int] = field(default_factory=dict)     # 메인을 줄일 수 있는 적 uid → 혼자 최대로 줄이는 양
    solo_kill: bool = False                                  # 혼자 잡을 수 있는 적이 있나
    kill: str = "none"                                       # none(못 잡음) / confirmed(확인) / unconfirmed(미확인)
    nearest: int | None = None                               # 메인까지 걷는 거리: 가장 가까운 적
    near3: float | None = None                               # 가까운 셋의 평균
    seconds: float = 0.0

    @property
    def total(self) -> int:
        return sum(self.reach.values())


def _main_options(state: BattleState, uid: int, main_uid: int) -> list[tuple[int, BattleState]]:
    """그 적 유닛의 공격 조합 중 메인을 줄이는 것: [(줄인 양, 그 뒤 판)]. 엔진으로 돌린다."""
    unit = state.units[uid]
    if not unit.alive or not can_attack(state, unit):
        return []
    before = toughness(state, main_uid)
    found = []
    for _, after in attack_results(state, unit):
        lost = before - toughness(after, main_uid)
        if lost > 0:
            found.append((lost, after))
    return found


def kill_search(root: BattleState, uids: list[int], main_uid: int, beam: int = KILL_BEAM) -> bool:
    """닿는 적만으로 이번 턴에 메인을 잡는 순서가 있나(설계 §3). 찾으면 True, 못 찾으면 False(= 미확인).
    단계마다 아직 쓰지 않은 유닛 하나의 메인을 줄이는 공격을 붙이고, 메인이 잃은 양 상위 beam개만 남긴다."""
    start = toughness(root, main_uid)
    frontier = [(root, frozenset(uids))]
    while frontier:
        children: dict[tuple, tuple[int, int, BattleState, frozenset]] = {}
        for state, remaining in frontier:
            for uid in remaining:
                for _, after in _main_options(state, uid, main_uid):
                    if not after.units[main_uid].alive:
                        return True
                    key = signature(after)
                    if key not in children:
                        rest = remaining - {uid}
                        children[key] = (start - toughness(after, main_uid), len(rest), after, rest)
        ranked = sorted(children.values(), key=lambda c: (c[0], c[1]), reverse=True)[:beam]
        frontier = [(state, rest) for _, _, state, rest in ranked if rest]
    return False


def opportunity(state: BattleState) -> Opportunity:
    """적 턴 시작 판에서 메인에 대해 가능했던 것(설계 M1)."""
    started = time.perf_counter()
    root = prediction_root(state)
    main = root.main_character()
    result = Opportunity()
    if main is None or not main.alive:
        return result
    for unit in sorted(root.living(Side.ENEMY), key=lambda u: u.uid):
        options = _main_options(root, unit.uid, main.uid)
        if options:
            result.reach[unit.uid] = max(lost for lost, _ in options)
            result.solo_kill |= any(not after.units[main.uid].alive for _, after in options)
    # 처치 가능: 피해 합이 메인 HP에 못 미치면 못 잡음으로 본다(보호막은 빼고 거른다 — 못 잡음으로 잘못 거르지 않게).
    if result.solo_kill:
        result.kill = "confirmed"
    elif result.total >= main.hp and len(result.reach) > 1:
        result.kill = "confirmed" if kill_search(root, sorted(result.reach), main.uid) else "unconfirmed"
    distances = distance_field(root, [main.cell])
    near = sorted(distances[u.cell] for u in root.living(Side.ENEMY) if u.cell in distances)
    if near:
        result.nearest = near[0]
        result.near3 = round(statistics.mean(near[:3]), 2)
    result.seconds = round(time.perf_counter() - started, 4)
    return result


# ---------------------------------------------------------------- M2 결과
@dataclass
class TurnResult:
    main_lost: int = 0
    main_killed: bool = False
    hitters: list[int] = field(default_factory=list)        # 그 턴에 메인을 맞힌 적 uid
    instead: dict[int, str] = field(default_factory=dict)    # 닿았던 적 중 메인을 안 친 유닛이 대신 한 것


def _hits_of(entry: dict) -> list[dict]:
    if entry["type"] in ("attack", "self_destruct"):
        return entry["hits"]
    if entry["type"] == "kill_chain":
        return [entry["hit"]]
    return []


def turn_result(start: BattleState, end: BattleState, segment: list[dict], reach: dict[int, int]) -> TurnResult:
    """그 적 턴에 실제로 한 것(설계 M2). start = 턴 시작 판, end = 적 행동이 끝난 판(턴 끝내기 전), segment = 그 사이 사건."""
    main = start.main_character()
    result = TurnResult()
    if main is None:
        return result
    result.main_lost = toughness(start, main.uid) - toughness(end, main.uid)
    result.main_killed = main.alive and not end.units[main.uid].alive
    players = {u.uid for u in start.units.values() if u.side is Side.PLAYER}
    hitters, acts = [], {}
    for entry in segment:
        uid = entry.get("uid")
        hits = _hits_of(entry)
        if any(h.get("target") == main.uid and h.get("kind", "unit") == "unit" for h in hits) and uid not in hitters:
            hitters.append(uid)
        if entry["type"] == "attack":          # 유닛 하나는 한 턴에 한 번 친다(이동 뒤 공격이면 덮어씀)
            others = [h for h in hits if h.get("kind") == "unit" and h["target"] in players and h["target"] != main.uid]
            acts[uid] = "kill_other" if any(h["killed"] for h in others) else "hit_other" if others else "attack_other"
        elif entry["type"] == "move":
            acts.setdefault(uid, "move_only")
    result.hitters = hitters
    for uid in reach:
        if uid in hitters:
            continue
        if not end.units[uid].alive and uid not in acts:
            result.instead[uid] = "died"
        else:
            result.instead[uid] = acts.get(uid, "wait")
    return result


@dataclass
class IdleWalk:
    """공격하지 않은 적 유닛의 메인 쪽 걸음(설계 docs/2026-11-14-planner-goal-pressure-design.md §1.1, Q-G6).
    거리는 메인 칸까지 걷는 거리(distance_field, 유닛은 지나간다고 봄). 시작은 턴 시작 판의 메인 칸, 끝은 끝 판의 메인 칸."""
    units: int = 0       # 턴 시작에 행동할 수 있었고(공격할 수 있는 종류) 공격하지 않았으며 턴 끝까지 살아 거리를 잴 수 있는 적
    toward: int = 0      # 그중 메인과의 거리가 줄어든 적
    still: int = 0       # 그대로인 적
    away: int = 0        # 늘어난 적
    closer: int = 0      # 줄어든 칸 합(늘어나면 음수로 더함)
    near2_end: int = 0   # 턴 끝에 메인까지 걷는 거리가 이동력 × 2 + 사거리 이하인 적(대략 다음다음 턴까지 칠 수 있는 적)


def idle_walk(start: BattleState, end: BattleState, segment: list[dict]) -> IdleWalk:
    """그 적 턴에 공격하지 않은 적 유닛이 메인 쪽으로 걸었나(사실 계산, 판단 아님)."""
    result = IdleWalk()
    main = start.main_character()
    if main is None or not main.alive:
        return result
    before = distance_field(start, [main.cell])
    end_main = end.units[main.uid]
    after = distance_field(end, [end_main.cell]) if end_main.alive else {}
    acted = {entry["uid"] for entry in segment if entry["type"] in ("attack", "self_destruct")}
    for unit in start.living(Side.ENEMY):
        if unit.use.value not in ("available", "moved") or not unit.defn.can_attack or unit.uid in acted:
            continue
        moved = end.units[unit.uid]
        if not moved.alive or unit.cell not in before or moved.cell not in after:
            continue
        gained = before[unit.cell] - after[moved.cell]
        result.units += 1
        result.closer += gained
        if gained > 0:
            result.toward += 1
        elif gained == 0:
            result.still += 1
        else:
            result.away += 1
    result.near2_end = sum(1 for unit in end.living(Side.ENEMY)
                           if unit.cell in after and after[unit.cell] <= 2 * unit.defn.move + unit.defn.attack_range)
    return result


def classify(opp: Opportunity, result: TurnResult) -> str:
    """기회 × 결과 → 분류(설계 M3)."""
    if result.main_killed:
        if opp.kill in ("confirmed", "unconfirmed"):
            return KILL
        return OUTSIDE_KILL
    hit = result.main_lost > 0
    if not opp.reach:
        return OUTSIDE_HIT if hit else NO_CHANCE
    if opp.kill == "confirmed":
        return MISSED_KILL
    if opp.kill == "unconfirmed":
        return SHORT_UNCONFIRMED if hit else MISSED_ATTACK_UNCONFIRMED
    return SHORT if hit else MISSED_ATTACK


# ---------------------------------------------------------------- 기록 다시 돌리기
def action_from_json(data: dict):
    """run.py가 남긴 행동 기록 한 줄 → 엔진 행동(JSON이 튜플을 목록으로 바꾼 것을 되돌린다)."""
    if data["type"] == "move":
        return Move(data["uid"], tuple(tuple(cell) for cell in data["path"]))
    if data["type"] == "attack":
        aim = data["aim"]
        return Attack(data["uid"], tuple(aim) if isinstance(aim, list) else aim)
    if data["type"] == "end_turn":
        return EndTurn()
    raise ValueError(f"모르는 행동 기록: {data}")


@dataclass
class EnemyTurn:
    round: int
    start: BattleState          # 적 턴 시작 판(턴 시작·증원 뒤, 적 행동 전)
    end: BattleState            # 적 행동이 끝난 판(턴 끝내기 전, 전투가 끝났으면 그 판)
    segment: list               # 그 사이 사건


def replay(initial: BattleState, actions: list[dict]) -> tuple[list[EnemyTurn], BattleState]:
    """처음 판에서 행동 기록을 차례로 다시 돌린다. 적 턴마다 시작 판·끝 판·사건을 모은다."""
    state, turns, current = initial, [], None
    if state.turn_side is Side.ENEMY and state.outcome is None:
        current = (state, len(state.log))
    for entry in actions:
        action = action_from_json(entry["action"])
        if current is not None and isinstance(action, EndTurn):
            turns.append(EnemyTurn(current[0].round, current[0], state, state.log[current[1]:]))
            current = None
        state = apply(state, action)
        if current is not None and state.outcome is not None:
            turns.append(EnemyTurn(current[0].round, current[0], state, state.log[current[1]:]))
            current = None
        if isinstance(action, EndTurn) and state.turn_side is Side.ENEMY and state.outcome is None:
            current = (state, len(state.log))
    return turns, state


def same_log(state: BattleState, saved: list) -> bool:
    """다시 돌린 사건 기록이 저장된 것과 같은가(양쪽을 JSON 모양으로 맞춰 비교 — 튜플과 목록을 같게 본다)."""
    return json.loads(json.dumps(state.log)) == json.loads(json.dumps(saved))


def replay_checked(initial: BattleState, actions: list[dict], saved_log: list) -> tuple[list[EnemyTurn], bool]:
    """다시 돌리고 저장된 사건 기록과 비교한다. 엔진이 그 행동을 거절하면(엔진이 바뀐 뒤의 옛 기록) 다름으로 본다."""
    try:
        turns, final = replay(initial, actions)
    except RuleError:
        return [], False
    return turns, same_log(final, saved_log)


# ---------------------------------------------------------------- 결과 폴더 읽기
_ASSETS: GameAssets | None = None


def _assets() -> GameAssets:
    global _ASSETS
    if _ASSETS is None:
        _ASSETS = GameAssets()
    return _ASSETS


def initial_state(record: dict) -> BattleState:
    """run.py run_battle과 같은 처음 판(스테이지·아군 구성·배치·씨앗)."""
    assets = _assets()
    stage = assets.stage(record["stage"])
    loadout = assets.player_loadout()
    return start_battle(new_battle(stage, loadout, deployment(stage, loadout), seed=record["seed"]))


def _short(name: str) -> str:
    return name.replace("enemy-", "").replace("player-", "")


def analyze_file(job: tuple[str, bool]) -> dict:
    """전투 기록 파일 하나 → {"file", "same", "rows"}. 다시 돌린 기록이 다르면 행을 내지 않는다(설계 §4)."""
    path, replay_only = job
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    record, actions = data["record"], data["actions"]
    turns, same = replay_checked(initial_state(record), actions[:-1], actions[-1]["log"])
    if not same:
        return {"file": path, "same": False, "rows": []}
    if replay_only:
        return {"file": path, "same": True, "rows": []}
    rows = []
    for turn in turns:
        opp = opportunity(turn.start)
        result = turn_result(turn.start, turn.end, turn.segment, opp.reach)
        walk = idle_walk(turn.start, turn.end, turn.segment)
        main = turn.start.main_character()
        rows.append({
            "file": Path(path).name, "stage": record["stage"], "seed": record["seed"],
            "enemy": _short(record["enemy"]), "player": _short(record["player"]), "outcome": record["outcome"],
            "round": turn.round, "main_hp": main.hp, "main_shield": main.shield,
            "reach_units": len(opp.reach), "reach_total": opp.total, "solo_kill": opp.solo_kill, "kill": opp.kill,
            "nearest": opp.nearest, "near3": opp.near3,
            "main_lost": result.main_lost, "main_killed": result.main_killed, "hitters": len(result.hitters),
            "instead": json.dumps(sorted(result.instead.values()), ensure_ascii=False),
            "category": classify(opp, result),
            "idle_units": walk.units, "idle_toward": walk.toward, "idle_still": walk.still, "idle_away": walk.away,
            "idle_closer": walk.closer, "near2_end": walk.near2_end, "seconds": opp.seconds,
        })
    return {"file": path, "same": True, "rows": rows}


def battle_files(folders: list[Path]) -> list[Path]:
    files = []
    for folder in folders:
        found = sorted((folder / "battles").glob("*.json"))
        if not found:
            raise FileNotFoundError(f"전투 기록이 없습니다(run.py --log로 돌린 폴더인가요): {folder}")
        files += found
    return files


# ---------------------------------------------------------------- 요약
def _rate(part: int, whole: int) -> float | None:
    return round(part / whole, 3) if whole else None


def summarize(rows: list[dict]) -> list[dict]:
    """조합(적 × 플레이어)마다 분류 비율과 전환율, 적 승 판과 아닌 판의 차이(설계 §5 표 1·2·3)."""
    groups: dict[tuple, list[dict]] = {}
    for row in rows:
        groups.setdefault((row["enemy"], row["player"]), []).append(row)
    out = []
    for (enemy, player), items in sorted(groups.items()):
        n = len(items)
        count = {c: sum(r["category"] == c for r in items) for c in CATEGORIES}
        reachable = [r for r in items if r["reach_units"] > 0]
        confirmed = [r for r in items if r["kill"] == "confirmed"]
        instead: dict[str, int] = {}
        for r in items:
            if r["category"] in (MISSED_ATTACK, MISSED_ATTACK_UNCONFIRMED, MISSED_KILL, SHORT, SHORT_UNCONFIRMED):
                for what in json.loads(r["instead"]):
                    instead[what] = instead.get(what, 0) + 1
        by_outcome = {}
        for label, pick in (("enemy_win", lambda r: r["outcome"] == "enemy_win"),
                            ("other", lambda r: r["outcome"] != "enemy_win")):
            part = [r for r in items if pick(r)]
            battles = {r["file"] for r in part}
            first = {}
            for r in part:
                if r["kill"] != "none":
                    first[r["file"]] = min(first.get(r["file"], 99), r["round"])
            near = [r["nearest"] for r in part if r["nearest"] is not None]
            by_outcome[label] = {
                "battles": len(battles), "enemy_turns": len(part),
                "confirmed_turns_per_battle": round(sum(r["kill"] == "confirmed" for r in part) / len(battles), 2)
                if battles else None,
                "reach_turns_per_battle": round(sum(r["reach_units"] > 0 for r in part) / len(battles), 2)
                if battles else None,
                "battles_with_kill_chance": len(first),
                "first_kill_chance_round": round(statistics.mean(first.values()), 2) if first else None,
                "nearest": round(statistics.mean(near), 2) if near else None,
            }
        idle = sum(r.get("idle_units", 0) for r in items)
        near2 = [r.get("near2_end", 0) for r in items]
        idle_walk_summary = {       # 공격하지 않은 적 유닛의 걸음(설계 goal-pressure §1.1)
            "units": idle,
            "toward": _rate(sum(r.get("idle_toward", 0) for r in items), idle),
            "still": _rate(sum(r.get("idle_still", 0) for r in items), idle),
            "away": _rate(sum(r.get("idle_away", 0) for r in items), idle),
            "closer_per_unit": round(sum(r.get("idle_closer", 0) for r in items) / idle, 2) if idle else None,
            "near2_end_per_turn": round(statistics.mean(near2), 2) if near2 else None,
            "near2_two_or_more": _rate(sum(x >= 2 for x in near2), n),
        }
        out.append({
            "enemy": enemy, "player": player, "battles": len({r["file"] for r in items}), "enemy_turns": n,
            "share": {c: _rate(count[c], n) for c in CATEGORIES},
            "kill_conversion": _rate(sum(r["main_killed"] for r in confirmed), len(confirmed)),
            "hit_when_reachable": _rate(sum(r["main_lost"] > 0 for r in reachable), len(reachable)),
            "reach_units_when_reachable": round(statistics.mean(r["reach_units"] for r in reachable), 2)
            if reachable else None,
            "instead": instead,
            "idle_walk": idle_walk_summary,
            "by_outcome": by_outcome,
            "seconds_per_turn": round(statistics.mean(r["seconds"] for r in items), 3) if items else None,
        })
    return out


LABELS = {NO_CHANCE: "기회 없음", MISSED_ATTACK: "놓친 공격", MISSED_ATTACK_UNCONFIRMED: "놓친 공격(미확인)",
          SHORT: "쳤지만 모자람", SHORT_UNCONFIRMED: "모자람(미확인)", MISSED_KILL: "놓친 처치", KILL: "잡음",
          OUTSIDE_HIT: "추정 밖 피해", OUTSIDE_KILL: "추정 밖 처치"}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="저장된 전투 기록에서 적 메인 노림 지표를 센다")
    parser.add_argument("folders", nargs="+", type=Path, help="run.py --log 결과 폴더")
    parser.add_argument("--jobs", type=int, default=5)
    parser.add_argument("--replay-only", action="store_true", help="다시 돌린 기록이 저장된 것과 같은지만 본다")
    parser.add_argument("--out", type=Path, default=None)
    args = parser.parse_args(argv)
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")

    files = battle_files(args.folders)
    started = time.perf_counter()
    jobs = [(str(path), args.replay_only) for path in files]
    if args.jobs > 1:
        with Pool(args.jobs) as pool:
            results = pool.map(analyze_file, jobs, chunksize=4)
    else:
        results = [analyze_file(job) for job in jobs]
    mismatched = [r["file"] for r in results if not r["same"]]
    rows = [row for r in results for row in r["rows"]]
    seconds = round(time.perf_counter() - started, 1)
    print(f"판 {len(files)}: 다시 돌린 기록이 같음 {len(files) - len(mismatched)}, 다름 {len(mismatched)} ({seconds}초)")
    for path in mismatched:
        print(f"  다름: {path}")

    out = args.out or RESULTS / datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S.%fZ")
    out.mkdir(parents=True, exist_ok=False)
    (out / "mismatch.json").write_text(json.dumps(mismatched, ensure_ascii=False, indent=1), encoding="utf-8")
    (out / "run.json").write_text(json.dumps({"folders": [str(f) for f in args.folders], "battles": len(files),
                                              "replay_only": args.replay_only, "mismatched": len(mismatched),
                                              "kill_beam": KILL_BEAM, "seconds": seconds},
                                             ensure_ascii=False, indent=2), encoding="utf-8")
    if not args.replay_only:
        if rows:
            with (out / "turns.csv").open("w", newline="", encoding="utf-8-sig") as handle:
                writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
                writer.writeheader()
                writer.writerows(rows)
        summary = summarize(rows)
        (out / "summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
        for row in summary:
            share = "  ".join(f"{LABELS[c]} {v:.2f}" for c, v in row["share"].items() if v)
            print(f"\n적 {row['enemy']} × 플레이어 {row['player']}: 판 {row['battles']}, 적 턴 {row['enemy_turns']}")
            print(f"  {share}")
            print(f"  처치 확인 턴 중 잡은 비율 {row['kill_conversion']}, 닿는 턴 중 메인을 친 비율 {row['hit_when_reachable']}")
            walk = row["idle_walk"]
            print(f"  공격 안 한 적 {walk['units']}: 메인 쪽 {walk['toward']} / 제자리 {walk['still']} / 멀어짐 {walk['away']}, "
                  f"평균 {walk['closer_per_unit']}칸, 턴 끝 2턴 거리 안 {walk['near2_end_per_turn']}")
    print(f"\n결과: {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
