using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class HuntersMarkTraitDefinition : TraitDefinition
    {
        public int BonusDamagePercent { get; }
        public int DurationAffectedSideTurnEnds { get; }

        public HuntersMarkTraitDefinition(int bonusDamagePercent, int durationAffectedSideTurnEnds) : base(TraitKey.HuntersMark)
        {
            if (bonusDamagePercent < 0) throw new ArgumentOutOfRangeException(nameof(bonusDamagePercent));
            if (durationAffectedSideTurnEnds < 1) throw new ArgumentOutOfRangeException(nameof(durationAffectedSideTurnEnds));
            BonusDamagePercent = bonusDamagePercent;
            DurationAffectedSideTurnEnds = durationAffectedSideTurnEnds;
        }
    }
}
