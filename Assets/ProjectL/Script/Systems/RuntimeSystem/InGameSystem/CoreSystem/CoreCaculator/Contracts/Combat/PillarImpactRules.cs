using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 기둥 낙하의 압사 수신자 순서와 잔해 벽을 받을 칸을 한 곳에서 정한다(설계 2026-11-17 OB-6).
    /// 낙하 계산과 Buffer 근거 검사가 같은 규칙을 부르므로, 잔해 정책을 바꿀 때 이 파일만 고치면 두 쪽이 함께 바뀐다.
    /// </summary>
    public static class PillarImpactRules
    {
        /// <summary>영향 칸에 닿은 살아 있는 필드 유닛을 가장 가까운 영향 칸 순, 같으면 ID 순으로 돌려준다. 플랫폼 위 유닛도 포함한다.</summary>
        public static IReadOnlyList<CharacterCombatFacts> CrushedUnits(IEnumerable<CharacterCombatFacts> characters,
            IReadOnlyList<CellCoord> impactCells)
        {
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            var distances = Distances(impactCells);
            return characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                    c.OccupiedCells.Any(distances.ContainsKey))
                .OrderBy(c => Distance(distances, c.OccupiedCells)).ThenBy(c => c.EntityId).ToArray();
        }

        /// <summary>영향 칸에 닿은 파괴되지 않은 장애물을 가장 가까운 영향 칸 순, 같으면 ID 순으로 돌려준다. 일반 구조물은 포함하지 않는다.</summary>
        public static IReadOnlyList<StructureCombatFacts> DestroyedObstacles(IEnumerable<StructureCombatFacts> structures,
            IReadOnlyList<CellCoord> impactCells)
        {
            if (structures == null) throw new ArgumentNullException(nameof(structures));
            var distances = Distances(impactCells);
            return structures.Where(s => s.ObstacleDurability != null && !s.IsDestroyed && s.OccupiedCells.Any(distances.ContainsKey))
                .OrderBy(s => Distance(distances, s.OccupiedCells)).ThenBy(s => s.EntityId).ToArray();
        }

        /// <summary>
        /// 잔해 벽을 받을 수 있는 칸인지다. 지지하는 플랫폼 칸, 이미 벽이거나 메운 낭떠러지인 칸, 살아 있는 일반 구조물 칸은 받지 않는다.
        /// 지형과 플랫폼은 현재 바인딩에 있는 원본만 본다. 낭떠러지는 받으며 같은 ID로 메운다.
        /// </summary>
        public static bool CanReceiveDebris(IEnumerable<MovementTileFact> sources, EnvironmentSemanticBindings bindings,
            IEnumerable<StructureCombatFacts> structures, CellCoord cell)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (structures == null) throw new ArgumentNullException(nameof(structures));
            var tiles = sources as IReadOnlyCollection<MovementTileFact> ?? sources.ToArray();
            return !tiles.OfType<MovingPlatformFact>().Any(p => PlatformMotionRules.SuppliesSupport(p, bindings) && p.Occupies(cell)) &&
                !tiles.OfType<StaticTerrainFact>().Any(t => t.Active && t.Cell == cell && bindings.TerrainSourceIds.Contains(t.Id) &&
                    t.Kind is StaticTerrainKind.Wall or StaticTerrainKind.FilledCliff) &&
                !structures.Any(s => !s.IsDestroyed && s.ObstacleDurability == null && s.OccupiedCells.Contains(cell));
        }

        private static Dictionary<CellCoord, int> Distances(IReadOnlyList<CellCoord> impactCells)
        {
            if (impactCells == null) throw new ArgumentNullException(nameof(impactCells));
            var distances = new Dictionary<CellCoord, int>();
            for (int index = 0; index < impactCells.Count; index++) distances.Add(impactCells[index], index);
            return distances;
        }

        private static int Distance(Dictionary<CellCoord, int> distances, IReadOnlyList<CellCoord> occupied) =>
            occupied.Where(distances.ContainsKey).Min(c => distances[c]);
    }
}
