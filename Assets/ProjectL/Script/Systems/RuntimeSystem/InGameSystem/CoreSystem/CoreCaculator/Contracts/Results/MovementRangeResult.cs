using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>
    /// unit 하나에 대해 가능한 목적지 전체를 전달
    /// </summary>
    public readonly struct MovementRangeResult
    {
        public bool Succeeded { get; }
        public MovementFailureReason FailureReason { get; }
        public EntityId UnitId { get; }
        public ReadOnlyMemory<MovementDestination> Destinations { get; }

        private MovementRangeResult(bool succeeded, MovementFailureReason failureReason, EntityId unitId, MovementDestination[] destinations)
        {
            if (destinations == null)
                throw new ArgumentNullException(nameof(destinations));

            MovementDestination[] copiedDestinations = new MovementDestination[destinations.Length];
            Array.Copy(destinations, copiedDestinations, destinations.Length);

            Succeeded = succeeded;
            FailureReason = failureReason;
            UnitId = unitId;
            Destinations = copiedDestinations;
        }

        public static MovementRangeResult Failure(MovementFailureReason failureReason, EntityId unitId)
        {
            return new MovementRangeResult(false, failureReason, unitId, Array.Empty<MovementDestination>());
        }

        public static MovementRangeResult Success(EntityId unitId, MovementDestination[] destinations)
        {
            return new MovementRangeResult(true, MovementFailureReason.None, unitId, destinations);
        }
    }
}