"""rule-game(게임 적 AI 점수식 복제)의 식과 선택 규칙을 확인하는 unittest.

기대값은 게임 코드(EnemyUtilityScorer.cs, EnemyDecisionSession.cs)의 식으로 손으로 계산한 값이다.
"""

from __future__ import annotations

import copy
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))

from rule_game import HOLD_ID, evaluate_game_case, load_pair, load_weights, loss_permille, terms  # noqa: E402
from rule_policy import PolicyInputError  # noqa: E402

ASSET = load_weights(profile="asset")
DEFAULT = load_weights(profile="codeDefault")


def facts(**updates):
    value = {"attackBenefit": 0, "hazardLoss": 0, "threatPermille": 0, "threatLethal": False, "progress": 0,
             "followup": 0, "nextTurnStrike": 0, "bind": False}
    value.update(updates)
    return value


def candidate(candidate_id, kind="attack", **updates):
    value = {"id": candidate_id, "label": candidate_id, "legal": True, "kind": kind,
             "moveSteps": 0 if kind in ("wait", "attack") else 1, "selfHpAfter": 10, "headquartersHpAfter": 5,
             "playerHpAfter": {"main": 30, "sniper": 10}, "allyHpAfter": {}, "hitIds": [],
             "exposure": {"threats": [], "hazardDamage": 0}}
    value.update(updates)
    return value


def case(*candidates):
    return {"schemaVersion": 2, "situationId": "T", "title": "시험",
            "self": {"id": "e", "unit": "Gravekeeper", "hp": 10, "maxHp": 20, "moveRange": 4, "attackRange": 1},
            "players": [{"id": "main", "role": "main", "hp": 30, "maxHp": 30},
                        {"id": "sniper", "role": "regular", "hp": 10, "maxHp": 10}],
            "allies": [], "headquarters": {"hp": 5, "maxHp": 5}, "candidates": list(candidates)}


def overlay(entries, **extra):
    return {"schema": "rule-game-overlay-1", "situationId": "T", "engagementCostBefore": 2,
            "candidates": entries, **extra}


class FormulaTests(unittest.TestCase):
    def test_loss_permille_clamps_to_remaining_hp_and_floors(self):
        self.assertEqual(loss_permille(6, 8, 23), 260)    # 6000 // 23
        self.assertEqual(loss_permille(15, 12, 30), 400)  # 남은 12까지만
        self.assertEqual(loss_permille(5, 5, 0), 0)

    def test_player_risk_is_threat_percent_unless_lethal(self):
        self.assertEqual(terms(facts(threatPermille=300), ASSET)["playerRisk"], -60)
        self.assertEqual(terms(facts(threatPermille=300), DEFAULT)["playerRisk"], -150)
        # 죽을 수 있는 칸은 피해와 상관없이 목숨값 하나다.
        self.assertEqual(terms(facts(threatPermille=999, threatLethal=True), ASSET)["playerRisk"], -200)
        self.assertEqual(terms(facts(threatPermille=1, threatLethal=True), DEFAULT)["playerRisk"], -500)

    def test_progress_is_clamped_and_next_turn_uses_percent(self):
        self.assertEqual(terms(facts(progress=7), ASSET)["progress"], 100)
        self.assertEqual(terms(facts(progress=-7), ASSET)["progress"], -100)
        self.assertEqual(terms(facts(nextTurnStrike=171), ASSET)["nextTurn"], 342)
        self.assertEqual(terms(facts(nextTurnStrike=171), DEFAULT)["nextTurn"], 85)   # 정수 나눗셈
        self.assertEqual(terms(facts(followup=-50), ASSET)["followup"], 0)            # 후속 공격은 0 아래로 안 감
        self.assertEqual(terms(facts(bind=True), ASSET)["bind"], -500)
        self.assertEqual(terms(facts(hazardLoss=400), ASSET)["hazard"], -1600)


class SelectionTests(unittest.TestCase):
    def evaluate(self, base, entries, **extra):
        return evaluate_game_case(base, overlay(entries, **extra), ASSET)

    def test_kill_counts_two_thousand_and_beats_damage(self):
        result = self.evaluate(case(candidate("kill", playerHpAfter={"main": 30, "sniper": 0}),
                                    candidate("hit", playerHpAfter={"main": 24, "sniper": 10}),
                                    candidate("wait", "wait")),
                               {"kill": {"nextTurn": None}, "hit": {"nextTurn": None}, "wait": {"nextTurn": None}})
        self.assertEqual(result["choice"], "kill")
        kill = next(item for item in result["candidates"] if item["id"] == "kill")
        self.assertEqual(kill["terms"]["attack"], 3000)   # 10/10 = 1000‰ + 2000

    def test_only_in_place_threats_count_and_only_the_strongest(self):
        threats = [{"opponentId": "main", "reach": "in_place", "damage": 6},
                   {"opponentId": "sniper", "reach": "in_place", "damage": 4},
                   {"opponentId": "sniper", "reach": "after_move", "damage": 9}]
        result = self.evaluate(case(candidate("hit", playerHpAfter={"main": 24, "sniper": 10},
                                              exposure={"threats": threats, "hazardDamage": 0})),
                               {"hit": {"nextTurn": None}},
                               hold={"inPlaceThreats": [], "nextTurn": None})
        hit = result["candidates"][0]
        # 6 + 4 = 10이면 HP 10을 다 깎지만 게임은 가장 센 6만 본다 → 치명 아님, 300‰ × 20%.
        self.assertFalse(hit["facts"]["threatLethal"])
        self.assertEqual(hit["terms"]["playerRisk"], -60)

    def test_candidate_must_strictly_beat_hold(self):
        # 대기와 점수가 같으면 행동하지 않는다.
        result = self.evaluate(case(candidate("move", "move")),
                               {"move": {"engagementCostAfter": 2, "nextTurn": None}},
                               hold={"inPlaceThreats": [], "nextTurn": None})
        self.assertEqual(result["choice"], HOLD_ID)
        self.assertEqual(result["holdReason"], "exchange_loss")

    def test_certain_hazard_death_is_dropped_when_a_safe_option_exists(self):
        doomed = candidate("doomed", playerHpAfter={"main": 30, "sniper": 0},
                           exposure={"threats": [], "hazardDamage": 10})
        safe = candidate("safe", playerHpAfter={"main": 29, "sniper": 10})
        result = self.evaluate(case(doomed, safe), {"doomed": {"nextTurn": None}, "safe": {"nextTurn": None}},
                               hold={"inPlaceThreats": [], "nextTurn": None})
        self.assertEqual(result["choice"], "safe")   # 처치 점수가 더 커도 확실한 기믹 죽음은 고르지 않는다

    def test_move_attack_is_scored_as_move_with_followup(self):
        result = self.evaluate(case(candidate("go", "move_attack", playerHpAfter={"main": 24, "sniper": 10})),
                               {"go": {"engagementCostAfter": 0, "nextTurn": None}},
                               hold={"inPlaceThreats": [], "nextTurn": None})
        go = result["candidates"][0]
        self.assertEqual((go["gameAction"], go["terms"]["attack"], go["terms"]["followup"], go["terms"]["progress"]),
                         ("move", 0, 200, 40))

    def test_main_kill_gets_no_terminal_tier(self):
        # 게임의 승패 등급은 불확실성이 없을 때만 붙는데 HiddenEnvironment가 늘 있어 붙지 않는다(2026-11-13 확인).
        # 메인 처치(30/30 = 1000‰ + 2000)와 Sniper 처치(10/10 = 1000‰ + 2000)는 3000으로 같고, 동점은 후보 순서로 정한다.
        result = self.evaluate(case(candidate("win", playerHpAfter={"main": 0, "sniper": 10}),
                                    candidate("kill", playerHpAfter={"main": 30, "sniper": 0})),
                               {"win": {"nextTurn": None}, "kill": {"nextTurn": None}},
                               hold={"inPlaceThreats": [], "nextTurn": None})
        win = next(item for item in result["candidates"] if item["id"] == "win")
        self.assertEqual((win["tier"], win["terms"]["attack"]), (0, 3000))
        self.assertEqual(result["choice"], "kill")

    def test_missing_hold_or_next_turn_is_an_error(self):
        with self.assertRaises(PolicyInputError):
            self.evaluate(case(candidate("hit")), {"hit": {"nextTurn": None}})
        with self.assertRaises(PolicyInputError):
            self.evaluate(case(candidate("hit")), {"hit": {}}, hold={"inPlaceThreats": [], "nextTurn": None})


class FixtureTests(unittest.TestCase):
    """F01~F06의 게임 AI 선택(2026-11-13 손 계산과 같은 값). 덧붙임 파일이나 식이 바뀌면 여기서 드러난다."""

    EXPECTED = {
        "asset": {"F01": "a_kill_sniper", "F02": "a_hit_main", "F03": "a_inplace_akimbo", "F04": "a_hit_main",
                  "F04b": "a_hit_main", "F05": "b_safe_akimbo", "F06": "a_hit_main"},
        "codeDefault": {"F01": "a_kill_sniper", "F02": "b_hit_shotgun", "F03": "b_move_main", "F04": "a_hit_main",
                        "F04b": "a_hit_main", "F05": "b_safe_akimbo", "F06": "a_hit_main"},
    }

    def test_fixture_choices(self):
        for profile, expected in self.EXPECTED.items():
            weights = load_weights(profile=profile)
            for path in sorted((ROOT / "rule_fixtures_game").glob("*.game.json")):
                base, extra, _ = load_pair(path)
                with self.subTest(profile=profile, case=base["situationId"]):
                    self.assertEqual(evaluate_game_case(copy.deepcopy(base), extra, weights)["choice"],
                                     expected[base["situationId"]])


if __name__ == "__main__":
    unittest.main()
