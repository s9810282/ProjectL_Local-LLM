using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal sealed class EffectStageClearedEvaluation
    {
        internal EntityId TargetId { get; }
        internal int ProjectedHealth { get; }
        internal int FinalHealth { get; }
        internal long RequestedHealing { get; }
        internal IReadOnlyList<EffectHealContribution> Contributions { get; }
        internal IReadOnlyList<EffectStateExpectation> Expectations { get; }

        internal EffectStageClearedEvaluation(
            EntityId targetId,
            int projectedHealth,
            int finalHealth,
            long requestedHealing,
            EffectHealContribution[] contributions,
            EffectStateExpectation[] expectations)
        {
            if (targetId.IsEmpty) throw new ArgumentException(nameof(targetId));
            if (projectedHealth <= 0) throw new ArgumentOutOfRangeException(nameof(projectedHealth));
            if (finalHealth < projectedHealth) throw new ArgumentOutOfRangeException(nameof(finalHealth));
            if (requestedHealing < 0) throw new ArgumentOutOfRangeException(nameof(requestedHealing));
            if (contributions == null) throw new ArgumentNullException(nameof(contributions));
            if (expectations == null) throw new ArgumentNullException(nameof(expectations));

            var copiedContributions = (EffectHealContribution[])contributions.Clone();
            Array.Sort(copiedContributions, (left, right) => left.Origin.CompareTo(right.Origin));
            long total = 0;
            var seen = new HashSet<EffectOriginKey>();
            foreach (EffectHealContribution contribution in copiedContributions)
            {
                if (contribution.TargetId != targetId || seen.Add(contribution.Origin) == false)
                    throw new ArgumentException("Invalid StageCleared contribution.", nameof(contributions));
                total = checked(total + contribution.RequestedAmount);
            }
            if (total != requestedHealing)
                throw new ArgumentException("StageCleared healing total does not match contributions.", nameof(contributions));

            TargetId = targetId;
            ProjectedHealth = projectedHealth;
            FinalHealth = finalHealth;
            RequestedHealing = requestedHealing;
            Contributions = Array.AsReadOnly(copiedContributions);
            Expectations = Array.AsReadOnly((EffectStateExpectation[])expectations.Clone());
        }
    }
}
