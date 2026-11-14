"""턴 계획 AI T-b: 빔 탐색. R-b: 2단 평가(설계 docs/2026-11-13-planner-engine-reply-design.md R3).
D-a: 빔 다양성(설계 docs/2026-11-13-planner-beam-diversity-design.md D1·D2)."""

from __future__ import annotations

from pathlib import Path
import random
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tests"))
sys.path.insert(0, str(ROOT))

from test_sim_rules import battle, to_enemy_turn, unit_named  # noqa: E402
from sim.assets import TerrainKind  # noqa: E402
from sim.evaluate import Evaluator, PlannerConfig  # noqa: E402
from sim.planner import PlannerPolicy, _Node, planner_from_name, prediction_seed, result_key, signature  # noqa: E402
from sim.rules import Attack, EndTurn, apply, legal_actions  # noqa: E402
from sim.state import Outcome  # noqa: E402
from sim.targeting import Side  # noqa: E402


def play_turn(policy, state, limit=20):
    actions = []
    for _ in range(limit):
        action = policy.act(state)
        if isinstance(action, EndTurn):
            return state, actions
        assert action in legal_actions(state, useful_only=False), f"규칙상 할 수 없는 행동: {action}"
        actions.append(action)
        state = apply(state, action)
    raise AssertionError("턴이 끝나지 않았다")


class PlannerTests(unittest.TestCase):
    def test_focus_fire_to_kill(self):
        # Demon(HP 15, 방 3): Akimbo 10×97/100 = 9, ShotGun 7×97/100 = 6 → 둘이 함께 쳐야 15로 잡힌다.
        state = battle([("Unit_Akimbo", (3, 2)), ("Unit_ShotGun", (5, 2))], [("Unit_Demon", (4, 4))])
        unit_named(state, "Unit_Demon").hp = 15
        after, actions = play_turn(PlannerPolicy(Side.PLAYER, PlannerConfig(beam=4)), state)
        self.assertFalse(unit_named(after, "Unit_Demon").alive)
        attackers = {after.units[a.uid].name for a in actions if isinstance(a, Attack)}
        self.assertEqual(attackers, {"Unit_Akimbo", "Unit_ShotGun"})

    def test_same_state_same_plan(self):
        state = battle([("Unit_Akimbo", (3, 1)), ("Unit_ShotGun", (5, 1))], [("Unit_Demon", (4, 6))], hq=(4, 7))
        first = play_turn(PlannerPolicy(Side.PLAYER), state)[1]
        second = play_turn(PlannerPolicy(Side.PLAYER), state)[1]
        self.assertEqual(first, second)

    def test_plan_does_not_peek_at_the_real_random_state(self):
        # 계획은 고정 씨앗으로 하므로 실제 난수 상태가 달라도 같은 계획이다.
        state = battle([("Unit_Akimbo", (3, 1)), ("Unit_ShotGun", (5, 1))], [("Unit_Demon", (4, 6))], hq=(4, 7))
        other = state.clone()
        other.rng = random.Random(12345)
        policy_a, policy_b = PlannerPolicy(Side.PLAYER), PlannerPolicy(Side.PLAYER)
        self.assertEqual(policy_a.act(state), policy_b.act(other))

    def test_replans_when_the_board_differs_from_the_plan(self):
        state = battle([("Unit_Akimbo", (3, 1)), ("Unit_ShotGun", (5, 1))], [("Unit_Demon", (4, 6))], hq=(4, 7))
        policy = PlannerPolicy(Side.PLAYER)
        first = policy.act(state)
        changed = apply(state, first)
        unit_named(changed, "Unit_Demon").hp -= 5              # 예측과 다른 판
        policy.act(changed)
        self.assertEqual(policy.stats.replans, 1)

    def test_beam_one_works_and_enemy_side_works(self):
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 4)), ("Unit_Gravekeeper", (6, 6))]))
        after, actions = play_turn(PlannerPolicy(Side.ENEMY, PlannerConfig(beam=1)), state)
        self.assertTrue(actions)
        self.assertLess(unit_named(after, "Unit_Akimbo").hp, 30)

    def test_does_not_expand_finished_boards(self):
        # 자폭 Demon(HP 1)이 메인 옆에 있다. 그 Demon을 치면 자폭(5)으로 메인(HP 3)이 죽어 계획 안에서 전투가 끝난다.
        # 끝난 판을 더 펼치면 엔진이 "이미 끝난 전투" 오류를 낸다 → 계획은 그 판을 펼치지 않고 끝까지 간다.
        state = battle([("Unit_Akimbo", (4, 3)), ("Unit_Sniper", (4, 0)), ("Unit_ShotGun", (0, 0))],
                       [("Unit_Demon_SelfDestruct", (4, 4)), ("Unit_Demon", (8, 8))])
        unit_named(state, "Unit_Akimbo").hp = 3
        unit_named(state, "Unit_Demon_SelfDestruct").hp = 1
        # 빔을 크게 잡아 지는 판도 다음 단계 후보로 남게 한다(작은 빔이면 점수가 가장 낮아 자연히 빠진다).
        after, actions = play_turn(PlannerPolicy(Side.PLAYER, PlannerConfig(beam=50)), state)
        self.assertIsNot(after.outcome, Outcome.ENEMY_WIN)   # 계획은 메인을 죽이는 자폭을 고르지 않는다

    def test_names_set_beam_fear_and_opportunity(self):
        policy = planner_from_name(Side.ENEMY, "plan8:f0.8:o0.3")
        self.assertEqual((policy.config.beam, policy.config.fear, policy.config.opportunity), (8, 0.8, 0.3))
        self.assertEqual(policy.name, "enemy-plan8:f0.8:o0.3")
        with self.assertRaises(ValueError):
            planner_from_name(Side.ENEMY, "plan4:x1")

    def test_signature_ignores_random_state(self):
        state = battle([("Unit_Akimbo", (3, 1))], [("Unit_Demon", (4, 6))])
        other = state.clone()
        other.rng = random.Random(1)
        self.assertEqual(signature(state), signature(other))


class RerankTests(unittest.TestCase):
    @staticmethod
    def cliff_board():
        # 7×7, 낭떠러지 (4,4). 메인 Sniper(공 5), Akimbo(공 10, HP 30, 이동 3, 사거리 2).
        # 적 Gravekeeper(공 12, HP 24, 이동 4, 넉백 1), Demon(공 8, HP 20).
        # Akimbo가 이번 턴에 Gravekeeper를 칠 수 있는 칸은 (4,3)·(3,4)뿐이고 둘 다 낭떠러지 옆이다.
        return battle([("Unit_Sniper", (0, 0)), ("Unit_Akimbo", (1, 3))],
                      [("Unit_Gravekeeper", (6, 2)), ("Unit_Demon", (5, 6))],
                      size=(7, 7), terrain=[(TerrainKind.CLIFF, (4, 4))])

    def test_rerank_gives_up_the_attack_from_beside_the_cliff(self):
        state = self.cliff_board()
        # 빠른 V: 둘 다 치는 계획 = 450 − 292 − 0.5 × 180(Gravekeeper 11×10 + Demon 7×10) + 0.5 × 156 = 146
        #         Sniper만 치는 계획 = 450 − 400 − 0 + 0.5 × 156 = 128 → 빠른 V는 둘 다 친다(Akimbo는 (4,3)).
        fast = planner_from_name(Side.PLAYER, "plan4:r0")
        after_fast, _ = play_turn(fast, state)
        self.assertEqual(unit_named(after_fast, "Unit_Akimbo").cell, (4, 3))
        self.assertEqual(fast.last_plan["value"], 146.0)
        self.assertNotIn("deep", fast.last_plan)
        self.assertEqual((fast.stats.reranks, fast.stats.deep_evals, fast.stats.flips), (0, 0, 0))
        # 깊은 평가: 그 판에서 Gravekeeper가 (4,2)로 와 위로 치면 Akimbo가 (4,4)로 떨어진다 → 위험 300(Akimbo 전부)
        #           = 450 − 292 − 0.5 × 300 + 0.5 × 132(Gravekeeper HP 11로 잘림) = 74 < 128.
        evaluator = Evaluator(state)
        seed = prediction_seed(state, Side.PLAYER)
        deep_of_fast = evaluator.deep_breakdown(after_fast, Side.PLAYER, seed)
        self.assertEqual((deep_of_fast["risk"], deep_of_fast["total"]), (300.0, 74.0))

        deep = planner_from_name(Side.PLAYER, "plan4")
        after_deep, actions = play_turn(deep, state)
        self.assertEqual(unit_named(after_deep, "Unit_Akimbo").cell, (1, 3))
        self.assertEqual({after_deep.units[a.uid].name for a in actions if isinstance(a, Attack)}, {"Unit_Sniper"})
        plan = deep.last_plan
        self.assertEqual((plan["value"], plan["chosenValue"], plan["deep"]["total"], plan["flipped"]),
                         (146.0, 128.0, 128.0, True))
        self.assertEqual((deep.stats.reranks, deep.stats.deep_evals, deep.stats.flips), (1, 4, 1))

    def test_rerank_of_one_keeps_the_fast_best(self):
        # 후보가 하나뿐이면 다시 매길 것이 없다 → 옛 동작과 같다.
        state = self.cliff_board()
        policy = planner_from_name(Side.PLAYER, "plan4:r1")
        after, _ = play_turn(policy, state)
        self.assertEqual(unit_named(after, "Unit_Akimbo").cell, (4, 3))
        self.assertEqual(policy.stats.reranks, 0)

    def test_rerank_is_deterministic_and_ignores_the_live_random_state(self):
        state = self.cliff_board()
        other = state.clone()
        other.rng = random.Random(12345)
        first = play_turn(planner_from_name(Side.PLAYER, "plan4"), state)[1]
        second = play_turn(planner_from_name(Side.PLAYER, "plan4"), other)[1]
        self.assertEqual(first, second)

    def test_prediction_seed_is_a_plain_number_formula(self):
        # 문자열 hash를 쓰지 않아 프로세스마다 같다(Q-R4). (라운드 × 2 + 진영) × 1000003 + 다음 ID.
        state = self.cliff_board()
        self.assertEqual(prediction_seed(state, Side.PLAYER), 2 * 1_000_003 + state.next_id)
        self.assertEqual(prediction_seed(state, Side.ENEMY), 3 * 1_000_003 + state.next_id)

    def test_names_set_rerank(self):
        self.assertEqual(planner_from_name(Side.ENEMY, "plan4").config.rerank_count, 4)
        self.assertEqual(planner_from_name(Side.ENEMY, "plan8:f0.8:r2").config.rerank_count, 2)
        off = planner_from_name(Side.ENEMY, "plan4:r0")
        self.assertEqual((off.config.rerank_count, off.name), (0, "enemy-plan4:r0"))
        with self.assertRaises(ValueError):
            planner_from_name(Side.ENEMY, "plan4:r-1")


class DiversityTests(unittest.TestCase):
    board = staticmethod(RerankTests.cliff_board)

    def test_result_key_ignores_only_our_own_cells(self):
        state = self.board()
        moved_own = state.clone()
        unit_named(moved_own, "Unit_Akimbo").cell = (2, 3)
        moved_enemy = state.clone()
        unit_named(moved_enemy, "Unit_Demon").cell = (5, 5)
        hurt_own = state.clone()
        unit_named(hurt_own, "Unit_Akimbo").hp -= 1
        # 플레이어 쪽에서 보면 아군 칸만 다른 판은 같은 결과, 적 칸·아군 HP가 다르면 다른 결과다.
        self.assertEqual(result_key(state, Side.PLAYER), result_key(moved_own, Side.PLAYER))
        self.assertNotEqual(result_key(state, Side.PLAYER), result_key(moved_enemy, Side.PLAYER))
        self.assertNotEqual(result_key(state, Side.PLAYER), result_key(hurt_own, Side.PLAYER))
        # 적 쪽에서 보면 반대다(진영 공통).
        self.assertNotEqual(result_key(state, Side.ENEMY), result_key(moved_own, Side.ENEMY))
        self.assertEqual(result_key(state, Side.ENEMY), result_key(moved_enemy, Side.ENEMY))
        # 서명은 둘 다 다르다고 본다.
        self.assertNotEqual(signature(state), signature(moved_own))

    def test_keep_takes_at_most_n_per_result_in_value_order(self):
        state = self.board()
        a = _Node(state, frozenset(), [], 30.0)
        b_state = state.clone()
        unit_named(b_state, "Unit_Akimbo").cell = (2, 3)       # a와 같은 결과(자리 변형)
        b = _Node(b_state, frozenset(), [], 20.0)
        c_state = state.clone()
        unit_named(c_state, "Unit_Demon").hp -= 5              # 다른 결과
        c = _Node(c_state, frozenset(), [], 10.0)
        ranked = [a, b, c]
        keep = lambda name: planner_from_name(Side.PLAYER, name)._keep(ranked)
        self.assertEqual(keep("plan2"), [a, b])            # 끔: 빠른 V 상위 2개
        self.assertEqual(keep("plan2:d1"), [a, c])         # 결과마다 하나: b는 a와 같은 결과라 빠진다
        self.assertEqual(keep("plan2:d2"), [a, b])         # 결과마다 둘까지
        self.assertEqual(keep("plan4:d1"), [a, c])         # 빔이 남아도 같은 결과는 더 넣지 않는다

    def test_d0_is_the_old_plan(self):
        state = self.board()
        old = planner_from_name(Side.PLAYER, "plan4")
        off = planner_from_name(Side.PLAYER, "plan4:d0")
        self.assertEqual(play_turn(old, state)[1], play_turn(off, state)[1])
        self.assertEqual(old.last_plan, off.last_plan)
        self.assertEqual((old.stats.beam_nodes, old.stats.beam_results), (off.stats.beam_nodes, off.stats.beam_results))

    def test_d1_keeps_only_different_results_in_every_beam_step(self):
        state = self.board()
        off = planner_from_name(Side.PLAYER, "plan4:r0")
        play_turn(off, state)
        # 이 판은 끄면 빔에 같은 결과의 자리 변형이 들어간다(그래서 거를 것이 있다).
        self.assertLess(off.stats.beam_results, off.stats.beam_nodes)
        on = planner_from_name(Side.PLAYER, "plan4:r0:d1")
        play_turn(on, state)
        self.assertGreater(on.stats.beam_steps, 0)
        self.assertEqual(on.stats.beam_results, on.stats.beam_nodes)

    def test_d1_is_deterministic_and_ignores_the_live_random_state(self):
        state = self.board()
        other = state.clone()
        other.rng = random.Random(12345)
        first = play_turn(planner_from_name(Side.PLAYER, "plan4:d1"), state)[1]
        second = play_turn(planner_from_name(Side.PLAYER, "plan4:d1"), other)[1]
        self.assertEqual(first, second)

    def test_enemy_side_works(self):
        state = to_enemy_turn(self.board())
        policy = planner_from_name(Side.ENEMY, "plan4:d1")
        after, _ = play_turn(policy, state)
        self.assertIs(after.turn_side, Side.ENEMY)
        self.assertEqual(policy.stats.beam_results, policy.stats.beam_nodes)

    def test_names_set_distinct(self):
        self.assertEqual(planner_from_name(Side.ENEMY, "plan4").config.distinct, 0)
        on = planner_from_name(Side.ENEMY, "plan4:r0:d1")
        self.assertEqual((on.config.distinct, on.config.rerank_count, on.name), (1, 0, "enemy-plan4:r0:d1"))
        with self.assertRaises(ValueError):
            planner_from_name(Side.ENEMY, "plan4:d-1")


if __name__ == "__main__":
    unittest.main()
