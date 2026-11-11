using System;
using System.Collections.Generic;
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
        internal static bool CollapsesOnNextEntry(MovementTileFact tile, EnvironmentSemanticBindings bindings) =>
            tile is StaticTerrainFact ground && IsWeakGround(ground, bindings) && ground.RemainingEntries <= 1;

        private static bool IsWeakGround(StaticTerrainFact ground, EnvironmentSemanticBindings bindings) =>
            ground.Active && ground.Kind == StaticTerrainKind.WeakGround && bindings.TerrainSourceIds.Contains(ground.Id);

        internal static void RegisterEntry(MovementProjection projection, EntityId characterId, CellCoord cell)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var character = projection.Character(characterId);
            if (character.Health.IsDead || character.Area != CharacterRuntimeArea.Field || !character.OccupiedCells.Contains(cell))
                throw new InvalidOperationException("실제 진입 셀에 있는 살아 있는 캐릭터만 지반 횟수를 누적할 수 있습니다.");

            projection.TerrainQueryCells.Add(cell);
            var ground = projection.Environment.Tiles.OfType<StaticTerrainFact>().SingleOrDefault(source =>
                source.Cell == cell && IsWeakGround(source, projection.Bindings));
            if (ground != null) projection.ChangeTerrain(ground, ground.RegisterEntry());
        }

        /// <summary>
        /// 예정 지형을 모두 바꾼 뒤 개체별로 한 번만 추락시킨다. 배치 중간의 승패로 중단하지 않는다.
        /// 부르는 곳은 명령 끝 한 곳이다(<see cref="MovementProjection.Complete"/>).
        /// </summary>
        internal static void CollapsePending(MovementProjection projection)
        {
            foreach (var _ in CollapsePendingSteps(projection)) { }
        }

        internal static IEnumerable<string> CollapsePendingSteps(MovementProjection projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            SortedDictionary<long, StaticTerrainFact> pending = null;
            foreach (var tile in projection.Environment.Tiles)
            {
                if (tile is StaticTerrainFact ground && ground.PendingCollapse &&
                    projection.Bindings.TerrainSourceIds.Contains(ground.Id))
                    (pending ??= new SortedDictionary<long, StaticTerrainFact>()).Add(ground.Id, ground);
                yield return "WeakGroundScan";
            }
            if (pending == null) yield break;
            var groundsByCell = new Dictionary<CellCoord, long>();
            foreach (var ground in pending.Values)
            {
                projection.ChangeTerrain(ground, ground.Collapse());
                groundsByCell.TryAdd(ground.Cell, ground.Id);
                yield return "WeakGroundCollapse";
            }

            using var observation = projection.BeginCurrentInput();
            while (!observation.IsComplete)
            {
                observation.Step();
                yield return "WeakGroundObservation";
            }
            var current = observation.Result;
            var occupants = new SortedDictionary<(EntityId Id, bool IsStructure), IReadOnlyList<CellCoord>>();
            foreach (var character in current.Characters)
            {
                if (!character.Health.IsDead && character.Area == CharacterRuntimeArea.Field)
                    occupants.Add((character.EntityId, false), character.OccupiedCells);
                yield return "WeakGroundOccupant";
            }
            foreach (var structure in current.State.Structures)
            {
                if (!structure.Health.IsDead && projection.Bindings.ObstacleEntityIds.Contains(structure.EntityId))
                    occupants.Add((structure.EntityId, true), structure.OccupiedCells);
                yield return "WeakGroundOccupant";
            }
            foreach (var occupant in occupants)
            {
                long? first = null;
                CellCoord contact = default;
                foreach (var cell in occupant.Value)
                {
                    if (groundsByCell.TryGetValue(cell, out long id) && (!first.HasValue || id < first.Value))
                    {
                        first = id;
                        contact = cell;
                    }
                    yield return "WeakGroundContact";
                }
                if (first.HasValue) CliffFallCalculator.ResolveAt(projection, occupant.Key.Id, contact, settleDeaths: false);
                yield return "WeakGroundFall";
            }
            projection.ResolveDeathBoundary();
            yield return "WeakGroundDeaths";
        }
    }
}
