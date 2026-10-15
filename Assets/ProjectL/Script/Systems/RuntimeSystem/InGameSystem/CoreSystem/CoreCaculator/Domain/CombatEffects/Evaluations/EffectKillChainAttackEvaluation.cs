using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    /// <summary>
    /// 처치 연쇄 공격 기여를 모은 결과입니다. 기여가 여러 개여도 처치 1명당 추가 공격은 1회입니다.
    /// </summary>
    internal sealed class EffectKillChainAttackEvaluation
    {
        public bool HasContribution { get; }
        public IReadOnlyList<EffectKillChainAttackContribution> Contributions { get; }
        public IReadOnlyList<EffectStateExpectation> Expectations { get; }

        public EffectKillChainAttackEvaluation(
            EffectKillChainAttackContribution[] contributions,
            EffectStateExpectation[] expectations)
        {
            if (contributions == null)
                throw new ArgumentNullException(nameof(contributions));
            if (expectations == null)
                throw new ArgumentNullException(nameof(expectations));

            var copiedContributions = (EffectKillChainAttackContribution[])contributions.Clone();
            Array.Sort(
                copiedContributions,
                (left, right) => left.Origin.CompareTo(right.Origin));
            var contributionOrigins = new HashSet<EffectOriginKey>();
            foreach (EffectKillChainAttackContribution contribution in copiedContributions)
            {
                if (contribution.Origin.IsValid == false ||
                    contributionOrigins.Add(contribution.Origin) == false)
                {
                    throw new ArgumentException(
                        "유효하지 않거나 중복된 처치 연쇄 공격 기여입니다.",
                        nameof(contributions));
                }
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
                        "유효하지 않은 처치 연쇄 공격 상태 기대값입니다.",
                        nameof(expectations));
                }
            }

            if (contributionOrigins.SetEquals(expectationOrigins) == false)
            {
                throw new ArgumentException(
                    "처치 연쇄 공격 기여와 상태 기대값의 원본이 일치해야 합니다.",
                    nameof(expectations));
            }

            HasContribution = copiedContributions.Length > 0;
            Contributions = Array.AsReadOnly(copiedContributions);
            Expectations = Array.AsReadOnly(copiedExpectations);
        }
    }
}
