using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class PullTraitSpec : TraitSpec
    {
        public int ExtraRangeCells { get; }

        public PullTraitSpec(int extraRangeCells) : base(TraitKey.Pull)
        {
            if (extraRangeCells < 0) throw new ArgumentOutOfRangeException(nameof(extraRangeCells));
            ExtraRangeCells = extraRangeCells;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (PullTraitSpec)other;
            return ExtraRangeCells == otherSpec.ExtraRangeCells;
        }
    }
}
