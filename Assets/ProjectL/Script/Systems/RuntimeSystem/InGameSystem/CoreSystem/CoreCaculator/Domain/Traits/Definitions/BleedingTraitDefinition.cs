using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class BleedingTraitDefinition : TraitDefinition
    {
        public int DamagePerStack { get; }

        public BleedingTraitDefinition(int damagePerStack) : base(TraitKey.Bleeding)
        {
            DamagePerStack = new BleedingTraitSpec(damagePerStack).DamagePerStack;
        }
    }
}
