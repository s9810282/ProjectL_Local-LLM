using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules
{
    /// <summary>공격 지정과 대상 자격 정책을 상태 변경 없이 기여한다.</summary>
    internal interface IEffectAttackTargetingRule : ICombatEffectRule
    {
        EffectAttackTargetingContribution EvaluateAttackTargeting(in EffectRuleInput input);
    }

    internal interface IEffectAttackCostRule : ICombatEffectRule
    {
        EffectAttackCostContribution EvaluateAttackCost(
            in EffectRuleInput input,
            in AttackCostEffectContext context);
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
    /// 성공한 능동 공격에서 이동 비용 환급을 요청하는 capability입니다.
    /// 실제 배분과 상한 적용은 공통 evaluator가 담당합니다.
    /// </summary>
    internal interface IEffectAttackCostRefundRule : ICombatEffectRule
    {
        EffectAttackCostRefundContribution EvaluateAttackCostRefund(
            in EffectRuleInput input,
            in AttackActionCostRefundEffectContext context);

        /// <summary>확정된 실제 환급액을 받아 소유자 상태 전이를 계획합니다.</summary>
        EffectRuleResult PlanAttackCostRefundApplied(in EffectRuleInput input, int actualRefund);
    }

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
