using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    internal readonly struct ForcedMovementRequest
    {
        internal EntityId TargetId { get; }
        internal CellCoord Step { get; }
        internal int Distance { get; }
        internal CombatMovementCause Cause { get; }
        internal bool DeferLastObjects { get; }
        internal bool PublishMovedEvents { get; }

        internal ForcedMovementRequest(EntityId targetId, CellCoord step, int distance, CombatMovementCause cause,
            bool deferLastObjects, bool publishMovedEvents)
        {
            if (targetId.IsEmpty) throw new ArgumentOutOfRangeException(nameof(targetId));
            if (step == CellCoord.Zero || Math.Abs(step.X) > 1 || Math.Abs(step.Y) > 1)
                throw new ArgumentOutOfRangeException(nameof(step));
            if (distance < 1) throw new ArgumentOutOfRangeException(nameof(distance));
            TargetId = targetId;
            Step = step;
            Distance = distance;
            Cause = cause ?? throw new ArgumentNullException(nameof(cause));
            DeferLastObjects = deferLastObjects;
            PublishMovedEvents = publishMovedEvents;
        }
    }

    internal readonly struct ForcedMovementResult
    {
        internal ForcedDisplacementPath Path { get; }
        internal int NormalizationHealthLoss { get; }

        internal ForcedMovementResult(ForcedDisplacementPath path, int normalizationHealthLoss)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            NormalizationHealthLoss = normalizationHealthLoss;
        }
    }
}
