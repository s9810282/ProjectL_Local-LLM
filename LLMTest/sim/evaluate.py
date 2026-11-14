"""턴 계획 AI의 판 평가 V(판, 진영). 설계 docs/2026-11-13-turn-planner-design.md §2, 시뮬레이터 설계 §B6(Q-E3).

V = 자산(우리) − 자산(상대) − 겁 × 위험(우리) + 기회 × 기회(우리) − 거리 벌점

- 유닛 전력 = 공격력 × 남은 HP (메인은 × main_weight)
- 적 자산에는 본진(본진 값 × 남은 HP 비율, 본진 값 = 전투 시작 때 적 유닛 전력 합)과 남은 증원(× 할인, 본진이 살아 있을 때)
- 위험·기회는 "유닛이 이 칸에 있으면 다음 턴에 어느 칸을 칠 수 있나"(도달 표)로 빠르게 계산한다.
  특성(처치 연쇄·넉백 등)은 위험·기회 예상에 넣지 않는다. 실제로 둔 행동의 결과에는 엔진이 다 넣는다.
- 깊은 판 평가 V_deep(deep_breakdown): 같은 식에서 위험·기회·거리 벌점만 엔진 응수(reply.py)로 바꾼다.
  계획 AI가 빔의 마지막 후보 몇 개를 다시 매길 때 쓴다. 설계 docs/2026-11-13-planner-engine-reply-design.md R2.
- 승리 대상 노림 G1·G2(goal_terms): 상대가 잃으면 지는 것(적 → 플레이어 메인, 플레이어 → 적 본진·격파 대상)을
  멀리서는 걸어 다가가고(G1) 가까이서는 함께 닿게(G2) 한다. 빠른 V·깊은 V 모두 같은 판·같은 도달 표로 더한다.
  손잡이를 끄면(0) 옛 V와 같다. 설계 docs/2026-11-14-planner-goal-pressure-design.md §2.
"""

from __future__ import annotations

from collections import deque
from dataclasses import dataclass

from .assets import Cell, Objective
from .reply import engine_terms
from .rules import RuleError, attack_aims, coverage_parts, hit_damage, reachable, Hit
from .state import ActionUse, BattleState, Outcome, Structure, Unit
from .targeting import Side

WIN = 1e9


@dataclass(frozen=True)
class PlannerConfig:
    beam: int = 4                         # K: 난이도
    per_unit: int = 6                     # N: 유닛당 확장 후보
    move_only: int = 3                    # 이동만 하는 후보(도달 표로 고른 상위)
    rerank: int | None = None             # M: 빠른 V 상위 몇 개를 깊은 평가로 다시 매기나(None = beam, 0 = 끔)
    distinct: int = 0                     # 빔 다양성: 결과 열쇠마다 빔에 최대 몇 개(0 = 끔, 1 = 결과마다 하나)
    fear: float = 0.5                     # 겁
    opportunity: float = 0.5              # 기회
    step_penalty: float = 10.0            # 칠 대상이 없을 때 걸음당 벌점
    main_weight: float = 3.0              # 메인 배수
    reinforcement_discount: float = 0.5   # 증원 할인
    goal_step: float = 0.0                # G1: 승리 대상까지 남은 걸음당 승리 대상 가치의 몇 %를 빼나(0 = 끔)
    goal_together: float = 0.0            # G2: 함께 닿음 배수(0 = 끔)

    @property
    def rerank_count(self) -> int:
        return self.beam if self.rerank is None else self.rerank


def unit_power(unit: Unit, config: PlannerConfig) -> float:
    if not unit.alive:
        return 0.0
    power = unit.attack * unit.hp
    return power * config.main_weight if unit.is_main else float(power)


class Evaluator:
    """한 번의 계획(한 턴) 동안 쓰는 평가기. 도달 표는 계획 시작 판을 기준으로 만들고 (유닛, 칸)마다 기억한다."""

    def __init__(self, base: BattleState, config: PlannerConfig = PlannerConfig()):
        self.base = base
        self.config = config
        stage = base.board.stage
        self.hq_value = float(sum(e.unit.attack * e.unit.max_hp for e in stage.enemies))
        self.pool_power = {}
        if stage.reinforcement is not None:
            for defn, _ in stage.reinforcement.pool:
                self.pool_power[defn.name] = float(defn.attack * defn.max_hp)
        self._reach: dict[tuple[int, Cell], dict[Cell, int]] = {}
        self._fields: dict[tuple, dict[Cell, int]] = {}

    # ---------------------------------------------------------------- 자산
    def assets(self, state: BattleState, side: Side) -> float:
        total = sum(unit_power(u, self.config) for u in state.living(side))
        if side is Side.ENEMY:
            hq_alive = False
            for structure in state.structures.values():
                if structure.side is Side.ENEMY and structure.alive:
                    hq_alive = True
                    total += self.hq_value * structure.hp / structure.max_hp
            if hq_alive:
                total += self.config.reinforcement_discount * sum(
                    self.pool_power.get(name, 0.0) * count for name, count in state.reinforcement_pool.items())
        return total

    # ---------------------------------------------------------------- 도달 표
    def reach(self, unit: Unit, cell: Cell) -> dict[Cell, int]:
        """그 유닛이 cell에 있으면 다음 턴에 (이동해서) 공격이 닿는 칸 → 적용 비율(지정 칸 100, 스플래시 칸 스플래시 %)."""
        key = (unit.uid, cell)
        if key in self._reach:
            return self._reach[key]
        probe = self.base.clone()
        probe.turn_side = unit.side
        if unit.uid not in probe.units:     # 계획 중에 생긴 유닛(지금은 없음)
            self._reach[key] = {}
            return {}
        mover = probe.units[unit.uid]
        mover.cell, mover.hp = cell, max(1, unit.hp)
        mover.use = ActionUse.AVAILABLE
        mover.statuses = {k: v for k, v in unit.statuses.items() if k not in ("moved", "disarm")}
        result: dict[Cell, int] = {}
        spots = [cell, *reachable(probe, mover)]
        splash = unit.defn.targeting.splash_percent
        for spot in spots:
            mover.cell = spot
            for aim in attack_aims(probe, mover):
                try:
                    _, direct, extra = coverage_parts(probe, mover, aim)
                except RuleError:
                    continue
                for target in direct:
                    result[target] = 100
                for target in extra:
                    if result.get(target, 0) < splash:
                        result[target] = splash
        self._reach[key] = result
        return result

    # ---------------------------------------------------------------- 피해 예상
    @staticmethod
    def estimate(state: BattleState, attacker: Unit, target: Unit | Structure, percent: int) -> int:
        """다음 턴 공격 한 번의 예상 피해(엔진 피해 식: 상성·방어·지형). 지형 판정 출발 칸은 공격자의 지금 칸."""
        role = "primary" if percent >= 100 else "additional"
        if isinstance(target, Unit):
            hit = Hit("unit", target.uid, role, percent, attacker.cell)
        else:
            hit = Hit("structure", target.sid, role, percent, attacker.cell)
        return hit_damage(state, attacker, hit, attacker.attack)

    def _loss(self, target: Unit | Structure, damage: int) -> float:
        """그 피해로 잃는 자산(전력 점)."""
        if isinstance(target, Unit):
            lost = min(damage, target.hp) * target.attack
            return lost * self.config.main_weight if target.is_main else float(lost)
        return self.hq_value * min(damage, target.hp) / target.max_hp

    def _targets(self, state: BattleState, side: Side) -> list:
        """side가 칠 수 있는 상대: 상대 유닛 + (플레이어라면) 적 본진."""
        targets: list = list(state.living(side.other))
        if side is Side.PLAYER:
            targets += [s for s in state.structures.values() if s.side is Side.ENEMY and s.alive]
        return targets

    def _best_strike(self, state: BattleState, attacker: Unit, targets: list,
                     own: bool = False) -> tuple[object | None, int, float]:
        """그 유닛이 다음 턴에 고를 가장 좋은 공격 하나: (대상, 피해, 잃게 하는 자산).
        무장 해제는 그 유닛 편의 턴이 끝날 때 하나 준다. 우리 유닛(own)은 이번 턴 끝에 하나 줄어든 뒤를 본다."""
        if not attacker.defn.can_attack or attacker.statuses.get("disarm", 0) > (1 if own else 0):
            return None, 0, 0.0
        reach = self.reach(attacker, attacker.cell)
        best = (None, 0, 0.0)
        for target in targets:
            cells = (target.cell,) if isinstance(target, Unit) else target.cells
            percent = max((reach.get(c, 0) for c in cells), default=0)
            if percent <= 0:
                continue
            damage = self.estimate(state, attacker, target, percent)
            value = self._loss(target, damage)
            if value > best[2]:
                best = (target, damage, value)
        return best

    # ---------------------------------------------------------------- 위험·기회·거리
    def risk(self, state: BattleState, side: Side) -> float:
        """상대 유닛마다 우리 자산을 가장 많이 깎는 공격 하나를 고른다고 보고, 대상마다 피해를 더한다(Q-E2)."""
        incoming: dict[int, int] = {}
        objects: dict[int, object] = {}
        mine = self._targets(state, side.other)
        for attacker in state.living(side.other):
            target, damage, _ = self._best_strike(state, attacker, mine)
            if target is None:
                continue
            ident = id(target)
            incoming[ident] = incoming.get(ident, 0) + damage
            objects[ident] = target
        return sum(self._loss(objects[ident], damage) for ident, damage in incoming.items())

    def opportunity(self, state: BattleState, side: Side) -> tuple[float, float]:
        """(기회, 거리 벌점). 우리 유닛마다 다음 턴의 가장 좋은 공격 하나. 닿는 대상이 없으면 거리 벌점."""
        targets = self._targets(state, side)
        if not targets:
            return 0.0, 0.0
        gain, penalty = 0.0, 0.0
        field = None
        for unit in state.living(side):
            target, _, value = self._best_strike(state, unit, targets, own=True)
            if target is not None:
                gain += value
                continue
            if field is None:
                field = self._distance_field(state, targets)
            steps = field.get(unit.cell)
            if steps is None:
                continue
            beyond = max(0, steps - unit.defn.move - max(0, unit.defn.attack_range - 1))
            penalty += self.config.step_penalty * beyond
        return gain, penalty

    def _distance_field(self, state: BattleState, targets: list) -> dict[Cell, int]:
        """대상 옆 칸까지 걷는 거리(유닛은 지나간다고 봄, 벽·낭떠러지·구조물은 못 지남)."""
        sources = set()
        for target in targets:
            cells = (target.cell,) if isinstance(target, Unit) else target.cells
            for x, y in cells:
                sources.update({(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)})
        key = tuple(sorted(sources)) + (len(state.cliffs),)
        if key in self._fields:
            return self._fields[key]
        board = state.board
        structures = {c for s in state.structures.values() if s.alive for c in s.cells}
        dist: dict[Cell, int] = {}
        queue: deque[Cell] = deque()
        for cell in sources:
            if board.inside(cell) and not state.blocks_entry(cell) and cell not in structures:
                dist[cell] = 0
                queue.append(cell)
        while queue:
            cell = queue.popleft()
            for dx, dy in ((0, 1), (1, 0), (0, -1), (-1, 0)):
                nxt = (cell[0] + dx, cell[1] + dy)
                if nxt in dist or not board.inside(nxt) or state.blocks_entry(nxt) or nxt in structures:
                    continue
                dist[nxt] = dist[cell] + 1
                queue.append(nxt)
        self._fields[key] = dist
        return dist

    # ---------------------------------------------------------------- 승리 대상 노림(설계 goal-pressure §2)
    @staticmethod
    def win_targets(state: BattleState, side: Side) -> list:
        """side가 잡으면 이기는 상대의 것(산 것만, §2.1). 적 → 플레이어 메인.
        플레이어 → 본진 목표면 적 본진, 격파 목표면 격파 대상(rules._check_outcome과 같은 조건)."""
        if side is Side.ENEMY:
            main = state.main_character()
            return [main] if main is not None and main.alive and main.side is Side.PLAYER else []
        if state.board.stage.objective is Objective.HEADQUARTERS:
            return [s for s in state.structures.values() if s.side is Side.ENEMY and s.alive]
        return [u for u in state.living(Side.ENEMY) if u.victory_target]

    def win_value(self, target: Unit | Structure) -> float:
        """승리 대상 가치. 최대 HP 기준으로 고정한다(P1): 다쳐도 G1의 끌림이 줄지 않게."""
        if isinstance(target, Unit):
            value = float(target.attack * target.max_hp)
            return value * self.config.main_weight if target.is_main else value
        return self.hq_value

    def goal_steps(self, state: BattleState, unit: Unit, cell: Cell, targets: list) -> int:
        """그 유닛이 cell에 있을 때 승리 대상까지 모자란 걸음(G1, §2.2). 거리 벌점과 같은 식·같은 거리 표.
        도달 표로 다음 턴에 승리 대상을 칠 수 있으면 0. 공격할 수 없는 종류(P2)·걸어갈 수 없는 칸(P3)도 0."""
        if not targets or not unit.defn.can_attack:
            return 0
        reach = self.reach(unit, cell)
        for target in targets:
            cells = (target.cell,) if isinstance(target, Unit) else target.cells
            if any(reach.get(c, 0) > 0 for c in cells):
                return 0
        steps = self._distance_field(state, targets).get(cell)
        if steps is None:
            return 0
        return max(0, steps - unit.defn.move - max(0, unit.defn.attack_range - 1))

    def goal_penalty(self, state: BattleState, unit: Unit, cell: Cell, targets: list) -> float:
        """G1의 유닛 하나 몫(빼는 점수, 0 이상). 판 평가(goal_terms)와 이동만 후보 점수가 함께 부른다(§3)."""
        if self.config.goal_step <= 0 or not targets:
            return 0.0
        steps = self.goal_steps(state, unit, cell, targets)
        if steps == 0:
            return 0.0
        reference = max(self.win_value(target) for target in targets)
        return self.config.goal_step * reference / 100 * steps

    def goal_together_value(self, state: BattleState, side: Side, targets: list) -> float:
        """G2(§2.3): 대상마다 t × 가치 × r², r = min(1, 다음 턴에 닿는 우리 유닛 예상 피해 합 / 대상 HP). 가장 큰 것.
        닿음·피해는 기회 항목과 같은 도달 표·피해 예상, 무장 해제 조건도 _best_strike와 같다(P2)."""
        if self.config.goal_together <= 0 or not targets:
            return 0.0
        attackers = [u for u in state.living(side)
                     if u.defn.can_attack and u.statuses.get("disarm", 0) <= 1]
        best = 0.0
        for target in targets:
            cells = (target.cell,) if isinstance(target, Unit) else target.cells
            damage = 0
            for unit in attackers:
                reach = self.reach(unit, unit.cell)
                percent = max((reach.get(c, 0) for c in cells), default=0)
                if percent > 0:
                    damage += self.estimate(state, unit, target, percent)
            if damage <= 0 or target.hp <= 0:
                continue
            ratio = min(1.0, damage / target.hp)
            best = max(best, self.config.goal_together * self.win_value(target) * ratio * ratio)
        return best

    def goal_terms(self, state: BattleState, side: Side) -> tuple[float, float]:
        """(G1, G2). G1은 0 이하(빼는 점수), G2는 0 이상. 둘 다 끄면 (0, 0)."""
        config = self.config
        if config.goal_step <= 0 and config.goal_together <= 0:
            return 0.0, 0.0
        targets = self.win_targets(state, side)
        if not targets:
            return 0.0, 0.0
        g1 = 0.0 - sum(self.goal_penalty(state, unit, unit.cell, targets) for unit in state.living(side))
        return g1, self.goal_together_value(state, side, targets)

    # ---------------------------------------------------------------- 합
    def breakdown(self, state: BattleState, side: Side) -> dict:
        win = Outcome.PLAYER_WIN if side is Side.PLAYER else Outcome.ENEMY_WIN
        if state.outcome is not None:
            value = WIN if state.outcome is win else -WIN
            return {"outcome": state.outcome.value, "total": value}
        mine, theirs = self.assets(state, side), self.assets(state, side.other)
        risk = self.risk(state, side)
        gain, penalty = self.opportunity(state, side)
        config = self.config
        total = mine - theirs - config.fear * risk + config.opportunity * gain - penalty
        goal_step, goal_together = self.goal_terms(state, side)
        if config.goal_step > 0 or config.goal_together > 0:     # 끄면 옛 합을 그대로(P6)
            total += goal_step + goal_together
        return {"assets": mine, "opponentAssets": theirs, "risk": risk, "opportunity": gain,
                "stepPenalty": penalty, "goalStep": goal_step, "goalTogether": goal_together, "total": total}

    def value(self, state: BattleState, side: Side) -> float:
        return self.breakdown(state, side)["total"]

    def deep_breakdown(self, state: BattleState, side: Side, seed: int) -> dict:
        """V_deep: breakdown과 같은 식, 위험·기회·거리 벌점은 엔진 응수. state는 side 차례여야 한다(끝난 판은 ±WIN)."""
        win = Outcome.PLAYER_WIN if side is Side.PLAYER else Outcome.ENEMY_WIN
        if state.outcome is not None:
            value = WIN if state.outcome is win else -WIN
            return {"outcome": state.outcome.value, "total": value}
        mine, theirs = self.assets(state, side), self.assets(state, side.other)
        terms = engine_terms(state, side, self, seed)
        config = self.config
        total = mine - theirs - config.fear * terms.risk + config.opportunity * terms.opportunity - terms.step_penalty
        goal_step, goal_together = self.goal_terms(state, side)     # 빠른 V와 같은 판·같은 도달 표(Q-G4)
        if config.goal_step > 0 or config.goal_together > 0:
            total += goal_step + goal_together
        return {"assets": mine, "opponentAssets": theirs, "risk": terms.risk, "opportunity": terms.opportunity,
                "stepPenalty": terms.step_penalty, "goalStep": goal_step, "goalTogether": goal_together,
                "total": total}

    def deep_value(self, state: BattleState, side: Side, seed: int) -> float:
        return self.deep_breakdown(state, side, seed)["total"]
