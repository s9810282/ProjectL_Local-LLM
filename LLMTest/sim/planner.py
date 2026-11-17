"""턴 계획 AI(빔 탐색). 설계 docs/2026-11-13-turn-planner-design.md §3.

진영 공통이다. 한 턴 동안 우리 유닛들의 행동 묶음(누가 먼저, 어디로, 누구를)을 찾고, 부를 때마다 하나씩 돌려준다.
- 노드 = (판, 아직 안 움직인 유닛, 지금까지 행동). 단계마다 남은 유닛의 후보 행동을 적용하고 V로 매겨 상위 K개만 남긴다.
- 후보: 공격이 있는 조합은 모두, 이동만 하는 것은 도달 표로 고른 상위 몇 개. 그중 V 상위 N개만 확장한다.
- 예측 모드: 계획용 판은 난수를 고정 씨앗으로 다시 맞춘다. 실행 중 결과가 예측과 다르면 다시 계획한다.
- 2단 평가(설계 docs/2026-11-13-planner-engine-reply-design.md R3): 시작 판과 단계마다 남긴 판을 모아, 빠른 V 상위
  M개를 깊은 평가(다음 턴 추정을 엔진 응수로)로 다시 매겨 고른다. M = 0이면 빠른 V 1등(옛 동작).
- 빔 다양성(설계 docs/2026-11-13-planner-beam-diversity-design.md D1·D2): 켜면 단계마다 결과 열쇠(우리 유닛이 선 칸만
  뺀 판)가 같은 판은 빠른 V 상위 몇 개만 빔에 남긴다. 끄면(0) 옛 동작.
- 승리 대상 노림(설계 docs/2026-11-14-planner-goal-pressure-design.md): V의 G1·G2는 evaluate.py가 더한다.
  여기서는 이동만 하는 후보 점수에 G1의 유닛 하나 몫을 더한다(P5). 끄면(0) 옛 동작.
"""

from __future__ import annotations

from dataclasses import dataclass, field, replace
import random
import time

from .analysis import Option, attack_destinations, simulate, unit_options
from .evaluate import Evaluator, PlannerConfig
from .policy import Policy
from .rules import EndTurn
from .state import ActionUse, BattleState, Unit
from .targeting import Side


def planner_from_name(side: Side, name: str) -> "PlannerPolicy":
    """이름으로 계획 AI를 만든다:
    plan{K}[:f{겁}][:o{기회}][:n{유닛당 후보}][:r{다시 매김}][:d{결과마다 빔 수}][:g{승리 대상 걸음}][:t{함께 닿음}]
        [:m{이동 + 공격 도착 칸 수}][:q{응수 도착 칸 수}]
    (예: plan4 = 다시 매김 4, plan4:r0 = 다시 매김 끔(옛 동작), plan4:d1 = 빔 다양성 켬, plan4:f0.8:o0.3:r8,
    plan4:g1:t0.5 = 승리 대상 노림 켬 — 설계 docs/2026-11-14-planner-goal-pressure-design.md,
    plan4:g1:m3:q2 = 이동 + 공격 도착 칸 거름 — 설계 docs/2026-11-18-planner-candidate-pruning-design.md)."""
    head, *rest = name.split(":")
    values = {"beam": int(head[4:] or PlannerConfig().beam)}
    for part in rest:
        key = {"f": "fear", "o": "opportunity", "n": "per_unit", "r": "rerank", "d": "distinct",
               "g": "goal_step", "t": "goal_together", "m": "move_attack_cells",
               "q": "reply_move_attack_cells"}.get(part[:1])
        if key is None:
            raise ValueError(f"모르는 계획 AI 설정: {part} "
                             "(f겁, o기회, n유닛당 후보, r다시 매김, d빔 다양성, g승리 대상 걸음, t함께 닿음, "
                             "m이동 + 공격 도착 칸, q응수 도착 칸)")
        integer = key in ("per_unit", "rerank", "distinct", "move_attack_cells", "reply_move_attack_cells")
        values[key] = int(part[1:]) if integer else float(part[1:])
    if values.get("rerank", 0) < 0:
        raise ValueError(f"다시 매김 수는 0 이상이어야 합니다: {name}")
    if values.get("distinct", 0) < 0:
        raise ValueError(f"빔 다양성 수는 0 이상이어야 합니다: {name}")
    if values.get("goal_step", 0) < 0 or values.get("goal_together", 0) < 0:
        raise ValueError(f"승리 대상 노림 값은 0 이상이어야 합니다: {name}")
    if values.get("move_attack_cells", 0) < 0 or values.get("reply_move_attack_cells", 0) < 0:
        raise ValueError(f"도착 칸 수는 0 이상이어야 합니다: {name}")
    config = PlannerConfig(**values)
    prefix = "enemy" if side is Side.ENEMY else "player"
    return PlannerPolicy(side, config, name=f"{prefix}-{name}")


def prediction_seed(state: BattleState, side: Side) -> int:
    """계획용 판의 고정 씨앗(라운드·진영·다음 ID). 문자열 hash는 프로세스마다 달라(PYTHONHASHSEED) 쓰지 않는다.
    설계 planner-engine-reply Q-R4."""
    side_index = 0 if side is Side.PLAYER else 1
    return ((state.round * 2 + side_index) * 1_000_003 + state.next_id) & 0xFFFFFFFF


def signature(state: BattleState) -> tuple:
    """같은 판인지: 산 유닛의 자리·HP·행동 단계·보호막, 구조물 HP, 무너진 칸 수."""
    units = tuple(sorted((u.uid, u.cell, u.hp, u.use.value, u.shield) for u in state.units.values() if u.alive))
    structures = tuple(sorted((s.sid, s.hp) for s in state.structures.values()))
    return units, structures, len(state.cliffs), state.outcome


def result_key(state: BattleState, side: Side) -> tuple:
    """같은 결과인지: 판 서명에서 그 진영 유닛의 칸만 뺀 것(설계 planner-beam-diversity D1).
    상대 유닛의 칸은 남긴다(넉백·끌어오기로 어디로 보냈는지는 다른 결과다)."""
    units = tuple(sorted((u.uid, None if u.side is side else u.cell, u.hp, u.use.value, u.shield)
                         for u in state.units.values() if u.alive))
    structures = tuple(sorted((s.sid, s.hp) for s in state.structures.values()))
    return units, structures, len(state.cliffs), state.outcome


@dataclass
class _Node:
    state: BattleState
    remaining: frozenset
    steps: list = field(default_factory=list)      # [(행동, 그 뒤 판 서명)]
    value: float = 0.0


@dataclass
class PlanStats:
    plans: int = 0
    replans: int = 0
    nodes: int = 0          # 매긴 판 수
    seconds: float = 0.0    # 계획 전체(깊은 평가 포함)
    reranks: int = 0        # 다시 매긴 계획 수(후보가 둘 이상일 때만)
    deep_evals: int = 0     # 깊은 평가 수
    flips: int = 0          # 다시 매김이 빠른 V 1등과 다른 계획을 고른 수
    deep_seconds: float = 0.0
    beam_steps: int = 0     # 빔 단계 수(남긴 판이 있는 단계)
    beam_nodes: int = 0     # 빔에 남긴 판 수(단계 합)
    beam_results: int = 0   # 그중 결과 열쇠가 다른 것의 수(단계 합) — 빔 다양성 지표


class PlannerPolicy(Policy):
    def __init__(self, side: Side, config: PlannerConfig = PlannerConfig(), name: str | None = None):
        self.side = side
        self.config = config
        self.name = name or f"{'enemy' if side is Side.ENEMY else 'player'}-plan-k{config.beam}"
        self.stats = PlanStats()
        self._plan: list = []
        self._turn_key = None
        self.last_plan: dict = {}

    def act(self, state: BattleState):
        if state.turn_side is not self.side:
            raise ValueError(f"{self.name}: 지금은 {state.turn_side.value} 턴입니다")
        key = (state.round, state.turn_side)
        expected = self._plan[0][1] if self._plan else None
        if key != self._turn_key:
            self._turn_key = key
            self._plan = self._make_plan(state)
        elif self._plan and expected != signature(state):
            self.stats.replans += 1
            self._plan = self._make_plan(state)
        if not self._plan:
            return EndTurn()
        action, _, _ = self._plan.pop(0)
        return action

    # ---------------------------------------------------------------- 계획
    def _make_plan(self, state: BattleState) -> list:
        """[(행동, 행동 전 판 서명, 행동 뒤 판 서명)]. 실행 때 행동 전 서명이 다르면 다시 계획한다."""
        started = time.perf_counter()
        self.stats.plans += 1
        config = self.config
        seed = prediction_seed(state, self.side)
        root = state.clone()
        root.rng = random.Random(seed)
        evaluator = Evaluator(root, config)
        movable = frozenset(u.uid for u in root.living(self.side) if u.use in (ActionUse.AVAILABLE, ActionUse.MOVED))
        start = _Node(root, movable, [], evaluator.value(root, self.side))
        best = start
        pool = [start]                      # 다시 매김 후보: 시작 판 + 단계마다 남긴 판(서명이 같으면 처음 것)
        seen = {signature(root)}
        frontier = [start]
        while frontier:
            children: dict[tuple, _Node] = {}
            for node in frontier:
                if node.state.outcome is not None:   # 계획 안에서 전투가 끝난 판은 더 펼치지 않는다
                    continue
                for uid in sorted(node.remaining):
                    unit = node.state.units[uid]
                    if not unit.alive:
                        continue
                    for option, after, value in self._candidates(node.state, unit, evaluator):
                        steps = node.steps + [(a, s) for a, s in self._step_signatures(node.state, option)]
                        child = _Node(after, node.remaining - {uid}, steps, value)
                        key = signature(after)
                        if key not in children or children[key].value < value:
                            children[key] = child
            ranked = sorted(children.values(), key=lambda n: -n.value)
            frontier = self._keep(ranked)
            if frontier:
                self.stats.beam_steps += 1
                self.stats.beam_nodes += len(frontier)
                self.stats.beam_results += len({result_key(n.state, self.side) for n in frontier})
            for node in frontier:
                key = signature(node.state)
                if key not in seen:
                    seen.add(key)
                    pool.append(node)
            if frontier and frontier[0].value > best.value:
                best = frontier[0]
            if frontier and frontier[0].state.outcome is not None:
                break
        fast_best = best           # 빠른 V 1등(다시 매김을 끄면 이것을 둔다)
        deep = self._rerank(pool, evaluator, seed) if config.rerank_count > 0 else None
        if deep is not None:
            best = deep[0]
        self.stats.seconds += time.perf_counter() - started
        self.last_plan = {"value": fast_best.value, "start": start.value, "actions": len(best.steps),
                          "breakdown": evaluator.breakdown(best.state, self.side)}
        if deep is not None:
            self.last_plan.update({"chosenValue": best.value, "deep": deep[1], "flipped": deep[2]})
        # 실행용: 각 행동의 "행동 전 판 서명"(첫 행동은 지금 판)
        plan = []
        before = signature(state)
        for action, after_signature in best.steps:
            plan.append((action, before, after_signature))
            before = after_signature
        return plan

    def _keep(self, ranked: list[_Node]) -> list[_Node]:
        """빠른 V 순으로 줄 선 자식에서 빔에 남길 K개. 빔 다양성을 켜면 결과 열쇠마다 최대 distinct개
        (같은 열쇠 안에서는 빠른 V가 큰 것, 같으면 먼저 나온 것). 설계 planner-beam-diversity D2."""
        beam, limit = self.config.beam, self.config.distinct
        if limit <= 0:
            return ranked[:beam]
        kept, counts = [], {}
        for node in ranked:
            key = result_key(node.state, self.side)
            if counts.get(key, 0) >= limit:
                continue
            counts[key] = counts.get(key, 0) + 1
            kept.append(node)
            if len(kept) == beam:
                break
        return kept

    def _rerank(self, pool: list[_Node], evaluator: Evaluator, seed: int) -> tuple[_Node, dict, bool] | None:
        """빠른 V 상위 M개를 깊은 평가로 다시 매겨 (고른 판, 그 깊은 평가 내역, 빠른 V 1등과 다른가).
        후보가 하나뿐이면 None.
        같으면 빠른 V가 큰 것, 그것도 같으면 먼저 모은 것(정렬이 안정적이라 앞에 있는 것)."""
        top = sorted(pool, key=lambda n: -n.value)[:self.config.rerank_count]
        if len(top) < 2:
            return None
        started = time.perf_counter()
        chosen, chosen_breakdown = None, None
        for node in top:
            breakdown = evaluator.deep_breakdown(node.state, self.side, seed)
            if chosen is None or breakdown["total"] > chosen_breakdown["total"]:
                chosen, chosen_breakdown = node, breakdown
        self.stats.reranks += 1
        self.stats.deep_evals += len(top)
        flipped = chosen is not top[0]
        self.stats.flips += flipped
        self.stats.deep_seconds += time.perf_counter() - started
        return chosen, chosen_breakdown, flipped

    def _step_signatures(self, state: BattleState, option: Option):
        """옵션의 행동마다 (행동, 그 뒤 판 서명)."""
        current = state
        for action in option.actions():
            current = simulate(current, [action])
            yield action, signature(current)

    def _candidates(self, state: BattleState, unit: Unit, evaluator: Evaluator):
        """유닛 하나의 확장 후보: 공격 조합 전부 + 이동만 상위 move_only개 → V 상위 per_unit개."""
        config = self.config
        attacks, moves = [], []
        # 속도 개선 SP1: 이동 + 공격은 빠른 점수 상위 도착 칸에서만 만든다(끄면 None = 옛 동작)
        attack_cells = Evaluator.best_cells(
            attack_destinations(state, unit) if config.move_attack_cells > 0 else [],
            lambda cell: evaluator.move_attack_score(state, unit, cell), config.move_attack_cells)
        for option in unit_options(state, unit, attack_cells=attack_cells):
            (attacks if option.attack is not None else moves).append(option)
        if moves:
            moves.sort(key=lambda o: -self._quick_move_score(state, unit, o, evaluator))
            moves = moves[:config.move_only]
        scored = []
        for option in attacks + moves:
            after = simulate(state, option.actions())
            self.stats.nodes += 1
            scored.append((option, after, evaluator.value(after, self.side)))
        scored.sort(key=lambda item: -item[2])
        return scored[:config.per_unit]

    def _quick_move_score(self, state: BattleState, unit: Unit, option: Option, evaluator: Evaluator) -> float:
        """이동만 하는 후보를 엔진 없이 빠르게: 그 칸에서의 다음 턴 기회 − 겁 × 그 칸에서 맞을 피해
        − 그 칸의 G1 몫(승리 대상 걸음을 켰을 때, 설계 goal-pressure P5)."""
        cell = option.move.path[-1]
        moved = replace(unit, cell=cell)
        targets = evaluator._targets(state, self.side)
        _, _, gain = evaluator._best_strike(state, moved, targets, own=True)
        loss = evaluator.danger_loss(state, unit, cell)
        if gain == 0:   # 칠 대상이 없으면 가까워지는 쪽
            field = evaluator._distance_field(state, evaluator._targets(state, self.side))
            gain = -self.config.step_penalty * field.get(cell, 99)
        score = self.config.opportunity * gain - self.config.fear * loss
        if self.config.goal_step > 0:
            score -= evaluator.goal_penalty(state, moved, cell, evaluator.win_targets(state, self.side))
        return score
