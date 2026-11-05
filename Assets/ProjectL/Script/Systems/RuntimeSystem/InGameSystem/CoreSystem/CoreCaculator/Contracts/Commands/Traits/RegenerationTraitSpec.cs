using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    /// <summary>자기 진영 종료 주기마다 회복할 체력량과 주기입니다.</summary>
    public sealed class RegenerationTraitSpec : TraitSpec
    {
        public int HealAmount { get; }
        public int HealEveryOwnSideTurnEnds { get; }

        public RegenerationTraitSpec(int healAmount, int healEveryOwnSideTurnEnds = 1)
            : base(TraitKey.Regeneration)
        {
            if (healAmount < 1)
                throw new ArgumentOutOfRangeException(nameof(healAmount));

            if (healEveryOwnSideTurnEnds < 1)
                throw new ArgumentOutOfRangeException(nameof(healEveryOwnSideTurnEnds));

            HealAmount = healAmount;
            HealEveryOwnSideTurnEnds = healEveryOwnSideTurnEnds;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (RegenerationTraitSpec)other;
            return HealAmount == otherSpec.HealAmount &&
                HealEveryOwnSideTurnEnds == otherSpec.HealEveryOwnSideTurnEnds;
        }
    }
}
