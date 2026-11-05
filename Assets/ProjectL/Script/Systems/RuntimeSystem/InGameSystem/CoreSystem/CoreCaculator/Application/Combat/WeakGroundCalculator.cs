using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 실제로 진입한 캐릭터의 약한 지반 누적을 명령 내부 투영에만 반영한다.
    /// 이동 경로·넉백·끌어오기 진입을 모두 세고, 한도에 닿은 칸은 그 명령 끝(<see cref="MovementProjection.Complete"/>)에 무너진다(약한 지반 설계 WG1).
    /// </summary>
    internal static class WeakGroundCalculator
    {
        internal static void RegisterEntry(MovementProjection projection, EntityId characterId, CellCoord cell)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var character = projection.Character(characterId);
            if (character.Health.IsDead || character.Area != CharacterRuntimeArea.Field || !character.OccupiedCells.Contains(cell))
                throw new InvalidOperationException("실제 진입 셀에 있는 살아 있는 캐릭터만 지반 횟수를 누적할 수 있습니다.");

            projection.TerrainQueryCells.Add(cell);
            var ground = projection.Environment.Tiles.OfType<StaticTerrainFact>().SingleOrDefault(source =>
                source.Active && source.Kind == StaticTerrainKind.WeakGround && source.Cell == cell &&
                projection.Bindings.TerrainSourceIds.Contains(source.Id));
            if (ground != null) projection.ChangeTerrain(ground, ground.RegisterEntry());
        }

        /// <summary>
        /// 예정 지형을 모두 바꾼 뒤 개체별로 한 번만 추락시킨다. 배치 중간의 승패로 중단하지 않는다.
        /// 부르는 곳은 명령 끝 한 곳이다(<see cref="MovementProjection.Complete"/>).
        /// </summary>
        internal static void CollapsePending(MovementProjection projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var pending = projection.Environment.Tiles.OfType<StaticTerrainFact>()
                .Where(t => t.PendingCollapse && projection.Bindings.TerrainSourceIds.Contains(t.Id))
                .OrderBy(t => t.Id).ToArray();
            if (pending.Length == 0) return;
            foreach (var ground in pending) projection.ChangeTerrain(ground, ground.Collapse());

            var current = projection.CurrentInput();
            var occupants = current.Characters.Where(c => !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field)
                .Select(c => new { Id = c.EntityId, Cells = c.OccupiedCells })
                .Concat(current.State.Structures.Where(s => !s.Health.IsDead && projection.Bindings.ObstacleEntityIds.Contains(s.EntityId))
                    .Select(s => new { Id = s.EntityId, Cells = s.OccupiedCells }))
                .OrderBy(c => c.Id).ToArray();
            foreach (var occupant in occupants)
            {
                var ground = pending.FirstOrDefault(t => occupant.Cells.Contains(t.Cell));
                if (ground != null) CliffFallCalculator.ResolveAt(projection, occupant.Id, ground.Cell, settleDeaths: false);
            }
            projection.ResolveDeathBoundary();
        }
    }
}
