using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class ReconSquadTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int IncreasePercent { get; }
        public int Radius { get; }
        public ReconSquadTraitStatusSnapshot() : this(10, 1)
        {
        }
        public ReconSquadTraitStatusSnapshot(int increasePercent, int radius) : base(TraitKey.ReconSquad)
        {
            if (increasePercent < 0) throw new ArgumentOutOfRangeException(nameof(increasePercent));
            if (radius < 1) throw new ArgumentOutOfRangeException(nameof(radius));
            IncreasePercent = increasePercent;
            Radius = radius;
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is ReconSquadTraitStatusSnapshot status &&
            IncreasePercent == status.IncreasePercent && Radius == status.Radius;
    }
}
