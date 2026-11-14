"""정책 틀: 한 번 부를 때 행동 하나를 돌려준다(게임처럼 한 번에 한 행동).

`OptionPolicy`는 할 수 있는 (이동 + 공격) 조합을 모두 엔진으로 미리 돌려 보고 점수가 가장 높은 것을 한다.
점수가 0보다 큰 것이 없으면 턴을 끝낸다. 점수와 갈 수 없는 칸은 하위 정책이 정한다.
"""

from __future__ import annotations

from .analysis import Option, simulate, unit_options
from .rules import Action, EndTurn
from .state import BattleState, Unit


class Policy:
    name = "policy"

    def act(self, state: BattleState) -> Action:
        raise NotImplementedError

    def begin_turn(self, state: BattleState) -> None:
        """턴 시작에 한 번(턴 동안 쓸 계산을 준비할 때)."""

    def holds(self) -> dict[int, str]:
        """마지막 턴 끝에 행동하지 않은 유닛과 이유(기다림 지표용)."""
        return {}


class OptionPolicy(Policy):
    def __init__(self):
        self._holds: dict[int, str] = {}

    # 하위 정책이 정하는 것
    def score(self, before: BattleState, after: BattleState, unit: Unit, option: Option) -> float:
        raise NotImplementedError

    def allowed(self, state: BattleState, unit: Unit, option: Option, after: BattleState) -> bool:
        return True

    def act(self, state: BattleState) -> Action:
        best: tuple[float, tuple, Option] | None = None
        reasons: dict[int, str] = {}
        for unit in sorted(state.living(state.turn_side), key=lambda u: u.uid):
            options = unit_options(state, unit)
            if not options:
                reasons[unit.uid] = "no_option"
                continue
            unit_best = None
            for option in options:
                after = simulate(state, option.actions())
                if not self.allowed(state, unit, option, after):
                    continue
                value = self.score(state, after, unit, option)
                # 같은 점수면 공격 먼저, 그다음 이동 없는 것, 그다음 유닛 번호
                key = (value, option.attack is not None, option.move is None, -unit.uid)
                if unit_best is None or key > unit_best[0]:
                    unit_best = (key, option)
            if unit_best is None or unit_best[0][0] <= 0:
                reasons[unit.uid] = "no_gain" if unit_best is not None else "not_allowed"
                continue
            if best is None or unit_best[0] > best[1]:
                best = (unit_best[0][0], unit_best[0], unit_best[1])
        if best is None:
            self._holds = reasons
            return EndTurn()
        return best[2].actions()[0]

    def holds(self) -> dict[int, str]:
        return dict(self._holds)
