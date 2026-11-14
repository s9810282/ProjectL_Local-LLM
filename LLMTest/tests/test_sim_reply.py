"""엔진 응수(다음 턴 추정을 엔진으로). 설계 docs/2026-11-13-planner-engine-reply-design.md R1·R6.

기대값은 손 계산이다(유닛 수치는 게임 에셋에서 읽음).
- 자산 손실 = 잃은 HP × 공격력(메인은 × 3).
- 피해 = 공격력 × 상성 × (100 − 방어)% (시뮬레이터 설계 피해 식).
"""

from __future__ import annotations

from pathlib import Path
import random
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tests"))
sys.path.insert(0, str(ROOT))

from test_sim_rules import U, battle, to_enemy_turn, unit_named  # noqa: E402
from sim.analysis import attack_results  # noqa: E402
from sim.assets import PlayerLoadout, TerrainKind  # noqa: E402
from sim.evaluate import Evaluator  # noqa: E402
from sim.reply import (best_replies, combined_loss, engine_opportunity, engine_risk, engine_terms,  # noqa: E402
                       reply_boards)
from sim.rules import KNOWN_TRAITS, unknown_traits  # noqa: E402
from sim.run import unknown_trait_notes  # noqa: E402
from sim.targeting import Side  # noqa: E402

SEED = 7


def stood_at(option, start):
    return option.move.path[-1] if option.move is not None else start


class EngineRiskTests(unittest.TestCase):
    def test_cliff_knockback_costs_the_whole_unit(self):
        # Gravekeeper(기병, 공 12, 넉백 1)가 (4,3)으로 와서 Cowboy(4,4)를 위(+y)로 밀면 (4,5) 낭떠러지로 떨어진다.
        # 엔진: Cowboy HP 25 전부 × 공 10 = 250. 표: 피해만 12 × 97% = 11 → 110.
        state = battle([("Unit_Akimbo", (14, 14)), ("Unit_Cowboy", (4, 4))], [("Unit_Gravekeeper", (4, 1))],
                       size=(15, 15), terrain=[(TerrainKind.CLIFF, (4, 5))])
        evaluator = Evaluator(state)
        self.assertEqual(engine_risk(state, Side.PLAYER, evaluator, SEED), 250.0)
        self.assertEqual(evaluator.risk(state, Side.PLAYER), 110.0)

    def test_solo_shield_blocks_what_the_table_counts(self):
        # Demon_SoloAction(보호막 10, 반경 1에 아무도 없을 때)을 Akimbo(공 10, 사거리 2, 이동 3)가 옆까지는 못 온다.
        # 두 칸 떨어져 치면 10 × 95% = 9가 보호막에 다 막힌다 → 엔진 0. 표는 9 × 공 10 = 90.
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 3))], [("Unit_Demon_SoloAction", (4, 8))]))
        evaluator = Evaluator(state)
        self.assertEqual(engine_risk(state, Side.ENEMY, evaluator, SEED), 0.0)
        self.assertEqual(evaluator.risk(state, Side.ENEMY), 90.0)

    def test_two_attackers_on_one_target_are_capped_by_its_hp(self):
        # Gravekeeper 둘이 각각 Cowboy(HP 15)에 11씩: 22 → HP 15로 잘려 15 × 10 = 150.
        state = battle([("Unit_Akimbo", (14, 14)), ("Unit_Cowboy", (4, 4))],
                       [("Unit_Gravekeeper", (4, 1)), ("Unit_Gravekeeper", (4, 7))], size=(15, 15))
        cowboy = unit_named(state, "Unit_Cowboy")
        cowboy.hp = 15
        evaluator = Evaluator(state)
        terms = engine_terms(state, Side.PLAYER, evaluator, SEED)
        self.assertEqual([r.lost for r in terms.risk_replies], [{("unit", cowboy.uid): 11}] * 2)
        self.assertEqual(terms.risk, 150.0)
        self.assertEqual(combined_loss(state, [{("unit", cowboy.uid): 11}] * 2, evaluator), 150.0)


class EngineOpportunityTests(unittest.TestCase):
    def test_kill_chain_counts_the_second_hit(self):
        # Akimbo(처치 연쇄)가 HP 5 Demon을 잡으면(10 × 97% = 9) 남은 Demon에 한 번 더 9.
        # 엔진: 5 × 8 + 9 × 8 = 112. 표: 첫 대상만 5 × 8 = 40.
        state = battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 3)), ("Unit_Demon", (8, 8))])
        state.units[next(u.uid for u in state.units.values() if u.cell == (4, 3))].hp = 5
        evaluator = Evaluator(state)
        self.assertEqual(engine_opportunity(state, Side.PLAYER, evaluator, SEED), (112.0, 0.0))
        self.assertEqual(evaluator.opportunity(state, Side.PLAYER), (40.0, 0.0))


class ReplyChoiceTests(unittest.TestCase):
    def test_opponent_avoids_a_self_destruct_kill_next_to_itself(self):
        # 우리(적) Demon_SelfDestruct HP 5. Akimbo(메인)가 옆에서 잡으면 폭발 10 × 50% = 5를 맞아
        # 순이득 5 × 10 − 5 × 10 × 3 = −100. 두 칸 떨어져 잡으면 +50 → 그것을 고른다.
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon_SelfDestruct", (4, 4))]))
        demon = unit_named(state, "Unit_Demon_SelfDestruct")
        demon.hp = 5
        evaluator = Evaluator(state)
        risk_board, _ = reply_boards(state, Side.ENEMY, SEED)
        akimbo = unit_named(risk_board, "Unit_Akimbo")
        adjacent = []
        for option, after in attack_results(risk_board, akimbo):
            cell = stood_at(option, akimbo.cell)
            if max(abs(cell[0] - demon.cell[0]), abs(cell[1] - demon.cell[1])) == 1 and not after.units[demon.uid].alive:
                gain = (evaluator.assets(risk_board, Side.ENEMY) - evaluator.assets(after, Side.ENEMY)) - \
                       (evaluator.assets(risk_board, Side.PLAYER) - evaluator.assets(after, Side.PLAYER))
                adjacent.append(gain)
        self.assertTrue(adjacent)
        self.assertTrue(all(g == -100.0 for g in adjacent))
        [reply] = best_replies(risk_board, Side.PLAYER, evaluator)
        cell = stood_at(reply.option, akimbo.cell)
        self.assertEqual(max(abs(cell[0] - demon.cell[0]), abs(cell[1] - demon.cell[1])), 2)
        self.assertEqual((reply.gain, reply.lost), (50.0, {("unit", demon.uid): 5}))
        self.assertEqual(engine_risk(state, Side.ENEMY, evaluator, SEED), 50.0)

    def test_unit_whose_options_gain_nothing_is_left_out(self):
        # 공격 후보는 있지만 모두 보호막에 막혀 순이득 0인 Akimbo는 응수 목록에서 빠진다(아무것도 안 한다고 봄).
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 3))], [("Unit_Demon_SoloAction", (4, 8))]))
        risk_board, _ = reply_boards(state, Side.ENEMY, SEED)
        self.assertTrue(attack_results(risk_board, unit_named(risk_board, "Unit_Akimbo")))
        self.assertEqual(best_replies(risk_board, Side.PLAYER, Evaluator(state)), [])


class BoardTests(unittest.TestCase):
    def test_turn_passing_applies_our_turn_end_effects(self):
        # 우리(적) UndeadHeavy(재생 5, 우리 턴 끝마다)가 HP 20이면 위험 판에서는 25로 맞는다.
        state = to_enemy_turn(battle([("Unit_Akimbo", (0, 0))], [("Unit_UndeadHeavy", (8, 8))]))
        unit_named(state, "Unit_UndeadHeavy").hp = 20
        risk_board, chance_board = reply_boards(state, Side.ENEMY, SEED)
        self.assertEqual((risk_board.turn_side, chance_board.turn_side), (Side.PLAYER, Side.ENEMY))
        self.assertEqual(unit_named(risk_board, "Unit_UndeadHeavy").hp, 25)

    def test_same_board_and_seed_give_the_same_values_whatever_the_live_rng(self):
        # 처치 연쇄 두 번째 대상이 무작위(Demon 둘 중 하나)인 판.
        state = battle([("Unit_Akimbo", (4, 1))],
                       [("Unit_Demon", (4, 3)), ("Unit_Demon", (8, 8)), ("Unit_Demon", (0, 8))])
        state.units[next(u.uid for u in state.units.values() if u.cell == (4, 3))].hp = 5
        evaluator = Evaluator(state)
        first = engine_terms(state, Side.PLAYER, evaluator, SEED)
        state.rng = random.Random(12345)
        second = engine_terms(state, Side.PLAYER, Evaluator(state), SEED)
        self.assertEqual((first.risk, first.opportunity, first.step_penalty),
                         (second.risk, second.opportunity, second.step_penalty))
        self.assertEqual(first.opportunity_replies, second.opportunity_replies)

    def test_rejects_a_board_that_is_not_our_turn(self):
        state = battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4))])
        with self.assertRaises(ValueError):
            reply_boards(state, Side.ENEMY, SEED)


class UnknownTraitTests(unittest.TestCase):
    def test_lists_only_traits_the_engine_does_not_handle(self):
        self.assertEqual(unknown_traits([U("Unit_Stage1Boss"), U("Unit_Demon"), U("Unit_Gravekeeper")]),
                         [("Unit_Stage1Boss", "Roar")])
        self.assertIn("Knockback", KNOWN_TRAITS)

    def test_battle_notes_cover_enemies_and_the_player_loadout(self):
        stage = battle([("Unit_Akimbo", (4, 1))], [("Unit_Stage1Boss", (4, 6))]).board.stage
        loadout = PlayerLoadout("T", U("Unit_Akimbo"), (U("Unit_CactusCannon"),), 2)
        notes = unknown_trait_notes(stage, loadout)
        self.assertEqual(len(notes), 2)
        self.assertTrue(any("Unit_CactusCannon(ThornField)" in n for n in notes))
        self.assertTrue(any("Unit_Stage1Boss(Roar)" in n for n in notes))


if __name__ == "__main__":
    unittest.main()
