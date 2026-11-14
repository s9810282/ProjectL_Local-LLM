"""지금 게임 적 AI(Utility 세션)를 시뮬레이터 판 위로 옮긴 것. 설계 §A5 "게임 AI".

원본: Core Application/Combat/EnemyAi/ (Project_L develop 424242ab8)
  EnemyDecisionSession.SelectUtility, EnemyActionCandidates.Build, EnemyEngagementField, EnemyThreatField,
  CombatActionAssessment. 점수식은 tools/rule_game.py의 `terms`·`attack_benefit`을 그대로 쓴다(한 곳).

행동하는 진영(side)을 인자로 받는다. 게임에서는 적만 쓰지만, 같은 절차를 플레이어 쪽에도 쓸 수 있게 했다
(영리한 플레이어 모형, sim/player.py). 한 번 부를 때 행동 하나를 고른다.
1. 행동할 수 있는 유닛(Available·Moved)을 "지금 칠 수 있는 유닛 먼저, 그다음 번호 순"으로 돈다.
2. 유닛마다 후보(지금 칸에서의 공격 최대 4개 + 이동 빔 6개)를 점수 매기고, 대기보다 확실히 높은 것이 있으면 그 유닛이 한다.
3. 유닛끼리는 비교하지 않는다. 아무도 대기보다 나은 게 없으면 턴을 끝낸다.

게임과 다른 점(지금 판에 없는 것): 도발, 기믹 예고(낙석·하붑·기차·토네이도 피해·묶임), 가시밭, 지뢰, 위치 교환, 보스 포효.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from heapq import heappop, heappush
from pathlib import Path
import sys

from .assets import Cell, ChoiceMode
from .policy import Policy
from .rules import (Attack, EndTurn, Move, RuleError, apply, attack_aims, can_attack, can_move, common_damage,
                    coverage_parts, hit_damage, policy_of, resolve_attack, selection_range)
from .state import ActionUse, BattleState, Outcome, Structure, Unit
from .targeting import DIRECTIONS, Side, splash_offsets, supported_directions

_TOOLS = Path(__file__).resolve().parents[1] / "tools"
if str(_TOOLS) not in sys.path:
    sys.path.insert(0, str(_TOOLS))
from rule_game import OPPOSING_ELIMINATION_WEIGHT, attack_benefit, load_weights, loss_permille, terms  # noqa: E402

# EnemyDecisionSettings 기본값(Contracts/Combat/EnemyAi/EnemyDecisionSettings.cs)
ATTACK_LIMIT = 4
MOVE_BEAM = 6
REFINED_MOVES = 2
PREVIEW_PER_ACTOR = 8
PREVIEW_PER_DECISION = 16
FIELD_AREA_LIMIT = 289
ALLY_STEP_EXTRA = 2              # 아군이 있는 칸을 지나면 1 + 2 (EnemyAiTuning)
BFS_ORDER = ((1, 0), (0, 1), (-1, 0), (0, -1))   # Right, Up, Left, Down
LETHAL_RANK = (True, 1 << 30, 1 << 30, 0)

Target = Unit | Structure


def _chebyshev(a: Cell, b: Cell) -> int:
    return max(abs(a[0] - b[0]), abs(a[1] - b[1]))


def target_id(target: Target) -> int:
    return target.uid if isinstance(target, Unit) else target.sid


def target_cells(target: Target) -> tuple[Cell, ...]:
    return (target.cell,) if isinstance(target, Unit) else target.cells


@dataclass
class Scored:
    kind: str                      # "hold", "attack", "move"
    action: object | None
    facts: dict
    total: int = 0
    lethal: bool = False
    lethal_rank: tuple = (False, 0, 0, 0)
    precise: bool = False
    endpoint: Cell = (0, 0)
    detail: dict = field(default_factory=dict)

    def intent(self) -> tuple:
        """EnemyUtilityScorer.CompareIntent: 공격이 이동보다 앞, 그다음 칸."""
        aim = self.action.aim if isinstance(self.action, Attack) else None
        cell = aim if isinstance(aim, tuple) else self.endpoint
        return (0 if self.kind == "attack" else 1, cell[1], cell[0], str(aim))


# ---------------------------------------------------------------- 평가 공통
def _restricted(state: BattleState, side: Side) -> int:
    return sum(("disarm" in u.statuses) + ("bind" in u.statuses) for u in state.living(side))


def precise_benefit(before: BattleState, after: BattleState, side: Side = Side.ENEMY, objective=None) -> int:
    """정밀 평가(CombatActionAssessment): 유닛·구조물마다 HP 손실 천분율, 처치, 제약 변화. side = 행동하는 진영.
    objective는 게임 AI에는 없는 승리 조건 인식(영리한 플레이어용, Objective)."""
    change = {"opponentLoss": 0, "opponentKills": 0, "alliedLoss": 0, "alliedKills": 0}
    for uid, unit in before.units.items():
        if not unit.alive:
            continue
        later = after.units[uid]
        lost = max(0, unit.hp - max(0, later.hp)) * 1000 // unit.max_hp
        group = "allied" if unit.side is side else "opponent"
        change[f"{group}Loss"] += lost
        change[f"{group}Kills"] += not later.alive
    for sid, structure in before.structures.items():
        if not structure.alive:
            continue
        later = after.structures[sid]
        group = "allied" if structure.side is side else "opponent"
        lost = max(0, structure.hp - later.hp) * 1000 // structure.max_hp
        if objective is not None and group == "opponent":
            lost *= objective.structure_weight
        change[f"{group}Loss"] += lost
        change[f"{group}Kills"] += not later.alive
    restriction = ((_restricted(after, side.other) - _restricted(before, side.other))
                   - (_restricted(after, side) - _restricted(before, side)))
    value = attack_benefit(change, restriction)
    if objective is not None and after.outcome is not None and after.outcome is not before.outcome:
        value += objective.win_bonus if _won(after, side) else -objective.win_bonus
    return value


def _won(state: BattleState, side: Side) -> bool:
    return state.outcome is (Outcome.PLAYER_WIN if side is Side.PLAYER else Outcome.ENEMY_WIN)


@dataclass(frozen=True)
class Objective:
    """승리 조건 인식(게임 AI에는 없음): 상대 본진 손실 가중치, 이기는(지는) 행동의 가산, 본진을 처음부터 대상에 넣을지."""
    structure_weight: int = 1
    win_bonus: int = 0
    structures_with_units: bool = False
    move_to_structures: bool = False      # 이동(교전 비용·다음 턴)은 상대 본진만 향한다. 공격은 그대로


def rough_benefit(state: BattleState, actor: Unit, aim, objective=None) -> int:
    """어림 평가(EstimateAttackSteps): 공통 피해 × 적용 비율, 지금 HP까지. 처치·제약은 세지 않는다."""
    plan = resolve_attack(state, actor, aim)
    common = common_damage(actor)
    change = {"opponentLoss": 0, "opponentKills": 0, "alliedLoss": 0, "alliedKills": 0}
    for hit in plan.hits:
        target = state.units[hit.target] if hit.kind == "unit" else state.structures[hit.target]
        base = common * hit.percent // 100
        group = "opponent" if target.side is not actor.side else "allied"
        lost = loss_permille(base, target.hp, target.max_hp)
        if objective is not None and group == "opponent" and hit.kind == "structure":
            lost *= objective.structure_weight
        change[f"{group}Loss"] += lost
    return attack_benefit(change, 0)


# ---------------------------------------------------------------- 공격 조준 (CombatAttackQueries.EnumerateAimsThatCanHit)
def aims_for_target(state: BattleState, actor: Unit, cells: tuple[Cell, ...]) -> list:
    method = actor.defn.targeting
    if method.choice is ChoiceMode.SINGLE:
        aims = set()
        for cell in cells:
            aims.add(cell)
            aims |= {(cell[0] + dx, cell[1] + dy) for dx, dy in splash_offsets(method.splash)}
        return sorted(aims, key=lambda c: (c[1], c[0]))
    if method.choice is ChoiceMode.DIRECTION:
        return [d for d in DIRECTIONS if d in supported_directions(method.shape)]
    return [None]


def delivers(state: BattleState, actor: Unit, aim, target: Target) -> bool:
    """그 조준이 그 대상에게 실제로 피해를 주나(DeliveryCheck)."""
    try:
        plan = resolve_attack(state, actor, aim)
    except RuleError:
        return False
    kind = "unit" if isinstance(target, Unit) else "structure"
    common = common_damage(actor)
    return any(h.kind == kind and h.target == target_id(target) and hit_damage(state, actor, h, common) > 0
               for h in plan.hits)


def opponents(state: BattleState, side: Side) -> tuple[list[Unit], list[Structure]]:
    """대상: 상대 유닛(번호 순)과 상대 본진(산 것)."""
    units = sorted(state.living(side.other), key=lambda u: u.uid)
    structures = sorted((s for s in state.structures.values() if s.alive and s.side is side.other),
                        key=lambda s: s.sid)
    return units, structures


def attack_candidates(state: BattleState, actor: Unit, limit: int = ATTACK_LIMIT) -> list:
    """지금 칸에서의 공격 조준. 대상 번호 순으로 처음 찾은 것부터 유닛 조준 최대 4개 + 본진 조준 1개."""
    if not can_attack(state, actor):
        return []
    units, structures = opponents(state, actor.side)
    aims, seen = [], set()
    for target in units:
        for aim in aims_for_target(state, actor, target_cells(target)):
            if len(aims) >= limit:
                break
            if aim not in seen and delivers(state, actor, aim, target):
                seen.add(aim)
                aims.append(aim)
    for structure in structures:
        hq_aim = next((aim for aim in aims_for_target(state, actor, structure.cells)
                       if aim not in seen and delivers(state, actor, aim, structure)), None)
        if hq_aim is not None:
            if len(aims) >= limit:
                aims[-1] = hq_aim      # 유닛 조준이 가득이면 마지막을 본진 조준으로 바꾼다
            else:
                aims.append(hq_aim)
            break
    return aims


# ---------------------------------------------------------------- 위협 필드 (EnemyThreatField)
def threat_field(state: BattleState, attacker_side: Side = Side.PLAYER) -> dict[Cell, int]:
    """attacker_side 유닛의 지금 자리에서 닿는 칸마다 공통 피해의 최댓값(방어·상성 없음). 이동해서 치는 것은 안 본다."""
    result: dict[Cell, int] = {}
    for unit in state.living(attacker_side):
        if not unit.defn.can_attack or "disarm" in unit.statuses:
            continue
        damage = common_damage(unit)
        splash = damage * unit.defn.targeting.splash_percent // 100
        policy = policy_of(unit)
        for aim in attack_aims(state, unit):
            try:
                primary, direct, extra = coverage_parts(state, unit, aim)
            except RuleError:
                continue
            for cell in direct:
                result[cell] = max(result.get(cell, 0), damage)
            if extra and primary is not None:
                occupant = state.unit_at(primary)
                structure = state.structure_at(primary)
                eligible = ((occupant is not None and occupant.side is not attacker_side)
                            or (structure is not None and structure.side is not attacker_side))
                if policy.allows_empty_single or eligible:
                    for cell in extra:
                        result[cell] = max(result.get(cell, 0), splash)
    return result


def threat_at(threat: dict[Cell, int], actor: Unit, cell: Cell, hp: int) -> tuple[int, bool]:
    damage = threat.get(cell, 0)
    return loss_permille(damage, hp, actor.max_hp), damage > 0 and damage >= hp


# ---------------------------------------------------------------- 교전 필드 (EnemyEngagementField)
class EngagementField:
    """공격할 수 있는 칸(비용 0)까지 거꾸로 잰 걸음 비용. 아군이 있는 칸을 지나면 3.
    대상은 상대 유닛이 먼저이고, 어떤 유닛도 출발 칸에 비용을 주지 못할 때만 상대 본진을 쓴다."""

    def __init__(self, state: BattleState, actor: Unit, objective=None):
        self.actor = actor
        self._objective = objective
        self.costs: dict[Cell, int] = {}
        self.best: dict[Cell, Target] = {}
        self.values: dict[int, int] = {}
        self.truncated = False
        board = state.board
        side = actor.side
        hostiles = {u.cell for u in state.living(side.other)}
        self.ally_cells = {u.cell for u in state.living(side) if u.uid != actor.uid}
        structures = {c for s in state.structures.values() if s.alive for c in s.cells}

        def passable(cell):
            return (board.inside(cell) and cell not in structures and not state.blocks_entry(cell)
                    and cell not in hostiles)

        def collapsing(cell):
            return cell in state.weak_entries and state.weak_entries[cell] + 1 >= board.weak_threshold[cell]

        area, pending = {actor.cell}, [actor.cell]
        while pending:
            cell = pending.pop()
            for dx, dy in BFS_ORDER:
                nxt = (cell[0] + dx, cell[1] + dy)
                if nxt not in area and passable(nxt):
                    area.add(nxt)
                    pending.append(nxt)
                    if len(area) > FIELD_AREA_LIMIT:
                        self.truncated = True
                        return
        self._area = area
        self._landable = {c for c in area if (c not in self.ally_cells and not collapsing(c)) or c == actor.cell}
        self._common = common_damage(actor)
        probe = state.clone()
        probe.turn_side = side
        mover = probe.units[actor.uid]
        mover.use = ActionUse.MOVED
        mover.statuses["moved"] = 1
        mover.statuses.pop("disarm", None)
        self._probe, self._mover = probe, mover
        units, structures_ = opponents(state, side)
        together = objective is not None and objective.structures_with_units
        if objective is not None and objective.move_to_structures and structures_:
            self._fill(structures_)
        else:
            self._fill(units + structures_ if together else units)
        if actor.cell not in self.costs and structures_ and not together:
            self.costs, self.best = {}, {}
            self._fill(structures_)
        mover.cell = actor.cell

    def _fill(self, targets) -> None:
        method = self.actor.defn.targeting
        radius = selection_range(self._probe, self._mover) + (1 if method.splash_percent else 0)
        for target in targets:
            ident = target_id(target)
            weight = self._objective.structure_weight if self._objective and isinstance(target, Structure) else 1
            self.values[ident] = loss_permille(self._common, target.hp, target.max_hp) * weight + \
                (OPPOSING_ELIMINATION_WEIGHT if self._common >= target.hp else 0)
            probe_target = (self._probe.units[ident] if isinstance(target, Unit) else self._probe.structures[ident])
            cells = target_cells(target)
            sources = []
            for cell in self._landable:
                if method.choice is not ChoiceMode.ALL and min(_chebyshev(cell, c) for c in cells) > radius:
                    continue
                self._mover.cell = cell
                if any(delivers(self._probe, self._mover, aim, probe_target)
                       for aim in aims_for_target(self._probe, self._mover, cells)):
                    sources.append(cell)
            for cell, cost in self._dijkstra(sources).items():
                current = self.best.get(cell)
                key = (cost, -self.values[ident], isinstance(target, Structure), ident)
                if current is None or key < (self.costs[cell], -self.values[target_id(current)],
                                             isinstance(current, Structure), target_id(current)):
                    self.costs[cell] = cost
                    self.best[cell] = target

    def _dijkstra(self, sources) -> dict[Cell, int]:
        costs = {cell: 0 for cell in sources}
        queue = [(0, cell[1], cell[0]) for cell in sources]
        while queue:
            cost, y, x = heappop(queue)
            cell = (x, y)
            if costs.get(cell) != cost:
                continue
            step = 1 + (ALLY_STEP_EXTRA if cell in self.ally_cells else 0)
            for dx, dy in BFS_ORDER:
                nxt = (x + dx, y + dy)
                if nxt in self._area and cost + step < costs.get(nxt, 1 << 30):
                    costs[nxt] = cost + step
                    heappush(queue, (cost + step, nxt[1], nxt[0]))
        return costs

    def progress(self, origin: Cell, endpoint: Cell) -> int:
        if self.truncated or origin not in self.costs or endpoint not in self.costs:
            return 0
        return self.costs[origin] - self.costs[endpoint]

    def next_turn(self, endpoint: Cell) -> int:
        """종점에서 다음 턴에 칠 수 있는 대상 하나의 값(결정 시작 HP 기준). 이동력 안에 공격 칸이 있어야 한다."""
        if self.truncated or endpoint not in self.costs or endpoint not in self.best:
            return 0
        if self.costs[endpoint] > self.actor.defn.move:
            return 0
        return self.values[target_id(self.best[endpoint])]

    def best_value(self, cell: Cell) -> int:
        return self.values[target_id(self.best[cell])] if cell in self.best else 0

    def is_attack_position(self, cell: Cell) -> bool:
        return not self.truncated and self.costs.get(cell) == 0


# ---------------------------------------------------------------- 결정 (EnemyDecisionSession.SelectUtility)
class GameAIPolicy(Policy):
    def __init__(self, profile: str = "asset", side: Side = Side.ENEMY):
        self.weights = load_weights(profile=profile)
        self.side = side
        self.objective: Objective | None = None     # 게임 AI는 없음
        self.name = f"{'enemy' if side is Side.ENEMY else 'player'}-game-{profile}"
        self._holds: dict[int, str] = {}
        self.last_decision: dict = {}

    def holds(self) -> dict[int, str]:
        return dict(self._holds)

    def extra_lethal(self, state: BattleState, actor: Unit, endpoint: Cell, hp: int) -> int | None:
        """하위 정책이 더하는 치명 판정(게임 AI 자체는 없음). 치명이면 맞을 피해(작을수록 덜 치명), 아니면 None."""
        return None

    def extra_penalty(self, state: BattleState, actor: Unit, endpoint: Cell, hp: int) -> int:
        """하위 정책이 더하는 위치 벌점(게임 AI 자체는 없음)."""
        return 0

    def act(self, state: BattleState):
        if state.turn_side is not self.side:
            raise ValueError(f"{self.name}: 지금은 {state.turn_side.value} 턴입니다")
        decision = _Decision(state, self)
        action = decision.select()
        self.last_decision = decision.trace
        if isinstance(action, EndTurn):
            self._holds = decision.reasons
        return action


class _Decision:
    def __init__(self, state: BattleState, policy: GameAIPolicy):
        self.state = state
        self.policy = policy
        self.weights = policy.weights
        self.side = policy.side
        self.objective = policy.objective
        self.budget = PREVIEW_PER_DECISION
        self.threat = threat_field(state, self.side.other)
        self.reasons: dict[int, str] = {}
        self.trace: dict = {}

    def _charge(self, actor_budget: list) -> bool:
        if self.budget > 0 and actor_budget[0] > 0:
            self.budget -= 1
            actor_budget[0] -= 1
            return True
        return False

    def _score(self, scored: Scored, actor: Unit, hp: int) -> Scored:
        scored.detail = terms(scored.facts, self.weights)
        scored.total = scored.detail["total"]
        if hp <= 0:
            scored.lethal, scored.lethal_rank = True, LETHAL_RANK
            return scored
        damage = self.policy.extra_lethal(self.state, actor, scored.endpoint, hp)
        if damage is not None:
            scored.lethal, scored.lethal_rank = True, (True, damage, 0, 0)
        penalty = self.policy.extra_penalty(self.state, actor, scored.endpoint, hp)
        if penalty:
            scored.detail = {**scored.detail, "extraPenalty": -penalty}
            scored.total -= penalty
        return scored

    def _position(self, actor: Unit, hp: int, endpoint: Cell, field: EngagementField | None,
                  attack_value: int, progress: int = 0, followup: int = 0) -> dict:
        if hp <= 0:
            return {"attackBenefit": attack_value, "hazardLoss": 0, "threatPermille": 0, "threatLethal": False,
                    "progress": 0, "followup": 0, "nextTurnStrike": 0, "bind": False}
        permille, lethal = threat_at(self.threat, actor, endpoint, hp)
        return {"attackBenefit": attack_value, "hazardLoss": 0, "threatPermille": permille, "threatLethal": lethal,
                "progress": progress, "followup": followup,
                "nextTurnStrike": field.next_turn(endpoint) if field is not None else 0, "bind": False}

    def select(self):
        state = self.state
        actors = sorted((u for u in state.living(self.side) if u.use in (ActionUse.AVAILABLE, ActionUse.MOVED)),
                        key=lambda u: u.uid)
        if not actors:
            return EndTurn()
        attacks = {u.uid: attack_candidates(state, u) for u in actors}
        order = sorted(actors, key=lambda u: (0 if attacks[u.uid] else 1, u.uid))
        for actor in order:
            chosen = self._decide_actor(actor, attacks[actor.uid])
            if chosen is not None:
                return chosen.action
        return EndTurn()

    def _decide_actor(self, actor: Unit, aims: list) -> Scored | None:
        state = self.state
        actor_budget = [PREVIEW_PER_ACTOR]
        origin = actor.cell
        movable = can_move(state, actor)
        field = EngagementField(state, actor, self.objective) if movable else None
        hold = self._score(Scored("hold", None, self._position(actor, actor.hp, origin, field, 0), endpoint=origin),
                           actor, actor.hp)
        options: list[Scored] = []
        for aim in aims:
            attack = Attack(actor.uid, aim)
            if self._charge(actor_budget):
                after = apply(state, attack)
                post = after.units[actor.uid]
                hp = max(0, post.hp)
                scored = Scored("attack", attack, self._position(actor, hp, origin, field,
                                                                 precise_benefit(state, after, self.side, self.objective)),
                                precise=True, endpoint=origin)
            else:
                hp = actor.hp
                scored = Scored("attack", attack, self._position(actor, hp, origin, field,
                                                                 rough_benefit(state, actor, aim, self.objective)),
                                endpoint=origin)
            options.append(self._score(scored, actor, hp))
        if movable:
            options.extend(self._moves(actor, field, actor_budget))
        # 치명 거르기(EnemyDecisionSession.cs:221-239)
        safe = [o for o in options if not o.lethal]
        if safe:
            kept = [o for o in safe if hold.lethal or o.total > hold.total]
        elif options and hold.lethal:
            least = min(o.lethal_rank for o in options)
            kept = [o for o in options if o.lethal_rank == least and o.total > hold.total]
        else:
            kept = []
        self.trace[actor.uid] = {"unit": actor.name, "hold": hold.total, "holdLethal": hold.lethal,
                                 "options": [(o.kind, getattr(o.action, "aim", None) or o.endpoint, o.total,
                                              o.precise, o.lethal) for o in options]}
        if not kept:
            self.reasons[actor.uid] = ("no_option" if not options else
                                       "lethal_only" if not safe else "exchange_loss")
            return None
        top = max(o.total for o in kept)
        return min((o for o in kept if o.total == top), key=Scored.intent)

    # ---- 이동 (EnemyActionCandidates.MoveSteps, SelectBeam) ----
    def _landings(self, actor: Unit) -> dict[Cell, tuple[Cell, ...]]:
        state = self.state
        board = state.board
        occupied = {u.cell for u in state.living() if u.uid != actor.uid}
        structures = {c for s in state.structures.values() if s.alive for c in s.cells}

        def open_(cell):
            return (board.inside(cell) and not state.blocks_entry(cell) and cell not in occupied
                    and cell not in structures)

        parents = {actor.cell: None}
        frontier = [actor.cell]
        for _ in range(actor.defn.move):
            nxt_frontier = []
            for cell in frontier:
                for dx, dy in BFS_ORDER:
                    nxt = (cell[0] + dx, cell[1] + dy)
                    if nxt not in parents and open_(nxt):
                        parents[nxt] = cell
                        nxt_frontier.append(nxt)
            frontier = nxt_frontier
        paths = {}
        for cell in parents:
            if cell == actor.cell:
                continue
            path, step = [], cell
            while step is not None:
                path.append(step)
                step = parents[step]
            paths[cell] = tuple(reversed(path))
        return paths

    def _collapses(self, cell: Cell) -> bool:
        state = self.state
        return cell in state.weak_entries and state.weak_entries[cell] + 1 >= state.board.weak_threshold[cell]

    def _followup(self, world: BattleState, actor_uid: int, field: EngagementField, precise: bool) -> int:
        """종점에서의 후속 공격 하나: 값이 큰 대상부터(유닛이 본진보다 앞), 처음 찾은 조준(followupOnly)."""
        mover = world.units[actor_uid]
        if not mover.alive or not can_attack(world, mover):
            return 0
        units, structures = opponents(world, self.side)
        targets = sorted(units, key=lambda u: (-field.values.get(u.uid, 0), u.uid)) + structures
        for target in targets:
            for aim in aims_for_target(world, mover, target_cells(target)):
                if delivers(world, mover, aim, target):
                    if precise:
                        return precise_benefit(world, apply(world, Attack(actor_uid, aim)), self.side, self.objective)
                    return rough_benefit(world, mover, aim, self.objective)
        return 0

    def _moves(self, actor: Unit, field: EngagementField, actor_budget: list) -> list[Scored]:
        state = self.state
        landings = {cell: path for cell, path in self._landings(actor).items() if not self._collapses(cell)}
        if not landings:
            return []
        cheap = {}
        for cell in landings:
            facts = self._position(actor, actor.hp, cell, field, 0, field.progress(actor.cell, cell),
                                   field.best_value(cell) if field.is_attack_position(cell) else 0)
            cheap[cell] = terms(facts, self.weights)["total"]
        ranked = sorted(landings, key=lambda c: (-cheap[c], len(landings[c]), c[1], c[0]))
        beam: list[Cell] = []
        attack_spot = next((c for c in ranked if field.is_attack_position(c)), None)
        if attack_spot is not None:
            beam.append(attack_spot)
        progressing = [c for c in ranked if field.progress(actor.cell, c) > 0]
        if progressing:
            best_progress = max(progressing, key=lambda c: field.progress(actor.cell, c))
            if best_progress not in beam:
                beam.append(best_progress)
        for cell in ranked:
            if len(beam) >= MOVE_BEAM:
                break
            if cell not in beam:
                beam.append(cell)
        moves = []
        for cell in beam:
            relocated = state.clone()
            mover = relocated.units[actor.uid]
            mover.cell, mover.use = cell, ActionUse.MOVED
            mover.statuses["moved"] = 1
            relocated.turn_side = self.side
            followup = self._followup(relocated, actor.uid, field, False) if field.is_attack_position(cell) else 0
            facts = self._position(actor, actor.hp, cell, field, 0, field.progress(actor.cell, cell), followup)
            moves.append(self._score(Scored("move", Move(actor.uid, landings[cell]), facts, endpoint=cell),
                                     actor, actor.hp))
        refine = sorted(moves, key=lambda m: (m.lethal, -m.total, m.intent()))[:REFINED_MOVES]
        for rough in refine:
            if not self._charge(actor_budget):
                break
            after = apply(state, rough.action)
            post = after.units[actor.uid]
            hp = max(0, post.hp)
            followup = 0
            if post.alive and field.is_attack_position(rough.endpoint):
                followup = self._followup(after, actor.uid, field, self._charge(actor_budget))
            precise = Scored("move", rough.action,
                             self._position(actor, hp, rough.endpoint, field,
                                            precise_benefit(state, after, self.side, self.objective),
                                            field.progress(actor.cell, rough.endpoint), followup),
                             precise=True, endpoint=rough.endpoint)
            moves[moves.index(rough)] = self._score(precise, actor, hp)
        return moves
