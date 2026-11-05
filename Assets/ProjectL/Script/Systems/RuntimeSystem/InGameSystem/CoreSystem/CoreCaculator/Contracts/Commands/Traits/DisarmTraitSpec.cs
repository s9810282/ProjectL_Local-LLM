using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class DisarmTraitSpec : TraitSpec
    {
        public int DurationAffectedSideTurnEnds { get; }

        public DisarmTraitSpec(int durationAffectedSideTurnEnds) : base(TraitKey.Disarm)
        {
            if (durationAffectedSideTurnEnds < 1)
                throw new ArgumentOutOfRangeException(nameof(durationAffectedSideTurnEnds));
            DurationAffectedSideTurnEnds = durationAffectedSideTurnEnds;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (DisarmTraitSpec)other;
            return DurationAffectedSideTurnEnds == otherSpec.DurationAffectedSideTurnEnds;
        }
    }
}
