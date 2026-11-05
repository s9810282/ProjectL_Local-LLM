using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    internal static class HaboobTargetingRules
    {
        internal static bool CanDesignate(IReadOnlyList<CellCoord> area, CellCoord attackerCell, CellCoord primaryCell) =>
            area.Contains(attackerCell) == area.Contains(primaryCell);
    }
}
