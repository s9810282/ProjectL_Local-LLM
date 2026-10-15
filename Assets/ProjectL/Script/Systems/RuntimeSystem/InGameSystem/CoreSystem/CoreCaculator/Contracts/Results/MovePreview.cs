using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using System.Collections.Generic;
using System.Linq;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct MovePreview
    {
        public bool CanMove { get; }
        public MovementFailureReason FailureReason { get; }
        public MovementDestination? Destination { get; }
        public ReadOnlyMemory<TraitStatusSnapshot> ProjectedTraitStatuses { get; }
        public CategorySnapshot ProjectedCategory { get; }
        public MovementPathResult PathMovement { get; }
        public int ValidStepCount { get; }
        public CharacterCombatFacts ProjectedActor { get; }
        public IReadOnlyList<CombatEvent> PublicEffects { get; }
        public int QuotedActionCost { get; }
        public int QuotedDistance { get; }
        public bool CanReturn { get; }

        private MovePreview(
            bool canMove,
            MovementFailureReason failureReason,
            MovementDestination? destination,
            TraitStatusSnapshot[] projectedTraitStatuses,
            CategorySnapshot projectedCategory,
            MovementPathResult pathMovement = null, int validStepCount = 0, CharacterCombatFacts projectedActor = null,
            IReadOnlyList<CombatEvent> publicEffects = null, int quotedActionCost = 0, bool canReturn = false)
        {
            if (projectedTraitStatuses == null)
                throw new ArgumentNullException(nameof(projectedTraitStatuses));
            if (validStepCount < 0 || (pathMovement != null && validStepCount > pathMovement.RequestedPath.StepCount))
                throw new ArgumentOutOfRangeException(nameof(validStepCount));

            TraitStatusSnapshot[] copiedTraitStatuses =
                new TraitStatusSnapshot[projectedTraitStatuses.Length];
            for (int i = 0; i < projectedTraitStatuses.Length; i++)
            {
                if (projectedTraitStatuses[i] == null)
                    throw new ArgumentException(
                        "Projected Trait Status 목록에는 null 항목을 넣을 수 없습니다.",
                        nameof(projectedTraitStatuses));

                copiedTraitStatuses[i] = projectedTraitStatuses[i];
            }

            CanMove = canMove;
            FailureReason = failureReason;
            Destination = destination;
            ProjectedTraitStatuses = copiedTraitStatuses;
            ProjectedCategory = projectedCategory;
            PathMovement = pathMovement;
            ValidStepCount = validStepCount;
            ProjectedActor = projectedActor;
            PublicEffects = Array.AsReadOnly((publicEffects ?? Array.Empty<CombatEvent>()).ToArray());
            QuotedActionCost = quotedActionCost; QuotedDistance = pathMovement?.RequestedPath.StepCount ?? 0; CanReturn = canReturn;
        }

        public static MovePreview Success(
            MovementDestination destination,
            TraitStatusSnapshot[] projectedTraitStatuses,
            CategorySnapshot projectedCategory)
        {
            if (projectedCategory == null)
                throw new ArgumentNullException(nameof(projectedCategory));

            return new MovePreview(
                true,
                MovementFailureReason.None,
                destination,
                projectedTraitStatuses,
                projectedCategory);
        }

        public static MovePreview Success(MovementPathResult pathMovement,
            TraitStatusSnapshot[] projectedTraitStatuses, CategorySnapshot projectedCategory,
            CharacterCombatFacts projectedActor = null, IReadOnlyList<CombatEvent> publicEffects = null,
            int quotedActionCost = 0, bool canReturn = false)
        {
            if (pathMovement == null) throw new ArgumentNullException(nameof(pathMovement));
            if (projectedCategory == null) throw new ArgumentNullException(nameof(projectedCategory));
            return new MovePreview(true, MovementFailureReason.None, null, projectedTraitStatuses,
                projectedCategory, pathMovement, pathMovement.RequestedPath.StepCount, projectedActor, publicEffects, quotedActionCost, canReturn);
        }

        public static MovePreview Failure(MovementFailureReason failureReason, int validStepCount = 0,
            MovementPathResult pathMovement = null)
        {
            if (failureReason == MovementFailureReason.None)
                throw new ArgumentException(nameof(failureReason));

            return new MovePreview(
                false,
                failureReason,
                null,
                Array.Empty<TraitStatusSnapshot>(),
                null, pathMovement, validStepCount);
        }
    }
}
