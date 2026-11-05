using System;
using System.Collections.Generic;
using System.Numerics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    /// <summary>
    /// 대상 피해 기여의 정확한 접두 곱을 보관하며, 원래 피해에 배율을 적용한 뒤 한 번만 반올림합니다.
    /// </summary>
    internal sealed class EffectTargetDamageEvaluation
    {
        public bool BypassPhysicalDefense { get; }
        private readonly BigInteger[] _numerators;
        private readonly BigInteger[] _denominators;
        public bool HasContribution { get; }
        public IReadOnlyList<EffectTargetDamageContribution> Contributions { get; }
        public IReadOnlyList<EffectStateExpectation> Expectations { get; }

        public EffectTargetDamageEvaluation(
            EffectTargetDamageContribution[] contributions,
            EffectStateExpectation[] expectations)
        {
            if (contributions == null)
                throw new ArgumentNullException(nameof(contributions));
            if (expectations == null)
                throw new ArgumentNullException(nameof(expectations));

            var copiedContributions = (EffectTargetDamageContribution[])contributions.Clone();
            Array.Sort(
                copiedContributions,
                (left, right) => left.Origin.CompareTo(right.Origin));

            _numerators = new BigInteger[copiedContributions.Length + 1];
            _denominators = new BigInteger[copiedContributions.Length + 1];
            _numerators[0] = BigInteger.One;
            _denominators[0] = BigInteger.One;
            int index = 0;
            var contributionOrigins = new HashSet<EffectOriginKey>();
            foreach (EffectTargetDamageContribution contribution in copiedContributions)
            {
                if (contribution.Origin.IsValid == false || contribution.Numerator < 1 || contribution.Denominator < 1)
                {
                    throw new ArgumentException(
                        "유효하지 않은 대상 피해 기여입니다.",
                        nameof(contributions));
                }
                if (contributionOrigins.Add(contribution.Origin) == false)
                {
                    throw new ArgumentException(
                        "대상 피해 기여의 원본이 중복되었습니다.",
                        nameof(contributions));
                }

                _numerators[index + 1] = _numerators[index] * contribution.Numerator;
                _denominators[index + 1] = _denominators[index] * contribution.Denominator;
                index++;
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
                        "유효하지 않은 대상 피해 상태 기대값입니다.",
                        nameof(expectations));
                }
            }

            if (contributionOrigins.SetEquals(expectationOrigins) == false)
            {
                throw new ArgumentException(
                    "대상 피해 기여와 상태 기대값의 원본이 일치해야 합니다.",
                    nameof(expectations));
            }

            BypassPhysicalDefense = Array.Exists(copiedContributions, c => c.BypassPhysicalDefense);
            HasContribution = copiedContributions.Length > 0;
            Contributions = Array.AsReadOnly(copiedContributions);
            Expectations = Array.AsReadOnly(copiedExpectations);
        }

        internal int Scale(int baseDamage, int contributionCount)
        {
            if (baseDamage < 0) throw new ArgumentOutOfRangeException(nameof(baseDamage));
            if (contributionCount < 0 || contributionCount > Contributions.Count)
                throw new ArgumentOutOfRangeException(nameof(contributionCount));
            var denominator = _denominators[contributionCount];
            return (int)((baseDamage * _numerators[contributionCount] + denominator / 2) / denominator);
        }
    }
}
