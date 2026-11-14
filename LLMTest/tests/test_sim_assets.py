"""시뮬레이터 S-a: Unity 에셋 파서와 게임 데이터 읽기를 확인한다.

실제 에셋 시험의 기대값은 2026-11-13 Combat_LD_P20.asset 원본 글자에서 옮겼다.
게임 데이터가 바뀌면 이 값도 바뀔 수 있다(그때는 원본을 다시 보고 고친다).
"""

from __future__ import annotations

from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from sim.assets import (AssetError, Category, ChoiceAngle, ChoiceMode, GameAssets, Objective, Shape,  # noqa: E402
                        Splash, TerrainKind, WallDelivery, _known_to)
from sim.render import render_stage, stage_problems  # noqa: E402
from sim.unity_yaml import UnityYamlError, parse  # noqa: E402

SAMPLE = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_Name: Sample
  ref: {fileID: 11400000, guid: abc, type: 2}
  empty: []
  nothing: {fileID: 0}
  items:
  - unit: {fileID: 1, guid: u1, type: 2}
    anchor:
      x: 5
      y: -2
    rotation: 0
  - unit: {fileID: 1, guid: u2, type: 2}
    anchor:
      x: 1
      y: 0
    rotation: 3
  plain:
  - 7
  - text
  ratio: 0.25
  hex: 0100000002000000
  nested:
    deep:
      value: 1
  after: 9
"""


class UnityYamlTests(unittest.TestCase):
    def test_parses_unity_shapes(self):
        data = parse(SAMPLE)
        self.assertEqual(data["m_Name"], "Sample")
        self.assertEqual(data["ref"], {"fileID": 11400000, "guid": "abc", "type": 2})
        self.assertEqual(data["empty"], [])
        self.assertEqual(data["nothing"], {"fileID": 0})
        # 목록 대시가 부모 키와 같은 들여쓰기여도 목록으로 읽고, 목록 뒤 키로 돌아온다.
        self.assertEqual([item["anchor"] for item in data["items"]], [{"x": 5, "y": -2}, {"x": 1, "y": 0}])
        self.assertEqual(data["items"][1]["rotation"], 3)
        self.assertEqual(data["plain"], [7, "text"])
        self.assertEqual(data["ratio"], 0.25)
        self.assertEqual(data["hex"], "0100000002000000")   # 0으로 시작하는 글자는 숫자로 바꾸지 않는다
        self.assertEqual(data["nested"], {"deep": {"value": 1}})
        self.assertEqual(data["after"], 9)

    def test_rejects_unknown_shapes(self):
        with self.assertRaises(UnityYamlError):
            parse("MonoBehaviour:\n  a: 1\n  key without colon\n")
        with self.assertRaises(UnityYamlError):
            parse("")
        # 더 깊이 들여쓴 줄은 Unity가 긴 값을 여러 줄로 쓴 것이라 이어 붙인다.
        self.assertEqual(parse("MonoBehaviour:\n  text: first\n    second\n  b: 2\n"),
                         {"text": "first second", "b": 2})

    def test_known_to_is_little_endian_int_array(self):
        self.assertEqual(_known_to("0100000002000000"), (1, 2))
        self.assertEqual(_known_to("02000000"), (2,))
        with self.assertRaises(AssetError):
            _known_to("010")


class GameAssetTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.assets = GameAssets()

    def test_every_combat_stage_loads(self):
        names = self.assets.combat_stage_names()
        self.assertIn("Combat_LD_P20", names)
        for name in names:
            with self.subTest(stage=name):
                stage = self.assets.stage(name)
                self.assertEqual(stage_problems(stage), [])
                self.assertTrue(render_stage(stage))

    def test_p20_matches_asset(self):
        stage = self.assets.stage("Combat_LD_P20")
        self.assertEqual((stage.width, stage.height), (13, 13))
        self.assertIs(stage.objective, Objective.HEADQUARTERS)
        self.assertEqual([(e.unit.name, e.cell) for e in stage.enemies],
                         [("Unit_UndeadHeavy", (5, 5)), ("Unit_Demon_SelfDestruct", (8, 7)),
                          ("Unit_UndeadCannon", (2, 8)), ("Unit_Gravekeeper", (6, 9))])
        self.assertEqual(stage.headquarters.name, "Structure_Headquarters")
        self.assertEqual(stage.headquarters_cells(), ((6, 11), (7, 11), (6, 12), (7, 12)))
        self.assertEqual(stage.headquarters.max_hp, 30)
        self.assertEqual(len(stage.deployment_cells), 29)
        kinds = [p.terrain.kind for p in stage.terrain]
        self.assertEqual((kinds.count(TerrainKind.WALL), kinds.count(TerrainKind.WEAK_GROUND),
                          kinds.count(TerrainKind.CLIFF)), (31, 12, 5))
        self.assertEqual(stage.reinforcement.per_turn, 1)
        self.assertEqual(stage.reinforcement.field_limit, 4)
        self.assertEqual(len(stage.reinforcement.pool), 8)
        self.assertEqual(stage.gimmicks, frozenset())

    def test_gimmicks_are_detected(self):
        self.assertEqual(self.assets.stage("Combat_LD_P21").gimmicks, frozenset({"railway"}))
        self.assertIn("tornado", self.assets.stage("Combat_LD_P23").gimmicks)
        self.assertEqual(self.assets.stage("Combat_LD_P34").gimmicks, frozenset({"rockfall", "haboob"}))

    def test_movement_comes_from_category(self):
        moves = self.assets.movement_by_category
        self.assertEqual((moves[Category.MAIN], moves[Category.RIDER], moves[Category.HEAVY],
                          moves[Category.CANNON], moves[Category.BOSS]), (3, 4, 3, 2, 0))
        stage = self.assets.stage("Combat_LD_P20")
        cannon = next(e.unit for e in stage.enemies if e.unit.name == "Unit_UndeadCannon")
        self.assertEqual((cannon.category, cannon.move, cannon.attack_range), (Category.CANNON, 2, 3))
        self.assertIs(cannon.wall_delivery, WallDelivery.IGNORE_WALLS)
        self.assertEqual((cannon.targeting.shape, cannon.targeting.choice, cannon.targeting.splash,
                          cannon.targeting.splash_percent), (Shape.RADIAL_SQUARE, ChoiceMode.SINGLE, Splash.RADIAL, 50))

    def test_traits_and_angle(self):
        stage = self.assets.stage("Combat_LD_P20")
        heavy = next(e.unit for e in stage.enemies if e.unit.name == "Unit_UndeadHeavy")
        self.assertEqual([t.kind for t in heavy.traits], ["Regeneration"])
        self.assertIs(heavy.targeting.angle, ChoiceAngle.FRONT)
        keeper = next(e.unit for e in stage.enemies if e.unit.name == "Unit_Gravekeeper")
        self.assertIsNotNone(keeper.trait("Knockback"))
        self.assertIsNone(keeper.trait("Regeneration"))

    def test_boss_stage_has_victory_target(self):
        stage = self.assets.stage("Combat_LD_P35")
        self.assertIs(stage.objective, Objective.BOSS)
        targets = [e for e in stage.enemies if e.victory_target]
        self.assertEqual([(e.unit.name, e.cell) for e in targets], [("Unit_Stage1Boss", (7, 14))])
        self.assertIsNone(stage.headquarters)

    def test_player_loadout(self):
        loadout = self.assets.player_loadout()
        self.assertEqual(loadout.main.name, "Unit_Akimbo")
        self.assertEqual([unit.name for unit in loadout.subs], ["Unit_ShotGun", "Unit_Ballista", "Unit_Cowboy"])
        self.assertEqual(loadout.hand_size, 4)

    def test_unknown_stage_is_an_error(self):
        with self.assertRaises(AssetError):
            self.assets.stage("Combat_DoesNotExist")


if __name__ == "__main__":
    unittest.main()
