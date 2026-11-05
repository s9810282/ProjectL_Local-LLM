using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>
    /// 아군 배치 구역이다. 배치 국면에 아군·메인 캐릭터는 이 칸에만 놓이고, 적은 이 칸 밖에만 놓인다.
    /// 적 배치 구역은 따로 없다. 판정 규칙은 <c>DeploymentZonePolicy</c>가 이 값만 읽어 정한다.
    /// </summary>
    /// <remarks>
    /// 모양은 둘이다. 줄 구역은 "아래부터 N줄"(y &lt; N)이라 판 너비 없이 판정할 수 있다.
    /// 칸 구역은 저작한 칸 목록이다. 판 안인지는 판 크기를 아는 생성자가 <see cref="FitsBoard"/>로 검사한다.
    /// </remarks>
    public sealed class PlayerDeploymentZone
    {
        private readonly HashSet<CellCoord> _cells;

        /// <summary>줄 구역이면 줄 수, 칸 구역이면 null이다. 줄 수를 받는 옛 계약 속성(전환 입구)만 읽는다.</summary>
        public int? RowDepth { get; }

        /// <summary>칸 구역의 칸이다. y → x 순서이며, 줄 구역이면 비어 있다.</summary>
        public IReadOnlyList<CellCoord> ListedCells { get; }

        public bool IsEmpty => RowDepth.HasValue ? RowDepth.Value == 0 : ListedCells.Count == 0;

        public static PlayerDeploymentZone Empty { get; } = new PlayerDeploymentZone(0, Array.Empty<CellCoord>());

        private PlayerDeploymentZone(int? rowDepth, CellCoord[] cells)
        {
            RowDepth = rowDepth;
            ListedCells = Array.AsReadOnly(cells);
            _cells = new HashSet<CellCoord>(cells);
        }

        /// <summary>아래(y = 0)부터 <paramref name="depth"/>줄을 구역으로 만든다. 0이면 빈 구역이다.</summary>
        public static PlayerDeploymentZone Rows(int depth)
        {
            if (depth < 0)
                throw new ArgumentOutOfRangeException(nameof(depth));
            return depth == 0 ? Empty : new PlayerDeploymentZone(depth, Array.Empty<CellCoord>());
        }

        /// <summary>저작한 칸 목록으로 구역을 만든다. 칸은 y → x 순서로 정렬해 복사하며, 같은 칸이 두 번 있으면 거절한다.</summary>
        public static PlayerDeploymentZone Of(IEnumerable<CellCoord> cells)
        {
            if (cells == null)
                throw new ArgumentNullException(nameof(cells));

            var unique = new HashSet<CellCoord>();
            foreach (CellCoord cell in cells)
            {
                if (unique.Add(cell) == false)
                    throw new ArgumentException($"아군 배치 칸 ({cell.X}, {cell.Y})이 중복되었습니다.", nameof(cells));
            }

            return new PlayerDeploymentZone(null, unique.OrderBy(c => c.Y).ThenBy(c => c.X).ToArray());
        }

        public bool Contains(CellCoord cell) =>
            RowDepth.HasValue ? cell.Y >= 0 && cell.Y < RowDepth.Value : _cells.Contains(cell);

        /// <summary>칸 구역에서 판 밖에 있는 칸을 y → x 순서로 돌려준다. 줄 구역이면 비어 있다.</summary>
        public IReadOnlyList<CellCoord> CellsOutside(int width, int height) =>
            ListedCells.Where(c => c.X < 0 || c.Y < 0 || c.X >= width || c.Y >= height).ToArray();

        /// <summary>구역이 판 안에 들어가는지 본다. 줄 구역은 줄 수가 판 높이 이하인지, 칸 구역은 모든 칸이 판 안인지 본다.</summary>
        public bool FitsBoard(int width, int height) =>
            RowDepth.HasValue ? RowDepth.Value <= height : CellsOutside(width, height).Count == 0;
    }
}
