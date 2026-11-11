using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class RampageEffectRule : IEffectOrderedHitRule
    {
        public bool RequiresOrderedHits => true;

        internal RampageEffectRule(RampageTraitDefinition definition, RampageTraitRule rule)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (rule == null) throw new ArgumentNullException(nameof(rule));
        }
    }

    internal sealed class RampageEffectStateSnapshot : IEffectStateSnapshot
    {
        internal int KillAttackBonus { get; }

        internal RampageEffectStateSnapshot(int killAttackBonus)
        {
            if (killAttackBonus < 0) throw new ArgumentOutOfRangeException(nameof(killAttackBonus));
            KillAttackBonus = killAttackBonus;
        }

        public bool ValueEquals(IEffectStateSnapshot other) => other is RampageEffectStateSnapshot state && KillAttackBonus == state.KillAttackBonus;
    }
}
