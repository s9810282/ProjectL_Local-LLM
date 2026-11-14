"""턴 계획 AI T-a: 판 평가 항목마다 손 계산."""

from __future__ import annotations

from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tests"))
sys.path.insert(0, str(ROOT))

from test_sim_rules import ASSETS, battle, unit_named  # noqa: E402
from sim.evaluate import WIN, Evaluator, PlannerConfig  # noqa: E402
from sim.player import deployment  # noqa: E402
from sim.rules import start_battle  # noqa: E402
from sim.state import Outcome, new_battle  # noqa: E402
from sim.targeting import Side  # noqa: E402


class AssetTests(unittest.TestCase):
    def test_p20_start_assets(self):
        # 플레이어: Akimbo 10×30×3 = 900, ShotGun 7×35 = 245, Ballista 13×15 = 195, Cowboy 10×25 = 250 → 1590
        # 적 유닛: Heavy 6×35 = 210, 자폭 Demon 10×20 = 200, Cannon 15×12 = 180, Gravekeeper 12×24 = 288 → 878
        # 본진 값 = 878(시작 적 전력), 증원 8마리 전력 1490 × 0.5 = 745 → 적 자산 2501
        stage = ASSETS.stage("Combat_LD_P20")
        loadout = ASSETS.player_loadout()
        state = start_battle(new_battle(stage, loadout, deployment(stage, loadout)))
        evaluator = Evaluator(state)
        self.assertEqual(evaluator.assets(state, Side.PLAYER), 1590)
        self.assertEqual(evaluator.hq_value, 878)
        self.assertEqual(evaluator.assets(state, Side.ENEMY), 878 + 878 + 745)

    def test_headquarters_and_pool_drop_with_damage(self):
        stage = ASSETS.stage("Combat_LD_P20")
        loadout = ASSETS.player_loadout()
        state = start_battle(new_battle(stage, loadout, deployment(stage, loadout)))
        evaluator = Evaluator(state)
        hq = next(iter(state.structures.values()))
        hq.hp = 15
        self.assertEqual(evaluator.assets(state, Side.ENEMY), 878 + 439 + 745)
        hq.hp = 0                                  # 본진이 무너지면 증원 몫도 사라진다
        self.assertEqual(evaluator.assets(state, Side.ENEMY), 878)


class RiskOpportunityTests(unittest.TestCase):
    def test_risk_of_one_attacker_on_main(self):
        # Demon(공 8) → Akimbo(방 5): 8×95/100 = 7. 메인 손실 = 7 × 공격력 10 × 3 = 210
        state = battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4))])
        self.assertEqual(Evaluator(state).risk(state, Side.PLAYER), 210)

    def test_focus_fire_sums_and_caps_at_full_power(self):
        # Demon 둘이 Sniper(HP 10, 방 5)를 칠 수 있다: 7 + 7 = 14 ≥ 10 → Sniper 전력 전부 5 × 10 = 50.
        # (메인 Akimbo는 멀리 두고, Demon은 메인 900보다 Sniper를 고른다 — 메인에 닿지 않는다)
        state = battle([("Unit_Akimbo", (0, 0)), ("Unit_Sniper", (6, 4))],
                       [("Unit_Demon", (6, 7)), ("Unit_Demon", (8, 6))])
        self.assertEqual(Evaluator(state).risk(state, Side.PLAYER), 50)

    def test_opportunity_is_best_strike_next_turn(self):
        # Akimbo(공 10, 이동 3, 사거리 2) → Demon(방 3): 10×97/100 = 9 → Demon이 잃는 전력 9 × 8 = 72
        state = battle([("Unit_Akimbo", (4, 0))], [("Unit_Demon", (4, 5))])
        gain, penalty = Evaluator(state).opportunity(state, Side.PLAYER)
        self.assertEqual((gain, penalty), (72, 0))

    def test_player_counts_headquarters_as_a_target(self):
        # Cowboy(공 10)가 본진에 닿는다: 본진 값 × 10/30. 본진 값 = 시작 적 전력(Demon 160) → 160 × 10/30
        state = battle([("Unit_Akimbo", (0, 0)), ("Unit_Cowboy", (4, 2))], [("Unit_Demon", (0, 8))], hq=(4, 4))
        evaluator = Evaluator(state)
        target, damage, value = evaluator._best_strike(state, unit_named(state, "Unit_Cowboy"),
                                                       evaluator._targets(state, Side.PLAYER), own=True)
        self.assertEqual((target.name, damage), ("Structure_Headquarters", 10))
        self.assertAlmostEqual(value, 160 * 10 / 30)

    def test_step_penalty_when_nothing_in_reach(self):
        # Akimbo (0,0) → Demon (8,8) 옆 칸까지 15걸음, 이동 3 + (사거리 2 − 1) = 4 → 넘는 걸음 11 × 10 = 110
        state = battle([("Unit_Akimbo", (0, 0))], [("Unit_Demon", (8, 8))])
        self.assertEqual(Evaluator(state).opportunity(state, Side.PLAYER), (0.0, 110.0))

    def test_disarmed_opponent_is_no_threat(self):
        state = battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4))])
        unit_named(state, "Unit_Demon").statuses["disarm"] = 1
        self.assertEqual(Evaluator(state).risk(state, Side.PLAYER), 0)


class TotalTests(unittest.TestCase):
    def test_total_combines_terms(self):
        state = battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4))])
        config = PlannerConfig(fear=0.5, opportunity=0.5)
        parts = Evaluator(state, config).breakdown(state, Side.PLAYER)
        expected = parts["assets"] - parts["opponentAssets"] - 0.5 * parts["risk"] + 0.5 * parts["opportunity"] \
            - parts["stepPenalty"]
        self.assertAlmostEqual(parts["total"], expected)

    def test_outcome_is_infinite(self):
        state = battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4))])
        state.outcome = Outcome.ENEMY_WIN
        evaluator = Evaluator(state)
        self.assertEqual((evaluator.value(state, Side.ENEMY), evaluator.value(state, Side.PLAYER)), (WIN, -WIN))


if __name__ == "__main__":
    unittest.main()
