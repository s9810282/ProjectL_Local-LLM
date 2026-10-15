using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Placement
{
    /// <summary>셀의 최종 후보에서 확정된 공존 위반을 모으며 요소를 변경하거나 선택하지 않는다.</summary>
    internal static class EnvironmentPlacementRules
    {
        internal static EnvironmentPlacementViolation Evaluate(CellEnvironmentSnapshot cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            if (!cell.IsInside) return EnvironmentPlacementViolation.OutsideBoard;

            var violations = EnvironmentPlacementViolation.None;
            bool hasObstacle = cell.ObstacleIds.Count > 0;
            if (hasObstacle && cell.TerrainSourceIds.Count > 0)
                violations |= EnvironmentPlacementViolation.TerrainWithObstacle;
            if (hasObstacle && cell.ObjectSourceIds.Count > 0)
                violations |= EnvironmentPlacementViolation.ObstacleWithObject;
            if (cell.ObstacleIds.Count > 1)
                violations |= EnvironmentPlacementViolation.OverlappingObstacles;
            if (cell.IsWallOccupied && (cell.CharacterIds.Count > 0 || cell.BlockingStructureIds.Count > 0 ||
                cell.ObjectSourceIds.Count > 0 || cell.TerrainSourceIds.Count > 1))
                violations |= EnvironmentPlacementViolation.WallOverlap;
            return violations;
        }
    }
}
