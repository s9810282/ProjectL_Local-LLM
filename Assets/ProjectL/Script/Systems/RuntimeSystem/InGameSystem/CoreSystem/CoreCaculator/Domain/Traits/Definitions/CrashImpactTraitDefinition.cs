using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class CrashImpactTraitDefinition : TraitDefinition
    {
        public int DistanceCells { get; }
        public int WallDamagePercent { get; }
        public int UnitDamagePercent { get; }

        public CrashImpactTraitDefinition(int distanceCells, int wallDamagePercent, int unitDamagePercent)
            : base(TraitKey.CrashImpact)
        {
            if (distanceCells < 1)
                throw new ArgumentOutOfRangeException(nameof(distanceCells));
            if (wallDamagePercent < 0 || wallDamagePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(wallDamagePercent));
            if (unitDamagePercent < 0 || unitDamagePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(unitDamagePercent));

            DistanceCells = distanceCells;
            WallDamagePercent = wallDamagePercent;
            UnitDamagePercent = unitDamagePercent;
        }
    }
}
