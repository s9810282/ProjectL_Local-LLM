using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    /// <summary>
    /// 자폭을 사망 특성 기여(단계 비율)로 냅니다. 적 AI는 사망 특성을 모형화하지 않으므로(적 전용 특성 설계 §11, 계획 TP-10)
    /// 추가 즉시 피해 없음으로 선언합니다.
    /// </summary>
    internal sealed class SelfDestructEffectRule : IEffectDeathTraitRule, IEffectAssessmentHarmRule
    {
        public EffectAssessmentHarmScope AssessmentHarmScope => EffectAssessmentHarmScope.NoAdditionalImmediateHarm;

        private readonly SelfDestructTraitDefinition _definition;
        private readonly SelfDestructTraitRule _rule;

        internal SelfDestructEffectRule(SelfDestructTraitDefinition definition, SelfDestructTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public EffectDeathTraitContribution EvaluateDeath(in EffectRuleInput input)
        {
            if (input.State is not StatelessEffectStateSnapshot ||
                TraitEffectOrigin.GetRequiredTraitKey(input.Origin, new EntityId(input.Origin.OwnerInstanceId)) != _rule.Key)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
            return new EffectDeathTraitContribution(input.Origin, EnemyDeathTraitLevels.SelfDestructDamagePercent(_definition.Level), false);
        }
    }
}
