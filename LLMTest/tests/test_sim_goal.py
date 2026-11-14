"""계획 AI 승리 대상 노림 G1·G2(설계 docs/2026-11-14-planner-goal-pressure-design.md G-a).

손 판은 test_sim_rules.battle()로 만든다(players[0]이 메인, 적이 먼저 번호를 받는다).
- 메인 가치(P1) = 공격력 × 최대 HP × 3: Sniper 5 × 10 × 3 = 150, Akimbo 10 × 30 × 3 = 900.
- 피해 손 계산: Demon(공 8) → Akimbo(방 5) = 8 × 95 // 100 = 7.
- G1 남은 걸음 = 메인 옆 칸까지 걷는 거리 − 이동력 − (사거리 − 1). Demon은 이동 3·사거리 1.
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
from sim.analysis import unit_options  # noqa: E402
from sim.assets import Objective  # noqa: E402
from sim.evaluate import Evaluator, PlannerConfig  # noqa: E402
from sim.planner import PlannerPolicy, planner_from_name, prediction_seed  # noqa: E402
from sim.targeting import Side  # noqa: E402


def far_board():
    # 메인 Sniper (0,0), Demon (8,8): 메인 옆 칸 (1,0)·(0,1)까지 15걸음 → 남은 걸음 15 − 3 = 12
    return to_enemy_turn(battle([("Unit_Sniper", (0, 0))], [("Unit_Demon", (8, 8))]))


def together_board():
    # 메인 Akimbo (4,2). Demon 둘이 다음 턴에 각각 (4,3)·(3,2)로 와서 친다: 7 + 7 = 14
    return to_enemy_turn(battle([("Unit_Akimbo", (4, 2))], [("Unit_Demon", (4, 6)), ("Unit_Demon", (2, 4))]))


class WinTargetTests(unittest.TestCase):
    def test_enemy_side_aims_at_the_player_main(self):
        state = far_board()
        self.assertEqual(Evaluator.win_targets(state, Side.ENEMY), [state.main_character()])

    def test_player_side_aims_at_the_headquarters(self):
        state = battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4))], hq=(4, 7))
        targets = Evaluator.win_targets(state, Side.PLAYER)
        self.assertEqual([t.name for t in targets], ["Structure_Headquarters"])
        evaluator = Evaluator(state)
        self.assertEqual(evaluator.win_value(targets[0]), evaluator.hq_value)

    def test_player_side_aims_at_the_victory_target_in_a_boss_battle(self):
        state = battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4)), ("Unit_Gravekeeper", (6, 6))],
                       objective=Objective.BOSS)
        boss = unit_named(state, "Unit_Gravekeeper")
        boss.victory_target = True
        self.assertEqual(Evaluator.win_targets(state, Side.PLAYER), [boss])
        # 격파 대상은 메인이 아니므로 가치 = 공격력 × 최대 HP
        self.assertEqual(Evaluator(state).win_value(boss), float(boss.attack * boss.max_hp))

    def test_dead_main_means_no_target_and_no_terms(self):
        state = far_board()
        state.main_character().hp = 0
        evaluator = Evaluator(state, PlannerConfig(goal_step=1.0, goal_together=1.0))
        self.assertEqual(evaluator.win_targets(state, Side.ENEMY), [])
        self.assertEqual(evaluator.goal_terms(state, Side.ENEMY), (0.0, 0.0))


class GoalStepTests(unittest.TestCase):
    def test_far_unit_counts_its_missing_steps(self):
        state = far_board()
        demon = unit_named(state, "Unit_Demon")
        evaluator = Evaluator(state, PlannerConfig(goal_step=2.0))
        targets = evaluator.win_targets(state, Side.ENEMY)
        self.assertEqual(evaluator.win_value(targets[0]), 150.0)
        self.assertEqual(evaluator.goal_steps(state, demon, demon.cell, targets), 12)
        self.assertEqual(evaluator.goal_penalty(state, demon, demon.cell, targets), 2.0 * 150 / 100 * 12)   # 36
        parts = evaluator.breakdown(state, Side.ENEMY)
        self.assertEqual((parts["goalStep"], parts["goalTogether"]), (-36.0, 0.0))
        expected = parts["assets"] - parts["opponentAssets"] - 0.5 * parts["risk"] + 0.5 * parts["opportunity"] \
            - parts["stepPenalty"] + parts["goalStep"]
        self.assertAlmostEqual(parts["total"], expected)

    def test_unit_that_can_hit_the_main_next_turn_has_no_missing_steps(self):
        # Demon (4,6) → (4,3)으로 와서 (4,2)의 메인을 칠 수 있다
        state = to_enemy_turn(battle([("Unit_Sniper", (4, 2))], [("Unit_Demon", (4, 6))]))
        demon = unit_named(state, "Unit_Demon")
        evaluator = Evaluator(state, PlannerConfig(goal_step=1.0))
        self.assertEqual(evaluator.goal_steps(state, demon, demon.cell, evaluator.win_targets(state, Side.ENEMY)), 0)
        self.assertEqual(evaluator.goal_terms(state, Side.ENEMY), (0.0, 0.0))

    def test_closer_cell_is_worth_more_only_when_the_term_is_on(self):
        # 같은 Demon을 (8,8)과 (5,8)에 두고 비교: 남은 걸음 12 → 9. g = 0이면 G1 차이는 없다.
        far = far_board()
        near = far.clone()
        unit_named(near, "Unit_Demon").cell = (5, 8)
        on = PlannerConfig(goal_step=1.0)
        for state, steps in ((far, 12), (near, 9)):
            evaluator = Evaluator(state, on)
            demon = unit_named(state, "Unit_Demon")
            self.assertEqual(evaluator.goal_steps(state, demon, demon.cell, evaluator.win_targets(state, Side.ENEMY)),
                             steps)
            off_total = Evaluator(state, PlannerConfig()).breakdown(state, Side.ENEMY)["total"]
            on_total = evaluator.breakdown(state, Side.ENEMY)["total"]
            self.assertAlmostEqual(on_total - off_total, -1.0 * 150 / 100 * steps)

    def test_move_only_candidates_follow_the_term(self):
        # 메인 Sniper (0,8)은 멀고 ShotGun (8,3)은 가깝다. g = 0이면 이동만 후보 상위 셋은 ShotGun 쪽이고,
        # 메인 쪽으로 가장 가까워지는 (5,8)(남은 걸음 1)은 들지 않는다. g = 20이면 (5,8)이 1위다(P5).
        state = to_enemy_turn(battle([("Unit_Sniper", (0, 8)), ("Unit_ShotGun", (8, 3))], [("Unit_Demon", (8, 8))]))
        demon = unit_named(state, "Unit_Demon")
        moves = [option for option in unit_options(state, demon) if option.attack is None]

        def top3(goal_step):
            policy = PlannerPolicy(Side.ENEMY, PlannerConfig(goal_step=goal_step))
            evaluator = Evaluator(state, policy.config)
            ranked = sorted(moves, key=lambda o: -policy._quick_move_score(state, demon, o, evaluator))
            return [option.move.path[-1] for option in ranked[:3]]

        self.assertNotIn((5, 8), top3(0.0))
        self.assertEqual(top3(20.0)[0], (5, 8))


class GoalTogetherTests(unittest.TestCase):
    def test_value_is_t_times_target_value_times_ratio_squared(self):
        state = together_board()
        evaluator = Evaluator(state, PlannerConfig(goal_together=0.5))
        self.assertEqual(evaluator.goal_terms(state, Side.ENEMY), (0.0, 0.5 * 900 * (14 / 30) ** 2))   # 98

    def test_one_unit_alone_is_worth_less_than_half_of_two(self):
        # 혼자면 r = 7/30, 둘이면 14/30: r²라서 둘의 값이 혼자의 네 배다
        two = together_board()
        one = two.clone()
        unit_named(one, "Unit_Demon").hp = 0                      # 먼저 번호를 받은 (4,6)의 Demon을 없앤다
        config = PlannerConfig(goal_together=0.5)
        alone = Evaluator(one, config).goal_terms(one, Side.ENEMY)[1]
        both = Evaluator(two, config).goal_terms(two, Side.ENEMY)[1]
        self.assertAlmostEqual(alone, 0.5 * 900 * (7 / 30) ** 2)
        self.assertAlmostEqual(both, 4 * alone)

    def test_ratio_is_capped_at_one(self):
        state = together_board()
        state.main_character().hp = 10                            # 14 ≥ 10 → r = 1
        evaluator = Evaluator(state, PlannerConfig(goal_together=0.5))
        self.assertEqual(evaluator.goal_terms(state, Side.ENEMY)[1], 0.5 * 900)

    def test_deep_value_adds_the_same_terms(self):
        state = together_board()
        on, off = PlannerConfig(goal_step=1.0, goal_together=0.5), PlannerConfig()
        seed = prediction_seed(state, Side.ENEMY)
        deep_on = Evaluator(state, on).deep_breakdown(state, Side.ENEMY, seed)
        deep_off = Evaluator(state, off).deep_breakdown(state, Side.ENEMY, seed)
        fast_on = Evaluator(state, on).breakdown(state, Side.ENEMY)
        self.assertEqual((deep_on["goalStep"], deep_on["goalTogether"]), (fast_on["goalStep"], fast_on["goalTogether"]))
        self.assertAlmostEqual(deep_on["total"] - deep_off["total"], deep_on["goalStep"] + deep_on["goalTogether"])


class OffIsUnchangedTests(unittest.TestCase):
    def test_breakdown_without_the_terms_is_the_old_sum(self):
        state = together_board()
        parts = Evaluator(state, PlannerConfig()).breakdown(state, Side.ENEMY)
        self.assertEqual((parts["goalStep"], parts["goalTogether"]), (0.0, 0.0))
        old = parts["assets"] - parts["opponentAssets"] - 0.5 * parts["risk"] + 0.5 * parts["opportunity"] \
            - parts["stepPenalty"]
        self.assertEqual(parts["total"], old)

    def test_zero_knobs_plan_the_same_turn(self):
        state = to_enemy_turn(battle([("Unit_Sniper", (0, 8)), ("Unit_ShotGun", (8, 3))],
                                     [("Unit_Demon", (8, 8)), ("Unit_Gravekeeper", (6, 7))]))
        default = play_turn(planner_from_name(Side.ENEMY, "plan4"), state)[1]
        zeros = play_turn(planner_from_name(Side.ENEMY, "plan4:g0:t0"), state)[1]
        self.assertEqual(default, zeros)


class NameTests(unittest.TestCase):
    def test_name_reads_both_knobs(self):
        config = planner_from_name(Side.ENEMY, "plan4:g1:t0.5").config
        self.assertEqual((config.goal_step, config.goal_together), (1.0, 0.5))
        self.assertEqual(planner_from_name(Side.ENEMY, "plan4").config.goal_step, 0.0)

    def test_negative_values_are_rejected(self):
        for name in ("plan4:g-1", "plan4:t-0.5"):
            with self.assertRaises(ValueError):
                planner_from_name(Side.ENEMY, name)


if __name__ == "__main__":
    unittest.main()
