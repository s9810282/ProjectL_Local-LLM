using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class HuntersMarkTraitSpec : TraitSpec
    {
        public int BonusDamagePercent { get; }
        public int DurationAffectedSideTurnEnds { get; }

        public HuntersMarkTraitSpec(int bonusDamagePercent, int durationAffectedSideTurnEnds) : base(TraitKey.HuntersMark)
        {
            if (bonusDamagePercent < 0) throw new ArgumentOutOfRangeException(nameof(bonusDamagePercent));
            if (durationAffectedSideTurnEnds < 1) throw new ArgumentOutOfRangeException(nameof(durationAffectedSideTurnEnds));
            BonusDamagePercent = bonusDamagePercent;
            DurationAffectedSideTurnEnds = durationAffectedSideTurnEnds;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (HuntersMarkTraitSpec)other;
            return BonusDamagePercent == otherSpec.BonusDamagePercent &&
                DurationAffectedSideTurnEnds == otherSpec.DurationAffectedSideTurnEnds;
        }
    }
}
