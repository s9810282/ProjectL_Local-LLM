"""전투를 끝까지 돌리고 지표를 남긴다. 설계 §A6.

사용:
  python LLMTest/sim/run.py                                   일반 15판 × 시드 3, 돌진형·신중형, 단순 탐욕 적
  python LLMTest/sim/run.py --stages Combat_LD_P20 --seeds 1 --log   한 판, 행동 기록까지
결과: LLMTest/results/sim/<UTC>/ (battles.csv, summary.json, run.json, --log면 battles/*.json)
"""

from __future__ import annotations

import argparse
import csv
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
import json
from pathlib import Path
import statistics
import sys
import time

if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
    __package__ = "sim"

from .assets import GameAssets, StageDef  # noqa: E402
from .enemy import make_enemy_policy  # noqa: E402
from .player import deployment, make_player_policy  # noqa: E402
from .planner import PlannerPolicy  # noqa: E402
from .policy import Policy  # noqa: E402
from .rules import Attack, EndTurn, Move, apply, start_battle, unknown_traits  # noqa: E402
from .state import ActionUse, BattleState, new_battle  # noqa: E402
from .targeting import Side  # noqa: E402

RESULTS = Path(__file__).resolve().parents[1] / "results" / "sim"
DEFAULT_STAGES = [f"Combat_LD_P{n}" for n in range(20, 35)]
MAX_ACTIONS_PER_TURN = 60


@dataclass
class BattleRecord:
    stage: str
    seed: int
    player: str
    enemy: str
    outcome: str = "timeout"                  # player_win / enemy_win / timeout
    rounds: int = 0
    gimmicks_off: str = ""
    player_units_lost: int = 0
    player_hp_lost: int = 0
    main_hp: int = 0
    enemy_units_lost: int = 0
    enemy_spawned: int = 0
    hq_hp: int = -1
    first_attack_round: int = -1              # 누구든 처음 공격한 라운드
    first_enemy_attack_round: int = -1
    enemy_hold_ratio: float = 0.0             # 적 턴마다 행동할 수 있었는데 아무것도 안 한 유닛 비율의 평균
    hold_reasons: dict = field(default_factory=dict)
    mean_gap: float = 0.0                     # 라운드 끝마다 적 유닛과 가장 가까운 플레이어 유닛 거리의 평균
    falls: dict = field(default_factory=dict)  # 편마다 추락 사망 수
    friendly_fire: dict = field(default_factory=dict)   # 편마다 자기편에 준 피해
    planner: dict = field(default_factory=dict)  # 계획 AI 편마다 계획·다시 매김·뒤집힘 수와 시간
    seconds: float = 0.0
    anomalies: list = field(default_factory=list)


def _gap(state: BattleState) -> float | None:
    players = [u.cell for u in state.living(Side.PLAYER)]
    enemies = [u.cell for u in state.living(Side.ENEMY)]
    if not players or not enemies:
        return None
    return statistics.mean(min(abs(e[0] - p[0]) + abs(e[1] - p[1]) for p in players) for e in enemies)


_WARNED: set[tuple[str, str]] = set()


def unknown_trait_notes(stage: StageDef, loadout) -> list[str]:
    """이 판에 나올 수 있는 유닛(배치 적·증원 후보·아군 구성) 중 엔진이 모르는 특성. 설계 planner-engine-reply R6."""
    defs = [placement.unit for placement in stage.enemies] + [loadout.main, *loadout.subs]
    if stage.reinforcement is not None:
        defs += [defn for defn, _ in stage.reinforcement.pool]
    return [f"엔진이 모르는 특성: {name}({kind}) — 시뮬레이터에서 아무 일도 안 함"
            for name, kind in unknown_traits(defs)]


def run_battle(stage: StageDef, assets: GameAssets, player: Policy, enemy: Policy, seed: int,
               max_rounds: int = 20, keep_actions: bool = False) -> tuple[BattleRecord, list]:
    loadout = assets.player_loadout()
    state = start_battle(new_battle(stage, loadout, deployment(stage, loadout), seed=seed))
    record = BattleRecord(stage.name, seed, player.name, enemy.name, gimmicks_off=",".join(sorted(stage.gimmicks)))
    for note in unknown_trait_notes(stage, loadout):
        record.anomalies.append(note)
        if (stage.name, note) not in _WARNED:      # 콘솔에는 스테이지·특성마다 한 번
            _WARNED.add((stage.name, note))
            print(f"[경고] {stage.name}: {note}")
    started = time.perf_counter()
    actions_log, hold_ratios, gaps = [], [], []
    reasons: dict[str, int] = {}
    policies = {Side.PLAYER: player, Side.ENEMY: enemy}
    turn_actions = 0
    acted: set[int] = set()
    ready = {u.uid for u in state.living(Side.PLAYER) if u.use is ActionUse.AVAILABLE}
    while state.outcome is None and state.round <= max_rounds:
        side = state.turn_side
        action = policies[side].act(state)
        turn_actions += 1
        if turn_actions > MAX_ACTIONS_PER_TURN and not isinstance(action, EndTurn):
            record.anomalies.append(f"r{state.round} {side.value}: 한 턴 행동이 {MAX_ACTIONS_PER_TURN}개를 넘어 턴을 끝냄")
            action = EndTurn()
        if isinstance(action, Attack):
            record.first_attack_round = record.first_attack_round if record.first_attack_round > 0 else state.round
            if side is Side.ENEMY and record.first_enemy_attack_round < 0:
                record.first_enemy_attack_round = state.round
        if isinstance(action, (Move, Attack)):
            acted.add(action.uid)
        if keep_actions:
            actions_log.append({"round": state.round, "side": side.value, "action": _action_json(state, action)})
        if isinstance(action, EndTurn):
            if side is Side.ENEMY:
                idle = ready - acted
                hold_ratios.append(len(idle) / len(ready) if ready else 0.0)
                for uid, reason in policies[side].holds().items():
                    if uid in idle:
                        reasons[reason] = reasons.get(reason, 0) + 1
        before_round = state.round
        state = apply(state, action)
        if isinstance(action, EndTurn):
            turn_actions = 0
            acted = set()
            ready = {u.uid for u in state.living(state.turn_side) if u.use is ActionUse.AVAILABLE}
            if state.round != before_round:
                gap = _gap(state)
                if gap is not None:
                    gaps.append(gap)
    if state.outcome is not None:
        record.outcome = state.outcome.value
    record.rounds = min(state.round, max_rounds)
    _summarize_state(record, state)
    record.enemy_hold_ratio = round(statistics.mean(hold_ratios), 3) if hold_ratios else 0.0
    record.hold_reasons = reasons
    record.mean_gap = round(statistics.mean(gaps), 2) if gaps else 0.0
    for side, policy in policies.items():
        if isinstance(policy, PlannerPolicy):
            stats = policy.stats
            record.planner[side.value] = {"plans": stats.plans, "replans": stats.replans, "reranks": stats.reranks,
                                          "deep_evals": stats.deep_evals, "flips": stats.flips,
                                          "beam_steps": stats.beam_steps, "beam_nodes": stats.beam_nodes,
                                          "beam_results": stats.beam_results,
                                          "seconds": round(stats.seconds, 3),
                                          "deep_seconds": round(stats.deep_seconds, 3)}
    record.seconds = round(time.perf_counter() - started, 2)
    return record, actions_log + [{"log": state.log}] if keep_actions else []


def _summarize_state(record: BattleRecord, state: BattleState) -> None:
    players = [u for u in state.units.values() if u.side is Side.PLAYER]
    enemies = [u for u in state.units.values() if u.side is Side.ENEMY]
    record.player_units_lost = sum(1 for u in players if not u.alive)
    record.player_hp_lost = sum(u.max_hp - max(0, u.hp) for u in players)
    main = state.main_character()
    record.main_hp = max(0, main.hp) if main else 0
    record.enemy_units_lost = sum(1 for u in enemies if not u.alive)
    record.enemy_spawned = sum(1 for entry in state.log if entry["type"] == "reinforce")
    record.hq_hp = sum(s.hp for s in state.structures.values()) if state.structures else -1
    sides = {u.uid: u.side for u in state.units.values()}
    falls, friendly = {}, {}
    for entry in state.log:
        if entry["type"] == "fall":
            key = sides[entry["uid"]].value
            falls[key] = falls.get(key, 0) + 1
        elif entry["type"] == "attack":
            attacker = sides[entry["uid"]]
            for hit in entry["hits"]:
                same = (hit["kind"] == "unit" and sides.get(hit["target"]) is attacker) or \
                       (hit["kind"] == "structure" and attacker is Side.ENEMY)
                if same:
                    friendly[attacker.value] = friendly.get(attacker.value, 0) + hit["damage"]
    record.falls, record.friendly_fire = falls, friendly


def _action_json(state: BattleState, action) -> dict:
    if isinstance(action, Move):
        return {"type": "move", "unit": state.units[action.uid].name, "uid": action.uid, "path": list(action.path)}
    if isinstance(action, Attack):
        return {"type": "attack", "unit": state.units[action.uid].name, "uid": action.uid, "aim": action.aim}
    return {"type": "end_turn"}


SUMMARY_KEYS = ("outcome", "rounds", "first_enemy_attack_round", "enemy_hold_ratio", "mean_gap",
                "player_units_lost", "enemy_units_lost")


def _flip_ratio(records: list[BattleRecord], side: Side) -> float | None:
    """그 편 계획 AI가 다시 매긴 계획 중 빠른 V 1등과 다른 계획을 고른 비율. 다시 매김이 없으면 None."""
    reranks = sum(r.planner.get(side.value, {}).get("reranks", 0) for r in records)
    flips = sum(r.planner.get(side.value, {}).get("flips", 0) for r in records)
    return round(flips / reranks, 3) if reranks else None


def _beam_results(records: list[BattleRecord], side: Side) -> float | None:
    """그 편 계획 AI의 빔 단계마다 남긴 판 중 결과가 다른 것의 평균 수(빔 다양성 지표). 계획 AI가 아니면 None."""
    steps = sum(r.planner.get(side.value, {}).get("beam_steps", 0) for r in records)
    results = sum(r.planner.get(side.value, {}).get("beam_results", 0) for r in records)
    return round(results / steps, 2) if steps else None


def summarize(records: list[BattleRecord]) -> list[dict]:
    groups: dict[tuple, list[BattleRecord]] = {}
    for record in records:
        groups.setdefault((record.enemy, record.player), []).append(record)
    rows = []
    for (enemy, player), items in sorted(groups.items()):
        n = len(items)
        first = [r.first_enemy_attack_round for r in items if r.first_enemy_attack_round > 0]
        rows.append({
            "enemy": enemy, "player": player, "battles": n,
            "enemy_win": round(sum(r.outcome == "enemy_win" for r in items) / n, 3),
            "player_win": round(sum(r.outcome == "player_win" for r in items) / n, 3),
            "timeout": round(sum(r.outcome == "timeout" for r in items) / n, 3),
            "rounds": round(statistics.mean(r.rounds for r in items), 2),
            "first_enemy_attack_round": round(statistics.mean(first), 2) if first else None,
            "never_attacked": sum(r.first_enemy_attack_round < 0 for r in items),
            "enemy_hold_ratio": round(statistics.mean(r.enemy_hold_ratio for r in items), 3),
            "mean_gap": round(statistics.mean(r.mean_gap for r in items), 2),
            "player_units_lost": round(statistics.mean(r.player_units_lost for r in items), 2),
            "enemy_units_lost": round(statistics.mean(r.enemy_units_lost for r in items), 2),
            "seconds": round(statistics.mean(r.seconds for r in items), 2),
            "enemy_flip": _flip_ratio(items, Side.ENEMY),
            "player_flip": _flip_ratio(items, Side.PLAYER),
            "enemy_beam_results": _beam_results(items, Side.ENEMY),
            "player_beam_results": _beam_results(items, Side.PLAYER),
        })
    return rows


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="게임 스테이지 데이터로 전투를 끝까지 돌린다")
    parser.add_argument("--stages", nargs="*", default=DEFAULT_STAGES)
    parser.add_argument("--seeds", type=int, default=3)
    parser.add_argument("--player", default="rush,cautious", help="쉼표로: rush, cautious, smart")
    parser.add_argument("--enemy", default="greedy", help="쉼표로: greedy")
    parser.add_argument("--max-rounds", type=int, default=20)
    parser.add_argument("--log", action="store_true", help="전투마다 행동 기록을 남긴다")
    parser.add_argument("--out", type=Path, default=None)
    args = parser.parse_args(argv)
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")

    assets = GameAssets()
    out = args.out or RESULTS / datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S.%fZ")
    out.mkdir(parents=True, exist_ok=True)
    if args.log:
        (out / "battles").mkdir(exist_ok=True)
    records = []
    for name in args.stages:
        stage = assets.stage(name)
        for enemy_name in args.enemy.split(","):
            for style in args.player.split(","):
                for seed in range(args.seeds):
                    record, actions = run_battle(stage, assets, make_player_policy(style), make_enemy_policy(enemy_name),
                                                 seed, args.max_rounds, args.log)
                    records.append(record)
                    print(f"{name:<16} {enemy_name:<8} {style:<9} seed {seed}: {record.outcome:<10} "
                          f"r{record.rounds:<2} 첫 적 공격 r{record.first_enemy_attack_round:<2} "
                          f"대기 {record.enemy_hold_ratio:.2f} 거리 {record.mean_gap:.1f} ({record.seconds}s)")
                    if args.log:
                        stem = f"{name}__{enemy_name}__{style}__{seed}".replace(":", "-")   # Windows 파일 이름에 ':' 불가
                        path = out / "battles" / f"{stem}.json"
                        path.write_text(json.dumps({"record": asdict(record), "actions": actions},
                                                   ensure_ascii=False, indent=1), encoding="utf-8")
    with (out / "battles.csv").open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(asdict(records[0]).keys()))
        writer.writeheader()
        for record in records:
            row = asdict(record)
            for key in ("hold_reasons", "falls", "friendly_fire", "planner", "anomalies"):
                row[key] = json.dumps(row[key], ensure_ascii=False)
            writer.writerow(row)
    summary = summarize(records)
    (out / "summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    (out / "run.json").write_text(json.dumps({"args": {k: str(v) for k, v in vars(args).items()},
                                              "battles": len(records)}, ensure_ascii=False, indent=2),
                                  encoding="utf-8")
    print("\n요약")
    for row in summary:
        print("  " + "  ".join(f"{k}={v}" for k, v in row.items()))
    print(f"\n결과: {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
