using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 칸마다 화면에 보이는 지형 종류 표입니다. 이웃 모양을 고를 때만 씁니다(지형 이웃 모양 설계 TC2).
    /// 채우는 쪽은 지형 표시 하나(<see cref="FieldTerrainPresenter"/>의 그리기 맨 앞)이고, 이웃 모양을 쓰는 종류만 표시합니다.
    /// 한 칸에 여러 종류가 같이 있을 수 있어 종류마다 비트 하나를 씁니다. 배열은 생성 때 한 번 만들고 다시 채울 때 재사용합니다.
    /// </summary>
    internal sealed class FieldTerrainNeighbors
    {
        private readonly uint[] _kinds;

        internal FieldTerrainNeighbors(int columnCount, int rowCount)
        {
            if (columnCount <= 0) throw new ArgumentOutOfRangeException(nameof(columnCount));
            if (rowCount <= 0) throw new ArgumentOutOfRangeException(nameof(rowCount));
            ColumnCount = columnCount;
            RowCount = rowCount;
            _kinds = new uint[checked(columnCount * rowCount)];
        }

        internal int ColumnCount { get; }
        internal int RowCount { get; }

        /// <summary>모든 칸을 빈칸으로 되돌립니다.</summary>
        internal void Clear() => Array.Clear(_kinds, 0, _kinds.Length);

        /// <summary>칸에 종류를 표시합니다. 격자 밖 칸은 계약 오류입니다.</summary>
        internal void Mark(CellCoord cell, TerrainContentKind kind)
        {
            if (!IsInside(cell))
                throw new ArgumentOutOfRangeException(nameof(cell), $"격자 범위를 벗어난 칸입니다: {cell}");
            _kinds[IndexOf(cell)] |= BitOf(kind);
        }

        /// <summary>칸에 그 종류가 보이는지입니다. 격자 밖 칸은 빈칸입니다.</summary>
        internal bool Has(CellCoord cell, TerrainContentKind kind) =>
            IsInside(cell) && (_kinds[IndexOf(cell)] & BitOf(kind)) != 0;

        private bool IsInside(CellCoord cell) =>
            cell.X >= 0 && cell.X < ColumnCount && cell.Y >= 0 && cell.Y < RowCount;

        private int IndexOf(CellCoord cell) => (cell.Y * ColumnCount) + cell.X;

        private static uint BitOf(TerrainContentKind kind)
        {
            int value = (int)kind;
            if (value < 0 || value >= 32)
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "지형 종류 값은 0~31이어야 합니다.");
            return 1u << value;
        }
    }
}
