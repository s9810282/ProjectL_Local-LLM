using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist
{
    /// <summary>호버 칸을 포함하는 토네이도·하붑 그룹의 공개된 현재 칸을 모읍니다. 모양이나 다음 위치를 계산하지 않습니다.</summary>
    internal static class CombatGimmickHoverCells
    {
        /// <summary>목록을 비우고 해당 그룹들의 합집합을 채웁니다. 떨어진 칸도 같은 그룹이면 포함합니다.</summary>
        internal static void Collect(IReadOnlyList<TornadoAreaFact> tornadoes,
            IReadOnlyList<StageGimmickState> gimmicks, CellCoord cell, List<CellCoord> into)
        {
            if (tornadoes == null) throw new ArgumentNullException(nameof(tornadoes));
            if (gimmicks == null) throw new ArgumentNullException(nameof(gimmicks));
            if (into == null) throw new ArgumentNullException(nameof(into));

            into.Clear();
            for (int i = 0; i < tornadoes.Count; i++)
            {
                IReadOnlyList<CellCoord> cells = tornadoes[i].Cells;
                if (Contains(cells, cell)) AppendDistinct(cells, into);
            }
            for (int i = 0; i < gimmicks.Count; i++)
            {
                if (gimmicks[i] is HaboobGimmickState haboob && Contains(haboob.Cells, cell))
                    AppendDistinct(haboob.Cells, into);
            }
        }

        private static bool Contains(IReadOnlyList<CellCoord> cells, CellCoord cell)
        {
            for (int i = 0; i < cells.Count; i++)
                if (cells[i] == cell) return true;
            return false;
        }

        private static void AppendDistinct(IReadOnlyList<CellCoord> cells, List<CellCoord> into)
        {
            for (int i = 0; i < cells.Count; i++)
                if (!into.Contains(cells[i])) into.Add(cells[i]);
        }
    }
}
