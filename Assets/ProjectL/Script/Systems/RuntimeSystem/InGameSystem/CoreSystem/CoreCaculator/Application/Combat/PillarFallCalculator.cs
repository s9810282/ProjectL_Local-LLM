using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>기둥의 각도·영향 칸과 압사·잔해 생성을 계산한다. 플랫폼 이동 순서는 알지 않는다.</summary>
    internal static class PillarFallCalculator
    {
        // HTML 행은 아래로 증가하므로 그 방향표의 Y를 Core 좌표로 뒤집는다.
        private static readonly CellCoord[] Directions =
        {
            new CellCoord(1, 0), new CellCoord(1, -1), new CellCoord(0, -1), new CellCoord(-1, -1),
            new CellCoord(-1, 0), new CellCoord(-1, 1), new CellCoord(0, 1), new CellCoord(1, 1)
        };

        internal static IReadOnlyList<CellCoord> GetCells(CellCoord pillar, CellCoord source, int length,
            int boardWidth, int boardHeight)
        {
            if (length < 1) throw new ArgumentOutOfRangeException(nameof(length));
            if (boardWidth < 1) throw new ArgumentOutOfRangeException(nameof(boardWidth));
            if (boardHeight < 1) throw new ArgumentOutOfRangeException(nameof(boardHeight));
            bool Inside(CellCoord c) => c.X >= 0 && c.Y >= 0 && c.X < boardWidth && c.Y < boardHeight;
            if (!Inside(pillar)) throw new ArgumentOutOfRangeException(nameof(pillar));
            if (!Inside(source) || source == pillar)
                throw new ArgumentException("낙하 방향에는 기둥과 다른 보드 안의 물리 출처가 필요합니다.", nameof(source));
            var direction = Direction(pillar, source);
            var cells = new List<CellCoord>();
            var cell = pillar;
            for (int distance = 0; distance < length; distance++)
            {
                cell += direction;
                if (!Inside(cell)) break;
                cells.Add(cell);
            }
            return cells.AsReadOnly();
        }

        private static CellCoord Direction(CellCoord pillar, CellCoord source)
        {
            double angle = Math.Atan2((double)source.Y - pillar.Y, (double)pillar.X - source.X);
            int index = ((int)Math.Floor((angle + Math.PI / 8) / (Math.PI / 4)) % 8 + 8) % 8;
            return Directions[index];
        }

        internal static IEnumerable<string> ResolveSteps(CombatCommandInput input, CommandEnvironmentState environment,
            CombatDamageApplication damage, Dictionary<EntityId, StructureCombatFacts> structures,
            Dictionary<EntityId, CellCoord> deathAnchors, ObstacleReactionCalculator obstacles,
            CombatObstacleDurabilityEvent destruction, CombatObstacleReactionEvent reaction,
            int length, IReadOnlyList<CellCoord> cells)
        {
            // 수신자 순서와 잔해 칸 자격은 근거 검사와 같은 규칙을 쓴다(OB-6).
            var victims = PillarImpactRules.CrushedUnits(damage.Characters.Values, cells);
            var destroyed = PillarImpactRules.DestroyedObstacles(structures.Values, cells);
            var fall = new CombatPillarFallEvent(destruction, reaction, length, Direction(destruction.Expected.Anchor, destruction.Origin.PhysicalCell.Value), cells,
                victims.Select(c => c.EntityId).ToArray(), destroyed.Select(s => s.EntityId).ToArray());
            damage.Events.Add(fall);
            obstacles.TerrainQueryCells.UnionWith(cells);
            obstacles.TerrainQueryCells.Add(fall.PillarCell);
            yield return "PillarImpact";
            foreach (var victim in victims)
            {
                obstacles.BlastTargetIds.Add(victim.EntityId);
                deathAnchors[victim.EntityId] = victim.Anchor.Value;
                // 앞선 압사의 처치 정산이 뒤 수신자의 체력을 바꿀 수 있어 낙하 시작 사본이 아니라 실제 감소량을 더한다(OB-9).
                int loss = 0;
                foreach (var phase in damage.CrushSteps(victim.EntityId, reaction.Origin, fall, value => loss = value))
                    yield return phase;
                obstacles.BlastDamage[victim.EntityId] = checked(obstacles.BlastDamage.GetValueOrDefault(victim.EntityId) + loss);
            }
            foreach (var obstacle in destroyed)
            {
                obstacles.BlastTargetIds.Add(obstacle.EntityId);
                obstacles.DestroyObstacle(obstacle.EntityId, reaction.Origin, ObstacleDestructionCause.PillarCrush, fall);
                yield return "PillarObstacle";
            }

            var current = environment.Facts;
            var changes = new List<CombatPillarTerrainEvent>();
            long nextId = environment.Progress.NextSourceId;
            foreach (var cell in cells)
            {
                // 지지면은 잔해만 막는다. 위의 압사 수신자 조사에서는 플랫폼 여부를 보지 않는다.
                if (!PillarImpactRules.CanReceiveDebris(current.Tiles, environment.Bindings, structures.Values, cell)) continue;
                var cliff = current.Tiles.OfType<StaticTerrainFact>().SingleOrDefault(t => t.Active && t.Cell == cell &&
                    environment.Bindings.TerrainSourceIds.Contains(t.Id) && t.Kind == StaticTerrainKind.Cliff);
                var next = cliff?.FillCliff() ?? new StaticTerrainFact(nextId, cell, StaticTerrainKind.Wall, true,
                    new[] { Owner.Player, Owner.Enemy });
                if (cliff == null) nextId = checked(nextId + 1);
                changes.Add(new CombatPillarTerrainEvent(fall, cliff, next));
            }
            environment.ApplyPillarTerrainChanges(changes);
            damage.Events.AddRange(changes);
            yield return "PillarTerrain";
        }
    }
}
