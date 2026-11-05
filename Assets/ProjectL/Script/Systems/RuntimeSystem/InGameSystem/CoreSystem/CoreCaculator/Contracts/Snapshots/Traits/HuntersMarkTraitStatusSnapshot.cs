using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>정의에서 전달된 피해 증가율입니다. 대상의 표식 상태와 분리합니다.</summary>
    public sealed class HuntersMarkTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int BonusDamagePercent { get; }
        public int DurationAffectedSideTurnEnds { get; }

        public HuntersMarkTraitStatusSnapshot(int bonusDamagePercent, int durationAffectedSideTurnEnds) : base(TraitKey.HuntersMark)
        {
            if (bonusDamagePercent < 0) throw new ArgumentOutOfRangeException(nameof(bonusDamagePercent));
            if (durationAffectedSideTurnEnds < 1) throw new ArgumentOutOfRangeException(nameof(durationAffectedSideTurnEnds));
            BonusDamagePercent = bonusDamagePercent;
            DurationAffectedSideTurnEnds = durationAffectedSideTurnEnds;
        }

        public override bool SameValues(TraitStatusSnapshot other) =>
            other is HuntersMarkTraitStatusSnapshot mark && BonusDamagePercent == mark.BonusDamagePercent && DurationAffectedSideTurnEnds == mark.DurationAffectedSideTurnEnds;
    }
}
