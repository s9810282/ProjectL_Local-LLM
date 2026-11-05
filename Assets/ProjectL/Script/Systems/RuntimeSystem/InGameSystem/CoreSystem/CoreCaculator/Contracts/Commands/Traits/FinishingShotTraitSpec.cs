using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class FinishingShotTraitSpec : TraitSpec
    {
        public int AttackThresholdPercent { get; }

        public FinishingShotTraitSpec(int attackThresholdPercent) : base(TraitKey.FinishingShot)
        {
            if (attackThresholdPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(attackThresholdPercent));
            AttackThresholdPercent = attackThresholdPercent;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (FinishingShotTraitSpec)other;
            return AttackThresholdPercent == otherSpec.AttackThresholdPercent;
        }
    }
}
