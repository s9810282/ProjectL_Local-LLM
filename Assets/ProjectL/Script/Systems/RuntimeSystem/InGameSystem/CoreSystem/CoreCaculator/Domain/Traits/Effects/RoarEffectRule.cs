using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    /// <summary>
    /// 포효는 공격·이동 효과 훅에 참여하지 않는다. 피격 집계는 피해 writer(RoarHitCounter), 턴 종료 집계는 진영 종료 정리(RoarTurnApplication),
    /// 반응은 BossRoarCalculator가 처리한다.
    /// 등록 계약을 맞추기 위한 수동 규칙이며 효과 기여를 만들지 않는다.
    /// </summary>
    internal sealed class RoarEffectRule : ICombatEffectRule, IEffectAssessmentCoverageRule
    {
        public Contracts.CombatEffects.CombatAssessmentGap AssessmentGaps => Contracts.CombatEffects.CombatAssessmentGap.ActualOnlyReaction;

        internal RoarEffectRule(RoarTraitDefinition definition, RoarTraitRule rule)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (rule == null) throw new ArgumentNullException(nameof(rule));
        }
    }
}
