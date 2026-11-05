using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class AnkleBreakerTraitSpec : TraitSpec
    {
        public int DurationAffectedSideTurnEnds { get; }

        public AnkleBreakerTraitSpec(int durationAffectedSideTurnEnds) : base(TraitKey.AnkleBreaker)
        {
            if (durationAffectedSideTurnEnds < 1)
                throw new ArgumentOutOfRangeException(nameof(durationAffectedSideTurnEnds));
            DurationAffectedSideTurnEnds = durationAffectedSideTurnEnds;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (AnkleBreakerTraitSpec)other;
            return DurationAffectedSideTurnEnds == otherSpec.DurationAffectedSideTurnEnds;
        }
    }
}
