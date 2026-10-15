using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>저작된 지형 배치를 기존 위치 보정 원본과 불변 의미 분류로 변환한다.</summary>
    internal static class StageEnvironmentAdapter
    {
        internal static IReadOnlyList<InitialObstaclePlacementSpec> ReadObstacles(IReadOnlyList<InitialObstaclePlacementSpec> placements)
        {
            if (placements == null) throw new ArgumentNullException(nameof(placements));
            var copy = placements.ToArray();
            if (copy.Any(p => p == null)) throw new ArgumentException("장애물 배치는 null일 수 없습니다.");
            return Array.AsReadOnly(copy);
        }

        internal static (MovementEnvironmentFacts Environment, EnvironmentSemanticBindings Bindings)
            Read(int boardWidth, int boardHeight, IReadOnlyList<TerrainPresencePlacementData> placements)
            => Read(boardWidth, boardHeight, placements, Array.Empty<StaticTerrainPlacementData>());

        internal static (MovementEnvironmentFacts Environment, EnvironmentSemanticBindings Bindings)
            Read(int boardWidth, int boardHeight, IReadOnlyList<TerrainPresencePlacementData> placements,
                IReadOnlyList<StaticTerrainPlacementData> staticPlacements,
                IReadOnlyList<InitialObjectPlacementSpec> objectPlacements = null,
                IReadOnlyList<TornadoSourceFact> tornadoPlacements = null, IReadOnlyList<CellCoord> wallCells = null)
        {
            if (placements == null) throw new ArgumentNullException(nameof(placements));
            if (staticPlacements == null) throw new ArgumentNullException(nameof(staticPlacements));
            objectPlacements ??= Array.Empty<InitialObjectPlacementSpec>();
            tornadoPlacements ??= Array.Empty<TornadoSourceFact>();
            var walls = (wallCells ?? Array.Empty<CellCoord>()).OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray();
            if (walls.Distinct().Count() != walls.Length)
                throw new ArgumentException("벽 셀을 중복 작성할 수 없습니다.", nameof(wallCells));
            int terrainCount = placements.Count + staticPlacements.Count + tornadoPlacements.Count;
            int sourceCount = terrainCount + objectPlacements.Count;
            var sources = new MovementTileFact[sourceCount + walls.Length];
            var terrainIds = new long[terrainCount + walls.Length];
            var objectIds = new long[objectPlacements.Count];
            for (int i = 0; i < placements.Count; i++)
            {
                var placement = placements[i] ?? throw new ArgumentException("지형 배치는 null일 수 없습니다.", nameof(placements));
                sources[i] = new DestinationTileModifierFact(placement.SourceId, placement.Cell, true,
                    placement.KnownTo, placement.Definition.AttackBonus, placement.Definition.MaxHealthBonus);
                terrainIds[i] = placement.SourceId;
            }
            for (int i = 0; i < staticPlacements.Count; i++)
            {
                var placement = staticPlacements[i] ?? throw new ArgumentException("지형 배치는 null일 수 없습니다.", nameof(staticPlacements));
                sources[placements.Count + i] = new StaticTerrainFact(placement.SourceId, placement.Cell,
                    placement.Kind, true, placement.KnownTo);
                terrainIds[placements.Count + i] = placement.SourceId;
            }

            for (int i = 0; i < tornadoPlacements.Count; i++)
            {
                var source = tornadoPlacements[i] ?? throw new ArgumentException("Tornado 배치는 null일 수 없습니다.", nameof(tornadoPlacements));
                int index = placements.Count + staticPlacements.Count + i;
                sources[index] = source;
                terrainIds[index] = source.Id;
            }

            for (int i = 0; i < objectPlacements.Count; i++)
            {
                var placement = objectPlacements[i] ?? throw new ArgumentException("Object 배치는 null일 수 없습니다.", nameof(objectPlacements));
                var source = placement.CreateSource(checked((long)i + 1));
                sources[terrainCount + i] = source;
                objectIds[i] = source.Id;
            }

            long maximumSourceId = sources.Take(sourceCount).Select(source => source.Id).DefaultIfEmpty(0).Max();
            for (int i = 0; i < walls.Length; i++)
            {
                long id = checked(maximumSourceId + i + 1L);
                sources[sourceCount + i] = new StaticTerrainFact(id, walls[i], StaticTerrainKind.Wall,
                    true, new[] { Owner.Player, Owner.Enemy });
                terrainIds[terrainCount + i] = id;
            }

            var environment = new MovementEnvironmentFacts(sources);
            CellEnvironmentQuery.ValidateInitialEnvironment(boardWidth, boardHeight, environment);
            if (sources.Where(s => terrainIds.Contains(s.Id)).GroupBy(source => source.Cell)
                .Any(group => group.Count() > 1 && group.Any(EnvironmentContentSupport.IsExclusiveTerrain)))
                throw new NotSupportedException("정적 지형과 다른 Terrain의 같은 셀 조합은 아직 지원하지 않습니다.");
            var bindings = new EnvironmentSemanticBindings(terrainIds, objectIds, Array.Empty<EntityId>());
            return (environment, bindings);
        }
    }
}
