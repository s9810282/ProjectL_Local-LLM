using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    /// <summary>
    /// 대상 하나에 대한 거리 배율 기여를 곱한 결과입니다. 피해에는 Numerator/Denominator를 한 번만 적용합니다.
    /// </summary>
    internal sealed class EffectTargetDistanceDamageEvaluation
    {
        public int Numerator { get; }
        public int Denominator { get; }
        public bool HasContribution { get; }
        public IReadOnlyList<EffectTargetDistanceDamageContribution> Contributions { get; }
        public IReadOnlyList<EffectStateExpectation> Expectations { get; }

        public EffectTargetDistanceDamageEvaluation(
            EffectTargetDistanceDamageContribution[] contributions,
            EffectStateExpectation[] expectations)
        {
            if (contributions == null)
                throw new ArgumentNullException(nameof(contributions));
            if (expectations == null)
                throw new ArgumentNullException(nameof(expectations));

            var copiedContributions = (EffectTargetDistanceDamageContribution[])contributions.Clone();
            Array.Sort(
                copiedContributions,
                (left, right) => left.Origin.CompareTo(right.Origin));

            int numerator = 1;
            int denominator = 1;
            var contributionOrigins = new HashSet<EffectOriginKey>();
            foreach (EffectTargetDistanceDamageContribution contribution in copiedContributions)
            {
                if (contribution.Origin.IsValid == false || contribution.Percent < 0)
                {
                    throw new ArgumentException(
                        "유효하지 않은 거리 배율 기여입니다.",
                        nameof(contributions));
                }
                if (contributionOrigins.Add(contribution.Origin) == false)
                {
                    throw new ArgumentException(
                        "거리 배율 기여의 원본이 중복되었습니다.",
                        nameof(contributions));
                }

                numerator = checked(numerator * contribution.Percent);
                denominator = checked(denominator * 100);
            }

            var copiedExpectations = (EffectStateExpectation[])expectations.Clone();
            Array.Sort(
                copiedExpectations,
                (left, right) => left.Origin.CompareTo(right.Origin));
            var expectationOrigins = new HashSet<EffectOriginKey>();
            foreach (EffectStateExpectation expectation in copiedExpectations)
            {
                if (expectation.Origin.IsValid == false ||
                    expectation.ExpectedState == null ||
                    expectationOrigins.Add(expectation.Origin) == false)
                {
                    throw new ArgumentException(
                        "유효하지 않은 거리 배율 상태 기대값입니다.",
                        nameof(expectations));
                }
            }

            if (contributionOrigins.SetEquals(expectationOrigins) == false)
            {
                throw new ArgumentException(
                    "거리 배율 기여와 상태 기대값의 원본이 일치해야 합니다.",
                    nameof(expectations));
            }

            Numerator = numerator;
            Denominator = denominator;
            HasContribution = copiedContributions.Length > 0;
            Contributions = Array.AsReadOnly(copiedContributions);
            Expectations = Array.AsReadOnly(copiedExpectations);
        }
    }
}
