"""플레이 성격 손잡이(설계 docs/2026-11-18-enemy-playstyle-knobs-design.md). PK0 자기 보존 `:p`, PK6 행동 규칙 `:i`,
판 값 고치기 VF1 걸음 벌점 대상 고정 `:v`·VF2 기회 모으기 `:b`(§12.8·§13).

손 판은 test_sim_rules.battle()로 만든다(players[0]이 메인). 수치의 근거는 test_sim_curved_terms와 같은 판이다.
"""

from __future__ import annotations

from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tests"))
sys.path.insert(0, str(ROOT))

from test_sim_rules import battle, to_enemy_turn, unit_named  # noqa: E402
from sim.evaluate import Evaluator, PlannerConfig  # noqa: E402
from sim.planner import _Node, planner_from_name  # noqa: E402
from sim.rules import EndTurn, apply  # noqa: E402
from sim.state import ActionUse  # noqa: E402
from sim.targeting import Side  # noqa: E402


def focus_board():
    """Demon 둘이 Sniper(HP 10, 공 5)를 함께 칠 수 있다: 7 + 7 = 14 ≥ 10 → 처치(test_sim_evaluate와 같은 판)."""
    return battle([("Unit_Akimbo", (0, 0)), ("Unit_Sniper", (6, 4))],
                  [("Unit_Demon", (6, 7)), ("Unit_Demon", (8, 6))])


class PreserveNameTests(unittest.TestCase):
    def test_knob_parses_and_defaults_to_off(self):
        self.assertEqual(planner_from_name(Side.ENEMY, "plan4:g1:l1:p1.5").config.preserve, 1.5)
        self.assertEqual(planner_from_name(Side.ENEMY, "plan4:g1:l1").config.preserve, 0.0)
        with self.assertRaises(ValueError):
            planner_from_name(Side.ENEMY, "plan4:p-1")

    def test_evaluator_needs_the_planning_side(self):
        state = focus_board()
        with self.assertRaises(ValueError):
            Evaluator(state, PlannerConfig(preserve=1.0))
        Evaluator(state, PlannerConfig(preserve=1.0), Side.ENEMY)     # 편을 주면 된다
        Evaluator(state, PlannerConfig())                             # 끄면 편 없이도 된다


class PreserveValueTests(unittest.TestCase):
    def test_only_the_planning_side_gets_the_lump(self):
        # 메인 Akimbo 공 10 × HP 30 × 3 = 900. 플레이어가 계획하는 편이면 + 1 × 10 × 30 × 3 = 1800, 적이 계획하면 900 그대로
        state = battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4))])
        demon = unit_named(state, "Unit_Demon")
        plain = Evaluator(state).assets(state, Side.ENEMY)
        mine = Evaluator(state, PlannerConfig(preserve=1.0), Side.PLAYER)
        theirs = Evaluator(state, PlannerConfig(preserve=1.0), Side.ENEMY)
        self.assertEqual(mine.assets(state, Side.PLAYER), 1800)
        self.assertEqual(mine.assets(state, Side.ENEMY), plain)
        self.assertEqual(theirs.assets(state, Side.PLAYER), 900)
        self.assertEqual(theirs.assets(state, Side.ENEMY), plain + demon.attack * demon.max_hp)

    def test_death_of_own_unit_costs_the_lump_but_kills_gain_nothing(self):
        # 처치되는 Sniper: 5 × 10 = 50. 플레이어가 계획하는 편이면(자기 유닛) 50 + 5 × 10 = 100,
        # 적이 계획하는 편이면(상대 유닛을 잡는 것) 50 그대로 — 생존 단절 s와 다른 점
        state = focus_board()
        sniper = unit_named(state, "Unit_Sniper")
        own = Evaluator(state, PlannerConfig(preserve=1.0), Side.PLAYER)
        other = Evaluator(state, PlannerConfig(preserve=1.0), Side.ENEMY)
        self.assertEqual(own.risk(state, Side.PLAYER), 100)
        self.assertEqual(own._loss(sniper, 14), 100)
        self.assertEqual(other._loss(sniper, 14), 50)
        self.assertEqual(Evaluator(state, PlannerConfig(survival=1.0))._loss(sniper, 14), 100)   # s는 양쪽 다

    def test_non_lethal_loss_is_unchanged(self):
        state = focus_board()
        sniper = unit_named(state, "Unit_Sniper")
        self.assertEqual(Evaluator(state, PlannerConfig(preserve=1.0), Side.PLAYER)._loss(sniper, 7), 35)

    def test_planner_runs_with_the_knob(self):
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4))]))
        policy = planner_from_name(Side.ENEMY, "plan4:g1:l1:p1")
        self.assertIsNotNone(policy.act(state))


class PreserveHurtTests(unittest.TestCase):
    """PK0b 다친 유닛에만 살리기 `:y`: 계획을 시작할 때 다친 우리 유닛에만 살아 있음 덩어리가 붙는다."""

    def board(self):
        state = to_enemy_turn(focus_board())
        demons = sorted(state.living(Side.ENEMY), key=lambda u: u.uid)
        demons[0].hp = demons[0].max_hp // 2          # 다친 유닛(HP 10/20)
        return state, demons[0], demons[1]

    def test_knob_parses_and_defaults_to_off(self):
        self.assertEqual(planner_from_name(Side.ENEMY, "plan4:g1:l1:y1.5").config.preserve_hurt, 1.5)
        self.assertEqual(planner_from_name(Side.ENEMY, "plan4:g1:l1").config.preserve_hurt, 0.0)
        with self.assertRaises(ValueError):
            planner_from_name(Side.ENEMY, "plan4:y-1")
        with self.assertRaises(ValueError):
            Evaluator(focus_board(), PlannerConfig(preserve_hurt=1.0))

    def test_only_units_hurt_at_the_start_of_the_plan_get_the_lump(self):
        state, hurt, fresh = self.board()
        evaluator = Evaluator(state, PlannerConfig(preserve_hurt=1.0), Side.ENEMY)
        self.assertEqual(evaluator.keep_value(hurt), 1.0 * hurt.attack * hurt.max_hp)
        self.assertEqual(evaluator.keep_value(fresh), 0.0)
        self.assertEqual(evaluator.keep_value(unit_named(state, "Unit_Sniper")), 0.0)
        # 자기 보존(모든 유닛)과 함께 켜면 다친 유닛은 둘을 더한 값
        both = Evaluator(state, PlannerConfig(preserve=0.5, preserve_hurt=1.0), Side.ENEMY)
        self.assertEqual(both.keep_value(hurt), 1.5 * hurt.attack * hurt.max_hp)
        self.assertEqual(both.keep_value(fresh), 0.5 * fresh.attack * fresh.max_hp)

    def test_getting_hurt_during_the_plan_adds_nothing(self):
        # 계획 도중(응수를 둬 본 판)에 다친 유닛에는 붙지 않는다: 다쳤다고 자산이 늘면 안 된다
        state, hurt, fresh = self.board()
        evaluator = Evaluator(state, PlannerConfig(preserve_hurt=1.0), Side.ENEMY)
        before = evaluator.assets(state, Side.ENEMY)
        later = state.clone()
        later.units[fresh.uid].hp = 1
        self.assertEqual(evaluator.keep_value(later.units[fresh.uid]), 0.0)
        self.assertLess(evaluator.assets(later, Side.ENEMY), before)


class MustActTests(unittest.TestCase):
    """PK6 행동 규칙: 행동해서 보는 손해가 아주 크지 않으면 유닛은 그 턴에 행동한다."""

    def board(self):
        return to_enemy_turn(battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4)), ("Unit_Demon", (8, 8))]))

    def test_knob_parses_and_defaults_to_off(self):
        self.assertEqual(planner_from_name(Side.ENEMY, "plan4:g1:l1:i0.5").config.must_act, 0.5)
        self.assertEqual(planner_from_name(Side.ENEMY, "plan4:g1:l1").config.must_act, 0.0)
        with self.assertRaises(ValueError):
            planner_from_name(Side.ENEMY, "plan4:i-1")

    def test_idle_cost_counts_units_that_have_not_acted(self):
        state = self.board()
        demons = sorted(state.living(Side.ENEMY), key=lambda u: u.uid)
        power = demons[0].attack * demons[0].max_hp
        policy = planner_from_name(Side.ENEMY, "plan4:i0.5")
        both = _Node(state, frozenset(u.uid for u in demons))
        self.assertEqual(policy.idle_cost(both), 0.5 * 2 * power)
        self.assertEqual(policy.idle_cost(_Node(state, frozenset({demons[0].uid}))), 0.5 * power)
        # 이번 턴에 이미 이동한 유닛은 제자리가 아니다
        moved = state.clone()
        moved.units[demons[0].uid].use = ActionUse.MOVED
        self.assertEqual(policy.idle_cost(_Node(moved, frozenset(u.uid for u in demons))), 0.5 * power)
        # 끄면 0
        self.assertEqual(planner_from_name(Side.ENEMY, "plan4").idle_cost(both), 0.0)

    def test_every_unit_acts_when_the_limit_is_high(self):
        # 한도를 아주 크게 주면(전력의 100배) 행동할 수 있는 유닛은 모두 이동하거나 공격한다
        state = self.board()
        policy = planner_from_name(Side.ENEMY, "plan4:g1:l1:i100")
        for _ in range(20):
            action = policy.act(state)
            if isinstance(action, EndTurn):
                break
            state = apply(state, action)
        for unit in state.living(Side.ENEMY):
            self.assertIn(unit.use, (ActionUse.MOVED, ActionUse.ATTACKED))


class ValueFixTests(unittest.TestCase):
    """판 값 고치기: 지금 치면 손해로 계산되던 두 항목(설계 §12.8). 끄면 옛 값과 같다."""

    @staticmethod
    def endgame():
        # 12×12 적 턴. 메인 Akimbo는 멀리 (0,0), HP 1인 ShotGun(공 7) 옆에 Demon 둘. 잡으면 남은 상대는 먼 Akimbo뿐이다.
        state = battle([("Unit_Akimbo", (0, 0)), ("Unit_ShotGun", (8, 8))],
                       [("Unit_Demon", (8, 10)), ("Unit_Demon", (10, 9))], size=(12, 12))
        unit_named(state, "Unit_ShotGun").hp = 1
        return to_enemy_turn(state)

    @staticmethod
    def play_turn(state, name):
        policy = planner_from_name(Side.ENEMY, name)
        for _ in range(20):
            action = policy.act(state)
            if isinstance(action, EndTurn):
                break
            state = apply(state, action)
        return state

    def test_knobs_parse_and_default_to_off(self):
        config = planner_from_name(Side.ENEMY, "plan4:g1:l1:v1:b1").config
        self.assertEqual((config.step_anchor, config.pooled_chance), (1, 1))
        config = planner_from_name(Side.ENEMY, "plan4:g1:l1").config
        self.assertEqual((config.step_anchor, config.pooled_chance), (0, 0))
        self.assertEqual(planner_from_name(Side.ENEMY, "plan4:g1:l1:v0:b0").config, config)
        for name in ("plan4:v2", "plan4:b2"):
            with self.assertRaises(ValueError):
                planner_from_name(Side.ENEMY, name)

    def test_chance_is_pooled_per_target_up_to_its_hp(self):
        # Demon 둘이 다음 턴에 Sniper(공 5)를 7씩 칠 수 있다.
        # 끈 식: 유닛마다 min(7, HP) × 5를 더한다 → HP 10이면 70, HP 3이면 30(남은 HP를 두 번 센다).
        # 켠 식: 피해를 모아 min(14, HP) × 5 → HP 10이면 50, HP 3이면 15.
        state = to_enemy_turn(focus_board())
        sniper = unit_named(state, "Unit_Sniper")
        for hp, plain, pooled in ((10, 70.0, 50.0), (3, 30.0, 15.0)):
            sniper.hp = hp
            self.assertEqual(Evaluator(state, PlannerConfig(), Side.ENEMY).opportunity(state, Side.ENEMY)[0], plain)
            self.assertEqual(Evaluator(state, PlannerConfig(pooled_chance=1), Side.ENEMY).opportunity(state, Side.ENEMY)[0],
                             pooled)

    def test_hitting_now_no_longer_costs_more_chance_than_it_gains(self):
        # Sniper HP 10 → 3으로 깎으면(7 × 5 = 35를 얻는다) 끈 식은 기회가 70 → 30으로 40 줄고, 켠 식은 50 → 15로 35 준다.
        # 기회 배수 0.5를 곱하면 끈 식도 35 − 20 > 0이지만, 노리는 유닛이 늘수록 끈 식의 감소만 커진다(셋이면 105 → 45).
        state = to_enemy_turn(focus_board())
        sniper = unit_named(state, "Unit_Sniper")

        def chance(config, hp):
            sniper.hp = hp
            return Evaluator(state, config, Side.ENEMY).opportunity(state, Side.ENEMY)[0]

        self.assertEqual(chance(PlannerConfig(), 10) - chance(PlannerConfig(), 3), 40.0)
        self.assertEqual(chance(PlannerConfig(pooled_chance=1), 10) - chance(PlannerConfig(pooled_chance=1), 3), 35.0)

    def test_killing_the_near_target_does_not_raise_the_step_penalty(self):
        # ShotGun을 잡은 판: 끈 식은 남은 상대(먼 Akimbo)까지의 걸음으로 벌점 290이 생긴다.
        # 켠 식은 계획 시작 때 살아 있던 ShotGun의 칸도 "닿은 곳"으로 남아 벌점이 그대로 0이다.
        state = self.endgame()
        killed = state.clone()
        unit_named(killed, "Unit_ShotGun").hp = 0
        plain = Evaluator(state, PlannerConfig(), Side.ENEMY)
        self.assertEqual(plain.opportunity(state, Side.ENEMY)[1], 0.0)
        self.assertEqual(plain.opportunity(killed, Side.ENEMY)[1], 290.0)
        anchored = Evaluator(state, PlannerConfig(step_anchor=1), Side.ENEMY)
        self.assertEqual(anchored.opportunity(killed, Side.ENEMY)[1], 0.0)
        # 쓰러진 상대는 죽은 순간의 칸이 아니라 계획 시작 판의 칸으로 센다(게임 Core는 죽은 캐릭터의 칸을 지운다)
        pushed = state.clone()
        fallen = unit_named(pushed, "Unit_ShotGun")
        fallen.cell, fallen.hp = (0, 11), 0
        cells = {t.cell for t in anchored.step_targets(pushed, Side.ENEMY)}
        self.assertIn((8, 8), cells)
        self.assertNotIn((0, 11), cells)
        # 계획을 새로 시작하면(다음 턴) 살아 있는 상대만 본다: 잡은 판에서 시작한 평가기는 다시 벌점을 준다
        self.assertEqual(Evaluator(killed, PlannerConfig(step_anchor=1), Side.ENEMY).opportunity(killed, Side.ENEMY)[1], 290.0)

    def test_the_planner_finishes_the_dying_unit(self):
        # 지금 식(끔)은 HP 1짜리를 잡지 않고 둘 다 이동만 한다(잡으면 걸음 벌점이 늘어서). 켜면 잡는다.
        left = self.play_turn(self.endgame(), "plan4:g1:l1:i0.25")
        self.assertTrue(unit_named(left, "Unit_ShotGun").alive)
        done = self.play_turn(self.endgame(), "plan4:g1:l1:i0.25:v1:b1")
        self.assertFalse(unit_named(done, "Unit_ShotGun").alive)


if __name__ == "__main__":
    unittest.main()
