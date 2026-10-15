using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public sealed class CombatTimelineAdvanceResult
    {
        public bool Succeeded { get; }
        public CombatTimelineAdvanceFailureReason FailureReason { get; }
        public CombatTimelineAdvanceKind Kind { get; }
        public EntityId? ActiveUnitId { get; }
        public StageGimmickActivationSnapshot GimmickActivation { get; }
        public ReadOnlyMemory<TraitActivationSnapshot> TraitActivations { get; }
        public StageSnapshot CurrentStage { get; }

        private CombatTimelineAdvanceResult(
            bool succeeded, 
            CombatTimelineAdvanceFailureReason failureReason, 
            CombatTimelineAdvanceKind kind, 
            EntityId? activeUnitId, 
            StageGimmickActivationSnapshot gimmickActivation, 
            TraitActivationSnapshot[] traitActivations, 
            StageSnapshot currentStage)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            Kind = kind;
            ActiveUnitId = activeUnitId;
            GimmickActivation = gimmickActivation;
            TraitActivations = traitActivations;
            CurrentStage = currentStage;
        }

        public static CombatTimelineAdvanceResult UnitStarted(
            EntityId activeUnitId,
            StageSnapshot currentStage,
            TraitActivationSnapshot[] traitActivations)
        {
            if (activeUnitId.IsEmpty)
                throw new ArgumentException("Active Unit ID는 비어 있을 수 없습니다.", nameof(activeUnitId));
            if (currentStage == null)
                throw new ArgumentNullException(nameof(currentStage));
            if (traitActivations == null)
                throw new ArgumentNullException(nameof(traitActivations));

            TraitActivationSnapshot[] copiedActivations = new TraitActivationSnapshot[traitActivations.Length];
            for (int i = 0; i < traitActivations.Length; i++)
            {
                TraitActivationSnapshot snapshot = traitActivations[i];
                if (snapshot == null)
                    throw new ArgumentException($"{i}번째 snapshot이 비어있습니다.", nameof(traitActivations));
                copiedActivations[i] = snapshot;
            }

            return new CombatTimelineAdvanceResult(
                true,
                CombatTimelineAdvanceFailureReason.None,
                CombatTimelineAdvanceKind.UnitTurnStarted,
                activeUnitId,
                null,
                copiedActivations,
                currentStage);
        }

        public static CombatTimelineAdvanceResult GimmickActivated(StageGimmickActivationSnapshot activation, StageSnapshot currentStage)
        {
            if (activation == null)
                throw new ArgumentNullException(nameof(activation));
            if (currentStage == null)
                throw new ArgumentNullException(nameof(currentStage));

            return new CombatTimelineAdvanceResult(
                true,
                CombatTimelineAdvanceFailureReason.None,
                CombatTimelineAdvanceKind.StageGimmickActivated, 
                null,
                activation,
                Array.Empty<TraitActivationSnapshot>(),
                currentStage);
        }

        public static CombatTimelineAdvanceResult Failure(CombatTimelineAdvanceFailureReason failureReason, StageSnapshot currentStage)
        {
            if (failureReason == CombatTimelineAdvanceFailureReason.None ||
                Enum.IsDefined(typeof(CombatTimelineAdvanceFailureReason), failureReason) == false)
                throw new ArgumentOutOfRangeException(nameof(failureReason));

            if (currentStage == null)
                throw new ArgumentNullException(nameof(currentStage));

            return new CombatTimelineAdvanceResult(
                false,
                failureReason,
                CombatTimelineAdvanceKind.None,
                null,
                null,
                Array.Empty<TraitActivationSnapshot>(),
                currentStage);
        }
    }
}
