"""플레이 성격 지표(설계 docs/2026-11-18-enemy-playstyle-consistency-design.md PS-0).

손 판은 test_sim_rules.battle()로 만든다(players[0]이 메인). 실제 기록 시험은 results/sim의 기준 기록 하나를 다시 돌린다.
"""

from __future__ import annotations

from pathlib import Path
import sys
from types import SimpleNamespace
import unittest
from unittest import mock

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tests"))
sys.path.insert(0, str(ROOT))

from test_sim_rules import battle, to_enemy_turn, unit_named  # noqa: E402
from sim.playstyle import (METRICS, Count, StyleTurn, analyze_file, battle_metrics, can_hit_opponent,  # noqa: E402
                           declined_attackers, game_key, kill_threatened,
                           manhattan, reach_table, replay_steps, role_score, threat_cells)
from sim.playstyle_compare import compare, distinct_keys, pooled  # noqa: E402
from sim.main_focus import EnemyTurn, initial_state  # noqa: E402
from sim.rules import EndTurn, Move, apply, reachable  # noqa: E402
from sim.state import ActionUse, Outcome  # noqa: E402
from sim.targeting import Side  # noqa: E402


def turn_of(start, end, segment=None):
    """적 턴 하나. 도중에 끝나지 않았으면 턴을 넘긴 판(handed)까지 만든다."""
    handed = apply(end, EndTurn()) if end.outcome is None else None
    return StyleTurn(start.round, start, end, end.log[len(start.log):] if segment is None else segment, handed=handed)


def toward(state, unit, cell, nearest=True):
    """그 유닛이 걸어서 갈 수 있는 칸 중 cell에 가장 가까운(nearest=False면 가장 먼) 칸으로 가는 이동."""
    paths = reachable(state, unit)
    pick = min if nearest else max
    return Move(unit.uid, paths[pick(paths, key=lambda c: abs(c[0] - cell[0]) + abs(c[1] - cell[1]))])


class ThreatTests(unittest.TestCase):
    def test_kill_threat_needs_enough_damage_in_reach(self):
        # Akimbo(예상 9)·Sniper(예상 4)가 함께 닿는 Demon: HP 20이면 13 < 20이라 아님, HP 13이면 잡힐 칸.
        # 판 끝 멀리 있는 Demon은 닿지 않아 HP와 상관없이 아님
        state = battle([("Unit_Akimbo", (4, 1)), ("Unit_Sniper", (0, 0))],
                       [("Unit_Demon", (4, 3)), ("Unit_Demon", (8, 8))])
        table = reach_table(state, Side.PLAYER)
        near = next(u for u in state.living(Side.ENEMY) if u.cell == (4, 3))
        far = next(u for u in state.living(Side.ENEMY) if u.cell == (8, 8))
        self.assertFalse(kill_threatened(state, near, table))
        near.hp = 13
        self.assertTrue(kill_threatened(state, near, table))
        far.hp = 1
        self.assertFalse(kill_threatened(state, far, table))

    def test_reach_table_is_per_unit_with_percent(self):
        state = battle([("Unit_Akimbo", (4, 1)), ("Unit_Sniper", (0, 0))], [("Unit_Demon", (8, 8))])
        table = reach_table(state, Side.PLAYER)
        self.assertEqual(set(table), {u.uid for u in state.living(Side.PLAYER) if u.defn.can_attack})
        akimbo = unit_named(state, "Unit_Akimbo")
        self.assertEqual(table[akimbo.uid][(4, 2)], 100)
        self.assertIn((4, 2), threat_cells(table))

    def test_can_hit_opponent_asks_the_engine(self):
        # 플레이어 턴: Akimbo는 (4,3)의 Demon을 칠 수 있고, 판 끝 Demon뿐이면 못 친다
        near = battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 3))])
        self.assertTrue(can_hit_opponent(near, unit_named(near, "Unit_Akimbo")))
        far = battle([("Unit_Akimbo", (0, 0))], [("Unit_Demon", (8, 8))])
        self.assertFalse(can_hit_opponent(far, unit_named(far, "Unit_Akimbo")))
        # 상대 턴의 유닛은 못 친다
        self.assertFalse(can_hit_opponent(near, unit_named(near, "Unit_Demon")))

    def test_aims_that_also_hit_own_side_do_not_count(self):
        # 검토 M5: 상대를 맞히려면 제 편도 함께 맞혀야만 하는 조준뿐이면 "칠 수 있었다"가 아니다
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 3)), ("Unit_Demon", (8, 8))]))
        near = next(u for u in state.living(Side.ENEMY) if u.cell == (4, 3))
        far = next(u for u in state.living(Side.ENEMY) if u.cell == (8, 8))
        akimbo = unit_named(state, "Unit_Akimbo")

        def plan(*targets):
            return (None, SimpleNamespace(hits=[SimpleNamespace(kind="unit", target=uid) for uid in targets]))

        with mock.patch("sim.playstyle.attack_destinations", return_value=[]):
            with mock.patch("sim.playstyle.legal_attacks", return_value=[plan(akimbo.uid, far.uid)]):
                self.assertFalse(can_hit_opponent(state, near))
            with mock.patch("sim.playstyle.legal_attacks", return_value=[plan(akimbo.uid, far.uid), plan(akimbo.uid)]):
                self.assertTrue(can_hit_opponent(state, near))

    def test_shield_counts_only_while_active(self):
        # 검토 L2: 단독 행동 보호막은 반경 안에 다른 유닛이 없을 때만 막는다.
        # Akimbo 9 + Sniper 4 = 13. HP 13 + 보호막 10: 홀로면 13 < 23이라 안 잡히고, 옆에 제 편이 붙으면 13 ≥ 13이라 잡힌다
        alone = battle([("Unit_Akimbo", (4, 1)), ("Unit_Sniper", (0, 0))],
                       [("Unit_Demon_SoloAction", (4, 3)), ("Unit_Demon", (8, 8))])
        solo = unit_named(alone, "Unit_Demon_SoloAction")
        solo.hp = 13
        self.assertEqual(solo.shield, 10)
        self.assertFalse(kill_threatened(alone, solo, reach_table(alone, Side.PLAYER)))
        paired = battle([("Unit_Akimbo", (4, 1)), ("Unit_Sniper", (0, 0))],
                        [("Unit_Demon_SoloAction", (4, 3)), ("Unit_Demon", (4, 4))])
        solo = unit_named(paired, "Unit_Demon_SoloAction")
        solo.hp = 13
        self.assertEqual(solo.shield, 10)
        self.assertTrue(kill_threatened(paired, solo, reach_table(paired, Side.PLAYER)))


class DeclinedTests(unittest.TestCase):
    """점검 R4: "닿았는데 안 침"은 그 유닛이 행동한 순간의 판으로 센다."""

    def board(self):
        # 적 턴. 가까운 Demon은 Akimbo를 칠 수 있고, 먼 Demon은 못 친다.
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 3)), ("Unit_Demon", (8, 8))]))
        near = next(u for u in state.living(Side.ENEMY) if u.cell == (4, 3))
        far = next(u for u in state.living(Side.ENEMY) if u.cell == (8, 8))
        return state, near, far

    def test_idle_unit_is_judged_on_the_final_board(self):
        state, near, far = self.board()
        turn = EnemyTurn(state.round, state, state, [])
        self.assertEqual(declined_attackers(turn, [], set()), {near.uid})

    def test_attacker_is_not_declined(self):
        state, near, far = self.board()
        turn = EnemyTurn(state.round, state, state, [])
        self.assertEqual(declined_attackers(turn, [], {near.uid}), set())

    def test_moved_unit_is_judged_before_its_move(self):
        # 가까운 Demon이 Akimbo에서 가장 먼 칸으로 물러났다: 끝 판에서는 못 치지만, 이동 직전에는 칠 수 있었다
        state, near, far = self.board()
        paths = reachable(state, near)
        away = max(paths, key=lambda c: abs(c[0] - 4) + abs(c[1] - 1))
        move = Move(near.uid, paths[away])
        end = apply(state, move)
        self.assertFalse(can_hit_opponent(end, end.units[near.uid]))
        turn = EnemyTurn(state.round, state, end, [])
        self.assertEqual(declined_attackers(turn, [(state, move)], set()), {near.uid})


class NaturalTests(unittest.TestCase):
    """자연스러움 지표: D1 칠 수 있는데 안 침, D2 제자리."""

    RECORD = {"enemy_spawned": 0, "enemy_units_lost": 0}

    def board(self):
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 3)), ("Unit_Demon", (8, 8))]))
        far = next(u for u in state.living(Side.ENEMY) if u.cell == (8, 8))
        return state, far

    def test_nobody_acts(self):
        # 가까운 Demon은 칠 수 있는데 안 쳤고(D1 1/1), 둘 다 이동도 공격도 안 했다(D2 2/2)
        state, _ = self.board()
        row = battle_metrics(state, [turn_of(state, state)], [[]], self.RECORD)
        self.assertEqual((row["D1_num"], row["D1_den"]), (1, 1))
        self.assertEqual((row["D2_num"], row["D2_den"]), (2, 2))

    def test_declining_turn_after_turn_is_a_streak(self):
        # 같은 유닛이 두 턴 이어서 칠 수 있는데 안 치면 이어진 턴 수는 2. 치면 끊긴다
        state, _ = self.board()
        near = next(u for u in state.living(Side.ENEMY) if u.cell == (4, 3))
        idle = turn_of(state, state)
        row = battle_metrics(state, [idle, idle], [[], []], self.RECORD)
        self.assertEqual(row["streak"], 2)
        hit = turn_of(state, state, [{"type": "attack", "uid": near.uid, "hits": []}])
        row = battle_metrics(state, [idle, hit, idle], [[], [], []], self.RECORD)
        self.assertEqual(row["streak"], 1)

    def test_a_move_is_not_standing_still(self):
        # 먼 Demon이 걸으면 제자리는 가까운 Demon 하나(D2 1/2)
        state, far = self.board()
        paths = reachable(state, far)
        move = Move(far.uid, paths[max(paths, key=lambda c: abs(c[0] - 8) + abs(c[1] - 8))])
        end = apply(state, move)
        row = battle_metrics(state, [turn_of(state, end)], [[(state, move)]], self.RECORD)
        self.assertEqual((row["D2_num"], row["D2_den"]), (1, 2))
        self.assertEqual((row["D1_num"], row["D1_den"]), (1, 1))


    def test_units_that_cannot_act_are_not_counted(self):
        # 방금 들어온 증원(DEFERRED)은 그 턴에 움직일 수 없으므로 제자리로 세지 않는다
        state, far = self.board()
        far.use = ActionUse.DEFERRED
        row = battle_metrics(state, [turn_of(state, state)], [[]], self.RECORD)
        self.assertEqual((row["D2_num"], row["D2_den"]), (1, 1))
        self.assertEqual(row["C2_den"], 1)


class AvoidableTests(unittest.TestCase):
    """C2v 피할 수 있었는데 노출: 잡히지 않는 칸으로 갈 수 있었던 공격 안 한 유닛만 센다."""

    RECORD = {"enemy_spawned": 0, "enemy_units_lost": 0}

    def test_unit_that_could_have_stepped_out_counts(self):
        # Akimbo(9)·Sniper(4)가 닿는 칸에 HP 13인 Demon이 그대로 선다: 잡힐 칸이고, 걸어서 벗어날 칸이 있었다 → C2v 1/1.
        # 판 끝의 Demon은 안 잡히는 칸에 있다 → 분모에만 든다(C2v 1/2).
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 1)), ("Unit_Sniper", (0, 0))],
                                     [("Unit_Demon", (4, 3)), ("Unit_Demon", (8, 8))]))
        near = next(u for u in state.living(Side.ENEMY) if u.cell == (4, 3))
        near.hp = 13
        row = battle_metrics(state, [turn_of(state, state)], [[]], self.RECORD)
        self.assertEqual((row["C2n_num"], row["C2n_den"]), (1, 2))
        self.assertEqual((row["C2v_num"], row["C2v_den"]), (1, 2))

    def test_unit_with_no_way_out_is_left_out(self):
        # 움직일 수 없는(발목 꺾임) 유닛이 잡힐 칸에 서 있으면 피할 칸이 없었다 → C2n에는 들고 C2v에는 안 든다
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 1)), ("Unit_Sniper", (0, 0))],
                                     [("Unit_Demon", (4, 3)), ("Unit_Demon", (8, 8))]))
        near = next(u for u in state.living(Side.ENEMY) if u.cell == (4, 3))
        near.hp = 13
        near.statuses["bind"] = 2
        row = battle_metrics(state, [turn_of(state, state)], [[]], self.RECORD)
        self.assertEqual((row["C2n_num"], row["C2n_den"]), (1, 2))
        self.assertEqual((row["C2v_num"], row["C2v_den"]), (0, 1))


class ApproachTests(unittest.TestCase):
    """D3 칠 수 없을 때 접근(안 다친 유닛), 그리고 접근·후퇴를 재는 기준(검토 H1·M1)."""

    RECORD = {"enemy_spawned": 0, "enemy_units_lost": 0}

    def board(self):
        # 적 턴. 가까운 Demon은 Akimbo를 칠 수 있다(D3에 안 든다). 먼 Demon은 이동해도 못 친다.
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 1))], [("Unit_Demon", (4, 3)), ("Unit_Demon", (7, 7))]))
        near = next(u for u in state.living(Side.ENEMY) if u.cell == (4, 3))
        far = next(u for u in state.living(Side.ENEMY) if u.cell == (7, 7))
        return state, near, far

    def metrics(self, state, move):
        end = apply(state, move)
        return battle_metrics(state, [turn_of(state, end)], [[(state, move)]], self.RECORD)

    def test_coming_closer_counts(self):
        state, _, far = self.board()
        row = self.metrics(state, toward(state, far, (4, 1)))
        self.assertEqual((row["D3_num"], row["D3_den"]), (1, 1))
        self.assertEqual((row["A2_num"], row["A2_den"]), (1, 1))

    def test_walking_away_does_not(self):
        state, _, far = self.board()
        row = self.metrics(state, toward(state, far, (4, 1), nearest=False))
        self.assertEqual((row["D3_num"], row["D3_den"]), (0, 1))

    def test_standing_far_away_does_not(self):
        state, _, _ = self.board()
        row = battle_metrics(state, [turn_of(state, state)], [[]], self.RECORD)
        self.assertEqual((row["D3_num"], row["D3_den"]), (0, 1))

    def test_hurt_units_are_left_out(self):
        # 다친 유닛(HP가 최대의 절반 이하)이 물러나는 것은 싸움을 피하는 것으로 세지 않는다
        state, _, far = self.board()
        far.hp = far.max_hp // 2
        row = self.metrics(state, toward(state, far, (4, 1), nearest=False))
        self.assertEqual(row["D3_den"], 0)

    def test_units_whose_turn_never_came_are_left_out(self):
        # 검토 H1: 적 턴 도중에 전투가 끝나면, 그때까지 이동도 공격도 안 한 유닛은 접근·후퇴·제자리에 세지 않는다
        state, _, _ = self.board()
        end = state.clone(keep_log=True)
        end.outcome = Outcome.ENEMY_WIN
        row = battle_metrics(state, [turn_of(state, end, [])], [[]], self.RECORD)
        for name in ("A2", "D2", "D3", "B1", "C2"):
            self.assertEqual(row[f"{name}_den"], 0, name)

    def test_retreat_is_measured_against_where_the_player_stood(self):
        # 검토 M1: 같은 턴에 플레이어 유닛이 밀려나도(끝 판에서 멀어져도) 가만히 있던 유닛은 후퇴가 아니다
        state, near, far = self.board()
        end = state.clone(keep_log=True)
        unit_named(end, "Unit_Akimbo").cell = (0, 0)
        segment = [{"type": "attack", "uid": near.uid, "hits": []}]
        row = battle_metrics(state, [turn_of(state, end, segment)], [[]], self.RECORD)
        self.assertEqual((row["B5_num"], row["B5_den"]), (0, 1))
        self.assertEqual(row["attacks"], 1)


class KnockbackTests(unittest.TestCase):
    """A3 RIDER 넉백: 밀 수 있는 대상이 있는 공격만 센다(검토 M2)."""

    RECORD = {"enemy_spawned": 0, "enemy_units_lost": 0}

    def row(self, target_name, knock):
        state = to_enemy_turn(battle([("Unit_Akimbo", (0, 0)), ("Unit_ShotGun", (4, 2))], [("Unit_Gravekeeper", (4, 3))]))
        rider = unit_named(state, "Unit_Gravekeeper")
        target = unit_named(state, target_name)
        segment = [{"type": "attack", "uid": rider.uid, "hits": [{"kind": "unit", "target": target.uid, "killed": False}]},
                   dict(knock, type="knockback", uid=target.uid)]
        return battle_metrics(state, [turn_of(state, state, segment)], [[]], self.RECORD)

    def test_immune_target_is_not_a_failed_knockback(self):
        # ShotGun은 굳건한 의지(넉백 면역): 밀 수 없는 대상뿐인 공격은 분모에 넣지 않는다
        row = self.row("Unit_ShotGun", {"stop": "immune"})
        self.assertEqual(row["A3_den"], 0)

    def test_pushable_target_counts(self):
        pushed = self.row("Unit_Akimbo", {"path": [[0, 1]], "fell": False})
        self.assertEqual((pushed["A3_num"], pushed["A3_den"]), (1, 1))
        stuck = self.row("Unit_Akimbo", {"path": [], "fell": False})
        self.assertEqual((stuck["A3_num"], stuck["A3_den"]), (0, 1))


class CountTests(unittest.TestCase):
    def test_ratio_and_weighted_value(self):
        count = Count()
        count.add(True)
        count.add(False)
        self.assertEqual(count.value, 0.5)
        lost = Count()
        lost.add(3.0, weight=10.0)
        self.assertAlmostEqual(lost.value, 0.3)
        self.assertIsNone(Count().value)

    def test_manhattan(self):
        self.assertEqual(manhattan((0, 0), (2, 3)), 5)


class RoleScoreTests(unittest.TestCase):
    def counts(self, **values):
        result = {name: Count() for name in METRICS if name != "Astar"}
        for name, (num, den) in values.items():
            result[name] = Count(num, den)
        return result

    def test_lower_is_better_metrics_are_flipped(self):
        # HEAVY만: A5 = 1(대포보다 앞), A6 = 1(늘 후퇴) → (1 + 0) / 2 = 0.5
        self.assertEqual(role_score(self.counts(A5=(4, 4), A6=(2, 2))), 0.5)

    def test_categories_are_averaged(self):
        # MAIN 1.0(A1만), CANNON (1 + (1 − 0.5)) / 2 = 0.75 → 0.875
        self.assertEqual(role_score(self.counts(A1=(3, 3), A7=(2, 2), A8=(1, 2))), 0.875)

    def test_nothing_counted(self):
        self.assertIsNone(role_score(self.counts()))


class RecordTests(unittest.TestCase):
    def test_saved_record_replays_and_counts(self):
        files = sorted((ROOT / "results" / "sim").glob("20261118T152855Z_base2_g1/battles/*.json"))
        if not files:
            self.skipTest("기준 기록이 없습니다")
        result = analyze_file(str(files[0]))
        self.assertTrue(result["same"])
        row = result["row"]
        for name in METRICS:
            if name != "Astar":
                self.assertIn(f"{name}_num", row)
                # B1(걸음 합)·C1t(잃은 적 ÷ 적 턴)는 비율이 아니라 분자가 분모보다 클 수 있다
                self.assertLessEqual(row[f"{name}_num"], row[f"{name}_den"] if name not in ("B1", "C1t") else 1e9)
        self.assertGreater(row["C1_den"], 0)
        self.assertEqual(len(row["game"]), 12)
        # 노출은 공격한 유닛·안 한 유닛으로 빠짐없이 나뉜다
        self.assertEqual(row["C2_num"], row["C2n_num"] + row["C2a_num"])
        self.assertEqual(row["C2_den"], row["C2n_den"] + row["C2a_den"])
        self.assertGreaterEqual(row["turns"], row["idle_turns"])
        self.assertGreaterEqual(row["attacks"], row["turns"] - row["idle_turns"])

    def test_turns_carry_the_board_after_the_turn_is_passed(self):
        import json
        files = sorted((ROOT / "results" / "sim").glob("20261118T152855Z_base2_g1/battles/*.json"))
        if not files:
            self.skipTest("기준 기록이 없습니다")
        data = json.loads(files[0].read_text(encoding="utf-8"))
        turns, all_steps, _ = replay_steps(initial_state(data["record"]), data["actions"][:-1])
        self.assertEqual(len(turns), len(all_steps))
        self.assertTrue(turns)
        for turn in turns:
            if turn.end.outcome is None:      # 턴을 넘겼다: 넘긴 판은 플레이어 차례이거나 전투가 끝난 판
                self.assertIsNotNone(turn.handed)
                self.assertTrue(turn.handed.outcome is not None or turn.handed.turn_side is Side.PLAYER)
            else:                             # 도중에 끝났다
                self.assertIsNone(turn.handed)

    def test_game_key_sees_the_event_log(self):
        # 검토 L1: 행동이 같아도 사건 기록(난수 결과)이 다르면 다른 판
        actions = [{"round": 1, "side": "enemy", "action": {"type": "end"}}]
        self.assertEqual(game_key(actions + [{"log": [1]}]), game_key(actions + [{"log": [1]}]))
        self.assertNotEqual(game_key(actions + [{"log": [1]}]), game_key(actions + [{"log": [2]}]))


class CompareTests(unittest.TestCase):
    def row(self, game, a1=(1, 2), **parts):
        row = {"game": game, "Astar": "0.5", "outcome": "enemy_win", "turns": 4, "idle_turns": 1, "attacks": 6, "streak": 0}
        for name in METRICS:
            if name != "Astar":
                row[f"{name}_num"], row[f"{name}_den"] = 0, 0
        row["A1_num"], row["A1_den"] = a1
        for name, (num, den) in parts.items():
            row[f"{name}_num"], row[f"{name}_den"] = num, den
        return row

    def test_lines_are_judged_on_the_new_values_own_games(self):
        # 검토 M3: 새 값의 같은 판(씨앗 0·1)이 기준 쪽이 달라 짝으로는 둘로 남아도, 절대 기준에는 한 번만 들어간다.
        # 짝 기준: 노출 (1 + 1 + 0) / (1 + 1 + 3) = 0.4 > 0.25. 새 값 기준: (1 + 0) / (1 + 3) = 0.25 ≤ 0.25
        # (안정 운영형의 절대 기준은 피할 수 있었는데 노출 C2v다)
        base = {("S", seed, "rush"): self.row(f"b{seed}", C2v=(1, 1), C2n=(1, 1), C1t=(4, 4)) for seed in range(3)}
        new = {("S", 0, "rush"): self.row("n", C2v=(1, 1), C2n=(1, 1), C1t=(1, 4)),
               ("S", 1, "rush"): self.row("n", C2v=(1, 1), C2n=(1, 1), C1t=(1, 4)),
               ("S", 2, "rush"): self.row("m", C2v=(0, 3), C2n=(0, 3), C1t=(1, 4))}
        result = compare(base, new)
        self.assertEqual((result["pairs"], result["distinct"], result["own"]), (3, 3, 2))
        self.assertEqual(result["personas"]["C 안정 운영형"]["absolute"], (1, 1, []))
        self.assertAlmostEqual(result["metrics"]["C2v"]["new"], 0.4)

    def judged(self, base_rows, new_rows):
        base = {("S", seed, "rush"): row for seed, row in enumerate(base_rows)}
        new = {("S", seed, "rush"): row for seed, row in enumerate(new_rows)}
        return compare(base, new)

    def test_games_without_an_attack_only_count_when_the_baseline_had_one(self):
        # 하한: 적이 한 번도 공격하지 않은 판은 같은 판의 기준도 그랬으면 세지 않는다(판·상대의 생김새)
        quiet = dict(self.row("q"), turns=3, idle_turns=3, attacks=0)
        both = self.judged([dict(quiet, game="bq"), self.row("b1")], [quiet, self.row("n1")])
        self.assertEqual(both["fresh_never"], 0)
        self.assertTrue(both["personas"]["C 안정 운영형"]["floor"])
        fresh = self.judged([self.row("b0"), self.row("b1")], [quiet, self.row("n1")])
        self.assertEqual(fresh["fresh_never"], 1)
        self.assertFalse(fresh["personas"]["C 안정 운영형"]["floor"])

    def test_declined_attacks_are_part_of_naturalness(self):
        # 자연스러움: 칠 수 있는데 안 침 D1 ≤ 0.15 (모든 스테이지). 3/10은 넘고 1/10은 안 넘는다
        def natural(d1):
            result = self.judged([self.row("b0")], [self.row("n0", D1=d1, D2=(0, 10), D3=(9, 10))])
            return {name: met[0] == met[1] for name, _, _, met in result["natural"]}

        self.assertEqual(natural((3, 10)), {"D2": True, "D3": True, "D1": False})
        self.assertEqual(natural((1, 10)), {"D2": True, "D3": True, "D1": True})

    def test_same_games_count_once_per_combination(self):
        base = {("S", 0, "rush"): self.row("x"), ("S", 1, "rush"): self.row("x"), ("S", 2, "rush"): self.row("y"),
                ("S", 0, "plan8"): self.row("x")}
        self.assertEqual(distinct_keys(list(base), base), [("S", 0, "plan8"), ("S", 0, "rush"), ("S", 2, "rush")])
        # 짝 비교: 기준은 같아도 새 값이 다르면 다른 짝
        new = {key: self.row("n" + str(key[1])) for key in base}
        self.assertEqual(len(distinct_keys(list(base), base, new)), 4)

    def test_pooled_role_score_uses_pooled_parts(self):
        # MAIN만: A1 = (1 + 3) / (2 + 4) = 2/3 → A* = 2/3 (판마다의 A* 0.5 평균이 아님)
        rows = [self.row("x", (1, 2)), self.row("y", (3, 4))]
        self.assertAlmostEqual(pooled(rows, "A1"), 4 / 6)
        self.assertAlmostEqual(pooled(rows, "Astar"), 4 / 6)


if __name__ == "__main__":
    unittest.main()
