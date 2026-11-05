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

        // 피해 칸 모양의 원본은 CombatHazardAreas 하나다(View 호버도 같은 입구를 부른다).
        internal static IReadOnlyList<CellCoord> Cells(CellCoord origin, int width, int height)
        {
            var cells = new List<CellCoord>(9);
            CombatHazardAreas.MineCells(origin, width, height, cells);
            return cells.AsReadOnly();
        }
    }
}
