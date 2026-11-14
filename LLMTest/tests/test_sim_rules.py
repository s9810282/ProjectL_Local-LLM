"""시뮬레이터 S-b1: 이동·공격 모양·벽 시야·피해·턴·승패.

기대값은 게임 Core 식으로 손 계산했다(각 시험 주석). 유닛 수치는 게임 에셋에서 읽으므로,
에셋 수치가 바뀌면 손 계산도 다시 해야 한다.
"""

from __future__ import annotations

from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from sim.assets import (EnemyPlacement, GameAssets, Objective, PlayerLoadout, StageDef, StructureDef,  # noqa: E402
                        TerrainDef, TerrainKind, TerrainPlacement)
from sim.rules import (Attack, EndTurn, Move, RuleError, apply, hit_damage, legal_actions, reachable,  # noqa: E402
                       resolve_attack, start_battle)
from sim.state import ActionUse, Outcome, new_battle  # noqa: E402
from sim.targeting import Side, direction_offsets  # noqa: E402
from sim.assets import Shape  # noqa: E402
from sim.walls import WallGeometry  # noqa: E402

ASSETS = GameAssets()
U = ASSETS.unit_named
TERRAIN = {kind: TerrainDef(kind.name, kind, 2 if kind is TerrainKind.WEAK_GROUND else 0) for kind in TerrainKind}


def battle(players, enemies, *, size=(9, 9), terrain=(), hq=None, objective=Objective.HEADQUARTERS,
           reinforcement=None):
    """작은 가상 판. players/enemies = [(유닛 이름, 칸)], players[0]이 메인. terrain = [(종류, 칸)]."""
    width, height = size
    hq_def = StructureDef("Structure_Headquarters", 20, (2, 2), 30, 0) if hq else None
    stage = StageDef(name="T", width=width, height=height, objective=objective, enemy_level=1, seed=1,
                     enemies=tuple(EnemyPlacement(U(name), cell, 0, False) for name, cell in enemies),
                     headquarters=hq_def, headquarters_anchor=hq,
                     deployment_cells=frozenset((x, y) for x in range(width) for y in range(height)),
                     terrain=tuple(TerrainPlacement(TERRAIN[kind], cell, i + 1, (1, 2))
                                   for i, (kind, cell) in enumerate(terrain)),
                     reinforcement=reinforcement, rail_cells=(), gimmicks=frozenset(), corner_cut_ticks=128)
    loadout = PlayerLoadout("T", U(players[0][0]), tuple(U(name) for name, _ in players[1:]), len(players))
    state = new_battle(stage, loadout, {index: cell for index, (_, cell) in enumerate(players)})
    return start_battle(state)


def unit_named(state, name, side=None):
    return next(u for u in state.units.values() if u.name == name and (side is None or u.side is side))


def to_enemy_turn(state):
    return apply(state, EndTurn())


class DamageTests(unittest.TestCase):
    def damage(self, attacker, victim, terrain=()):
        state = to_enemy_turn(battle([(victim, (4, 3))], [(attacker, (4, 4))], terrain=terrain))
        a, v = unit_named(state, attacker), unit_named(state, victim)
        plan = resolve_attack(state, a, v.cell if a.defn.targeting.choice.name == "SINGLE" else "Down")
        hit = next(h for h in plan.hits if h.target == v.uid)
        return hit_damage(state, a, hit, a.attack)

    def test_defense_and_matchup(self):
        # Demon(일반, 공 8) → Akimbo(일반, 방 5): 8 → 8×95/100 = 7.6 → 7
        self.assertEqual(self.damage("Unit_Demon", "Unit_Akimbo"), 7)
        # Gravekeeper(기병, 공 12) → ShotGun(중장, 방 6): 상성 0.7 → (84+5)//10 = 8 → 8×94/100 = 7.52 → 7
        self.assertEqual(self.damage("Unit_Gravekeeper", "Unit_ShotGun"), 7)
        # UndeadHeavy(중장, 공 6) → Cowboy(기병, 방 3): 상성 1.5 → (90+5)//10 = 9 → 9×97/100 = 8.73 → 8
        self.assertEqual(self.damage("Unit_UndeadHeavy", "Unit_Cowboy"), 8)

    def test_high_ground_takes_seventy_percent(self):
        # 위 Demon → Akimbo 7에서, 맞는 쪽만 고지대면 7×70/100 = 4.9 → 4
        self.assertEqual(self.damage("Unit_Demon", "Unit_Akimbo", [(TerrainKind.HIGH_GROUND, (4, 3))]), 4)
        # 둘 다 고지대면 보정 없음
        self.assertEqual(self.damage("Unit_Demon", "Unit_Akimbo",
                                     [(TerrainKind.HIGH_GROUND, (4, 3)), (TerrainKind.HIGH_GROUND, (4, 4))]), 7)

    def test_cannon_splash_hits_friends_at_splash_percent(self):
        # UndeadCannon(대포, 공 15, 스플래시 50%)이 (4,2)를 조준: 옆 칸 Demon(아군)과 Ballista도 맞는다.
        state = to_enemy_turn(battle([("Unit_Akimbo", (8, 0)), ("Unit_Ballista", (5, 2))],
                                     [("Unit_UndeadCannon", (4, 5)), ("Unit_Demon", (3, 2))]))
        cannon = unit_named(state, "Unit_UndeadCannon")
        plan = resolve_attack(state, cannon, (4, 2))     # 빈 칸 조준도 된다
        damage = {state.units[h.target].name: hit_damage(state, cannon, h, cannon.attack) for h in plan.hits}
        # Ballista(대포, 방 2): 15×50/100 = 7 → 상성 1.0 → 7×98/100 = 6.86 → 6
        # Demon(일반, 방 3): 7 → 7×97/100 = 6.79 → 6
        self.assertEqual(damage, {"Unit_Ballista": 6, "Unit_Demon": 6})

    def test_cannon_cannot_aim_adjacent(self):
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 3))], [("Unit_UndeadCannon", (4, 4))]))
        with self.assertRaises(RuleError):
            resolve_attack(state, unit_named(state, "Unit_UndeadCannon"), (4, 3))


class ShapeTests(unittest.TestCase):
    def test_fan_direction(self):
        self.assertEqual(sorted(direction_offsets(Shape.FAN, 2, "Up")), [(-1, 2), (0, 1), (0, 2), (1, 2)])

    def test_heavy_front_angle_for_enemy_points_down(self):
        # UndeadHeavy: 8방향 직선, 한 칸 고르기, 정면만. 적의 정면은 -y.
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 3)), ("Unit_ShotGun", (4, 5))],
                                     [("Unit_UndeadHeavy", (4, 4))]))
        heavy = unit_named(state, "Unit_UndeadHeavy")
        resolve_attack(state, heavy, (4, 3))                 # 정면(아래)은 된다
        with self.assertRaises(RuleError):
            resolve_attack(state, heavy, (4, 5))             # 뒤(위)는 안 된다

    def test_gravekeeper_fan_hits_every_cell(self):
        # Gravekeeper: 부채꼴, 방향 고르기, 사거리 1 → 그 방향 한 칸
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 3)), ("Unit_ShotGun", (5, 3))],
                                     [("Unit_Gravekeeper", (4, 4))]))
        plan = resolve_attack(state, unit_named(state, "Unit_Gravekeeper"), "Down")
        self.assertEqual([state.units[h.target].name for h in plan.hits], ["Unit_Akimbo"])


class WallTests(unittest.TestCase):
    def test_straight_line_blocked(self):
        walls = WallGeometry([(1, 0)], 128)
        self.assertTrue(walls.blocks((0, 0), (2, 0)))

    def test_diagonal_past_corner_is_open(self):
        # 칸 (1,0)의 벽 모서리를 스치는 대각선 (0,0)→(1,1)은 열린 경계라 막히지 않는다.
        walls = WallGeometry([(1, 0)], 128)
        self.assertFalse(walls.blocks((0, 0), (1, 1)))

    def test_sniper_blocked_but_cannon_lobs(self):
        terrain = [(TerrainKind.WALL, (4, 3))]
        state = battle([("Unit_Sniper", (4, 2))], [("Unit_Demon", (4, 4)), ("Unit_UndeadCannon", (8, 8))],
                       terrain=terrain)
        with self.assertRaises(RuleError):
            resolve_attack(state, unit_named(state, "Unit_Sniper"), (4, 4))
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 2))], [("Unit_UndeadCannon", (4, 5))], terrain=terrain))
        plan = resolve_attack(state, unit_named(state, "Unit_UndeadCannon"), (4, 2))
        self.assertEqual(len(plan.hits), 1)

    def test_high_ground_shoots_over_walls(self):
        terrain = [(TerrainKind.WALL, (4, 3)), (TerrainKind.HIGH_GROUND, (4, 2))]
        state = battle([("Unit_Sniper", (4, 2))], [("Unit_Demon", (4, 4))], terrain=terrain)
        plan = resolve_attack(state, unit_named(state, "Unit_Sniper"), (4, 4))
        self.assertEqual(len(plan.hits), 1)

    def test_diagonal_wall_touch_is_rejected(self):
        with self.assertRaises(ValueError):
            WallGeometry([(1, 0), (2, 1)], 128)


class MoveTests(unittest.TestCase):
    def test_units_and_walls_block(self):
        state = battle([("Unit_Akimbo", (0, 0)), ("Unit_Sniper", (1, 0))], [("Unit_Demon", (8, 8))],
                       terrain=[(TerrainKind.WALL, (0, 1))])
        akimbo = unit_named(state, "Unit_Akimbo")
        self.assertEqual(reachable(state, akimbo), {})       # 오른쪽은 아군, 위는 벽

    def test_move_budget_and_shortest(self):
        state = battle([("Unit_Akimbo", (4, 4))], [("Unit_Demon", (8, 8))])
        options = reachable(state, unit_named(state, "Unit_Akimbo"))
        self.assertEqual(len(options), 24)                   # 이동력 3: 마름모 25칸 - 자기 칸
        self.assertEqual(len(options[(4, 7)]) - 1, 3)

    def test_rider_leaps_over_units(self):
        # Cowboy(기병)는 플레이어 소유라 도약: 중간 칸의 유닛을 지나간다.
        state = battle([("Unit_Akimbo", (0, 8)), ("Unit_Cowboy", (4, 4)), ("Unit_Sniper", (4, 5))],
                       [("Unit_Demon", (8, 8))])
        options = reachable(state, unit_named(state, "Unit_Cowboy"))
        self.assertIn((4, 6), options)
        self.assertNotIn((4, 5), options)

    def test_avoids_weak_ground_when_possible(self):
        state = battle([("Unit_Akimbo", (0, 0))], [("Unit_Demon", (8, 8))],
                       terrain=[(TerrainKind.WEAK_GROUND, (1, 0))])
        options = reachable(state, unit_named(state, "Unit_Akimbo"))
        # (2,1)은 3칸 길이 여럿이고 그중 (0,1)→(1,1)로 돌아가면 약한 지반을 안 밟는다.
        self.assertNotIn((1, 0), options[(2, 1)])
        # (2,0)은 피해 가면 4칸이라 이동력 3으로는 약한 지반을 지날 수밖에 없다.
        self.assertIn((1, 0), options[(2, 0)])

    def test_move_then_attack_then_done(self):
        state = battle([("Unit_Akimbo", (4, 0))], [("Unit_Demon", (4, 4))])
        akimbo = unit_named(state, "Unit_Akimbo")
        state = apply(state, Move(akimbo.uid, reachable(state, akimbo)[(4, 2)]))
        self.assertIs(state.units[akimbo.uid].use, ActionUse.MOVED)
        state = apply(state, Attack(akimbo.uid, (4, 4)))     # Akimbo 사거리 2
        self.assertIs(state.units[akimbo.uid].use, ActionUse.ATTACKED)
        self.assertFalse(any(getattr(a, "uid", None) == akimbo.uid for a in legal_actions(state)))


class TurnAndOutcomeTests(unittest.TestCase):
    def test_enemy_starts_deferred_then_available(self):
        state = battle([("Unit_Akimbo", (0, 0))], [("Unit_Demon", (8, 8))])
        self.assertIs(unit_named(state, "Unit_Demon").use, ActionUse.DEFERRED)
        state = apply(state, EndTurn())
        self.assertIs(state.turn_side, Side.ENEMY)
        self.assertIs(unit_named(state, "Unit_Demon").use, ActionUse.AVAILABLE)
        state = apply(state, EndTurn())
        self.assertEqual((state.turn_side, state.round), (Side.PLAYER, 2))

    def test_destroying_headquarters_wins(self):
        state = battle([("Unit_Akimbo", (4, 2)), ("Unit_ShotGun", (3, 2))], [("Unit_Demon", (8, 0))], hq=(4, 3))
        hq = next(iter(state.structures.values()))
        hq.hp = 1
        state = apply(state, Attack(unit_named(state, "Unit_Akimbo").uid, (4, 3)))
        self.assertIs(state.outcome, Outcome.PLAYER_WIN)
        self.assertEqual(legal_actions(state), [])

    def test_main_death_loses(self):
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 3))], [("Unit_Demon", (4, 4))]))
        unit_named(state, "Unit_Akimbo").hp = 1
        state = apply(state, Attack(unit_named(state, "Unit_Demon").uid, (4, 3)))
        self.assertIs(state.outcome, Outcome.ENEMY_WIN)

    def test_apply_does_not_change_the_original(self):
        state = battle([("Unit_Akimbo", (4, 0))], [("Unit_Demon", (4, 4))])
        akimbo = unit_named(state, "Unit_Akimbo")
        apply(state, Move(akimbo.uid, reachable(state, akimbo)[(4, 2)]))
        self.assertEqual(state.units[akimbo.uid].cell, (4, 0))


if __name__ == "__main__":
    unittest.main()
