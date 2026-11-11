using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules
{
    /// <summary>규칙 소유자가 공개 투영에서 생략하거나 확정할 수 없는 효과를 선언한다.</summary>
    internal interface IEffectAssessmentCoverageRule : ICombatEffectRule
    {
        Contracts.CombatEffects.CombatAssessmentGap AssessmentGaps { get; }
    }

    internal enum EffectAssessmentHarmScope
    {
        NoAdditionalImmediateHarm,
        ActingAttackOnly,
        Unbounded,
    }

    /// <summary>평가에서 확정할 수 없는 효과가 직접 피해 추정치 밖의 즉시 아군 피해를 늘리는 범위를 선언한다.</summary>
    internal interface IEffectAssessmentHarmRule : ICombatEffectRule
    {
        EffectAssessmentHarmScope AssessmentHarmScope { get; }
    }

    /// <summary>기존 공개 이동 투영으로 후속 공격 입력을 만들 수 있는 효과인지 선언한다. 전체 평가의 정밀도는 보장하지 않는다.</summary>
    internal interface IEffectMoveProjectionRule : ICombatEffectRule
    {
        bool SupportsMoveProjection { get; }
    }


    /// <summary>다음 타격을 계산하기 전에 직전 타격의 정산이 필요한 공격 규칙입니다.</summary>
    internal interface IEffectOrderedHitRule : ICombatEffectRule
    {
        bool RequiresOrderedHits { get; }
    }

    /// <summary>공격 지정과 대상 자격 정책을 상태 변경 없이 기여한다.</summary>
    internal interface IEffectAttackTargetingRule : ICombatEffectRule
    {
        EffectAttackTargetingContribution EvaluateAttackTargeting(in EffectRuleInput input);
    }


    /// <summary>대상별 방어 우회와 최종 배율을 기여합니다. 발동 조건은 개별 규칙이 소유합니다.</summary>

    internal interface IEffectTargetDamageRule : ICombatEffectRule
    {
        EffectTargetDamageContribution EvaluateTargetDamage(in EffectRuleInput input, in TargetDamageEffectContext context);
    }

    internal interface IEffectAttackDamageRule : ICombatEffectRule
    {
        EffectAttackDamageContribution EvaluateAttackDamage(
            in EffectRuleInput input,
            in AttackDamageEffectContext context);
    }

    internal interface IEffectPierceFalloffRule : ICombatEffectRule
    {
        EffectPierceFalloffContribution EvaluatePierceFalloff(
            in EffectRuleInput input,
            in PierceFalloffEffectContext context);
    }

    /// <summary>
    /// 공격자와 대상 사이 거리로 대상별 피해 백분율을 기여하는 capability입니다. 평가는 상태를 바꾸지 않습니다.
    /// </summary>
    internal interface IEffectTargetDistanceDamageRule : ICombatEffectRule
    {
        EffectTargetDistanceDamageContribution EvaluateTargetDistanceDamage(
            in EffectRuleInput input,
            in TargetDistanceDamageEffectContext context);
    }

    internal interface IEffectThornFieldRule : ICombatEffectRule
    {
        EffectThornFieldContribution EvaluateThornFieldApplication(
            in EffectRuleInput input,
            in ThornFieldApplicationEffectContext context);
    }

    internal interface IEffectKnockbackRule : ICombatEffectRule
    {
        EffectKnockbackContribution EvaluateKnockback(in EffectRuleInput input);
    }

    /// <summary>
    /// 공격으로 적을 처치하면 추가 공격을 요청하는 capability입니다. 활성 여부만 기여하며 상태를 바꾸지 않습니다.
    /// </summary>
    internal interface IEffectKillChainAttackRule : ICombatEffectRule
    {
        EffectKillChainAttackContribution EvaluateKillChainAttack(in EffectRuleInput input);
    }

    /// <summary>
    /// 소유자가 피해로 죽을 때 일으킬 효과(자폭 비율·공포 여부)를 기여하는 capability입니다. 평가는 상태를 바꾸지 않으며,
    /// 실제 실행(대상 찾기·피해·상태 적용)은 명령 완료 정산의 사망 특성 실행자가 맡습니다.
    /// </summary>
    internal interface IEffectDeathTraitRule : ICombatEffectRule
    {
        EffectDeathTraitContribution EvaluateDeath(in EffectRuleInput input);
    }

    /// <summary>
    /// 성공한 능동 공격에서 이동 비용 환급을 요청하는 capability입니다.
    /// 실제 배분과 상한 적용은 공통 evaluator가 담당합니다.
    /// </summary>


    /// <summary>
    /// 대상별 피격 피해 경감을 기여하는 capability입니다. 평가는 상태를 바꾸지 않습니다.
    /// </summary>
    internal interface IEffectIncomingDamageRule : ICombatEffectRule
    {
        EffectIncomingDamageContribution EvaluateIncomingDamage(
            in EffectRuleInput input,
            in IncomingDamageEffectContext context);
    }

    internal interface IEffectMoveSucceededRule : ICombatEffectRule
    {
        EffectRuleResult PlanMoveSucceeded(
            in EffectRuleInput input,
            in MoveSucceededEffectContext context);
    }

    internal interface IEffectAttackSucceededRule : ICombatEffectRule
    {
        EffectRuleResult PlanAttackSucceeded(
            in EffectRuleInput input,
            in AttackSucceededEffectContext context);
    }

    internal interface IEffectTurnStartedRule : ICombatEffectRule
    {
        EffectRuleResult PlanTurnStarted(in EffectRuleInput input);
    }

    internal interface IEffectTurnEndedRule : ICombatEffectRule
    {
        EffectRuleResult PlanTurnEnded(in EffectRuleInput input);
    }

    internal interface IEffectStageClearedRule : ICombatEffectRule
    {
        EffectHealContribution EvaluateStageCleared(
            in EffectRuleInput input,
            in StageClearedEffectContext context);
    }
}
