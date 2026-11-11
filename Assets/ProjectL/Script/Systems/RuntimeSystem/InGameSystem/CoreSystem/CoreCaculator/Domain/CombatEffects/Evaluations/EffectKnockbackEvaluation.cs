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
        /// <summary>막혀 멈출 때의 부딪힘 비율이다. 부딪힘 값을 낸 기여가 없으면 비어 있다.</summary>
        public EffectKnockbackCollision? Collision { get; }
        /// <summary>부딪힘 발동 사건의 출처다. <see cref="Collision"/>이 있을 때만 값이 있다.</summary>
        public EffectOriginKey? CollisionOrigin { get; }

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
                    !Enum.IsDefined(typeof(EffectKnockbackMode), contribution.Mode) ||
                    contribution.Mode != copiedContributions[0].Mode ||
                    contributionOrigins.Add(contribution.Origin) == false)
                {
                    throw new ArgumentException(
                        "Invalid Knockback contribution or conflicting displacement modes.",
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
            // 부딪힘 비율은 기여들 중 가장 큰 값을 벽·유닛 따로 쓴다. 출처는 부딪힘 값이 있는 첫 기여(출처 순)다.
            for (int i = 0; i < copiedContributions.Length; i++)
            {
                if (copiedContributions[i].Collision is not EffectKnockbackCollision collision) continue;
                CollisionOrigin ??= copiedContributions[i].Origin;
                Collision = Collision is EffectKnockbackCollision prior
                    ? new EffectKnockbackCollision(Math.Max(prior.WallDamagePercent, collision.WallDamagePercent),
                        Math.Max(prior.UnitDamagePercent, collision.UnitDamagePercent))
                    : collision;
            }
            Expectations = Array.AsReadOnly(copiedExpectations);
        }
    }
}
