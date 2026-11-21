"""턴 계획 AI의 판 평가 V(판, 진영). 설계 docs/2026-11-13-turn-planner-design.md §2, 시뮬레이터 설계 §B6(Q-E3).

V = 자산(우리) − 자산(상대) − 겁 × 위험(우리) + 기회 × 기회(우리) − 거리 벌점

- 유닛 전력 = 공격력 × 남은 HP (메인은 × main_weight)
- 적 자산에는 본진(본진 값 × 남은 HP 비율, 본진 값 = 전투 시작 때 적 유닛 전력 합)과 남은 증원(× 할인, 본진이 살아 있을 때)
- 위험·기회는 "유닛이 이 칸에 있으면 다음 턴에 어느 칸을 칠 수 있나"(도달 표)로 빠르게 계산한다.
  특성(처치 연쇄·넉백 등)은 위험·기회 예상에 넣지 않는다. 실제로 둔 행동의 결과에는 엔진이 다 넣는다.
- 깊은 판 평가 V_deep(deep_breakdown): 같은 식에서 위험·기회·거리 벌점만 엔진 응수(reply.py)로 바꾼다.
  계획 AI가 빔의 마지막 후보 몇 개를 다시 매길 때 쓴다. 설계 docs/2026-11-13-planner-engine-reply-design.md R2.
- 판 값 고치기 VF1·VF2(설계 docs/2026-11-18-enemy-playstyle-knobs-design.md §12.8·§13): 지금 치면 손해로 계산되던 두 항목.
  VF1(step_anchor): 거리 벌점의 대상을 계획 시작 때 살아 있던 상대로 고정 — 잡아도 벌점이 늘지 않는다.
  VF2(pooled_chance): 기회를 위험처럼 대상마다 피해를 모아 남은 HP까지만 — 같은 대상을 여러 유닛 몫으로 겹쳐 세지 않는다.
  둘 다 끄면(0) 옛 V와 같다.
- 승리 대상 노림 G1·G2(goal_terms): 상대가 잃으면 지는 것(적 → 플레이어 메인, 플레이어 → 적 본진·격파 대상)을
  멀리서는 걸어 다가가고(G1) 가까이서는 함께 닿게(G2) 한다. 빠른 V·깊은 V 모두 같은 판·같은 도달 표로 더한다.
  손잡이를 끄면(0) 옛 V와 같다. 설계 docs/2026-11-14-planner-goal-pressure-design.md §2.
"""

from __future__ import annotations

import math

from collections import deque
from dataclasses import dataclass, replace

from .assets import Cell, Objective
from .reply import engine_terms, lookahead_board
from .rules import RuleError, attack_aims, coverage_parts, hit_damage, reachable, Hit
from .state import ActionUse, BattleState, Outcome, Structure, Unit
from .targeting import Side

WIN = 1e9
HURT_RATIO = 0.5        # PK0b: 계획을 시작할 때 HP가 최대의 이 비율 이하면 "다친 유닛"


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
    move_attack_cells: int = 0            # 속도 개선 SP1: 이동 + 공격을 만들 도착 칸을 빠른 점수 상위 몇 개로(0 = 거르지 않음)
    reply_move_attack_cells: int = 0      # 속도 개선 SP2: 2단 평가 응수의 이동 + 공격 도착 칸 수(0 = 거르지 않음)
    lookahead: int = 0                    # 한 턴 더 보기: 1이면 깊은 평가가 상대 턴 전체를 차례대로 둬 보고 그 뒤 판을 빠른 V로(0 = 끔)
    survival: float = 0.0                 # 곡선 항목 CV1: 산 유닛 전력에 더하는 "살아 있음" 덩어리(공격력 × 최대HP의 배수, 0 = 끔)
    approach: int = 0                     # 곡선 항목 CV2: 걸음 벌점 포화 k(벌점 = 상수 × k × b / (b + k), 0 = 직선)
    ratio: float = 0.0                    # 곡선 항목 CV3: 자산을 비율(로그)로 보는 몫 w(0 = 차이 그대로, 1 = 로그 비율만)
    log_approach: int = 0                 # 곡선 항목 CV4: 걸음 벌점 로그 k(벌점 = 상수 × k × ln(1 + b / k), 0 = 직선)
    preserve: float = 0.0                 # 성격 손잡이 PK0 자기 보존: 계획하는 편 유닛에만 붙는 "살아 있음" 덩어리(0 = 끔)
    preserve_hurt: float = 0.0            # 성격 손잡이 PK0b 다친 유닛 살리기: 계획을 시작할 때 다친(HP ≤ 최대의 절반) 우리 유닛에만
                                          # 더 붙는 "살아 있음" 덩어리(0 = 끔). 안 다친 유닛은 그대로 싸운다
    must_act: float = 0.0                 # 행동 규칙 PK6: 행동 안 한 유닛마다 계획 값에서 빼는 양(유닛 전력의 배수, 0 = 끔).
                                          # 유닛은 행동의 손해가 이 값보다 클 때만 제자리에 있는다
    step_anchor: int = 0                  # 판 값 고치기 VF1: 1이면 거리 벌점의 대상을 계획 시작 때 살아 있던 상대로 고정한다
                                          # (이번 계획에서 잡은 대상의 칸도 "닿은 곳"으로 남아 잡아도 벌점이 늘지 않는다, 0 = 끔)
    pooled_chance: int = 0                # 판 값 고치기 VF2: 1이면 다음 턴 기회를 대상마다 피해를 모아 남은 HP까지만 센다
                                          # (위험과 같은 방식. 같은 대상을 노리는 유닛 수만큼 겹쳐 세지 않는다, 0 = 끔)

    @property
    def rerank_count(self) -> int:
        return self.beam if self.rerank is None else self.rerank

    def step_units(self, steps: int) -> float:
        """걸음 벌점·G1·빠른 이동 점수가 곱하는 걸음 양. 끄면 걸음 수 그대로, CV2는 k × b / (b + k), CV4는 k × ln(1 + b / k).
        설계 docs/2026-11-18-planner-curved-terms-design.md CV2."""
        if steps <= 0:
            return float(steps)
        if self.log_approach > 0:     # CV4: 가까우면 직선과 비슷하고, 멀어져도 계속 커진다(상한 없음)
            return self.log_approach * math.log1p(steps / self.log_approach)
        if self.approach <= 0:
            return float(steps)
        return self.approach * steps / (steps + self.approach)


def unit_power(unit: Unit, config: PlannerConfig) -> float:
    """유닛 전력 = 공격력 × (HP + 살아 있음 × 최대HP), 메인은 × 메인 배수. 죽으면 0(CV1)."""
    if not unit.alive:
        return 0.0
    power = unit.attack * (unit.hp + config.survival * unit.max_hp)
    return power * config.main_weight if unit.is_main else float(power)


class Evaluator:
    """한 번의 계획(한 턴) 동안 쓰는 평가기. 도달 표는 계획 시작 판을 기준으로 만들고 (유닛, 칸)마다 기억한다."""

    def __init__(self, base: BattleState, config: PlannerConfig = PlannerConfig(), side: Side | None = None):
        """side = 계획하는 편. 자기 보존(PK0·PK0b)은 이 편 유닛에만 걸리므로, 켜려면 side가 있어야 한다."""
        if (config.preserve > 0 or config.preserve_hurt > 0) and side is None:
            raise ValueError("자기 보존(preserve·preserve_hurt)을 켜려면 평가기에 계획하는 편(side)을 줘야 합니다")
        self.base = base
        self.config = config
        self.side = side
        # PK0b: 다쳤는지는 계획 시작 판으로 정해 계획 한 번 동안 고정한다. 판마다 다시 보면 "응수에 맞아 다치면
        # 살아 있음 덩어리가 새로 붙어 자산이 느는" 거꾸로 된 값이 생긴다.
        self._hurt = frozenset(
            u.uid for u in (base.living(side) if side is not None and config.preserve_hurt > 0 else [])
            if u.hp <= HURT_RATIO * u.max_hp)
        stage = base.board.stage
        self.hq_value = float(sum(e.unit.attack * e.unit.max_hp for e in stage.enemies))
        self.pool_power = {}
        if stage.reinforcement is not None:
            for defn, _ in stage.reinforcement.pool:
                # 남은 증원은 다친 적 없는 유닛과 같은 전력(살아 있음 덩어리 포함, CV1).
                # 증원은 적 편이다. 적이 계획하는 편이면 자기 보존 덩어리도 같이 든다(PK0).
                keep = config.preserve if side is Side.ENEMY else 0.0
                self.pool_power[defn.name] = float(defn.attack * defn.max_hp * (1 + config.survival + keep))
        # CV3 자산 비율의 기준 크기 R: 계획 시작 판의 두 진영 자산 평균(계획 한 번 동안 고정).
        self.asset_scale = (self.assets(base, Side.PLAYER) + self.assets(base, Side.ENEMY)) / 2
        self._reach: dict[tuple[int, Cell], dict[Cell, int]] = {}
        self._strike: dict[tuple[int, Cell], dict[Cell, int]] = {}
        self._fields: dict[tuple, dict[Cell, int]] = {}

    # ---------------------------------------------------------------- 자산
    def keep_value(self, unit: Unit) -> float:
        """자기 보존(PK0) 덩어리: 계획하는 편의 유닛이 살아 있는 값(공격력 × 최대HP × 값, 메인은 × 메인 배수).
        상대 편 유닛은 0이다 — 상대를 잡는 이득은 커지지 않는다(설계 docs/2026-11-18-enemy-playstyle-knobs-design.md PK0).
        계획을 시작할 때 다친 유닛에는 preserve_hurt만큼 더 붙는다(PK0b, §13)."""
        if unit.side is not self.side:
            return 0.0
        rate = self.config.preserve + (self.config.preserve_hurt if unit.uid in self._hurt else 0.0)
        if rate <= 0:
            return 0.0
        value = rate * unit.attack * unit.max_hp
        return value * self.config.main_weight if unit.is_main else value

    def assets(self, state: BattleState, side: Side) -> float:
        total = sum(unit_power(u, self.config) + self.keep_value(u) for u in state.living(side))
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
        if key not in self._reach:
            self._reach[key] = self._coverage(unit, cell, moving=True)
        return self._reach[key]

    def strike(self, unit: Unit, cell: Cell) -> dict[Cell, int]:
        """그 유닛이 cell에 서서 이번에 공격이 닿는 칸 → 적용 비율(이동은 더하지 않는다). 속도 개선 설계 SP1."""
        key = (unit.uid, cell)
        if key not in self._strike:
            self._strike[key] = self._coverage(unit, cell, moving=False)
        return self._strike[key]

    def _coverage(self, unit: Unit, cell: Cell, moving: bool) -> dict[Cell, int]:
        """도달 표·이번 공격 칸의 공통 계산. 계획 시작 판에 유닛을 cell로 옮겨, (moving이면 이동으로 닿는 칸까지) 공격 칸을 모은다."""
        probe = self.base.clone()
        probe.turn_side = unit.side
        if unit.uid not in probe.units:     # 계획 중에 생긴 유닛(지금은 없음)
            return {}
        mover = probe.units[unit.uid]
        mover.cell, mover.hp = cell, max(1, unit.hp)
        mover.use = ActionUse.AVAILABLE
        mover.statuses = {k: v for k, v in unit.statuses.items() if k not in ("moved", "disarm")}
        result: dict[Cell, int] = {}
        spots = [cell, *reachable(probe, mover)] if moving else [cell]
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
        """그 피해로 잃는 자산(전력 점). 자산(unit_power)과 같은 규칙: 죽이면 살아 있음 덩어리까지(CV1).
        위험·기회·도착 칸 점수·응수 손실이 모두 이 함수 하나를 쓴다."""
        if isinstance(target, Unit):
            lost = float(min(damage, target.hp) * target.attack)
            if damage >= target.hp and self.config.survival > 0:
                lost += self.config.survival * target.attack * target.max_hp
            lost = lost * self.config.main_weight if target.is_main else lost
            if damage >= target.hp:
                lost += self.keep_value(target)     # 계획하는 편 유닛이 죽을 때만(PK0)
            return lost
        return self.hq_value * min(damage, target.hp) / target.max_hp

    def _targets(self, state: BattleState, side: Side) -> list:
        """side가 칠 수 있는 상대: 상대 유닛 + (플레이어라면) 적 본진."""
        targets: list = list(state.living(side.other))
        if side is Side.PLAYER:
            targets += [s for s in state.structures.values() if s.side is Side.ENEMY and s.alive]
        return targets

    def step_targets(self, state: BattleState, side: Side) -> list:
        """거리 벌점을 재는 대상(빠른 V·응수 거리 벌점·이동만 후보 점수가 함께 부른다).
        끄면 지금 살아 있는 상대(_targets)다. VF1을 켜면 계획 시작 판에서 살아 있었지만 이 판에서는 죽은 상대를
        **계획 시작 판의 칸에** 더한다: 가까운 상대를 잡았다고 남은 유닛들의 거리 벌점이 한꺼번에 늘지 않게 한다.
        다음 턴 계획은 그때 살아 있는 상대로 다시 시작하므로 남은 상대 쪽으로의 끌림은 한 턴 뒤에 돌아온다.
        죽은 순간의 칸이 아니라 시작 판의 칸을 쓰는 까닭: 게임 Core는 죽은 캐릭터의 위치 값을 지운다
        (CombatCommandCalculator.Copy). Unity 계획 AI와 같은 정의로 맞춘다(이식 설계 Q-VP2)."""
        targets = self._targets(state, side)
        if self.config.step_anchor <= 0:
            return targets
        fallen: list = []
        for unit in self.base.living(side.other):
            now = state.units.get(unit.uid)
            if now is None or not now.alive:
                fallen.append(unit)          # 시작 판의 유닛(시작 판의 칸)
        if side is Side.PLAYER:
            for structure in self.base.structures.values():
                if structure.side is Side.ENEMY and structure.alive:
                    now = state.structures.get(structure.sid)
                    if now is not None and not now.alive:
                        fallen.append(now)
        return targets + fallen

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

    # ---------------------------------------------------------------- 도착 칸 점수(속도 개선 설계 SP1·SP2)
    def strike_value(self, state: BattleState, unit: Unit, cell: Cell, targets: list) -> float:
        """그 유닛이 cell에 서서 이번에 칠 수 있는 가장 좋은 공격 하나로 잃게 하는 자산. 공격할 수 없으면 0."""
        if not unit.defn.can_attack or "disarm" in unit.statuses:
            return 0.0
        moved = replace(unit, cell=cell)
        cover = self.strike(unit, cell)
        best = 0.0
        for target in targets:
            cells = (target.cell,) if isinstance(target, Unit) else target.cells
            percent = max((cover.get(c, 0) for c in cells), default=0)
            if percent > 0:
                best = max(best, self._loss(target, self.estimate(state, moved, target, percent)))
        return best

    def danger_loss(self, state: BattleState, unit: Unit, cell: Cell) -> float:
        """그 유닛이 cell에 있을 때 상대 유닛들이 지금 자리에서 그 칸에 줄 수 있는 피해를 더해 잃는 자산(빠른 이동 점수와 같은 식)."""
        moved = replace(unit, cell=cell)
        danger = 0
        for attacker in state.living(unit.side.other):
            percent = self.reach(attacker, attacker.cell).get(cell, 0)
            if percent and attacker.defn.can_attack and "disarm" not in attacker.statuses:
                danger += self.estimate(state, attacker, moved, percent)
        return self._loss(moved, danger)

    def move_attack_score(self, state: BattleState, unit: Unit, cell: Cell) -> float:
        """SP1 도착 칸 점수: 기회 × 이번 공격의 손실 − 겁 × 그 칸에서 맞을 피해 − 그 칸의 G1 몫(승리 대상 걸음을 켰을 때)."""
        config = self.config
        score = (config.opportunity * self.strike_value(state, unit, cell, self._targets(state, unit.side))
                 - config.fear * self.danger_loss(state, unit, cell))
        if config.goal_step > 0:
            score -= self.goal_penalty(state, replace(unit, cell=cell), cell, self.win_targets(state, unit.side))
        return score

    def reply_score(self, state: BattleState, unit: Unit, cell: Cell) -> float:
        """SP2 응수 도착 칸 점수: 이번 공격의 손실(응수는 순이득이 가장 큰 공격 하나만 고르므로 겁 항목이 없다)."""
        return self.strike_value(state, unit, cell, self._targets(state, unit.side))

    @staticmethod
    def best_cells(cells: list[Cell], score, limit: int) -> set[Cell] | None:
        """점수 상위 limit개 칸(같으면 먼저 나온 칸). limit이 0이거나 칸이 limit개 이하이면 None(거르지 않음)."""
        if limit <= 0 or len(cells) <= limit:
            return None
        return set(sorted(cells, key=lambda c: -score(c))[:limit])

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
        """(기회, 거리 벌점). 우리 유닛마다 다음 턴의 가장 좋은 공격 하나. 닿는 대상이 없으면 거리 벌점.
        VF2(pooled_chance)를 켜면 위험(risk)처럼 대상마다 피해를 모아 그 대상의 남은 HP까지만 센다:
        끈 식은 같은 대상을 노리는 유닛 수만큼 남은 HP를 겹쳐 세어, 지금 쳐서 HP를 깎으면 얻는 것(한 번)보다
        기회(여러 번)가 더 줄어 "지금 치면 손해"가 됐다."""
        targets = self._targets(state, side)
        if not targets:
            return 0.0, 0.0
        gain, penalty = 0.0, 0.0
        pooled = self.config.pooled_chance > 0
        incoming: dict[int, int] = {}
        objects: dict[int, object] = {}
        field = None
        for unit in state.living(side):
            target, damage, value = self._best_strike(state, unit, targets, own=True)
            if target is not None:
                if pooled:
                    ident = id(target)
                    incoming[ident] = incoming.get(ident, 0) + damage
                    objects[ident] = target
                else:
                    gain += value
                continue
            if field is None:
                field = self._distance_field(state, self.step_targets(state, side))
            steps = field.get(unit.cell)
            if steps is None:
                continue
            beyond = max(0, steps - unit.defn.move - max(0, unit.defn.attack_range - 1))
            penalty += self.config.step_penalty * self.config.step_units(beyond)
        if pooled:
            gain = sum(self._loss(objects[ident], damage) for ident, damage in incoming.items())
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
        return self.config.goal_step * reference / 100 * self.config.step_units(steps)

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
    def asset_terms(self, mine: float, theirs: float) -> tuple[float, float, float]:
        """(자산 항, 위험 배수, 기회 배수). CV3(설계 docs/2026-11-18-planner-curved-terms-design.md §6).
        끄면 (우리 − 상대, 1, 1). 켜면 자산 항 = (1 − w)(우리 − 상대) + w × R × ln(우리 / 상대).
        위험·기회는 자산을 깎는 양이라 로그의 기울기(R / 우리, R / 상대)를 곱해 같은 단위로 맞춘다."""
        w = self.config.ratio
        if w <= 0:
            return mine - theirs, 1.0, 1.0
        scale = max(self.asset_scale, 1.0)
        m, t = mine + 1.0, theirs + 1.0     # 0에서 로그가 터지지 않게(전멸은 승패로 먼저 끝남)
        term = (1 - w) * (mine - theirs) + w * scale * math.log(m / t)
        return term, (1 - w) + w * scale / m, (1 - w) + w * scale / t

    def breakdown(self, state: BattleState, side: Side) -> dict:
        win = Outcome.PLAYER_WIN if side is Side.PLAYER else Outcome.ENEMY_WIN
        if state.outcome is not None:
            value = WIN if state.outcome is win else -WIN
            return {"outcome": state.outcome.value, "total": value}
        mine, theirs = self.assets(state, side), self.assets(state, side.other)
        risk = self.risk(state, side)
        gain, penalty = self.opportunity(state, side)
        config = self.config
        asset, risk_w, gain_w = self.asset_terms(mine, theirs)
        total = asset - config.fear * risk_w * risk + config.opportunity * gain_w * gain - penalty
        goal_step, goal_together = self.goal_terms(state, side)
        if config.goal_step > 0 or config.goal_together > 0:     # 끄면 옛 합을 그대로(P6)
            total += goal_step + goal_together
        return {"assets": mine, "opponentAssets": theirs, "risk": risk, "opportunity": gain,
                "stepPenalty": penalty, "goalStep": goal_step, "goalTogether": goal_together, "total": total}

    def value(self, state: BattleState, side: Side) -> float:
        return self.breakdown(state, side)["total"]

    def deep_breakdown(self, state: BattleState, side: Side, seed: int) -> dict:
        """V_deep: breakdown과 같은 식, 위험·기회·거리 벌점은 엔진 응수. state는 side 차례여야 한다(끝난 판은 ±WIN).
        한 턴 더 보기(config.lookahead == 1, 설계 docs/2026-11-18-planner-two-turn-lookahead-design.md LA1)면 상대 턴을 차례대로
        둬 본 뒤 판(다시 side 차례)의 빠른 V다."""
        win = Outcome.PLAYER_WIN if side is Side.PLAYER else Outcome.ENEMY_WIN
        if state.outcome is not None:
            value = WIN if state.outcome is win else -WIN
            return {"outcome": state.outcome.value, "total": value}
        if self.config.lookahead == 1:
            board, replies = lookahead_board(state, side, self, seed)
            if board.outcome is not None:
                value = WIN if board.outcome is win else -WIN
                return {"outcome": board.outcome.value, "total": value, "lookahead": 1, "replies": len(replies)}
            result = dict(self.breakdown(board, side))
            result.update(lookahead=1, replies=len(replies))
            return result
        mine, theirs = self.assets(state, side), self.assets(state, side.other)
        terms = engine_terms(state, side, self, seed)
        config = self.config
        asset, risk_w, gain_w = self.asset_terms(mine, theirs)
        total = (asset - config.fear * risk_w * terms.risk + config.opportunity * gain_w * terms.opportunity
                 - terms.step_penalty)
        goal_step, goal_together = self.goal_terms(state, side)     # 빠른 V와 같은 판·같은 도달 표(Q-G4)
        if config.goal_step > 0 or config.goal_together > 0:
            total += goal_step + goal_together
        return {"assets": mine, "opponentAssets": theirs, "risk": terms.risk, "opportunity": terms.opportunity,
                "stepPenalty": terms.step_penalty, "goalStep": goal_step, "goalTogether": goal_together,
                "total": total}

    def deep_value(self, state: BattleState, side: Side, seed: int) -> float:
        return self.deep_breakdown(state, side, seed)["total"]
