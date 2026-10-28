"""LLM 투영이 정답을 흘리지 않고 룰 평가기와 같은 사실을 쓰는지 확인하는 unittest. Ollama 서버는 필요 없다."""

from __future__ import annotations

import json
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))

from llm_projection import project
from rule_policy import PolicyInputError, load_json
from run_ollama_test import validate

UNEVALUATED = {"R12", "R15"}


class LlmProjectionTests(unittest.TestCase):
    def setUp(self):
        self.settings = load_json(ROOT / "config" / "rule_weights.json")
        self.cases = {p.stem: load_json(p) for p in sorted((ROOT / "rule_fixtures").glob("R*.json"))}

    def test_view_hides_labels_expected_and_original_ids(self):
        for sid, case in self.cases.items():
            if sid in UNEVALUATED:
                continue
            with self.subTest(sid):
                view, aliases, _ = project(case, self.settings)
                text = json.dumps(view, ensure_ascii=False)
                for candidate in case["candidates"]:
                    self.assertNotIn(candidate["id"], text)
                    self.assertNotIn(candidate["label"], text)
                self.assertNotIn(case["expected"]["reason"], text)
                self.assertEqual(list(aliases), [f"c{i}" for i in range(1, len(aliases) + 1)])

    def test_aliases_follow_file_order_of_eligible_candidates(self):
        view, aliases, result = project(self.cases["R11"], self.settings)
        eligible = [c["id"] for c in result["candidates"] if c["eligible"]]
        self.assertEqual(list(aliases.values()), eligible)
        self.assertNotIn("a_unaffordable_move_attack", aliases.values())
        self.assertEqual([c["id"] for c in view["candidates"]], list(aliases))

    def test_next_turn_damage_matches_rule_schedule(self):
        for sid, expected in (("R19", (10, 0, True)), ("R20", (2, 8, False))):
            with self.subTest(sid):
                view, aliases, _ = project(self.cases[sid], self.settings)
                shot = next(c for c in view["candidates"] if aliases[c["id"]] == "a_lane_shot")
                after = shot["beforeMyNextTurn"]
                self.assertEqual((after["expectedDamage"], after["hpAfter"], after["dies"]), expected)

    def test_no_next_turn_after_death_or_battle_end(self):
        view, aliases, _ = project(self.cases["R18"], self.settings)
        dying = next(c for c in view["candidates"] if aliases[c["id"]] == "b_regular_kill_die")
        self.assertTrue(dying["self"]["diesNow"])
        self.assertIsNone(dying["beforeMyNextTurn"])
        view, aliases, _ = project(self.cases["R01"], self.settings)
        win = next(c for c in view["candidates"] if aliases[c["id"]] == "a_main_kill")
        self.assertEqual(win["battle"], "enemy_wins")
        self.assertIsNone(win["beforeMyNextTurn"])

    def test_hits_list_each_damaged_player(self):
        view, aliases, _ = project(self.cases["R22"], self.settings)
        lane = next(c for c in view["candidates"] if aliases[c["id"]] == "b_regular_first")
        self.assertEqual({h["id"]: (h["hpAfter"], h["killed"]) for h in lane["hits"]},
                         {"main": (46, False), "regular_a": (0, True), "regular_b": (3, False)})

    def test_unevaluated_cases_are_not_projected(self):
        for sid in sorted(UNEVALUATED):
            with self.subTest(sid), self.assertRaises(PolicyInputError):
                project(self.cases[sid], self.settings)

    @staticmethod
    def _facts_by_original(view, aliases):
        return {aliases[c["id"]]: {k: v for k, v in c.items() if k != "id"} for c in view["candidates"]}

    def test_reorder_changes_only_position_and_alias(self):
        base_view, base_aliases, base_result = project(self.cases["R16"], self.settings)
        for order, seed in (("reverse", 0), ("shuffle", 1), ("shuffle", 2)):
            with self.subTest(order=order, seed=seed):
                view, aliases, result = project(self.cases["R16"], self.settings, order, seed)
                self.assertEqual(self._facts_by_original(view, aliases), self._facts_by_original(base_view, base_aliases))
                self.assertEqual(result["choices"], base_result["choices"])
                self.assertEqual(list(aliases), ["c1", "c2", "c3"])
        _, reversed_aliases, _ = project(self.cases["R16"], self.settings, "reverse")
        self.assertEqual(list(reversed_aliases.values()), list(reversed(list(base_aliases.values()))))

    def test_shuffle_is_fixed_by_seed(self):
        first = project(self.cases["R16"], self.settings, "shuffle", 7)[1]
        again = project(self.cases["R16"], self.settings, "shuffle", 7)[1]
        self.assertEqual(first, again)
        with self.assertRaises(ValueError):
            project(self.cases["R16"], self.settings, "random")

    def test_validate_accepts_only_listed_string_ids(self):
        ids = ["c1", "c2"]
        self.assertEqual(validate('{"scores":[3,7],"choice":"c2","reason":"x"}', 2, "stop", ids)[1], "")
        self.assertEqual(validate('{"scores":[3,7],"choice":"c3","reason":"x"}', 2, "stop", ids)[1], "choice")
        self.assertEqual(validate('{"scores":[3,7],"choice":1,"reason":"x"}', 2, "stop", ids)[1], "choice")


if __name__ == "__main__":
    unittest.main()
