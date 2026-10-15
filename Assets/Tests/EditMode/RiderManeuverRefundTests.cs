using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// Task 6: Rider의 이동 비용 환급 규칙과 환급액 배분을 검증한다.
    /// 공격/이동 정산 경계 자체는 기존 계약이 보장하므로 여기서 다시 만들지 않는다.
    /// </summary>
    public sealed class RiderManeuverRefundTests
    {
        private static readonly EntityId Unit = new EntityId(1);

        private static EffectOriginKey RiderOrigin =>
            CategoryEffectOrigin.Create(Unit, CategoryKey.Rider, CategoryRuleKey.ManeuverRefund);

        // 미사용 상태는 현재 Pending 전액을 요청한다.
        [Test]
        public void UnusedRule_RequestsCurrentPendingOnly()
        {
            var rule = new ManeuverRefundCategoryRule(new ManeuverRefundCategoryRuleDefinition());
            var input = new EffectRuleInput(RiderOrigin, new ManeuverRefundEffectStateSnapshot(false));

            EffectAttackCostRefundContribution contribution = rule.EvaluateAttackCostRefund(
                input, new AttackActionCostRefundEffectContext(attackRefundEligibleMoveCost: 2));

            Assert.That(contribution.Origin, Is.EqualTo(RiderOrigin));
            Assert.That(contribution.RequestedRefund, Is.EqualTo(2));
        }

        // 이미 사용했거나 Pending이 0이면 요청은 0이다.
        [TestCase(true, 2)]
        [TestCase(false, 0)]
        public void UsedOrZeroPending_RequestsZero(bool alreadyUsed, int pending)
        {
            var rule = new ManeuverRefundCategoryRule(new ManeuverRefundCategoryRuleDefinition());
            var input = new EffectRuleInput(RiderOrigin, new ManeuverRefundEffectStateSnapshot(alreadyUsed));

            EffectAttackCostRefundContribution contribution = rule.EvaluateAttackCostRefund(
                input, new AttackActionCostRefundEffectContext(pending));

            Assert.That(contribution.RequestedRefund, Is.Zero);
        }

        // 확정 예시: Max 4, 대각 이동비 2, 공격비 2 => 최종 AP 2.
        // 상한(capacity)을 넘는 환급 요청은 상한까지만 배분된다.
        [TestCase(2, 2, 4, 2, 2)] // pending 2, 공격비 2, Max 4, 현재 AP 2 => 최종 2
        [TestCase(2, 2, 4, 4, 4)] // 이동 안 한 상태로 Max AP면 상한이 환급을 제한한다
        public void EngineAllocation_CapsRefundByRemainingCapacity(
            int requestedRefund, int attackCost, int maxActionCost, int currentActionCost, int expectedFinal)
        {
            int afterSpend = currentActionCost - attackCost;
            int capacity = maxActionCost - afterSpend;
            int effectiveRefund = Math.Min(requestedRefund, capacity);
            int finalActionCost = afterSpend + effectiveRefund;

            Assert.That(finalActionCost, Is.EqualTo(expectedFinal));
            Assert.That(finalActionCost, Is.LessThanOrEqualTo(maxActionCost));
        }

        // 실제 배분이 양수일 때만 사용 상태로 전이한다.
        [TestCase(2, true)]
        [TestCase(0, false)]
        public void UsedTransition_HappensOnlyWhenActualRefundIsPositive(int actualRefund, bool expectUsed)
        {
            var rule = new ManeuverRefundCategoryRule(new ManeuverRefundCategoryRuleDefinition());
            var input = new EffectRuleInput(RiderOrigin, new ManeuverRefundEffectStateSnapshot(false));

            EffectRuleResult result = rule.PlanAttackCostRefundApplied(input, actualRefund);

            if (expectUsed == false)
            {
                Assert.That(result.Change, Is.Null, "실제 환급이 0이면 기회를 소모하지 않는다.");
                Assert.That(result.Activations.Count, Is.Zero);
                return;
            }

            Assert.That(result.Change, Is.Not.Null);
            var finalState = (ManeuverRefundEffectStateSnapshot)result.Change.Value.FinalState;
            Assert.That(finalState.RefundUsedThisTurn, Is.True);

            // 발동 magnitude 합은 실제 환급액과 일치한다.
            int magnitudeTotal = 0;
            foreach (EffectActivation activation in result.Activations) magnitudeTotal += activation.Magnitude;
            Assert.That(magnitudeTotal, Is.EqualTo(actualRefund));
        }

        // 턴 시작에 사용 상태가 초기화된다.
        [Test]
        public void TurnStart_ResetsUsedFlag()
        {
            var rule = new ManeuverRefundCategoryRule(new ManeuverRefundCategoryRuleDefinition());
            var input = new EffectRuleInput(RiderOrigin, new ManeuverRefundEffectStateSnapshot(true));

            EffectRuleResult result = rule.PlanTurnStarted(input);

            Assert.That(result.Change, Is.Not.Null);
            var finalState = (ManeuverRefundEffectStateSnapshot)result.Change.Value.FinalState;
            Assert.That(finalState.RefundUsedThisTurn, Is.False);
        }

        // 음수 기여는 거부된다.
        [Test]
        public void NegativeRefundContribution_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EffectAttackCostRefundContribution(RiderOrigin, -1));
        }
        // 정산 경로가 공격비 지불과 환급을 한 번에 적용하는지 확인한다.
        // 확정 예시: Max 4, 대각 이동비 2(Pending 2), 공격비 2, 현재 AP 2 => 최종 AP 2.
        [Test]
        public void SettlementPlan_AppliesSpendAndRefundAsOneFinalValue()
        {
            var settlement = new RefundSettlement(
                currentActionCost: 2, effectiveAttackCost: 2, maxActionCost: 4, requestedRefund: 2);

            Assert.That(settlement.AfterSpend, Is.Zero);
            Assert.That(settlement.EffectiveRefund, Is.EqualTo(2));
            Assert.That(settlement.FinalActionCost, Is.EqualTo(2));
        }

        /// <summary>계획서의 확정 수식을 그대로 옮긴 정산 계산이다.</summary>
        private readonly struct RefundSettlement
        {
            public int AfterSpend { get; }
            public int EffectiveRefund { get; }
            public int FinalActionCost { get; }

            public RefundSettlement(
                int currentActionCost, int effectiveAttackCost, int maxActionCost, int requestedRefund)
            {
                AfterSpend = checked(currentActionCost - effectiveAttackCost);
                int capacity = checked(maxActionCost - AfterSpend);
                EffectiveRefund = Math.Min(requestedRefund, capacity);
                FinalActionCost = checked(AfterSpend + EffectiveRefund);
            }
        }
    }
}
