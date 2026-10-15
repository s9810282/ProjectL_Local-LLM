using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class PiercingFalloffTraitSpec : TraitSpec
    {
        public int ReductionPercentPerTarget { get; }
        public int MaxTargetsPerRay { get; }

        public PiercingFalloffTraitSpec(int reductionPercentPerTarget, int maxTargetsPerRay)
            : base(TraitKey.PiercingFalloff)
        {
            if (reductionPercentPerTarget < 1 || reductionPercentPerTarget > 100)
                throw new ArgumentOutOfRangeException(nameof(reductionPercentPerTarget));

            ReductionPercentPerTarget = reductionPercentPerTarget;
            if (maxTargetsPerRay < 0)
                throw new ArgumentOutOfRangeException(nameof(maxTargetsPerRay));
            MaxTargetsPerRay = maxTargetsPerRay;
        }
    }
}
