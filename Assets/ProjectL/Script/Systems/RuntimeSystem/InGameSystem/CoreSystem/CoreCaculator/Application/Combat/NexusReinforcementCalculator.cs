using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    internal static class NexusReinforcementCalculator
    {
        internal static CombatCommandTransition TryCalculate(CombatCommandInput startedEnemyTurn, int livingFieldEnemiesAtStart)
        {
            var input = startedEnemyTurn ?? throw new ArgumentNullException(nameof(startedEnemyTurn));
            if (livingFieldEnemiesAtStart < 0)
                throw new ArgumentOutOfRangeException(nameof(livingFieldEnemiesAtStart));
            var state = input.State;
            var spec = input.Battle?.NexusReinforcements;
            if (spec == null || state.Phase != CombatFlowPhase.Automatic || state.Turn.StagePhase != StagePhase.Combat ||
                state.Turn.ControllerPhase != CombatControllerPhase.Enemy)
                return null;
            if (!state.Reinforcements.IsEnabled || state.Objective.Kind != BattleObjectiveKind.EnemyHeadquartersDestroyed)
                throw new InvalidOperationException("증원 정의에 맞는 넥서스와 전투 목표가 필요합니다.");

            if (!state.Reinforcements.IsSpawnDue(spec, state.Turn.CompletedPlayerTurnCount))
                return null;

            int capacity = spec.FieldEnemyLimit - livingFieldEnemiesAtStart;
            if (capacity <= 0)
                return null;
            var remaining = state.Reinforcements.CopyRemainingCounts(spec);
            int total = remaining.Sum();
            if (total == 0)
                return null;

            var cells = Candidates(input).ToList();
            int count = Math.Min(Math.Min(spec.SpawnCountPerTurn, capacity), Math.Min(total, cells.Count));
            if (count == 0)
                return null;
            if (!state.Allocation.IsAvailable)
                throw new InvalidOperationException("넥서스 증원에는 전투 개체 할당 원본이 필요합니다.");

            var traits = TraitRegistry.CreateDefault();
            var units = new UnitCatalog(spec.Pool.Select(entry =>
                input.Profiles.Single(p => !p.OwnedId.HasValue && p.Definition.Key == entry.UnitKey).Definition).ToArray(),
                traits, new CategoryRegistry(input.Battle.Categories));
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>(count);
            var records = state.Reinforcements.Records.ToList();
            var origins = state.Turn.EnemyOrigins.ToList();
            uint random = state.Reinforcements.RandomState;
            long nextId = state.Allocation.NextEntityId;

            for (int i = 0; i < count; i++)
            {
                int roll = EnvironmentRandom.Choose(ref random, total);
                int entryIndex = 0;
                while (roll >= remaining[entryIndex])
                {
                    roll -= remaining[entryIndex];
                    entryIndex++;
                }
                remaining[entryIndex]--;
                total--;

                int cellIndex = EnvironmentRandom.Choose(ref random, cells.Count);
                var cell = cells[cellIndex];
                cells.RemoveAt(cellIndex);
                var id = new EntityId(checked((int)nextId));
                nextId++;
                var unit = units.GetRequired(spec.Pool[entryIndex].UnitKey);
                var created = BattleCharacterCalculator.CreateEnemy(id, unit, spec.EnemyLevel, cell,
                    Rotation4.R0, new[] { cell }, traits).With(actionUse: UnitActionUse.Deferred);
                var record = new NexusReinforcementRecord(id, entryIndex);
                records.Add(record);
                origins.Add(new CombatEnemyOrigin(id, cell, Rotation4.R0));
                projected.Add(id, created);
                events.Add(new CombatReinforcementEvent(state.Reinforcements.NexusId, record, created));
            }

            var turn = state.Turn;
            var final = state.With(turn: new CombatTurnFacts(turn.StagePhase,
                turn.ControllerPhase, turn.ControllerTurnSequence, turn.CompletedPlayerTurnCount,
                origins, turn.Summons, turn.CompletedControllerTurnCount),
                allocation: new CombatAllocationFacts(nextId, state.Allocation.SummonRandomState),
                reinforcements: new NexusReinforcementState(state.Reinforcements.NexusId, random, records, turn.CompletedPlayerTurnCount));
            // 배치 전체가 보이는 세계에서 한 번 정산하고 생성 개체의 최종 사실까지 같은 결과로 보낸다.
            return CombatCommandCalculator.Finish(input, projected, input.SoulCounts.ToDictionary(p => p.Key, p => p.Value),
                input.Souls, state.Structures.ToArray(), new List<long>(),
                new Dictionary<EntityId, CellCoord>(), new SoulEffectCalculator(input.Artifacts), null, final, events: events);
        }

        /// <summary>증원 범위 안에서 현재 증원에 사용할 수 있는 칸을 순서대로 반환한다.</summary>
        internal static IReadOnlyList<CellCoord> Candidates(CombatCommandInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var area = NexusReinforcementGeometry.Area(input.State, input.Battle?.NexusReinforcements);
            if (area.Count == 0)
                return area;

            var blocked = new HashSet<CellCoord>(input.Battle.RailCells);
            foreach (var structure in input.State.Structures)
                if (!structure.Health.IsDead)
                    blocked.UnionWith(structure.OccupiedCells);

            foreach (var character in input.Characters)
            {
                if (character.Area != CharacterRuntimeArea.Field || character.Health.IsDead)
                    continue;

                blocked.UnionWith(character.OccupiedCells);
                if (character.Owner != Owner.Player)
                    continue;

                foreach (var cell in character.OccupiedCells)
                {
                    blocked.Add(new CellCoord(cell.X - 1, cell.Y));
                    blocked.Add(new CellCoord(cell.X + 1, cell.Y));
                    blocked.Add(new CellCoord(cell.X, cell.Y - 1));
                    blocked.Add(new CellCoord(cell.X, cell.Y + 1));
                }
            }

            // 가시성과 관계없이 현재 활성인 실제 원본과 효과 칸을 제외한다.
            var bindings = input.EnvironmentBindings;
            foreach (var source in input.State.Environment.Tiles)
                if (source.Active &&
                    (source is ObjectSourceFact or TraitTrailFact || EnvironmentContentSupport.IsSupportedInitialTerrain(source) ||
                     bindings.TerrainSourceIds.Contains(source.Id) || bindings.ObjectSourceIds.Contains(source.Id)))
                    blocked.Add(source.Cell);

            AddGimmickCells(input, blocked);

            return Array.AsReadOnly(area.Where(cell => !blocked.Contains(cell)).ToArray());
        }

        private static void AddGimmickCells(CombatCommandInput input, HashSet<CellCoord> blocked)
        {
            var definitions = input.Battle.GimmickDefinitions;
            foreach (var gimmick in input.State.Gimmicks.Entries)
            {
                if (gimmick is HaboobGimmickState haboob)
                    blocked.UnionWith(haboob.Cells);
                if (gimmick is not TornadoGimmickState tornado || input.State.Turn.StagePhase != StagePhase.Combat)
                    continue;

                int index = tornado.Id.Value - 1;
                if (index >= definitions.Count || definitions[index] is not TornadoGimmickSpec spec)
                    throw new InvalidOperationException("토네이도 상태에 대응하는 정의가 없습니다.");

                foreach (var member in tornado.Members)
                {
                    var definition = spec.Members.Single(m => m.SourceId == member.SourceId);
                    blocked.UnionWith(TornadoGeometry.Footprint(member.Center, definition.SideLength));
                }
            }
        }
    }
}
