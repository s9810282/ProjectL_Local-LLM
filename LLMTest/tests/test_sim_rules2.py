"""시뮬레이터 S-b2: 넉백·낭떠러지·약한 지반·자폭·특성·증원.

기대값은 게임 Core 식으로 손 계산했다(각 시험 주석). 유닛 수치는 게임 에셋에서 읽는다.
"""

from __future__ import annotations

from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tests"))
sys.path.insert(0, str(ROOT))

from test_sim_rules import U, battle, to_enemy_turn, unit_named  # noqa: E402
from sim.assets import Reinforcement, TerrainKind  # noqa: E402
from sim.rules import Attack, EndTurn, Move, apply, can_attack, can_move, reachable  # noqa: E402
from sim.state import ActionUse, Outcome  # noqa: E402
from sim.targeting import Side  # noqa: E402


def attack(state, name, aim, side=None):
    return apply(state, Attack(unit_named(state, name, side).uid, aim))


class KnockbackTests(unittest.TestCase):
    def test_gravekeeper_pushes_target_away(self):
        # Gravekeeper(공 12, 기병) → Akimbo(방 5): 12×95/100 = 11.4 → 11, 아래로 한 칸 밀림
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 3))], [("Unit_Gravekeeper", (4, 4))]))
        state = attack(state, "Unit_Gravekeeper", "Down")
        akimbo = unit_named(state, "Unit_Akimbo")
        self.assertEqual((akimbo.hp, akimbo.cell), (19, (4, 2)))

    def test_push_into_cliff_kills_and_main_death_loses(self):
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 3))], [("Unit_Gravekeeper", (4, 4))],
                                     terrain=[(TerrainKind.CLIFF, (4, 2))]))
        state = attack(state, "Unit_Gravekeeper", "Down")
        self.assertFalse(unit_named(state, "Unit_Akimbo").alive)
        self.assertIs(state.outcome, Outcome.ENEMY_WIN)

    def test_push_stops_at_wall_and_unit(self):
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 3)), ("Unit_Sniper", (4, 2))],
                                     [("Unit_Gravekeeper", (4, 4))]))
        state = attack(state, "Unit_Gravekeeper", "Down")
        self.assertEqual(unit_named(state, "Unit_Akimbo").cell, (4, 3))

    def test_player_heavy_is_steadfast(self):
        # ShotGun은 플레이어 중장이라 굳건한 의지로 넉백 면역
        state = to_enemy_turn(battle([("Unit_Akimbo", (0, 0)), ("Unit_ShotGun", (4, 3))],
                                     [("Unit_Gravekeeper", (4, 4))]))
        state = attack(state, "Unit_Gravekeeper", "Down")
        self.assertEqual(unit_named(state, "Unit_ShotGun").cell, (4, 3))

    def test_impact_bolt_pushes_splash_victims_outward(self):
        # Ballista(공 13, 대포, 스플래시 70%)가 (4,4) 조준: 주 대상 13×97/100 = 12, 옆 대상 13×70/100 = 9 → 9×97/100 = 8
        # 옆 대상은 충돌 중심 (4,4)에서 바깥(+x)으로 1칸, 주 대상(중심)은 그대로
        state = battle([("Unit_Akimbo", (0, 0)), ("Unit_Ballista", (4, 0))],
                       [("Unit_Demon", (4, 4)), ("Unit_Demon", (5, 4))])
        state = attack(state, "Unit_Ballista", (4, 4))
        demons = sorted((u for u in state.units.values() if u.name == "Unit_Demon"), key=lambda u: u.uid)
        self.assertEqual([(d.hp, d.cell) for d in demons], [(8, (4, 4)), (12, (6, 4))])


class TerrainTests(unittest.TestCase):
    def test_weak_ground_collapses_after_second_entry(self):
        state = battle([("Unit_Akimbo", (0, 0)), ("Unit_Sniper", (2, 0))], [("Unit_Demon", (8, 8))],
                       terrain=[(TerrainKind.WEAK_GROUND, (1, 1))])
        akimbo, sniper = unit_named(state, "Unit_Akimbo"), unit_named(state, "Unit_Sniper")
        state = apply(state, Move(akimbo.uid, ((0, 0), (0, 1), (1, 1), (1, 2))))   # 1번째 진입(지나감)
        self.assertEqual(state.weak_entries[(1, 1)], 1)
        state = apply(state, Move(sniper.uid, ((2, 0), (2, 1), (1, 1))))            # 2번째 진입 → 명령 끝에 무너짐
        self.assertIn((1, 1), state.cliffs)
        self.assertFalse(state.units[sniper.uid].alive)


class TraitTests(unittest.TestCase):
    def test_self_destruct_hits_all_neighbors_ignoring_defense(self):
        # Demon_SelfDestruct(공 10, 레벨 1 = 50%)가 죽으면 주변 8칸에 5, 방어 무시
        state = battle([("Unit_Akimbo", (0, 0)), ("Unit_Sniper", (4, 1)), ("Unit_ShotGun", (5, 4))],
                       [("Unit_Demon_SelfDestruct", (4, 4)), ("Unit_Demon", (3, 5))])
        unit_named(state, "Unit_Demon_SelfDestruct").hp = 1
        state = attack(state, "Unit_Sniper", (4, 4))
        self.assertEqual(unit_named(state, "Unit_ShotGun").hp, 35 - 5)
        self.assertEqual(unit_named(state, "Unit_Demon").hp, 20 - 5)

    def test_regeneration_heals_at_own_turn_end(self):
        state = to_enemy_turn(battle([("Unit_Akimbo", (0, 0))], [("Unit_UndeadHeavy", (8, 8))]))
        unit_named(state, "Unit_UndeadHeavy").hp = 20
        state = apply(state, EndTurn())
        self.assertEqual(unit_named(state, "Unit_UndeadHeavy").hp, 25)

    def test_stationary_fire_doubles_when_not_moved(self):
        # 제자리 사격 대포(공 15): 안 움직이면 30 → Akimbo 30×95/100 = 28, 움직이면 15 → 14
        def run(move_first):
            state = to_enemy_turn(battle([("Unit_Akimbo", (4, 2))], [("Unit_UndeadCannon_StationaryFire", (4, 4))]))
            cannon = unit_named(state, "Unit_UndeadCannon_StationaryFire")
            if move_first:
                state = apply(state, Move(cannon.uid, ((4, 4), (4, 5))))
            state = attack(state, "Unit_UndeadCannon_StationaryFire", (4, 2))
            return 30 - unit_named(state, "Unit_Akimbo").hp
        self.assertEqual((run(False), run(True)), (28, 14))

    def test_disarm_blocks_attacks_for_the_victims_next_turn(self):
        # UndeadRider_Disarm(대포 병과, 공 15, 넉백 1): Akimbo 14 피해, (4,2) → (4,1)로 밀림, 무장 해제
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 2))], [("Unit_UndeadRider_Disarm", (4, 4))]))
        state = attack(state, "Unit_UndeadRider_Disarm", (4, 2))
        akimbo = unit_named(state, "Unit_Akimbo")
        self.assertEqual((akimbo.hp, akimbo.cell), (16, (4, 1)))
        state = apply(state, EndTurn())                       # 플레이어 턴
        self.assertFalse(can_attack(state, unit_named(state, "Unit_Akimbo")))
        state = apply(apply(state, EndTurn()), EndTurn())     # 플레이어 턴 끝에 풀림 → 다음 플레이어 턴
        self.assertTrue(can_attack(state, unit_named(state, "Unit_Akimbo")))

    def test_ankle_breaker_binds(self):
        state = to_enemy_turn(battle([("Unit_Akimbo", (4, 3))], [("Unit_Demon_AnkleBreaker", (4, 4))]))
        state = attack(state, "Unit_Demon_AnkleBreaker", (4, 3))
        state = apply(state, EndTurn())
        self.assertFalse(can_move(state, unit_named(state, "Unit_Akimbo")))

    def test_solo_action_shield_only_when_alone(self):
        # Sniper(공 5) → Demon_SoloAction(방 5): 5×95/100 = 4. 혼자면 보호막 10이 다 막는다.
        alone = battle([("Unit_Akimbo", (0, 0)), ("Unit_Sniper", (4, 2))], [("Unit_Demon_SoloAction", (4, 6))])
        alone = attack(alone, "Unit_Sniper", (4, 6))
        solo = unit_named(alone, "Unit_Demon_SoloAction")
        self.assertEqual((solo.hp, solo.shield), (20, 6))
        crowded = battle([("Unit_Akimbo", (0, 0)), ("Unit_Sniper", (4, 2))],
                         [("Unit_Demon_SoloAction", (4, 6)), ("Unit_Demon", (5, 6))])
        crowded = attack(crowded, "Unit_Sniper", (4, 6))
        self.assertEqual(unit_named(crowded, "Unit_Demon_SoloAction").hp, 16)

    def test_kill_chain_hits_another_enemy(self):
        # Akimbo(공 10) → Demon(방 3): 10×97/100 = 9. 처치 뒤 남은 Demon 하나를 한 번 더 친다.
        state = battle([("Unit_Akimbo", (4, 2))], [("Unit_Demon", (4, 4)), ("Unit_Demon", (8, 8))])
        first = sorted(u.uid for u in state.units.values() if u.name == "Unit_Demon")
        state.units[first[0]].hp = 1
        state = attack(state, "Unit_Akimbo", (4, 4))
        self.assertEqual(state.units[first[1]].hp, 20 - 9)

    def test_hunters_mark_adds_damage_for_the_rest_of_the_turn(self):
        # ShotGun(공 7, 중장 → 일반 1.0) → Demon(방 3) 7×97/100 = 6 → 표식 +30%
        # 그다음 Sniper(공 5): 5×97/100 = 4 → 표식으로 4×130/100 = 5
        state = battle([("Unit_Akimbo", (0, 0)), ("Unit_ShotGun", (4, 3)), ("Unit_Sniper", (3, 2))],
                       [("Unit_Demon", (4, 4))])
        state = attack(state, "Unit_ShotGun", "Up")
        self.assertEqual(unit_named(state, "Unit_Demon").hp, 14)
        state = attack(state, "Unit_Sniper", (4, 4))
        self.assertEqual(unit_named(state, "Unit_Demon").hp, 9)


class ReinforcementTests(unittest.TestCase):
    def test_spawns_near_headquarters_at_enemy_turn_start(self):
        spec = Reinforcement(per_turn=1, padding=1, field_limit=4, pool=((U("Unit_Demon"), 2),))
        state = battle([("Unit_Akimbo", (4, 0))], [("Unit_Gravekeeper", (0, 8))], hq=(4, 7), reinforcement=spec)
        state = apply(state, EndTurn())
        spawned = [u for u in state.units.values() if u.name == "Unit_Demon"]
        self.assertEqual(len(spawned), 1)
        cell = spawned[0].cell
        self.assertTrue(3 <= cell[0] <= 6 and 6 <= cell[1] <= 8)
        self.assertNotIn(cell, {(4, 7), (5, 7), (4, 8), (5, 8)})
        self.assertIs(spawned[0].use, ActionUse.DEFERRED)    # 나온 턴에는 행동 못 함
        self.assertEqual(state.reinforcement_pool["Unit_Demon"], 1)

    def test_no_spawn_at_field_limit(self):
        spec = Reinforcement(per_turn=1, padding=1, field_limit=1, pool=((U("Unit_Demon"), 2),))
        state = battle([("Unit_Akimbo", (4, 0))], [("Unit_Gravekeeper", (0, 8))], hq=(4, 7), reinforcement=spec)
        state = apply(state, EndTurn())
        self.assertEqual(len(state.living(Side.ENEMY)), 1)


if __name__ == "__main__":
    unittest.main()
