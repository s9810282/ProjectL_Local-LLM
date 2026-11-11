using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    /// <summary>
    /// 작렬은 공격자 기준 넉백 기여 하나를 낸다. 넉백 실행은 공통 넉백 계산기가 하고,
    /// 이 규칙은 거리와 부딪힘 비율만 알린다. 피해는 행위자 공격 처리 안에서만 난다(넉백과 같은 평가 범위).
    /// </summary>
    internal sealed class CrashImpactEffectRule : IEffectKnockbackRule, IEffectAssessmentHarmRule
    {
        public EffectAssessmentHarmScope AssessmentHarmScope => EffectAssessmentHarmScope.ActingAttackOnly;

        private readonly CrashImpactTraitDefinition _definition;
        private readonly CrashImpactTraitRule _rule;

        internal CrashImpactEffectRule(
            CrashImpactTraitDefinition definition,
            CrashImpactTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public EffectKnockbackContribution EvaluateKnockback(in EffectRuleInput input)
        {
            ValidateBoundInput(input);
            return new EffectKnockbackContribution(input.Origin, _definition.DistanceCells,
                collision: new EffectKnockbackCollision(_definition.WallDamagePercent, _definition.UnitDamagePercent));
        }

        private void ValidateBoundInput(in EffectRuleInput input)
        {
            EffectOriginKey origin = input.Origin;
            if (origin.OwnerInstanceId <= 0)
            {
                throw new ArgumentException(
                    "CrashImpact effect owner must be a valid unit.",
                    nameof(input));
            }

            TraitKey key = TraitEffectOrigin.GetRequiredTraitKey(
                origin,
                new EntityId(origin.OwnerInstanceId));
            if (key != _rule.Key)
            {
                throw new ArgumentException(
                    "CrashImpact effect requires its matching Trait origin.",
                    nameof(input));
            }
            if (input.State is not CrashImpactEffectStateSnapshot)
            {
                throw new ArgumentException(
                    "CrashImpact effect requires its matching snapshot.",
                    nameof(input));
            }
        }
    }
}
