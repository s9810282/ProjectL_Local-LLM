using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>기존 요청 번호로 식별하는 미리보기 응답이다. 게임 변경 결과와 수명을 분리한다.</summary>
    public sealed class CombatPreviewResult
    {
        public int RequestNumber { get; }
        public AttackRangeResult? Range { get; }
        public AttackPreview? Attack { get; }
        public MovePreview? Move { get; }
        public MovementRangeResult? MoveRange { get; }
        public CombatPlacementPreview? Placement { get; }
        public CheckpointReturnPreview CheckpointReturn { get; }
        public MovementPreviewContext MovementContext { get; }
        private CombatPreviewResult(int number, AttackRangeResult? range = null, AttackPreview? attack = null,
            MovePreview? move = null, MovementRangeResult? moveRange = null, CombatPlacementPreview? placement = null,
            CheckpointReturnPreview checkpointReturn = null, MovementPreviewContext movementContext = null)
        {
            if (number < 1) throw new ArgumentOutOfRangeException(nameof(number));
            if ((range.HasValue ? 1 : 0) + (attack.HasValue ? 1 : 0) + (move.HasValue ? 1 : 0) +
                (moveRange.HasValue ? 1 : 0) + (placement.HasValue ? 1 : 0) + (checkpointReturn != null ? 1 : 0) != 1)
                throw new ArgumentException("미리보기 내용은 하나만 필요합니다.");
            RequestNumber = number; Range = range; Attack = attack; Move = move; MoveRange = moveRange; Placement = placement;
            CheckpointReturn = checkpointReturn;
            MovementContext = movementContext;
        }
        public static CombatPreviewResult ForAttackRange(int number, AttackRangeResult range)
        {
            if (range.AttackerId.IsEmpty || (range.Succeeded == (range.FailureReason != AttackFailureReason.None)))
                throw new ArgumentException("범위 결과가 초기화되지 않았습니다.", nameof(range));
            return new CombatPreviewResult(number, range, null);
        }
        public static CombatPreviewResult ForAttack(int number, AttackPreview attack)
        {
            if (attack.AttackerId.IsEmpty || (attack.CanAttack == (attack.FailureReason != AttackFailureReason.None)))
                throw new ArgumentException("공격 예상 결과가 초기화되지 않았습니다.", nameof(attack));
            return new CombatPreviewResult(number, null, attack);
        }
        public static CombatPreviewResult ForMove(int number, MovePreview move, MovementPreviewContext context = null) => new(number, move: move, movementContext: context);
        public static CombatPreviewResult ForMoveRange(int number, MovementRangeResult range) => new(number, moveRange: range);
        public static CombatPreviewResult ForPlacement(int number, CombatPlacementPreview placement) => new(number, placement: placement);
        public static CombatPreviewResult ForCheckpointReturn(int number, CheckpointReturnPreview preview, MovementPreviewContext context = null) =>
            new(number, checkpointReturn: preview ?? throw new ArgumentNullException(nameof(preview)), movementContext: context);
    }
}
