using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact
{
    internal static class ArtifactActivationCalculator
    {
        internal static ArtifactActivationSnapshot[] FromHealing(
            EffectStageClearedEvaluation evaluation)
        {
            var activations = new ArtifactActivationSnapshot[evaluation.Contributions.Count];
            long remainingApplied = evaluation.FinalHealth - evaluation.ProjectedHealth;
            for (int i = 0; i < activations.Length; i++)
            {
                EffectHealContribution contribution = evaluation.Contributions[i];
                long applied = Math.Min(contribution.RequestedAmount, remainingApplied);
                activations[i] = new ArtifactActivationSnapshot(
                    contribution.Origin,
                    contribution.TargetId,
                    contribution.RequestedAmount,
                    applied);
                remainingApplied -= applied;
            }

            if (remainingApplied != 0)
                throw new ArgumentException("Applied Artifact healing does not match final health.", nameof(evaluation));
            return activations;
        }
    }
}
