using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement
{
    /// <summary>낭떠러지의 바닥 형식과 현재 플랫폼의 지지를 같은 기준으로 판정한다.</summary>
    internal static class BoardSurfaceRules
    {
        internal static bool HasPlatformSupport(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, CellCoord cell) =>
            environment.Tiles.OfType<MovingPlatformFact>()
                .Any(platform => SuppliesSupport(platform, bindings) && platform.Occupies(cell));

        /// <summary>
        /// 장애물이 이 칸들에 들어갈 수 있는 바닥인지다. 플랫폼은 장애물을 운반하지 않고 점유되면 멈추므로 현재 플랫폼 칸에는 들어가지 못한다.
        /// 장애물을 옮기는 모든 경로(넉백 등)가 이 판정을 부른다. 맵 밖·벽·점유 검사는 호출자가 따로 한다(검토 PF-5).
        /// </summary>
        internal static bool AdmitsObstacleAt(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, IEnumerable<CellCoord> cells) =>
            !cells.Any(cell => HasPlatformSupport(environment, bindings, cell));

        internal static bool IsUnsupportedCliff(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, CellCoord cell) => UnsupportedCliffAt(environment, bindings, cell) != null;

        internal static StaticTerrainFact UnsupportedCliffAt(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, CellCoord cell)
        {
            var cliff = environment.Tiles.OfType<StaticTerrainFact>().SingleOrDefault(t =>
                t.Cell == cell && EnvironmentTerrainRules.IsTerrain(t, bindings, StaticTerrainKind.Cliff));
            return cliff != null && !HasPlatformSupport(environment, bindings, cell) ? cliff : null;
        }

        internal static IEnumerable<CellCoord?> BlockedSurfaceCellSteps(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, bool includeWalls)
        {
            var support = new HashSet<CellCoord>();
            var walls = includeWalls ? new HashSet<CellCoord>() : null;
            // 기존 벽 조사 단계에서 지지 셀도 색인하여 AI 작업량 분할을 유지한다.
            foreach (var tile in environment.Tiles)
            {
                if (tile is MovingPlatformFact platform && SuppliesSupport(platform, bindings))
                    support.UnionWith(platform.OccupiedCells);
                if (includeWalls)
                    yield return tile is StaticTerrainFact terrain &&
                        EnvironmentTerrainRules.IsTerrain(terrain, bindings, StaticTerrainKind.Wall) && walls.Add(terrain.Cell)
                        ? terrain.Cell : (CellCoord?)null;
            }
            foreach (var cell in EnvironmentTerrainRules.TerrainCellSteps(environment, bindings, StaticTerrainKind.Cliff))
                yield return cell.HasValue && support.Contains(cell.Value) ? null : cell;
        }

        /// <summary>플랫폼 경로가 사용할 바닥 형식이다. 현재 점유 검사는 호출자가 별도로 수행한다.</summary>
        internal static bool IsPlatformRouteSurface(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, CellCoord cell) =>
            EnvironmentTerrainRules.HasTerrain(environment, bindings, cell, StaticTerrainKind.Cliff) &&
            !EnvironmentTerrainRules.IsWallOccupied(environment, bindings, cell) &&
            !EnvironmentTerrainRules.HasTerrain(environment, bindings, cell, StaticTerrainKind.FilledCliff);

        internal static bool IsPlatformRouteSurface(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, MovingPlatformSpec spec, CellCoord anchor) =>
            spec.FootprintAt(anchor).All(cell => IsPlatformRouteSurface(environment, bindings, cell));

        private static bool SuppliesSupport(MovingPlatformFact platform, EnvironmentSemanticBindings bindings) =>
            PlatformMotionRules.SuppliesSupport(platform, bindings);
    }
}
