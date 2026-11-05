using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public sealed class CombatActionAvailability
    {
        public long SourceRevision { get; }
        public MovementUndoAvailability MovementUndo { get; }
        public CombatActionAvailability WithSourceRevision(long revision) => new CombatActionAvailability(
            BattleInstanceId, ControllerTurnSequence, ActorId, ActionUse, MovementAllowance, MoveFailure, AttackFailure, MovementUndo, revision);
        public long BattleInstanceId { get; }
        public long ControllerTurnSequence { get; }
        public EntityId ActorId { get; }
        public UnitActionUse ActionUse { get; }
        public int? MovementAllowance { get; }
        public MovementFailureReason MoveFailure { get; }
        public AttackFailureReason AttackFailure { get; }
        public bool CanMove => MoveFailure == MovementFailureReason.None;
        public bool CanAttack => AttackFailure == AttackFailureReason.None;

        public CombatActionAvailability(long battleInstanceId,long controllerTurnSequence,EntityId actorId,            UnitActionUse actionUse,int? movementAllowance,MovementFailureReason moveFailure,AttackFailureReason attackFailure, MovementUndoAvailability movementUndo = null, long sourceRevision = 0)
        {
            if (sourceRevision < 0) throw new ArgumentOutOfRangeException(nameof(sourceRevision));
            SourceRevision = sourceRevision;
            MovementUndo = movementUndo ?? MovementUndoAvailability.Unavailable;
            if (MovementUndo.CanUndo && (battleInstanceId < 1 || controllerTurnSequence < 1 || actorId.IsEmpty))
                throw new ArgumentException("취소 가능 응답에는 유효한 전투·턴·개체 식별자가 필요합니다.");
            if (battleInstanceId < 0 || controllerTurnSequence < 0 || movementAllowance < 0 ||
                !Enum.IsDefined(typeof(UnitActionUse), actionUse) || !Enum.IsDefined(typeof(MovementFailureReason), moveFailure) ||
                !Enum.IsDefined(typeof(AttackFailureReason), attackFailure)) throw new ArgumentException("행동 가능성 값이 잘못되었습니다.");
            BattleInstanceId = battleInstanceId; ControllerTurnSequence = controllerTurnSequence; ActorId = actorId;
            ActionUse = actionUse; MovementAllowance = movementAllowance;
            MoveFailure = moveFailure; AttackFailure = attackFailure;
        }
    }
}
