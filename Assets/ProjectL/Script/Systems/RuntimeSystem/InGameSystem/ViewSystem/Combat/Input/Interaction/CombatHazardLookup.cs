using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 칸의 환경 위험 원본을 찾아 그 피해 칸을 모읍니다(호버 공격 범위 설계 §8 HZ3, 위험 범위 계획 T6·P6).
    /// 무엇을 위험으로 보는지의 원본은 설계 §8.1 표이고, 여기는 그 조건만 확인합니다. 피해 칸 모양은 Core 공개 입구
    /// <see cref="CombatHazardAreas"/>가 정합니다(View 사본 없음). 상태를 갖지 않고, 호출자 목록을 재사용해 할당하지 않습니다.
    /// </summary>
    internal static class CombatHazardLookup
    {
        /// <summary>
        /// <paramref name="into"/>를 비우고 <paramref name="cell"/>의 위험 원본마다 피해 칸을 덧붙입니다. 위험 원본이 하나라도 있으면 true입니다.
        /// 지뢰: 오브젝트로 분류되고 활성이며 플레이어에게 알려진 <see cref="MineSourceFact"/>. 화약통: 칸을 점유한 살아 있는 구조물 중
        /// 장애물로 분류되고 프로필이 <see cref="ObstacleContentKind.PowderBarrel"/>인 것(피해 칸은 기준 칸 둘레, Core 폭발과 같음).
        /// </summary>
        internal static bool Collect(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings,
            IReadOnlyList<StructureCombatFacts> structures, IReadOnlyList<ObstacleContentProfile> obstacles,
            int boardWidth, int boardHeight, CellCoord cell, List<CellCoord> into)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (structures == null) throw new ArgumentNullException(nameof(structures));
            if (obstacles == null) throw new ArgumentNullException(nameof(obstacles));
            if (into == null) throw new ArgumentNullException(nameof(into));

            into.Clear();
            bool found = false;
            IReadOnlyList<MovementTileFact> tiles = environment.Tiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                MovementTileFact tile = tiles[i];
                if (tile.Cell != cell || !Contains(bindings.ObjectSourceIds, tile.Id)) continue;
                if (tile is not ObjectSourceFact)
                    throw new InvalidOperationException("Object 분류와 Buffer 원본 형식이 다릅니다.");
                if (tile is not MineSourceFact || !CombatEnvironmentFacts.IsShown(tile)) continue;
                CombatHazardAreas.MineCells(tile.Cell, boardWidth, boardHeight, into);
                found = true;
            }

            for (int i = 0; i < structures.Count; i++)
            {
                StructureCombatFacts structure = structures[i];
                if (structure.Health.IsDead || !Contains(structure.OccupiedCells, cell)) continue;
                if (!CombatEnvironmentFacts.TryGetObstacle(bindings, obstacles, structure, out ObstacleContentProfile obstacle)
                    || obstacle.Kind != ObstacleContentKind.PowderBarrel) continue;
                CombatHazardAreas.PowderBarrelCells(structure.Anchor, boardWidth, boardHeight, into);
                found = true;
            }
            return found;
        }

        private static bool Contains(IReadOnlyList<long> values, long id)
        {
            for (int i = 0; i < values.Count; i++) if (values[i] == id) return true;
            return false;
        }

        private static bool Contains(IReadOnlyList<CellCoord> values, CellCoord cell)
        {
            for (int i = 0; i < values.Count; i++) if (values[i] == cell) return true;
            return false;
        }
    }
}
