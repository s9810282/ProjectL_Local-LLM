using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal sealed class EffectThornFieldEvaluation
    {
        public bool HasContribution { get; }
        public IReadOnlyList<EffectThornFieldContribution> Contributions { get; }
        public IReadOnlyList<EffectStateExpectation> Expectations { get; }

        public EffectThornFieldEvaluation(
            EffectThornFieldContribution[] contributions,
            EffectStateExpectation[] expectations)
        {
            if (contributions == null)
                throw new ArgumentNullException(nameof(contributions));
            if (expectations == null)
                throw new ArgumentNullException(nameof(expectations));

            var copiedContributions =
                (EffectThornFieldContribution[])contributions.Clone();
            Array.Sort(
                copiedContributions,
                (left, right) => left.Origin.CompareTo(right.Origin));

            var contributionOrigins = new HashSet<EffectOriginKey>();
            for (int i = 0; i < copiedContributions.Length; i++)
            {
                EffectThornFieldContribution contribution = copiedContributions[i];
                if (contribution.Origin.IsValid == false ||
                    contribution.ThornDamage < 1 ||
                    contribution.LifetimeUnitTurns < 1 ||
                    contribution.ApplicationCells == null)
                {
                    throw new ArgumentException(
                        "Invalid ThornField contribution.",
                        nameof(contributions));
                }

                var cells = new HashSet<CellCoord>();
                for (int cellIndex = 0;
                     cellIndex < contribution.ApplicationCells.Count;
                     cellIndex++)
                {
                    if (cells.Add(contribution.ApplicationCells[cellIndex]) == false)
                    {
                        throw new ArgumentException(
                            "Duplicate ThornField application cell.",
                            nameof(contributions));
                    }
                }

                if (contributionOrigins.Add(contribution.Origin) == false)
                {
                    throw new ArgumentException(
                        "Duplicate ThornField contribution origin.",
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
                        "Invalid ThornField expectation.",
                        nameof(expectations));
                }
            }

            if (contributionOrigins.SetEquals(expectationOrigins) == false)
            {
                throw new ArgumentException(
                    "ThornField contribution and expectation origins must match.",
                    nameof(expectations));
            }

            HasContribution = copiedContributions.Length > 0;
            Contributions = Array.AsReadOnly(copiedContributions);
            Expectations = Array.AsReadOnly(copiedExpectations);
        }
    }
}
