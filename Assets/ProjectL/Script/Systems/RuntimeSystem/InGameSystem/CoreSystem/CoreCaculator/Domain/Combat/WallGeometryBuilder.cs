using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>벽 원본의 유효성과 연결 성분을 판정한다. 공격 또는 이동 허용 여부는 판정하지 않는다.</summary>
    internal static class WallGeometryBuilder
    {
        private static readonly CellCoord[] CardinalOffsets =
            { new CellCoord(-1, 0), new CellCoord(1, 0), new CellCoord(0, -1), new CellCoord(0, 1) };

        public static WallGeometry Build(int width, int height, MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, WallBlockingSpec settings)
        {
            WallGeometry geometry = null;
            foreach (var _ in BuildSteps(width, height, environment, bindings, settings, value => geometry = value)) { }
            return geometry;
        }

        internal static IEnumerable<string> BuildSteps(int width, int height, MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, WallBlockingSpec settings, Action<WallGeometry> completed)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var walls = new SortedDictionary<long, StaticTerrainFact>();
            foreach (var tile in environment.Tiles)
            {
                if (tile is StaticTerrainFact wall && wall.Kind == StaticTerrainKind.Wall) walls.Add(wall.Id, wall);
                yield return "WallGeometrySource";
            }
            var terrainIds = new HashSet<long>();
            foreach (var id in bindings.TerrainSourceIds)
            {
                terrainIds.Add(id);
                yield return "WallGeometryBinding";
            }
            var byCell = new Dictionary<CellCoord, StaticTerrainFact>();
            var remaining = new HashSet<CellCoord>();
            foreach (var wall in walls.Values)
            {
                if (!wall.Active || !wall.KnownTo.Contains(Owner.Player) || !wall.KnownTo.Contains(Owner.Enemy))
                    throw InvalidContent("벽은 활성 상태이며 양측에 공개되어야 합니다.", new[] { wall });
                if (!terrainIds.Contains(wall.Id))
                    throw InvalidContent("벽 원본은 지형으로 분류되어야 합니다.", new[] { wall });
                if (wall.Cell.X < 0 || wall.Cell.X >= width || wall.Cell.Y < 0 || wall.Cell.Y >= height)
                    throw InvalidContent("벽 셀이 보드 범위를 벗어났습니다.", new[] { wall });
                if (byCell.TryGetValue(wall.Cell, out var existing))
                    throw InvalidContent("같은 셀에 벽 원본을 중복 배치할 수 없습니다.", new[] { existing, wall });
                byCell.Add(wall.Cell, wall);
                remaining.Add(wall.Cell);
                yield return "WallGeometryValidate";
            }
            var runs = new List<WallRun>();
            var runByCell = new Dictionary<CellCoord, WallRun>();
            foreach (var wall in walls.Values)
            {
                yield return "WallGeometryComponent";
                if (!remaining.Remove(wall.Cell)) continue;
                var occupied = new HashSet<CellCoord>();
                var cells = new SortedSet<(int Y, int X)>();
                var ids = new List<long>();
                int minX = wall.Cell.X, maxX = minX, minY = wall.Cell.Y, maxY = minY;
                var pending = new Queue<CellCoord>();
                pending.Enqueue(wall.Cell);
                while (pending.Count != 0)
                {
                    var cell = pending.Dequeue();
                    occupied.Add(cell);
                    cells.Add((cell.Y, cell.X));
                    ids.Add(byCell[cell].Id);
                    minX = Math.Min(minX, cell.X);
                    maxX = Math.Max(maxX, cell.X);
                    minY = Math.Min(minY, cell.Y);
                    maxY = Math.Max(maxY, cell.Y);
                    foreach (var offset in CardinalOffsets)
                    {
                        var neighbor = new CellCoord(checked(cell.X + offset.X), checked(cell.Y + offset.Y));
                        if (remaining.Remove(neighbor)) pending.Enqueue(neighbor);
                    }
                    yield return "WallGeometryCell";
                }
                const long scale = WallBlockingSpec.TicksPerTile;
                const long halfCell = scale / 2;
                long xMin = checked(minX * scale - halfCell), xMax = checked(maxX * scale + halfCell);
                long yMin = checked(minY * scale - halfCell), yMax = checked(maxY * scale + halfCell);
                var ordered = new List<CellCoord>();
                var parts = new List<WallConvexPart>();
                bool straight = minX == maxX || minY == maxY;
                foreach (var item in cells)
                {
                    var cell = new CellCoord(item.X, item.Y);
                    ordered.Add(cell);
                    if (!straight) parts.Add(CreateCellPart(cell, occupied, settings.CornerCutTicks));
                    yield return "WallGeometryPart";
                }
                var run = straight
                    ? new WallRun(xMin, xMax, yMin, yMax, settings.CornerCutTicks, ids, ordered)
                    : new WallRun(xMin, xMax, yMin, yMax, settings.CornerCutTicks, ids, ordered, parts);
                runs.Add(run);
                yield return "WallGeometryRun";
                foreach (var cell in run.Cells)
                {
                    runByCell.Add(cell, run);
                    yield return "WallGeometryIndex";
                }
            }
            // 대각선으로만 닿는 잔해는 별도 연결 성분으로 유지하고 각 성분의 기존 차단 형상을 쓴다.
            completed(new WallGeometry(runs, runByCell));
            yield return "WallGeometryComplete";
        }

        private static WallConvexPart CreateCellPart(CellCoord cell, HashSet<CellCoord> occupied, int cut)
        {
            bool left = occupied.Contains(new CellCoord(cell.X - 1, cell.Y));
            bool right = occupied.Contains(new CellCoord(cell.X + 1, cell.Y));
            bool bottom = occupied.Contains(new CellCoord(cell.X, cell.Y - 1));
            bool top = occupied.Contains(new CellCoord(cell.X, cell.Y + 1));
            WallBoundary exterior = WallBoundary.None;
            if (!left) exterior |= WallBoundary.Left;
            if (!right) exterior |= WallBoundary.Right;
            if (!bottom) exterior |= WallBoundary.Bottom;
            if (!top) exterior |= WallBoundary.Top;
            // 꼭짓점 주변 네 칸 중 이 칸만 점유한 경우가 바깥 볼록 모서리다.
            WallCorner corners = WallCorner.None;
            if (!left && !bottom && !occupied.Contains(new CellCoord(cell.X - 1, cell.Y - 1))) corners |= WallCorner.BottomLeft;
            if (!left && !top && !occupied.Contains(new CellCoord(cell.X - 1, cell.Y + 1))) corners |= WallCorner.TopLeft;
            if (!right && !bottom && !occupied.Contains(new CellCoord(cell.X + 1, cell.Y - 1))) corners |= WallCorner.BottomRight;
            if (!right && !top && !occupied.Contains(new CellCoord(cell.X + 1, cell.Y + 1))) corners |= WallCorner.TopRight;
            const long scale = WallBlockingSpec.TicksPerTile;
            const long halfCell = scale / 2;
            return new WallConvexPart(checked(cell.X * scale - halfCell), checked(cell.X * scale + halfCell),
                checked(cell.Y * scale - halfCell), checked(cell.Y * scale + halfCell), cut, exterior, corners);
        }


        private static ArgumentException InvalidContent(string reason, IEnumerable<StaticTerrainFact> sources)
        {
            string evidence = string.Join(", ", sources.OrderBy(source => source.Id)
                .Select(source => $"{source.Id}@({source.Cell.X},{source.Cell.Y})"));
            return new ArgumentException($"{reason} 원본: {evidence}", "environment");
        }
    }
}
