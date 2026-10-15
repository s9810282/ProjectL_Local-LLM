using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>지뢰의 범위와 현재 보드 후보만 계산한다. 피해·연쇄·사망·소모는 기존 진입 조정자가 정산한다.</summary>
    public sealed class MineMovementTileRule : IMovementTileRule
    {
        public string Key => MineSourceFact.Key;
        public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            var mine = (MineSourceFact)tile;
            var cells = new HashSet<CellCoord>(Cells(mine.Cell, current.State.BoardWidth, current.State.BoardHeight));
            return Array.AsReadOnly(current.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                    c.OccupiedCells.Any(cells.Contains)).Select(c => c.EntityId)
                .Concat(current.State.Structures.Where(s => !s.Health.IsDead && s.OccupiedCells.Any(cells.Contains)).Select(s => s.EntityId))
                .OrderBy(id => id).ToArray());
        }

        public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
        {
            var mine = (MineSourceFact)tile;
            return new(damage: input.Targets.Select(t => new MovementHealthRequest(t.EntityId, mine.Damage)).ToArray(),
                expectedSource: mine, finalSource: mine.WithActive(false));
        }

        internal static IReadOnlyList<CellCoord> Cells(CellCoord origin, int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
            if (origin.X < 0 || origin.Y < 0 || origin.X >= width || origin.Y >= height)
                throw new ArgumentOutOfRangeException(nameof(origin));
            var cells = new List<CellCoord>(9);
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    long x = (long)origin.X + dx;
                    long y = (long)origin.Y + dy;
                    if (x >= 0 && y >= 0 && x < width && y < height)
                        cells.Add(new CellCoord((int)x, (int)y));
                }
            return cells.AsReadOnly();
        }
    }
}
