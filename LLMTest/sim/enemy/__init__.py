"""적 정책. 이름으로 만든다: make_enemy_policy("greedy")."""

from __future__ import annotations

from ..game_ai import GameAIPolicy
from ..planner import planner_from_name
from ..targeting import Side
from .greedy import GreedyEnemyPolicy

ENEMY_POLICIES = {
    "greedy": GreedyEnemyPolicy,
    "game": lambda: GameAIPolicy("asset"),            # 지금 게임 AI, 조정 에셋 값
    "game-default": lambda: GameAIPolicy("codeDefault"),
}


def make_enemy_policy(name: str):
    """greedy, game, game-default, plan{K}[:f겁][:o기회][:r다시 매김][:d빔 다양성](턴 계획 AI, 예: plan4:f0.8:o0.3, plan4:r0, plan4:d1)."""
    if name in ENEMY_POLICIES:
        return ENEMY_POLICIES[name]()
    if name.startswith("plan"):
        return planner_from_name(Side.ENEMY, name)
    raise ValueError(f"모르는 적 정책: {name} (있는 것: {', '.join(ENEMY_POLICIES)}, plan1·plan4·plan8)")
