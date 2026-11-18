"""계획 AI 한 턴 더 보기(설계 docs/2026-11-18-planner-two-turn-lookahead-design.md LA-0).

손 판은 test_sim_rules.battle()로 만든다(players[0]이 메인, 적이 먼저 번호를 받는다).
"""

from __future__ import annotations

from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tests"))
sys.path.insert(0, str(ROOT))

from test_sim_rules import battle, to_enemy_turn  # noqa: E402
from sim.evaluate import Evaluator, PlannerConfig  # noqa: E402
from sim.main_focus import EnemyTurn, exposure  # noqa: E402
from sim.planner import planner_from_name  # noqa: E402
from sim.reply import best_replies, lookahead_board, sequential_turn  # noqa: E402
from sim.targeting import Side  # noqa: E402


def weak_enemy_board():
    """플레이어 차례. 아군 셋 바로 앞에 HP 1인 Demon 하나, 멀리 Demon 하나. 따로 고르면 여럿이 약한 Demon을 노린다."""
    state = battle([("Unit_Akimbo", (4, 2)), ("Unit_ShotGun", (3, 2)), ("Unit_Sniper", (5, 2))],
                   [("Unit_Demon", (4, 3)), ("Unit_Demon", (4, 8))])
    weak = min((u for u in state.units.values() if u.side is Side.ENEMY), key=lambda u: u.uid)
    weak.hp = 1
    return state, weak.uid


def hits(replies, uid):
    return sum(1 for r in replies if ("unit", uid) in r.lost)


class NameTests(unittest.TestCase):
    def test_knob_parses_and_defaults_to_off(self):
        self.assertEqual(planner_from_name(Side.ENEMY, "plan4:g1:l1").config.lookahead, 1)
        self.assertEqual(planner_from_name(Side.ENEMY, "plan4:g1").config.lookahead, 0)
        self.assertEqual(PlannerConfig().lookahead, 0)

    def test_only_zero_or_one_is_accepted(self):
        for name in ("plan4:l2", "plan4:l-1"):
            with self.assertRaises(ValueError):
                planner_from_name(Side.ENEMY, name)


class SequentialTurnTests(unittest.TestCase):
    def test_later_units_see_what_earlier_units_did(self):
        state, weak = weak_enemy_board()
        self.assertIs(state.turn_side, Side.PLAYER)
        evaluator = Evaluator(state, PlannerConfig())
        apart = best_replies(state, Side.PLAYER, evaluator)
        played, together = sequential_turn(state, Side.PLAYER, evaluator)
        # 따로 고르면 둘 이상이 같은 약한 대상을 노린다(이 판의 전제)
        self.assertGreaterEqual(hits(apart, weak), 2)
        # 차례대로 두면 처음 한 유닛만 잡고, 그 뒤 판에서는 이미 죽었다
        self.assertEqual(hits(together, weak), 1)
        self.assertFalse(played.units[weak].alive)
        self.assertTrue(state.units[weak].alive)     # 원래 판은 그대로

    def test_units_with_nothing_worth_doing_stay(self):
        state, _ = weak_enemy_board()
        evaluator = Evaluator(state, PlannerConfig())
        _, together = sequential_turn(state, Side.PLAYER, evaluator)
        self.assertTrue(all(r.gain > 0 for r in together))


class LookaheadBoardTests(unittest.TestCase):
    def test_returns_the_planner_side_turn_after_the_opponent_played(self):
        state, _ = weak_enemy_board()
        enemy_turn = to_enemy_turn(state)
        evaluator = Evaluator(enemy_turn, PlannerConfig(lookahead=1))
        board, _ = lookahead_board(enemy_turn, Side.ENEMY, evaluator, seed=1)
        if board.outcome is None:
            self.assertIs(board.turn_side, Side.ENEMY)
            self.assertGreater(board.round, enemy_turn.round)

    def test_requires_the_planner_side_turn(self):
        state, _ = weak_enemy_board()
        evaluator = Evaluator(state, PlannerConfig(lookahead=1))
        with self.assertRaises(ValueError):
            lookahead_board(state, Side.ENEMY, evaluator, seed=1)

    def test_deep_breakdown_uses_the_branch_only_when_on(self):
        state, _ = weak_enemy_board()
        enemy_turn = to_enemy_turn(state)
        on = Evaluator(enemy_turn, PlannerConfig(lookahead=1)).deep_breakdown(enemy_turn, Side.ENEMY, 1)
        off = Evaluator(enemy_turn, PlannerConfig()).deep_breakdown(enemy_turn, Side.ENEMY, 1)
        self.assertEqual(on.get("lookahead"), 1)
        self.assertNotIn("lookahead", off)


class ExposureTests(unittest.TestCase):
    def turn(self, attacked):
        """약한 Demon이 다음 적 턴 전에 죽은 적 턴. attacked면 그 Demon이 이 적 턴에 공격했다."""
        state, weak = weak_enemy_board()
        end = to_enemy_turn(state)
        after = end.clone()
        after.units[weak].hp = 0
        segment = [{"type": "attack", "uid": weak, "unit": "Unit_Demon", "aim": (4, 2), "hits": []}] if attacked else []
        return EnemyTurn(end.round, end, end, segment, after)

    def test_counts_enemies_killed_before_the_next_enemy_turn(self):
        self.assertEqual(exposure(self.turn(attacked=False)), (1, 1))

    def test_an_enemy_that_attacked_is_not_idle(self):
        self.assertEqual(exposure(self.turn(attacked=True)), (1, 0))

    def test_no_following_board_means_nothing(self):
        turn = self.turn(attacked=False)
        turn.after = None
        self.assertEqual(exposure(turn), (0, 0))


if __name__ == "__main__":
    unittest.main()
