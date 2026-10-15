using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// UnitMovementService가 확정한 이동 실행 사실을 StageCombatFacade에 전달하는 중간 결과입니다.
    /// </summary>
    /// <remarks>기존 공개 결과 값의 복사와 유효성 검증 계약을 보존한다. 런타임 상태나 실행 서비스를 소유하지 않는다.</remarks>
    public readonly struct MovementExecutionResult
    {
        public bool Succeeded { get; }
        public MovementFailureReason FailureReason { get; }
        public EntityId UnitId { get; }
        public CellCoord? FromAnchor { get; }
        public MovementDestination? Destination { get; }
        public ReadOnlyMemory<TraitActivationSnapshot> TraitActivations { get; }
        public ReadOnlyMemory<CategoryActivationSnapshot> CategoryActivations { get; }

        private MovementExecutionResult(
            bool succeeded,
            MovementFailureReason failureReason,
            EntityId unitId,
            CellCoord? fromAnchor,
            MovementDestination? destination,
            TraitActivationSnapshot[] traitActivations,
            CategoryActivationSnapshot[] categoryActivations)
        {
            if (traitActivations == null)
                throw new ArgumentNullException(nameof(traitActivations));
            if (categoryActivations == null)
                throw new ArgumentNullException(nameof(categoryActivations));

            TraitActivationSnapshot[] copiedActivations =
                new TraitActivationSnapshot[traitActivations.Length];
            for (int i = 0; i < traitActivations.Length; i++)
            {
                if (traitActivations[i] == null)
                {
                    throw new ArgumentException(
                        "Trait Activation 목록에는 null 항목을 넣을 수 없습니다.",
                        nameof(traitActivations));
                }

                copiedActivations[i] = traitActivations[i];
            }

            Succeeded = succeeded;
            FailureReason = failureReason;
            UnitId = unitId;
            FromAnchor = fromAnchor;
            Destination = destination;
            TraitActivations = copiedActivations;
            CategoryActivations = (CategoryActivationSnapshot[])categoryActivations.Clone();
        }

        public static MovementExecutionResult Success(
            EntityId unitId,
            CellCoord fromAnchor,
            MovementDestination destination,
            TraitActivationSnapshot[] traitActivations)
        {
            return Success(unitId, fromAnchor, destination, traitActivations,
                Array.Empty<CategoryActivationSnapshot>());
        }

        public static MovementExecutionResult Success(
            EntityId unitId,
            CellCoord fromAnchor,
            MovementDestination destination,
            TraitActivationSnapshot[] traitActivations,
            CategoryActivationSnapshot[] categoryActivations)
        {
            if (unitId == EntityId.Empty)
                throw new ArgumentException("성공한 이동의 UnitId는 Empty일 수 없습니다.", nameof(unitId));

            return new MovementExecutionResult(
                true,
                MovementFailureReason.None,
                unitId,
                fromAnchor,
                destination,
                traitActivations,
                categoryActivations);
        }

        public static MovementExecutionResult Failure(MovementFailureReason failureReason, EntityId unitId)
        {
            if (failureReason == MovementFailureReason.None)
                throw new ArgumentException("실패 결과의 이유는 None일 수 없습니다.", nameof(failureReason));

            return new MovementExecutionResult(false, failureReason, unitId, null, null,
                Array.Empty<TraitActivationSnapshot>(), Array.Empty<CategoryActivationSnapshot>());
        }
    }

}