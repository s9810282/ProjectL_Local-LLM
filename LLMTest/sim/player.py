"""플레이어 역할(상대 모형)과 배치 규칙. 설계 §A4.

- 돌진형(rush): 칠 수 있으면 처치·본진·큰 피해 순으로 치고, 못 치면 본진 쪽으로 간다.
- 신중형(cautious): 적이 다음 턴에 닿을 수 있는 칸에는 서지 않는다(이기는 수는 예외). 그 밖은 돌진형과 같다.
- 둘 다 메인은 맨 앞 아군보다 한 줄 뒤를 지킨다(메인이 죽으면 바로 진다).
- 영리한 플레이어(smart): 게임 적 AI와 같은 절차·점수식(sim/game_ai.py)을 플레이어 쪽에서 쓴다. 메인 보호만 더한다.
"""

from __future__ import annotations

from .analysis import Option, change, distance_field, neighbors_of, reach_damage, threat_cells
from .assets import Cell, PlayerLoadout, StageDef
from .game_ai import GameAIPolicy, Objective, loss_permille
from .policy import OptionPolicy
from .render import usable_deployment_cells
from .state import BattleState, Unit
from .targeting import Side


def deployment(stage: StageDef, loadout: PlayerLoadout) -> dict[int, Cell]:
    """배치 칸 중 적 본진 쪽(위쪽) 줄부터 가운데로 서브를 놓고, 메인은 그 뒤 줄 가운데에 놓는다."""
    center = (stage.width - 1) / 2
    cells = sorted(usable_deployment_cells(stage), key=lambda c: (-c[1], abs(c[0] - center), c[0]))
    subs = len(loadout.subs)
    if len(cells) < subs + 1:
        raise ValueError(f"{stage.name}: 배치 칸이 모자랍니다")
    placed = {index + 1: cells[index] for index in range(subs)}
    front = max(cell[1] for cell in placed.values()) if placed else cells[0][1]
    behind = [c for c in cells[subs:] if c[1] < front] or cells[subs:]
    placed[0] = behind[0]
    return placed


class PlayerPolicy(OptionPolicy):
    def __init__(self, style: str = "rush"):
        super().__init__()
        if style not in ("rush", "cautious"):
            raise ValueError(f"모르는 플레이어 역할: {style}")
        self.style = style
        self.name = f"player-{style}"
        self._threat_key = None
        self._threat: set[Cell] = set()
        self._field_for = None
        self._field: dict[Cell, int] = {}

    # ---- 공통 계산 ----
    def _hq_distance(self, state: BattleState) -> dict[Cell, int]:
        if self._field_for is state:
            return self._field
        targets = [c for s in state.structures.values() if s.side is Side.ENEMY and s.alive for c in s.cells]
        if not targets:   # 보스 목표 등: 격파 대상 또는 적 유닛 쪽
            targets = [u.cell for u in state.living(Side.ENEMY) if u.victory_target] or \
                      [u.cell for u in state.living(Side.ENEMY)]
        return distance_field(state, neighbors_of(targets))

    def _enemy_threat(self, state: BattleState) -> set[Cell]:
        key = (state.round, tuple(sorted((u.uid, u.cell) for u in state.living(Side.ENEMY))))
        if key != self._threat_key:
            self._threat_key, self._threat = key, threat_cells(state, Side.ENEMY)
        return self._threat

    def _front_line(self, state: BattleState, exclude: int) -> int | None:
        others = [u.cell[1] for u in state.living(Side.PLAYER) if u.uid != exclude and not u.is_main]
        return max(others) if others else None

    # ---- 정책 ----
    def allowed(self, state: BattleState, unit: Unit, option: Option, after: BattleState) -> bool:
        if option.move is None:
            return True
        destination = option.move.path[-1]
        if unit.is_main:
            front = self._front_line(state, unit.uid)
            if front is not None and destination[1] > front - 1 and destination[1] > unit.cell[1]:
                return False     # 메인은 맨 앞 아군보다 한 줄 뒤까지만 나아간다
        if self.style == "cautious" and option.attack is None and destination in self._enemy_threat(state):
            return change(state, after, Side.PLAYER).win   # 신중형: 공격 없이 적이 닿는 칸에 들어가지 않는다
        return True

    MAIN_DANGER = 150      # 메인이 적이 닿는 칸에 서는 벌점, 벗어나는 보상(메인이 죽으면 바로 진다)
    CAUTIOUS_DANGER = 25   # 신중형 서브가 공격하면서 적이 닿는 칸에 서는 벌점

    def score(self, before: BattleState, after: BattleState, unit: Unit, option: Option) -> float:
        c = change(before, after, Side.PLAYER)
        if c.win:
            return 1_000_000
        if c.loss:
            return -1_000_000
        value = 3 * c.hq_damage + 60 * c.opp_kills + c.opp_hp_lost - 2 * c.own_hp_lost - 80 * c.own_kills
        start = unit.cell
        end = option.move.path[-1] if option.move is not None else start
        threat = self._enemy_threat(before)
        if unit.is_main:
            value += self.MAIN_DANGER * ((start in threat) - (end in threat))
        elif self.style == "cautious":
            value += self.CAUTIOUS_DANGER * ((start in threat) - (end in threat))
        if option.move is not None:
            field = self._hq_distance(before)
            far = max(field.values(), default=0) + 1
            value += 2 * (field.get(start, far) - field.get(end, far))
        return value


class SmartPlayerPolicy(GameAIPolicy):
    """영리한 플레이어: 게임 적 AI의 절차·점수식을 플레이어 쪽에서 그대로 쓴다(대상 = 적 유닛, 없으면 적 본진).

    게임 AI 점수식은 지켜야 할 메인도, 부숴야 할 본진도 없는 적 쪽 식이다(위험은 "지금 자리에서 치는 가장 센 한 방"만,
    이동은 가장 가까운 상대 유닛 쪽). 그대로 쓰면 서브가 하나씩 몰아치기를 당하고, 본진은 치지 않아 증원에 소모전으로 진다.
    그래서 더한 것(게임 AI에는 없음, 2026-11-13 시험으로 정함):
    (1) 승리 조건 인식: 이동은 적 본진을 향하고(교전 비용·다음 턴), 본진 손실은 3배, 이기는 행동은 +100만. 공격은 그대로.
    (2) 몰아치기 검사: 모든 유닛이 서는 칸은 적이 다음 턴에 이동해서 칠 수 있는 피해 합이 HP 이상이면 치명으로 보고
        게임과 같은 치명 거르기를 한다. 모두 치명이면 맞을 피해가 가장 작은 칸을 고른다.
    (3) 메인 보호: 메인은 죽지 않는 칸이라도 맞을 수 있는 피해 천분율만큼 벌점(위험 회피 100%).
    """

    # 플레이어는 적 본진을 부수면 이긴다(증원도 멈춘다). 게임 AI에는 없는 승리 조건 인식이다.
    OBJECTIVE = Objective(structure_weight=3, win_bonus=1_000_000, structures_with_units=True,
                          move_to_structures=True)

    def __init__(self, profile: str = "asset"):
        super().__init__(profile, Side.PLAYER)
        self.name = f"player-smart-{profile}"
        self.objective = self.OBJECTIVE
        self._reach_key = None
        self._reach: dict[Cell, int] = {}

    def _enemy_reach(self, state: BattleState) -> dict[Cell, int]:
        key = (state.round, tuple(sorted((u.uid, u.cell, "disarm" in u.statuses) for u in state.living(Side.ENEMY))),
               tuple(sorted(u.cell for u in state.living(Side.PLAYER))))
        if key != self._reach_key:
            self._reach_key, self._reach = key, reach_damage(state, Side.ENEMY)
        return self._reach

    def extra_lethal(self, state: BattleState, actor: Unit, endpoint: Cell, hp: int) -> int | None:
        """모든 유닛: 적이 다음 턴에 이동해서 칠 수 있는 피해 합이 HP 이상이면 치명(작을수록 덜 치명)."""
        damage = self._enemy_reach(state).get(endpoint, 0)
        return damage if damage >= hp else None

    def extra_penalty(self, state: BattleState, actor: Unit, endpoint: Cell, hp: int) -> int:
        """메인: 죽지 않는 칸이라도 맞을 수 있는 피해 천분율만큼 벌점(위험 회피 100%). 메인이 죽으면 바로 진다."""
        if not actor.is_main:
            return 0
        return loss_permille(self._enemy_reach(state).get(endpoint, 0), hp, actor.max_hp)


def make_player_policy(name: str):
    """이름으로 플레이어 역할을 만든다: rush, cautious, smart, plan{K}[:f겁][:o기회][:r다시 매김][:d빔 다양성](턴 계획 AI, 예: plan4, plan4:r0, plan4:d1)."""
    if name in ("rush", "cautious"):
        return PlayerPolicy(name)
    if name == "smart":
        return SmartPlayerPolicy()
    if name.startswith("plan"):
        from .planner import planner_from_name
        return planner_from_name(Side.PLAYER, name)
    raise ValueError(f"모르는 플레이어 역할: {name} (있는 것: rush, cautious, smart, plan1·plan4·plan8)")
