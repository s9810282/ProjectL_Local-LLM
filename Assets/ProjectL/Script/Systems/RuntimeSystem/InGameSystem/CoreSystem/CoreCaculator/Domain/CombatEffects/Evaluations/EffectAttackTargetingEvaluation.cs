using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    /// <summary>출처별 기여를 합친 불변 정책이다. 상태 전이나 비용 정산을 발생시키지 않는다.</summary>
    internal sealed class EffectAttackTargetingEvaluation
    {
        public AttackTargetingPolicy Policy { get; }
        public IReadOnlyList<EffectAttackTargetingContribution> Contributions { get; }
        public IReadOnlyList<EffectStateExpectation> Expectations { get; }

        public EffectAttackTargetingEvaluation(EffectAttackTargetingContribution[] contributions,
            EffectStateExpectation[] expectations)
        {
            if (contributions == null) throw new ArgumentNullException(nameof(contributions));
            if (expectations == null) throw new ArgumentNullException(nameof(expectations));
            if (contributions.Length != expectations.Length)
                throw new ArgumentException("정책 기여와 상태 기대값의 개수가 다릅니다.", nameof(expectations));

            var copiedContributions = (EffectAttackTargetingContribution[])contributions.Clone();
            var copiedExpectations = (EffectStateExpectation[])expectations.Clone();
            Array.Sort(copiedContributions, (left, right) => left.Origin.CompareTo(right.Origin));
            Array.Sort(copiedExpectations, (left, right) => left.Origin.CompareTo(right.Origin));

            AttackTargetingPolicy policy = default;
            for (int i = 0; i < copiedContributions.Length; i++)
            {
                var contribution = copiedContributions[i];
                var expectation = copiedExpectations[i];
                if (!contribution.Origin.IsValid ||
                    (i > 0 && copiedContributions[i - 1].Origin == contribution.Origin))
                    throw new ArgumentException("정책 기여의 출처가 유효하지 않거나 중복됩니다.", nameof(contributions));
                // 정렬된 출처의 일대일 대응으로 기대값의 중복과 누락도 함께 검사한다.
                if (expectation.Origin != contribution.Origin || expectation.ExpectedState == null)
                    throw new ArgumentException("정책 기여와 상태 기대값의 출처가 일치하지 않습니다.", nameof(expectations));
                policy = policy.Combine(contribution.Policy);
            }

            Policy = policy;
            Contributions = Array.AsReadOnly(copiedContributions);
            Expectations = Array.AsReadOnly(copiedExpectations);
        }
    }
}
