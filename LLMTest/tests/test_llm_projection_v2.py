"""진영 턴 상황(rule_fixtures_v2)의 LLM 화면 project_v2를 확인한다."""

from __future__ import annotations

import json
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))

from llm_projection import project
from rule_policy import load_json

SETTINGS = load_json(ROOT / "config" / "rule_weights_v3.json")
FIXTURES = sorted((ROOT / "rule_fixtures_v2").glob("*.json"))


def view_of(name, order="file"):
    return project(load_json(ROOT / "rule_fixtures_v2" / f"{name}.json"), SETTINGS, order)


def by_original(view, aliases):
    return {aliases[c["id"]]: c for c in view["candidates"]}


class ProjectionV2Tests(unittest.TestCase):
    def test_no_scores_labels_or_expectations_leak(self):
        for path in FIXTURES:
            case = load_json(path)
            view, _, _ = project(case, SETTINGS)
            text = json.dumps(view, ensure_ascii=False)
            for word in ("score", "Score", "expected", "label", "discount", "victory"):
                self.assertNotIn(word, text, f"{path.name}: {word}")
            for candidate in case["candidates"]:
                self.assertNotIn(candidate["id"], text, f"{path.name}: original id leaked")

    def test_dies_flags_follow_reach(self):
        view, aliases, _ = view_of("F02")
        c = by_original(view, aliases)
        self.assertTrue(c["a_hit_main"]["nextPlayerTurn"]["diesIfInPlaceAttack"])
        self.assertFalse(c["b_hit_shotgun"]["nextPlayerTurn"]["diesIfInPlaceAttack"])
        self.assertTrue(c["b_hit_shotgun"]["nextPlayerTurn"]["diesIfAllAttack"])
        self.assertEqual(c["b_hit_shotgun"]["nextPlayerTurn"]["afterMove"], {"attackers": 2, "damage": 15})
        self.assertEqual(c["b_hit_shotgun"]["action"], "move 2 -> attack")

    def test_hazard_counts_in_place(self):
        view, aliases, _ = view_of("F05")
        c = by_original(view, aliases)
        self.assertEqual(c["a_hazard_main"]["nextPlayerTurn"]["hazardDamage"], 15)
        self.assertTrue(c["a_hazard_main"]["nextPlayerTurn"]["diesIfInPlaceAttack"])
        self.assertEqual(view["announcedHazards"][0]["damage"], 15)

    def test_chain_is_shown_as_fact(self):
        view, aliases, _ = view_of("F06")
        c = by_original(view, aliases)
        self.assertTrue(c["a_hit_main"]["nextPlayerTurn"]["chainPossible"])
        self.assertFalse(c["a_hit_main"]["nextPlayerTurn"]["diesIfAllAttack"])

    def test_turn_context(self):
        view, _, _ = view_of("F06")
        self.assertEqual(view["turn"]["actedAllies"], [{"id": "e_demon", "summary": "대기"}])
        self.assertEqual(view["turn"]["remainingAfterYou"], 1)

    def test_reverse_keeps_same_facts(self):
        file_view, file_aliases, _ = view_of("F03")
        rev_view, rev_aliases, _ = view_of("F03", "reverse")
        strip = lambda c: {k: v for k, v in c.items() if k != "id"}
        a = {file_aliases[c["id"]]: strip(c) for c in file_view["candidates"]}
        b = {rev_aliases[c["id"]]: strip(c) for c in rev_view["candidates"]}
        self.assertEqual(a, b)
        self.assertNotEqual(list(file_aliases.values()), list(rev_aliases.values()))


if __name__ == "__main__":
    unittest.main()
