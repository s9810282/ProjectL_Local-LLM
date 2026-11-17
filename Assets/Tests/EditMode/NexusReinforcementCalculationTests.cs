using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class NexusReinforcementCalculationTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Interval_FirstAttemptWaitsForConfiguredTurn(int interval)
        {
            var spec = new NexusReinforcementSpec(new[] { new NexusReinforcementEntry(EntityKey.Unit_Demon, 8) }, 1, spawnIntervalTurns: interval);
            for (int turn = 1; turn <= interval; turn++)
            {
                var input = EnemyControllerPhaseTests.Input(NexusReinforcementTestData.Input(spec), CombatControllerPhase.Enemy, completed: turn);
                Assert.That(NexusReinforcementCalculator.TryCalculate(input, 0) != null, Is.EqualTo(turn == interval));
            }
        }

        [TestCase(false, 3)]
        [TestCase(true, 3)]
        [TestCase(false, 4)]
        [TestCase(true, 4)]
        [TestCase(false, 9)]
        [TestCase(true, 9)]
        public void Interval_BlockedThenPartialSuccessRestartsCycle(bool capacityBlocked, int successTurn)
        {
            var spec = new NexusReinforcementSpec(new[] { new NexusReinforcementEntry(EntityKey.Unit_Demon, 8) }, 1, spawnIntervalTurns: 2);
            var cells = NexusReinforcementCalculator.Candidates(NexusReinforcementTestData.Input(spec)).ToArray();
            var blocked = EnemyControllerPhaseTests.Input(NexusReinforcementTestData.Input(spec, blocked: capacityBlocked ? null : cells), CombatControllerPhase.Enemy, completed: 2);
            for (int turn = 2; turn < successTurn; turn++)
            {
                blocked = EnemyControllerPhaseTests.Input(blocked, CombatControllerPhase.Enemy, completed: turn);
                Assert.That(NexusReinforcementCalculator.TryCalculate(blocked, capacityBlocked ? 5 : 0), Is.Null);
            }
            var ready = EnemyControllerPhaseTests.Input(NexusReinforcementTestData.Input(spec, blocked: cells.Skip(1).ToArray()), CombatControllerPhase.Enemy, completed: successTurn);
            var batch = NexusReinforcementCalculator.TryCalculate(ready, 0);
            Assert.That(batch.Characters.Created.Count, Is.EqualTo(1));
            Assert.That(batch.FinalState.Reinforcements.RemainingTotal(spec), Is.EqualTo(7));
            var next = CombatCommandComposition.Project(batch);
            var open = NexusReinforcementTestData.Input(spec);
            next = open.WithWorld(next.State, next.Characters, next.Main, next.SoulCounts, next.Souls, next.EnvironmentBindings);
            next = EnemyControllerPhaseTests.Input(next, CombatControllerPhase.Enemy, completed: successTurn + 1);
            Assert.That(NexusReinforcementCalculator.Candidates(next).Count, Is.GreaterThan(2));
            Assert.That(NexusReinforcementCalculator.TryCalculate(next, 1), Is.Null);
            next = EnemyControllerPhaseTests.Input(next, CombatControllerPhase.Enemy, completed: successTurn + 2);
            Assert.That(NexusReinforcementCalculator.TryCalculate(next, 1).Characters.Created.Count, Is.EqualTo(2));
        }

        [TestCase(3, 2, 1)]
        [TestCase(3, 3, 0)]
        [TestCase(8, 6, 2)]
        [TestCase(8, 7, 1)]
        [TestCase(8, 8, 0)]
        public void Batch_RespectsConfiguredCapacity(int limit, int living, int expected)
        {
            var input = NexusReinforcementTestData.Input(NexusReinforcementTestData.Pool(limit: limit), living);
            var result = NexusReinforcementCalculator.TryCalculate(input, living);
            Assert.That(result?.Characters.Created.Count ?? 0, Is.EqualTo(expected));
        }

        [TestCase(0, 2, 2)]
        [TestCase(3, 2, 2)]
        [TestCase(4, 2, 1)]
        [TestCase(5, 2, 0)]
        [TestCase(6, 2, 0)]
        [TestCase(2, 4, 3)]
        public void Batch_RespectsDefaultCapacity(int living, int batch, int expected)
        {
            var input = NexusReinforcementTestData.Input(NexusReinforcementTestData.Pool(batch: batch), living);
            var result = NexusReinforcementCalculator.TryCalculate(input, living);
            Assert.That(result?.Characters.Created.Count ?? 0, Is.EqualTo(expected));
            if (result == null)
                return;

            var created = result.Characters.Created;
            Assert.That(created.Select(c => c.EntityId.Value), Is.EqualTo(Enumerable.Range(100, expected)));
            Assert.That(created.SelectMany(c => c.OccupiedCells).Distinct().Count(), Is.EqualTo(expected));
            Assert.That(created.All(c => c.ActionUse == UnitActionUse.Deferred), Is.True);
            Assert.That(result.FinalState.Reinforcements.Records.Count, Is.EqualTo(expected));
            Assert.That(result.FinalState.Allocation.SummonRandomState, Is.EqualTo(input.State.Allocation.SummonRandomState));
            Assert.That(result.FinalState.EnvironmentProgress, Is.SameAs(input.State.EnvironmentProgress));
            Assert.That(result.FinalState.ItemRandomState, Is.EqualTo(input.State.ItemRandomState));
        }

        [Test]
        public void WithoutReplacement_ConsumesOnlySelectedIndividuals()
        {
            var spec = NexusReinforcementTestData.Pool();
            var input = NexusReinforcementTestData.Input(spec);
            var keys = new List<EntityKey>();
            int[] remaining = { 3, 2 };
            uint random = input.State.Reinforcements.RandomState;
            for (int turn = 0; turn < 3; turn++)
            {
                input = EnemyControllerPhaseTests.Input(input, CombatControllerPhase.Enemy, completed: turn + 1);
                var cells = NexusReinforcementCalculator.Candidates(input).ToList();
                var step = NexusReinforcementCalculator.TryCalculate(input, 0);
                var creations = step.Events.OfType<CombatReinforcementEvent>().ToArray();
                foreach (var creation in creations)
                {
                    int poolIndex = EnvironmentRandom.Choose(ref random, remaining.Sum()) < remaining[0] ? 0 : 1;
                    remaining[poolIndex]--;
                    int cellIndex = EnvironmentRandom.Choose(ref random, cells.Count);
                    Assert.That(creation.Record.PoolEntryIndex, Is.EqualTo(poolIndex));
                    Assert.That(creation.Created.Anchor, Is.EqualTo(cells[cellIndex]));
                    cells.RemoveAt(cellIndex);
                    keys.Add(creation.Created.CharacterKey);
                }
                Assert.That(step.FinalState.Reinforcements.RandomState, Is.EqualTo(random));
                input = CombatCommandComposition.Project(step);
            }
            Assert.That(keys.Count(k => k == EntityKey.Unit_Demon), Is.EqualTo(3));
            Assert.That(keys.Count(k => k == EntityKey.Unit_Cowboy), Is.EqualTo(2));
            Assert.That(input.State.Reinforcements.RemainingTotal(spec), Is.Zero);
            Assert.That(NexusReinforcementCalculator.TryCalculate(input, 0), Is.Null);
        }

        [Test]
        public void SameInput_ProducesSameBatchAndFinalRandomState()
        {
            var input = NexusReinforcementTestData.Input(NexusReinforcementTestData.Pool());
            var first = NexusReinforcementCalculator.TryCalculate(input, 0);
            var repeated = NexusReinforcementCalculator.TryCalculate(input, 0);
            Assert.That(repeated.FinalState.SameValues(first.FinalState), Is.True);
            Assert.That(repeated.Characters.Created.Zip(first.Characters.Created, (a, b) => a.SameValues(b)), Is.All.True);
            Assert.That(input.State.Reinforcements.Records, Is.Empty);
            Assert.That(input.State.Reinforcements.RandomState, Is.EqualTo(17u));
        }

        [Test]
        public void NoSpawn_PreservesAllProgress()
        {
            var spec = NexusReinforcementTestData.Pool();
            var input = NexusReinforcementTestData.Input(spec);
            var area = NexusReinforcementCalculator.Candidates(input);
            var blocked = NexusReinforcementTestData.Input(spec, blocked: area.ToArray());
            Assert.That(NexusReinforcementCalculator.TryCalculate(blocked, 0), Is.Null);
            Assert.That(NexusReinforcementCalculator.TryCalculate(NexusReinforcementTestData.Input(NexusReinforcementTestData.Pool(0, 0)), 0), Is.Null);
            Assert.That(NexusReinforcementCalculator.TryCalculate(NexusReinforcementTestData.Input(null), 0), Is.Null);
            Assert.That(blocked.State.Reinforcements.Records, Is.Empty);
            Assert.That(blocked.State.Reinforcements.RandomState, Is.EqualTo(17u));
            Assert.That(blocked.State.Allocation.NextEntityId, Is.EqualTo(100));
        }

        [TestCase(1)]
        [TestCase(3)]
        public void OnlyOneCandidate_UsesOneCellAndPreservesSingletonDraws(int supply)
        {
            var spec = NexusReinforcementTestData.Pool(supply, 0);
            var cells = NexusReinforcementCalculator.Candidates(NexusReinforcementTestData.Input(spec));
            var input = NexusReinforcementTestData.Input(spec, blocked: cells.Skip(1).ToArray());
            var result = NexusReinforcementCalculator.TryCalculate(input, 0);
            Assert.That(result.Characters.Created.Single().Anchor, Is.EqualTo(cells[0]));
            Assert.That(result.FinalState.Reinforcements.RandomState == input.State.Reinforcements.RandomState, Is.EqualTo(supply == 1));
            Assert.That(result.FinalState.Reinforcements.RemainingTotal(spec), Is.EqualTo(supply - 1));
        }

        [Test]
        public void Creation_UsesPinnedStageLevelAndPoolProfile()
        {
            var spec = NexusReinforcementTestData.Pool(0, 1);
            var input = NexusReinforcementTestData.Input(spec);
            var actor = NexusReinforcementCalculator.TryCalculate(input, 0).Characters.Created.Single();
            Assert.That(actor.CharacterKey, Is.EqualTo(EntityKey.Unit_Cowboy));
            Assert.That(actor.OwnedId, Is.Null);
            Assert.That(actor.Experience.EnemyLevel, Is.EqualTo(3));
            Assert.That(actor.Health.MaxHealth, Is.EqualTo(16));
            Assert.That(actor.Stats.Category.Key, Is.EqualTo(CategoryKey.Heavy));
            Assert.That(actor.Stats.TraitStatuses.ToArray().Single().Key, Is.EqualTo(TraitKey.Focus));
        }

        [Test]
        public void AllNewActors_ParticipateInPresenceReconciliation()
        {
            var input = NexusReinforcementTestData.Input(NexusReinforcementTestData.Pool(), living: 1, presence: true);
            var result = NexusReinforcementCalculator.TryCalculate(input, 1);
            Assert.That(result.Events.Take(2), Is.All.TypeOf<CombatReinforcementEvent>());
            Assert.That(result.Characters.Created, Has.Count.EqualTo(2));
            Assert.That(result.Characters.Created.Select(c => c.StatusEffects.Entries.Any(s => s.Key == StatusEffectKey.Taunted)), Is.All.True);
            Assert.That(result.Events.OfType<CombatReinforcementEvent>().Select(e =>
                e.Created.StatusEffects.Entries.Any(s => s.Key == StatusEffectKey.Taunted)), Is.All.False);
            var existing = result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(40)).Final;
            Assert.That(existing.StatusEffects.Entries.Any(s => s.Key == StatusEffectKey.Taunted), Is.True);
            Assert.That(existing.TraitEffects.Shields.Single().RemainingBudget, Is.EqualTo(3));
        }

        [Test]
        public void Evidence_RequiresTheTypedCreationFact()
        {
            var input = NexusReinforcementTestData.Input(NexusReinforcementTestData.Pool(1, 0));
            var evt = NexusReinforcementCalculator.TryCalculate(input, 0).Events.OfType<CombatReinforcementEvent>().Single();
            Assert.That(CombatChangeEvidenceReader.ReadEventShapes(new[] { evt }).Single().AffectedEntityIds, Is.EqualTo(new[] { evt.Created.EntityId }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CombatTurnEvent(CombatEventKind.Reinforced, EntityId.Empty, input.State.Turn, input.State.Turn));
        }
    }
}
