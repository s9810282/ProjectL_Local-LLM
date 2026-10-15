using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal sealed class EffectKnockbackEvaluation
    {
        public bool HasContribution { get; }
        public IReadOnlyList<EffectKnockbackContribution> Contributions { get; }
        public IReadOnlyList<EffectStateExpectation> Expectations { get; }

        public EffectKnockbackEvaluation(
            EffectKnockbackContribution[] contributions,
            EffectStateExpectation[] expectations)
        {
            if (contributions == null)
                throw new ArgumentNullException(nameof(contributions));
            if (expectations == null)
                throw new ArgumentNullException(nameof(expectations));

            var copiedContributions = (EffectKnockbackContribution[])contributions.Clone();
            Array.Sort(
                copiedContributions,
                (left, right) => left.Origin.CompareTo(right.Origin));
            var contributionOrigins = new HashSet<EffectOriginKey>();
            for (int i = 0; i < copiedContributions.Length; i++)
            {
                EffectKnockbackContribution contribution = copiedContributions[i];
                if (contribution.Origin.IsValid == false ||
                    contribution.DistanceCells < 1 ||
                    contributionOrigins.Add(contribution.Origin) == false)
                {
                    throw new ArgumentException(
                        "Invalid Knockback contribution.",
                        nameof(contributions));
                }
            }

            var copiedExpectations = (EffectStateExpectation[])expectations.Clone();
            Array.Sort(
                copiedExpectations,
                (left, right) => left.Origin.CompareTo(right.Origin));
            var expectationOrigins = new HashSet<EffectOriginKey>();
            for (int i = 0; i < copiedExpectations.Length; i++)
            {
                EffectStateExpectation expectation = copiedExpectations[i];
                if (expectation.Origin.IsValid == false ||
                    expectation.ExpectedState == null ||
                    expectationOrigins.Add(expectation.Origin) == false)
                {
                    throw new ArgumentException(
                        "Invalid Knockback expectation.",
                        nameof(expectations));
                }
            }

            if (contributionOrigins.SetEquals(expectationOrigins) == false)
            {
                throw new ArgumentException(
                    "Knockback contribution and expectation origins must match.",
                    nameof(expectations));
            }

            HasContribution = copiedContributions.Length > 0;
            Contributions = Array.AsReadOnly(copiedContributions);
            Expectations = Array.AsReadOnly(copiedExpectations);
        }
    }
}
