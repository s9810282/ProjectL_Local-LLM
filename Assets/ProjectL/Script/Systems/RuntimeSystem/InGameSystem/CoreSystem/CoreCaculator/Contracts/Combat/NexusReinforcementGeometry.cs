using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public static class NexusReinforcementGeometry
    {
        /// <summary>준비된 넥서스의 기하 범위다. 현재 배치와 풀의 남은 수량은 읽지 않는다.</summary>
        public static IReadOnlyList<CellCoord> Area(CombatCommandState state, NexusReinforcementSpec spec)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (spec == null || !state.Reinforcements.IsEnabled || state.Phase is CombatFlowPhase.None or CombatFlowPhase.Finished)
                return Array.Empty<CellCoord>();

            var nexus = state.Structures.SingleOrDefault(s => s.EntityId == state.Reinforcements.NexusId &&
                s.Owner == Owner.Enemy && s.Definition.Key == EntityKey.Structure_Headquarters && !s.Health.IsDead);
            return nexus == null ? Array.Empty<CellCoord>() : Area(nexus, spec.RangePadding, state.BoardWidth, state.BoardHeight);
        }

        /// <summary>실제 점유 범위에서 바깥으로 넓힌 뒤 보드와 겹치는 칸을 Y, X 순서로 반환한다.</summary>
        public static IReadOnlyList<CellCoord> Area(StructureCombatFacts nexus, int rangePadding, int boardWidth, int boardHeight)
        {
            if (nexus == null)
                throw new ArgumentNullException(nameof(nexus));
            if (rangePadding < 1)
                throw new ArgumentOutOfRangeException(nameof(rangePadding));
            if (boardWidth < 1)
                throw new ArgumentOutOfRangeException(nameof(boardWidth));
            if (boardHeight < 1)
                throw new ArgumentOutOfRangeException(nameof(boardHeight));

            var occupied = new HashSet<CellCoord>(nexus.OccupiedCells);
            long minX = Math.Max(0L, (long)occupied.Min(cell => cell.X) - rangePadding);
            long maxX = Math.Min(boardWidth - 1L, (long)occupied.Max(cell => cell.X) + rangePadding);
            long minY = Math.Max(0L, (long)occupied.Min(cell => cell.Y) - rangePadding);
            long maxY = Math.Min(boardHeight - 1L, (long)occupied.Max(cell => cell.Y) + rangePadding);

            var result = new List<CellCoord>();
            for (long y = minY; y <= maxY; y++)
                for (long x = minX; x <= maxX; x++)
                {
                    var cell = new CellCoord((int)x, (int)y);
                    if (!occupied.Contains(cell))
                        result.Add(cell);
                }

            return result.AsReadOnly();
        }
    }
}
