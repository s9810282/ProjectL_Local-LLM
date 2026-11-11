using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    /// <summary>등록 계약만 연결합니다. 모든 캐릭터의 턴 종료 회복은 전용 계산기가 처리합니다.</summary>
    internal sealed class RegenerationEffectRule : IEffectAssessmentHarmRule, IEffectMoveProjectionRule
    {
        public EffectAssessmentHarmScope AssessmentHarmScope => EffectAssessmentHarmScope.NoAdditionalImmediateHarm;
        public bool SupportsMoveProjection => true;

        internal RegenerationEffectRule(RegenerationTraitDefinition definition, RegenerationTraitRule rule)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
        }
    }
}
