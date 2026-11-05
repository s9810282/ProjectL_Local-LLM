using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class TrackingShotTraitSpec : TraitSpec
    {
        public int TargetHealthThresholdPercent { get; }
        public int ExtraDamagePercent { get; }

        public TrackingShotTraitSpec(int targetHealthThresholdPercent, int extraDamagePercent) : base(TraitKey.TrackingShot)
        {
            if (targetHealthThresholdPercent < 0 || targetHealthThresholdPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(targetHealthThresholdPercent));
            if (extraDamagePercent < 0)
                throw new ArgumentOutOfRangeException(nameof(extraDamagePercent));
            TargetHealthThresholdPercent = targetHealthThresholdPercent;
            ExtraDamagePercent = extraDamagePercent;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (TrackingShotTraitSpec)other;
            return TargetHealthThresholdPercent == otherSpec.TargetHealthThresholdPercent &&
                ExtraDamagePercent == otherSpec.ExtraDamagePercent;
        }
    }
}
