using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class DeadHandTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.DeadHand;
    }
}
