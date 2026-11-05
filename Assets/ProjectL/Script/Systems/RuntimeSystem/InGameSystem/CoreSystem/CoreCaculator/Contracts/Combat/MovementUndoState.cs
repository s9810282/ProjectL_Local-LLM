using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public sealed class MovementUndoState
    {
        public static MovementUndoState Empty { get; } = new MovementUndoState(Array.Empty<MovementUndoCheckpoint>());
        public IReadOnlyList<MovementUndoCheckpoint> Checkpoints { get; }
        public MovementUndoState(IReadOnlyList<MovementUndoCheckpoint> checkpoints)
        {
            if (checkpoints == null) throw new ArgumentNullException(nameof(checkpoints));
            var copy = checkpoints.ToArray();
            if (copy.Any(c => c == null) || copy.Select(c => c.ActorId).Distinct().Count() != copy.Length)
                throw new ArgumentException("이동 취소 기록은 null이나 중복 개체를 포함할 수 없습니다.", nameof(checkpoints));
            Checkpoints = Array.AsReadOnly(copy.OrderBy(c => c.ActorId).ToArray());
        }

        public bool TryGet(EntityId actorId, out MovementUndoCheckpoint checkpoint)
        {
            checkpoint = Checkpoints.SingleOrDefault(c => c.ActorId == actorId);
            return checkpoint != null;
        }

        public bool SameValues(MovementUndoState other) => other != null && Checkpoints.Count == other.Checkpoints.Count &&
            Checkpoints.Zip(other.Checkpoints, (a, b) => a.SameValues(b)).All(same => same);

        public MovementUndoState WithoutAffected(CombatChangeEvidence evidence)
        {
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));
            if (evidence.Kind == CombatChangeEvidenceKind.Lifecycle) return Empty;
            var retained = Checkpoints.Where(c => !evidence.UsedMovements.Contains(c.Movement) && !c.Participants.Any(p => evidence.AffectedEntityIds.Contains(p.EntityId) ||
                evidence.ChangedSourceCells.Contains(p.Restoration.To)) && !c.Effects.Any(e =>
                    e is MovementTrailReceipt trail && evidence.ChangedSourceIds.Contains(trail.CreatedSource.Id) ||
                    evidence.CharacterChanges.Any(change => change.Final.EntityId == e.RecipientId && !e.MatchesGuard(change.Final)))).ToArray();
            return retained.Length == Checkpoints.Count ? this : retained.Length == 0 ? Empty : new MovementUndoState(retained);
        }
    }
}
