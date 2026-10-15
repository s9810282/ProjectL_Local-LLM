using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct HeadquartersPlacementResult
    {
        public bool Succeeded { get; }
        public HeadquartersPlacementFailureReason FailureReason { get; }
        public EntityId HeadquartersId { get; }
        public StageSnapshot CurrentStage { get; }

        private HeadquartersPlacementResult(
            bool succeeded,
            HeadquartersPlacementFailureReason failureReason,
            EntityId id,
            StageSnapshot snapshot)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            HeadquartersId = id;
            CurrentStage = snapshot;
        }

        public static HeadquartersPlacementResult Success(EntityId headquartersId, StageSnapshot currentStage)
        {
            if (headquartersId.IsEmpty) throw new ArgumentException(nameof(headquartersId));
            if (currentStage == null) throw new ArgumentNullException(nameof(currentStage));

            return new HeadquartersPlacementResult(true, HeadquartersPlacementFailureReason.None, headquartersId, currentStage);
        }

        public static HeadquartersPlacementResult Failure(HeadquartersPlacementFailureReason failureReason, StageSnapshot currentStage)
        {
            if (failureReason == HeadquartersPlacementFailureReason.None ||
                Enum.IsDefined(typeof(HeadquartersPlacementFailureReason), failureReason) == false)
                throw new ArgumentOutOfRangeException(nameof(failureReason));
            if (currentStage == null)
                throw new ArgumentNullException(nameof(currentStage));

            return new HeadquartersPlacementResult(false, failureReason, EntityId.Empty, currentStage);
        }
    }
}
