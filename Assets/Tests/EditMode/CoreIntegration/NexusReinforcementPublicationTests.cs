using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class NexusReinforcementPublicationTests
    {
        [Test]
        public void BufferRange_IsGeometricAndReadOnly()
        {
            var snapshot = ReadPrepared(enemyCount: 1);
            Assert.That(snapshot.CommandState.Turn.StagePhase, Is.EqualTo(StagePhase.Deployment));
            var area = snapshot.ReadNexusReinforcementArea();
            Assert.That(area, Is.Not.Empty);
            var capped = ReadPrepared(enemyCount: NexusReinforcementSpec.DefaultFieldEnemyLimit);
            Assert.That(capped.ReadNexusReinforcementArea(), Is.EqualTo(area));

            var hiddenCell = area.First();
            var mine = new MineSourceFact(1, hiddenCell, 1, 5, true, new[] { Owner.Enemy });
            var original = snapshot.CommandState.Reinforcements;
            var exhausted = new NexusReinforcementState(original.NexusId, 91,
                new[] { new NexusReinforcementRecord(new EntityId(99), 0) });
            var hiddenEnvironment = new MovementEnvironmentFacts(new MovementTileFact[] { mine });
            var withChanges = snapshot.CommandState.With(reinforcements: exhausted,
                environment: hiddenEnvironment, environmentProgress: EnvironmentProgressFacts.Initial(hiddenEnvironment));
            var derived = new CombatStateData(snapshot, withChanges,
                new EnvironmentSemanticBindings(Array.Empty<long>(), new[] { mine.Id }, Array.Empty<EntityId>()));

            Assert.That(exhausted.RemainingTotal(snapshot.NexusReinforcements), Is.Zero);
            Assert.That(derived.ReadNexusReinforcementArea(), Does.Contain(hiddenCell));
            Assert.That(derived.ReadNexusReinforcementArea(), Is.EqualTo(area));
            Assert.That(derived.CommandState.Reinforcements, Is.SameAs(exhausted));
            Assert.That(exhausted.RandomState, Is.EqualTo(91u));
            Assert.That(exhausted.Records.Select(r => r.EntityId), Is.EqualTo(new[] { new EntityId(99) }));
            Assert.That(snapshot.CommandState.Reinforcements, Is.SameAs(original));
            Assert.That(original.Records, Is.Empty);
        }

        [Test]
        public void BufferRange_DisappearsWhenSourceOrBattleIsUnavailable()
        {
            var snapshot = ReadPrepared(enemyCount: 1);
            Assert.That(ReadPrepared(enemyCount: 1, enabled: false).ReadNexusReinforcementArea(), Is.Empty);
            Assert.That(new CombatStateData(snapshot, snapshot.CommandState.With(phase: CombatFlowPhase.Finished))
                .ReadNexusReinforcementArea(), Is.Empty);
            Assert.That(new CombatStateData(snapshot, snapshot.CommandState.With(phase: CombatFlowPhase.None))
                .ReadNexusReinforcementArea(), Is.Empty);

            var nexus = snapshot.Structures.Single();
            var destroyed = new StructureCombatFacts(nexus.EntityId, nexus.Definition,
                new HealthStateSnapshot(0, nexus.Health.MaxHealth), nexus.Anchor, nexus.Rotation, nexus.OccupiedCells);
            var after = new CombatStateData(snapshot, snapshot.CommandState.With(structures: new[] { destroyed }));
            Assert.That(after.ReadNexusReinforcementArea(), Is.Empty);
        }


        [Test]
        public void ValidNexusCreation_PublishesAllStoresTogether()
        {
            using var battle = CreateBattle(1);
            var parts = Publication(battle);
            var input = ToEnemyStart(battle, parts);
            var proposal = CreationProposal(input, "");
            var buffers = battle.Buffers;
            int sequence = buffers.CombatStateReadSink.LatestResultSequence;
            long revision = buffers.CombatStateReadSink.QueryRevision;
            int gold = buffers.CurrencyReadSink.Amount;
            var created = proposal.Characters.Created.Single();

            Assert.That(parts.Publisher.Publish(proposal), Is.True);

            Assert.That(buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(buffers.CombatStateReadSink.QueryRevision, Is.EqualTo(revision + 1));
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(gold));
            Assert.That(buffers.CharacterReadSink.Characters.Single(c => c.EntityId == created.EntityId).ToFacts().SameValues(created), Is.True);
            Assert.That(buffers.CombatStateReadSink.Current.CommandState.SameValues(proposal.FinalState), Is.True);
            Assert.That(buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.OfType<CombatReinforcementEvent>().Single().Created, Is.SameAs(created));
            var once = new CoreIntegrationSnapshot(buffers);
            Assert.Throws<InvalidOperationException>(() => parts.Publisher.Publish(proposal));
            once.AssertUnchanged(buffers);
        }

        [TestCase("missing record")]
        [TestCase("cross-source")]
        [TestCase("overspent")]
        [TestCase("old origin")]
        [TestCase("wrong source")]
        [TestCase("wrong binding")]
        [TestCase("wrong type")]
        [TestCase("wrong index")]
        [TestCase("wrong phase")]
        [TestCase("missing event")]
        [TestCase("duplicate event")]
        [TestCase("event index")]
        [TestCase("wrong origin")]
        [TestCase("missing origin")]
        [TestCase("non-deferred")]
        [TestCase("allocation gap")]
        [TestCase("rng only")]
        [TestCase("structure summoner")]
        public void InvalidCreation_RejectsEveryStore(string fault)
        {
            using var battle = CreateBattle(1);
            var parts = Publication(battle);
            var input = fault == "wrong phase" ? StartInput(parts) : ToEnemyStart(battle, parts);
            var proposal = CreationProposal(input, fault);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            var original = battle.Buffers.CombatStateReadSink.Current.CommandState;

            Assert.Throws<InvalidOperationException>(() => parts.Publisher.Publish(proposal));

            before.AssertUnchanged(battle.Buffers);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState, Is.SameAs(original));
        }

        [Test]
        public void CharacterSummonContracts_RemainUnchanged()
        {
            using var battle = CreateBattle(1);
            var parts = Publication(battle);
            var input = ToEnemyStart(battle, parts);
            var proposal = CreationProposal(input, "summon");
            Assert.That(parts.Publisher.Publish(proposal), Is.True);
            var final = battle.Buffers.CombatStateReadSink.Current.CommandState;
            Assert.That(final.Reinforcements, Is.SameAs(input.State.Reinforcements));
            Assert.That(final.Turn.Summons.Single().SummonedId, Is.EqualTo(proposal.Characters.Created.Single().EntityId));
            Assert.That(final.Turn.EnemyOrigins, Is.EqualTo(input.State.Turn.EnemyOrigins));
        }

        [Test]
        public void PriorRecordCannotBeRewrittenOnAnotherStart()
        {
            using var battle = CreateBattle(1);
            var parts = Publication(battle);
            var first = CreationProposal(ToEnemyStart(battle, parts), "");
            Assert.That(parts.Publisher.Publish(first), Is.True);
            battle.AdvanceToPlayerOrDay();
            var input = ToEnemyStart(battle, parts);
            var proposal = CreationProposal(input, "rng only");
            var progress = input.State.Reinforcements;
            var rewritten = new NexusReinforcementState(progress.NexusId, progress.RandomState,
                new[] { new NexusReinforcementRecord(progress.Records.Single().EntityId, 1) });
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => parts.Publisher.Publish(
                proposal.WithFinalState(proposal.FinalState.With(reinforcements: rewritten))));
            before.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void CreationFollowedByDeath_PublishesOneFinalActorWithoutRefund()
        {
            using var battle = CreateBattle(1);
            var parts = Publication(battle);
            var input = ToEnemyStart(battle, parts);
            var creation = CreationProposal(input, "");
            var created = creation.Characters.Created.Single();
            var dead = created.WithPlacement(CharacterRuntimeArea.Excluded, null, null, null, Array.Empty<CellCoord>(),
                health: new HealthStateSnapshot(0, created.Health.MaxHealth));
            var proposal = new CombatCommandTransition(input, true, "",
                new CharacterChanges(creation.Characters.Characters, Array.Empty<SoulCountChange>(), created: new[] { dead }),
                creation.Field, creation.FinalState, creation.Outcome,
                events: creation.Events.Append(new CombatCharacterEvent(CombatEventKind.Died, created, dead)).ToArray());

            Assert.That(parts.Publisher.Publish(proposal), Is.True);
            Assert.That(battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == dead.EntityId).ToFacts().SameValues(dead), Is.True);
            var snapshot = battle.Buffers.CombatStateReadSink.Current;
            Assert.That(snapshot.CommandState.Reinforcements.RemainingTotal(snapshot.NexusReinforcements), Is.Zero);
            Assert.That(snapshot.CommandState.Reinforcements.Records, Has.Count.EqualTo(1));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.OfType<CombatReinforcementEvent>().Single().Created.Health.IsDead, Is.False);
        }


        [TestCase(false)]
        [TestCase(true)]
        public void DisabledAndBossStages_DoNotCreateReinforcements(bool boss)
        {
            var config = ConfiguredMovementBattle.Config;
            var database = new CoreIntegrationTestData(config);
            if (boss)
            {
                database.SetUnit(StationaryEnemy(EntityKey.Unit_Stage1Boss, CategoryKey.Boss));
                database.SetStage(new StageRootData(1101, 1, StageMapRole.Boss,
                    new CombatStageDetail(17, "field", 1, 0, 1,
                        new[] { new EntityPlacement(EntityKey.Unit_Stage1Boss, new CellCoord(3, 5), Rotation4.R0, isVictoryTarget: true) },
                        Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.None, CellCoord.Zero, Rotation4.R0,
                        CellCoord.Zero, Rotation4.R0, null, objectiveKind: BattleObjectiveKind.DesignatedBossDefeated)));
            }
            using var battle = new ConfiguredMovementBattle(config, database: database);
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.OfType<CombatReinforcementEvent>(), Is.Empty);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.ReadNexusReinforcementArea(), Is.Empty);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Reinforcements.IsEnabled, Is.False);
        }

        private static UnitData StationaryEnemy(EntityKey key, CategoryKey category) =>
            new(key, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly, 1, 1, 1, 0, 0, category,
                1, 1, Array.Empty<UnitTraitData>(), targetingMethodKey: 1,
                behavior: new UnitBehaviorSpec(false, true, false), killReward: new UnitKillRewardSpec(11, 13));

        private static ConfiguredMovementBattle.PublicationServices Publication(ConfiguredMovementBattle battle)
        {
            var definition = BattleDefinitionAdapter.Read(battle.Database, 1, 1101, battle.Buffers.CharacterReadSink.Owned);
            return new ConfiguredMovementBattle.PublicationServices(battle,
                definition.WithAttackProfiles(BattleDefinitionAdapter.ReadAttackProfiles(battle.Database, definition.Units)),
                battle.Core.ChapterProgression);
        }

        private static CombatCommandInput StartInput(ConfiguredMovementBattle.PublicationServices parts) =>
            parts.Assembler.Assemble(new CombatActionRequest(0, CombatCommandKind.StartControllerTurn, EntityId.Empty,
                default, processingKind: CombatProcessingKind.Advance));

        private static CombatCommandInput ToEnemyStart(ConfiguredMovementBattle battle, ConfiguredMovementBattle.PublicationServices parts)
        {
            battle.Submit(battle.EndPlayerTurn());
            return StartInput(parts);
        }

        private static CombatCommandTransition CreationProposal(CombatCommandInput input, string fault)
        {
            var before = input.State;
            var turn = before.Turn;
            var source = before.Reinforcements;
            var nexus = source.NexusId;
            bool summon = fault is "summon" or "structure summoner";
            int count = fault == "rng only" ? 0 : fault == "overspent" ? 2 : 1;
            long nextId = before.Allocation.NextEntityId;
            var template = input.Characters.First(c => c.Owner == Owner.Enemy);
            var created = Enumerable.Range(0, count).Select(i =>
            {
                var cell = new CellCoord(2 + i, 4);
                return new CharacterCombatFacts(new EntityId(checked((int)nextId + i + (fault == "allocation gap" ? 1 : 0))),
                    null, fault == "wrong type" ? EntityKey.Unit_Cowboy : EntityKey.Unit_Demon, Owner.Enemy, null,
                    CharacterRuntimeArea.Field, template.Health, template.Stats, null, cell, Rotation4.R0, new[] { cell },
                    fault == "non-deferred" ? UnitActionUse.Available : UnitActionUse.Deferred, experience: template.Experience);
            }).ToArray();
            var records = created.Select(c => new NexusReinforcementRecord(c.EntityId, fault == "wrong index" ? 2 : 0)).ToArray();
            var origins = turn.EnemyOrigins.ToList();
            if (fault == "old origin")
                origins[0] = new CombatEnemyOrigin(origins[0].EntityId, new CellCoord(1, 4), Rotation4.R0);
            if (!summon && fault is not ("missing origin" or "cross-source"))
                origins.AddRange(created.Select(c => new CombatEnemyOrigin(c.EntityId,
                    fault == "wrong origin" ? new CellCoord(6, 4) : c.Anchor.Value, c.Rotation.Value)));
            var summons = turn.Summons.Concat(summon || fault == "cross-source"
                ? created.Select(c => new CombatSummonRecord(c.EntityId,
                    fault == "structure summoner" ? nexus : template.EntityId, TraitKey.Necromancy))
                : Array.Empty<CombatSummonRecord>()).ToArray();
            bool starts = fault != "wrong phase";
            var startTurn = starts ? new CombatTurnFacts(StagePhase.Combat,
                CombatControllerPhase.Enemy, turn.ControllerTurnSequence + 1, turn.CompletedPlayerTurnCount, turn.EnemyOrigins, turn.Summons, turn.CompletedControllerTurnCount) : turn;
            var finalTurn = new CombatTurnFacts(StagePhase.Combat,
                startTurn.ControllerPhase, startTurn.ControllerTurnSequence, startTurn.CompletedPlayerTurnCount, origins, summons, startTurn.CompletedControllerTurnCount);
            var finalRecords = summon || fault == "missing record" ? source.Records : source.Records.Concat(records).ToArray();
            var progress = summon ? source : new NexusReinforcementState(
                fault == "wrong binding" ? new EntityId(999) : nexus, source.RandomState + 1, finalRecords);
            var events = new List<CombatEvent>();
            if (starts) events.Add(new CombatTurnEvent(CombatEventKind.ControllerTurnStarted, EntityId.Empty, turn, startTurn));
            if (summon)
                events.Add(new CombatSummonEvent(summons.Last(), created[0]));
            else if (fault != "missing event")
                for (int i = 0; i < created.Length; i++)
                    events.Add(new CombatReinforcementEvent(fault == "wrong source" ? template.EntityId : nexus,
                        fault == "event index" ? new NexusReinforcementRecord(created[i].EntityId, 1) : records[i], created[i]));
            if (fault == "duplicate event")
                events.Add(events.Last());
            var changes = input.Characters.Where(c => starts && c.Owner == Owner.Enemy && c.Area == CharacterRuntimeArea.Field)
                .Select(c => new CharacterStateChange(c, c.With(actionUse: UnitActionUse.Available))).ToArray();
            var final = before.With(turn: finalTurn, reinforcements: progress,
                allocation: new CombatAllocationFacts(nextId + count, before.Allocation.SummonRandomState));
            return new CombatCommandTransition(input, true, "", new CharacterChanges(changes, Array.Empty<SoulCountChange>(), created: created),
                new SoulFieldChange(input.Souls, input.Souls), final, new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None),
                events: events);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PresenceAffectedReinforcement_PublishesInitialEventAndFinalEffects(bool taunt)
        {
            using var battle = PresenceBattle(taunt);
            var parts = Publication(battle);
            var input = ToEnemyStart(battle, parts);
            var proposal = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            var evt = proposal.Events.OfType<CombatReinforcementEvent>().Single();
            var final = proposal.Characters.Created.Single();
            Assert.That(evt.Created.StatusEffects.Entries, Is.Empty);
            Assert.That(evt.Created.TraitEffects.SameValues(CharacterTraitEffects.Empty), Is.True);
            Assert.That(final.StatusEffects.Entries, Is.Not.Empty);
            if (taunt)
                Assert.That(final.StatusEffects.Entries.Any(s => s.Key == StatusEffectKey.Taunted), Is.True);
            else
                Assert.That(final.TraitEffects.Shields.Single().RemainingBudget, Is.EqualTo(3));
            Assert.That(parts.Publisher.Publish(proposal), Is.True);
            Assert.That(battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == final.EntityId).ToFacts().SameValues(final), Is.True);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.OfType<CombatReinforcementEvent>().Single().Created, Is.SameAs(evt.Created));
        }

        [Test]
        public void CreationEvent_WithPreexistingEffects_IsRejectedAtomically()
        {
            using var battle = PresenceBattle(true);
            var parts = Publication(battle);
            var input = ToEnemyStart(battle, parts);
            var proposal = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            var initial = proposal.Events.OfType<CombatReinforcementEvent>().Single();
            var final = proposal.Characters.Created.Single();
            var events = proposal.Events.Select(e => ReferenceEquals(e, initial) ?
                new CombatReinforcementEvent(initial.NexusId, initial.Record, final) : e).ToArray();
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => parts.Publisher.Publish(WithCreation(proposal, final, events)));
            before.AssertUnchanged(battle.Buffers);
        }

        [TestCase("missing")]
        [TestCase("wrong expected")]
        [TestCase("before creation")]
        [TestCase("wrong final")]
        [TestCase("level")]
        public void CreationPositionChain_RejectsUnexplainedChanges(string fault)
        {
            using var battle = CreateBattle(1);
            var parts = Publication(battle);
            var proposal = CreationProposal(ToEnemyStart(battle, parts), "");
            var initial = proposal.Characters.Created.Single();
            var cell = new CellCoord(6, 4);
            var final = initial.WithPlacement(CharacterRuntimeArea.Field, null, cell, initial.Rotation, new[] { cell });
            var events = proposal.Events.ToList();
            if (fault != "missing")
            {
                var expected = fault == "wrong expected"
                    ? initial.WithPlacement(CharacterRuntimeArea.Field, null, new CellCoord(5, 4), initial.Rotation, new[] { new CellCoord(5, 4) })
                    : initial;
                var movement = new CombatCharacterEvent(CombatEventKind.Moved, expected, final);
                if (fault == "before creation")
                    events.Insert(1, movement);
                else
                    events.Add(movement);
            }
            if (fault == "wrong final")
                final = final.WithPlacement(CharacterRuntimeArea.Field, null, new CellCoord(7, 4), final.Rotation, new[] { new CellCoord(7, 4) });
            if (fault == "level")
                final = final.With(experience: CharacterExperienceFacts.ForEnemy(7));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => parts.Publisher.Publish(WithCreation(proposal, final, events)));
            before.AssertUnchanged(battle.Buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CreationPositionChain_AllowsRecordedMovementAndDeath(bool dies)
        {
            using var battle = CreateBattle(1);
            var parts = Publication(battle);
            var proposal = CreationProposal(ToEnemyStart(battle, parts), "");
            var initial = proposal.Characters.Created.Single();
            var cell = new CellCoord(6, 4);
            var moved = initial.WithPlacement(CharacterRuntimeArea.Field, null, cell, initial.Rotation, new[] { cell });
            var events = proposal.Events.Append(new CombatCharacterEvent(CombatEventKind.Moved, initial, moved)).ToList();
            var final = moved;
            if (dies)
            {
                final = moved.WithPlacement(CharacterRuntimeArea.Excluded, null, null, null, Array.Empty<CellCoord>(),
                    health: new HealthStateSnapshot(0, moved.Health.MaxHealth));
                events.Add(new CombatCharacterEvent(CombatEventKind.Died, moved, final));
            }
            Assert.That(parts.Publisher.Publish(WithCreation(proposal, final, events)), Is.True);
            var installed = battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == final.EntityId).ToFacts();
            Assert.That(installed.SameValues(final), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.EnemyOrigins
                .Single(o => o.EntityId == final.EntityId).Anchor, Is.EqualTo(initial.Anchor));
        }

        private static CombatCommandTransition WithCreation(CombatCommandTransition source, CharacterCombatFacts final,
            IReadOnlyList<CombatEvent> events) =>
            new(source.Input, source.Accepted, source.Reason,
                new CharacterChanges(source.Characters.Characters, source.Characters.Souls, source.Characters.Main,
                    new[] { final }, source.Characters.Owned), source.Field, source.FinalState, source.Outcome,
                events: events, rewardSettlement: source.RewardSettlement, rewardChange: source.RewardChange);

        private static ConfiguredMovementBattle PresenceBattle(bool taunt)
        {
            var config = ConfiguredMovementBattle.Config;
            var database = new CoreIntegrationTestData(config);
            var key = taunt ? EntityKey.Unit_Cowboy : EntityKey.Unit_Demon;
            database.SetUnit(new UnitData(key, UnitRole.Regular, UnitOwnerEligibility.Both,
                1, 1, 10, 0, 2, CategoryKey.Rider, 5, 1,
                new UnitTraitData[] { taunt ? new TauntTraitData(7) : new SoloActionTraitData(3, 7, 4, 1) },
                targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));
            var spec = new NexusReinforcementSpec(new[] { new NexusReinforcementEntry(EntityKey.Unit_Demon, 1) }, 1);
            database.SetStage(new StageRootData(1101, 1, StageMapRole.Regular,
                new CombatStageDetail(17, "field", 1, 0, 1, Array.Empty<EntityPlacement>(),
                    Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters,
                    CellCoord.Zero, Rotation4.R0, new CellCoord(3, 5), Rotation4.R0, null, nexusReinforcements: spec)));
            return new ConfiguredMovementBattle(config, database: database);
        }

        private static CombatStateData ReadPrepared(int enemyCount, bool enabled = true)
        {
            using var battle = CreateBattle(enemyCount, enabled, enterCombat: false);
            Assert.That(battle.Buffers.CharacterReadSink.Characters.Count(c => c.ToFacts().Owner == Owner.Enemy),
                Is.EqualTo(enemyCount));
            return battle.Buffers.CombatStateReadSink.Current;
        }

        private static ConfiguredMovementBattle CreateBattle(int enemyCount, bool enabled = true, bool enterCombat = true,
            int supply = 1, int padding = 2)
        {
            var config = ConfiguredMovementBattle.Config;
            var database = new CoreIntegrationTestData(config);
            var spec = enabled ? new NexusReinforcementSpec(
                new[] { new NexusReinforcementEntry(EntityKey.Unit_Demon, supply) }, 1, rangePadding: padding) : null;
            var enemies = Enumerable.Range(0, enemyCount)
                .Select(x => new EntityPlacement(EntityKey.Unit_Demon, new CellCoord(x, 4), Rotation4.R0)).ToArray();
            database.SetStage(new StageRootData(1101, 1, StageMapRole.Regular,
                new CombatStageDetail(17, "field", 1, 0, 1, enemies,
                    Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters,
                    CellCoord.Zero, Rotation4.R0, new CellCoord(3, 5), Rotation4.R0, null, nexusReinforcements: spec)));

            return new ConfiguredMovementBattle(config, database: database, enterCombat: enterCombat);
        }
    }
}
