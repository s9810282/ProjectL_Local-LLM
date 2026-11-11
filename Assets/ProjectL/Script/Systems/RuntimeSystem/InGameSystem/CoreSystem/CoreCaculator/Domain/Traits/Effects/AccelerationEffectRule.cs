using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class AccelerationEffectRule : IEffectMoveSucceededRule, IEffectAssessmentHarmRule
    {
        public EffectAssessmentHarmScope AssessmentHarmScope => EffectAssessmentHarmScope.NoAdditionalImmediateHarm;

        private readonly AccelerationTraitDefinition _definition;
        private readonly AccelerationTraitRule _rule;
        internal AccelerationEffectRule(AccelerationTraitDefinition definition, AccelerationTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }
        public EffectRuleResult PlanMoveSucceeded(in EffectRuleInput input, in MoveSucceededEffectContext context)
        {
            if (input.State is not AccelerationEffectStateSnapshot state ||
                TraitEffectOrigin.GetRequiredTraitKey(input.Origin, new EntityId(input.Origin.OwnerInstanceId)) != _definition.TraitKey)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
            int final = _rule.Gain(state.HeldAttackBonus, context.AnchorChanged ? context.Distance : 0, _definition.AttackPerMovedCell);
            return new EffectRuleResult(final == state.HeldAttackBonus ? null : new EffectStateChange(input.Origin, input.State,
                new AccelerationEffectStateSnapshot(final)), Array.Empty<EffectActivation>());
        }
    }
}
