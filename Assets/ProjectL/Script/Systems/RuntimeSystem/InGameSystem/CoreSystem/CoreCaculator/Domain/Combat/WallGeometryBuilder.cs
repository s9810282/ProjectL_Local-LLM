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
                var run = CreateStraightRun(component, settings);
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

        private static WallRun CreateStraightRun(IReadOnlyList<StaticTerrainFact> component, WallBlockingSpec settings)
        {
            int minX = component.Min(wall => wall.Cell.X), maxX = component.Max(wall => wall.Cell.X);
            int minY = component.Min(wall => wall.Cell.Y), maxY = component.Max(wall => wall.Cell.Y);
            // 사방 연결 성분에서 한 축이 일정하면 빈틈 없는 한 줄임이 보장된다.
            if (minX != maxX && minY != maxY)
                throw InvalidContent("벽 연결 성분은 가로 또는 세로 한 줄이어야 합니다.", component);

            const long scale = WallBlockingSpec.TicksPerTile;
            const long halfCell = scale / 2;
            return new WallRun(checked(minX * scale - halfCell), checked(maxX * scale + halfCell),
                checked(minY * scale - halfCell), checked(maxY * scale + halfCell), settings.CornerCutTicks,
                component.Select(wall => wall.Id), component.Select(wall => wall.Cell));
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
