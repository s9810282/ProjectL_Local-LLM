using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>재생의 설정과 이번 주기의 진행값을 공개합니다. 체력은 캐릭터 사실만 보관합니다.</summary>
    public sealed class RegenerationTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int HealAmount { get; }
        public int HealEveryOwnSideTurnEnds { get; }
        public int CompletedEligibleTurnEnds { get; }

        public RegenerationTraitStatusSnapshot(int healAmount, int healEveryOwnSideTurnEnds = 1, int completedEligibleTurnEnds = 0)
            : base(TraitKey.Regeneration)
        {
            if (healAmount < 1)
                throw new ArgumentOutOfRangeException(nameof(healAmount));

            if (healEveryOwnSideTurnEnds < 1)
                throw new ArgumentOutOfRangeException(nameof(healEveryOwnSideTurnEnds));
            if (completedEligibleTurnEnds < 0 || completedEligibleTurnEnds >= healEveryOwnSideTurnEnds)
                throw new ArgumentOutOfRangeException(nameof(completedEligibleTurnEnds));

            HealAmount = healAmount;
            HealEveryOwnSideTurnEnds = healEveryOwnSideTurnEnds;
            CompletedEligibleTurnEnds = completedEligibleTurnEnds;
        }

        public override bool SameValues(TraitStatusSnapshot other) =>
            other is RegenerationTraitStatusSnapshot regeneration && HealAmount == regeneration.HealAmount &&
            HealEveryOwnSideTurnEnds == regeneration.HealEveryOwnSideTurnEnds && CompletedEligibleTurnEnds == regeneration.CompletedEligibleTurnEnds;
    }
}
