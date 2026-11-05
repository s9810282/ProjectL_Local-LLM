using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class SteadfastWillEffectRule : ICombatEffectRule
    {
        private readonly SteadfastWillTraitRule _rule;

        internal SteadfastWillEffectRule(SteadfastWillTraitDefinition definition, SteadfastWillTraitRule rule)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        internal bool GrantsKnockbackImmunity(in EffectRuleInput input, EntityId owner)
        {
            if (input.State is not StatelessEffectStateSnapshot || TraitEffectOrigin.GetRequiredTraitKey(input.Origin, owner) != _rule.Key)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
            return _rule.GrantsKnockbackImmunity;
        }
    }
}
