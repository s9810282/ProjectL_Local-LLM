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
        public CombatActionAvailability ActionAvailability { get; }
        public MovementPreviewContext MovementContext { get; }
        /// <summary>필드 유닛 하나의 공격 범위 칸 조회 응답이다(호버 공격 범위 설계 UR2).</summary>
        public UnitAttackRangeResult? UnitRange { get; }
        private CombatPreviewResult(int number, AttackRangeResult? range = null, AttackPreview? attack = null,
            MovePreview? move = null, MovementRangeResult? moveRange = null, CombatPlacementPreview? placement = null,
            CombatActionAvailability actionAvailability = null, MovementPreviewContext movementContext = null,
            UnitAttackRangeResult? unitRange = null)
        {
            if (number < 1) throw new ArgumentOutOfRangeException(nameof(number));
            if ((range.HasValue ? 1 : 0) + (attack.HasValue ? 1 : 0) + (move.HasValue ? 1 : 0) +
                (moveRange.HasValue ? 1 : 0) + (placement.HasValue ? 1 : 0) + (actionAvailability != null ? 1 : 0) +
                (unitRange.HasValue ? 1 : 0) != 1)
                throw new ArgumentException("미리보기 내용은 하나만 필요합니다.");
            RequestNumber = number; Range = range; Attack = attack; Move = move; MoveRange = moveRange; Placement = placement;
            ActionAvailability = actionAvailability;
            MovementContext = movementContext;
            UnitRange = unitRange;
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
        public static CombatPreviewResult ForActionAvailability(int number, CombatActionAvailability availability) =>
            new(number, actionAvailability: availability ?? throw new ArgumentNullException(nameof(availability)));
        public static CombatPreviewResult ForUnitAttackRange(int number, UnitAttackRangeResult range)
        {
            if (range.UnitId.IsEmpty || (range.Succeeded == (range.FailureReason != AttackFailureReason.None)))
                throw new ArgumentException("유닛 공격 범위 결과가 초기화되지 않았습니다.", nameof(range));
            return new CombatPreviewResult(number, unitRange: range);
        }
    }
}
