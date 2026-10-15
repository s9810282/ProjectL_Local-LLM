using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>체크포인트 복귀는 기록된 자원을 환급하는 별도 명령이며 통과·획득 효과를 실행하지 않는다.</summary>
    public static class CheckpointReturnCalculator
    {
        public static CheckpointReturnPreview Preview(CombatCommandInput input)
        {
            var failure = Evaluate(input, out var actor);
            return failure == MovementFailureReason.None
                ? new CheckpointReturnPreview(true, failure, Result(actor))
                : new CheckpointReturnPreview(false, failure);
        }

        internal static CombatCommandTransition Calculate(CombatCommandInput input)
        {
            var failure = Evaluate(input, out var actor);
            if (failure != MovementFailureReason.None) return CombatCommandCalculator.Reject(input, failure.ToString());
            var stats = CombatCommandEffects.With(actor.Stats,
                ap: checked(actor.Stats.CurrentActionCost + actor.PendingMoveActionCost),
                remainingMoveRange: checked(actor.Stats.MoveRange - actor.CommittedMoveDistance));
            var final = CombatMovementCalculator.CopyMovement(actor, stats, actor.MovementCheckpointAnchor,
                actor.CommittedMoveDistance, 0, 0,
                checked(actor.AttackRefundEligibleMoveCost - actor.PendingMoveActionCost), actor.MovementCheckpointAnchor);
            var relocated = final;
            final = PositionTileModifierCalculator.Replace(relocated,
                PositionTileModifierCalculator.ResolveAt(actor.MovementCheckpointAnchor, input.State.Environment));
            var events = new List<CombatEvent> { new CombatCharacterEvent(CombatEventKind.Returned, actor, relocated) };
            PositionTileModifierCalculator.RecordNormalization(events, relocated, final);
            return new CombatCommandTransition(input, true, "",
                new CharacterChanges(new[] { new CharacterStateChange(actor, final) }, Array.Empty<SoulCountChange>(),
                    PositionTileModifierCalculator.MainHealthChange(input, final)),
                new SoulFieldChange(input.Souls, input.Souls), input.State,
                new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None),
                events: events,
                returnMovement: Result(actor));
        }

        private static MovementFailureReason Evaluate(CombatCommandInput input, out CharacterCombatFacts actor)
        {
            var failure = CombatMovementCalculator.Actor(input, out actor);
            if (failure != MovementFailureReason.None) return failure;
            if (actor.Owner != Owner.Player || input.ProcessingKind != CombatProcessingKind.Command)
                return MovementFailureReason.WrongPhase;
            failure = MovementRouteValidator.ValidateLedger(actor);
            if (failure != MovementFailureReason.None) return failure;
            if (actor.PendingMoveDistance == 0) return MovementFailureReason.NoPendingMovement;
            if (!input.State.IsInside(actor.MovementCheckpointAnchor)) return MovementFailureReason.OutOfBounds;
            if (!MovementRouteValidator.CanEnter(input, actor.MovementCheckpointAnchor))
                return MovementFailureReason.DestinationOccupied;
            if ((long)actor.Stats.CurrentActionCost + actor.PendingMoveActionCost > actor.Stats.MaxActionCost)
                return MovementFailureReason.StateInconsistent;
            return MovementFailureReason.None;
        }

        private static CheckpointReturnResult Result(CharacterCombatFacts actor) =>
            new(actor.Anchor.Value, actor.MovementCheckpointAnchor, actor.PendingMoveActionCost, actor.PendingMoveDistance);
    }
}
