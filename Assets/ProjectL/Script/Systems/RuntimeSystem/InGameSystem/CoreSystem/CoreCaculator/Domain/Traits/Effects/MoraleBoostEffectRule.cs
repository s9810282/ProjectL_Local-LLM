using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class MoraleBoostEffectRule : ICombatEffectRule
    {
        private readonly MoraleBoostTraitDefinition _definition;
        private readonly MoraleBoostTraitRule _rule;
        internal MoraleBoostEffectRule(MoraleBoostTraitDefinition definition, MoraleBoostTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }
        internal TraitAttackGrant Evaluate(in EffectRuleInput input, int basis)
        {
            if (input.State is not StatelessEffectStateSnapshot ||
                TraitEffectOrigin.GetRequiredTraitKey(input.Origin, new EntityId(input.Origin.OwnerInstanceId)) != _rule.Key)
                throw new ArgumentException("특성 원본과 상태가 일치해야 합니다.", nameof(input));
            return new TraitAttackGrant(input.Origin, _rule.Amount(_definition, basis), _definition.DurationControllerTurnEnds);
        }
    }
}
