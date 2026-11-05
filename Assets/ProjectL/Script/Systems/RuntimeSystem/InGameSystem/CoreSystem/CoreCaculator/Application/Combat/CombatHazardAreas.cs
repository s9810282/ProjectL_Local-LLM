using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 환경 위험 원본의 피해 칸 모양이다. 지뢰 진입 피해(<see cref="MineMovementTileRule"/>)와 화약통 폭발(<see cref="ObstacleReactionCalculator"/>)이
    /// 이 모양을 쓰고, View 호버도 규칙 사본 없이 같은 칸을 그리려고 이 입구를 부른다. 상태가 없는 순수 계산이다.
    /// </summary>
    /// <remarks>
    /// 호출자 목록을 비우지 않고 뒤에 덧붙인다(한 칸에 원본이 여럿이면 합집합). 보드 밖 칸은 넣지 않는다.
    /// </remarks>
    public static class CombatHazardAreas
    {
        /// <summary>지뢰의 피해 칸: 지뢰 칸을 포함한 3×3이다.</summary>
        public static void MineCells(CellCoord origin, int width, int height, ICollection<CellCoord> into)
        {
            Validate(origin, width, height, into);
            AddSquare(origin, width, height, includeCenter: true, into);
        }

        /// <summary>화약통의 폭발 칸: 기준 칸을 뺀 둘레 8칸이다.</summary>
        public static void PowderBarrelCells(CellCoord origin, int width, int height, ICollection<CellCoord> into)
        {
            Validate(origin, width, height, into);
            AddSquare(origin, width, height, includeCenter: false, into);
        }

        private static void Validate(CellCoord origin, int width, int height, ICollection<CellCoord> into)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
            if (origin.X < 0 || origin.Y < 0 || origin.X >= width || origin.Y >= height)
                throw new ArgumentOutOfRangeException(nameof(origin));
            if (into == null) throw new ArgumentNullException(nameof(into));
        }

        private static void AddSquare(CellCoord origin, int width, int height, bool includeCenter, ICollection<CellCoord> into)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (!includeCenter && dx == 0 && dy == 0) continue;
                    long x = (long)origin.X + dx;
                    long y = (long)origin.Y + dy;
                    if (x >= 0 && y >= 0 && x < width && y < height)
                        into.Add(new CellCoord((int)x, (int)y));
                }
        }
    }
}
