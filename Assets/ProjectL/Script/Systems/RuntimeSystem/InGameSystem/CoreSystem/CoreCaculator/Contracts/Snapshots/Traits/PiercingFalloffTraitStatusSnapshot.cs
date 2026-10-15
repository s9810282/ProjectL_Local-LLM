using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class PiercingFalloffTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int ReductionPercentPerTarget { get; }

        public PiercingFalloffTraitStatusSnapshot(int reductionPercentPerTarget)
            : base(TraitKey.PiercingFalloff)
        {
            if (reductionPercentPerTarget < 1 || reductionPercentPerTarget > 100)
                throw new ArgumentOutOfRangeException(nameof(reductionPercentPerTarget));

            ReductionPercentPerTarget = reductionPercentPerTarget;
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is PiercingFalloffTraitStatusSnapshot b &&
            ReductionPercentPerTarget == b.ReductionPercentPerTarget;
    }
}
