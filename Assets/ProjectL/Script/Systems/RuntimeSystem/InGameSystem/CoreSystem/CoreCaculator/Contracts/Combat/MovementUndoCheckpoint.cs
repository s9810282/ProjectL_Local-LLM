using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public sealed class MovementUndoCheckpoint
    {
        public MovementEffectId Movement { get; }
        public IReadOnlyList<MovementUndoParticipant> Participants { get; }
        public IReadOnlyList<MovementEffectReceipt> Effects { get; }
        public long BattleInstanceId => Movement.BattleInstanceId;
        public long ControllerTurnSequence => Movement.ControllerTurnSequence;
        public int MoveCommandNumber => Movement.MoveCommandNumber;
        public EntityId ActorId => Movement.ActorId;
        public CellCoord Anchor => Participants[0].Restoration.To;
        public Rotation4 Rotation => Participants[0].Restoration.Rotation;
        public IReadOnlyList<CellCoord> OccupiedCells { get; }
        public int RecordedDistance => Participants[0].Restoration.DistanceReverted;
        public MovementUndoOriginFacts OriginFacts => Participants[0].OriginFacts;
        public CharacterCombatFacts ExpectedActorAfterMove => Participants[0].ExpectedAfterMove;

        public MovementUndoCheckpoint(long battleInstanceId, long controllerTurnSequence, int moveCommandNumber,
            EntityId actorId, CellCoord anchor, Rotation4 rotation, IReadOnlyList<CellCoord> occupiedCells,
            int recordedDistance, MovementUndoOriginFacts originFacts, CharacterCombatFacts expectedActorAfterMove)
            : this(new MovementEffectId(battleInstanceId, controllerTurnSequence, actorId, moveCommandNumber),
                new[] { new MovementUndoParticipant(expectedActorAfterMove, originFacts, rotation, true, recordedDistance) },
                Array.Empty<MovementEffectReceipt>())
        {
            if (occupiedCells == null || occupiedCells.Count != 1 || occupiedCells[0] != anchor || Anchor != anchor)
                throw new ArgumentException("원래 배치와 원점 의존 사실이 일치해야 합니다.");
        }

        public MovementUndoCheckpoint(MovementEffectId movement, IReadOnlyList<MovementUndoParticipant> participants,
            IReadOnlyList<MovementEffectReceipt> effects)
        {
            if (!movement.IsValid) throw new ArgumentException("유효한 이동 출처가 필요합니다.", nameof(movement));
            var people = (participants ?? throw new ArgumentNullException(nameof(participants))).ToArray();
            var receipts = (effects ?? throw new ArgumentNullException(nameof(effects))).ToArray();
            if (people.Length < 1 || people.Length > 2 || people.Any(p => p == null) ||
                people.Select(p => p.EntityId).Distinct().Count() != people.Length ||
                people.Count(p => p.EntityId == movement.ActorId) != 1 ||
                people.Any(p => p.Restoration.RestoresActionUse != (p.EntityId == movement.ActorId)) ||
                people.Select(p => p.Restoration.To).Distinct().Count() != people.Length ||
                people.Select(p => p.Restoration.From).Distinct().Count() != people.Length)
                throw new ArgumentException("참여자는 중복 없이 행위자를 포함하고 행위자만 행동·거리를 복원해야 합니다.", nameof(participants));
            if (receipts.Any(r => r == null || r.Movement != movement) ||
                receipts.Where((r, i) => receipts.Take(i).Any(r.SameContribution)).Any() ||
                receipts.OfType<MovementTrailReceipt>().GroupBy(r => r.CreatedSource.Id).Any(g => g.Count() > 1))
                throw new ArgumentException("역산 영수증은 같은 이동에 속하며 기여분과 환경 출처가 중복되지 않아야 합니다.", nameof(effects));
            Movement = movement;
            Participants = Array.AsReadOnly(people.OrderBy(p => p.EntityId == movement.ActorId ? 0 : 1).ToArray());
            Effects = Array.AsReadOnly(receipts);
            OccupiedCells = Array.AsReadOnly(new[] { Anchor });
        }

        public bool SameValues(MovementUndoCheckpoint other) => other != null && Movement == other.Movement &&
            Participants.Count == other.Participants.Count && Participants.Zip(other.Participants, (a, b) => a.SameValues(b)).All(same => same) &&
            Effects.Count == other.Effects.Count && Effects.Zip(other.Effects, (a, b) => a.SameValues(b)).All(same => same);
    }
}
