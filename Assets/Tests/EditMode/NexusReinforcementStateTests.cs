using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class NexusReinforcementStateTests
    {
        private static readonly NexusReinforcementSpec Spec = new(
            new[] { new NexusReinforcementEntry(EntityKey.Unit_Demon, 3), new NexusReinforcementEntry(EntityKey.Unit_Cowboy, 2) }, 1);

        [Test]
        public void RemainingSupply_IsDerivedFromAppendOnlyRecords()
        {
            var first = new NexusReinforcementRecord(new EntityId(40), 0);
            var records = new[]
            {
                first, new NexusReinforcementRecord(new EntityId(41), 0), new NexusReinforcementRecord(new EntityId(42), 1)
            };
            var state = new NexusReinforcementState(new EntityId(30), 17, records);
            records[0] = null;

            Assert.That(state.Records[0], Is.SameAs(first));
            Assert.Throws<NotSupportedException>(() => ((IList<NexusReinforcementRecord>)state.Records).Clear());
            Assert.That(state.RemainingCount(Spec, 0), Is.EqualTo(1));
            Assert.That(state.RemainingCount(Spec, 1), Is.EqualTo(1));
            Assert.That(state.RemainingTotal(Spec), Is.EqualTo(2));
            Assert.That(state.Contains(first.EntityId), Is.True);
            Assert.That(state.Contains(new EntityId(43)), Is.False);
        }

        [Test]
        public void InvalidRecordsAndPoolIndices_AreRejected()
        {
            var nexus = new EntityId(30);
            var record = new NexusReinforcementRecord(new EntityId(40), 0);
            Assert.Throws<ArgumentException>(() => new NexusReinforcementRecord(EntityId.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NexusReinforcementRecord(record.EntityId, -1));
            Assert.Throws<ArgumentException>(() => new NexusReinforcementState(EntityId.Empty, 0, Array.Empty<NexusReinforcementRecord>()));
            Assert.Throws<ArgumentException>(() => new NexusReinforcementState(nexus, 0, new[] { record, record }));
            Assert.Throws<ArgumentException>(() => new NexusReinforcementState(nexus, 0, new NexusReinforcementRecord[] { null }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NexusReinforcementState(nexus, 0, new[] { record }).RemainingCount(Spec, 2));
            var wrongPool = new NexusReinforcementState(nexus, 0, new[] { new NexusReinforcementRecord(record.EntityId, 2) });
            Assert.Throws<InvalidOperationException>(() => wrongPool.RemainingTotal(Spec));
            var overspent = new NexusReinforcementState(nexus, 0, Enumerable.Range(40, 3)
                .Select(id => new NexusReinforcementRecord(new EntityId(id), 1)).ToArray());
            Assert.Throws<InvalidOperationException>(() => overspent.RemainingTotal(Spec));
        }

        [Test]
        public void DisabledState_IsEmptyAndImmutable()
        {
            var disabled = CombatCommandTestData.Input().State.Reinforcements;
            Assert.That(disabled, Is.SameAs(NexusReinforcementState.Disabled));
            Assert.That(disabled.IsEnabled, Is.False);
            Assert.That(disabled.NexusId.IsEmpty, Is.True);
            Assert.That(disabled.RandomState, Is.Zero);
            Assert.That(disabled.Records, Is.Empty);
            Assert.Throws<NotSupportedException>(() => ((IList<NexusReinforcementRecord>)disabled.Records).Clear());
        }

        [Test]
        public void Copies_PreserveProvenanceAndRandomState()
        {
            var nexus = CellEnvironmentTestData.Structure(30, cells: new[] { new CellCoord(2, 5) });
            var progress = Progress(nexus.EntityId);
            var source = CombatCommandTestData.Input(empty: true);
            var reinforced = CombatCommandTestData.Character(40, EntityKey.Unit_Demon, new CellCoord(5, 6), Owner.Enemy);
            var battle = new CombatBattleFacts(8, 42, PlayerDeploymentZone.Rows(2),
                Array.Empty<CharacterInstanceId>(), 0, nexusReinforcements: NexusReinforcementTestData.Pool(1, 0));
            var state = source.State.With(structures: new[] { nexus }, reinforcements: progress);
            var input = new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, state,
                source.Characters.Append(reinforced).ToArray(), source.Profiles, source.Main, source.Owned, source.SoulCounts,
                source.Souls, source.Artifacts, battle: battle);
            Assert.That(input.State.With().Reinforcements, Is.SameAs(progress));

            var before = EnemyControllerPhaseTests.Input(input, CombatControllerPhase.StartingEnemy);
            var started = CombatTurnCalculator.BeginControllerTurn(before, Array.Empty<IMovementTileRule>());
            Assert.That(started.Accepted, Is.True, started.Reason);
            var projected = CombatCommandComposition.Project(started);
            Assert.That(projected.State.Reinforcements, Is.SameAs(progress));

            var ended = CombatTurnCalculator.EndEnemyPhase(projected, Array.Empty<IMovementTileRule>());
            Assert.That(ended.Accepted, Is.True, ended.Reason);
            var merged = CombatCommandComposition.Merge(before, new[] { started, ended });
            Assert.That(merged.FinalState.Reinforcements, Is.SameAs(progress));

            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId),
                new List<CombatEvent>());
            projection.MergeStructure(new StructureCombatFacts(nexus.EntityId, nexus.Definition, new HealthStateSnapshot(0, 10),
                nexus.Anchor, nexus.Rotation, nexus.OccupiedCells));
            var terminal = projection.Complete();
            Assert.That(terminal.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(terminal.FinalState.Reinforcements, Is.SameAs(progress));
        }

        [Test]
        public void DifferentProvenance_ChangesGameplayEquality()
        {
            var id = new EntityId(30);
            var progress = Progress(id);
            var state = CombatCommandTestData.Input().State.With(reinforcements: progress);
            var same = new NexusReinforcementState(id, 79, new[] { new NexusReinforcementRecord(new EntityId(40), 0) });
            var otherIndex = new NexusReinforcementState(id, 79, new[] { new NexusReinforcementRecord(new EntityId(40), 1) });
            Assert.That(state.SameValues(state.With(reinforcements: same)), Is.True);
            Assert.That(state.SameGameplayValues(state.With(reinforcements: otherIndex)), Is.False);
            Assert.That(state.SameGameplayValues(state.With(reinforcements: new NexusReinforcementState(id, 0, progress.Records))), Is.False);
            Assert.That(state.SameGameplayValues(state.With(reinforcements: new NexusReinforcementState(new EntityId(31), 79, progress.Records))), Is.False);
            Assert.That(state.SameGameplayValues(state.With(reinforcements: NexusReinforcementState.Disabled)), Is.False);
        }

        [Test]
        public void NewBattle_StartsWithFreshSupply()
        {
            var definition = NexusReinforcementDefinitionTests.Initialization(Spec);
            var first = NexusReinforcementDefinitionTests.Prepare(definition);
            var nexus = first.Structures.Single();
            var initial = first.CommandState.Reinforcements;
            Assert.That(initial.NexusId, Is.EqualTo(nexus.EntityId));
            Assert.That(initial.RandomState, Is.EqualTo(unchecked((uint)first.Combat.TimelineSeed ^ 0x4E585350u)));
            var spent = first.CommandState.With(reinforcements: Progress(nexus.EntityId));
            var seededRecord = new NexusReinforcementState(nexus.EntityId, initial.RandomState, spent.Reinforcements.Records);

            Assert.Throws<ArgumentException>(() => Rebuild(first, spent));
            Assert.Throws<ArgumentException>(() => Rebuild(first, first.CommandState.With(reinforcements: seededRecord)));
            Assert.Throws<ArgumentException>(() => Rebuild(first, first.CommandState.With(reinforcements: NexusReinforcementState.Disabled)));
            Assert.That(Rebuild(first, first.CommandState).CommandState.Reinforcements, Is.SameAs(initial));

            var fresh = NexusReinforcementDefinitionTests.Prepare(definition).CommandState.Reinforcements;
            Assert.That(fresh.IsEnabled, Is.True);
            Assert.That(fresh.Records, Is.Empty);
            Assert.That(fresh.RemainingTotal(Spec), Is.EqualTo(5));
            Assert.That(fresh.SameValues(initial), Is.True);
        }

        private static NexusReinforcementState Progress(EntityId nexus) =>
            new(nexus, 79, new[] { new NexusReinforcementRecord(new EntityId(40), 0) });

        private static BattlePreparation Rebuild(BattlePreparation prepared, CombatCommandState state) =>
            new(prepared.Characters, prepared.Combat, prepared.Structures, commandState: state);
    }
}
