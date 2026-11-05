using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class HunterTraitDefinition : TraitDefinition
    {
        public int AttackPerTargetKill { get; }
        public int MaxRewardsPerStage { get; }

        public HunterTraitDefinition(int attackPerTargetKill, int maxRewardsPerStage) : base(TraitKey.Hunter)
        {
            var spec = new HunterTraitSpec(attackPerTargetKill, maxRewardsPerStage);
            AttackPerTargetKill = spec.AttackPerTargetKill;
            MaxRewardsPerStage = spec.MaxRewardsPerStage;
        }
    }
}
