using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum CombatProcessingKind
    {
        Command = 1,
        Advance = 2
    }

    /// <summary>명령 또는 자동 진행 한 번의 최종 처리 사실. 미리보기 응답은 포함하지 않는다.</summary>
    public sealed class CombatResult
    {
        public int Sequence { get; }
        public CombatProcessingKind Kind { get; }
        public int CommandNumber { get; }
        public bool Accepted { get; }

        /// <summary>수락 시 빈 문자열, 거절 시 Core의 사유 코드. 표시 문구가 아니다.</summary>
        public string Reason { get; }
        public AttackResult? Attack { get; }
        public StageOutcomeSnapshot? Outcome { get; }
        public CombatCommandKind CommandKind { get; }
        public EntityId ActorId { get; }
        public MovementDestination? Movement { get; }
        public MovementPathResult PathMovement { get; }
        public CheckpointReturnResult ReturnMovement { get; }
        public CombatDrawSettlement Draw { get; }
        public IReadOnlyList<CombatEvent> Events { get; }
        public IReadOnlyList<AttackBlockedContact> BlockedContacts { get; }

        public CombatResult(int sequence, CombatProcessingKind kind, int commandNumber,
            bool accepted, string reason, AttackResult? attack = null, StageOutcomeSnapshot? outcome = null,
            CombatCommandKind commandKind = CombatCommandKind.None, EntityId actorId = default,
            MovementDestination? movement = null, CombatDrawSettlement draw = null, IReadOnlyList<CombatEvent> events = null,
            MovementPathResult pathMovement = null, CheckpointReturnResult returnMovement = null,
            IReadOnlyList<AttackBlockedContact> blockedContacts = null)
        {
            if (sequence <= 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            if (kind != CombatProcessingKind.Command && kind != CombatProcessingKind.Advance)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == CombatProcessingKind.Command ? commandNumber <= 0 : commandNumber != 0)
                throw new ArgumentOutOfRangeException(nameof(commandNumber));
            if (reason == null || (accepted ? reason.Length != 0 : string.IsNullOrWhiteSpace(reason)))
                throw new ArgumentException("수락은 빈 사유, 거절은 비어 있지 않은 사유 코드가 필요합니다.",
                    nameof(reason));

            Sequence = sequence;
            BlockedContacts = AttackBlockedContact.CopyCollection(blockedContacts);
            Kind = kind;
            CommandNumber = commandNumber;
            Accepted = accepted;
            Reason = reason;
            Attack = attack;
            Outcome = outcome;
            CommandKind = commandKind; ActorId = actorId; Movement = movement; Draw = draw;
            if ((!accepted && (pathMovement != null || returnMovement != null)) ||
                (pathMovement != null && returnMovement != null))
                throw new ArgumentException("이동 결과는 수락된 명령의 단일 이동 유형에만 넣을 수 있습니다.");
            PathMovement = pathMovement;
            ReturnMovement = returnMovement;
            Events = Array.AsReadOnly((events ?? Array.Empty<CombatEvent>()).ToArray());
            if (Events.Any(e => e == null) || (!accepted && Events.Count != 0))
                throw new ArgumentException("거절 결과 또는 null 사건을 게시할 수 없습니다.");
        }
    }
}
