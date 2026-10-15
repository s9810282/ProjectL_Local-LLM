using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal sealed class EffectAttackDamageEvaluation
    {
        public int EffectiveDamage { get; }
        public IReadOnlyList<EffectAttackDamageContribution> Contributions { get; }
        public IReadOnlyList<EffectStateExpectation> Expectations { get; }

        public EffectAttackDamageEvaluation(
            int effectiveDamage,
            EffectAttackDamageContribution[] contributions,
            EffectStateExpectation[] expectations)
        {
            if (effectiveDamage < 0) throw new ArgumentOutOfRangeException(nameof(effectiveDamage));
            if (contributions == null) throw new ArgumentNullException(nameof(contributions));
            if (expectations == null) throw new ArgumentNullException(nameof(expectations));

            var copiedContributions = (EffectAttackDamageContribution[])contributions.Clone();
            Array.Sort(copiedContributions, (left, right) => left.Origin.CompareTo(right.Origin));
            foreach (EffectAttackDamageContribution contribution in copiedContributions)
            {
                if (contribution.Origin.IsValid == false || contribution.FlatBonus < 0)
                    throw new ArgumentException("Invalid attack damage contribution", nameof(contributions));
            }

            var copiedExpectations = (EffectStateExpectation[])expectations.Clone();
            Array.Sort(copiedExpectations, (left, right) => left.Origin.CompareTo(right.Origin));
            var seen = new HashSet<global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects.EffectOriginKey>();
            foreach (EffectStateExpectation expectation in copiedExpectations)
            {
                if (expectation.Origin.IsValid == false || expectation.ExpectedState == null)
                    throw new ArgumentException("Invalid state expectation", nameof(expectations));
                if (seen.Add(expectation.Origin) == false)
                    throw new ArgumentException("Duplicate expectation origin", nameof(expectations));
            }

            EffectiveDamage = effectiveDamage;
            Contributions = Array.AsReadOnly(copiedContributions);
            Expectations = Array.AsReadOnly(copiedExpectations);
        }
    }
}
