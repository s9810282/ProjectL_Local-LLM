"""정책들이 함께 쓰는 판 분석: 후보 행동, 미리 돌려 보기, 거리, 위협 칸.

여기 있는 것은 판단이 아니라 사실 계산이다. 특성·기믹은 모두 엔진(rules.py)이 처리하고,
여기서는 엔진에 "이렇게 하면 어떻게 되나"를 물어본다.
"""

from __future__ import annotations

from collections import deque
from dataclasses import dataclass

from .assets import Cell
from .rules import (Action, Attack, Move, RuleError, apply, attack_aims, can_attack, coverage, legal_attacks,
                    reachable, selection_range)
from .state import ActionUse, BattleState, Outcome, Unit
from .targeting import Side


@dataclass(frozen=True)
class Option:
    """한 유닛이 이번에 할 수 있는 것: (이동) + (공격). 둘 다 없으면 대기."""
    uid: int
    move: Move | None
    attack: Attack | None

    def actions(self) -> list[Action]:
        return [a for a in (self.move, self.attack) if a is not None]


def simulate(state: BattleState, actions: list[Action]) -> BattleState:
    for action in actions:
        state = apply(state, action)
        if state.outcome is not None:
            break
    return state


def _targets_near(state: BattleState, unit: Unit, cell: Cell, slack: int = 1) -> bool:
    """그 칸에서 칠 수 있는 대상이 사거리(+스플래시 여유) 안에 있나. 후보를 줄이는 빠른 거름."""
    reach = selection_range(state, unit) + slack
    cells = [u.cell for u in state.living() if u.uid != unit.uid] + \
            [c for s in state.structures.values() if s.alive for c in s.cells]
    return any(max(abs(c[0] - cell[0]), abs(c[1] - cell[1])) <= reach for c in cells)


def attack_destinations(state: BattleState, unit: Unit) -> list[Cell]:
    """이동 + 공격을 만들 수 있는 도착 칸(사거리 + 1 안에 무언가 있는 칸). unit_options·attack_results와 같은 순서·거름."""
    if unit.use is not ActionUse.AVAILABLE:
        return []
    return [cell for cell in reachable(state, unit) if _targets_near(state, unit, cell)]


def unit_options(state: BattleState, unit: Unit, *, moves: bool = True,
                 attack_cells: set[Cell] | None = None) -> list[Option]:
    """이 유닛의 (이동 + 공격) 조합 전부. 이동만 하는 선택지도 포함한다. 대기는 넣지 않는다.
    attack_cells가 있으면 그 도착 칸에서만 이동 + 공격을 만든다(속도 개선 설계 docs/2026-11-18-planner-candidate-pruning-design.md
    SP1). None이면 거르지 않는다(옛 동작)."""
    options: list[Option] = []
    if can_attack(state, unit):
        for attack, _ in legal_attacks(state, unit):
            options.append(Option(unit.uid, None, attack))
    if moves and unit.use is ActionUse.AVAILABLE:
        for destination, path in reachable(state, unit).items():
            move = Move(unit.uid, path)
            options.append(Option(unit.uid, move, None))
            if not _targets_near(state, unit, destination):
                continue
            if attack_cells is not None and destination not in attack_cells:
                continue
            moved = apply(state, move)
            mover = moved.units[unit.uid]
            if mover.alive and moved.outcome is None:
                for attack, _ in legal_attacks(moved, mover):
                    options.append(Option(unit.uid, move, attack))
    return options


def attack_results(state: BattleState, unit: Unit,
                   attack_cells: set[Cell] | None = None) -> list[tuple[Option, BattleState]]:
    """공격이 들어 있는 조합(제자리 공격, 이동 + 공격)마다 (옵션, 엔진으로 돌린 뒤 판).
    순서·거름은 unit_options와 같다(이동만 하는 것은 뺀다). 이동한 판을 한 번만 만들어 다시 쓴다.
    attack_cells가 있으면 그 도착 칸에서만 이동 + 공격을 돌린다(속도 개선 설계 SP2). None이면 거르지 않는다."""
    results: list[tuple[Option, BattleState]] = []
    if can_attack(state, unit):
        for attack, _ in legal_attacks(state, unit):
            results.append((Option(unit.uid, None, attack), apply(state, attack)))
    if unit.use is ActionUse.AVAILABLE:
        for destination, path in reachable(state, unit).items():
            if not _targets_near(state, unit, destination):
                continue
            if attack_cells is not None and destination not in attack_cells:
                continue
            move = Move(unit.uid, path)
            moved = apply(state, move)
            mover = moved.units[unit.uid]
            if mover.alive and moved.outcome is None:
                for attack, _ in legal_attacks(moved, mover):
                    results.append((Option(unit.uid, move, attack), apply(moved, attack)))
    return results


@dataclass(frozen=True)
class Change:
    """한 편이 본 변화량(앞 상태 → 뒤 상태)."""
    opp_hp_lost: int
    opp_kills: int
    own_hp_lost: int
    own_kills: int          # 우리 편 유닛이 죽은 수
    hq_damage: int          # 적 본진이 잃은 HP
    own_hq_damage: int
    win: bool
    loss: bool


def change(before: BattleState, after: BattleState, side: Side) -> Change:
    def hp(state, s):
        return {u.uid: max(0, u.hp) for u in state.units.values() if u.side is s}

    def alive(state, s):
        return {u.uid for u in state.units.values() if u.side is s and u.alive}

    opp = side.other
    own_before, own_after = hp(before, side), hp(after, side)
    opp_before, opp_after = hp(before, opp), hp(after, opp)

    def hq(state, s):
        return sum(x.hp for x in state.structures.values() if x.side is s)

    win = after.outcome is (Outcome.PLAYER_WIN if side is Side.PLAYER else Outcome.ENEMY_WIN)
    loss = after.outcome is (Outcome.ENEMY_WIN if side is Side.PLAYER else Outcome.PLAYER_WIN)
    return Change(
        opp_hp_lost=sum(opp_before[uid] - opp_after.get(uid, 0) for uid in opp_before),
        opp_kills=len(alive(before, opp) - alive(after, opp)),
        own_hp_lost=sum(own_before[uid] - own_after.get(uid, 0) for uid in own_before),
        own_kills=len(alive(before, side) - alive(after, side)),
        hq_damage=hq(before, Side.ENEMY) - hq(after, Side.ENEMY) if side is Side.PLAYER else 0,
        own_hq_damage=hq(before, Side.ENEMY) - hq(after, Side.ENEMY) if side is Side.ENEMY else 0,
        win=win, loss=loss)


def distance_field(state: BattleState, sources: list[Cell], *, through_units: bool = True) -> dict[Cell, int]:
    """칸까지 걷는 거리(상하좌우, 벽·낭떠러지·구조물은 못 지남). sources에서 0부터."""
    board = state.board
    structures = {c for s in state.structures.values() if s.alive for c in s.cells}
    units = set() if through_units else {u.cell for u in state.living()}
    dist: dict[Cell, int] = {}
    queue: deque[Cell] = deque()
    for cell in sources:
        if board.inside(cell):
            dist[cell] = 0
            queue.append(cell)
    while queue:
        cell = queue.popleft()
        for dx, dy in ((0, 1), (1, 0), (0, -1), (-1, 0)):
            nxt = (cell[0] + dx, cell[1] + dy)
            if nxt in dist or not board.inside(nxt) or state.blocks_entry(nxt) or nxt in structures or nxt in units:
                continue
            dist[nxt] = dist[cell] + 1
            queue.append(nxt)
    return dist


def neighbors_of(cells) -> list[Cell]:
    result = set()
    for x, y in cells:
        result.update({(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)})
    return sorted(result - set(cells))


def hypothetical(state: BattleState, unit: Unit) -> BattleState:
    """그 유닛 편의 턴이고 그 유닛이 아직 행동하지 않았다고 친 복사본(다음 턴 위협을 볼 때)."""
    copy = state.clone()
    copy.turn_side = unit.side
    mover = copy.units[unit.uid]
    mover.use = ActionUse.AVAILABLE
    mover.statuses.pop("moved", None)
    return copy


def reach_damage(state: BattleState, side: Side) -> dict[Cell, int]:
    """side 유닛들이 다음 자기 턴에 이동해서 칠 수 있는 칸마다, 닿는 유닛들의 공통 피해 합(몰아치기, 방어 없음).
    유닛 하나는 한 칸에 한 번만 센다. 영리한 플레이어의 메인 보호에 쓴다(게임 AI의 위협 필드보다 넓다)."""
    from .rules import common_damage
    total: dict[Cell, int] = {}
    for unit in state.living(side):
        if not unit.defn.can_attack or "disarm" in unit.statuses:
            continue
        cells: set[Cell] = set()
        base = hypothetical(state, unit)
        mover = base.units[unit.uid]
        for spot in [mover.cell, *reachable(base, mover)]:
            mover.cell = spot
            for aim in attack_aims(base, mover):
                try:
                    cells |= coverage(base, mover, aim)
                except RuleError:
                    continue
        damage = common_damage(unit)
        for cell in cells:
            total[cell] = total.get(cell, 0) + damage
    return total


def threat_cells(state: BattleState, side: Side) -> set[Cell]:
    """side 유닛들이 다음 자기 턴에 (이동해서) 공격으로 닿을 수 있는 칸. 대상이 지금 거기 없어도 센다."""
    cells: set[Cell] = set()
    for unit in state.living(side):
        if not unit.defn.can_attack or "disarm" in unit.statuses:
            continue
        base = hypothetical(state, unit)
        mover = base.units[unit.uid]
        spots = [mover.cell, *reachable(base, mover)]
        for spot in spots:
            mover.cell = spot
            for aim in attack_aims(base, mover):
                try:
                    cells |= coverage(base, mover, aim)
                except RuleError:
                    continue
        mover.cell = unit.cell
    return cells
