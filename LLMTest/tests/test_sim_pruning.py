"""계획 AI 속도 개선: 이동 + 공격 도착 칸 거름(설계 docs/2026-11-18-planner-candidate-pruning-design.md SP-a).

손 판은 test_sim_rules.battle()로 만든다(players[0]이 메인, 적이 먼저 번호를 받는다).
"""

from __future__ import annotations

from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tests"))
sys.path.insert(0, str(ROOT))

from test_sim_planner import play_turn  # noqa: E402
from test_sim_rules import battle, to_enemy_turn, unit_named  # noqa: E402
from sim.analysis import attack_destinations, attack_results, unit_options  # noqa: E402
from sim.evaluate import Evaluator, PlannerConfig  # noqa: E402
from sim.planner import planner_from_name  # noqa: E402
from sim.reply import engine_terms  # noqa: E402
from sim.targeting import Side  # noqa: E402


def crowd_board():
    """적 Demon 둘이 아군 셋 근처: Demon마다 사거리 + 1 안에 무언가 있는 도착 칸이 여럿이다."""
    return to_enemy_turn(battle([("Unit_Akimbo", (4, 2)), ("Unit_ShotGun", (2, 2)), ("Unit_Sniper", (6, 2))],
                                [("Unit_Demon", (4, 5)), ("Unit_Demon", (2, 5))]))


class NameTests(unittest.TestCase):
    def test_knobs_parse_as_integers_and_default_to_off(self):
        config = planner_from_name(Side.ENEMY, "plan4:g1:m3:q2").config
        self.assertEqual((config.move_attack_cells, config.reply_move_attack_cells), (3, 2))
        self.assertEqual((PlannerConfig().move_attack_cells, PlannerConfig().reply_move_attack_cells), (0, 0))

    def test_negative_cell_counts_are_rejected(self):
        with self.assertRaises(ValueError):
            planner_from_name(Side.ENEMY, "plan4:m-1")
        with self.assertRaises(ValueError):
            planner_from_name(Side.ENEMY, "plan4:q-2")


class BestCellsTests(unittest.TestCase):
    def test_no_filter_when_off_or_few_cells(self):
        cells = [(0, 0), (1, 0), (2, 0)]
        self.assertIsNone(Evaluator.best_cells(cells, lambda c: c[0], 0))
        self.assertIsNone(Evaluator.best_cells(cells, lambda c: c[0], 3))
        self.assertIsNone(Evaluator.best_cells(cells, lambda c: c[0], 5))

    def test_keeps_the_top_cells_and_the_earlier_cell_on_ties(self):
        cells = [(0, 0), (1, 0), (2, 0), (3, 0)]
        self.assertEqual(Evaluator.best_cells(cells, lambda c: c[0], 2), {(3, 0), (2, 0)})
        self.assertEqual(Evaluator.best_cells(cells, lambda c: 1.0, 2), {(0, 0), (1, 0)})


class OptionFilterTests(unittest.TestCase):
    def test_none_means_the_old_options(self):
        state = crowd_board()
        demon = unit_named(state, "Unit_Demon")
        self.assertEqual(unit_options(state, demon, attack_cells=None), unit_options(state, demon))
        self.assertEqual([o for o, _ in attack_results(state, demon, attack_cells=None)],
                         [o for o, _ in attack_results(state, demon)])

    def test_move_attacks_come_only_from_allowed_cells(self):
        state = crowd_board()
        demon = unit_named(state, "Unit_Demon")
        cells = attack_destinations(state, demon)
        self.assertGreater(len(cells), 1)
        full = unit_options(state, demon)
        # 실제로 이동 뒤 공격이 나오는 칸 하나만 허용한다(사거리 + 1 거름은 칠 수 없는 칸도 넣는다)
        attacking = [o.move.path[-1] for o in full if o.move is not None and o.attack is not None]
        self.assertGreater(len(set(attacking)), 1)
        allowed = {attacking[0]}
        options = unit_options(state, demon, attack_cells=allowed)
        moved_attacks = [o for o in options if o.move is not None and o.attack is not None]
        self.assertTrue(moved_attacks)
        self.assertTrue(all(o.move.path[-1] in allowed for o in moved_attacks))
        # 이동만·제자리 공격은 그대로 남는다
        self.assertEqual([o for o in options if o.attack is None or o.move is None],
                         [o for o in full if o.attack is None or o.move is None])
        results = attack_results(state, demon, attack_cells=allowed)
        self.assertTrue(all(o.move is None or o.move.path[-1] in allowed for o, _ in results))


class ScoreTests(unittest.TestCase):
    def test_strike_cells_are_part_of_the_reach_table(self):
        state = crowd_board()
        evaluator = Evaluator(state)
        demon = unit_named(state, "Unit_Demon")
        strike, reach = evaluator.strike(demon, demon.cell), evaluator.reach(demon, demon.cell)
        self.assertTrue(set(strike) <= set(reach))
        self.assertLess(len(strike), len(reach))     # 이동으로 닿는 칸은 이번 공격에 없다

    def test_move_attack_score_prefers_a_cell_that_can_hit(self):
        state = crowd_board()
        evaluator = Evaluator(state)
        demon = unit_named(state, "Unit_Demon")
        akimbo = unit_named(state, "Unit_Akimbo")
        beside = (akimbo.cell[0], akimbo.cell[1] + 1)
        targets = evaluator._targets(state, Side.ENEMY)
        self.assertGreater(evaluator.strike_value(state, demon, beside, targets), 0)
        self.assertEqual(evaluator.strike_value(state, demon, (8, 8), targets), 0)


class PlannerTests(unittest.TestCase):
    def test_pruned_planner_plays_a_legal_turn(self):
        state = crowd_board()
        policy = planner_from_name(Side.ENEMY, "plan4:g1:m1:q1")
        after, actions = play_turn(policy, state)
        self.assertTrue(actions)

    def test_large_limits_change_nothing(self):
        # 칸이 limit개 이하이면 거르지 않으므로 큰 값은 끔과 같은 계획을 낸다
        state = crowd_board()
        _, off = play_turn(planner_from_name(Side.ENEMY, "plan4:g1"), state)
        _, wide = play_turn(planner_from_name(Side.ENEMY, "plan4:g1:m99:q99"), state)
        self.assertEqual(off, wide)

    def test_reply_limit_large_equals_off(self):
        state = crowd_board()
        off = engine_terms(state, Side.ENEMY, Evaluator(state, PlannerConfig()), seed=1)
        wide = engine_terms(state, Side.ENEMY, Evaluator(state, PlannerConfig(reply_move_attack_cells=99)), seed=1)
        self.assertEqual((off.risk, off.opportunity, off.step_penalty), (wide.risk, wide.opportunity, wide.step_penalty))


if __name__ == "__main__":
    unittest.main()
