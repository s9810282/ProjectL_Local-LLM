using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist
{
    /// <summary>
    /// 클릭한 예고(Warning) 토네이도의 다음 도착 영역을 모읍니다. 도착 칸은 Core가 실제 이동과 같은 경로 함수로 공개한
    /// <see cref="TornadoAreaFact.DestinationCells"/>를 그대로 씁니다(평가 C3-01). 방향 선택·난수·반사·이동 규칙은 만들지 않습니다.
    /// </summary>
    internal static class CombatTornadoDestinationCells
    {
        internal static void Collect(CombatStateData state, CellCoord clickedCell, List<CellCoord> into)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (into == null) throw new ArgumentNullException(nameof(into));
            into.Clear();
            foreach (TornadoAreaFact area in state.ReadTornadoAreas(Owner.Player))
            {
                if (!area.IsActive || area.Phase != TornadoGimmickPhase.Warning || !Contains(area.Cells, clickedCell))
                    continue;
                // 예고 중에는 Core가 확정 방향으로 도착 칸을 채웁니다. 방향 없음(정지)이면 지금 칸입니다.
                IReadOnlyList<CellCoord> destination = area.DestinationCells;
                for (int i = 0; i < destination.Count; i++)
                    if (!into.Contains(destination[i])) into.Add(destination[i]);
            }
        }

        private static bool Contains(IReadOnlyList<CellCoord> cells, CellCoord cell)
        {
            for (int i = 0; i < cells.Count; i++)
                if (cells[i] == cell) return true;
            return false;
        }
    }
}
