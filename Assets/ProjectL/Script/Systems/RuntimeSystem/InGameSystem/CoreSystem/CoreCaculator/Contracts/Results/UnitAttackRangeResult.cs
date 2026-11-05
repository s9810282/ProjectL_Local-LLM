using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>
    /// 필드 유닛 하나의 공격 범위 칸과 이동 칸 조회 결과다. 차례·국면과 무관한 조회라 조준 목록·AP 판정 같은 행동 답은 담지 않는다
    /// (행동 질문의 답은 <see cref="AttackRangeResult"/>·<see cref="MovementRangeResult"/>).
    /// 성공·실패는 공격 범위 기준이고, 이동 칸은 공격 가능 여부와 따로 계산해 실패 결과에도 담는다(겹친 강조·호버 이동 범위 설계 EM1·P8).
    /// </summary>
    public readonly struct UnitAttackRangeResult
    {
        public bool Succeeded { get; }
        public AttackFailureReason FailureReason { get; }
        public EntityId UnitId { get; }
        public ReadOnlyMemory<CellCoord> RangeCells { get; }
        /// <summary>그 유닛이 지금 자리에서 스스로 걸어 닿을 수 있는 칸(자기 칸 제외, Y·X 순서)이다. 스스로 움직일 수 없으면 비어 있다.</summary>
        public ReadOnlyMemory<CellCoord> MoveCells { get; }

        private UnitAttackRangeResult(bool succeeded, AttackFailureReason failureReason, EntityId unitId, CellCoord[] rangeCells,
            CellCoord[] moveCells)
        {
            if (rangeCells == null)
                throw new ArgumentNullException(nameof(rangeCells));

            Succeeded = succeeded;
            FailureReason = failureReason;
            UnitId = unitId;
            RangeCells = Copy(rangeCells);
            MoveCells = Copy(moveCells ?? Array.Empty<CellCoord>());
        }

        public static UnitAttackRangeResult Success(EntityId unitId, CellCoord[] rangeCells, CellCoord[] moveCells = null)
        {
            return new UnitAttackRangeResult(true, AttackFailureReason.None, unitId, rangeCells, moveCells);
        }

        public static UnitAttackRangeResult Failure(AttackFailureReason failureReason, EntityId unitId, CellCoord[] moveCells = null)
        {
            if (failureReason == AttackFailureReason.None)
                throw new ArgumentException("실패 결과에는 실패 이유가 필요합니다.", nameof(failureReason));

            return new UnitAttackRangeResult(false, failureReason, unitId, Array.Empty<CellCoord>(), moveCells);
        }

        private static CellCoord[] Copy(CellCoord[] cells)
        {
            CellCoord[] copied = new CellCoord[cells.Length];
            Array.Copy(cells, copied, cells.Length);
            return copied;
        }
    }
}
