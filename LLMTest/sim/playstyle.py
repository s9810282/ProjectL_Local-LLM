"""적 AI 플레이 성격 지표. 설계 docs/2026-11-18-enemy-playstyle-consistency-design.md §3·§4.

저장된 전투 기록(run.py --log)을 처음부터 다시 돌려(main_focus.replay와 같은 방법) 적 턴마다 사실을 세고,
판(전투)마다 지표의 분자·분모를 낸다. 판단이 아니라 사실 계산이다. 엔진·정책 코드는 바꾸지 않는다.

- A 역할 정적형: 카테고리별 역할 지표 A1~A8, 한 값 A*
- B 집단 돌입형: B1 뭉침, B2 홀로 앞섬, B3 첫 교전 동시성, B4 따로 들어감, B5 돌입 뒤 후퇴
- C 안정 운영형: C1 잃은 적, C2 노출(처치 위협 칸), C3 다친 유닛 빼기, C4 무리한 교환
- 자연스러움(성격 공통): D1 칠 수 있는데 안 침(모든 카테고리), D2 제자리(그 턴에 이동도 공격도 안 한 유닛),
  D3 칠 수 없을 때 접근(다치지 않은 유닛이 그 턴에 칠 수 없었을 때 플레이어 쪽으로 다가간 비율)
- 유닛마다 세는 지표는 **그 턴에 행동할 수 있었던 유닛만** 센다. 방금 들어온 증원(use = DEFERRED)은 규칙상 그 턴에
  움직일 수 없어 AI의 선택이 아니다(2026-11-19 정정: 처음에는 섞여 있어 제자리의 78%가 증원이었다).
- 2026-11-19 코드 검토 반영(손잡이 설계 §11.4·§12): 적 턴 도중에 전투가 끝나면 차례가 오기 전이던 유닛은 세지 않는다(H1) ·
  접근·후퇴는 시작 칸·끝 칸을 모두 **턴 시작의 플레이어 칸**까지로 잰다(M1) · 넉백은 밀 수 있는 대상이 있을 때만(M2) ·
  처치 위협은 적이 **턴을 넘긴 뒤** 판에서(M4) · "칠 수 있었다"는 제 편을 맞히지 않는 조준이 있을 때만(M5) ·
  판 열쇠는 사건 기록까지(L1) · 보호막은 켜져 있을 때만(L2) · 이웃·대포 목록에는 방금 온 증원도 넣는다(L3, 무리 중심은 아님).

사용:
  python -m sim.playstyle <결과 폴더> [...] [--jobs 15] [--out <폴더>]
결과: LLMTest/results/playstyle/<UTC>/ (battles.csv, mismatch.json, run.json)
판 짝 비교는 sim/playstyle_compare.py.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from dataclasses import dataclass, replace
from datetime import datetime, timezone
import json
from multiprocessing import Pool
from pathlib import Path
import statistics
import sys
import time

if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
    __package__ = "sim"

from .analysis import attack_destinations, distance_field  # noqa: E402
from .assets import Category  # noqa: E402
from .evaluate import Evaluator  # noqa: E402
from .main_focus import EnemyTurn, _short, action_from_json, battle_files, initial_state, same_log  # noqa: E402
from .rules import (EndTurn, Move, RuleError, _solo_shield_active, apply, can_attack, knockback_immune,  # noqa: E402
                    legal_attacks, reachable)
from .state import ActionUse, BattleState, Unit  # noqa: E402
from .targeting import Side  # noqa: E402

RESULTS = Path(__file__).resolve().parents[1] / "results" / "playstyle"
LONE_RADIUS = 2         # B2: 이 걸음(맨해튼) 안에 다른 적이 없으면 "홀로"(Q-PS3 A)
HURT_RATIO = 0.5        # C3·D3: 턴 시작 HP가 최대의 이 비율 이하면 "다침"
FEW_ATTACKS = 3         # 보고만: 적이 판 전체에서 공격한 유닛 턴이 이 수 이하인 채 진 판 = "거의 안 싸우고 진 판"
STREAK_TURNS = 3        # 보고만: 한 유닛이 이 턴 수 이상 이어서 칠 수 있는데 안 친 판 = "공격을 계속 피한 판"
ATTACK_TYPES = ("attack", "self_destruct")

# 지표 이름 → (성격 쪽 방향, 설명). 방향 +1 = 클수록 성격대로, −1 = 작을수록.
METRICS = {
    "A1": (+1, "MAIN 닿는 턴에 공격"), "A2": (+1, "MAIN 안 닿는 턴에 접근"),
    "A3": (+1, "RIDER 공격 중 넉백"), "A4": (+1, "RIDER 넉백이 위험으로 밂"),
    "A5": (+1, "HEAVY 대포보다 앞"), "A6": (-1, "HEAVY 교전 뒤 후퇴"),
    "A7": (+1, "CANNON 거리 2 이상 사격"), "A8": (-1, "CANNON 맞붙음"),
    "Astar": (+1, "역할 한 값"),
    "B1": (-1, "뭉침(중심까지 평균 걸음)"), "B2": (-1, "홀로 앞섬"),
    "B3": (+1, "첫 교전 동시성"), "B4": (-1, "교전 턴 따로 들어감"), "B5": (-1, "교전 뒤 후퇴"),
    "C1": (-1, "잃은 적 비율"), "C1t": (-1, "적 턴당 잃은 적"), "C2": (-1, "처치 위협 칸 노출"),
    "C3": (+1, "다친 유닛 빼기"), "C4": (-1, "무리한 교환"),
    # 노출을 그 턴에 공격한 유닛·안 한 유닛으로 나눈 것(2026-11-18 사용자: 공격해서 노출되는 것은 어쩔 수 없다)
    "C2n": (-1, "공격 안 한 유닛의 노출"), "C2a": (-1, "공격한 유닛의 노출"),
    # 공격 안 한 유닛 가운데 잡히지 않는 칸으로 갈 수 있었던 유닛만 본 노출(2026-11-19, 손잡이 설계 §12.6):
    # 피할 칸이 없던 노출은 스테이지 생김새이지 AI의 선택이 아니다. 보고만 한다(판정에 쓸지는 정하지 않았다).
    "C2v": (-1, "피할 수 있었는데 노출"),
    # 자연스러움(2026-11-18 사용자: 제자리에 서 있어도, 칠 수 있는데 계속 안 쳐도 이상하다). 성격과 상관없이 본다.
    "D1": (-1, "칠 수 있는데 안 침"), "D2": (-1, "제자리(이동도 공격도 안 함)"),
    # 싸움을 피하지 않는다(2026-11-19 사용자 "권장대로", 손잡이 설계 Q-RV1): 다치지 않았고 그 턴에 칠 수 없던 유닛이
    # 플레이어 쪽으로 다가간 비율. 멀리서 서성이는 적을 거른다.
    "D3": (+1, "칠 수 없을 때 접근(안 다친 유닛)"),
}
# 역할 한 값 A*에 들어가는 (카테고리, 지표) — 작을수록 좋은 지표는 1 − 값으로 맞춘다(설계 §4.1).
ROLE_PARTS = {Category.MAIN: ("A1", "A2"), Category.RIDER: ("A3", "A4"),
              Category.HEAVY: ("A5", "A6"), Category.CANNON: ("A7", "A8")}


# ---------------------------------------------------------------- 사실 계산 도우미
def reach_table(state: BattleState, side: Side) -> dict[int, dict]:
    """side 유닛마다 다음 자기 턴에 (이동해서) 공격이 닿는 칸 → 적용 비율(지정 칸 100, 스플래시 칸 스플래시 %).
    계획 AI의 위험 계산이 쓰는 도달 표(Evaluator.reach)를 그대로 부른다. 못 치는 유닛(무장 해제 포함)은 뺀다."""
    evaluator = Evaluator(state)
    table: dict[int, dict] = {}
    for unit in state.living(side):
        if not unit.defn.can_attack or unit.statuses.get("disarm", 0) > 0:
            continue
        table[unit.uid] = evaluator.reach(unit, unit.cell)
    return table


def threat_cells(table: dict[int, dict]) -> set:
    return {cell for cells in table.values() for cell, percent in cells.items() if percent > 0}


def kill_threatened(state: BattleState, unit: Unit, table: dict[int, dict]) -> bool:
    """그 칸에 선 unit을 다음 상대 턴에 닿는 상대 유닛들이 함께 쳐서 잡을 수 있나.
    유닛마다 그 칸에 닿는 비율(지정 100·스플래시 %)의 예상 피해(Evaluator.estimate)를 더해 HP + 보호막 이상이면 참.
    보호막은 그 판에서 켜져 있을 때만 더한다(단독 행동: 반경 안에 다른 유닛이 없을 때, 엔진과 같은 판정)."""
    total = 0
    for uid, cells in table.items():
        percent = cells.get(unit.cell, 0)
        if percent > 0:
            total += Evaluator.estimate(state, state.units[uid], unit, percent)
    guard = unit.shield if _solo_shield_active(state, unit) else 0
    return total >= unit.hp + guard


def had_safe_cell(start: BattleState, handed: BattleState, unit: Unit, table: dict[int, dict]) -> bool:
    """그 유닛(턴을 넘긴 판의 유닛)이 이번 턴에 잡히지 않는 칸에 설 수 있었나.
    칸 = 턴 시작 칸, 거기서 걸어 갈 수 있던 칸, 실제로 선 칸. 잡히나는 턴을 넘긴 판의 상대 도달 표로 본다.
    근사: 같은 턴에 다른 유닛이 길을 막거나 연 것, 그 유닛이 자리를 옮겨 상대 도달 표가 달라지는 것은 보지 않는다."""
    origin = start.units.get(unit.uid)
    cells = [unit.cell]
    if origin is not None:
        cells += [origin.cell, *reachable(start, origin)]
    seen = set()
    for cell in cells:
        if cell in seen:
            continue
        seen.add(cell)
        if not kill_threatened(handed, replace(unit, cell=cell), table):
            return True
    return False


def can_hit_opponent(state: BattleState, unit: Unit) -> bool:
    """지금 판에서 그 유닛이 (이동해서) **제 편을 맞히지 않고** 상대 유닛을 맞히는 합법 공격이 하나라도 있나.
    엔진에 묻는다(판은 그 유닛 편의 턴). 제 편도 함께 맞혀야만 하는 조준은 "칠 수 있었다"로 세지 않는다(검토 M5)."""
    if state.outcome is not None or not can_attack(state, unit):
        return False

    def hits(board: BattleState, actor: Unit) -> bool:
        for _, plan in legal_attacks(board, actor):
            sides = [board.units[h.target].side for h in plan.hits if h.kind == "unit"]
            if any(side is not actor.side for side in sides) and not any(side is actor.side for side in sides):
                return True
        return False

    if hits(state, unit):
        return True
    if unit.use is ActionUse.AVAILABLE:
        paths = reachable(state, unit)
        for destination in attack_destinations(state, unit):
            try:
                moved = apply(state, Move(unit.uid, paths[destination]))
            except RuleError:
                continue
            mover = moved.units[unit.uid]
            if mover.alive and moved.outcome is None and hits(moved, mover):
                return True
    return False


def manhattan(a, b) -> int:
    return abs(a[0] - b[0]) + abs(a[1] - b[1])


@dataclass
class Count:
    num: float = 0.0
    den: float = 0.0

    def add(self, hit: bool | float, weight: float = 1.0) -> None:
        self.num += float(hit) * weight if isinstance(hit, bool) else float(hit)
        self.den += weight

    @property
    def value(self) -> float | None:
        return self.num / self.den if self.den else None


def _attack_cells(turn: EnemyTurn) -> dict[int, tuple]:
    """적 유닛이 공격한 순간의 칸: 그 턴의 마지막 이동 끝(없으면 시작 칸)."""
    cells = {u.uid: u.cell for u in turn.start.living(Side.ENEMY)}
    for entry in turn.segment:
        if entry["type"] == "move" and entry["uid"] in cells:
            cells[entry["uid"]] = tuple(entry["path"][-1])
    return cells


# ---------------------------------------------------------------- 다시 돌리기(행동 순간의 판까지)
@dataclass
class StyleTurn(EnemyTurn):
    """적 턴 하나 + 적이 턴을 넘긴 직후 판(플레이어 턴 시작: 적의 재생·상태 만료·보호막 채우기가 끝난 판).
    적 턴 도중에 전투가 끝났으면 handed는 None이다."""
    handed: BattleState | None = None


def replay_steps(initial: BattleState, actions: list[dict]) -> tuple[list[StyleTurn], list[list], BattleState]:
    """main_focus.replay와 같은 순서로 다시 돌리되, 적 턴마다 [(행동 직전 판, 행동)]과 턴을 넘긴 직후 판도 모은다."""
    state, turns, all_steps, current, steps = initial, [], [], None, []
    if state.turn_side is Side.ENEMY and state.outcome is None:
        current, steps = (state, len(state.log)), []
    for entry in actions:
        action = action_from_json(entry["action"])
        closing = None
        if current is not None:
            if isinstance(action, EndTurn):
                closing = StyleTurn(current[0].round, current[0], state, state.log[current[1]:])
                turns.append(closing)
                all_steps.append(steps)
                current = None
            else:
                steps.append((state, action))
        state = apply(state, action)
        if closing is not None:
            closing.handed = state
        if current is not None and state.outcome is not None:
            turns.append(StyleTurn(current[0].round, current[0], state, state.log[current[1]:]))
            all_steps.append(steps)
            current = None
        if isinstance(action, EndTurn) and state.turn_side is Side.ENEMY and state.outcome is None:
            current, steps = (state, len(state.log)), []
    for index, turn in enumerate(turns):
        turn.after = turns[index + 1].start if index + 1 < len(turns) else state
    return turns, all_steps, state


def declined_attackers(turn: EnemyTurn, steps: list, attackers: set[int]) -> set[int]:
    """그 적 턴에 공격하지 않았지만, **자기가 행동한 순간**에 상대를 맞힐 공격이 있었던 적 유닛(점검 R4).
    행동한 순간 = 이동만 했으면 그 이동 직전 판, 아무것도 안 했으면 적 행동이 다 끝난 판.
    이동만 한 유닛은 끝 판에서 제자리 공격이 남았는지도 본다. 그 턴에 죽은 유닛은 뺀다."""
    moved_from = {}
    for state, action in steps:
        if isinstance(action, Move) and action.uid not in moved_from:
            moved_from[action.uid] = state
    declined = set()
    for unit in turn.start.living(Side.ENEMY):
        if unit.uid in attackers or not unit.defn.can_attack:
            continue
        last = turn.end.units[unit.uid]
        if not last.alive:
            continue
        before = moved_from.get(unit.uid)
        if before is not None and can_hit_opponent(before, before.units[unit.uid]):
            declined.add(unit.uid)
        elif can_hit_opponent(turn.end, last):
            declined.add(unit.uid)
    return declined


# ---------------------------------------------------------------- 판 하나
def battle_metrics(initial: BattleState, turns: list[EnemyTurn], all_steps: list[list], record: dict) -> dict:
    counts = {name: Count() for name in METRICS if name != "Astar"}
    engaged = False
    first_engagement_done = False
    idle_turns = 0
    attack_count = 0
    streaks: dict[int, int] = {}       # 유닛마다 지금 이어지고 있는 "칠 수 있는데 안 침" 턴 수(치거나 칠 수 없던 턴에 끊긴다)
    longest_streak = 0
    for turn, steps in zip(turns, all_steps):
        start, end, segment = turn.start, turn.end, turn.segment
        attacks = [e for e in segment if e["type"] in ATTACK_TYPES]
        attackers = {e["uid"] for e in attacks}
        movers = {e["uid"] for e in segment if e["type"] == "move"}
        # 이 턴에 행동할 수 있었던 적 유닛(방금 들어온 증원은 뺀다). 유닛마다 세는 지표는 이 유닛들만 본다.
        actable = {u.uid for u in start.living(Side.ENEMY) if u.use is not ActionUse.DEFERRED}
        declined = declined_attackers(turn, steps, attackers)
        # 접근·후퇴는 시작 칸·끝 칸을 모두 턴 시작의 플레이어 칸까지로 잰다(M1: 같은 턴에 다른 적이 가까운 플레이어를
        # 잡거나 밀어도 가만히 있던 유닛이 "후퇴"로 세어지지 않게). 끝 판의 줄 세우기(A5)만 끝 판의 플레이어 칸을 쓴다.
        before_near = distance_field(start, [u.cell for u in start.living(Side.PLAYER)])
        after_near = distance_field(end, [u.cell for u in end.living(Side.PLAYER)])
        turn_engaged = bool(attacks)
        idle_turns += not turn_engaged
        attack_count += len(attackers)
        first = turn_engaged and not first_engagement_done
        if turn_engaged:
            engaged = True

        # ongoing = 적이 행동을 다 하고 턴을 넘겼다(도중에 전투가 끝나지 않았다).
        # faced = 턴을 넘긴 뒤에도 전투가 이어진다 → 그 판(플레이어 턴 시작)에서 다음 턴에 닿는 칸·비율을 본다
        # (C2·C3·C4·B2·A4, M4). 전투가 끝난 판은 다음 턴이 없다.
        ongoing = end.outcome is None
        handed = turn.handed
        faced = handed is not None and handed.outcome is None
        player_reach = reach_table(handed, Side.PLAYER) if faced else {}
        enemy_reach = reach_table(handed, Side.ENEMY) if faced else {}
        threat = threat_cells(player_reach)
        present_end = end.living(Side.ENEMY)                    # 이웃(B2)·대포 목록(A5)에는 방금 온 증원도 넣는다(L3)
        living_end = [u for u in present_end if u.uid in actable]

        # ---- A·B5: 유닛마다
        positions = _attack_cells(turn)
        for unit in start.living(Side.ENEMY):
            if unit.uid not in actable:
                continue
            last = end.units[unit.uid]
            category = unit.defn.category
            acted = unit.uid in attackers
            if unit.defn.can_attack:
                streaks[unit.uid] = 0 if acted or unit.uid not in declined else streaks.get(unit.uid, 0) + 1
                longest_streak = max(longest_streak, streaks[unit.uid])
            # 적 턴 도중에 전투가 끝났고 그때까지 이동도 공격도 안 한 유닛은 차례가 오지 않은 것이다(H1): 세지 않는다.
            waiting = not ongoing and not acted and unit.uid not in movers
            dist_before = before_near.get(unit.cell)
            dist_after = before_near.get(last.cell) if last.alive else None
            measured = dist_before is not None and dist_after is not None
            retreated = measured and dist_after > dist_before
            if category is Category.MAIN and unit.defn.can_attack:
                if acted or unit.uid in declined:
                    counts["A1"].add(acted)
                elif last.alive and measured and not waiting:
                    counts["A2"].add(dist_after < dist_before)
            if category is Category.HEAVY and engaged and not acted and last.alive and measured and not waiting:
                counts["A6"].add(retreated)
            if not acted and engaged and last.alive and unit.defn.can_attack and measured and not waiting:
                counts["B5"].add(retreated)
            # 자연스러움: 칠 수 있었던 유닛(친 유닛 + 행동한 순간 칠 수 있었는데 안 친 유닛) 중 안 친 비율, 제자리 비율
            if unit.defn.can_attack and (acted or unit.uid in declined):
                counts["D1"].add(not acted)
            if last.alive and ongoing:     # 전투가 턴 중간에 끝나면 아직 행동하지 않은 유닛이 제자리로 보이므로 뺀다
                counts["D2"].add(not acted and unit.uid not in movers)
            # 칠 수 없을 때 접근: 다치지 않았고, 움직일 수 있고, 그 턴에 칠 수 없었고(공격도 안 했고 칠 기회도 없었다),
            # 더 다가갈 칸이 있는(거리 2 이상) 유닛이 플레이어 쪽으로 다가갔나.
            can_move = unit.defn.can_move and unit.defn.move > 0 and "bind" not in unit.statuses
            if (unit.defn.can_attack and can_move and not acted and unit.uid not in declined and last.alive
                    and not waiting and unit.hp > HURT_RATIO * unit.max_hp and measured and dist_before >= 2):
                counts["D3"].add(dist_after < dist_before)
        for entry in attacks:
            unit = start.units.get(entry["uid"])
            if unit is None:
                continue
            hits = [h for h in entry.get("hits", []) if h.get("kind", "unit") == "unit"
                    and h["target"] in start.units and start.units[h["target"]].side is Side.PLAYER]
            if unit.defn.category is Category.CANNON and hits:
                cell = positions.get(unit.uid, unit.cell)
                counts["A7"].add(min(manhattan(cell, start.units[h["target"]].cell) for h in hits) >= 2)
            if unit.defn.category is Category.RIDER and entry["type"] == "attack":
                # 넉백이 생길 수 있는 공격만 센다: 맞고 살아남았고 넉백 면역이 아닌 플레이어 유닛이 있어야 한다
                # (잡은 대상·면역 대상은 밀 수 없음, M2).
                if any(not h.get("killed") and not knockback_immune(start.units[h["target"]]) for h in hits):
                    knocks = [e for e in _following_knockbacks(segment, entry)
                              if e["type"] == "knockback" and e.get("path") and e["uid"] in start.units
                              and start.units[e["uid"]].side is Side.PLAYER]
                    counts["A3"].add(bool(knocks))
                    for knock in knocks:
                        victim = end.units.get(knock["uid"])
                        if knock.get("fell"):
                            counts["A4"].add(True)
                        elif victim is not None and victim.alive and faced:
                            cell = tuple(knock["path"][-1])
                            hunters = sum(1 for cells in enemy_reach.values() if cells.get(cell, 0) > 0)
                            counts["A4"].add(hunters >= 2)
                        # 그 턴에 다른 공격으로 죽은 대상은 세지 않는다(점검 R7)
        cannons = [u for u in present_end if u.defn.category is Category.CANNON]
        for unit in living_end:
            dist = after_near.get(unit.cell)
            if unit.defn.category is Category.HEAVY and cannons and dist is not None and ongoing:
                cannon_dist = [after_near[c.cell] for c in cannons if c.cell in after_near]
                if cannon_dist:
                    counts["A5"].add(dist <= min(cannon_dist))
            if unit.defn.category is Category.CANNON and ongoing:
                counts["A8"].add(any(manhattan(unit.cell, p.cell) == 1 for p in end.living(Side.PLAYER)))

        # ---- B
        # 설계 §4.2는 "교전 전"만이었으나 측정 판은 모두 1라운드에 교전해 모든 턴 끝으로 센다(PS-0 기록).
        # 도중에 끝난 턴은 줄을 다 세우기 전이라 세지 않는다(H1).
        if living_end and ongoing:
            # 무리 중심은 이 턴에 행동할 수 있었던 유닛으로 잡는다: 방금 온 증원은 본진 옆에 놓여(AI의 선택이 아님)
            # 중심을 뒤로 끌어 뭉침을 나쁘게 보이게 한다. 이웃(B2)·대포 목록(A5)에는 넣는다.
            xs = sorted(u.cell[0] for u in living_end)
            ys = sorted(u.cell[1] for u in living_end)
            center = (xs[len(xs) // 2], ys[len(ys) // 2])
            counts["B1"].add(statistics.mean(manhattan(u.cell, center) for u in living_end), weight=1.0)
            if faced:
                for unit in living_end:
                    lone = not any(o.uid != unit.uid and manhattan(o.cell, unit.cell) <= LONE_RADIUS for o in present_end)
                    counts["B2"].add(unit.cell in threat and lone)
        if first:
            # 도중에 끝난 턴이면 차례가 오지 않은 유닛은 분모에서 뺀다(H1)
            alive = [u for u in start.living(Side.ENEMY) if u.defn.can_attack and u.uid in actable
                     and (ongoing or u.uid in attackers or u.uid in movers)]
            if alive:
                counts["B3"].add(len(attackers & {u.uid for u in alive}) / len(alive), weight=1.0)
            first_engagement_done = True
        if turn_engaged and ongoing:
            counts["B4"].add(len(attackers) == 1 and bool(declined))

        # ---- C (턴을 넘긴 뒤 판에서: 적의 재생·표식 만료·보호막 채우기가 끝난 상태, M4)
        if faced:
            for unit in handed.living(Side.ENEMY):
                if unit.uid not in actable:
                    continue
                exposed = kill_threatened(handed, unit, player_reach)
                counts["C2"].add(exposed)
                counts["C2a" if unit.uid in attackers else "C2n"].add(exposed)
                if unit.uid not in attackers and (not exposed or had_safe_cell(start, handed, unit, player_reach)):
                    counts["C2v"].add(exposed)
            for unit in start.living(Side.ENEMY):
                last = handed.units[unit.uid]
                if unit.uid in actable and last.alive and unit.hp <= HURT_RATIO * unit.max_hp:
                    counts["C3"].add(last.cell not in threat)
            for entry in attacks:
                last = handed.units.get(entry["uid"])
                if last is None or not last.alive:
                    continue
                killed = any(h.get("killed") for h in entry.get("hits", []))
                counts["C4"].add(kill_threatened(handed, last, player_reach) and not killed)

    enemies_total = len(initial.living(Side.ENEMY)) + int(record["enemy_spawned"])
    counts["C1"].add(float(record["enemy_units_lost"]), weight=float(enemies_total))
    if turns:
        counts["C1t"].add(float(record["enemy_units_lost"]), weight=float(len(turns)))
    row = {"turns": len(turns), "idle_turns": idle_turns, "attacks": attack_count, "streak": longest_streak}
    for name, count in counts.items():
        row[f"{name}_num"] = round(count.num, 4)
        row[f"{name}_den"] = round(count.den, 4)
    row["Astar"] = role_score(counts)
    return row


def _following_knockbacks(segment: list[dict], attack: dict) -> list[dict]:
    """그 공격 사건 바로 뒤에 붙은 넉백·낙하 사건(다음 공격·이동 전까지)."""
    found, started = [], False
    for entry in segment:
        if entry is attack:
            started = True
            continue
        if not started:
            continue
        if entry["type"] in ("knockback", "fall"):
            found.append(entry)
            continue
        break
    return found


def role_score(counts: dict[str, Count]) -> float | None:
    """A*: 카테고리마다 역할 지표(작을수록 좋은 것은 1 − 값) 평균, 그것을 카테고리끼리 평균(설계 §4.1)."""
    scores = []
    for parts in ROLE_PARTS.values():
        values = []
        for name in parts:
            value = counts[name].value
            if value is None:
                continue
            values.append(value if METRICS[name][0] > 0 else 1 - value)
        if values:
            scores.append(sum(values) / len(values))
    return round(sum(scores) / len(scores), 4) if scores else None


# ---------------------------------------------------------------- 파일·폴더
def game_key(actions: list[dict]) -> str:
    """같은 판은 같은 열쇠(점검 R1: 씨앗이 달라도 같은 판이 되풀이되는 조합을 하나로 세려고).
    행동 기록과 사건 기록(마지막 항목의 log)을 함께 넣는다: 행동이 같아도 난수 결과가 다르면 다른 판이다(검토 L1)."""
    return hashlib.md5(json.dumps(actions, sort_keys=True).encode("utf-8")).hexdigest()[:12]


def analyze_file(path: str) -> dict:
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    record, actions = data["record"], data["actions"]
    initial = initial_state(record)
    try:
        turns, all_steps, final = replay_steps(initial, actions[:-1])
    except RuleError:
        return {"file": path, "same": False, "row": None}
    if not same_log(final, actions[-1]["log"]):
        return {"file": path, "same": False, "row": None}
    row = {"file": Path(path).name, "stage": record["stage"], "seed": record["seed"],
           "enemy": _short(record["enemy"]), "player": _short(record["player"]), "outcome": record["outcome"],
           "rounds": record["rounds"], "game": game_key(actions)}
    row.update(battle_metrics(initial, turns, all_steps, record))
    return {"file": path, "same": True, "row": row}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="저장된 전투 기록에서 적 AI 플레이 성격 지표를 센다")
    parser.add_argument("folders", nargs="+", type=Path, help="run.py --log 결과 폴더")
    parser.add_argument("--jobs", type=int, default=5)
    parser.add_argument("--out", type=Path, default=None)
    args = parser.parse_args(argv)
    files = [str(p) for p in battle_files(args.folders)]
    started = time.perf_counter()
    with Pool(args.jobs) as pool:
        results = pool.map(analyze_file, files, chunksize=4)
    rows = [r["row"] for r in results if r["same"]]
    mismatch = [r["file"] for r in results if not r["same"]]
    out = args.out or RESULTS / datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    out.mkdir(parents=True, exist_ok=True)
    if rows:
        with open(out / "battles.csv", "w", encoding="utf-8-sig", newline="") as f:
            writer = csv.DictWriter(f, fieldnames=list(rows[0]))
            writer.writeheader()
            writer.writerows(rows)
    (out / "mismatch.json").write_text(json.dumps(mismatch, ensure_ascii=False, indent=1), encoding="utf-8")
    (out / "run.json").write_text(json.dumps({"folders": [str(f) for f in args.folders], "battles": len(files),
                                              "same": len(rows), "seconds": round(time.perf_counter() - started, 1)},
                                             ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"판 {len(files)}: 다시 돌린 기록이 같음 {len(rows)}, 다름 {len(mismatch)} "
          f"({time.perf_counter() - started:.1f}초)")
    print(f"결과: {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
