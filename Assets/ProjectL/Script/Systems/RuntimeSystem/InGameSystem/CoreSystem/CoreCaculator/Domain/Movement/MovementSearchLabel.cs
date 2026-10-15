using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement
{
    /// <summary>AP와 칸 수 중 하나만 좋은 경로도 유지한다. 탐색 중에만 사용하며 게시 상태를 소유하지 않는다.</summary>
    internal sealed class MovementSearchLabel
    {
        internal CellCoord Cell { get; }
        internal int Distance { get; }
        internal long Cost { get; }
        internal MovementSearchLabel Previous { get; }

        internal MovementSearchLabel(CellCoord cell, int distance, long cost, MovementSearchLabel previous = null)
        { Cell = cell; Distance = distance; Cost = cost; Previous = previous; }

        internal static bool TryAdd(Dictionary<CellCoord, List<MovementSearchLabel>> labels, MovementSearchLabel candidate)
        {
            if (!labels.TryGetValue(candidate.Cell, out var existing))
                labels.Add(candidate.Cell, existing = new List<MovementSearchLabel>());
            if (existing.Any(l => l.Distance <= candidate.Distance && l.Cost <= candidate.Cost)) return false;
            existing.RemoveAll(l => candidate.Distance <= l.Distance && candidate.Cost <= l.Cost);
            existing.Add(candidate);
            return true;
        }
    }
}
