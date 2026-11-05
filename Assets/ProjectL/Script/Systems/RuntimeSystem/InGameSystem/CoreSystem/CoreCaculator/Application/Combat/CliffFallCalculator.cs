using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    internal static class CliffFallCalculator
    {
        internal static bool ResolveAt(MovementProjection projection, EntityId entityId, CellCoord cell, bool settleDeaths = true)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            projection.TerrainQueryCells.Add(cell);
            var cliff = projection.Environment.Tiles.OfType<StaticTerrainFact>().SingleOrDefault(t =>
                t.Active && t.Kind == StaticTerrainKind.Cliff && t.Cell == cell && projection.Bindings.TerrainSourceIds.Contains(t.Id));
            if (cliff == null) return false;
            var structure = projection.Structure(entityId);
            if (structure != null)
            {
                if (!projection.Bindings.ObstacleEntityIds.Contains(entityId) || structure.Health.IsDead ||
                    !structure.OccupiedCells.Contains(cell)) return false;
            }
            else
            {
                var character = projection.Character(entityId);
                if (character.Health.IsDead || character.Area != CharacterRuntimeArea.Field || !character.OccupiedCells.Contains(cell)) return false;
            }
            projection.ApplyFall(entityId, cliff.Id, cell);
            if (settleDeaths) projection.ResolveDeathBoundary();
            return true;
        }
    }
}
