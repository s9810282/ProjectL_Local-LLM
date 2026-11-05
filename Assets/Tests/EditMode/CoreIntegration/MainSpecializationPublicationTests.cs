using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class MainSpecializationPublicationTests
    {
        [Test]
        public void CategoryDefault_FreshNativeUnitUsesCurrentSettingWithoutSpendingGrowth()
        {
            using var game = new TraitGrowthTestRuntime(initialLevel: 1);
            var before = game.Current;
            foreach (var key in new[] { TraitKey.Vanguard, TraitKey.SoloAction })
            {
                game.SetCategoryDefault(CategoryKey.Cannon, key);
                var stats = game.Core.CharacterStats.ReadOwned().Items.Single(o => o.OwnedId == game.TargetId).Stats;
                Assert.That(stats.TraitKeys.ToArray(), Is.EquivalentTo(new[] { TraitKey.Knockback, key }));
                Assert.That(game.Current, Is.SameAs(before));
                Assert.That(game.Current.Growth.IsEmpty, Is.True);
            }
        }

        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void CategoryDefault_LevelOneRosterReflectsSelectedCombatCategory(CategoryKey selected)
        {
            using var game = new TraitGrowthTestRuntime(main: true, initialLevel: 1);
            game.SetCategoryDefault(selected == CategoryKey.Cannon ? CategoryKey.Rider : selected, TraitKey.Vanguard);
            var initial = game.Core.CharacterStats.ReadOwned().Items.Single(o => o.OwnedId == game.TargetId).Stats;
            Assert.That(initial.TraitKeys.ToArray(), Has.No.Member(TraitKey.Vanguard));
            var prepared = MainSpecializationTestSupport.Prepare(game, selected);
            Assert.That(game.Service.ApplyMainSpecialization(prepared).Succeeded, Is.True);
            var current = game.Core.CharacterStats.ReadOwned().Items.Single(o => o.OwnedId == game.TargetId).Stats;
            Assert.That(current.TraitKeys.ToArray().Contains(TraitKey.Vanguard), Is.EqualTo(selected != CategoryKey.Cannon));
            Assert.That((current.CategoryKey, current.CombatCategoryKey, current.AttackRange),
                Is.EqualTo((CategoryKey.Main, selected, initial.AttackRange)));
            Assert.That(game.Current.Growth.IsEmpty, Is.True);
        }

        [Test]
        public void CategoryDefault_ChangeAfterPreparationRejectsSpecialization()
        {
            using var game = new TraitGrowthTestRuntime(main: true, initialLevel: 1);
            game.SetCategoryDefault(CategoryKey.Rider, TraitKey.Vanguard);
            var prepared = MainSpecializationTestSupport.Prepare(game, CategoryKey.Rider);
            Assert.That(prepared.Decision.Succeeded, Is.True);
            game.SetCategoryDefault(CategoryKey.Rider, TraitKey.SoloAction);
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.That(game.Service.ApplyMainSpecialization(prepared).FailureReason, Is.EqualTo(MainSpecializationFailureReason.SourceChanged));
            before.AssertUnchanged(game.Buffers);
        }

        [TestCase(CategoryKey.Rider, 5)]
        [TestCase(CategoryKey.Heavy, 3)]
        [TestCase(CategoryKey.Cannon, 2)]
        public void ApplyEachClassUpdatesOnlyOwnedClassAndRevisionAndNextBattle(CategoryKey selected, int distance)
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            var before = game.Current;
            var revision = game.Revision;
            game.Buffers.RunDeckReadSink.TryGetCurrent(out var deck);
            game.Buffers.CharacterReadSink.TryGetMainState(game.TargetId, out var health);
            var growth = MainSpecializationStateCheck.Growth(game.Buffers);
            var currency = (game.Buffers.CurrencyReadSink.Amount, game.Buffers.CurrencyReadSink.RunSpent, game.Buffers.FameReadSink.Amount);
            var shop = game.Buffers.CurrentProgressReadSink.Shop;
            var souls = game.Buffers.CharacterReadSink.Owned.Select(o => game.Buffers.CharacterReadSink.GetSoulCount(o.Id)).ToArray();
            var other = game.Buffers.CharacterReadSink.Owned.Where(o => o.Id != game.TargetId).ToArray();
            var prepared = MainSpecializationTestSupport.Prepare(game, selected);
            Assert.That(game.Service.ApplyMainSpecialization(prepared).Succeeded, Is.True);
            Assert.That(game.Revision, Is.EqualTo(revision + 1));
            Assert.That(game.Current.SameValues(prepared.Decision.Change.Final), Is.True);
            Assert.That((game.Current.CharacterKey, game.Current.Role, game.Current.Level, game.Current.Experience, game.Current.HunterAttackBonus),
                Is.EqualTo((before.CharacterKey, before.Role, before.Level, before.Experience, before.HunterAttackBonus)));
            Assert.That(game.Current.Growth.SameValues(before.Growth), Is.True);
            Assert.That(MainSpecializationStateCheck.Growth(game.Buffers), Is.EqualTo(growth));
            Assert.That((game.Buffers.CurrencyReadSink.Amount, game.Buffers.CurrencyReadSink.RunSpent, game.Buffers.FameReadSink.Amount), Is.EqualTo(currency));
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop, Is.SameAs(shop));
            Assert.That(game.Buffers.CharacterReadSink.Owned.Where(o => o.Id != game.TargetId).ToArray(), Is.EqualTo(other));
            Assert.That(game.Buffers.CharacterReadSink.Owned.Select(o => game.Buffers.CharacterReadSink.GetSoulCount(o.Id)), Is.EqualTo(souls));
            game.Buffers.RunDeckReadSink.TryGetCurrent(out var afterDeck);
            Assert.That(afterDeck, Is.SameAs(deck));
            game.Buffers.CharacterReadSink.TryGetMainState(game.TargetId, out var afterHealth);
            Assert.That(afterHealth, Is.SameAs(health));
            var stats = game.Core.CharacterStats.ReadOwned().Items.Single(o => o.OwnedId == game.TargetId).Stats;
            Assert.That((stats.CategoryKey, stats.CombatCategoryKey, stats.MoveRange), Is.EqualTo((CategoryKey.Main, selected, distance)));
            game.EnterBattle();
            var facts = game.Buffers.CharacterReadSink.Characters.Single(c => c.ToFacts().OwnedId == game.TargetId).ToFacts();
            Assert.That((facts.Stats.IdentityCategoryKey, facts.Stats.CombatCategoryKey, facts.Stats.MoveRange), Is.EqualTo((CategoryKey.Main, selected, distance)));
            Assert.That(facts.Stats.AttackRange, Is.EqualTo(2));
            Assert.That(facts.Stats.TraitStatuses.ToArray().Select(t => t.Key), Has.Member(TraitKey.Knockback));
        }

        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Cannon)]
        public void RepeatedSameOrDifferentClassReturnsAlreadySpecialized(CategoryKey second)
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            Assert.That(game.Service.ApplyMainSpecialization(MainSpecializationTestSupport.Prepare(game)).Succeeded, Is.True);
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.That(MainSpecializationTestSupport.Prepare(game, second).Decision.FailureReason, Is.EqualTo(MainSpecializationFailureReason.AlreadySpecialized));
            before.AssertUnchanged(game.Buffers);
        }

        [TestCase("run")]
        [TestCase("roster")]
        [TestCase("deck")]
        [TestCase("health")]
        [TestCase("growth")]
        [TestCase("battle")]
        public void RunRosterDeckHealthOrGrowthChangeBetweenPrepareAndApplyRejects(string scenario)
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            var prepared = MainSpecializationTestSupport.Prepare(game);
            Mutate(game, scenario);
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.That(game.Service.ApplyMainSpecialization(prepared).FailureReason, Is.EqualTo(MainSpecializationFailureReason.SourceChanged));
            before.AssertUnchanged(game.Buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RelevantDefinitionChangeRejectsButEquivalentCopiesDoNot(bool change)
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            var prepared = MainSpecializationTestSupport.Prepare(game);
            game.Database.SetUnit(MainSpecializationTestSupport.Unit(EntityKey.Unit_Akimbo, attack: change ? 9 : 5));
            var before = new MainSpecializationStateCheck(game.Buffers);
            var result = game.Service.ApplyMainSpecialization(prepared);
            Assert.That(result.Succeeded, Is.EqualTo(!change));
            if (change)
            {
                Assert.That(result.FailureReason, Is.EqualTo(MainSpecializationFailureReason.SourceChanged));
                before.AssertUnchanged(game.Buffers);
            }
        }

        [Test]
        public void ForeignOrReusedPreparationRejects()
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            using var foreign = new TraitGrowthTestRuntime(main: true, enabled: false);
            var prepared = MainSpecializationTestSupport.Prepare(game);
            var before = new MainSpecializationStateCheck(foreign.Buffers);
            Assert.That(foreign.Service.ApplyMainSpecialization(prepared).FailureReason, Is.EqualTo(MainSpecializationFailureReason.SourceChanged));
            before.AssertUnchanged(foreign.Buffers);
            Assert.That(game.Service.ApplyMainSpecialization(prepared).Succeeded, Is.True);
            before = new MainSpecializationStateCheck(game.Buffers);
            Assert.That(game.Service.ApplyMainSpecialization(prepared).FailureReason, Is.EqualTo(MainSpecializationFailureReason.SourceChanged));
            before.AssertUnchanged(game.Buffers);
        }

        [TestCase("run")]
        [TestCase("roster")]
        [TestCase("deck")]
        [TestCase("health")]
        [TestCase("growth")]
        [TestCase("battle")]
        public void DirectBufferInstallRechecksOriginalSource(string scenario)
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            var prepared = game.Buffers.MainSpecializationOperations.Prepare(game.Buffers.RunProgressReadSink.RunId, game.Revision,
                MainSpecializationRules.Plan(game.Current, CategoryKey.Rider).Change);
            Mutate(game, scenario);
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.Throws<InvalidOperationException>(() => game.Buffers.MainSpecializationOperations.Install(prepared));
            before.AssertUnchanged(game.Buffers);
        }

        [TestCase("run")]
        [TestCase("revision")]
        [TestCase("deck")]
        [TestCase("health")]
        [TestCase("growth")]
        [TestCase("battle")]
        public void DirectBufferPreparationCannotBypassTargetAndSourceGuards(string scenario)
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            var run = game.Buffers.RunProgressReadSink.RunId;
            long revision = game.Revision;
            var change = MainSpecializationRules.Plan(game.Current, CategoryKey.Rider).Change;
            if (scenario == "revision") revision--;
            else if (scenario == "health") MainSpecializationTestSupport.SetHealth(game, 0);
            else if (scenario == "deck") ((IRunDeckStore)game.Buffers.RunDeckReadSink).Reset();
            else Mutate(game, scenario);
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.Throws<InvalidOperationException>(() => game.Buffers.MainSpecializationOperations.Prepare(run, revision, change));
            before.AssertUnchanged(game.Buffers);
        }

        [Test]
        public void DirectBufferForeignReusedAndNullTokensReject()
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            using var foreign = new TraitGrowthTestRuntime(main: true, enabled: false);
            var ops = game.Buffers.MainSpecializationOperations;
            var prepared = ops.Prepare(game.Buffers.RunProgressReadSink.RunId, game.Revision, MainSpecializationRules.Plan(game.Current, CategoryKey.Heavy).Change);
            var before = new MainSpecializationStateCheck(foreign.Buffers);
            Assert.Throws<InvalidOperationException>(() => foreign.Buffers.MainSpecializationOperations.Install(prepared));
            before.AssertUnchanged(foreign.Buffers);
            Assert.Throws<ArgumentNullException>(() => ops.Install(null));
            Assert.Throws<ArgumentNullException>(() => ops.Prepare(game.Buffers.RunProgressReadSink.RunId, game.Revision, null));
            ops.Install(prepared);
            before = new MainSpecializationStateCheck(game.Buffers);
            Assert.Throws<InvalidOperationException>(() => ops.Install(prepared));
            before.AssertUnchanged(game.Buffers);
        }

        [Test]
        public void OriginalCorePreparationIsRecheckedAfterBufferPrepare()
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            var prepared = MainSpecializationTestSupport.Prepare(game);
            var original = game.Buffers.MainSpecializationOperations;
            var wrapper = new ObservedTransaction(original, () => game.Database.SetUnit(MainSpecializationTestSupport.Unit(EntityKey.Unit_Akimbo, 19)));
            typeof(BufferSystemGroup).GetField("<MainSpecializationOperations>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game.Buffers, wrapper);
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.That(game.Service.ApplyMainSpecialization(prepared).FailureReason, Is.EqualTo(MainSpecializationFailureReason.SourceChanged));
            Assert.That(wrapper.Installed, Is.False);
            before.AssertUnchanged(game.Buffers);
        }

        [Test]
        public void RevisionOverflowDoesNotMutate()
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            typeof(CharacterBuffer).GetProperty("RosterRevision").SetValue(game.Buffers.CharacterReadSink, long.MaxValue);
            var prepared = MainSpecializationTestSupport.Prepare(game);
            Assert.That(prepared.Decision.Succeeded, Is.True);
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.That(game.Service.ApplyMainSpecialization(prepared).FailureReason, Is.EqualTo(MainSpecializationFailureReason.SourceChanged));
            Assert.Throws<OverflowException>(() => game.Buffers.MainSpecializationOperations.Prepare(prepared.Source.RunId,
                long.MaxValue, prepared.Decision.Change));
            before.AssertUnchanged(game.Buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GrowthDefinitionCopiesCompareValuesAndChangedPayloadRejects(bool change)
        {
            using var game = new TraitGrowthTestRuntime(main: true, initialLevel: 1);
            var tables = new MainSpecializationGrowthSource(game.Tables);
            game.Database.CategoryTraitProgressions = tables;
            game.Database.TraitGrowthDefinitions = tables;
            var prepared = MainSpecializationTestSupport.Prepare(game);
            Assert.That(prepared.Decision.Succeeded, Is.True);
            tables.KnockbackDistance = change ? 2 : 1;
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.That(game.Service.ApplyMainSpecialization(prepared).Succeeded, Is.EqualTo(!change));
            if (change) before.AssertUnchanged(game.Buffers);
        }

        [Test]
        public void StateChangeDuringApplyDefinitionReadPreservesTheInterveningChange()
        {
            using var game = new TraitGrowthTestRuntime(main: true, initialLevel: 1);
            var prepared = MainSpecializationTestSupport.Prepare(game);
            MainSpecializationStateCheck afterChange = null;
            game.Tables.BeforeRead = () =>
            {
                game.Tables.BeforeRead = null;
                MainSpecializationTestSupport.SetHealth(game, 4);
                afterChange = new MainSpecializationStateCheck(game.Buffers);
            };
            Assert.That(game.Service.ApplyMainSpecialization(prepared).FailureReason, Is.EqualTo(MainSpecializationFailureReason.SourceChanged));
            Assert.That(afterChange, Is.Not.Null);
            afterChange.AssertUnchanged(game.Buffers);
        }

        [TestCase("run")]
        [TestCase("health")]
        [TestCase("deck")]
        [TestCase("growth")]
        public void OriginalCoreStateIsRecheckedAfterBufferPreparation(string mutation)
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            var prepared = MainSpecializationTestSupport.Prepare(game);
            MainSpecializationStateCheck afterChange = null;
            var wrapper = new ObservedTransaction(game.Buffers.MainSpecializationOperations, () =>
            {
                Mutate(game, mutation);
                afterChange = new MainSpecializationStateCheck(game.Buffers);
            });
            typeof(BufferSystemGroup).GetField("<MainSpecializationOperations>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game.Buffers, wrapper);
            Assert.That(game.Service.ApplyMainSpecialization(prepared).FailureReason, Is.EqualTo(MainSpecializationFailureReason.SourceChanged));
            Assert.That(wrapper.Installed, Is.False);
            afterChange.AssertUnchanged(game.Buffers);
        }

        [Test]
        public void EnteredAndEndedBattleStillInvalidatesBothOriginalPreparations()
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            var prepared = MainSpecializationTestSupport.Prepare(game);
            var bufferPrepared = game.Buffers.MainSpecializationOperations.Prepare(prepared.Source.RunId, prepared.Source.Revision, prepared.Decision.Change);
            game.EnterBattle();
            game.Buffers.BattleWriteSink.EndBattle();
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.That(game.Service.ApplyMainSpecialization(prepared).FailureReason, Is.EqualTo(MainSpecializationFailureReason.SourceChanged));
            Assert.Throws<InvalidOperationException>(() => game.Buffers.MainSpecializationOperations.Install(bufferPrepared));
            before.AssertUnchanged(game.Buffers);
        }

        private static void Mutate(TraitGrowthTestRuntime game, string scenario)
        {
            if (scenario == "run") ((IRunProgressBufferStore)game.Buffers.RunProgressReadSink).InstallRunId(new RunId(Guid.NewGuid()));
            if (scenario == "roster") game.SetTargetLevel(3);
            if (scenario == "health") MainSpecializationTestSupport.SetHealth(game, 4);
            if (scenario == "deck")
            {
                game.Buffers.RunDeckReadSink.TryGetCurrent(out var deck);
                var store = (IRunDeckStore)game.Buffers.RunDeckReadSink;
                store.Install(store.Prepare(deck.RunId, new[] { deck.MainId }.Concat(deck.OrderedIds.Skip(1).Reverse()).ToArray()));
            }
            if (scenario == "growth")
            {
                game.Database.CategoryTraitProgressions = game.Tables;
                game.Database.TraitGrowthDefinitions = game.Tables;
                game.Open();
            }
            if (scenario == "battle") game.EnterBattle();
        }

        private sealed class ObservedTransaction : IMainSpecializationTransaction
        {
            private readonly IMainSpecializationTransaction _inner;
            private readonly Action _afterPrepare;
            internal bool Installed { get; private set; }
            internal ObservedTransaction(IMainSpecializationTransaction inner, Action afterPrepare) { _inner = inner; _afterPrepare = afterPrepare; }
            public PreparedMainSpecializationApplication Prepare(RunId run, long revision, MainSpecializationChange change)
            {
                var prepared = _inner.Prepare(run, revision, change);
                _afterPrepare();
                return prepared;
            }
            public void Install(PreparedMainSpecializationApplication prepared) { Installed = true; _inner.Install(prepared); }
        }
    }
}
