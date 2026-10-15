using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>드래그 원점부터 선택한 칸까지 복사해 보관한다. 경로의 적법성은 Core가 판단한다.</summary>
    public sealed class MovementPath
    {
        public IReadOnlyList<CellCoord> Cells { get; }
        public CellCoord Origin => Cells[0];
        public CellCoord Endpoint => Cells[Cells.Count - 1];
        public int StepCount => Cells.Count - 1;

        public MovementPath(IReadOnlyList<CellCoord> cells)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (cells.Count == 0) throw new ArgumentException("경로에는 원점이 필요합니다.", nameof(cells));
            Cells = Array.AsReadOnly(cells.ToArray());
        }
    }
}
