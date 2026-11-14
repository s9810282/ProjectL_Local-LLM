"""적 메인 노림 지표(설계 docs/2026-11-14-enemy-main-focus-metrics-design.md M-a).

손 판은 test_sim_rules.battle()로 만든다(players[0]이 메인, 적이 먼저 번호를 받는다).
피해 손 계산(hit_damage): (공격 × 상성 + 5) // 10, × (100 − 방어) // 100.
- Demon(본체, 공 8) → Sniper(본체, 방 5, HP 10): (8 × 10 + 5) // 10 = 8, 8 × 95 // 100 = 7
- UndeadCannon(대포, 공 15) → Demon_SoloAction(본체, 방 5, HP 20, 보호막 10): 15 × 95 // 100 = 14
"""

from __future__ import annotations

from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
sys.path.insert(0, str(Path(__file__).resolve().parent))

from test_sim_rules import battle, to_enemy_turn, unit_named  # noqa: E402
from sim.assets import TerrainKind  # noqa: E402
from sim.enemy import make_enemy_policy  # noqa: E402
from sim.main_focus import (KILL, MISSED_ATTACK, MISSED_ATTACK_UNCONFIRMED, MISSED_KILL, NO_CHANCE,  # noqa: E402
                            OUTSIDE_HIT, OUTSIDE_KILL, SHORT, SHORT_UNCONFIRMED, Opportunity, TurnResult, classify,
                            idle_walk, opportunity, replay_checked, summarize, toughness, turn_result)
from sim.player import make_player_policy  # noqa: E402
from sim.rules import Attack, EndTurn, Move, apply, reachable  # noqa: E402
from sim.run import _action_json  # noqa: E402
from sim.targeting import Side  # noqa: E402


def move_to(state, uid, cell):
    return apply(state, Move(uid, reachable(state, state.units[uid])[cell]))


def turn(start, end):
    """시작 판·끝 판 → (기회, 결과, 분류)."""
    opp = opportunity(start)
    result = turn_result(start, end, end.log[len(start.log):], opp.reach)
    return opp, result, classify(opp, result)


class OpportunityTests(unittest.TestCase):
    def test_far_enemy_is_no_chance(self):
        # Demon 이동 3 + 사거리 1, 메인까지 걷는 거리 16 → 닿지 않음
        state = to_enemy_turn(battle([("Unit_Sniper", (0, 0))], [("Unit_Demon", (8, 8))]))
        opp, result, category = turn(state, state)
        self.assertEqual((opp.reach, opp.kill, opp.nearest), ({}, "none", 16))
        self.assertEqual((result.main_lost, category), (0, NO_CHANCE))

    def test_one_enemy_reaches_short_or_missed(self):
        # Demon (4,6) → (4,3)으로 와서 (4,2)의 메인을 친다: 7(머리글 손 계산). 메인 HP 10이라 처치는 못 잡음.
        start = to_enemy_turn(battle([("Unit_Sniper", (4, 2))], [("Unit_Demon", (4, 6))]))
        opp = opportunity(start)
        self.assertEqual((opp.reach, opp.solo_kill, opp.kill), ({1: 7}, False, "none"))
        end = apply(move_to(start, 1, (4, 3)), Attack(1, (4, 2)))
        _, result, category = turn(start, end)
        self.assertEqual((result.main_lost, result.main_killed, result.hitters), (7, False, [1]))
        self.assertEqual(category, SHORT)
        # 아무것도 안 하면 놓친 공격, 대신 한 것은 대기
        _, idle, category = turn(start, start)
        self.assertEqual((category, idle.instead), (MISSED_ATTACK, {1: "wait"}))

    @staticmethod
    def three_demons():
        # 메인 Sniper(4,2) HP 10, Akimbo(4,4). Demon 1(4,6)·2(7,2)·3(1,2). 둘이 치면 7 + 7 = 14 ≥ 10.
        return to_enemy_turn(battle([("Unit_Sniper", (4, 2)), ("Unit_Akimbo", (4, 4))],
                                    [("Unit_Demon", (4, 6)), ("Unit_Demon", (7, 2)), ("Unit_Demon", (1, 2))]))

    def test_two_together_kill_is_confirmed_and_taking_it_is_kill(self):
        start = self.three_demons()
        opp = opportunity(start)
        self.assertEqual((opp.solo_kill, opp.kill), (False, "confirmed"))
        self.assertTrue({2, 3} <= set(opp.reach))
        self.assertTrue(all(lost == 7 for lost in opp.reach.values()))
        end = apply(move_to(start, 2, (5, 2)), Attack(2, (4, 2)))
        end = apply(move_to(end, 3, (3, 2)), Attack(3, (4, 2)))
        _, result, category = turn(start, end)
        self.assertEqual((result.main_killed, result.hitters, category), (True, [2, 3], KILL))
        self.assertEqual(result.main_lost, 10)

    def test_hitting_someone_else_instead_is_missed_kill(self):
        start = self.three_demons()
        end = apply(move_to(start, 1, (4, 5)), Attack(1, (4, 4)))        # 1은 Akimbo를 친다
        end = move_to(end, 2, (6, 3))                                     # 2는 움직이기만
        opp, result, category = turn(start, end)
        self.assertEqual(category, MISSED_KILL)
        self.assertEqual(result.main_lost, 0)
        expected = {2: "move_only", 3: "wait"}
        if 1 in opp.reach:
            expected[1] = "hit_other"
        self.assertEqual(result.instead, expected)

    def test_one_free_cell_makes_the_kill_unconfirmed(self):
        # 구석 메인(0,0): (0,1)·(1,1)이 낭떠러지라 칠 수 있는 자리는 (1,0) 하나. 둘 다 닿지만 함께 칠 순서가 없다.
        start = to_enemy_turn(battle([("Unit_Sniper", (0, 0)), ("Unit_Akimbo", (8, 8))],
                                     [("Unit_Demon", (3, 0)), ("Unit_Demon", (3, 1))],
                                     terrain=[(TerrainKind.CLIFF, (0, 1)), (TerrainKind.CLIFF, (1, 1))]))
        opp, result, category = turn(start, start)
        self.assertEqual((opp.reach, opp.total, opp.kill), ({1: 7, 2: 7}, 14, "unconfirmed"))
        self.assertEqual(category, MISSED_ATTACK_UNCONFIRMED)

    def test_shield_counts_in_what_the_main_loses(self):
        # 단독 행동 메인: 반경 1에 아무도 없어 보호막 10이 막는다. 대포 14 → 보호막 10 + HP 4.
        start = to_enemy_turn(battle([("Unit_Demon_SoloAction", (4, 4))], [("Unit_UndeadCannon", (4, 7))]))
        main = start.main_character()
        self.assertEqual((main.hp, main.shield, toughness(start, main.uid)), (20, 10, 30))
        opp = opportunity(start)
        self.assertEqual(opp.reach, {1: 14})
        end = apply(start, Attack(1, (4, 4)))
        _, result, category = turn(start, end)
        self.assertEqual((end.units[main.uid].hp, end.units[main.uid].shield), (16, 0))
        self.assertEqual((result.main_lost, category), (14, SHORT))

    def test_does_not_touch_the_live_random_state(self):
        start = self.three_demons()
        before = start.rng.getstate()
        opportunity(start)
        self.assertEqual(start.rng.getstate(), before)


class ClassifyTests(unittest.TestCase):
    def test_table(self):
        none, reach = Opportunity(), Opportunity(reach={1: 5})
        unconfirmed = Opportunity(reach={1: 5, 2: 6}, kill="unconfirmed")
        confirmed = Opportunity(reach={1: 5, 2: 6}, kill="confirmed")
        idle, hit, killed = TurnResult(), TurnResult(main_lost=4), TurnResult(main_lost=10, main_killed=True)
        cases = [(none, idle, NO_CHANCE), (none, hit, OUTSIDE_HIT), (none, killed, OUTSIDE_KILL),
                 (reach, idle, MISSED_ATTACK), (reach, hit, SHORT), (reach, killed, OUTSIDE_KILL),
                 (unconfirmed, idle, MISSED_ATTACK_UNCONFIRMED), (unconfirmed, hit, SHORT_UNCONFIRMED),
                 (unconfirmed, killed, KILL),
                 (confirmed, idle, MISSED_KILL), (confirmed, hit, MISSED_KILL), (confirmed, killed, KILL)]
        for opp, result, expected in cases:
            with self.subTest(kill=opp.kill, reach=len(opp.reach), lost=result.main_lost):
                self.assertEqual(classify(opp, result), expected)


class ReplayTests(unittest.TestCase):
    def play(self, state, rounds=3):
        """run.py처럼 한 행동씩 돌리고 행동 기록을 남긴다."""
        policies = {Side.PLAYER: make_player_policy("rush"), Side.ENEMY: make_enemy_policy("greedy")}
        actions = []
        while state.outcome is None and state.round <= rounds and len(actions) < 200:
            action = policies[state.turn_side].act(state)
            actions.append({"round": state.round, "side": state.turn_side.value, "action": _action_json(state, action)})
            state = apply(state, action)
        return actions, state

    def test_replay_gives_the_same_log_and_one_entry_per_enemy_turn(self):
        initial = battle([("Unit_Sniper", (1, 1)), ("Unit_Akimbo", (2, 1))],
                         [("Unit_Demon", (6, 7)), ("Unit_Gravekeeper", (7, 7))])
        actions, final = self.play(initial)
        turns, same = replay_checked(initial, actions, final.log)
        self.assertTrue(same)
        enemy_turns = sum(e["type"] == "turn_begin" and e["side"] == "enemy" for e in final.log)
        self.assertEqual(len(turns), enemy_turns)
        self.assertGreater(len(turns), 0)
        for item in turns:
            self.assertIs(item.start.turn_side, Side.ENEMY)
            self.assertFalse(any(e["type"] in ("turn_begin", "turn_end") for e in item.segment))

    def test_changed_actions_are_reported_as_different(self):
        initial = battle([("Unit_Sniper", (1, 1)), ("Unit_Akimbo", (2, 1))],
                         [("Unit_Demon", (6, 7)), ("Unit_Gravekeeper", (7, 7))])
        actions, final = self.play(initial)
        # 이동을 빼면 엔진이 공격을 거절한다(RuleError) → 다름
        changed = [a for a in actions if a["action"]["type"] != "move"]
        self.assertLess(len(changed), len(actions))
        self.assertFalse(replay_checked(initial, changed, final.log)[1])
        # 마지막 행동을 빼면 거절은 없지만 사건 기록이 짧다 → 다름
        self.assertFalse(replay_checked(initial, actions[:-1], final.log)[1])


class SummaryTests(unittest.TestCase):
    def test_rates(self):
        def row(file, kill, category, lost, killed, reach, outcome="other"):
            return {"file": file, "enemy": "e", "player": "p", "outcome": outcome, "round": 1, "reach_units": reach,
                    "kill": kill, "category": category, "main_lost": lost, "main_killed": killed,
                    "nearest": 2, "instead": "[]", "seconds": 0.0}
        rows = [row("a", "confirmed", MISSED_KILL, 0, False, 2), row("a", "confirmed", KILL, 10, True, 2, "enemy_win"),
                row("b", "none", SHORT, 4, False, 1), row("b", "none", NO_CHANCE, 0, False, 0)]
        rows[0]["outcome"] = "enemy_win"
        [summary] = summarize(rows)
        self.assertEqual((summary["battles"], summary["enemy_turns"]), (2, 4))
        self.assertEqual(summary["kill_conversion"], 0.5)          # 처치 확인 2턴 중 1턴 잡음
        self.assertEqual(summary["hit_when_reachable"], 0.667)     # 닿는 3턴 중 2턴 침
        self.assertEqual(summary["share"][NO_CHANCE], 0.25)
        self.assertEqual(summary["by_outcome"]["enemy_win"]["confirmed_turns_per_battle"], 2.0)
        self.assertEqual(summary["by_outcome"]["other"]["battles_with_kill_chance"], 0)



class IdleWalkTests(unittest.TestCase):
    """공격하지 않은 적 유닛의 걸음(설계 docs/2026-11-14-planner-goal-pressure-design.md §1.1, Q-G6)."""

    def test_toward_away_still_and_near2(self):
        # 메인 Sniper (0,0). Demon A (8,8) → (5,8): 16 → 13, 가까워짐 3. Demon B (8,0) → (8,3): 8 → 11, 멀어짐.
        # Demon C (3,3)은 제자리: 거리 6 ≤ 이동 3 × 2 + 사거리 1 = 7이라 턴 끝 2턴 거리 안.
        start = to_enemy_turn(battle([("Unit_Sniper", (0, 0))],
                                     [("Unit_Demon", (8, 8)), ("Unit_Demon", (8, 0)), ("Unit_Demon", (3, 3))]))
        a, b, c = (next(u.uid for u in start.living(Side.ENEMY) if u.cell == cell) for cell in ((8, 8), (8, 0), (3, 3)))
        end = move_to(move_to(start, a, (5, 8)), b, (8, 3))
        walk = idle_walk(start, end, end.log[len(start.log):])
        self.assertEqual((walk.units, walk.toward, walk.away, walk.still, walk.closer, walk.near2_end),
                         (3, 1, 1, 1, 3 - 3, 1))

    def test_attacking_unit_is_not_idle(self):
        # Demon (4,6) → (4,3)으로 와서 메인을 친다: 공격했으므로 세지 않는다
        start = to_enemy_turn(battle([("Unit_Sniper", (4, 2))], [("Unit_Demon", (4, 6))]))
        end = apply(move_to(start, 1, (4, 3)), Attack(1, (4, 2)))
        walk = idle_walk(start, end, end.log[len(start.log):])
        self.assertEqual((walk.units, walk.near2_end), (0, 1))

    def test_summary_shares(self):
        def row(units, toward, still, away, closer, near2):
            return {"file": "a", "enemy": "e", "player": "p", "outcome": "other", "round": 1, "reach_units": 0,
                    "kill": "none", "category": NO_CHANCE, "main_lost": 0, "main_killed": False, "nearest": 5,
                    "instead": "[]", "seconds": 0.0, "idle_units": units, "idle_toward": toward,
                    "idle_still": still, "idle_away": away, "idle_closer": closer, "near2_end": near2}
        [summary] = summarize([row(3, 2, 1, 0, 5, 2), row(1, 0, 0, 1, -1, 0)])
        walk = summary["idle_walk"]
        self.assertEqual((walk["units"], walk["toward"], walk["still"], walk["away"]), (4, 0.5, 0.25, 0.25))
        self.assertEqual((walk["closer_per_unit"], walk["near2_end_per_turn"], walk["near2_two_or_more"]),
                         (1.0, 1.0, 0.5))


if __name__ == "__main__":
    unittest.main()
