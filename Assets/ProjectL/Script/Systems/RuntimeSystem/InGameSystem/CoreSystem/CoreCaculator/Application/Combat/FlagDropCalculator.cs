using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>사망 전 위치를 사용해 한 명령의 깃발·분류·할당 진행값을 함께 제안한다.</summary>
    internal static class FlagDropCalculator
    {
        internal static void ResolveDeaths(CombatCommandInput input,
            Dictionary<EntityId, CharacterCombatFacts> characters, IReadOnlyList<StructureCombatFacts> structures,
            IReadOnlyDictionary<EntityId, CellCoord> deathAnchors, ref MovementEnvironmentFacts environment,
            ref EnvironmentSemanticBindings bindings, ref EnvironmentProgressFacts progress, List<CombatEvent> events,
            IReadOnlyCollection<EntityId> fallenEntityIds = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
        }

        internal static IReadOnlyList<CellCoord> NearestCells(CombatCommandState state,
            IReadOnlyList<CharacterCombatFacts> characters, IReadOnlyList<StructureCombatFacts> structures, CellCoord origin,
            EnvironmentSemanticBindings bindings)
        {
            var blocked = structures.Where(s => !s.Health.IsDead).SelectMany(s => s.OccupiedCells)
                .Concat(characters.Where(c => !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field)
                    .SelectMany(c => c.OccupiedCells)).ToHashSet();
            var candidates = new List<CellCoord>();
            // 제곱 거리로 비교하여 대각선 전용 규칙이나 부동소수점 오차 없이 직선거리를 보존한다.
            decimal nearest = decimal.MaxValue;
            for (int y = 0; y < state.BoardHeight; y++)
                for (int x = 0; x < state.BoardWidth; x++)
                {
                    var cell = new CellCoord(x, y);
                    if (blocked.Contains(cell) || EnvironmentTerrainRules.IsWallOccupied(state.Environment, bindings, cell)) continue;
                    decimal dx = (decimal)x - origin.X, dy = (decimal)y - origin.Y;
                    decimal distance = dx * dx + dy * dy;
                    if (distance > nearest) continue;
                    if (distance < nearest) { candidates.Clear(); nearest = distance; }
                    candidates.Add(cell);
                }
            return candidates.AsReadOnly();
        }

        internal static void CreateAt(HeldFlag payload, CellCoord cell,
            Dictionary<EntityId, CharacterCombatFacts> characters, ref MovementEnvironmentFacts environment,
            ref EnvironmentSemanticBindings bindings, ref EnvironmentProgressFacts progress, List<CombatEvent> events)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
        }
    }
}
