"""규칙 엔진: 판 상태 + 행동 → 다음 판 상태.

`apply(state, action)`은 원래 상태를 바꾸지 않고 새 상태를 돌려준다(AI가 미리 돌려 볼 수 있게).
게임 원본은 함수마다 주석으로 적었다(Core = Assets/ProjectL/Script/.../CoreSystem/CoreCaculator).
"""

from __future__ import annotations

from dataclasses import dataclass
import heapq

from .assets import Category, Cell, ChoiceMode, Objective, Shape, WallDelivery, WallPropagation
from .state import ActionUse, BattleState, Outcome, Structure, Unit, player_traits
from .targeting import (DIRECTIONS, Side, all_offsets, angle_contains, base_contains, direction_offsets, splash_offsets,
                        supported_directions)


class RuleError(ValueError):
    """규칙상 할 수 없는 행동."""


# ---------------------------------------------------------------- 행동
@dataclass(frozen=True)
class Move:
    uid: int
    path: tuple[Cell, ...]     # path[0]은 출발 칸


@dataclass(frozen=True)
class Attack:
    uid: int
    aim: Cell | str | None     # 한 칸 고르기 = 칸, 방향 고르기 = 방향 이름, 전체 = None


@dataclass(frozen=True)
class EndTurn:
    pass


Action = Move | Attack | EndTurn


# ---------------------------------------------------------------- 병과 상성 (Contracts/Commands/CategoryMatchupSpec.cs, 분모 10)
_MATCHUP = {
    (Category.RIDER, Category.HEAVY): 7, (Category.RIDER, Category.CANNON): 12,
    (Category.HEAVY, Category.RIDER): 15, (Category.HEAVY, Category.CANNON): 9,
    (Category.CANNON, Category.RIDER): 9, (Category.CANNON, Category.HEAVY): 11,
}


def matchup(attacker: Category, defender: Category) -> int:
    return _MATCHUP.get((attacker, defender), 10)


# ---------------------------------------------------------------- 진행
def start_battle(state: BattleState) -> BattleState:
    """전투 시작: 모든 유닛은 Deferred, 플레이어 턴부터(CombatTurnCalculator.EnterCombat)."""
    state = state.clone(keep_log=True)
    for unit in state.units.values():
        unit.use = ActionUse.DEFERRED
    state.turn_side = Side.PLAYER
    _refill_shields(state)
    _begin_side_turn(state, Side.PLAYER)
    return state


def apply(state: BattleState, action: Action) -> BattleState:
    if state.outcome is not None:
        raise RuleError("이미 끝난 전투입니다")
    state = state.clone(keep_log=True)
    if isinstance(action, Move):
        _apply_move(state, action)
    elif isinstance(action, Attack):
        _apply_attack(state, action)
    elif isinstance(action, EndTurn):
        _end_turn(state)
    else:
        raise RuleError(f"모르는 행동입니다: {action!r}")
    _check_outcome(state)
    return state


def _begin_side_turn(state: BattleState, side: Side) -> None:
    """그 편 살아 있는 유닛을 Available로(CombatTurnCalculator.BeginControllerTurn). 적 턴이면 증원."""
    for unit in state.living(side):
        unit.use = ActionUse.AVAILABLE
        unit.statuses.pop("moved", None)          # 제자리 사격: 자기 턴 시작에 "이동함"을 지운다
    state.log.append({"type": "turn_begin", "side": side.value, "round": state.round})
    if side is Side.ENEMY:
        _reinforce(state)


def _end_turn(state: BattleState) -> None:
    """턴 끝: 그 편의 재생·상태 이상 줄이기, 모든 단독 행동 보호막 채우기(CombatTurnCalculator.EndPlayerTurn/EndEnemyPhase)."""
    side = state.turn_side
    state.completed_turns += 1
    for unit in state.living(side):
        regen = unit.defn.trait("Regeneration")
        if regen is not None:
            count = unit.statuses.get("regen", 0) + 1
            if count >= int(regen.data.get("healEveryOwnSideTurnEnds", 1)):
                count = 0
                unit.hp = min(unit.max_hp, unit.hp + int(regen.data.get("healAmount", 1)))
            unit.statuses["regen"] = count
        for key in ("disarm", "bind", "mark"):    # "그 편 턴 끝 N번" 동안의 상태
            if key in unit.statuses:
                unit.statuses[key] -= 1
                if unit.statuses[key] <= 0:
                    del unit.statuses[key]
                    if key == "mark":
                        unit.statuses.pop("mark_pct", None)
    _refill_shields(state)
    state.log.append({"type": "turn_end", "side": side.value, "round": state.round})
    if side is Side.ENEMY:
        state.round += 1
    state.turn_side = side.other
    _check_outcome(state)
    if state.outcome is None:
        _begin_side_turn(state, state.turn_side)


def _refill_shields(state: BattleState) -> None:
    """단독 행동 보호막은 양쪽 턴 끝마다 다시 찬다(SoloActionTraitRule, resetEveryControllerTurnEnds = 1)."""
    for unit in state.living():
        solo = unit.defn.trait("SoloAction")
        if solo is not None:
            unit.shield = int(solo.data.get("shieldAmount", 10))


# ---------------------------------------------------------------- 증원 (NexusReinforcementCalculator, NexusReinforcementGeometry)
def _reinforce(state: BattleState) -> None:
    stage = state.board.stage
    spec = stage.reinforcement
    if spec is None or stage.objective is not Objective.HEADQUARTERS:
        return
    nexus = next((s for s in state.structures.values() if s.side is Side.ENEMY and s.alive), None)
    if nexus is None:
        return
    capacity = spec.field_limit - len(state.living(Side.ENEMY))
    remaining = {name: count for name, count in state.reinforcement_pool.items() if count > 0}
    cells = spawn_cells(state, nexus, spec.padding)
    count = min(spec.per_turn, capacity, sum(remaining.values()), len(cells))
    units_by_name = {unit.name: unit for unit, _ in spec.pool}
    for _ in range(max(0, count)):
        names = sorted(remaining)
        name = state.rng.choices(names, weights=[remaining[n] for n in names])[0]
        remaining[name] -= 1
        state.reinforcement_pool[name] -= 1
        if remaining[name] == 0:
            del remaining[name]
        cell = cells.pop(state.rng.randrange(len(cells)))
        defn = units_by_name[name]
        unit = Unit(uid=state.next_id, defn=defn, side=Side.ENEMY, cell=cell, hp=defn.max_hp, max_hp=defn.max_hp,
                    attack=defn.attack, defense=defn.defense, use=ActionUse.DEFERRED)
        if defn.trait("SoloAction") is not None:
            unit.shield = int(defn.trait("SoloAction").data.get("shieldAmount", 10))
        state.units[unit.uid] = unit
        state.next_id += 1
        state.log.append({"type": "reinforce", "uid": unit.uid, "unit": name, "cell": cell})


def spawn_cells(state: BattleState, nexus: Structure, padding: int) -> list[Cell]:
    """본진을 감싼 사각형을 padding만큼 넓힌 칸 중 빈 칸. 지형·레일 칸, 플레이어 유닛 상하좌우 옆 칸은 뺀다."""
    xs, ys = [c[0] for c in nexus.cells], [c[1] for c in nexus.cells]
    terrain = {p.cell for p in state.board.stage.terrain} | set(state.cliffs)
    blocked = terrain | set(state.board.stage.rail_cells) | set(nexus.cells)
    for unit in state.living():
        blocked.add(unit.cell)
        if unit.side is Side.PLAYER:
            blocked.update((unit.cell[0] + dx, unit.cell[1] + dy) for dx, dy in ((0, 1), (1, 0), (0, -1), (-1, 0)))
    for structure in state.structures.values():
        if structure.alive:
            blocked.update(structure.cells)
    return [(x, y) for y in range(min(ys) - padding, max(ys) + padding + 1)
            for x in range(min(xs) - padding, max(xs) + padding + 1)
            if state.board.inside((x, y)) and (x, y) not in blocked]


def _check_outcome(state: BattleState) -> None:
    """메인 사망 = 패배(승리보다 우선), 목표 1 = 적 본진 파괴, 목표 2 = 격파 대상 처치(BattleOutcomeCalculator)."""
    if state.outcome is not None:
        return
    main = state.main_character()
    if main is not None and not main.alive:
        state.outcome = Outcome.ENEMY_WIN
    elif state.board.stage.objective is Objective.HEADQUARTERS:
        if any(not s.alive for s in state.structures.values() if s.side is Side.ENEMY):
            state.outcome = Outcome.PLAYER_WIN
    elif any(u.victory_target and not u.alive for u in state.units.values()):
        state.outcome = Outcome.PLAYER_WIN
    if state.outcome is not None:
        state.log.append({"type": "outcome", "outcome": state.outcome.value, "round": state.round})


# ---------------------------------------------------------------- 이동 (Domain/Movement/MovementRouteValidator.cs)
def has_trait(unit: Unit, kind: str) -> bool:
    if unit.defn.trait(kind) is not None:
        return True
    return unit.side is Side.PLAYER and kind in player_traits(unit.defn)


# 엔진이 규칙을 가진 특성 종류. 여기 없는 특성은 시뮬레이터에서 아무 일도 안 한다
# (설계 docs/2026-11-13-planner-engine-reply-design.md R6). 특성 규칙을 넣으면 여기에 더한다.
KNOWN_TRAITS = frozenset({
    "AnkleBreaker", "ArmorBreaker", "Disarm", "HuntersMark", "ImpactBolt", "KillChainAttack", "Knockback",
    "Leap", "Pull", "Regeneration", "SelfDestruct", "SoloAction", "StationaryFire", "SteadfastWill",
})


def unknown_traits(defs) -> list[tuple[str, str]]:
    """유닛 정의들 중 엔진이 모르는 특성: [(유닛 이름, 특성 종류)], 이름·종류 순, 겹침 없이."""
    return sorted({(defn.name, trait.kind) for defn in defs for trait in defn.traits if trait.kind not in KNOWN_TRAITS})


def can_move(state: BattleState, unit: Unit) -> bool:
    """발목 꺾기(bind)에 걸리면 못 움직인다(CombatActionAdmission)."""
    return (unit.alive and unit.side is state.turn_side and unit.use is ActionUse.AVAILABLE
            and unit.defn.can_move and unit.defn.move > 0 and "bind" not in unit.statuses)


def _passability(state: BattleState, unit: Unit):
    """(지나갈 수 있나, 멈출 수 있나). 벽·낭떠러지·모든 산 유닛·산 구조물이 막는다(MovementRouteValidator.CanOccupy)."""
    leap = has_trait(unit, "Leap")
    board = state.board
    occupied = {u.cell for u in state.living() if u.uid != unit.uid}
    structures = {cell for s in state.structures.values() if s.alive for cell in s.cells}

    def can_land(cell: Cell) -> bool:
        return board.inside(cell) and not state.blocks_entry(cell) and cell not in occupied and cell not in structures

    def can_pass(cell: Cell) -> bool:
        if leap:   # 도약: 중간 칸의 유닛·벽·낭떠러지는 지나간다. 구조물은 못 지나간다(LeapTraitRule).
            return board.inside(cell) and cell not in structures
        return can_land(cell)

    return can_pass, can_land


def reachable(state: BattleState, unit: Unit) -> dict[Cell, tuple[Cell, ...]]:
    """갈 수 있는 칸 → 경로. 상하좌우 한 칸씩, 칸마다 비용 1, 같은 칸 두 번 안 됨.

    경로가 여럿이면 약한 지반을 덜 밟고, 그다음 짧은 경로를 고른다(게임에서는 플레이어가 경로를 고른다).
    """
    if not can_move(state, unit):
        return {}
    budget = unit.defn.move
    can_pass, can_land = _passability(state, unit)
    start = unit.cell
    best: dict[tuple[Cell, int], tuple[int, tuple[Cell, ...]]] = {(start, 0): (0, (start,))}
    queue = [(0, 0, start, (start,))]
    while queue:
        weak, steps, cell, path = heapq.heappop(queue)
        if best.get((cell, steps), (None,))[0] != weak or steps == budget:
            continue
        for dx, dy in ((0, 1), (1, 0), (0, -1), (-1, 0)):
            nxt = (cell[0] + dx, cell[1] + dy)
            if nxt in path or not can_pass(nxt):
                continue
            key = (nxt, steps + 1)
            cost = weak + (1 if nxt in state.weak_entries else 0)
            if key not in best or cost < best[key][0]:
                best[key] = (cost, path + (nxt,))
                heapq.heappush(queue, (cost, steps + 1, nxt, path + (nxt,)))
    result: dict[Cell, tuple[int, int, tuple[Cell, ...]]] = {}
    for (cell, steps), (weak, path) in best.items():
        if cell == start or not can_land(cell):
            continue
        if cell not in result or (weak, steps) < result[cell][:2]:
            result[cell] = (weak, steps, path)
    return {cell: value[2] for cell, value in result.items()}


def _apply_move(state: BattleState, move: Move) -> None:
    unit = state.units.get(move.uid)
    if unit is None or not can_move(state, unit):
        raise RuleError(f"{move.uid}번 유닛은 지금 이동할 수 없습니다")
    path = tuple(move.path)
    if len(path) < 2 or path[0] != unit.cell:
        raise RuleError("경로가 유닛 칸에서 시작하지 않습니다")
    if len(path) - 1 > unit.defn.move:
        raise RuleError("이동력을 넘는 경로입니다")
    if len(set(path)) != len(path):
        raise RuleError("같은 칸을 두 번 지나는 경로입니다")
    can_pass, can_land = _passability(state, unit)
    for previous, cell in zip(path, path[1:]):
        if abs(cell[0] - previous[0]) + abs(cell[1] - previous[1]) != 1:
            raise RuleError("상하좌우 한 칸씩 가야 합니다")
    if not all(can_pass(cell) for cell in path[1:-1]) or not can_land(path[-1]):
        raise RuleError(f"{unit.name}은(는) 그 경로로 갈 수 없습니다")
    destination = path[-1]
    for cell in path[1:]:
        _register_entry(state, cell)
    unit.cell = destination
    unit.use = ActionUse.MOVED
    unit.statuses["moved"] = 1
    state.log.append({"type": "move", "uid": unit.uid, "unit": unit.name, "path": list(path)})
    _collapse_weak_ground(state)


# ---------------------------------------------------------------- 지형 (WeakGroundCalculator, CliffFallCalculator)
def _register_entry(state: BattleState, cell: Cell) -> None:
    """살아 있는 유닛이 실제로 들어간 약한 지반 칸을 센다. 무너짐은 명령 끝에 한꺼번에."""
    if cell in state.weak_entries:
        state.weak_entries[cell] += 1


def _collapse_weak_ground(state: BattleState) -> None:
    pending = [cell for cell, count in state.weak_entries.items() if count >= state.board.weak_threshold[cell]]
    if not pending:
        return
    for cell in pending:
        del state.weak_entries[cell]
    state.cliffs = state.cliffs | frozenset(pending)
    state.log.append({"type": "collapse", "cells": sorted(pending)})
    for unit in state.living():
        if unit.cell in pending:
            _fall(state, unit)


def _fall(state: BattleState, unit: Unit) -> None:
    """낭떠러지에 떨어지면 즉사. 추락 사망은 사망 특성(자폭)을 일으키지 않는다(DeathTraitApplication)."""
    unit.hp = 0
    unit.statuses["fell"] = 1
    state.log.append({"type": "fall", "uid": unit.uid, "unit": unit.name, "cell": unit.cell})


# ---------------------------------------------------------------- 공격
@dataclass(frozen=True)
class Policy:
    """공격 대상 정책(Domain/Combat/AttackTargetingPolicy.cs). 대포 병과는 SafeBombardmentCategoryRule."""
    allows_empty_single: bool = False
    allows_friendly_targets: bool = False
    excludes_adjacent: bool = False


_CANNON_POLICY = Policy(True, True, True)
_NORMAL_POLICY = Policy()


def policy_of(unit: Unit) -> Policy:
    return _CANNON_POLICY if unit.defn.category is Category.CANNON else _NORMAL_POLICY


@dataclass(frozen=True)
class Hit:
    kind: str           # "unit" 또는 "structure"
    target: int
    role: str           # "primary", "designated", "additional"
    percent: int
    origin: Cell        # 피해 출발 칸(지형 보정 판정에 씀)


@dataclass(frozen=True)
class PullMove:
    """끌어오기: 대상을 path대로 옮긴 뒤(마지막 칸 = 공격자 기본 사거리 경계) 적이면 그 자리에서 친다."""
    target: int
    path: tuple[Cell, ...]
    damage: bool                # 적대 대상이면 피해, 아군이면 옮기기만


@dataclass(frozen=True)
class AttackPlan:
    hits: tuple[Hit, ...]
    cells: tuple[Cell, ...]     # 공격이 덮은 칸(가림 적용 뒤)
    primary: Cell | None = None # 가림을 지난 주 칸(한 칸 고르기만)
    pull: PullMove | None = None


def can_attack(state: BattleState, unit: Unit) -> bool:
    """무장 해제(disarm)에 걸리면 못 친다(CombatActionAdmission.IsAttackRestricted)."""
    return (unit.alive and unit.side is state.turn_side and unit.defn.can_attack
            and unit.use in (ActionUse.AVAILABLE, ActionUse.MOVED) and "disarm" not in unit.statuses)


def attack_range(state: BattleState, unit: Unit) -> int:
    """모래폭풍 칸에 서 있으면 사거리 -1(1 이하는 그대로, EnvironmentTerrainRules.AppointRangeAt)."""
    r = unit.defn.attack_range
    if unit.cell in state.board.sandstorm and r > 1:
        r -= 1
    return r


def pull_extra(unit: Unit) -> int:
    """끌어오기 추가 사거리(PullTraitAuthoring.extraRangeCells). 없으면 0."""
    trait = unit.defn.trait("Pull")
    return int(trait.data.get("extraRangeCells", 2)) if trait is not None else 0


def selection_range(state: BattleState, unit: Unit) -> int:
    """조준을 고를 수 있는 사거리 = 지형 보정 기본 사거리 + 끌어오기 추가 사거리(CombatAttackQueries.SelectionRange)."""
    return attack_range(state, unit) + pull_extra(unit)


def _chebyshev(a: Cell, b: Cell) -> int:
    return max(abs(a[0] - b[0]), abs(a[1] - b[1]))


def _raw_selection(state: BattleState, unit: Unit, aim, policy: Policy):
    """지정 칸(AttackSelectionResolver + CombatAttackQueries.SelectionCellSteps)."""
    method = unit.defn.targeting
    r = selection_range(state, unit)
    ox, oy = unit.cell
    board = state.board
    if method.choice is ChoiceMode.SINGLE:
        if not (isinstance(aim, tuple) and len(aim) == 2):
            raise RuleError("한 칸 고르기 공격은 칸을 조준해야 합니다")
        dx, dy = aim[0] - ox, aim[1] - oy
        if not base_contains(method.shape, r, dx, dy) or not angle_contains(method.angle, unit.side, dx, dy):
            raise RuleError("조준 칸이 공격 모양 밖입니다")
        if not board.inside(aim):
            raise RuleError("조준 칸이 판 밖입니다")
        if policy.excludes_adjacent and _chebyshev(unit.cell, aim) <= 1:
            raise RuleError("대포는 바로 옆 칸을 조준할 수 없습니다")
        primary = aim
        designated = [aim]
        additional = [(aim[0] + sx, aim[1] + sy) for sx, sy in splash_offsets(method.splash)]
        additional = [cell for cell in additional if board.inside(cell)]
        return primary, designated, additional
    if method.choice is ChoiceMode.DIRECTION:
        if aim not in DIRECTIONS or aim not in supported_directions(method.shape):
            raise RuleError("방향 고르기 공격은 지원하는 방향을 골라야 합니다")
        step = DIRECTIONS[aim]
        if not angle_contains(method.angle, unit.side, *step):
            raise RuleError("그 방향은 공격 각도 밖입니다")
        offsets = direction_offsets(method.shape, r, aim)
    else:
        if aim is not None:
            raise RuleError("전체 공격은 조준이 없습니다")
        offsets = all_offsets(method.shape, r)
    cells = [(ox + x, oy + y) for x, y in offsets if angle_contains(method.angle, unit.side, x, y)]
    cells = [cell for cell in cells if board.inside(cell)
             and not (policy.excludes_adjacent and _chebyshev(unit.cell, cell) <= 1)]
    return None, cells, []


def _eligible(state: BattleState, unit: Unit, policy: Policy):
    """대상 자격(AttackTargetEligibility): 자기는 안 됨, 적대 진영, 정책이 허락하면 아군도."""
    friendly_units = policy.allows_friendly_targets or unit.defn.trait("Pull") is not None
    units = [u for u in state.living() if u.uid != unit.uid
             and (u.side is not unit.side or friendly_units)]
    structures = [s for s in state.structures.values() if s.alive
                  and (s.side is not unit.side or policy.allows_friendly_targets)]
    return units, structures


def _roles(primary, designated, additional, units, structures) -> list[tuple[str, int, str]]:
    """대상마다 역할(AttackTargetResolver): 주 칸 > 지정 칸 > 추가(스플래시) 칸."""
    designated_set, additional_set = set(designated), set(additional)
    roles = []
    for kind, ident, cells in ([("unit", u.uid, (u.cell,)) for u in units]
                               + [("structure", s.sid, s.cells) for s in structures]):
        role = None
        for cell in cells:
            if primary is not None and cell == primary:
                role = "primary"
                break
            if cell in designated_set:
                role = "designated"
            elif cell in additional_set and role is None:
                role = "additional"
        if role is not None:
            roles.append((kind, ident, role))
    return roles


def resolve_attack(state: BattleState, unit: Unit, aim) -> AttackPlan:
    """공격 하나가 누구를 어떤 비율로 맞히나(CombatAttackQueries.SelectionSteps → CombatAttackDelivery)."""
    if not can_attack(state, unit):
        raise RuleError(f"{unit.name}은(는) 지금 공격할 수 없습니다")
    policy = policy_of(unit)
    primary, designated, additional = _raw_selection(state, unit, aim, policy)
    units, structures = _eligible(state, unit, policy)
    roles = _roles(primary, designated, additional, units, structures)
    walls = state.board.walls
    if primary is not None:
        if walls.occupies(primary):
            raise RuleError("벽 칸은 조준할 수 없습니다")
        if not policy.allows_empty_single and not any(role == "primary" for _, _, role in roles):
            raise RuleError("조준 칸에 공격할 대상이 없습니다")
    if primary is None and not roles:
        raise RuleError("공격 범위에 대상이 없습니다")

    # 가림(AttackOcclusionResolver.ResolveCellSteps)
    delivery = unit.defn.wall_delivery
    high = state.board.high_ground

    def direct_blocked(cell: Cell) -> bool:
        if walls.occupies(cell):
            return True
        if delivery is WallDelivery.IGNORE_WALLS or (unit.cell in high and cell not in high):
            return False
        return walls.blocks(unit.cell, cell)

    reachable_designated = [cell for cell in designated if not direct_blocked(cell)]
    kept_primary = primary if primary is not None and reachable_designated else None
    reachable_additional = []
    if kept_primary is not None:
        for cell in additional:
            if walls.occupies(cell):
                continue
            if unit.defn.wall_propagation is WallPropagation.FROM_IMPACT and walls.blocks(kept_primary, cell):
                continue
            reachable_additional.append(cell)
    if primary is not None and kept_primary is None:
        raise RuleError("벽에 막혔습니다")
    roles = _roles(kept_primary, reachable_designated, reachable_additional, units, structures)
    if kept_primary is None and not roles:
        raise RuleError("벽에 막혔습니다")
    hits = tuple(Hit(kind, ident, role, unit.defn.targeting.splash_percent if role == "additional" else 100,
                     kept_primary if role == "additional" else unit.cell) for kind, ident, role in roles)
    cells = tuple(sorted(set(reachable_designated) | set(reachable_additional), key=lambda c: (c[1], c[0])))
    if unit.defn.trait("Pull") is not None and kept_primary is not None:
        return _pull_plan(state, unit, kept_primary, hits, cells)
    return AttackPlan(hits, cells, kept_primary)


def _pull_plan(state: BattleState, unit: Unit, contact: Cell, hits, cells) -> AttackPlan:
    """끌어오기 판정. 기본 사거리 안이면 그냥 친다(아군이면 거절). 밖이면 공격자 쪽 직선으로 기본 사거리 경계까지 끌어온다.
    길이 막히거나(벽·고지대 턱·유닛·구조물) 대상이 강제 이동 면역이면 공격 자체가 거절된다."""
    basic = attack_range(state, unit)
    dx, dy = contact[0] - unit.cell[0], contact[1] - unit.cell[1]
    if (dx == 0) == (dy == 0):
        raise RuleError("끌어오기 대상은 같은 행이나 열에 있어야 합니다")
    target = state.unit_at(contact)
    if abs(dx) + abs(dy) <= basic:
        destination = contact
    else:
        destination = (unit.cell[0] + ((dx > 0) - (dx < 0)) * basic, unit.cell[1] + ((dy > 0) - (dy < 0)) * basic)
    if target is None:   # 구조물: 기본 사거리 안에서만 보통 공격
        if destination != contact:
            raise RuleError("구조물은 끌어올 수 없어 기본 사거리 안에서만 칠 수 있습니다")
        return AttackPlan(hits, cells, contact)
    damage = target.side is not unit.side
    if destination == contact:
        if not damage:
            raise RuleError("이미 기본 사거리 안의 아군은 끌어올 것이 없습니다")
        return AttackPlan(hits, cells, contact)
    if target.defn.forced_move_immune:
        raise RuleError("강제 이동 면역 대상은 끌어올 수 없습니다")
    step = ((destination[0] > contact[0]) - (destination[0] < contact[0]),
            (destination[1] > contact[1]) - (destination[1] < contact[1]))
    board, path, current = state.board, [], contact
    while current != destination:
        nxt = (current[0] + step[0], current[1] + step[1])
        blocked = (not board.inside(nxt) or board.walls.occupies(nxt)
                   or (nxt in board.high_ground and current not in board.high_ground)
                   or (state.unit_at(nxt) is not None and state.unit_at(nxt).uid != target.uid)
                   or state.structure_at(nxt) is not None)
        if blocked:
            raise RuleError("끌어오는 길이 막혔습니다")
        path.append(nxt)
        current = nxt
    estimate = (Hit("unit", target.uid, "primary", 100, unit.cell),) if damage else ()
    return AttackPlan(estimate, cells, contact, PullMove(target.uid, tuple(path), damage))


def effective_defense(unit: Unit) -> int:
    """방어구 부수기는 맞을 때마다 방어를 깎고 스테이지 끝까지 남는다(ArmorBreakerTraitRule, 근사: 방어 점수에서 뺌)."""
    return max(0, unit.defense - unit.statuses.get("armor", 0))


def common_damage(unit: Unit) -> int:
    """공통 피해. 제자리 사격: 이번 자기 턴에 안 움직였으면 +extraDamagePercent%(StationaryFireTraitRule.Numerator)."""
    stationary = unit.defn.trait("StationaryFire")
    if stationary is not None and "moved" not in unit.statuses:
        return unit.attack * (100 + int(stationary.data.get("extraDamagePercent", 100))) // 100
    return unit.attack


def coverage_parts(state: BattleState, unit: Unit, aim) -> tuple[Cell | None, set[Cell], set[Cell]]:
    """그 조준이 닿는 칸을 (주 칸, 지정 칸, 스플래시 칸)으로(가림 적용). 대상이 있는지는 보지 않는다."""
    policy = policy_of(unit)
    primary, designated, additional = _raw_selection(state, unit, aim, policy)
    walls, high = state.board.walls, state.board.high_ground

    def direct_open(cell: Cell) -> bool:
        if walls.occupies(cell):
            return False
        if unit.defn.wall_delivery is WallDelivery.IGNORE_WALLS or (unit.cell in high and cell not in high):
            return True
        return not walls.blocks(unit.cell, cell)

    direct = {cell for cell in designated if direct_open(cell)}
    splash: set[Cell] = set()
    if primary is not None and primary in direct:
        for cell in additional:
            if walls.occupies(cell):
                continue
            if unit.defn.wall_propagation is WallPropagation.FROM_IMPACT and walls.blocks(primary, cell):
                continue
            splash.add(cell)
    return (primary if primary in direct else None), direct, splash


def coverage(state: BattleState, unit: Unit, aim) -> set[Cell]:
    """그 조준이 닿는 칸(가림 적용). 대상이 있는지는 보지 않는다 — "여기 서면 맞을 수 있나"를 볼 때 쓴다."""
    _, direct, splash = coverage_parts(state, unit, aim)
    return direct | splash


def hit_damage(state: BattleState, attacker: Unit, hit: Hit, common: int) -> int:
    """대상 하나가 받는 피해(TargetDamagePipeline + CombatDamageCalculator.Attack). 보호막 흡수는 그다음."""
    base = common * hit.percent // 100 if hit.role == "additional" else common
    if hit.kind == "structure":     # 구조물: 상성·방어·지형 없음
        return max(1, base) if base > 0 else base
    victim = state.units[hit.target]
    damage = (base * matchup(attacker.defn.category, victim.defn.category) + 5) // 10   # half-up
    damage = damage * (100 - min(effective_defense(victim), 100)) // 100
    board = state.board
    percent = 100
    if victim.cell in board.sandstorm:
        percent = 80
    elif victim.cell in board.high_ground and hit.origin not in board.high_ground:
        percent = 70
    if "mark_pct" in victim.statuses:   # 사냥꾼의 표식(약점 노출): 근사로 마지막에 +%
        damage = damage * percent * (100 + victim.statuses["mark_pct"]) // 10000
    else:
        damage = damage * percent // 100
    return max(1, damage) if base > 0 else damage


def _solo_shield_active(state: BattleState, unit: Unit) -> bool:
    """단독 행동: 반경 안에 다른 산 유닛이 없을 때만 보호막이 막는다(SoloActionTraitRule.Eligible)."""
    solo = unit.defn.trait("SoloAction")
    if solo is None or unit.shield <= 0:
        return False
    radius = int(solo.data.get("radiusCells", 1))
    return not any(other.uid != unit.uid and _chebyshev(other.cell, unit.cell) <= radius for other in state.living())


def _deal(state: BattleState, target: Unit | Structure, amount: int) -> dict:
    """피해를 적용한다. 단독 행동 보호막이 먼저 흡수한다."""
    absorbed = 0
    if isinstance(target, Unit) and amount > 0 and _solo_shield_active(state, target):
        absorbed = min(target.shield, amount)
        target.shield -= absorbed
    before = target.hp
    target.hp = max(0, target.hp - (amount - absorbed))
    return {"target": getattr(target, "uid", getattr(target, "sid", None)), "name": target.name, "damage": amount,
            "absorbed": absorbed, "hp": [before, target.hp], "killed": before > 0 and target.hp == 0}


def _apply_attack(state: BattleState, attack: Attack) -> None:
    unit = state.units.get(attack.uid)
    if unit is None:
        raise RuleError(f"{attack.uid}번 유닛이 없습니다")
    dead_before = {u.uid for u in state.units.values() if not u.alive}
    plan = resolve_attack(state, unit, attack.aim)
    if plan.pull is not None:
        plan = AttackPlan(_execute_pull(state, unit, plan.pull), plan.cells, plan.primary, plan.pull)
    common = common_damage(unit)
    damages = [(hit, hit_damage(state, unit, hit, common)) for hit in plan.hits]
    record, kills = [], 0
    for hit, damage in damages:   # 모든 피해는 같은 공통 피해로 먼저 계산하고 차례로 적용한다
        target: Unit | Structure = state.units[hit.target] if hit.kind == "unit" else state.structures[hit.target]
        result = _deal(state, target, damage)
        result.update(kind=hit.kind, role=hit.role)
        record.append(result)
        if hit.kind == "unit" and result["killed"] and target.side is not unit.side:
            kills += 1
        if hit.kind == "unit" and target.alive and target.side is not unit.side and damage > 0:
            _apply_hit_statuses(unit, target)
    unit.use = ActionUse.ATTACKED
    state.log.append({"type": "attack", "uid": unit.uid, "unit": unit.name, "aim": attack.aim, "hits": record})
    _check_outcome(state)
    if state.outcome is None:
        _knockback(state, unit, plan)
    settled = set(dead_before)
    _settle_deaths(state, settled)
    if state.outcome is None and has_trait(unit, "KillChainAttack"):
        _kill_chain(state, unit, kills, settled)
    _collapse_weak_ground(state)


def _execute_pull(state: BattleState, unit: Unit, pull: PullMove) -> tuple[Hit, ...]:
    """대상을 한 칸씩 옮긴다(약한 지반 진입을 세고, 낭떠러지면 떨어진다). 끝 칸에 살아서 닿고 적이면 그 자리에서 맞는다."""
    target = state.units[pull.target]
    start = target.cell
    for cell in pull.path:
        target.cell = cell
        _register_entry(state, cell)
        if cell in state.cliffs:
            _fall(state, target)
            break
    state.log.append({"type": "pull", "uid": unit.uid, "unit": unit.name, "target": target.uid,
                      "targetName": target.name, "from": start, "to": target.cell, "fell": not target.alive})
    _check_outcome(state)
    if (pull.damage and target.alive and target.cell == pull.path[-1] and unit.alive and state.outcome is None):
        return (Hit("unit", target.uid, "primary", 100, unit.cell),)
    return ()


def _apply_hit_statuses(attacker: Unit, victim: Unit) -> None:
    """맞고 살아남은 적에게 붙는 상태(무장 해제·발목 꺾기·방어구 부수기·사냥꾼의 표식)."""
    for kind, key in (("Disarm", "disarm"), ("AnkleBreaker", "bind")):
        trait = attacker.defn.trait(kind)
        if trait is not None:
            victim.statuses[key] = max(victim.statuses.get(key, 0), int(trait.data.get("durationAffectedSideTurnEnds", 1)))
    armor = attacker.defn.trait("ArmorBreaker")
    if armor is not None:
        victim.statuses["armor"] = victim.statuses.get("armor", 0) + int(armor.data.get("defenseReductionPercentPerHit", 0))
    mark = attacker.defn.trait("HuntersMark")
    if mark is not None:
        victim.statuses["mark"] = int(mark.data.get("durationAffectedSideTurnEnds", 1))
        victim.statuses["mark_pct"] = int(mark.data.get("bonusDamagePercent", 0))


# ---------------------------------------------------------------- 넉백 (AttackKnockbackCalculator, AttackForcedMovement)
def _sign(value: int) -> int:
    return (value > 0) - (value < 0)


def knockback_immune(unit: Unit) -> bool:
    """강제 이동 면역(보스 등) 또는 플레이어 중장의 굳건한 의지(KnockbackImmunityCheck)."""
    return unit.defn.forced_move_immune or has_trait(unit, "SteadfastWill")


def _knockback(state: BattleState, attacker: Unit, plan: AttackPlan) -> None:
    """모든 직접 피해 뒤에, 맞은 산 유닛을 ID 순서로 한 칸씩 민다. 충격 볼트는 충돌 중심에서 바깥으로."""
    knock, bolt = attacker.defn.trait("Knockback"), attacker.defn.trait("ImpactBolt")
    if knock is None and bolt is None:
        return
    distance = max(int(t.data.get("distanceCells", 1)) for t in (knock, bolt) if t is not None)
    impact = bolt is not None and (knock is None or int(bolt.data.get("distanceCells", 1)) >= distance)
    if impact and plan.primary is None:
        return
    origin = plan.primary if impact else attacker.cell
    for uid in sorted({h.target for h in plan.hits if h.kind == "unit"}):
        if state.outcome is not None:
            break
        victim = state.units[uid]
        if not victim.alive or victim.cell == origin:
            continue   # 죽은 대상(시체 밀기는 근사로 뺌), 충돌 중심의 대상은 밀지 않는다
        step = (_sign(victim.cell[0] - origin[0]), _sign(victim.cell[1] - origin[1]))
        if knockback_immune(victim):
            state.log.append({"type": "knockback", "uid": uid, "unit": victim.name, "stop": "immune"})
            continue
        _push(state, victim, step, distance)


def _push(state: BattleState, victim: Unit, step: Cell, distance: int) -> None:
    """강제 이동 한 칸씩: 판 밖·벽·낮은 칸 → 고지대 턱·산 유닛·구조물이면 멈춘다. 낭떠러지에 들어가면 떨어진다."""
    board = state.board
    path = []
    for _ in range(distance):
        here = victim.cell
        to = (here[0] + step[0], here[1] + step[1])
        blocked = (not board.inside(to) or board.walls.occupies(to)
                   or (to in board.high_ground and here not in board.high_ground)
                   or state.unit_at(to) is not None or state.structure_at(to) is not None)
        if blocked:
            break
        victim.cell = to
        path.append(to)
        _register_entry(state, to)
        if to in state.cliffs:
            _fall(state, victim)
            break
    state.log.append({"type": "knockback", "uid": victim.uid, "unit": victim.name, "path": path,
                      "fell": not victim.alive})
    _check_outcome(state)


# ---------------------------------------------------------------- 사망 특성·처치 연쇄
def _settle_deaths(state: BattleState, handled: set[int]) -> None:
    """자폭: 죽은(추락이 아닌) 보유자 주변 8칸의 산 유닛·구조물에 공격력 × 레벨 % 피해, 방어 무시, 연쇄(DeathTraitApplication)."""
    while state.outcome is None:
        round_ = [u for u in sorted(state.units.values(), key=lambda u: u.uid)
                  if not u.alive and u.uid not in handled]
        if not round_:
            return
        for unit in round_:
            handled.add(unit.uid)
            destruct = unit.defn.trait("SelfDestruct")
            if destruct is None or "fell" in unit.statuses:
                continue
            percent = {1: 50, 2: 75}.get(int(destruct.data.get("level", 1)), 110)   # EnemyDeathTraitLevels
            amount = unit.attack * percent // 100
            area = {(unit.cell[0] + dx, unit.cell[1] + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)} - {unit.cell}
            hits = []
            targets = [u for u in state.living() if u.cell in area] + \
                      [s for s in state.structures.values() if s.alive and set(s.cells) & area]
            for target in targets:
                hits.append(_deal(state, target, amount))
            state.log.append({"type": "self_destruct", "uid": unit.uid, "unit": unit.name, "hits": hits})
            _check_outcome(state)
            if state.outcome is not None:
                return


def _kill_chain(state: BattleState, unit: Unit, kills: int, settled: set[int]) -> None:
    """처치 연쇄: 처치마다 맵 어디든 적 하나를 무작위로 한 번 더 친다. 그 처치도 다시 이어진다(KillChainAttackCalculator)."""
    pending = kills
    while pending > 0 and state.outcome is None and unit.alive:
        pending -= 1
        enemies = sorted((u for u in state.living(unit.side.other)), key=lambda u: u.uid)
        if not enemies:
            return
        target = enemies[state.rng.randrange(len(enemies))]
        damage = hit_damage(state, unit, Hit("unit", target.uid, "primary", 100, unit.cell), common_damage(unit))
        result = _deal(state, target, damage)
        state.log.append({"type": "kill_chain", "uid": unit.uid, "unit": unit.name, "hit": result})
        if result["killed"]:
            pending += 1
        _check_outcome(state)
        _settle_deaths(state, settled)


# ---------------------------------------------------------------- 할 수 있는 행동
def attack_aims(state: BattleState, unit: Unit) -> list:
    """조준 후보(성공 여부는 따지지 않음)."""
    method = unit.defn.targeting
    if method.choice is ChoiceMode.DIRECTION:
        return list(supported_directions(method.shape))
    if method.choice is ChoiceMode.ALL:
        return [None]
    r = selection_range(state, unit)
    ox, oy = unit.cell
    aims = []
    for y in range(-r, r + 1):
        for x in range(-r, r + 1):
            if base_contains(method.shape, r, x, y) and angle_contains(method.angle, unit.side, x, y):
                cell = (ox + x, oy + y)
                if state.board.inside(cell):
                    aims.append(cell)
    return aims


def legal_attacks(state: BattleState, unit: Unit, useful_only: bool = True) -> list[tuple[Attack, AttackPlan]]:
    if not can_attack(state, unit):
        return []
    result = []
    for aim in attack_aims(state, unit):
        try:
            plan = resolve_attack(state, unit, aim)
        except RuleError:
            continue
        if useful_only and not plan.hits and plan.pull is None:
            continue
        # 같은 편만 맞는 조준(캐논이 제 편을 쏘는 것)은 뺀다. 판 값이 죽은 같은 편의 거리 벌점·위험을 함께 지워
        # 그런 조준을 좋게 볼 수 있어서다(2026-11-18). 같은 편이 스플래시로 함께 맞는 조준·끌어오기는 남긴다.
        # Unity 계획 AI PlannerCandidates.Aims와 같은 규칙.
        if useful_only and plan.pull is None and unit.defn.trait("Pull") is None and plan.hits and all(
                (state.units[h.target] if h.kind == "unit" else state.structures[h.target]).side is unit.side
                for h in plan.hits):
            continue
        result.append((Attack(unit.uid, aim), plan))
    return result


def legal_actions(state: BattleState, useful_only: bool = True) -> list[Action]:
    """지금 턴 편이 할 수 있는 행동 전부. 마지막은 늘 턴 끝내기."""
    if state.outcome is not None:
        return []
    actions: list[Action] = []
    for unit in sorted(state.living(state.turn_side), key=lambda u: u.uid):
        for path in reachable(state, unit).values():
            actions.append(Move(unit.uid, path))
        actions.extend(attack for attack, _ in legal_attacks(state, unit, useful_only))
    actions.append(EndTurn())
    return actions
