using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal sealed class EffectAttackCostEvaluation
    {
        public int EffectiveCost { get; }
        public IReadOnlyList<EffectAttackCostContribution> Contributions { get; }
        public IReadOnlyList<EffectOriginKey> PositiveCostContributors { get; }
        public IReadOnlyList<EffectStateExpectation> Expectations { get; }

        public EffectAttackCostEvaluation(
            int effectiveCost,
            EffectAttackCostContribution[] contributions,
            EffectOriginKey[] positiveCostContributors,
            EffectStateExpectation[] expectations)
        {
            if (effectiveCost < 1) throw new ArgumentOutOfRangeException(nameof(effectiveCost));

            EffectAttackCostContribution[] copiedContributions = CopyContributions(contributions);
            EffectOriginKey[] copiedPositive = CopyPositiveContributors(positiveCostContributors);
            EffectStateExpectation[] copiedExpectations = CopyExpectations(expectations);

            EffectiveCost = effectiveCost;
            Contributions = Array.AsReadOnly(copiedContributions);
            PositiveCostContributors = Array.AsReadOnly(copiedPositive);
            Expectations = Array.AsReadOnly(copiedExpectations);
        }

        private static EffectAttackCostContribution[] CopyContributions(
            EffectAttackCostContribution[] contributions)
        {
            if (contributions == null) throw new ArgumentNullException(nameof(contributions));

            var copied = (EffectAttackCostContribution[])contributions.Clone();
            Array.Sort(copied, (left, right) => left.Origin.CompareTo(right.Origin));
            foreach (EffectAttackCostContribution contribution in copied)
            {
                if (contribution.Origin.IsValid == false ||
                    contribution.FlatReduction < 0 ||
                    contribution.MinimumCost < 1)
                {
                    throw new ArgumentException("Invalid attack cost contribution", nameof(contributions));
                }
            }

            return copied;
        }

        private static EffectOriginKey[] CopyPositiveContributors(
            EffectOriginKey[] positiveCostContributors)
        {
            if (positiveCostContributors == null)
                throw new ArgumentNullException(nameof(positiveCostContributors));

            var copied = (EffectOriginKey[])positiveCostContributors.Clone();
            Array.Sort(copied);
            var seen = new HashSet<EffectOriginKey>();
            foreach (EffectOriginKey origin in copied)
            {
                if (origin.IsValid == false)
                    throw new ArgumentException("Invalid contributor origin", nameof(positiveCostContributors));
                if (seen.Add(origin) == false)
                    throw new ArgumentException("Duplicate contributor origin", nameof(positiveCostContributors));
            }

            return copied;
        }

        private static EffectStateExpectation[] CopyExpectations(
            EffectStateExpectation[] expectations)
        {
            if (expectations == null) throw new ArgumentNullException(nameof(expectations));

            var copied = (EffectStateExpectation[])expectations.Clone();
            Array.Sort(copied, (left, right) => left.Origin.CompareTo(right.Origin));
            var seen = new HashSet<EffectOriginKey>();
            foreach (EffectStateExpectation expectation in copied)
            {
                if (expectation.Origin.IsValid == false || expectation.ExpectedState == null)
                    throw new ArgumentException("Invalid state expectation", nameof(expectations));
                if (seen.Add(expectation.Origin) == false)
                    throw new ArgumentException("Duplicate expectation origin", nameof(expectations));
            }

            return copied;
        }
    }
}
