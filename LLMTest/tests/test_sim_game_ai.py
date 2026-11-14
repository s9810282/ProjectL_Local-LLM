"""시뮬레이터 S-d: 게임 적 AI(Utility 세션) 이식. 부품마다 게임 규칙을 확인한다."""

from __future__ import annotations

from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tests"))
sys.path.insert(0, str(ROOT))

from test_sim_rules import battle, to_enemy_turn, unit_named  # noqa: E402
from sim.game_ai import (EngagementField, GameAIPolicy, attack_candidates, precise_benefit,  # noqa: E402
                               rough_benefit, threat_field)
from sim.rules import Attack, EndTurn, Move, apply  # noqa: E402
from sim.targeting import Side  # noqa: E402


class ThreatFieldTests(unittest.TestCase):
    def test_cell_keeps_the_strongest_single_hit_not_the_sum(self):
        # (4,4)에는 Akimbo(공 10, 사거리 2)와 Sniper(공 5, 사거리 4)가 모두 닿는다. 게임은 가장 센 10만 남긴다.
        state = battle([("Unit_Akimbo", (4, 2)), ("Unit_Sniper", (0, 0))], [("Unit_Demon", (8, 8))])
        self.assertEqual(threat_field(state)[(4, 4)], 10)

    def test_only_current_positions_count(self):
        # Akimbo는 이동력 3 + 사거리 2지만 위협 필드는 지금 자리에서만 본다.
        state = battle([("Unit_Akimbo", (0, 0))], [("Unit_Demon", (8, 8))])
        self.assertNotIn((4, 0), threat_field(state))


class EngagementFieldTests(unittest.TestCase):
    def test_passing_an_ally_costs_three(self):
        # Demon (0,4) → 목표 Akimbo (4,4). 바로 가면 3칸이지만 (1,4)에 아군이 있어, 그 칸을 지나면 1+3+1 = 5,
        # 돌아가면 (0,5)→(1,5)→(2,5)→(3,5) 4칸. 그래서 비용은 4.
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 4))], [("Unit_Demon", (0, 4)), ("Unit_Demon", (1, 4))]))
        actor = min((u for u in state.living() if u.name == "Unit_Demon"), key=lambda u: u.cell)
        self.assertEqual(EngagementField(state, actor).costs[(0, 4)], 4)
        open_state = to_enemy_turn(battle([("Unit_Akimbo", (4, 4))], [("Unit_Demon", (0, 4))]))
        self.assertEqual(EngagementField(open_state, unit_named(open_state, "Unit_Demon")).costs[(0, 4)], 3)

    def test_next_turn_needs_cost_within_move_range(self):
        # Demon 이동력 3. 목표 Akimbo(HP 30, 방어 무시 공통 피해 8 → 266‰)
        state = to_enemy_turn(battle([("Unit_Akimbo", (5, 4))], [("Unit_Demon", (0, 4))]))
        field = EngagementField(state, unit_named(state, "Unit_Demon"))
        self.assertEqual(field.costs[(0, 4)], 4)
        self.assertEqual(field.next_turn((0, 4)), 0)          # 4 > 3
        self.assertEqual(field.next_turn((1, 4)), 266)        # 3 ≤ 3


class CandidateAndAssessmentTests(unittest.TestCase):
    def test_kill_bonus_only_in_precise_assessment(self):
        # Demon(공 8) → Sniper(HP 3): 정밀 = 3/10 → 300‰ + 처치 2000, 어림 = 300‰만
        state = to_enemy_turn(battle([("Unit_Akimbo", (0, 0)), ("Unit_Sniper", (4, 3))], [("Unit_Demon", (4, 4))]))
        unit_named(state, "Unit_Sniper").hp = 3
        demon = unit_named(state, "Unit_Demon")
        after = apply(state, Attack(demon.uid, (4, 3)))
        self.assertEqual(precise_benefit(state, after), 2300)
        self.assertEqual(rough_benefit(state, demon, (4, 3)), 300)

    def test_attack_candidates_follow_target_order_and_limit(self):
        state = to_enemy_turn(battle([("Unit_Akimbo", (3, 3)), ("Unit_Sniper", (4, 3)), ("Unit_ShotGun", (5, 3))],
                                     [("Unit_Demon", (4, 4))]))
        aims = attack_candidates(state, unit_named(state, "Unit_Demon"))
        self.assertEqual(aims, [(3, 3), (4, 3), (5, 3)])     # 대상 번호(배치 순서) 순


def summed_threat(state, cell):
    """플레이어 유닛마다 따로 만든 위협 필드를 더한 값(몰아치기). 게임은 이 합이 아니라 최댓값만 본다."""
    total = 0
    for player in state.living(Side.PLAYER):
        alone = state.clone()
        for other in alone.living(Side.PLAYER):
            if other.uid != player.uid:
                other.hp = 0
        total += threat_field(alone).get(cell, 0)
    return total


class DecisionTests(unittest.TestCase):
    def test_unit_that_can_attack_acts_first(self):
        # 번호가 앞선 Gravekeeper는 칠 게 없고, 뒤의 Demon은 지금 칠 수 있다 → Demon이 먼저 결정한다.
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 3))], [("Unit_Gravekeeper", (0, 8)), ("Unit_Demon", (4, 4))]))
        action = GameAIPolicy().act(state)
        self.assertEqual(state.units[action.uid].name, "Unit_Demon")

    def test_rough_followup_beats_attacking_now(self):
        # 게임 절차 그대로: 이동 후보의 어림 후속 공격은 방어를 빼지 않아(Akimbo 8/30 = 266‰) 지금 공격의 정밀 값
        # (방어 5 → 7/30 = 233‰)보다 크다. 상위 2개만 정밀로 다시 매기므로, 나머지 어림 이동이 이긴다 → 옆 칸으로 옮긴 뒤 친다.
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 3))], [("Unit_Demon", (4, 4))]))
        policy = GameAIPolicy()
        first = policy.act(state)
        self.assertIsInstance(first, Move)
        state = apply(state, first)
        self.assertIsInstance(policy.act(state), Attack)

    def test_blind_to_focus_fire(self):
        # F04b를 판 위에: Gravekeeper(HP 12)의 자리에서 가장 센 한 방은 10 < 12라 안전하다고 보지만,
        # 고른 칸에 닿는 공격을 합치면 12 이상이다(몰아치기면 죽는다).
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 3)), ("Unit_ShotGun", (5, 3)), ("Unit_Sniper", (4, 0))],
                                     [("Unit_Gravekeeper", (4, 4))]))
        keeper = unit_named(state, "Unit_Gravekeeper")
        keeper.hp = 12
        action = GameAIPolicy().act(state)
        end = action.path[-1] if isinstance(action, Move) else keeper.cell
        self.assertLess(threat_field(state).get(end, 0), 12)
        self.assertGreaterEqual(summed_threat(state, end), 12)

    def test_ends_turn_when_no_option_beats_hold(self):
        state = to_enemy_turn(battle([("Unit_Akimbo", (0, 0))], [("Unit_Stage1Boss", (8, 8))]))
        self.assertIsInstance(GameAIPolicy().act(state), EndTurn)

    def test_move_is_a_decision_of_its_own(self):
        # 이동 뒤 공격은 결정 둘: 먼저 이동, 그다음 결정에서 공격.
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4))]))
        policy = GameAIPolicy()
        first = policy.act(state)
        self.assertIsInstance(first, Move)
        state = apply(state, first)
        self.assertIsInstance(policy.act(state), Attack)
        self.assertEqual(policy.act(apply(state, policy.act(state))).__class__, EndTurn)


if __name__ == "__main__":
    unittest.main()
