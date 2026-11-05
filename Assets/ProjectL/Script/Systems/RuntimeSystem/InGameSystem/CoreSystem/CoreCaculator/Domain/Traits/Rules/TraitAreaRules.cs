using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    public static class TraitAreaRules
    {
        public static bool Contains(CellCoord origin, CellCoord target, TraitAreaSpec area)
        {
            if (area == null) throw new ArgumentNullException(nameof(area));
            long dx = Math.Abs((long)target.X - origin.X);
            long dy = Math.Abs((long)target.Y - origin.Y);
            if (dx == 0 && dy == 0) return area.IncludeCenter;
            if (dx > area.RadiusCells || dy > area.RadiusCells) return false;
            return area.Footprint == TraitAreaFootprint.SquareNeighbors || dx == 0 || dy == 0;
        }
    }
}
