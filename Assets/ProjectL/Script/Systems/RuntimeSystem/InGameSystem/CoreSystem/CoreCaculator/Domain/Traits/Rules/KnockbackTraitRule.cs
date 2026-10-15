using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class KnockbackTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.Knockback;
    }
}
