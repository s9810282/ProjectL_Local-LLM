using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal sealed class EffectPierceFalloffEvaluation
    {
        public int RemainingPercent { get; }
        public bool HasContribution { get; }
        public int? TargetBudget { get; }
        public IReadOnlyList<EffectPierceFalloffContribution> Contributions { get; }
        public IReadOnlyList<EffectStateExpectation> Expectations { get; }

        public EffectPierceFalloffEvaluation(
            int remainingPercent,
            int? targetBudget,
            EffectPierceFalloffContribution[] contributions,
            EffectStateExpectation[] expectations)
        {
            if (remainingPercent < 0 || remainingPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(remainingPercent));
            if (targetBudget < 0)
                throw new ArgumentOutOfRangeException(nameof(targetBudget));
            if (contributions == null)
                throw new ArgumentNullException(nameof(contributions));
            if (expectations == null)
                throw new ArgumentNullException(nameof(expectations));

            var copiedContributions =
                (EffectPierceFalloffContribution[])contributions.Clone();
            Array.Sort(
                copiedContributions,
                (left, right) => left.Origin.CompareTo(right.Origin));

            int foldedPercent = 100;
            int? foldedBudget = null;
            var seenContributionOrigins = new HashSet<EffectOriginKey>();
            foreach (EffectPierceFalloffContribution contribution in copiedContributions)
            {
                if (contribution.Origin.IsValid == false ||
                    contribution.RemainingPercent < 0 ||
                    contribution.RemainingPercent > 100 ||
                    contribution.TargetBudget < 0)
                {
                    throw new ArgumentException(
                        "Invalid Pierce falloff contribution.",
                        nameof(contributions));
                }
                if (seenContributionOrigins.Add(contribution.Origin) == false)
                {
                    throw new ArgumentException(
                        "Duplicate Pierce falloff contribution origin.",
                        nameof(contributions));
                }

                foldedPercent = Math.Min(
                    foldedPercent,
                    contribution.RemainingPercent);
                foldedBudget = foldedBudget.HasValue
                    ? Math.Min(foldedBudget.Value, contribution.TargetBudget)
                    : contribution.TargetBudget;
            }

            bool hasContribution = copiedContributions.Length > 0;
            if (hasContribution != targetBudget.HasValue ||
                foldedPercent != remainingPercent ||
                foldedBudget != targetBudget)
            {
                throw new ArgumentException(
                    "Pierce falloff folds do not match their contributions.");
            }

            var copiedExpectations = (EffectStateExpectation[])expectations.Clone();
            Array.Sort(
                copiedExpectations,
                (left, right) => left.Origin.CompareTo(right.Origin));
            var seenExpectationOrigins = new HashSet<EffectOriginKey>();
            foreach (EffectStateExpectation expectation in copiedExpectations)
            {
                if (expectation.Origin.IsValid == false ||
                    expectation.ExpectedState == null ||
                    seenExpectationOrigins.Add(expectation.Origin) == false)
                {
                    throw new ArgumentException(
                        "Invalid Pierce falloff expectation.",
                        nameof(expectations));
                }
            }

            if (seenContributionOrigins.SetEquals(seenExpectationOrigins) == false)
            {
                throw new ArgumentException(
                    "Pierce falloff contribution and expectation origins must match.",
                    nameof(expectations));
            }

            RemainingPercent = remainingPercent;
            HasContribution = hasContribution;
            TargetBudget = targetBudget;
            Contributions = Array.AsReadOnly(copiedContributions);
            Expectations = Array.AsReadOnly(copiedExpectations);
        }
    }
}
