using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class PositionSwapTraitSpec : TraitSpec
    {
        public int ShieldAmount { get; }

        public PositionSwapTraitSpec() : this(0) { }

        public PositionSwapTraitSpec(int shieldAmount) : base(TraitKey.PositionSwap)
        {
            if (shieldAmount < 0) throw new ArgumentOutOfRangeException(nameof(shieldAmount));
            ShieldAmount = shieldAmount;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (PositionSwapTraitSpec)other;
            return ShieldAmount == otherSpec.ShieldAmount;
        }
    }
}
