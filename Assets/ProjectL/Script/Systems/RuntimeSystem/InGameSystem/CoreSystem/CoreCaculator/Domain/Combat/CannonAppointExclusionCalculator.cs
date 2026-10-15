using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Map;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>
    /// Cannon의 직접 조준 금지 영역을 계산합니다.
    /// [사용자 확정] 실제 점유 footprint를 한 칸 바깥으로 확장한 영역이 금지 영역입니다.
    /// 1x1은 3x3, 2x2는 2x2 + 한 칸 테두리가 되며 비정형 footprint도 실제 모양을 따릅니다.
    /// 이 검사는 직접 조준 단계에만 적용하고 적법한 공격의 간접 피해에는 재적용하지 않습니다.
    /// </summary>
    public static class CannonAppointExclusionCalculator
    {
        /// <summary>
        /// 선택한 Cell이 금지 영역 안인지 판정합니다.
        /// </summary>
        public static bool IsForbidden(IReadOnlyList<CellCoord> occupiedCells, CellCoord selectedCell)
        {
            if (occupiedCells == null) throw new ArgumentNullException(nameof(occupiedCells));
            if (occupiedCells.Count == 0)
                throw new ArgumentException("점유 셀이 하나 이상 필요합니다.", nameof(occupiedCells));

            for (int i = 0; i < occupiedCells.Count; i++)
            {
                CellCoord occupied = occupiedCells[i];
                long deltaX = (long)selectedCell.X - occupied.X;
                long deltaY = (long)selectedCell.Y - occupied.Y;

                // 점유 셀 자신과 그 주변 한 칸이 금지된다.
                if (Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)) <= 1)
                    return true;
            }

            return false;
        }
    }
}
