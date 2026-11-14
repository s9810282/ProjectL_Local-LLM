"""시뮬레이터 S-c: 배치 규칙, 플레이어 역할, 단순 탐욕 적, 전투 실행기."""

from __future__ import annotations

from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tests"))
sys.path.insert(0, str(ROOT))

from test_sim_rules import ASSETS, battle, to_enemy_turn, unit_named  # noqa: E402
from sim.analysis import threat_cells  # noqa: E402
from sim.enemy import make_enemy_policy  # noqa: E402
from sim.player import PlayerPolicy, deployment  # noqa: E402
from sim.rules import Attack, EndTurn, Move, apply  # noqa: E402
from sim.run import run_battle  # noqa: E402
from sim.targeting import Side  # noqa: E402


class DeploymentTests(unittest.TestCase):
    def test_subs_front_main_behind(self):
        stage = ASSETS.stage("Combat_LD_P20")
        placed = deployment(stage, ASSETS.player_loadout())
        self.assertEqual(sorted(placed), [0, 1, 2, 3])
        self.assertTrue(all(cell in stage.deployment_cells for cell in placed.values()))
        front = max(placed[i][1] for i in (1, 2, 3))
        self.assertLess(placed[0][1], front)          # 메인은 서브보다 뒤 줄


class PlayerPolicyTests(unittest.TestCase):
    def test_rush_hits_headquarters_when_it_can(self):
        # 메인이 적 사정권 밖일 때(사정권 안이면 메인은 먼저 피한다 — 다음 시험)
        state = battle([("Unit_Akimbo", (4, 1)), ("Unit_ShotGun", (0, 0))], [("Unit_Demon", (0, 8))], hq=(4, 3))
        action = PlayerPolicy("rush").act(state)
        self.assertIsInstance(action, Attack)
        self.assertIn(action.aim, {(4, 3), (5, 3)})

    def test_main_leaves_enemy_reach_first(self):
        state = battle([("Unit_Akimbo", (4, 1)), ("Unit_ShotGun", (0, 0))], [("Unit_Demon", (8, 0))], hq=(4, 3))
        self.assertIn((4, 1), threat_cells(state, Side.ENEMY))
        action = PlayerPolicy("rush").act(state)
        self.assertIsInstance(action, Move)
        self.assertNotIn(action.path[-1], threat_cells(state, Side.ENEMY))

    def test_cautious_does_not_step_into_enemy_reach(self):
        state = battle([("Unit_Akimbo", (0, 0)), ("Unit_ShotGun", (4, 1))], [("Unit_Gravekeeper", (4, 6))], hq=(7, 7))
        policy = PlayerPolicy("cautious")
        danger = threat_cells(state, Side.ENEMY)
        for _ in range(6):
            action = policy.act(state)
            if isinstance(action, EndTurn):
                break
            if isinstance(action, Move):
                self.assertNotIn(action.path[-1], danger)
            state = apply(state, action)

    def test_main_stays_behind_front_line(self):
        state = battle([("Unit_Akimbo", (4, 0)), ("Unit_ShotGun", (4, 1))], [("Unit_Demon", (8, 8))], hq=(4, 7))
        policy = PlayerPolicy("rush")
        for _ in range(8):
            action = policy.act(state)
            if isinstance(action, EndTurn):
                break
            state = apply(state, action)
        main, shotgun = unit_named(state, "Unit_Akimbo"), unit_named(state, "Unit_ShotGun")
        self.assertLessEqual(main.cell[1], max(shotgun.cell[1] - 1, 0))


class GreedyEnemyTests(unittest.TestCase):
    def test_takes_the_kill(self):
        state = to_enemy_turn(battle([("Unit_Akimbo", (0, 0)), ("Unit_Sniper", (4, 3))], [("Unit_Demon", (4, 5))]))
        unit_named(state, "Unit_Sniper").hp = 3
        policy = make_enemy_policy("greedy")
        for _ in range(3):
            action = policy.act(state)
            if isinstance(action, EndTurn):
                break
            state = apply(state, action)
        self.assertFalse(unit_named(state, "Unit_Sniper").alive)

    def test_ends_turn_when_nothing_helps(self):
        # 이동 못 하는 보스는 칠 게 없으면 턴을 끝낸다.
        state = to_enemy_turn(battle([("Unit_Akimbo", (0, 0))], [("Unit_Stage1Boss", (8, 8))]))
        self.assertIsInstance(make_enemy_policy("greedy").act(state), EndTurn)


class RunnerTests(unittest.TestCase):
    def test_battle_is_deterministic_and_finishes(self):
        stage = ASSETS.stage("Combat_LD_P20")
        first, _ = run_battle(stage, ASSETS, PlayerPolicy("rush"), make_enemy_policy("greedy"), seed=0, max_rounds=10)
        second, _ = run_battle(stage, ASSETS, PlayerPolicy("rush"), make_enemy_policy("greedy"), seed=0, max_rounds=10)
        self.assertIn(first.outcome, ("player_win", "enemy_win", "timeout"))
        self.assertEqual((first.outcome, first.rounds, first.player_hp_lost, first.enemy_units_lost),
                         (second.outcome, second.rounds, second.player_hp_lost, second.enemy_units_lost))
        self.assertEqual(first.anomalies, [])

    def test_timeout_counts_rounds(self):
        stage = ASSETS.stage("Combat_LD_P20")

        class Idle:
            name = "idle"

            def act(self, state):
                return EndTurn()

            def holds(self):
                return {}

        record, _ = run_battle(stage, ASSETS, Idle(), Idle(), seed=0, max_rounds=3)
        self.assertEqual((record.outcome, record.rounds, record.first_attack_round), ("timeout", 3, -1))
        self.assertEqual(record.enemy_hold_ratio, 1.0)   # 적은 행동할 수 있었는데 아무것도 안 했다


if __name__ == "__main__":
    unittest.main()
