using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    /// <summary>재생 규칙의 불변 회복량과 자기 진영 종료 주기입니다.</summary>
    public sealed class RegenerationTraitDefinition : TraitDefinition
    {
        public int HealAmount { get; }
        public int HealEveryOwnSideTurnEnds { get; }

        public RegenerationTraitDefinition(int healAmount, int healEveryOwnSideTurnEnds = 1)
            : base(TraitKey.Regeneration)
        {
            if (healAmount < 1)
                throw new ArgumentOutOfRangeException(nameof(healAmount));

            if (healEveryOwnSideTurnEnds < 1)
                throw new ArgumentOutOfRangeException(nameof(healEveryOwnSideTurnEnds));

            HealAmount = healAmount;
            HealEveryOwnSideTurnEnds = healEveryOwnSideTurnEnds;
        }
    }
}
