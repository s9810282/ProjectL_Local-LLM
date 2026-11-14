"""단순 탐욕 적: 시뮬레이터 확인용 기준(설계 §A5). 연구 대상이 아니다.

할 수 있는 (이동 + 공격) 중 지금 결과가 가장 좋은 것을 한다. 칠 게 없으면 가장 가까운 플레이어 쪽으로 간다.
위험(다음 플레이어 턴에 맞을지)은 보지 않는다.
"""

from __future__ import annotations

from ..analysis import Option, change, distance_field, neighbors_of
from ..policy import OptionPolicy
from ..state import BattleState, Unit
from ..targeting import Side


class GreedyEnemyPolicy(OptionPolicy):
    name = "enemy-greedy"

    def __init__(self):
        super().__init__()
        self._field_for = None
        self._field: dict = {}

    def _approach_field(self, state: BattleState) -> dict:
        """가장 가까운 플레이어 유닛 옆 칸까지의 거리. 같은 판에서는 한 번만 계산한다."""
        if self._field_for is not state:
            self._field_for = state
            self._field = distance_field(state, neighbors_of([u.cell for u in state.living(Side.PLAYER)]))
        return self._field

    def score(self, before: BattleState, after: BattleState, unit: Unit, option: Option) -> float:
        c = change(before, after, Side.ENEMY)
        if c.win:
            return 1_000_000
        if c.loss:
            return -1_000_000
        value = 60 * c.opp_kills + c.opp_hp_lost - c.own_hp_lost - 40 * c.own_kills - 3 * c.own_hq_damage
        if option.move is not None:
            field = self._approach_field(before)
            far = max(field.values(), default=0) + 1
            value += field.get(unit.cell, far) - field.get(option.move.path[-1], far)
        return value
