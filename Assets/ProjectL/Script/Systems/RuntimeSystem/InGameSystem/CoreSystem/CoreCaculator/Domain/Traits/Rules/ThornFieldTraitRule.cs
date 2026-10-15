using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class ThornFieldTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.ThornField;

        internal static CellCoord[] CalculateApplicationCells(CellCoord selectedCenter)
        {
            var cells = new List<CellCoord>(8);
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    cells.Add(new CellCoord(
                        checked(selectedCenter.X + dx),
                        checked(selectedCenter.Y + dy)));
                }
            }

            return cells.ToArray();
        }
    }
}
