using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct CombatTurnResult
    {
        public bool Succeeded { get; }
        public CombatTurnFailureReason FailureReason { get; }
        public EntityId EndedUnitId { get; }
        public StageSnapshot CurrentStage { get; }
        public ReadOnlyMemory<TraitActivationSnapshot> TraitActivations { get; }

        public CombatTurnResult(bool succeeded, CombatTurnFailureReason failureReason,
            EntityId endedUnitId, StageSnapshot currentStage)
            : this(
                succeeded,
                failureReason,
                endedUnitId,
                currentStage,
                Array.Empty<TraitActivationSnapshot>())
        {
        }

        public CombatTurnResult(
            bool succeeded,
            CombatTurnFailureReason failureReason,
            EntityId endedUnitId,
            StageSnapshot currentStage,
            TraitActivationSnapshot[] traitActivations)
        {
            if (traitActivations == null)
                throw new ArgumentNullException(nameof(traitActivations));
            if (succeeded == false && traitActivations.Length > 0)
            {
                throw new ArgumentException(
                    "실패한 Turn 결과에는 Trait Activation을 넣을 수 없습니다.",
                    nameof(traitActivations));
            }

            TraitActivationSnapshot[] copiedTraitActivations =
                new TraitActivationSnapshot[traitActivations.Length];
            for (int i = 0; i < traitActivations.Length; i++)
            {
                if (traitActivations[i] == null)
                    throw new ArgumentException(
                        "Trait Activation 목록에는 null 항목을 넣을 수 없습니다.",
                        nameof(traitActivations));

                copiedTraitActivations[i] = traitActivations[i];
            }

            if (succeeded && endedUnitId.IsEmpty)
                throw new ArgumentException("Successful turn end requires an actor.", nameof(endedUnitId));
            if (!succeeded && !endedUnitId.IsEmpty)
                throw new ArgumentException("Failed turn end cannot contain an ended actor.", nameof(endedUnitId));

            EndedUnitId = endedUnitId;
            Succeeded = succeeded;
            FailureReason = failureReason;
            CurrentStage = currentStage ?? throw new ArgumentNullException(nameof(currentStage));
            TraitActivations = copiedTraitActivations;
        }
    }
}
