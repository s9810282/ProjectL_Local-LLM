using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 행동 이동 범위와 유닛 호버가 함께 쓰는 탐색이다. 각 칸의 최단 걸음 수와 처음 발견한 부모 칸,
    /// 그리고 "목적지 = 자기 칸이 아닌 착지 가능 칸, Y·X 순서" 규칙의 결과를 한 곳에서 만든다.
    /// </summary>
    internal sealed class MovementRangeSearch
    {
        private static readonly CellCoord[] Directions = { CellCoord.Right, CellCoord.Up, CellCoord.Left, CellCoord.Down };

        private readonly CellCoord _origin;
        private readonly Dictionary<CellCoord, int> _distances;
        private readonly Dictionary<CellCoord, CellCoord> _parents;

        /// <summary>원점을 뺀 착지 가능 칸이다(Y → X 순서). 경로 동률 해소 순서와는 무관한 출력 순서다.</summary>
        internal IReadOnlyList<CellCoord> Destinations { get; }

        private MovementRangeSearch(CellCoord origin, Dictionary<CellCoord, int> distances,
            Dictionary<CellCoord, CellCoord> parents, CellCoord[] destinations)
        {
            _origin = origin;
            _distances = distances;
            _parents = parents;
            Destinations = Array.AsReadOnly(destinations);
        }

        /// <summary>
        /// 검증된 행위자의 원점과 이동 한도 안에서 발견한 칸을 기록한다.
        /// 통과 가능 칸만 다음 탐색을 이어 가고, 통과는 안 되지만 착지 가능한 칸(자리 교환 종점)은 종점으로만 둔다.
        /// 도약으로 통과만 가능한 칸은 탐색 정점이지만 착지 판정에서 빠져 목적지가 되지 않는다.
        /// </summary>
        internal static MovementRangeSearch Calculate(CombatCommandInput input, CharacterCombatFacts actor,
            MovementEnvironmentFacts environment, int allowance)
        {
            var origin = actor.Anchor.Value;
            var distances = new Dictionary<CellCoord, int> { [origin] = 0 };
            var parents = new Dictionary<CellCoord, CellCoord>();
            var destinations = new List<CellCoord>();
            var queue = new Queue<CellCoord>();
            queue.Enqueue(origin);

            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                int distance = distances[current];
                if (distance == allowance) continue;

                foreach (var direction in Directions)
                {
                    var cell = current + direction;
                    if (distances.ContainsKey(cell)) continue;

                    bool canTraverse = MovementRouteValidator.CanTraverse(input, cell, false, environment);
                    bool canLand = CombatMovementCalculator.CanLand(input, cell, environment);
                    if (!canTraverse && !canLand) continue;

                    distances.Add(cell, distance + 1);
                    parents.Add(cell, current);
                    if (canLand) destinations.Add(cell);
                    if (canTraverse) queue.Enqueue(cell);
                }
            }

            return new MovementRangeSearch(origin, distances, parents,
                destinations.OrderBy(c => c.Y).ThenBy(c => c.X).ToArray());
        }

        /// <summary>발견한 칸까지 원점을 포함하는 경로를 복원한다.</summary>
        internal MovementPath BuildRoute(CellCoord target)
        {
            if (!_distances.TryGetValue(target, out int distance))
                throw new ArgumentException("이동 탐색에서 발견하지 못한 칸입니다.", nameof(target));

            var cells = new CellCoord[distance + 1];
            cells[distance] = target;
            for (int step = distance; step > 0; step--)
            {
                if (!_parents.TryGetValue(cells[step], out var parent))
                    throw new InvalidOperationException("이동 경로의 부모 칸을 찾을 수 없습니다.");
                cells[step - 1] = parent;
            }

            if (cells[0] != _origin)
                throw new InvalidOperationException("이동 경로가 탐색 원점과 연결되지 않았습니다.");

            return new MovementPath(cells);
        }
    }
}
