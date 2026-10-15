using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>기존 가중 추첨과 배치 계산을 순수 값으로 수행한다. Buffer·DB·실행 흐름을 소유하지 않는다.</summary>
    public static class EnemySpawnCalculator
    {
        public static InitialEnemyPlacementSpec[] Calculate(StageConfig stage,
            IReadOnlyList<EnemySpawnCandidate> candidates, int spawnCount, int seed,
            IReadOnlyList<StructureSpec> structures, MovementEnvironmentFacts environment = null,
            EnvironmentSemanticBindings bindings = null)
        {
            if (stage == null || candidates == null || structures == null)
                throw new ArgumentNullException("적 출현 정의가 필요합니다.");
            if (spawnCount < 0) throw new ArgumentOutOfRangeException(nameof(spawnCount));
            environment ??= MovementEnvironmentFacts.Empty;
            bindings ??= EnvironmentSemanticBindings.Empty;
            if (stage.InitialEnemyPlacements.Length != 0)
                throw new InvalidOperationException("직접 저작한 적 배치와 가중 출현은 동시에 사용할 수 없습니다.");
            if (spawnCount == 0) return Array.Empty<InitialEnemyPlacementSpec>();
            if (candidates.Count == 0)
                throw new InvalidOperationException("적 출현 후보가 필요합니다.");

            int totalWeight = 0;
            foreach (EnemySpawnCandidate candidate in candidates)
            {
                UnitSpec unit = candidate.Definition;
                if (unit == null || candidate.Weight <= 0 || unit.Role != UnitRole.Regular ||
                    unit.OwnerEligibility == UnitOwnerEligibility.PlayerOnly)
                    throw new InvalidOperationException("적 출현 후보의 정의·역할·소유 권한이 맞지 않습니다.");
                // 한 칸을 고르는 기존 정책을 다칸 배치 정책으로 임의 확장하지 않는다.
                if (unit.FootprintWidth != 1 || unit.FootprintHeight != 1 ||
                    unit.FootprintLocalCells.Length != 1 || unit.FootprintLocalCells.Span[0] != new CellCoord(0, 0))
                    throw new InvalidOperationException("다칸 적 출현은 기획 검토 전까지 지원하지 않습니다.");
                totalWeight = checked(totalWeight + candidate.Weight);
            }

            var reserved = ReservedStructureCells(stage, structures);
            var cells = new List<CellCoord>();
            for (int y = stage.MapHeight - stage.EnemyDeploymentDepth; y < stage.MapHeight; y++)
                for (int x = 0; x < stage.MapWidth; x++)
                {
                    var cell = new CellCoord(x, y);
                    if (!reserved.Contains(cell) && !EnvironmentTerrainRules.IsWallOccupied(environment, bindings, cell))
                        cells.Add(cell);
                }
            if (spawnCount > cells.Count)
                throw new InvalidOperationException("적 출현 수를 배치할 빈 칸이 부족합니다.");

            // 기존 구현처럼 모든 유닛 추첨 후 같은 난수 인스턴스로 행 우선 후보 칸을 섞는다.
            var random = new Random(seed);
            var keys = new EntityKey[spawnCount];
            for (int i = 0; i < keys.Length; i++)
            {
                int roll = random.Next(totalWeight);
                foreach (EnemySpawnCandidate candidate in candidates)
                {
                    roll -= candidate.Weight;
                    if (roll >= 0) continue;
                    keys[i] = candidate.Definition.Key;
                    break;
                }
            }
            for (int i = cells.Count - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                (cells[i], cells[swap]) = (cells[swap], cells[i]);
            }
            var placements = new InitialEnemyPlacementSpec[spawnCount];
            for (int i = 0; i < placements.Length; i++)
                placements[i] = new InitialEnemyPlacementSpec(keys[i], cells[i], Rotation4.R0);
            return placements;
        }

        private static HashSet<CellCoord> ReservedStructureCells(StageConfig stage,
            IReadOnlyList<StructureSpec> structures)
        {
            var definitions = new Dictionary<EntityKey, StructureSpec>();
            foreach (StructureSpec structure in structures) definitions.Add(structure.Key, structure);
            var reserved = new HashSet<CellCoord>();
            foreach (InitialEnemyStructurePlacementSpec placement in stage.InitialEnemyStructurePlacements.Span)
            {
                if (!definitions.TryGetValue(placement.StructureKey, out var definition))
                    throw new InvalidOperationException("적 구조물 정의를 찾을 수 없습니다.");
                var footprint = new TileFootprint(definition.FootprintWidth, definition.FootprintHeight,
                    definition.FootprintLocalCells.ToArray());
                for (int i = 0; i < footprint.CellCount; i++)
                    reserved.Add(footprint.GetOccupiedCell(i, placement.Anchor, placement.Rotation));
            }
            return reserved;
        }
    }
}
