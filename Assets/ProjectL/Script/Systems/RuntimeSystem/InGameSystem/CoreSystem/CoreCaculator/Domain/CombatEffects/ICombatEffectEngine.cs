using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects
{
    internal interface ICombatEffectEngine
    {
        EffectAttackSweepEvaluation EvaluateAttackSweep(IReadOnlyList<EffectRuleBinding> bindings, in AttackSweepEffectContext context);

        bool RequiresOrderedHits(IReadOnlyList<EffectRuleBinding> bindings);

        EffectAttackTargetingEvaluation EvaluateAttackTargeting(IReadOnlyList<EffectRuleBinding> bindings);


        EffectAttackDamageEvaluation EvaluateAttackDamage(
            IReadOnlyList<EffectRuleBinding> bindings,
            in AttackDamageEffectContext context);

        EffectPierceFalloffEvaluation EvaluatePierceFalloff(
            IReadOnlyList<EffectRuleBinding> bindings,
            in PierceFalloffEffectContext context);

        /// <summary>
        /// 대상 하나의 거리 배율 기여를 곱합니다. 상태는 바꾸지 않습니다.
        /// </summary>
        EffectTargetDamageEvaluation EvaluateTargetDamage(
            IReadOnlyList<EffectRuleBinding> bindings, in TargetDamageEffectContext context);

        EffectTargetDistanceDamageEvaluation EvaluateTargetDistanceDamage(
            IReadOnlyList<EffectRuleBinding> bindings,
            in TargetDistanceDamageEffectContext context);

        EffectKnockbackEvaluation EvaluateKnockback(
            IReadOnlyList<EffectRuleBinding> bindings);

        /// <summary>
        /// 처치 연쇄 공격 기여를 모읍니다. 상태는 바꾸지 않습니다.
        /// </summary>
        EffectKillChainAttackEvaluation EvaluateKillChainAttack(
            IReadOnlyList<EffectRuleBinding> bindings);

        /// <summary>
        /// 죽은 유닛의 사망 특성 기여를 모읍니다. 상태는 바꾸지 않습니다.
        /// </summary>
        EffectDeathTraitEvaluation EvaluateDeathTraits(IReadOnlyList<EffectRuleBinding> bindings);

        /// <summary>
        /// 환급 요청을 모아 남은 AP 용량 상한까지만 배분합니다.
        /// </summary>


        /// <summary>
        /// 대상별 피격 경감을 모아 피해량 상한까지 배분합니다.
        /// </summary>
        EffectIncomingDamageEvaluation EvaluateIncomingDamage(
            IReadOnlyList<EffectRuleBinding> bindings,
            in IncomingDamageEffectContext context);

        CombatEffectPlan PlanMoveSucceeded(
            IReadOnlyList<EffectRuleBinding> bindings,
            in MoveSucceededEffectContext context);

        CombatEffectPlan PlanAttackSucceeded(
            IReadOnlyList<EffectRuleBinding> bindings,
            in AttackSucceededEffectContext context);

        CombatEffectPlan PlanTurnStarted(IReadOnlyList<EffectRuleBinding> bindings);

        CombatEffectPlan PlanTurnEnded(IReadOnlyList<EffectRuleBinding> bindings);

        EffectStageClearedEvaluation EvaluateStageCleared(
            IReadOnlyList<EffectRuleBinding> bindings,
            in StageClearedEffectContext context);
    }
}
