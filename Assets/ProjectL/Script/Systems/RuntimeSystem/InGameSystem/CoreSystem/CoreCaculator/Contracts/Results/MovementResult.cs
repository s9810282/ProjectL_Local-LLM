using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>
    /// 실제 이동 Command가 확정한 성공·실패 사실과 처리 직후의 전체 Stage 상태입니다.
    /// </summary>
    /// <remarks>
    /// Preview와 달리 이동 거리와 행동력 값은 실제 상태 변경에 사용된 값입니다.
    /// ActionCostDelta는 이번 이동에서 실제 정산된 차액이며 환급 시 음수가 될 수 있습니다.
    /// 실패 시 이동 세부 정보는 비어 있지만 CurrentStage는 항상 제공되어 View가 Core 상태를 다시 적용할 수 있습니다.
    /// </remarks>
    public readonly struct MovementResult
    {
        public bool Succeeded { get; }
        public MovementFailureReason FailureReason { get; }
        public EntityId UnitId { get; }
        public CellCoord? FromAnchor { get; }
        public CellCoord? TargetAnchor { get; }
        public ReadOnlyMemory<CellCoord> DestinationOccupiedCells { get; }
        public int StepDistance { get; }
        public int DistanceFromCheckpoint { get; }
        public int TotalMoveActionCost { get; }
        public int ActionCostDelta { get; }
        public StageSnapshot CurrentStage { get; }
        public ReadOnlyMemory<TraitActivationSnapshot> TraitActivations { get; }
        public ReadOnlyMemory<CategoryActivationSnapshot> CategoryActivations { get; }

        private MovementResult(
            bool succeeded, 
            MovementFailureReason failureReason, 
            EntityId entityId, 
            CellCoord? fromAnchor,
            CellCoord? targetAnchor,
            CellCoord[] destinationOccupiedCells,
            int stepDistance,
            int distanceFromCheckpoint,
            int totalMoveActionCost,
            int actionCostDelta,
            StageSnapshot currentStage,
            TraitActivationSnapshot[] traitActivations,
            CategoryActivationSnapshot[] categoryActivations)
        {
            if (stepDistance < 0)
                throw new ArgumentOutOfRangeException(nameof(stepDistance));
            if (distanceFromCheckpoint < 0)
                throw new ArgumentOutOfRangeException(nameof(distanceFromCheckpoint));
            if (totalMoveActionCost < 0)
                throw new ArgumentOutOfRangeException(nameof(totalMoveActionCost));
            if (currentStage == null)
                throw new ArgumentNullException(nameof(currentStage));
            if (destinationOccupiedCells == null)
                throw new ArgumentNullException(nameof(destinationOccupiedCells));
            if (traitActivations == null)
                throw new ArgumentNullException(nameof(traitActivations));
            if (categoryActivations == null)
                throw new ArgumentNullException(nameof(categoryActivations));

            CellCoord[] copiedCells = new CellCoord[destinationOccupiedCells.Length];
            Array.Copy(destinationOccupiedCells, copiedCells, destinationOccupiedCells.Length);

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

            Succeeded = succeeded;
            FailureReason = failureReason;
            UnitId = entityId;
            FromAnchor = fromAnchor;
            TargetAnchor = targetAnchor;
            DestinationOccupiedCells = copiedCells;
            StepDistance = stepDistance;
            DistanceFromCheckpoint = distanceFromCheckpoint;
            TotalMoveActionCost = totalMoveActionCost;
            ActionCostDelta = actionCostDelta;
            CurrentStage = currentStage;
            TraitActivations = copiedTraitActivations;
            CategoryActivations = (CategoryActivationSnapshot[])categoryActivations.Clone();
        }

        public static MovementResult Success(
            EntityId entityId,
            CellCoord fromAnchor,
            CellCoord targetAnchor,
            CellCoord[] destinationOccupiedCells,
            int stepDistance,
            int distanceFromCheckpoint,
            int totalMoveActionCost,
            int actionCostDelta,
            StageSnapshot currentStage,
            TraitActivationSnapshot[] traitActivations,
            CategoryActivationSnapshot[] categoryActivations)
        {
            if (entityId == EntityId.Empty)
                throw new ArgumentException("성공한 이동의 UnitId는 Empty일 수 없습니다.", nameof(entityId));

            return new MovementResult(
                true,
                MovementFailureReason.None,
                entityId,
                fromAnchor,
                targetAnchor,
                destinationOccupiedCells,
                stepDistance,
                distanceFromCheckpoint,
                totalMoveActionCost,
                actionCostDelta,
                currentStage,
                traitActivations,
                categoryActivations);
        }

        public static MovementResult Failure(MovementFailureReason failureReason, EntityId entityId, StageSnapshot currentStage)
        {
            if (failureReason == MovementFailureReason.None)
                throw new ArgumentException(nameof(failureReason));

            return new MovementResult(
                false,
                failureReason,
                entityId,
                null,
                null,
                Array.Empty<CellCoord>(),
                0,
                0,
                0,
                0,
                currentStage,
                Array.Empty<TraitActivationSnapshot>(),
                Array.Empty<CategoryActivationSnapshot>());
        }
    }
}
