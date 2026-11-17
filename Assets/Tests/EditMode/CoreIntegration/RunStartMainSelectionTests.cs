using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using TestDatabase = ProjectL.Tests.CoreIntegration.RunStartMainSelectionTestRuntime.TestDatabase;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class RunStartMainSelectionTests
    {
        [Test]
        public void ReadMainOptions_BeforeRun_ReturnsSortedMainsAndAuthoredDefault()
        {
            using var game = new RunStartMainSelectionTestRuntime();
            game.Database.SetUnit(TestDatabase.Main(EntityKey.Unit_Ballista, 20, 6, 3));
            var buffers = game.Buffers;
            long rosterRevision = buffers.CharacterReadSink.RosterRevision;
            int? startingHandSize = buffers.RunProgressReadSink.StartingHandSize;
            int gold = buffers.CurrencyReadSink.Amount;

            RunStartMainOptions options = game.Core.RunStart.ReadMainOptions(1);

            Assert.That(options.LoadoutKey, Is.EqualTo(1));
            Assert.That(options.DefaultMainCharacterKey, Is.EqualTo(EntityKey.Unit_Akimbo));
            Assert.That(options.CandidateKeys, Is.EqualTo(new[]
            {
                EntityKey.Unit_Akimbo, EntityKey.Unit_Sniper, EntityKey.Unit_Ballista
            }));
            Assert.That(buffers.RunProgressReadSink.RunId.IsEmpty, Is.True);
            Assert.That(buffers.RunProgressReadSink.StartingHandSize, Is.EqualTo(startingHandSize));
            Assert.That(buffers.CharacterReadSink.Owned, Is.Empty);
            Assert.That(buffers.CharacterReadSink.RosterRevision, Is.EqualTo(rosterRevision));
            Assert.That(buffers.RunDeckReadSink.TryGetCurrent(out _), Is.False);
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(gold));
        }

        [Test]
        public void ReadMainOptions_UsesDefaultRatherThanFirstCandidate()
        {
            using var game = new RunStartMainSelectionTestRuntime(new TestDatabase(EntityKey.Unit_Sniper));

            RunStartMainOptions options = game.Core.RunStart.ReadMainOptions(1);

            Assert.That(options.CandidateKeys, Is.EqualTo(new[] { EntityKey.Unit_Akimbo, EntityKey.Unit_Sniper }));
            Assert.That(options.DefaultMainCharacterKey, Is.EqualTo(EntityKey.Unit_Sniper));
        }

        [TestCase(EntityKey.Unit_ShotGun)]
        [TestCase(EntityKey.Unit_Shielder)]
        public void ReadMainOptions_RejectsMissingLoadoutAndInvalidDefault(EntityKey invalidDefault)
        {
            using var game = new RunStartMainSelectionTestRuntime(new TestDatabase(invalidDefault));

            Assert.Throws<KeyNotFoundException>(() => game.Core.RunStart.ReadMainOptions(999));
            var error = Assert.Throws<InvalidOperationException>(() => game.Core.RunStart.ReadMainOptions(1));
            Assert.That(error.Message, Does.Contain("1").And.Contain(invalidDefault.ToString()));
        }

        [Test]
        public void ReadMainOptions_RequiresInitializationAndBinding()
        {
            using var game = new RunStartMainSelectionTestRuntime(new TestDatabase(), initialize: false);

            Assert.Throws<InvalidOperationException>(() => game.Core.RunStart.ReadMainOptions(1));
            game.InitData();
            Assert.Throws<InvalidOperationException>(() => game.Core.RunStart.ReadMainOptions(1));
            game.BindServices();
            Assert.That(game.Core.RunStart.ReadMainOptions(1).DefaultMainCharacterKey,
                Is.EqualTo(EntityKey.Unit_Akimbo));
        }

        [TestCase(UnitRole.MainCharacter, 10)]
        [TestCase(UnitRole.Regular, 0)]
        public void ReadMainOptions_MalformedDefinitionThrows(UnitRole role, int health)
        {
            using var game = new RunStartMainSelectionTestRuntime();
            game.Database.SetUnit(new UnitData(EntityKey.Unit_Ballista, role, UnitOwnerEligibility.EnemyOnly,
                1, 1, health, 0, 1, CategoryKey.Main, 5, 1, null,
                targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));

            Assert.Throws<InvalidOperationException>(() => game.Core.RunStart.ReadMainOptions(1));
        }

        [Test]
        public void Options_DefensivelyCopyCandidateKeys()
        {
            var candidates = new List<EntityKey> { EntityKey.Unit_Akimbo, EntityKey.Unit_Sniper };
            var options = new RunStartMainOptions(1, EntityKey.Unit_Akimbo, candidates);

            candidates[0] = EntityKey.Unit_Ballista;
            candidates.Clear();
            if (options.CandidateKeys is IList<EntityKey> exposed)
                Assert.Throws<NotSupportedException>(() => exposed[0] = EntityKey.Unit_Ballista);

            Assert.That(options.CandidateKeys, Is.EqualTo(new[] { EntityKey.Unit_Akimbo, EntityKey.Unit_Sniper }));
            Assert.Throws<ArgumentNullException>(() => new RunStartMainOptions(1, EntityKey.Unit_Akimbo, null));
        }

        [TestCase(EntityKey.Unit_Akimbo, 30, 10, 5)]
        [TestCase(EntityKey.Unit_Sniper, 10, 5, 2)]
        public void StartNewRun_ExplicitMain_PreservesSharedLoadout(EntityKey selected, int health, int attack, int defense)
        {
            using var game = new RunStartMainSelectionTestRuntime();
            var start = game.Core.RunStart;
            Assert.That(start.ReadMainOptions(1).CandidateKeys, Does.Contain(selected));

            var result = start.StartNewRun(1, selected);

            AssertInstalledLoadout(game, result, selected, health, attack, defense);
            Assert.That(start.ReadMainOptions(1).DefaultMainCharacterKey, Is.EqualTo(EntityKey.Unit_Akimbo));
        }

        [TestCase(EntityKey.Unit_Akimbo, 30, 10, 5)]
        [TestCase(EntityKey.Unit_Sniper, 10, 5, 2)]
        public void StartNewRun_DefaultOverload_UsesAuthoredMain(EntityKey authoredMain, int health, int attack, int defense)
        {
            using var game = new RunStartMainSelectionTestRuntime(new TestDatabase(authoredMain));

            var result = game.Core.RunStart.StartNewRun(1);

            AssertInstalledLoadout(game, result, authoredMain, health, attack, defense);
        }

        [TestCase(EntityKey.Unit_Akimbo, false)]
        [TestCase(EntityKey.Unit_Akimbo, true)]
        [TestCase(EntityKey.Unit_Sniper, false)]
        [TestCase(EntityKey.Unit_Sniper, true)]
        public void StartNewRun_NonDefaultHandSize_PreservesAuthoredValue(EntityKey selected, bool useDefaultOverload)
        {
            const int authoredHandSize = 2;
            Assert.That(authoredHandSize, Is.Not.EqualTo(InitialDrawPlan.DefaultStartingHandSize));
            var database = new TestDatabase(selected);
            var loadout = database.Loadout;
            database.Loadout = new PlayerLoadoutData(loadout.LoadoutKey, loadout.MainCharacterKey,
                loadout.Entries.ToArray(), authoredHandSize, loadout.StartingRunCurrency,
                loadout.InitialDeckGrantIndices.ToArray());
            using var game = new RunStartMainSelectionTestRuntime(database);

            var result = useDefaultOverload
                ? game.Core.RunStart.StartNewRun(loadout.LoadoutKey)
                : game.Core.RunStart.StartNewRun(loadout.LoadoutKey, selected);

            Assert.That(result.Succeeded, Is.True, result.FailureReason.ToString());
            var main = result.Acquired.Single(c => c.Role == CharacterOwnershipRole.Main);
            Assert.That(main.CharacterKey, Is.EqualTo(selected));
            Assert.That(game.Buffers.RunProgressReadSink.StartingHandSize, Is.EqualTo(authoredHandSize));
            Assert.That(result.Acquired.Count, Is.EqualTo(4));
            Assert.That(game.Buffers.RunDeckReadSink.TryGetCurrent(out var deck), Is.True);
            Assert.That(deck.OrderedIds, Is.EqualTo(result.Acquired.Select(c => c.Id)));
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(loadout.StartingRunCurrency));
        }

        [TestCase(EntityKey.None, false)]
        [TestCase((EntityKey)999, false)]
        [TestCase(EntityKey.Unit_Shielder, false)]
        [TestCase(EntityKey.Unit_ShotGun, false)]
        [TestCase(EntityKey.Unit_Demon, false)]
        [TestCase(EntityKey.Unit_ShotGun, true)]
        public void StartNewRun_InvalidMain_DoesNotResetExistingRun(EntityKey selected, bool retainBattle)
        {
            using var game = new RunStartMainSelectionTestRuntime();
            StartExistingRun(game);
            if (retainBattle)
            {
                Assert.That(game.Core.BattleEntry.EnterBattle(1, new StageNodeId(2)).Accepted, Is.True);
                Assert.That(game.Buffers.CombatStateReadSink.HasBattle, Is.True);
            }
            var before = new CoreIntegrationSnapshot(game.Buffers);

            var result = game.Core.RunStart.StartNewRun(1, selected);

            AssertFailure(result, RosterAcquireFailureReason.InvalidDefinition);
            before.AssertUnchanged(game.Buffers);
        }

        [Test]
        public void StartNewRun_MissingLoadout_DoesNotResetExistingRun()
        {
            using var game = new RunStartMainSelectionTestRuntime();
            StartExistingRun(game);
            var before = new CoreIntegrationSnapshot(game.Buffers);

            var result = game.Core.RunStart.StartNewRun(999, EntityKey.Unit_Sniper);

            AssertFailure(result, RosterAcquireFailureReason.InvalidDefinition);
            before.AssertUnchanged(game.Buffers);
        }

        [Test]
        public void StartNewRun_MalformedSelectedDefinition_ThrowsWithoutReset()
        {
            using var game = new RunStartMainSelectionTestRuntime();
            StartExistingRun(game);
            game.Database.SetUnit(TestDatabase.Main(EntityKey.Unit_Sniper, 0, 5, 2));
            var before = new CoreIntegrationSnapshot(game.Buffers);

            Assert.Throws<InvalidOperationException>(() => game.Core.RunStart.StartNewRun(1, EntityKey.Unit_Sniper));

            before.AssertUnchanged(game.Buffers);
        }

        [Test]
        public void StartNewRun_InvalidStartingSub_DoesNotResetExistingRun()
        {
            using var game = new RunStartMainSelectionTestRuntime();
            StartExistingRun(game);
            game.Database.SetUnit(TestDatabase.Main(EntityKey.Unit_Cowboy, 20, 6, 3));
            var before = new CoreIntegrationSnapshot(game.Buffers);

            var result = game.Core.RunStart.StartNewRun(1, EntityKey.Unit_Sniper);

            AssertFailure(result, RosterAcquireFailureReason.InvalidDefinition);
            before.AssertUnchanged(game.Buffers);
        }

        [Test]
        public void StartNewRun_InitialDeckWithoutMain_DoesNotResetExistingRun()
        {
            using var game = new RunStartMainSelectionTestRuntime();
            StartExistingRun(game);
            var loadout = game.Database.Loadout;
            game.Database.Loadout = new PlayerLoadoutData(loadout.LoadoutKey, loadout.MainCharacterKey,
                loadout.Entries.ToArray(), loadout.StartingHandSize, loadout.StartingRunCurrency, new[] { 1, 2, 3 });
            var before = new CoreIntegrationSnapshot(game.Buffers);

            var result = game.Core.RunStart.StartNewRun(1, EntityKey.Unit_Sniper);

            AssertFailure(result, RosterAcquireFailureReason.InvalidInitialDeck, DeckFailureReason.MissingMain);
            before.AssertUnchanged(game.Buffers);
        }

        [TestCase("roster")]
        [TestCase("deck")]
        [TestCase("currency")]
        [TestCase("outcome")]
        public void StartNewRun_StateChangesDuringDefinitionRead_RejectsWithoutPublication(string change)
        {
            using var game = new RunStartMainSelectionTestRuntime();
            StartExistingRun(game);
            var buffers = game.Buffers;
            CoreIntegrationSnapshot afterRead = null;
            game.Database.BeforeNextUnitLookup = () =>
            {
                ChangeRunSource(buffers, change);
                afterRead = new CoreIntegrationSnapshot(buffers);
            };

            var result = game.Core.RunStart.StartNewRun(1, EntityKey.Unit_Sniper);

            Assert.That(afterRead, Is.Not.Null);
            AssertFailure(result, RosterAcquireFailureReason.InvalidRequest);
            afterRead.AssertUnchanged(buffers);
        }

        private static void AssertInstalledLoadout(RunStartMainSelectionTestRuntime game,
            RosterAcquireResult result, EntityKey expectedMain, int health, int attack, int defense)
        {
            Assert.That(result.Succeeded, Is.True, result.FailureReason.ToString());
            var main = result.Acquired.Single(c => c.Role == CharacterOwnershipRole.Main);
            Assert.That(result.Acquired.Select(c => c.CharacterKey), Is.EqualTo(new[]
            {
                expectedMain, EntityKey.Unit_ShotGun, EntityKey.Unit_Cowboy, EntityKey.Unit_DynamiteHammer
            }));
            Assert.That(result.Acquired.Skip(1).Select(c => c.Role), Is.All.EqualTo(CharacterOwnershipRole.Sub));
            var buffers = game.Buffers;
            Assert.That(buffers.CharacterReadSink.Owned, Is.EqualTo(result.Acquired));
            Assert.That(buffers.CharacterReadSink.TryGetMainState(main.Id, out var state), Is.True);
            Assert.That((state.CurrentHealth, state.PersistentMaxHealth, state.PersistentAttack, state.PersistentDefense),
                Is.EqualTo((health, health, attack, defense)));
            var runId = buffers.RunProgressReadSink.RunId;
            Assert.That(runId.IsEmpty, Is.False);
            Assert.That(buffers.RunProgressReadSink.StartingHandSize, Is.EqualTo(4));
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(137));
            Assert.That(buffers.RunDeckReadSink.TryGetCurrent(out var deck), Is.True);
            Assert.That(deck.RunId, Is.EqualTo(runId));
            Assert.That(deck.MainId, Is.EqualTo(main.Id));
            Assert.That(deck.OrderedIds, Is.EqualTo(result.Acquired.Select(c => c.Id)));
        }

        private static void AssertFailure(RosterAcquireResult result, RosterAcquireFailureReason reason,
            DeckFailureReason initialDeckFailure = DeckFailureReason.None)
        {
            Assert.That(result.FailureReason, Is.EqualTo(reason));
            Assert.That(result.InitialDeckFailure, Is.EqualTo(initialDeckFailure));
            Assert.That(result.Acquired, Is.Empty);
        }

        private static void StartExistingRun(RunStartMainSelectionTestRuntime game)
        {
            Assert.That(game.Core.RunStart.StartNewRun(1).Succeeded, Is.True);
            var buffers = game.Buffers;
            SetMainHealth(buffers, 17);
            buffers.CurrencyWriteSink.SetAmount(97);
            buffers.ItemInventoryWriteSink.PublishGrant(new ItemInventoryChange(buffers.ItemInventoryReadSink.Current,
                new ItemInventorySnapshot(new Dictionary<int, int> { [1] = 2 })));
            buffers.StageProgressWriteSink.RegisterStages(1, new[]
            {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 1101, 1, 0, Array.Empty<int>())
            });
            Assert.That(game.Core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
        }

        private static void SetMainHealth(IBufferQuery buffers, int health)
        {
            var main = buffers.CharacterReadSink.Owned.Single(c => c.Role == CharacterOwnershipRole.Main);
            Assert.That(buffers.CharacterReadSink.TryGetMainState(main.Id, out var state), Is.True);
            buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                Array.Empty<SoulCountChange>(), main: new MainCharacterPersistentChange(
                    main.Id, state, state.WithCurrentHealth(health))));
        }

        private static void ChangeRunSource(IBufferQuery buffers, string change)
        {
            switch (change)
            {
                case "roster":
                    SetMainHealth(buffers, 11);
                    break;
                case "deck":
                    Assert.That(buffers.RunDeckReadSink.TryGetCurrent(out var deck), Is.True);
                    var store = (IRunDeckStore)buffers.RunDeckReadSink;
                    store.Install(store.Prepare(deck.RunId, new[] { deck.MainId }
                        .Concat(deck.OrderedIds.Skip(1).Reverse()).ToArray()));
                    break;
                case "currency":
                    buffers.CurrencyWriteSink.SetAmount(81);
                    break;
                case "outcome":
                    var run = (IRunProgressBufferStore)buffers.RunProgressReadSink;
                    run.InstallCompletion(run.PrepareCompletion(
                        new RunCompletionReceipt(RunOutcome.Defeat, 1, new StageNodeId(2), new StageKey(1101))),
                        buffers.CurrencyReadSink.RunSpent, buffers.FameReadSink.RunEarned);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(change));
            }
        }
    }
}
