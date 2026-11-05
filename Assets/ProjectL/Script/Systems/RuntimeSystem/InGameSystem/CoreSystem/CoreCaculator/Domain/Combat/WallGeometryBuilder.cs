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
        private static readonly CellCoord[] DiagonalOffsets =
            { new CellCoord(1, -1), new CellCoord(1, 1) };

        public static WallGeometry Build(int width, int height, MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, WallBlockingSpec settings)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var walls = environment.Tiles.OfType<StaticTerrainFact>()
                .Where(tile => tile.Kind == StaticTerrainKind.Wall).OrderBy(tile => tile.Id).ToArray();
            var byCell = ValidateWalls(width, height, walls, bindings);
            var remaining = new HashSet<CellCoord>(byCell.Keys);
            var runs = new List<WallRun>();
            var runByCell = new Dictionary<CellCoord, WallRun>();
            foreach (var wall in walls)
            {
                if (!remaining.Remove(wall.Cell)) continue;
                var component = CollectComponent(wall.Cell, byCell, remaining);
                var run = CreateRun(component, settings);
                runs.Add(run);
                foreach (var cell in run.Cells) runByCell.Add(cell, run);
            }
            RejectDiagonalContact(walls, byCell, runByCell);
            return new WallGeometry(runs);
        }

        private static Dictionary<CellCoord, StaticTerrainFact> ValidateWalls(int width, int height,
            IEnumerable<StaticTerrainFact> walls, EnvironmentSemanticBindings bindings)
        {
            var terrainIds = new HashSet<long>(bindings.TerrainSourceIds);
            var byCell = new Dictionary<CellCoord, StaticTerrainFact>();
            foreach (var wall in walls)
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
            }
            return byCell;
        }

        private static List<StaticTerrainFact> CollectComponent(CellCoord first,
            IReadOnlyDictionary<CellCoord, StaticTerrainFact> byCell, HashSet<CellCoord> remaining)
        {
            var component = new List<StaticTerrainFact>();
            var pending = new Queue<CellCoord>();
            pending.Enqueue(first);
            while (pending.Count != 0)
            {
                var cell = pending.Dequeue();
                component.Add(byCell[cell]);
                foreach (var offset in CardinalOffsets)
                {
                    var neighbor = new CellCoord(checked(cell.X + offset.X), checked(cell.Y + offset.Y));
                    if (remaining.Remove(neighbor)) pending.Enqueue(neighbor);
                }
            }
            return component;
        }

        private static WallRun CreateRun(IReadOnlyList<StaticTerrainFact> component, WallBlockingSpec settings)
        {
            int minX = component.Min(wall => wall.Cell.X), maxX = component.Max(wall => wall.Cell.X);
            int minY = component.Min(wall => wall.Cell.Y), maxY = component.Max(wall => wall.Cell.Y);
            const long scale = WallBlockingSpec.TicksPerTile;
            const long halfCell = scale / 2;
            long xMin = checked(minX * scale - halfCell), xMax = checked(maxX * scale + halfCell);
            long yMin = checked(minY * scale - halfCell), yMax = checked(maxY * scale + halfCell);
            var cells = component.Select(wall => wall.Cell).OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray();
            var ids = component.Select(wall => wall.Id);
            // 직선은 기존 단일 형상과 모서리 판정을 그대로 유지한다.
            if (minX == maxX || minY == maxY)
                return new WallRun(xMin, xMax, yMin, yMax, settings.CornerCutTicks, ids, cells);

            var occupied = new HashSet<CellCoord>(cells);
            var parts = cells.Select(cell => CreateCellPart(cell, occupied, settings.CornerCutTicks)).ToArray();
            return new WallRun(xMin, xMax, yMin, yMax, settings.CornerCutTicks, ids, cells, parts);
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

        private static void RejectDiagonalContact(IEnumerable<StaticTerrainFact> walls,
            IReadOnlyDictionary<CellCoord, StaticTerrainFact> byCell, IReadOnlyDictionary<CellCoord, WallRun> runByCell)
        {
            foreach (var wall in walls)
            foreach (var offset in DiagonalOffsets)
            {
                var neighbor = new CellCoord(checked(wall.Cell.X + offset.X), checked(wall.Cell.Y + offset.Y));
                if (byCell.TryGetValue(neighbor, out var other) && runByCell[wall.Cell] != runByCell[neighbor])
                    throw InvalidContent("서로 다른 벽 연결 성분은 대각선으로 접할 수 없습니다.", new[] { wall, other });
            }
        }

        private static ArgumentException InvalidContent(string reason, IEnumerable<StaticTerrainFact> sources)
        {
            string evidence = string.Join(", ", sources.OrderBy(source => source.Id)
                .Select(source => $"{source.Id}@({source.Cell.X},{source.Cell.Y})"));
            return new ArgumentException($"{reason} 원본: {evidence}", "environment");
        }
    }
}
