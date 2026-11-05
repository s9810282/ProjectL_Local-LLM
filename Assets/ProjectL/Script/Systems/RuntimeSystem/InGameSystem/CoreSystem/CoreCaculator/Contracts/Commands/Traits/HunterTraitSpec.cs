using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class HunterTraitSpec : TraitSpec
    {
        public int AttackPerTargetKill { get; }
        public int MaxRewardsPerStage { get; }

        public HunterTraitSpec(int attackPerTargetKill, int maxRewardsPerStage) : base(TraitKey.Hunter)
        {
            if (attackPerTargetKill < 0)
                throw new ArgumentOutOfRangeException(nameof(attackPerTargetKill));
            if (maxRewardsPerStage < 1)
                throw new ArgumentOutOfRangeException(nameof(maxRewardsPerStage));
            AttackPerTargetKill = attackPerTargetKill;
            MaxRewardsPerStage = maxRewardsPerStage;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (HunterTraitSpec)other;
            return AttackPerTargetKill == otherSpec.AttackPerTargetKill &&
                MaxRewardsPerStage == otherSpec.MaxRewardsPerStage;
        }
    }
}
