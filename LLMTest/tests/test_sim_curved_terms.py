"""판 평가 곡선 항목 CV1~CV4(설계 docs/2026-11-18-planner-curved-terms-design.md CV-0).

손 판은 test_sim_rules.battle()로 만든다(players[0]이 메인). 수치의 근거는 test_sim_evaluate와 같은 판이다.
"""

from __future__ import annotations

from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tests"))
sys.path.insert(0, str(ROOT))

from test_sim_rules import battle, unit_named  # noqa: E402
from sim.evaluate import Evaluator, PlannerConfig  # noqa: E402
from sim.planner import planner_from_name  # noqa: E402
from sim.targeting import Side  # noqa: E402


def focus_board():
    """Demon 둘이 Sniper(HP 10, 공 5)를 함께 칠 수 있다: 7 + 7 = 14 ≥ 10 → 처치(test_sim_evaluate와 같은 판)."""
    return battle([("Unit_Akimbo", (0, 0)), ("Unit_Sniper", (6, 4))],
                  [("Unit_Demon", (6, 7)), ("Unit_Demon", (8, 6))])


class NameTests(unittest.TestCase):
    def test_knobs_parse_and_default_to_off(self):
        config = planner_from_name(Side.ENEMY, "plan4:g1:l1:s0.5:a5").config
        self.assertEqual((config.survival, config.approach), (0.5, 5))
        config = planner_from_name(Side.ENEMY, "plan4:g1:l1").config
        self.assertEqual((config.survival, config.approach), (0.0, 0))

    def test_negative_values_are_rejected(self):
        for name in ("plan4:s-1", "plan4:a-2"):
            with self.assertRaises(ValueError):
                planner_from_name(Side.ENEMY, name)


class SurvivalTests(unittest.TestCase):
    def test_assets_add_a_living_lump(self):
        # 메인 Akimbo 공 10 × HP 30 × 3 = 900, 살아 있음 1이면 10 × (30 + 30) × 3 = 1800
        state = battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4))])
        self.assertEqual(Evaluator(state).assets(state, Side.PLAYER), 900)
        self.assertEqual(Evaluator(state, PlannerConfig(survival=1.0)).assets(state, Side.PLAYER), 1800)

    def test_killing_loss_includes_the_lump(self):
        # 처치되는 Sniper: 5 × 10 = 50, 살아 있음 1이면 50 + 1 × 5 × 10 = 100
        state = focus_board()
        self.assertEqual(Evaluator(state).risk(state, Side.PLAYER), 50)
        self.assertEqual(Evaluator(state, PlannerConfig(survival=1.0)).risk(state, Side.PLAYER), 100)

    def test_non_lethal_loss_is_unchanged(self):
        # Demon → 메인 Akimbo 7 피해(HP 30 미만): 7 × 10 × 3 = 210, 살아 있음과 상관없음
        state = battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4))])
        self.assertEqual(Evaluator(state, PlannerConfig(survival=1.0)).risk(state, Side.PLAYER), 210)

    def test_danger_loss_uses_the_same_rule(self):
        state = focus_board()
        sniper = unit_named(state, "Unit_Sniper")
        for survival in (0.0, 1.0):
            evaluator = Evaluator(state, PlannerConfig(survival=survival))
            self.assertEqual(evaluator.danger_loss(state, sniper, sniper.cell), evaluator._loss(sniper, 14))


class ApproachTests(unittest.TestCase):
    def test_step_units_saturate(self):
        self.assertEqual(PlannerConfig().step_units(11), 11)
        config = PlannerConfig(approach=5)
        self.assertEqual(config.step_units(0), 0)
        self.assertAlmostEqual(config.step_units(1), 5 / 6)
        self.assertLess(config.step_units(1000), 5)
        self.assertGreater(config.step_units(1000), 4.9)

    def test_step_penalty_follows_the_curve(self):
        # Akimbo (0,0) → Demon (8,8): 넘는 걸음 11. 직선 11 × 10 = 110, k 5이면 10 × 5 × 11 / 16 = 34.375
        state = battle([("Unit_Akimbo", (0, 0))], [("Unit_Demon", (8, 8))])
        self.assertEqual(Evaluator(state).opportunity(state, Side.PLAYER), (0.0, 110.0))
        gain, penalty = Evaluator(state, PlannerConfig(approach=5)).opportunity(state, Side.PLAYER)
        self.assertEqual(gain, 0.0)
        self.assertAlmostEqual(penalty, 34.375)


class RatioTests(unittest.TestCase):
    def test_off_is_the_plain_difference(self):
        state = focus_board()
        evaluator = Evaluator(state)
        self.assertEqual(evaluator.asset_terms(300.0, 100.0), (200.0, 1.0, 1.0))

    def test_log_ratio_terms(self):
        import math
        state = focus_board()
        evaluator = Evaluator(state, PlannerConfig(ratio=1.0))
        scale = evaluator.asset_scale
        self.assertEqual(scale, (evaluator.assets(state, Side.PLAYER) + evaluator.assets(state, Side.ENEMY)) / 2)
        term, risk_w, gain_w = evaluator.asset_terms(299.0, 99.0)
        self.assertAlmostEqual(term, scale * math.log(3.0))
        self.assertAlmostEqual(risk_w, scale / 300.0)
        self.assertAlmostEqual(gain_w, scale / 100.0)

    def test_losing_a_unit_costs_more_when_few_remain(self):
        evaluator = Evaluator(focus_board(), PlannerConfig(ratio=1.0))
        rich = evaluator.asset_terms(1000.0, 500.0)[0] - evaluator.asset_terms(900.0, 500.0)[0]
        poor = evaluator.asset_terms(200.0, 500.0)[0] - evaluator.asset_terms(100.0, 500.0)[0]
        self.assertGreater(poor, rich)

    def test_half_mix(self):
        evaluator = Evaluator(focus_board(), PlannerConfig(ratio=0.5))
        full = Evaluator(focus_board(), PlannerConfig(ratio=1.0))
        term = evaluator.asset_terms(299.0, 99.0)[0]
        self.assertAlmostEqual(term, 0.5 * 200.0 + 0.5 * full.asset_terms(299.0, 99.0)[0])


class LogApproachTests(unittest.TestCase):
    def test_step_units_grow_without_limit(self):
        import math
        config = PlannerConfig(log_approach=5)
        self.assertEqual(config.step_units(0), 0)
        self.assertAlmostEqual(config.step_units(11), 5 * math.log1p(11 / 5))
        self.assertGreater(config.step_units(1000), config.step_units(100))
        self.assertGreater(config.step_units(1000), 25)

    def test_names_and_conflicts(self):
        config = planner_from_name(Side.ENEMY, "plan4:g1:l1:w0.5:k5").config
        self.assertEqual((config.ratio, config.log_approach), (0.5, 5))
        for name in ("plan4:w-1", "plan4:w1.5", "plan4:k-1", "plan4:a5:k5"):
            with self.assertRaises(ValueError):
                planner_from_name(Side.ENEMY, name)


if __name__ == "__main__":
    unittest.main()
