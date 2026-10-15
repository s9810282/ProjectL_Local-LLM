 using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles
{
    /// <summary>
    /// 타일의 footprint를 나타내는 클래스입니다. footprint는 타일이 차지하는 셀들의 상대 좌표를 나타냅니다.
    /// </summary>
    public sealed class TileFootprint
    {
        public int Width { get; } // 타일 영역의 기준 너비
        public int Height { get; } // 타일 영역의 기준 높이
         
        private readonly CellCoord[] _localCells;
        public int CellCount => _localCells.Length;

        /// <summary>
        /// 타일의 기준 크기와 해당 타일이 차지하는 로컬 셀 배열을 사용하여 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="width">타일 기준 영역 너비</param>
        /// <param name="height">타일 기준 영역 높이</param>
        /// <param name="localCells">타일이 차지하는 셀들의 로컬 좌표 배열</param>
        /// <exception cref="ArgumentOutOfRangeException">너비/높이가 유효하지 않거나, 로컬 셀 좌표가 범위를 벗어날 때 발생합니다.</exception>
        /// <exception cref="ArgumentException">로컬 셀 배열이 비어있거나, 중복된 좌표가 포함되어 있을 때 발생합니다.</exception>
        public TileFootprint(int width, int height, CellCoord[] localCells)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "width 혹은 height는 1 이상이어야 합니다.");

            Width = width;
            Height = height;

            if (localCells == null || localCells.Length == 0)
                throw new ArgumentException("localCells는 비어 있을 수 없습니다.", nameof(localCells));

            _localCells = new CellCoord[localCells.Length];

            for (int i = 0; i < localCells.Length; i++)
            {
                CellCoord cell = localCells[i];

                if (cell.X < 0 || cell.Y < 0)
                    throw new ArgumentOutOfRangeException(nameof(localCells), "cell 좌표는 0 이상이어야 합니다.");

                if (cell.X >= Width || cell.Y >= Height)
                    throw new ArgumentOutOfRangeException(nameof(localCells),"cell이 로컬 셀 크기보다 큽니다.");

                for (int j = 0; j < i; j++)
                {
                    if (_localCells[j].Equals(cell) == true)
                        throw new ArgumentException("중복된 셀이 로컬에 들어오려 합니다.");
                }

                _localCells[i] = cell;
            }
        }

        /// <summary>
        /// 지정된 인덱스에 해당하는 로컬 셀 좌표를 반환합니다.
        /// </summary>
        /// <param name="index">가져올 셀의 인덱스</param>
        /// <returns>로컬 셀 좌표</returns>
        /// <exception cref="ArgumentOutOfRangeException">인덱스가 범위를 벗어났을 때 발생합니다.</exception>
        public CellCoord GetLocalCell(int index)
        {
            if (index >= CellCount || index < 0)
                throw new ArgumentOutOfRangeException("셀의 최대 index를 초과하였습니다.");

            return _localCells[index];
        }

        /// <summary>
        /// 지정된 인덱스의 로컬 셀 좌표를 주어진 회전 방향에 맞게 변환하여 반환합니다.
        /// </summary>
        /// <param name="index">가져올 셀의 인덱스</param>
        /// <param name="rotation">적용할 회전 방향</param>
        /// <returns>회전이 적용된 로컬 셀 좌표</returns>
        /// <exception cref="ArgumentOutOfRangeException">정의되지 않은 회전값일 때 발생합니다.</exception>
        public CellCoord GetRotatedLocalCell(int index, Rotation4 rotation)
        {
            CellCoord cell = GetLocalCell(index);
            int x = 0;
            int y = 0;

            switch (rotation)
            {
                case Rotation4.R0:
                    return cell;
                case Rotation4.R90:
                    x = cell.Y;
                    y = Width - 1 - cell.X;
                    return new CellCoord(x, y);
                case Rotation4.R180:
                    x = Width - 1 - cell.X;
                    y = Height - 1 - cell.Y;
                    return new CellCoord(x, y);
                case Rotation4.R270:
                    x = Height - 1 - cell.Y;
                    y = cell.X;
                    return new CellCoord(x, y);
                default:
                    throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null);
            }
        }

        /// <summary>
        /// 기준 앵커(Anchor) 좌표와 회전 방향을 적용하여 실제 맵 상에서 차지하게 될 절대 좌표를 계산합니다.
        /// </summary>
        /// <param name="index">가져올 셀의 인덱스</param>
        /// <param name="anchor">타일이 배치될 맵 상의 기준 좌표 (좌하단 등)</param>
        /// <param name="rotation">적용할 회전 방향</param>
        /// <returns>실제 맵 상의 셀 좌표</returns>
        public CellCoord GetOccupiedCell(int index, CellCoord anchor, Rotation4 rotation)
        {
            CellCoord rotated = GetRotatedLocalCell(index, rotation);
            return anchor + rotated;
        }
        

        public static TileFootprint CreateFromCells(CellCoord[] cellCoords)
        {
            if (cellCoords == null || cellCoords.Length == 0)
                throw new ArgumentException(
                    "cellCoords는 비어 있을 수 없습니다.",
                    nameof(cellCoords));

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            // Bounding Box 계산
            for (int i = 0; i < cellCoords.Length; i++)
            {
                CellCoord cell = cellCoords[i];

                if (cell.X < 0 || cell.Y < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(cellCoords),
                        "cell 좌표는 0 이상이어야 합니다.");
                }

                if (cell.X < minX)
                    minX = cell.X;

                if (cell.Y < minY)
                    minY = cell.Y;

                if (cell.X > maxX)
                    maxX = cell.X;

                if (cell.Y > maxY)
                    maxY = cell.Y;
            }

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            // Bounding Box 기준으로 로컬 좌표 정규화
            CellCoord[] localCells =
                new CellCoord[cellCoords.Length];

            for (int i = 0; i < cellCoords.Length; i++)
            {
                CellCoord cell = cellCoords[i];

                localCells[i] = new CellCoord(
                    cell.X - minX,
                    cell.Y - minY);
            }

            return new TileFootprint(
                width,
                height,
                localCells);
        }
    }
}
