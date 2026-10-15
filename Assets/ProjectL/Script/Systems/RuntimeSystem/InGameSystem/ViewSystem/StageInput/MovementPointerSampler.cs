using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput
{
    internal static class MovementPointerSampler
    {
        // 셀 (x,y)는 [x,x+1) × [y,y+1)이다. 화면/월드 좌표 변환은 씬의 책임이다.
        internal static void AppendCrossedCells(Vector2 fromGrid, Vector2 toGrid, List<CellCoord> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            var cell = CellAt(fromGrid);
            var end = CellAt(toGrid);
            double dx = (double)toGrid.x - fromGrid.x, dy = (double)toGrid.y - fromGrid.y;
            int sx = Math.Sign(dx), sy = Math.Sign(dy);
            double tx = sx == 0 ? double.PositiveInfinity : ((sx > 0 ? cell.X + 1d : cell.X) - fromGrid.x) / dx;
            double ty = sy == 0 ? double.PositiveInfinity : ((sy > 0 ? cell.Y + 1d : cell.Y) - fromGrid.y) / dy;
            double stepX = sx == 0 ? double.PositiveInfinity : 1d / Math.Abs(dx);
            double stepY = sy == 0 ? double.PositiveInfinity : 1d / Math.Abs(dy);
            while (cell != end)
            {
                double nextX = cell.X == end.X ? double.PositiveInfinity : tx;
                double nextY = cell.Y == end.Y ? double.PositiveInfinity : ty;
                // 정확한 꼭짓점에서는 X를 먼저 기록하고 다음 반복에서 Y를 기록한다.
                if (nextX <= nextY) { cell = new CellCoord(cell.X + sx, cell.Y); tx += stepX; }
                else { cell = new CellCoord(cell.X, cell.Y + sy); ty += stepY; }
                output.Add(cell);
            }
        }

        internal static CellCoord CellAt(Vector2 grid)
        {
            if (float.IsNaN(grid.x) || float.IsInfinity(grid.x) || float.IsNaN(grid.y) || float.IsInfinity(grid.y) ||
                grid.x < int.MinValue || (double)grid.x >= int.MaxValue || grid.y < int.MinValue || (double)grid.y >= int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(grid));
            return new CellCoord((int)Math.Floor(grid.x), (int)Math.Floor(grid.y));
        }

        internal static bool IsInside(Vector2 grid, float inset)
        {
            var cell = CellAt(grid);
            double x = (double)grid.x - cell.X, y = (double)grid.y - cell.Y;
            return x >= inset && x <= 1d - inset && y >= inset && y <= 1d - inset;
        }
    }
}
