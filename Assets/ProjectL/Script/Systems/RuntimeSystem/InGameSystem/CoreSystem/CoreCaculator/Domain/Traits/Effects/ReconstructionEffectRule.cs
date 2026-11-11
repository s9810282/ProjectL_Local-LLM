using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    /// <summary>
    /// 재구성은 공통 피해 경계가 <see cref="ReconstructionTraitRule"/>을 직접 불러 처리하므로 효과 기여가 없습니다.
    /// 적 AI는 재구성을 모형화하지 않으므로(적 전용 특성 설계 §11, 계획 TP-10) 추가 즉시 피해 없음으로 선언합니다.
    /// </summary>
    internal sealed class ReconstructionEffectRule : IEffectAssessmentHarmRule
    {
        public EffectAssessmentHarmScope AssessmentHarmScope => EffectAssessmentHarmScope.NoAdditionalImmediateHarm;

        internal ReconstructionEffectRule(ReconstructionTraitDefinition definition, ReconstructionTraitRule rule)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (rule == null) throw new ArgumentNullException(nameof(rule));
        }
    }
}
