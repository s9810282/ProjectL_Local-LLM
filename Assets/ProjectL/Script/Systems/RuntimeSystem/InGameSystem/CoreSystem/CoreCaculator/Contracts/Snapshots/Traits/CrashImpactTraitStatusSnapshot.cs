using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class CrashImpactTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int DistanceCells { get; }
        public int WallDamagePercent { get; }
        public int UnitDamagePercent { get; }

        public CrashImpactTraitStatusSnapshot(int distanceCells, int wallDamagePercent, int unitDamagePercent)
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

        public override bool SameValues(TraitStatusSnapshot other) => other is CrashImpactTraitStatusSnapshot c &&
            DistanceCells == c.DistanceCells && WallDamagePercent == c.WallDamagePercent && UnitDamagePercent == c.UnitDamagePercent;
    }
}
