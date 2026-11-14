"""엔진 응수: 판 평가의 다음 턴 추정(위험·기회)을 도달 표 대신 엔진으로 계산한다.
설계 docs/2026-11-13-planner-engine-reply-design.md R1.

- 판: 위험 판 R = 지금 판에서 턴을 넘긴 판(상대 차례). 기회 판 O = R에서 한 번 더 넘긴 판(우리 차례, 상대는 아무것도
  안 했다고 본다 — 도달 표 기회 추정과 같은 가정). 턴 넘김도 엔진이 하므로 재생·보호막 다시 채움·무장 해제 줄기·증원이
  엔진 순서대로 들어간다. 난수는 고정 씨앗으로 다시 맞춘다(실제 난수는 보지 않는다).
- 응수: 그 편 유닛마다(uid 순) 공격이 든 후보를 엔진으로 돌려, 그 유닛 편 입장의 순이득
  (상대 편이 잃은 자산 − 자기 편이 잃은 자산)이 가장 큰 것 하나를 고른다. 0 이하뿐이면 아무것도 안 한다고 본다(Q-R1).
- 합치기: 고른 행동마다 상대 편 대상별 잃은 HP를 적고, 대상마다 더해 그 대상의 HP로 자른 뒤 자산 손실로 바꾼다(Q-E2).

점수 식은 특성을 모른다. 넉백·끌어오기 낙사, 처치 연쇄, 자폭, 보호막, 재생은 모두 엔진 결과(자산 변화)로 들어간다.
"""

from __future__ import annotations

from dataclasses import dataclass
import random
from typing import TYPE_CHECKING, Iterable

from .analysis import Option, attack_results
from .rules import EndTurn, apply
from .state import BattleState
from .targeting import Side

if TYPE_CHECKING:     # evaluate.py가 이 파일을 부르므로 형식 검사 때만 불러온다
    from .evaluate import Evaluator

TargetKey = tuple[str, int]    # ("unit", uid) 또는 ("structure", sid)


@dataclass(frozen=True)
class Reply:
    """한 유닛이 고른 응수."""
    uid: int
    option: Option
    gain: float                      # 그 유닛 편 입장의 순이득(자산 점)
    lost: dict[TargetKey, int]       # 상대 편 대상별 잃은 HP(죽거나 떨어지면 남은 HP 전부)


@dataclass(frozen=True)
class EngineTerms:
    """판 평가에 들어갈 엔진 추정 세 값과, 그것을 만든 응수."""
    risk: float                      # 상대 편 응수(R)가 우리 편에 준 손실 합
    opportunity: float               # 우리 편 응수(O)가 상대 편에 준 손실 합
    step_penalty: float              # O에서 공격 후보가 하나도 없는 우리 유닛의 거리 벌점
    risk_replies: tuple[Reply, ...]
    opportunity_replies: tuple[Reply, ...]


# ---------------------------------------------------------------- 판
def _seeded(state: BattleState, seed: int) -> BattleState:
    board = state.clone()
    board.rng = random.Random(seed)
    return board


def reply_boards(state: BattleState, side: Side, seed: int) -> tuple[BattleState, BattleState | None]:
    """(R, O). state는 side 차례이고 끝나지 않은 판이어야 한다. 턴을 넘기다 전투가 끝나면 O는 None.
    두 판 모두 턴을 넘긴 뒤 난수를 같은 씨앗으로 다시 맞춘다(처치 연쇄 같은 무작위가 판 차이에 끌려가지 않게)."""
    if state.outcome is not None:
        raise ValueError("끝난 판에는 응수가 없습니다")
    if state.turn_side is not side:
        raise ValueError(f"{side.value} 차례 판이어야 합니다(지금 {state.turn_side.value})")
    risk_board = _seeded(apply(_seeded(state, seed), EndTurn()), seed)
    if risk_board.outcome is not None:
        return risk_board, None
    return risk_board, _seeded(apply(_seeded(risk_board, seed), EndTurn()), seed)


# ---------------------------------------------------------------- 응수
def lost_hp(before: BattleState, after: BattleState, side: Side) -> dict[TargetKey, int]:
    """before → after에서 side 편 대상(산 유닛·산 구조물)이 잃은 HP. 0 이하(회복 포함)는 뺀다."""
    lost: dict[TargetKey, int] = {}
    for uid, unit in before.units.items():
        if unit.side is side and unit.alive:
            amount = unit.hp - max(0, after.units[uid].hp)
            if amount > 0:
                lost[("unit", uid)] = amount
    for sid, structure in before.structures.items():
        if structure.side is side and structure.alive:
            amount = structure.hp - max(0, after.structures[sid].hp)
            if amount > 0:
                lost[("structure", sid)] = amount
    return lost


def _replies(board: BattleState, side: Side, evaluator: Evaluator) -> tuple[list[Reply], list[int]]:
    """(고른 응수들, 공격 후보가 하나도 없는 유닛들). board는 side 차례여야 한다."""
    opponent = side.other
    own_before = evaluator.assets(board, side)
    opp_before = evaluator.assets(board, opponent)
    replies: list[Reply] = []
    idle: list[int] = []
    for unit in sorted(board.living(side), key=lambda u: u.uid):
        results = attack_results(board, unit)
        if not results:
            idle.append(unit.uid)
            continue
        best = None
        for option, after in results:
            gain = (opp_before - evaluator.assets(after, opponent)) - (own_before - evaluator.assets(after, side))
            if gain > 0 and (best is None or gain > best[1]):    # 같으면 먼저 나온 것
                best = (option, gain, after)
        if best is not None:
            option, gain, after = best
            replies.append(Reply(unit.uid, option, gain, lost_hp(board, after, opponent)))
    return replies, idle


def best_replies(board: BattleState, side: Side, evaluator: Evaluator) -> list[Reply]:
    """board에서 side 유닛마다 가장 좋은 응수 하나(순이득이 0 이하뿐인 유닛은 빠진다)."""
    return _replies(board, side, evaluator)[0]


def combined_loss(board: BattleState, losses: Iterable[dict[TargetKey, int]], evaluator: Evaluator) -> float:
    """응수들의 대상별 잃은 HP를 대상마다 더하고, board에서 그 대상의 HP로 잘라 자산 손실로 바꾼 합."""
    totals: dict[TargetKey, int] = {}
    for lost in losses:
        for key, amount in lost.items():
            totals[key] = totals.get(key, 0) + amount
    total = 0.0
    for (kind, ident), amount in sorted(totals.items()):
        target = board.units[ident] if kind == "unit" else board.structures[ident]
        total += evaluator._loss(target, amount)
    return total


# ---------------------------------------------------------------- 위험·기회
def _step_penalty(board: BattleState, side: Side, idle: list[int], evaluator: Evaluator) -> float:
    """공격 후보가 없는 유닛: 칠 대상 옆까지 걸음 중 이동·사거리로 못 메우는 만큼 벌점(도달 표 추정과 같은 식)."""
    targets = evaluator._targets(board, side)
    if not idle or not targets:
        return 0.0
    field = evaluator._distance_field(board, targets)
    penalty = 0.0
    for uid in idle:
        unit = board.units[uid]
        steps = field.get(unit.cell)
        if steps is None:
            continue
        beyond = max(0, steps - unit.defn.move - max(0, unit.defn.attack_range - 1))
        penalty += evaluator.config.step_penalty * beyond
    return penalty


def _risk_on(risk_board: BattleState, side: Side, evaluator: Evaluator) -> tuple[float, list[Reply]]:
    """R(상대 차례)에서 상대 편 응수가 우리 편(side)에 준 손실 합."""
    if risk_board.outcome is not None:
        return 0.0, []
    replies = best_replies(risk_board, side.other, evaluator)
    return combined_loss(risk_board, (r.lost for r in replies), evaluator), replies


def _chance_on(chance_board: BattleState | None, side: Side, evaluator: Evaluator) -> tuple[float, float, list[Reply]]:
    """O(우리 차례)에서 (우리 편 응수가 상대 편에 준 손실 합, 거리 벌점, 응수)."""
    if chance_board is None or chance_board.outcome is not None:
        return 0.0, 0.0, []
    replies, idle = _replies(chance_board, side, evaluator)
    gain = combined_loss(chance_board, (r.lost for r in replies), evaluator)
    return gain, _step_penalty(chance_board, side, idle, evaluator), replies


def engine_terms(state: BattleState, side: Side, evaluator: Evaluator, seed: int) -> EngineTerms:
    """state(side 차례, 계획이 끝난 판)의 엔진 위험·기회·거리 벌점."""
    risk_board, chance_board = reply_boards(state, side, seed)
    risk, risk_replies = _risk_on(risk_board, side, evaluator)
    gain, penalty, chance_replies = _chance_on(chance_board, side, evaluator)
    return EngineTerms(risk, gain, penalty, tuple(risk_replies), tuple(chance_replies))


def engine_risk(state: BattleState, side: Side, evaluator: Evaluator, seed: int) -> float:
    """상대 편이 다음 차례에 우리 편(side) 자산을 얼마나 깎나(엔진 응수)."""
    risk_board, _ = reply_boards(state, side, seed)
    return _risk_on(risk_board, side, evaluator)[0]


def engine_opportunity(state: BattleState, side: Side, evaluator: Evaluator, seed: int) -> tuple[float, float]:
    """(기회, 거리 벌점) — Evaluator.opportunity와 같은 꼴. 우리 편(side)이 다음 차례에 상대 자산을 얼마나 깎나."""
    _, chance_board = reply_boards(state, side, seed)
    gain, penalty, _ = _chance_on(chance_board, side, evaluator)
    return gain, penalty
