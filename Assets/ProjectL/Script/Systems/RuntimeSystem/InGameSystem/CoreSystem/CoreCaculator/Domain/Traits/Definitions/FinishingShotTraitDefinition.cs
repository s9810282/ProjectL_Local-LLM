using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class FinishingShotTraitDefinition : TraitDefinition
    {
        public int AttackThresholdPercent { get; }

        public FinishingShotTraitDefinition(int attackThresholdPercent) : base(TraitKey.FinishingShot)
        {
            var spec = new FinishingShotTraitSpec(attackThresholdPercent);
            AttackThresholdPercent = spec.AttackThresholdPercent;
        }
    }
}
