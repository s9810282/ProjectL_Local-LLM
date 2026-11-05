using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class PositionSwapEffectRule : ICombatEffectRule
    {
        internal int ShieldAmount { get; }
        private readonly PositionSwapTraitRule _rule;
        internal PositionSwapEffectRule(PositionSwapTraitDefinition definition, PositionSwapTraitRule rule)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            ShieldAmount = definition.ShieldAmount;
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }
        internal bool AllowsExchange(in EffectRuleInput input, CharacterCombatFacts actor, CharacterCombatFacts passenger)
        {
            if (input.State is not StatelessEffectStateSnapshot || TraitEffectOrigin.GetRequiredTraitKey(input.Origin, actor.EntityId) != _rule.Key)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
            return PositionSwapTraitRule.AllowsExchange(actor, passenger);
        }
    }
}
