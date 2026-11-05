using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 레일 칸의 상하좌우 연결 성분을 서로 떨어진 가로·세로 직선으로 도출합니다.
    /// View 표시용 계산이며, 규칙·발동 시점·Unity 좌표 변환을 소유하지 않습니다.
    /// </summary>
    internal sealed class RailwayTrackLayout
    {
        private RailwayTrackLayout(Track[] tracks)
        {
            Tracks = Array.AsReadOnly(tracks);
        }

        /// <summary>시작 칸의 Y, X 순으로 정렬한 불변 경로 목록입니다.</summary>
        internal IReadOnlyList<Track> Tracks { get; }

        /// <summary>
        /// 입력 순서와 관계없이 같은 직선을 만듭니다. 빈 목록은 선로 없는 필드입니다.
        /// 중복·보드 밖·꺾임·교차·이웃 병렬 선로는 표시 계약에 맞지 않아 거절합니다.
        /// </summary>
        internal static RailwayTrackLayout Create(int columnCount, int rowCount, IReadOnlyList<CellCoord> railCells)
        {
            if (railCells == null)
                throw new ArgumentNullException(nameof(railCells));
            if (columnCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(columnCount));
            if (rowCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(rowCount));

            var sorted = new CellCoord[railCells.Count];
            var remaining = new HashSet<CellCoord>();
            for (int i = 0; i < railCells.Count; i++)
            {
                CellCoord cell = railCells[i];
                if (cell.X < 0 || cell.Y < 0 || cell.X >= columnCount || cell.Y >= rowCount)
                    throw new InvalidOperationException($"[RailwayTrackLayout] 보드 밖 레일 칸입니다: ({cell.X}, {cell.Y}).");
                if (!remaining.Add(cell))
                    throw new InvalidOperationException($"[RailwayTrackLayout] 중복 레일 칸입니다: ({cell.X}, {cell.Y}).");
                sorted[i] = cell;
            }

            Array.Sort(sorted, CompareCells);
            var tracks = new List<Track>();
            var queue = new Queue<CellCoord>();
            var component = new List<CellCoord>();
            foreach (CellCoord start in sorted)
            {
                if (!remaining.Remove(start))
                    continue;

                component.Clear();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    CellCoord cell = queue.Dequeue();
                    component.Add(cell);
                    EnqueueRemaining(cell + CellCoord.Right, remaining, queue);
                    EnqueueRemaining(cell + CellCoord.Left, remaining, queue);
                    EnqueueRemaining(cell + CellCoord.Up, remaining, queue);
                    EnqueueRemaining(cell + CellCoord.Down, remaining, queue);
                }

                component.Sort(CompareCells);
                bool sameX = true;
                bool sameY = true;
                foreach (CellCoord cell in component)
                {
                    sameX &= cell.X == start.X;
                    sameY &= cell.Y == start.Y;
                }

                if (!sameX && !sameY)
                {
                    string cells = string.Join(", ", component.ConvertAll(cell => $"({cell.X}, {cell.Y})"));
                    throw new InvalidOperationException(
                        $"[RailwayTrackLayout] 연결된 레일은 가로 또는 세로 직선이어야 합니다. 꺾임·교차·이웃 병렬 선로를 확인하세요: {cells}.");
                }

                tracks.Add(new Track(component.ToArray()));
            }

            return new RailwayTrackLayout(tracks.ToArray());
        }

        private static int CompareCells(CellCoord left, CellCoord right)
        {
            int row = left.Y.CompareTo(right.Y);
            return row != 0 ? row : left.X.CompareTo(right.X);
        }

        private static void EnqueueRemaining(CellCoord cell, HashSet<CellCoord> remaining, Queue<CellCoord> queue)
        {
            if (remaining.Remove(cell))
                queue.Enqueue(cell);
        }

        /// <summary>한 직선의 불변 칸 목록입니다. 기본 순서는 +X 또는 +Y이며, 단일 칸은 세로입니다.</summary>
        internal sealed class Track
        {
            internal Track(CellCoord[] cells)
            {
                // 생성자가 받은 배열도 복사해 이후 호출자 변경으로 경로가 달라지지 않게 합니다.
                Cells = Array.AsReadOnly((CellCoord[])cells.Clone());
            }

            internal IReadOnlyList<CellCoord> Cells { get; }
            internal bool IsVertical => Cells[0].X == Cells[Cells.Count - 1].X;
            internal CellCoord Direction => IsVertical ? CellCoord.Up : CellCoord.Right;

            /// <summary>Reverse는 끝점만 바꾸며 원본 칸 순서·집합을 바꾸지 않습니다.</summary>
            internal CellCoord GetStart(bool reverse = false) => Cells[reverse ? Cells.Count - 1 : 0];

            internal CellCoord GetEnd(bool reverse = false) => Cells[reverse ? 0 : Cells.Count - 1];
        }
    }
}
