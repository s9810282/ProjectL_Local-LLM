using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class PullTraitDefinition : TraitDefinition
    {
        public int ExtraRangeCells { get; }

        public PullTraitDefinition(int extraRangeCells) : base(TraitKey.Pull)
        {
            if (extraRangeCells < 0) throw new ArgumentOutOfRangeException(nameof(extraRangeCells));
            ExtraRangeCells = extraRangeCells;
        }
    }
}
