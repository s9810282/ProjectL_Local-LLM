using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal sealed class EffectAttackSweepEvaluation
    {
        public EffectAttackSweepContribution? Contribution { get; }
        public IReadOnlyList<EffectStateExpectation> Expectations { get; }

        public EffectAttackSweepEvaluation(EffectAttackSweepContribution? contribution, EffectStateExpectation[] expectations)
        {
            if (expectations == null) throw new ArgumentNullException(nameof(expectations));
            if (expectations.Length != (contribution.HasValue ? 1 : 0) ||
                contribution.HasValue && (!contribution.Value.Origin.IsValid || contribution.Value.DamagePercent < 0 ||
                expectations[0].Origin != contribution.Value.Origin || expectations[0].ExpectedState == null))
                throw new ArgumentException("쓸기 기여와 상태 기대값의 출처가 일치해야 합니다.", nameof(expectations));
            Contribution = contribution;
            Expectations = Array.AsReadOnly((EffectStateExpectation[])expectations.Clone());
        }
    }
}
