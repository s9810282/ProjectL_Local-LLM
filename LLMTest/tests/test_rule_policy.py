"""공식의 경계와 입력 계약을 확인하는 독립 unittest."""

from __future__ import annotations

import copy
from decimal import Decimal
import io
from pathlib import Path
import sys
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))

from rule_policy import PolicyInputError, evaluate_case, load_json, validate_weights


def candidate(candidate_id="wait", **updates):
    value = {
        "id": candidate_id,
        "label": candidate_id,
        "legal": True,
        "actions": [{"kind": "wait", "cost": 0, "refund": 0, "steps": 0}],
        "selfHpAfter": 5,
        "headquartersHpAfter": 20,
        "playerHpAfter": {"main": 50, "regular": 10},
        "allyHpAfter": {},
        "approachCostAfter": 5,
        "hitIds": [],
        "roleFacts": {"canThreatenNext": False, "nearestPlayerDistance": 5, "knockbackDelays": []},
        "responseWindow": {
            "complete": True,
            "assumptions": "검증용 고정 관측",
            "opportunities": [],
            "nonThreats": [{"opponentId": unit, "reason": "다음 자기 행동 전 공격 기회 없음"} for unit in ("main", "regular")],
        },
    }
    value.update(updates)
    return value


def case_with(*candidates, hp=5, max_hp=5, ap=5, unit="Demon"):
    return {
        "schemaVersion": 1,
        "situationId": "TEST",
        "title": "독립 경계 검증",
        "source": "synthetic",
        "assumptions": ["실제 게임의 기하·최종 피해를 검증하는 상황이 아님"],
        "self": {"unit": unit, "hp": hp, "maxHp": max_hp, "ap": ap, "maxAp": 5, "moveRange": 3},
        "players": [{"id": "main", "role": "main", "hp": 50, "maxHp": 50}, {"id": "regular", "role": "regular", "hp": 10, "maxHp": 10}],
        "allies": [],
        "headquarters": {"hp": 20, "maxHp": 20},
        "approachCostBefore": 5,
        "expected": {"victory": [candidates[0]["id"]], "role": [candidates[0]["id"]], "reason": "평가기에서 읽지 않는 시험용 기대값"},
        "candidates": list(candidates),
    }


def response(opportunities):
    return {
        "complete": True,
        "assumptions": "공격 동안 상태·비용·피해가 불변인 시험 일정",
        "opportunities": opportunities,
        "nonThreats": [{"opponentId": "main", "reason": "제자리 공격 불가"}],
    }


def attack(cost=1, refund=0):
    return {"kind": "attack", "cost": cost, "refund": refund, "steps": 0}


class RulePolicyTests(unittest.TestCase):
    def setUp(self):
        self.settings = load_json(ROOT / "config" / "rule_weights.json")

    def evaluate(self, case, settings=None):
        return evaluate_case(copy.deepcopy(case), copy.deepcopy(settings or self.settings))

    @staticmethod
    def by_id(result, candidate_id):
        return next(item for item in result["candidates"] if item["id"] == candidate_id)

    def test_expected_selection_does_not_change_policy(self):
        high = candidate("high", actions=[attack()], playerHpAfter={"main": 44, "regular": 10}, hitIds=["main"])
        low = candidate("low")
        case = case_with(high, low)
        original = self.evaluate(case)["choices"]
        case["expected"] = {"victory": ["low"], "role": ["low"], "reason": "고의로 다른 기대값"}
        self.assertEqual(original, self.evaluate(case)["choices"])

    def test_bool_numeric_and_missing_observation_are_rejected(self):
        case = case_with(candidate())
        case["self"]["hp"] = True
        with self.assertRaises(PolicyInputError):
            self.evaluate(case)
        case = case_with(candidate())
        del case["candidates"][0]["approachCostAfter"]
        with self.assertRaises(PolicyInputError):
            self.evaluate(case)

    def test_nonfinite_json_and_invalid_weights_are_rejected(self):
        with patch.object(Path, "open", return_value=io.StringIO('{"value": NaN}')):
            with self.assertRaises(ValueError):
                load_json("invalid.json")
        for key, invalid in (("riskDiscount", Decimal("1.01")), ("mainReferenceHp", 0), ("roleBonus", True)):
            setting = copy.deepcopy(self.settings)
            setting[key] = invalid
            with self.subTest(key=key), self.assertRaises(PolicyInputError):
                validate_weights(setting)

    def test_duplicate_json_keys_are_rejected(self):
        with patch.object(Path, "open", return_value=io.StringIO('{"hp": 1, "hp": 2}')):
            with self.assertRaises(PolicyInputError):
                load_json("duplicate.json")

    def test_insufficient_ap_excludes_candidate_without_future_refund(self):
        bad = candidate("bad", actions=[attack(4, refund=2)])
        result = self.evaluate(case_with(bad, candidate("wait"), ap=3))
        self.assertFalse(self.by_id(result, "bad")["eligible"])
        self.assertEqual("wait", result["choices"]["victory"])

    def test_move_then_attack_budget_and_move_range(self):
        bad = candidate("move_attack", actions=[{"kind": "move", "cost": 3, "refund": 0, "steps": 1}, attack(4)])
        too_far = candidate("too_far", actions=[{"kind": "move", "cost": 1, "refund": 0, "steps": 4}])
        result = self.evaluate(case_with(bad, too_far, candidate("wait"), unit="Ballista"))
        self.assertFalse(self.by_id(result, "move_attack")["eligible"])
        self.assertFalse(self.by_id(result, "too_far")["eligible"])
        self.assertEqual("wait", result["choices"]["victory"])

    def test_fixed_response_uses_available_ap(self):
        window = response([{"opponentId": "regular", "availableAp": 8, "maxAp": 8, "mode": "fixed", "repeatSafe": True, "attackCost": 3, "damagePerAttack": 4}])
        item = candidate(selfHpAfter=10, responseWindow=window)
        result = self.evaluate(case_with(item, hp=10, max_hp=10))
        observed = result["candidates"][0]
        self.assertEqual(8, observed["response"]["appliedDamage"])
        self.assertEqual(2, observed["response"]["attacksApplied"])
        self.assertEqual(Decimal("0.4"), observed["features"]["R"])
        legacy = copy.deepcopy(self.settings)
        legacy["riskMode"] = "single_attack"
        legacy_result = self.evaluate(case_with(item, hp=10, max_hp=10), legacy)
        self.assertEqual(4, legacy_result["candidates"][0]["response"]["appliedDamage"])

    def test_sequence_refund_is_applied_after_payment(self):
        window = response([{"opponentId": "regular", "availableAp": 4, "maxAp": 5, "mode": "sequence", "attacks": [{"cost": 4, "damage": 1, "refund": 2}, {"cost": 2, "damage": 1, "refund": 0}]}])
        result = self.evaluate(case_with(candidate(responseWindow=window)))
        self.assertEqual(2, result["candidates"][0]["response"]["attacksApplied"])
        window["opportunities"][0]["availableAp"] = 3
        with self.assertRaises(PolicyInputError):
            self.evaluate(case_with(candidate(responseWindow=window)))

    def test_same_opponent_multiple_opportunities_use_each_ap_budget(self):
        opportunities = [
            {"opponentId": "regular", "availableAp": 2, "maxAp": 4, "mode": "fixed", "repeatSafe": True, "attackCost": 2, "damagePerAttack": 2},
            {"opponentId": "regular", "availableAp": 4, "maxAp": 4, "mode": "fixed", "repeatSafe": True, "attackCost": 2, "damagePerAttack": 2},
        ]
        case = case_with(candidate(selfHpAfter=10, responseWindow=response(opportunities)), hp=10, max_hp=10)
        revised = self.evaluate(case)["candidates"][0]
        self.assertEqual(6, revised["response"]["appliedDamage"])
        self.assertEqual(3, revised["response"]["attacksApplied"])
        self.assertEqual(Decimal("0.3"), revised["features"]["R"])
        old = copy.deepcopy(self.settings)
        old["riskMode"] = "single_attack"
        self.assertEqual(2, self.evaluate(case, old)["candidates"][0]["response"]["appliedDamage"])

    def test_response_sequence_preserves_changing_cost_damage_and_refund(self):
        window = response([{
            "opponentId": "regular", "availableAp": 5, "maxAp": 5, "mode": "sequence",
            "attacks": [
                {"cost": 3, "damage": 1, "refund": 1},
                {"cost": 2, "damage": 4, "refund": 0},
                {"cost": 1, "damage": 2, "refund": 0},
            ],
        }])
        result = self.evaluate(case_with(candidate(selfHpAfter=10, responseWindow=window), hp=10, max_hp=10))["candidates"][0]
        self.assertEqual(7, result["response"]["appliedDamage"])
        self.assertEqual(3, result["response"]["attacksApplied"])
        self.assertEqual([3, 1, 0], [entry["apAfter"] for entry in result["response"]["trace"] if "apAfter" in entry])
        self.assertEqual(Decimal("0.35"), result["features"]["R"])

    def test_killed_opponent_is_removed_from_response(self):
        window = response([{"opponentId": "regular", "availableAp": 5, "maxAp": 5, "mode": "fixed", "repeatSafe": True, "attackCost": 1, "damagePerAttack": 100}])
        item = candidate("kill", actions=[attack()], playerHpAfter={"main": 50, "regular": 0}, hitIds=["regular"], responseWindow=window)
        result = self.evaluate(case_with(item))
        self.assertEqual(0, result["candidates"][0]["response"]["appliedDamage"])
        self.assertEqual(0, result["candidates"][0]["features"]["R"])

    def test_knockback_requires_unique_known_surviving_target(self):
        delay = {"targetId": "regular", "before": 0, "after": 5}
        item = candidate(roleFacts={"canThreatenNext": False, "nearestPlayerDistance": 5, "knockbackDelays": [delay]})
        valid = self.evaluate(case_with(item, unit="Gravekeeper"))
        self.assertGreater(valid["candidates"][0]["features"]["Q"], 0)
        invalid_delays = [
            [{"before": 0, "after": 5}],
            [copy.deepcopy(delay), copy.deepcopy(delay)],
            [{"targetId": "unknown", "before": 0, "after": 5}],
        ]
        for delays in invalid_delays:
            bad = copy.deepcopy(item)
            bad["roleFacts"]["knockbackDelays"] = delays
            with self.subTest(delays=delays), self.assertRaises(PolicyInputError):
                self.evaluate(case_with(bad, unit="Gravekeeper"))
        dead = copy.deepcopy(item)
        dead.update(actions=[attack()], playerHpAfter={"main": 50, "regular": 0}, hitIds=["regular"])
        with self.assertRaises(PolicyInputError):
            self.evaluate(case_with(dead, unit="Gravekeeper"))

    def test_incomplete_response_and_missing_live_player_are_unevaluated(self):
        item = candidate()
        item["responseWindow"]["complete"] = False
        with self.assertRaises(PolicyInputError):
            self.evaluate(case_with(item))
        item = candidate()
        item["responseWindow"]["nonThreats"].pop()
        with self.assertRaises(PolicyInputError):
            self.evaluate(case_with(item))

    def test_terminal_main_death_precedes_simultaneous_headquarters_death(self):
        terminal = candidate("terminal", actions=[attack()], playerHpAfter={"main": 0, "regular": 10}, hitIds=["main"], headquartersHpAfter=0)
        result = self.evaluate(case_with(candidate("ongoing"), terminal))
        self.assertEqual("enemy_win", self.by_id(result, "terminal")["outcome"])
        self.assertEqual({"victory": "terminal", "role": "terminal"}, result["choices"])
        self.assertFalse(self.by_id(result, "ongoing")["roleEligible"])

    def test_suicide_does_not_remove_self_loss_cost(self):
        window = response([{"opponentId": "regular", "availableAp": 1, "maxAp": 1, "mode": "fixed", "repeatSafe": True, "attackCost": 1, "damagePerAttack": 10}])
        alive = candidate("alive", selfHpAfter=1, responseWindow=copy.deepcopy(window))
        dead = candidate("dead", selfHpAfter=0, responseWindow=copy.deepcopy(window))
        result = self.evaluate(case_with(alive, dead, hp=1, max_hp=5))
        self.assertEqual(Decimal("0.6"), self.by_id(result, "alive")["features"]["R"])
        self.assertEqual(Decimal("1.2"), self.by_id(result, "dead")["features"]["R"])
        self.assertEqual("alive", result["choices"]["victory"])

    def test_role_regret_gate_uses_same_victory_score(self):
        high = candidate("high", actions=[attack()], playerHpAfter={"main": 50, "regular": 9}, hitIds=["regular"], approachCostAfter=0)
        role = candidate("role", approachCostAfter=0, roleFacts={"canThreatenNext": True, "nearestPlayerDistance": 5, "knockbackDelays": []})
        case = case_with(high, role)
        case["approachCostBefore"] = 0
        result = self.evaluate(case)
        self.assertEqual({"victory": "high", "role": "role"}, result["choices"])
        setting = copy.deepcopy(self.settings)
        setting["roleMaxLoss"] = 0
        limited = self.evaluate(case, setting)
        self.assertEqual({"victory": "high", "role": "high"}, limited["choices"])
        self.assertFalse(self.by_id(limited, "role")["roleEligible"])

    def test_candidate_order_and_irrelevant_candidate_do_not_change_scores(self):
        high = candidate("high", actions=[attack()], playerHpAfter={"main": 44, "regular": 10}, hitIds=["main"])
        low = candidate("low")
        first = self.evaluate(case_with(high, low))
        reordered = self.evaluate(case_with(low, high))
        extra = candidate("excluded", legal=False, legalityNote="명시적인 시험 제외")
        extended = self.evaluate(case_with(high, low, extra))
        self.assertEqual(first["choices"], reordered["choices"])
        self.assertEqual(first["choices"], extended["choices"])
        for candidate_id in ("high", "low"):
            self.assertEqual(self.by_id(first, candidate_id)["victoryScore"], self.by_id(extended, candidate_id)["victoryScore"])

    def test_no_legal_candidates_and_duplicate_ids_are_rejected(self):
        with self.assertRaises(PolicyInputError):
            self.evaluate(case_with(candidate(legal=False, legalityNote="시험 제외")))
        with self.assertRaises(PolicyInputError):
            self.evaluate(case_with(candidate("same"), candidate("same")))


if __name__ == "__main__":
    unittest.main()
