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
            ref EnvironmentSemanticBindings bindings, ref EnvironmentProgressFacts progress, List<CombatEvent> events)
        {
            if (BattleOutcomeCalculator.Resolve(characters.Values.ToArray(), structures).IsStageEnded) return;
            foreach (var dead in characters.Values.Where(c => c.Health.IsDead && c.HeldEnvironment.Flag != null &&
                input.Characters.Any(before => before.EntityId == c.EntityId && !before.Health.IsDead)).OrderBy(c => c.EntityId).ToArray())
            {
                var held = dead.HeldEnvironment.Flag;
                var cleared = HeldEnvironmentEffects.ClearFlag(dead);
                characters[dead.EntityId] = cleared;
                events.Add(new CombatHeldEnvironmentEvent(dead, cleared));
                var before = input.Characters.Single(c => c.EntityId == dead.EntityId);
                var anchor = deathAnchors.GetValueOrDefault(dead.EntityId, before.Anchor ?? before.MovementCheckpointAnchor);
                var candidates = NearestCells(input.State, characters.Values.ToArray(), structures, anchor, bindings);
                if (candidates.Count == 0) continue;
                uint random = progress.RandomState;
                int index = EnvironmentRandom.Choose(ref random, candidates.Count);
                progress = new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, random);
                CreateAt(held, candidates[index], characters, ref environment, ref bindings, ref progress, events);
            }
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
            var source = new FlagSourceFact(progress.NextSourceId, cell, progress.NextObjectEntryOrdinal,
                payload.PriorityBonus, true, payload.KnownTo);
            progress = new EnvironmentProgressFacts(checked(progress.NextObjectEntryOrdinal + 1),
                checked(progress.NextSourceId + 1), progress.RandomState);
            bindings = new EnvironmentSemanticBindings(bindings.TerrainSourceIds,
                bindings.ObjectSourceIds.Append(source.Id).ToArray(), bindings.ObstacleEntityIds);
            events.Add(new CombatObjectCreatedEvent(source));
            ObjectSourceFact final = source;
            var occupant = characters.Values.SingleOrDefault(c => !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field &&
                c.OccupiedCells.Contains(cell));
            if (occupant != null && occupant.HeldEnvironment.Flag == null)
            {
                var acquired = HeldEnvironmentEffects.AcquireFlag(occupant, source);
                characters[occupant.EntityId] = acquired;
                events.Add(new CombatObjectActivationEvent(source, occupant.EntityId));
                events.Add(new CombatHeldEnvironmentEvent(occupant, acquired));
                final = source.WithActive(false);
                events.Add(new CombatMovementSourceEvent(source, final));
            }
            environment = new MovementEnvironmentFacts(environment.Tiles.Append(final).ToArray());
        }
    }
}
