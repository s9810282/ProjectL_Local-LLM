using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Rules
{
    /// <summary>
    /// Rider 환급 규칙입니다. 턴당 실제 양수 환급 1회이며 요청액은 현재 Pending 이동 비용입니다.
    /// 규칙 자체는 live Unit, Map, Timeline을 변경하지 않고 요청/계획만 반환합니다.
    /// </summary>
    internal sealed class ManeuverRefundCategoryRule :
        ICombatEffectRule,
        IEffectAttackCostRefundRule,
        IEffectTurnStartedRule
    {
        private readonly ManeuverRefundCategoryRuleDefinition _definition;

        internal ManeuverRefundCategoryRule(ManeuverRefundCategoryRuleDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public EffectAttackCostRefundContribution EvaluateAttackCostRefund(
            in EffectRuleInput input,
            in AttackActionCostRefundEffectContext context)
        {
            ManeuverRefundEffectStateSnapshot state = RequireState(input);

            // 이미 이번 턴에 실제 환급을 사용했으면 더 요청하지 않는다.
            int requested = state.RefundUsedThisTurn ? 0 : context.AttackRefundEligibleMoveCost;
            return new EffectAttackCostRefundContribution(input.Origin, requested);
        }

        /// <summary>
        /// 공통 evaluator가 상한을 적용해 확정한 실제 환급액을 받아 사용 상태 전이를 계획합니다.
        /// 실제 배분이 0이면 기회를 소모하지 않습니다.
        /// </summary>
        public EffectRuleResult PlanAttackCostRefundApplied(in EffectRuleInput input, int actualRefund)
        {
            if (actualRefund < 0) throw new ArgumentOutOfRangeException(nameof(actualRefund));

            ManeuverRefundEffectStateSnapshot state = RequireState(input);
            if (actualRefund == 0 || state.RefundUsedThisTurn)
                return new EffectRuleResult(null, Array.Empty<EffectActivation>());

            return new EffectRuleResult(
                new EffectStateChange(
                    input.Origin,
                    state,
                    new ManeuverRefundEffectStateSnapshot(true)),
                new[]
                {
                    new EffectActivation(
                        input.Origin,
                        (int)CategoryActivationKind.RefundApplied,
                        actualRefund,
                        EntityId.Empty),
                });
        }

        public EffectRuleResult PlanTurnStarted(in EffectRuleInput input)
        {
            ManeuverRefundEffectStateSnapshot state = RequireState(input);
            if (state.RefundUsedThisTurn == false)
                return new EffectRuleResult(null, Array.Empty<EffectActivation>());

            return new EffectRuleResult(
                new EffectStateChange(
                    input.Origin,
                    state,
                    new ManeuverRefundEffectStateSnapshot(false)),
                Array.Empty<EffectActivation>());
        }

        private static ManeuverRefundEffectStateSnapshot RequireState(in EffectRuleInput input)
        {
            if (input.State is ManeuverRefundEffectStateSnapshot snapshot)
                return snapshot;

            throw new InvalidOperationException(
                "ManeuverRefund 규칙에는 ManeuverRefundEffectStateSnapshot이 필요합니다.");
        }
    }
}
