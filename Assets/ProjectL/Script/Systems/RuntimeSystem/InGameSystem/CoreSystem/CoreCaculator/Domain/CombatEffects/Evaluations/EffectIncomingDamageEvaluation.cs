using System;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    /// <summary>
    /// 상한이 적용된 최종 경감량과 Origin별 실제 배분입니다.
    /// </summary>
    internal sealed class EffectIncomingDamageEvaluation
    {
        public int EffectiveReduction { get; }
        public int FinalDamage { get; }
        public decimal DamageAfterPercentages { get; }
        public IReadOnlyList<int> RemainingDamagePercents { get; }
        public IReadOnlyList<EffectIncomingDamageContribution> AllocatedReductions { get; }
        public IReadOnlyList<EffectStateExpectation> Expectations { get; }

        public EffectIncomingDamageEvaluation(
            int incomingDamage,
            int effectiveReduction,
            EffectIncomingDamageContribution[] allocatedReductions,
            EffectStateExpectation[] expectations)
        {
            if (incomingDamage < 0) throw new ArgumentOutOfRangeException(nameof(incomingDamage));
            if (effectiveReduction < 0 || effectiveReduction > incomingDamage)
                throw new ArgumentOutOfRangeException(nameof(effectiveReduction));
            if (allocatedReductions == null) throw new ArgumentNullException(nameof(allocatedReductions));
            if (expectations == null) throw new ArgumentNullException(nameof(expectations));

            var copiedAllocations = (EffectIncomingDamageContribution[])allocatedReductions.Clone();
            Array.Sort(copiedAllocations, (left, right) => left.Origin.CompareTo(right.Origin));

            int allocatedTotal = 0;
            var seen = new HashSet<EffectOriginKey>();
            foreach (EffectIncomingDamageContribution allocation in copiedAllocations)
            {
                if (allocation.Origin.IsValid == false)
                    throw new ArgumentException("Invalid reduction allocation", nameof(allocatedReductions));
                if (seen.Add(allocation.Origin) == false)
                    throw new ArgumentException("Duplicate reduction allocation", nameof(allocatedReductions));

                allocatedTotal = checked(allocatedTotal + allocation.FlatReduction);
            }

            if (allocatedTotal != effectiveReduction)
            {
                throw new ArgumentException(
                    "실제 배분 합이 최종 경감량과 다릅니다.", nameof(allocatedReductions));
            }

            var copiedExpectations = (EffectStateExpectation[])expectations.Clone();
            Array.Sort(copiedExpectations, (left, right) => left.Origin.CompareTo(right.Origin));

            EffectiveReduction = effectiveReduction;
            RemainingDamagePercents = Array.AsReadOnly(copiedAllocations.Select(a => a.RemainingDamagePercent).ToArray());
            decimal exact = incomingDamage - effectiveReduction;
            foreach (int percent in RemainingDamagePercents) exact = exact * percent / 100m;
            DamageAfterPercentages = exact;
            FinalDamage = (int)decimal.Truncate(exact);
            AllocatedReductions = Array.AsReadOnly(copiedAllocations);
            Expectations = Array.AsReadOnly(copiedExpectations);
        }
    }
}
