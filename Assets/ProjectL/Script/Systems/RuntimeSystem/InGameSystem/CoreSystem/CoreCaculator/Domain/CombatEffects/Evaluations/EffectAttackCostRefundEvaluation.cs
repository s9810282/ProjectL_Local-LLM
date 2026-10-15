using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    /// <summary>
    /// 상한이 적용된 최종 환급액과 Origin별 실제 배분입니다.
    /// </summary>
    internal sealed class EffectAttackCostRefundEvaluation
    {
        public int EffectiveRefund { get; }
        public IReadOnlyList<EffectAttackCostRefundContribution> AllocatedRefunds { get; }
        public IReadOnlyList<EffectStateExpectation> Expectations { get; }

        public EffectAttackCostRefundEvaluation(
            int effectiveRefund,
            EffectAttackCostRefundContribution[] allocatedRefunds,
            EffectStateExpectation[] expectations)
        {
            if (effectiveRefund < 0) throw new ArgumentOutOfRangeException(nameof(effectiveRefund));
            if (allocatedRefunds == null) throw new ArgumentNullException(nameof(allocatedRefunds));
            if (expectations == null) throw new ArgumentNullException(nameof(expectations));

            var copiedAllocations = (EffectAttackCostRefundContribution[])allocatedRefunds.Clone();
            Array.Sort(copiedAllocations, (left, right) => left.Origin.CompareTo(right.Origin));

            int allocatedTotal = 0;
            var seenAllocations = new HashSet<EffectOriginKey>();
            foreach (EffectAttackCostRefundContribution allocation in copiedAllocations)
            {
                if (allocation.Origin.IsValid == false)
                    throw new ArgumentException("Invalid refund allocation", nameof(allocatedRefunds));
                if (seenAllocations.Add(allocation.Origin) == false)
                    throw new ArgumentException("Duplicate refund allocation", nameof(allocatedRefunds));

                allocatedTotal = checked(allocatedTotal + allocation.RequestedRefund);
            }

            if (allocatedTotal != effectiveRefund)
            {
                throw new ArgumentException(
                    "실제 배분 합이 최종 환급액과 다릅니다.", nameof(allocatedRefunds));
            }

            var copiedExpectations = (EffectStateExpectation[])expectations.Clone();
            Array.Sort(copiedExpectations, (left, right) => left.Origin.CompareTo(right.Origin));
            var seen = new HashSet<EffectOriginKey>();
            foreach (EffectStateExpectation expectation in copiedExpectations)
            {
                if (expectation.Origin.IsValid == false || expectation.ExpectedState == null)
                    throw new ArgumentException("Invalid state expectation", nameof(expectations));
                if (seen.Add(expectation.Origin) == false)
                    throw new ArgumentException("Duplicate expectation origin", nameof(expectations));
            }

            EffectiveRefund = effectiveRefund;
            AllocatedRefunds = Array.AsReadOnly(copiedAllocations);
            Expectations = Array.AsReadOnly(copiedExpectations);
        }
    }
}
