using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class SteadfastWillTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.SteadfastWill;
        internal bool GrantsKnockbackImmunity => true;
    }
}
