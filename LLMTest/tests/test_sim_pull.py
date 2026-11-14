"""시뮬레이터: 끌어오기(Pull, Cowboy).

게임 원본: Domain/Traits/Rules/PullTraitRule.cs, Application/Combat/PullAttackCalculator.cs.
Cowboy = 십자 직선 한 칸 고르기, 기본 사거리 1 + 끌어오기 추가 2 = 고를 수 있는 사거리 3, 공격력 10.
"""

from __future__ import annotations

from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tests"))
sys.path.insert(0, str(ROOT))

from test_sim_rules import battle, unit_named  # noqa: E402
from sim.rules import (Attack, RuleError, apply, attack_range, legal_attacks, resolve_attack,  # noqa: E402
                       selection_range)


def pull(state, target_cell):
    cowboy = unit_named(state, "Unit_Cowboy")
    return apply(state, Attack(cowboy.uid, target_cell))


class PullTests(unittest.TestCase):
    def test_selection_range_adds_the_extra_cells(self):
        state = battle([("Unit_Cowboy", (4, 1))], [("Unit_Demon", (4, 4))])
        cowboy = unit_named(state, "Unit_Cowboy")
        self.assertEqual((attack_range(state, cowboy), selection_range(state, cowboy)), (1, 3))

    def test_pulls_enemy_to_basic_range_then_hits(self):
        # Demon (4,4) → (4,3) → (4,2)(Cowboy 옆, 기본 사거리 1 경계). 피해 10 × (100 − 방어 3)% = 9 → HP 20 → 11.
        state = battle([("Unit_Cowboy", (4, 1))], [("Unit_Demon", (4, 4))])
        after = pull(state, (4, 4))
        demon = unit_named(after, "Unit_Demon")
        self.assertEqual((demon.cell, demon.hp), ((4, 2), 11))
        self.assertEqual([e["type"] for e in after.log if e["type"] in ("pull", "attack")], ["pull", "attack"])

    def test_enemy_already_in_basic_range_is_a_normal_attack(self):
        state = battle([("Unit_Cowboy", (4, 1))], [("Unit_Demon", (4, 2))])
        plan = resolve_attack(state, unit_named(state, "Unit_Cowboy"), (4, 2))
        self.assertIsNone(plan.pull)
        self.assertEqual(unit_named(pull(state, (4, 2)), "Unit_Demon").hp, 11)

    def test_blocked_path_rejects_the_attack(self):
        # 끌어올 길 (4,3)에 다른 유닛이 있으면 공격 자체가 거절된다(PullPathBlocked).
        state = battle([("Unit_Cowboy", (4, 1))], [("Unit_Demon", (4, 4)), ("Unit_Gravekeeper", (4, 3))])
        with self.assertRaises(RuleError):
            resolve_attack(state, unit_named(state, "Unit_Cowboy"), (4, 4))
        aims = {a.aim for a, _ in legal_attacks(state, unit_named(state, "Unit_Cowboy"))}
        self.assertNotIn((4, 4), aims)

    def test_target_falls_into_a_cliff_on_the_path_and_takes_no_hit(self):
        state = battle([("Unit_Cowboy", (4, 1))], [("Unit_Demon", (4, 4)), ("Unit_Gravekeeper", (8, 8))])
        state.cliffs = frozenset({(4, 3)})
        after = pull(state, (4, 4))
        demon = unit_named(after, "Unit_Demon")
        self.assertFalse(demon.alive)
        self.assertIn("fell", demon.statuses)
        self.assertFalse(any(e["type"] == "attack" and e["hits"] for e in after.log))

    def test_pulls_an_ally_without_damage(self):
        state = battle([("Unit_Cowboy", (4, 1)), ("Unit_Akimbo", (4, 4))], [("Unit_Demon", (8, 8))])
        akimbo = unit_named(state, "Unit_Akimbo")
        hp = akimbo.hp
        aims = {a.aim for a, _ in legal_attacks(state, unit_named(state, "Unit_Cowboy"))}
        self.assertIn((4, 4), aims)                       # 아군 끌어오기도 쓸모 있는 행동
        moved = unit_named(pull(state, (4, 4)), "Unit_Akimbo")
        self.assertEqual((moved.cell, moved.hp), ((4, 2), hp))

    def test_ally_in_basic_range_has_nothing_to_pull(self):
        state = battle([("Unit_Cowboy", (4, 1)), ("Unit_Akimbo", (4, 2))], [("Unit_Demon", (8, 8))])
        with self.assertRaises(RuleError):
            resolve_attack(state, unit_named(state, "Unit_Cowboy"), (4, 2))

    def test_forced_move_immune_target_rejects(self):
        state = battle([("Unit_Cowboy", (4, 1))], [("Unit_Stage1Boss", (4, 4))])
        with self.assertRaises(RuleError):
            resolve_attack(state, unit_named(state, "Unit_Cowboy"), (4, 4))

    def test_structure_only_inside_basic_range(self):
        far = battle([("Unit_Cowboy", (4, 1))], [("Unit_Demon", (8, 8))], hq=(4, 3))
        with self.assertRaises(RuleError):              # 본부 칸 (4,3)은 기본 사거리 1 밖
            resolve_attack(far, unit_named(far, "Unit_Cowboy"), (4, 3))
        near = battle([("Unit_Cowboy", (4, 1))], [("Unit_Demon", (8, 8))], hq=(4, 2))
        plan = resolve_attack(near, unit_named(near, "Unit_Cowboy"), (4, 2))
        self.assertIsNone(plan.pull)
        self.assertEqual([h.kind for h in plan.hits], ["structure"])

    def test_weak_ground_on_the_path_counts_an_entry(self):
        # 약한 지반 (4,3) 한도 1: 끌려 지나가면 명령 끝에 무너져 낭떠러지가 된다. Demon은 (4,2)에 있어 떨어지지 않는다.
        state = battle([("Unit_Cowboy", (4, 1))], [("Unit_Demon", (4, 4))])
        state.weak_entries[(4, 3)] = 0
        state.board.weak_threshold[(4, 3)] = 1
        after = pull(state, (4, 4))
        self.assertIn((4, 3), after.cliffs)
        self.assertTrue(unit_named(after, "Unit_Demon").alive)


if __name__ == "__main__":
    unittest.main()
