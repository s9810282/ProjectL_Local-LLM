using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal readonly struct EffectThornFieldContribution
    {
        public EffectOriginKey Origin { get; }
        public int ThornDamage { get; }
        public int LifetimeUnitTurns { get; }
        public IReadOnlyList<CellCoord> ApplicationCells { get; }

        public EffectThornFieldContribution(
            EffectOriginKey origin,
            int thornDamage,
            int lifetimeUnitTurns,
            CellCoord[] applicationCells)
        {
            if (origin.IsValid == false)
                throw new ArgumentException(nameof(origin));
            if (thornDamage < 1)
                throw new ArgumentOutOfRangeException(nameof(thornDamage));
            if (lifetimeUnitTurns < 1)
                throw new ArgumentOutOfRangeException(nameof(lifetimeUnitTurns));
            if (applicationCells == null)
                throw new ArgumentNullException(nameof(applicationCells));

            var copiedCells = (CellCoord[])applicationCells.Clone();
            var seen = new HashSet<CellCoord>();
            for (int i = 0; i < copiedCells.Length; i++)
            {
                if (seen.Add(copiedCells[i]) == false)
                {
                    throw new ArgumentException(
                        "Duplicate ThornField application cell.",
                        nameof(applicationCells));
                }
            }

            Origin = origin;
            ThornDamage = thornDamage;
            LifetimeUnitTurns = lifetimeUnitTurns;
            ApplicationCells = Array.AsReadOnly(copiedCells);
        }
    }
}
