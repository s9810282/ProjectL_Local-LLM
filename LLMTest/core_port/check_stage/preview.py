"""확인용 스테이지(Combat_Research_AiCheck)를 시뮬레이터에서 미리 돌려 본다.

플레이어는 정해 둔 칸에 놓고 아무것도 하지 않는다(매 턴 턴 끝내기). 적은 이름으로 고른다.
적 턴마다: 플레이어 유닛의 HP 변화, 적 유닛의 이동과 공격을 구역(미끼·무리·막힌 방)별로 적는다.
시뮬레이터는 게임과 같은 에셋을 읽지만 규칙은 옮겨 쓴 것이라, 게임에서의 수치와 조금 다를 수 있다.

사용(LLMTest에서): python -I core_port/check_stage/preview.py <적 이름> [미끼 유닛 이름] [라운드 수]
예: python -I core_port/check_stage/preview.py plan4:g1:l1 Unit_Cowboy 8
"""
from __future__ import annotations

import sys
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8")
sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from sim.assets import GameAssets  # noqa: E402
from sim.enemy import make_enemy_policy  # noqa: E402
from sim.rules import EndTurn, apply, start_battle  # noqa: E402
from sim.state import new_battle  # noqa: E402
from sim.targeting import Side  # noqa: E402

STAGE = "Combat_Research_AiCheck"
MAIN_CELL = (2, 2)                      # 본대 배치 구역 앞줄 가운데(여덟 방향이 열린 칸)
REAR_CELLS = [(1, 1), (3, 1)]           # 메인 뒤 좌우
BAIT_CELL = (12, 2)                     # 미끼 배치 구역 앞줄 가운데
ATTACK_TYPES = ("attack", "self_destruct")


def zone(cell) -> str:
    x, y = cell
    if x >= 11 and y >= 12:
        return "막힌 방"
    return "미끼 쪽" if x >= 8 else "무리"


def main() -> int:
    enemy_name = sys.argv[1] if len(sys.argv) > 1 else "plan4:g1:l1:i0.25:v1:b1"
    bait_name = sys.argv[2] if len(sys.argv) > 2 else "Unit_Cowboy"
    rounds = int(sys.argv[3]) if len(sys.argv) > 3 else 8
    assets = GameAssets()
    stage = assets.stage(STAGE)
    loadout = assets.player_loadout()
    roster = [loadout.main, *loadout.subs]
    bait_index = next(i for i, unit in enumerate(roster) if unit.name == bait_name)
    placements = {0: MAIN_CELL, bait_index: BAIT_CELL}
    rest = [i for i in range(1, len(roster)) if i != bait_index]
    for index, cell in zip(rest, REAR_CELLS):
        placements[index] = cell
    state = start_battle(new_battle(stage, loadout, placements, seed=1))
    enemy = make_enemy_policy(enemy_name)
    home = {u.uid: zone(u.cell) for u in state.living(Side.ENEMY)}
    print(f"=== {STAGE} — 적 {enemy_name}, 미끼 {bait_name.replace('Unit_', '')}{BAIT_CELL}, 메인 {MAIN_CELL}, 플레이어는 아무것도 안 함")
    while state.outcome is None and state.round <= rounds:
        if state.turn_side is Side.PLAYER:
            state = apply(state, EndTurn())
            continue
        start, mark = state, len(state.log)
        for _ in range(200):
            action = enemy.act(state)
            state = apply(state, action)
            if isinstance(action, EndTurn) or state.outcome is not None:
                break
        events = state.log[mark:]
        attackers = {e["uid"] for e in events if e["type"] in ATTACK_TYPES}
        print(f"-- 라운드 {start.round} 적 턴")
        hp = []
        for unit in start.living(Side.PLAYER):
            after = state.units.get(unit.uid)
            left = after.hp if after is not None and after.alive else 0
            name = unit.name.replace("Unit_", "") + ("(메인)" if unit.is_main else "")
            hp.append(f"{name}{unit.cell} HP {unit.hp}→{left}{'†' if left == 0 else ''}")
        print("   플레이어: " + ", ".join(hp))
        for label in ("미끼 쪽", "무리", "막힌 방"):
            parts = []
            for unit in start.living(Side.ENEMY):
                if home.get(unit.uid) != label:
                    continue
                after = state.units.get(unit.uid)
                where = "죽음" if after is None or not after.alive else (
                    "제자리" if after.cell == unit.cell else f"{unit.cell}→{after.cell}")
                parts.append(f"#{unit.uid} {where}{' 공격' if unit.uid in attackers else ''}")
            print(f"   {label}: " + "; ".join(parts))
    print(f"끝: {state.outcome or '진행 중'} (라운드 {state.round})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
