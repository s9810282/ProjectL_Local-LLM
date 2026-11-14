"""영리한 플레이어 모형: 게임 AI 절차를 플레이어 쪽에서 + 승리 조건 인식·몰아치기 검사·메인 보호."""

from __future__ import annotations

from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tests"))
sys.path.insert(0, str(ROOT))

from test_sim_rules import battle, unit_named  # noqa: E402
from sim.analysis import reach_damage  # noqa: E402
from sim.game_ai import EngagementField  # noqa: E402
from sim.player import SmartPlayerPolicy, make_player_policy  # noqa: E402
from sim.rules import Attack, EndTurn, apply  # noqa: E402
from sim.state import Outcome  # noqa: E402
from sim.targeting import Side  # noqa: E402


class ReachDamageTests(unittest.TestCase):
    def test_sums_every_enemy_that_can_move_and_hit(self):
        # Demon 둘(공 8, 이동 3 + 사거리 1)이 모두 (4,4)를 칠 수 있다 → 16. 한 유닛은 한 칸에 한 번만 센다.
        state = battle([("Unit_Akimbo", (0, 0))], [("Unit_Demon", (4, 7)), ("Unit_Demon", (1, 4))])
        self.assertEqual(reach_damage(state, Side.ENEMY)[(4, 4)], 16)
        self.assertNotIn((8, 0), reach_damage(state, Side.ENEMY))


class SmartPlayerTests(unittest.TestCase):
    def test_takes_the_winning_hit_on_headquarters(self):
        state = battle([("Unit_Akimbo", (4, 1)), ("Unit_ShotGun", (0, 0))], [("Unit_Demon", (0, 8))], hq=(4, 3))
        next(iter(state.structures.values())).hp = 1
        action = SmartPlayerPolicy().act(state)
        self.assertIsInstance(action, Attack)
        self.assertIs(apply(state, action).outcome, Outcome.PLAYER_WIN)

    def test_moves_toward_headquarters_not_nearest_enemy(self):
        # 이동 목표는 적 본진이다(게임 AI라면 가장 가까운 적 유닛 쪽).
        state = battle([("Unit_Akimbo", (0, 0)), ("Unit_ShotGun", (4, 1))], [("Unit_Demon", (0, 8))], hq=(7, 7))
        policy = SmartPlayerPolicy()
        field = EngagementField(state, unit_named(state, "Unit_ShotGun"), policy.objective)
        self.assertTrue(all(target.name == "Structure_Headquarters" for target in field.best.values()))

    def test_lethal_zone_check_for_every_unit(self):
        # 서브도 적이 이동해서 칠 수 있는 피해 합이 HP 이상인 칸은 치명.
        state = battle([("Unit_Akimbo", (0, 0)), ("Unit_Sniper", (8, 0))], [("Unit_Demon", (4, 7)), ("Unit_Demon", (1, 4))])
        policy = SmartPlayerPolicy()
        sniper = unit_named(state, "Unit_Sniper")
        self.assertEqual(policy.extra_lethal(state, sniper, (4, 4), sniper.hp), 16)   # 16 ≥ 10
        self.assertIsNone(policy.extra_lethal(state, sniper, (8, 0), sniper.hp))

    def test_main_penalty_only_for_main(self):
        state = battle([("Unit_Akimbo", (0, 0)), ("Unit_Sniper", (8, 0))], [("Unit_Demon", (4, 7)), ("Unit_Demon", (1, 4))])
        policy = SmartPlayerPolicy()
        main, sniper = unit_named(state, "Unit_Akimbo"), unit_named(state, "Unit_Sniper")
        self.assertEqual(policy.extra_penalty(state, main, (4, 4), main.hp), 16 * 1000 // 30)
        self.assertEqual(policy.extra_penalty(state, sniper, (4, 4), sniper.hp), 0)

    def test_factory_names(self):
        self.assertIsInstance(make_player_policy("smart"), SmartPlayerPolicy)
        with self.assertRaises(ValueError):
            make_player_policy("nope")

    def test_plays_a_whole_turn(self):
        state = battle([("Unit_Akimbo", (4, 0)), ("Unit_ShotGun", (3, 1))], [("Unit_Demon", (4, 6))], hq=(4, 7))
        policy = SmartPlayerPolicy()
        for _ in range(10):
            action = policy.act(state)
            if isinstance(action, EndTurn):
                break
            state = apply(state, action)
        else:
            self.fail("턴이 끝나지 않았다")


if __name__ == "__main__":
    unittest.main()
