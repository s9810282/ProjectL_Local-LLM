using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    /// <summary>아군 유닛 선택 권한만 기존 효과 합성에 기여합니다. 구조물의 공격 자격은 바꾸지 않습니다.</summary>
    internal sealed class PullEffectRule : IEffectAttackTargetingRule, IEffectAssessmentHarmRule
    {
        public EffectAssessmentHarmScope AssessmentHarmScope => EffectAssessmentHarmScope.ActingAttackOnly;

        internal PullEffectRule(PullTraitDefinition definition, PullTraitRule rule)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (rule == null) throw new ArgumentNullException(nameof(rule));
        }

        public EffectAttackTargetingContribution EvaluateAttackTargeting(in EffectRuleInput input) =>
            new(input.Origin, PullTraitRule.TargetingPolicy);
    }
}
