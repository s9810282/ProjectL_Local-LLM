using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Placement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>기존 가중 추첨과 배치 계산을 순수 값으로 수행한다. Buffer·DB·실행 흐름을 소유하지 않는다.</summary>
    public static class EnemySpawnCalculator
    {
        /// <summary>출현 적의 종류·위치를 정한다. 출현한 적은 모두 <paramref name="level"/>(스테이지 적 레벨)로 만든다.</summary>
        public static InitialEnemyPlacementSpec[] Calculate(StageConfig stage,
            IReadOnlyList<EnemySpawnCandidate> candidates, int spawnCount, int seed,
            IReadOnlyList<StructureSpec> structures, MovementEnvironmentFacts environment = null,
            EnvironmentSemanticBindings bindings = null, IReadOnlyList<InitialObstaclePlacementSpec> obstacles = null,
            int level = 1)
        {
            if (stage == null || candidates == null || structures == null)
                throw new ArgumentNullException("적 출현 정의가 필요합니다.");
            if (spawnCount < 0) throw new ArgumentOutOfRangeException(nameof(spawnCount));
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            environment ??= MovementEnvironmentFacts.Empty;
            bindings ??= EnvironmentSemanticBindings.Empty;
            obstacles ??= Array.Empty<InitialObstaclePlacementSpec>();
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

            var reserved = ReservedStructureCells(stage, structures, obstacles);
            // 적 배치 구역은 없다. 판 전체에서 아군 배치 구역을 뺀 칸이 후보이며, 판정 원본은 DeploymentZonePolicy다.
            var zones = new DeploymentZonePolicy(stage.PlayerZone);
            var cells = new List<CellCoord>();
            for (int y = 0; y < stage.MapHeight; y++)
                for (int x = 0; x < stage.MapWidth; x++)
                {
                    var cell = new CellCoord(x, y);
                    // 초기 배치가 거절하는 벽·낭떠러지는 일반 진입 제한과 같은 기준으로 후보에서 뺀다.
                    if (zones.CanDeploy(Owner.Enemy, new[] { cell }) && !reserved.Contains(cell) &&
                        !EnvironmentTerrainRules.BlocksVoluntaryEntry(environment, bindings, cell))
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
                placements[i] = new InitialEnemyPlacementSpec(keys[i], cells[i], Rotation4.R0, level: level);
            return placements;
        }

        private static HashSet<CellCoord> ReservedStructureCells(StageConfig stage,
            IReadOnlyList<StructureSpec> structures, IReadOnlyList<InitialObstaclePlacementSpec> obstacles)
        {
            var definitions = new Dictionary<EntityKey, StructureSpec>();
            foreach (StructureSpec structure in structures) definitions.Add(structure.Key, structure);
            var reserved = new HashSet<CellCoord>();
            foreach (InitialEnemyStructurePlacementSpec placement in stage.InitialEnemyStructurePlacements.Span)
            {
                if (!definitions.TryGetValue(placement.StructureKey, out var definition))
                    throw new InvalidOperationException("적 구조물 정의를 찾을 수 없습니다.");
                Reserve(reserved, definition, placement.Anchor, placement.Rotation);
            }
            // 장애물은 적 출현 뒤에 배치되므로 먼저 점유를 예약해야 배치 충돌로 전투 준비가 실패하지 않는다.
            foreach (InitialObstaclePlacementSpec placement in obstacles)
            {
                if (placement == null) throw new ArgumentException("장애물 배치는 null일 수 없습니다.", nameof(obstacles));
                Reserve(reserved, placement.Definition, placement.Anchor, placement.Rotation);
            }
            return reserved;
        }

        private static void Reserve(HashSet<CellCoord> reserved, StructureSpec definition, CellCoord anchor, Rotation4 rotation)
        {
            var footprint = new TileFootprint(definition.FootprintWidth, definition.FootprintHeight,
                definition.FootprintLocalCells.ToArray());
            for (int i = 0; i < footprint.CellCount; i++)
                reserved.Add(footprint.GetOccupiedCell(i, anchor, rotation));
        }
    }
}
