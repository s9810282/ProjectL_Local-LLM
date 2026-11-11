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
    internal sealed class StationaryFireEffectRule : IEffectMoveSucceededRule, IEffectTargetDamageRule, IEffectAssessmentHarmRule
    {
        public EffectAssessmentHarmScope AssessmentHarmScope => EffectAssessmentHarmScope.NoAdditionalImmediateHarm;

        private readonly StationaryFireTraitDefinition _definition;
        private readonly StationaryFireTraitRule _rule;
        internal StationaryFireEffectRule(StationaryFireTraitDefinition definition, StationaryFireTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }
        public EffectRuleResult PlanMoveSucceeded(in EffectRuleInput input, in MoveSucceededEffectContext context)
        {
            var state = Required(input);
            bool moved = state.MovedThisOwnTurn || context.AnchorChanged && context.Distance > 0;
            return new EffectRuleResult(moved == state.MovedThisOwnTurn ? null : new EffectStateChange(input.Origin, input.State,
                new StationaryFireEffectStateSnapshot(moved)), Array.Empty<EffectActivation>());
        }
        public EffectTargetDamageContribution EvaluateTargetDamage(in EffectRuleInput input, in TargetDamageEffectContext context) =>
            new EffectTargetDamageContribution(input.Origin, false, _rule.Numerator(Required(input).MovedThisOwnTurn, _definition.ExtraDamagePercent), 100);
        private StationaryFireEffectStateSnapshot Required(in EffectRuleInput input)
        {
            if (input.State is not StationaryFireEffectStateSnapshot state ||
                TraitEffectOrigin.GetRequiredTraitKey(input.Origin, new EntityId(input.Origin.OwnerInstanceId)) != _definition.TraitKey)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
            return state;
        }
    }
}
