using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement
{
    /// <summary>
    /// 확정 기믹 점유를 원본 상태(<see cref="StageGimmickState.BlockingCells"/>)에서 읽는다. 공격 차폐나 별도 가변 맵을 만들지 않는다.
    /// 해석 중 임시 예약(사슬 확장 칸 등)은 부르는 쪽이 넘기며, 확정 점유와 같은 판정으로 막는다.
    /// </summary>
    public static class GimmickOccupancyRules
    {
        /// <summary>모든 기믹의 확정 점유 칸이다. 기믹 식별자 순서·중복 없음이며, 점유가 없으면 빈 목록이다.</summary>
        public static IReadOnlyCollection<CellCoord> BlockingCells(StageGimmickStateSet gimmicks)
        {
            if (gimmicks == null) throw new ArgumentNullException(nameof(gimmicks));
            List<CellCoord> cells = null;
            var entries = gimmicks.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var blocking = entries[i].BlockingCells;
                for (int j = 0; j < blocking.Count; j++)
                {
                    cells ??= new List<CellCoord>();
                    if (!cells.Contains(blocking[j])) cells.Add(blocking[j]);
                }
            }
            return cells == null ? Array.Empty<CellCoord>() : cells.AsReadOnly();
        }

        public static bool Blocks(StageGimmickStateSet gimmicks, CellCoord cell) => Blocks(gimmicks, cell, null);

        /// <summary>확정 점유 또는 <paramref name="transientReservations"/>가 그 칸을 막는지 본다. 기믹 점유 판정의 유일한 원본이다.</summary>
        public static bool Blocks(StageGimmickStateSet gimmicks, CellCoord cell, IReadOnlyCollection<CellCoord> transientReservations)
        {
            if (gimmicks == null) throw new ArgumentNullException(nameof(gimmicks));
            if (transientReservations != null && transientReservations.Count != 0 && transientReservations.Contains(cell)) return true;
            var entries = gimmicks.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var blocking = entries[i].BlockingCells;
                for (int j = 0; j < blocking.Count; j++)
                    if (blocking[j] == cell) return true;
            }
            return false;
        }

        /// <summary>모양의 칸 하나라도 <see cref="Blocks(StageGimmickStateSet, CellCoord, IReadOnlyCollection{CellCoord})"/>에 걸리는지 본다.</summary>
        public static bool Overlaps(StageGimmickStateSet gimmicks, IReadOnlyList<CellCoord> occupiedCells,
            IReadOnlyCollection<CellCoord> transientReservations)
        {
            if (gimmicks == null) throw new ArgumentNullException(nameof(gimmicks));
            if (occupiedCells == null) throw new ArgumentNullException(nameof(occupiedCells));
            for (int i = 0; i < occupiedCells.Count; i++)
                if (Blocks(gimmicks, occupiedCells[i], transientReservations)) return true;
            return false;
        }
    }
}
