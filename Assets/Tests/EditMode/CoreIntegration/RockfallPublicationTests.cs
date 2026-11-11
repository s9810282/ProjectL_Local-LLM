using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class RockfallPublicationTests
    {
        [TestCase(StageGimmickKey.Rockfall)]
        [TestCase(StageGimmickKey.Haboob)]
        public void BufferRejectsMissingDueGimmick(StageGimmickKey missing)
        {
            var source = new HaboobPublicationTests.Source(new StageGimmickSpec[] {
                new RockfallGimmickSpec(1), new HaboobGimmickSpec() });
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, source);
            var input = HaboobPublicationTests.EndInput(battle);
            var valid = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var omitted = input.State.Gimmicks.Entries.Single(g => g.Key == missing);
            CombatEvent skipped = missing == StageGimmickKey.Rockfall
                ? valid.Events.OfType<CombatRockfallEvent>().Single() : valid.Events.OfType<CombatHaboobEvent>().Single();
            var forged = new CombatCommandTransition(input, true, "", valid.Characters, valid.Field,
                valid.FinalState.With(gimmicks: valid.FinalState.Gimmicks.Replace(omitted)), valid.Outcome,
                events: valid.Events.Where(e => e != skipped).ToArray(), rewardSettlement: valid.RewardSettlement);
            AssertRejectedThenApplyValid(battle, forged, valid);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BufferRejectsMissingSurvivorBind(bool keepGrantEvent)
        {
            using var battle = Create();
            EndPlayer(battle);
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var input = battle.Publication().EnemyStep();
            var valid = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            var target = valid.Characters.Characters.Single(c => c.Final.EntityId == battle.Actor.EntityId);
            Assert.That(target.Final.Health.CurrentHealth, Is.InRange(1, target.Expected.Health.CurrentHealth - 1));
            var grant = valid.Events.OfType<CombatTraitStateEvent>().Single(e => e.Final.EntityId == target.Final.EntityId &&
                e.Final.StatusEffects.Entries.OfType<ActionRestrictionStatus>().Any(s => s.Key == StatusEffectKey.Bind));
            var unbound = target.Final.WithStatusEffects(new StatusEffectSet(target.Final.StatusEffects.Entries
                .Where(s => s.Key != StatusEffectKey.Bind).ToArray()));
            var changes = valid.Characters;
            var forged = new CombatCommandTransition(input, true, "", new CharacterChanges(changes.Characters.Select(c =>
                c == target ? new CharacterStateChange(target.Expected, unbound) : c).ToArray(),
                changes.Souls, changes.Main, changes.Created, changes.Owned), valid.Field, valid.FinalState, valid.Outcome,
                events: valid.Events.Where(e => keepGrantEvent || e != grant).ToArray(), rewardSettlement: valid.RewardSettlement);
            AssertRejectedThenApplyValid(battle, forged, valid);
        }

        private static void AssertRejectedThenApplyValid(ConfiguredMovementBattle battle,
            CombatCommandTransition forged, CombatCommandTransition valid)
        {
            var before = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(forged));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            battle.Buffers.BattleWriteSink.Apply(valid);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ImpactPublicationRejectsMissingRecipientsAndRepeatedDamage(bool forged)
        {
            using var battle = Create();
            EndPlayer(battle);
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var input = battle.Publication().EnemyStep();
            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Events.OfType<CombatRockfallEvent>().Single().Step, Is.EqualTo(RockfallGimmickStep.Impact));
            if (forged)
            {
                var rock = result.Events.OfType<CombatRockfallEvent>().Single();
                var events = result.Events.Select(e => e == rock ? new CombatRockfallEvent(rock.Step, rock.Expected, rock.Final,
                    rock.RecipientIds.Skip(1).ToArray()) : e).ToArray();
                result = new CombatCommandTransition(input, true, "", result.Characters, result.Field, result.FinalState,
                    result.Outcome, events: events, rewardSettlement: result.RewardSettlement);
            }
            else battle.Buffers.BattleWriteSink.Apply(result);
            var stores = battle.CaptureStores();
            var health = battle.Actor.Health;
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(result));
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            Assert.That(battle.Actor.Health, Is.EqualTo(health));
        }

        [Test]
        public void WarningImpactAndBindPublishThroughRealTurnCompletion()
        {
            using var battle = Create();
            var before = battle.Actor.Health;
            EndPlayer(battle);
            var warning = (RockfallGimmickState)battle.Buffers.CombatStateReadSink.Current.Gimmicks.Get(new StageGimmickId(1));
            Assert.That(warning.Phase, Is.EqualTo(RockfallGimmickPhase.Warning));
            Assert.That(warning.WarnedCells, Does.Contain(battle.Actor.Anchor.Value));
            uint random = battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.RandomState;
            battle.AdvanceToPlayerOrDay();
            Assert.That(battle.Actor.Health.CurrentHealth, Is.EqualTo(before.CurrentHealth - before.MaxHealth / 5));
            Assert.That(battle.Actor.StatusEffects.Entries.OfType<ActionRestrictionStatus>().Single().RemainingAffectedSideTurnEnds, Is.EqualTo(1));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.RandomState, Is.EqualTo(random));
            var read = StatusEffectReadModel.Capture(battle.Actor).Single(s => s.Key == StatusEffectKey.Bind);
            Assert.That(read.Sources.Single().Origin.Value.Kind, Is.EqualTo(EffectOwnerKind.StageGimmick));
            var hp = battle.Actor.Health;
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.False);
            Assert.That(battle.Actor.Health, Is.EqualTo(hp));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RepeatedOrForgedPublicationIsAtomic(bool forged)
        {
            using var battle = Create();
            var b = battle.Buffers;
            int number = b.CombatViewWriteSink.Submit(CombatViewCommandData.EndPlayerTurn(b.CombatStateReadSink.BattleInstanceId,
                b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
            var input = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.EndPlayerTurn,
                default, default, expectedBattleInstanceId: b.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            if (forged)
                result = result.WithFinalState(result.FinalState.With(gimmicks: input.State.Gimmicks));
            else b.BattleWriteSink.Apply(result);
            var stores = battle.CaptureStores();
            var state = b.CombatStateReadSink.Current.CommandState;
            if (forged) Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(result));
            else Assert.DoesNotThrow(() => b.BattleWriteSink.Apply(result));
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            Assert.That(b.CombatStateReadSink.Current.CommandState.SameValues(state), Is.True);
        }

        [Test]
        public void TerminalImpactRemovesPendingWarningAndDoesNotRunAgain()
        {
            using var battle = Create(new RockfallGimmickSpec(1, 1, 64, 64, 100));
            EndPlayer(battle);
            battle.AdvanceToPlayerOrDay();
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.Gimmicks.Entries, Is.Empty);
            var sequence = battle.Buffers.CombatStateReadSink.LatestResultSequence;
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.False);
            Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
        }

        internal static ConfiguredMovementBattle Create(RockfallGimmickSpec spec = null) =>
            new(ConfiguredMovementBattle.Config, new Source(spec ?? new RockfallGimmickSpec(1, 1, 64, 64)));

        internal static void EndPlayer(ConfiguredMovementBattle battle) => battle.Submit(
            CombatViewCommandData.EndPlayerTurn(battle.Buffers.CombatStateReadSink.BattleInstanceId,
                battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));

        internal sealed class Source : IBattleInitializationSource
        {
            private readonly RockfallGimmickSpec _spec;
            internal Source(RockfallGimmickSpec spec) { _spec = spec; }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var d = new ConfiguredMovementBattle.EncounterSource().Read(database, chapterKey, stageKey);
                var s = d.Stage;
                var stage = new StageConfig(s.MapWidth, s.MapHeight, s.ReserveCapacity, s.PlayerZone, s.TimelineSeed,
                    s.InitialEnemyPlacements.ToArray(), s.RailCells.ToArray(), new StageGimmickSpec[] { _spec }, s.InitialEnemyStructurePlacements.ToArray());
                return new BattleInitialization(stage, d.MainAnchor, d.MainRotation, d.Units, d.Structures, d.Categories,
                    d.AttackProfiles, d.Environment, d.EnvironmentBindings, d.ObstaclePlacements, d.WallBlocking,
                    d.ObjectiveKind, d.ItemDefinitions, d.Progression, d.InitialRandomTornadoPlacements, d.ClearRewards, d.TraitGrowth);
            }
        }
    }
}
