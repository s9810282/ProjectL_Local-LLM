"""rule-v3(진영 턴, 상황 형식 v2)의 경계와 입력 계약을 확인하는 unittest."""

from __future__ import annotations

import copy
from decimal import Decimal
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))

from rule_policy import PolicyInputError, evaluate_case, load_json, validate_weights

SETTINGS = load_json(ROOT / "config" / "rule_weights_v3.json")


def candidate(candidate_id="wait", **updates):
    value = {
        "id": candidate_id, "label": candidate_id, "legal": True, "kind": "wait", "moveSteps": 0,
        "selfHpAfter": 10, "headquartersHpAfter": 5,
        "playerHpAfter": {"main": 30, "sniper": 10}, "allyHpAfter": {},
        "approachCostAfter": 3, "hitIds": [],
        "roleFacts": {"canThreatenNext": False, "nearestPlayerDistance": 3, "knockbackDelays": []},
        "exposure": {"complete": True, "assumptions": "검증용", "threats": [], "hazardDamage": 0,
                     "nonThreats": [{"opponentId": p, "reason": "닿지 않음"} for p in ("main", "sniper")]},
    }
    value.update(updates)
    return value


def case(*candidates, **self_updates):
    actor = {"id": "e1", "unit": "Gravekeeper", "hp": 10, "maxHp": 20, "moveRange": 4, "attackRange": 1}
    actor.update(self_updates)
    return {
        "schemaVersion": 2, "situationId": "T", "title": "t", "source": "synthetic", "assumptions": ["검증용"],
        "turn": {"dayNight": "day", "enemyOrder": ["e1"], "actingId": "e1", "actedAllies": []},
        "self": actor,
        "players": [
            {"id": "main", "role": "main", "unit": "MainCharacter", "hp": 30, "maxHp": 30, "moveRange": 3, "attackRange": 1, "traits": []},
            {"id": "sniper", "role": "regular", "unit": "Sniper", "hp": 10, "maxHp": 10, "moveRange": 3, "attackRange": 4, "traits": []},
        ],
        "allies": [], "headquarters": {"hp": 5, "maxHp": 5}, "announcedHazards": [],
        "approachCostBefore": 3, "candidates": list(candidates) or [candidate()],
    }


def exposed(threats, non_threats=(), hazard=0, hazard_ids=()):
    return {"complete": True, "assumptions": "검증용", "threats": threats, "hazardDamage": hazard,
            "hazardIds": list(hazard_ids), "nonThreats": [{"opponentId": p, "reason": "닿지 않음"} for p in non_threats]}


def power(hp, maximum=20):
    return Decimal(0) if hp <= 0 else 1 + Decimal(hp) / maximum


class RuleV3Tests(unittest.TestCase):
    def test_settings_validate_through_v1_entry(self):
        validate_weights(copy.deepcopy(SETTINGS))

    def test_tiered_discounts(self):
        threats = [{"opponentId": "main", "reach": "in_place", "damage": 4},
                   {"opponentId": "sniper", "reach": "after_move", "damage": 4}]
        result = evaluate_case(case(candidate(exposure=exposed(threats))), copy.deepcopy(SETTINGS))
        loss = result["candidates"][0]["response"]["futurePowerLoss"]
        expected = Decimal("0.75") * (power(10) - power(6)) + Decimal("0.5") * (power(6) - power(2))
        self.assertEqual(loss, expected)

    def test_chain_is_not_scored_by_default(self):
        data = exposed([], ["main", "sniper"])
        data["chains"] = [{"opponentId": "sniper", "damage": 10, "note": "무작위"}]
        result = evaluate_case(case(candidate(exposure=data)), copy.deepcopy(SETTINGS))
        self.assertEqual(result["candidates"][0]["response"]["futurePowerLoss"], Decimal(0))
        self.assertEqual(result["candidates"][0]["response"]["chainDamage"], 10)

    def test_hazard_is_full_weight(self):
        data = case(candidate(exposure=exposed([], ["main", "sniper"], hazard=4, hazard_ids=["rock"])))
        data["announcedHazards"] = [{"id": "rock", "description": "낙석 예고", "damage": 4}]
        result = evaluate_case(data, copy.deepcopy(SETTINGS))
        self.assertEqual(result["candidates"][0]["response"]["futurePowerLoss"], power(10) - power(6))

    def test_hazard_requires_announced_id(self):
        with self.assertRaises(PolicyInputError):
            evaluate_case(case(candidate(exposure=exposed([], ["main", "sniper"], hazard=4))), copy.deepcopy(SETTINGS))

    def test_coverage_missing_is_rejected(self):
        with self.assertRaises(PolicyInputError):
            evaluate_case(case(candidate(exposure=exposed([], ["main"]))), copy.deepcopy(SETTINGS))

    def test_killed_player_is_dropped_from_threats(self):
        attack = candidate("hit", kind="attack", playerHpAfter={"main": 30, "sniper": 0}, hitIds=["sniper"],
                           exposure=exposed([{"opponentId": "sniper", "reach": "in_place", "damage": 9}], ["main"]))
        result = evaluate_case(case(attack), copy.deepcopy(SETTINGS))
        self.assertEqual(result["candidates"][0]["response"]["futurePowerLoss"], Decimal(0))

    def test_move_out_of_range_is_excluded(self):
        far = candidate("far", kind="move", moveSteps=5)
        result = evaluate_case(case(far, candidate()), copy.deepcopy(SETTINGS))
        self.assertFalse(result["candidates"][0]["eligible"])
        self.assertEqual(result["choices"]["victory"], "wait")

    def test_budget_is_optional_and_enforced(self):
        costly = candidate("costly", kind="move", moveSteps=2, budgetCost=4)
        cheap = candidate("cheap", budgetCost=0)
        result = evaluate_case(case(costly, cheap, budget=3), copy.deepcopy(SETTINGS))
        self.assertTrue(result["candidates"][0]["reason"].startswith("budget_exceeded"))

    def test_wait_cannot_damage(self):
        with self.assertRaises(PolicyInputError):
            evaluate_case(case(candidate(playerHpAfter={"main": 25, "sniper": 10})), copy.deepcopy(SETTINGS))

    def test_acted_allies_must_match_order(self):
        data = case()
        data["turn"] = {"dayNight": "day", "enemyOrder": ["e0", "e1"], "actingId": "e1", "actedAllies": []}
        with self.assertRaises(PolicyInputError):
            evaluate_case(data, copy.deepcopy(SETTINGS))


if __name__ == "__main__":
    unittest.main()
