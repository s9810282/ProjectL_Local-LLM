using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class MainSpecializationPreparationTests
    {
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void PrepareEachClassDoesNotMutate(CategoryKey selected)
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            var before = new MainSpecializationStateCheck(game.Buffers);
            var prepared = MainSpecializationTestSupport.Prepare(game, selected);
            Assert.That(prepared.Decision.Succeeded, Is.True);
            Assert.That(prepared.Decision.Change.Final.SpecializationCategory, Is.EqualTo(selected));
            Assert.That(prepared.Growth.Status, Is.EqualTo(TraitGrowthProgressStatus.ReadyToContinue));
            before.AssertUnchanged(game.Buffers);
        }

        [TestCase("sub")]
        [TestCase("nonselected")]
        [TestCase("dead")]
        public void RejectSubNonselectedAndDeadMain(string scenario)
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            var target = game.TargetId;
            if (scenario == "sub") target = game.Buffers.CharacterReadSink.Owned.First(o => o.Role == CharacterOwnershipRole.Sub).Id;
            if (scenario == "nonselected") ((IRunDeckStore)game.Buffers.RunDeckReadSink).Reset();
            if (scenario == "dead") MainSpecializationTestSupport.SetHealth(game, 0);
            var before = new MainSpecializationStateCheck(game.Buffers);
            var prepared = game.Service.PrepareMainSpecialization(game.Buffers.RunProgressReadSink.RunId, game.Revision, target, CategoryKey.Rider);
            Assert.That(prepared.Decision.FailureReason, Is.EqualTo(MainSpecializationFailureReason.InvalidTarget));
            Assert.That(game.Service.ApplyMainSpecialization(prepared).FailureReason, Is.EqualTo(prepared.Decision.FailureReason));
            before.AssertUnchanged(game.Buffers);
        }

        [TestCase("empty", MainSpecializationFailureReason.RunUnavailable)]
        [TestCase("ended", MainSpecializationFailureReason.RunUnavailable)]
        [TestCase("battle", MainSpecializationFailureReason.InBattle)]
        [TestCase("run", MainSpecializationFailureReason.SourceChanged)]
        [TestCase("revision", MainSpecializationFailureReason.SourceChanged)]
        public void RejectEmptyOrEndedRunAndActiveBattleAndStaleSources(string scenario, MainSpecializationFailureReason expected)
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            var run = game.Buffers.RunProgressReadSink.RunId;
            long revision = game.Revision;
            if (scenario == "empty") game.Buffers.RunProgressWriteSink.ResetRunProgress();
            if (scenario == "ended") game.EndRun();
            if (scenario == "battle") game.EnterBattle();
            if (scenario == "run") run = new RunId(Guid.NewGuid());
            if (scenario == "revision") revision--;
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.That(game.Service.PrepareMainSpecialization(run, revision, game.TargetId, CategoryKey.Rider).Decision.FailureReason, Is.EqualTo(expected));
            before.AssertUnchanged(game.Buffers);
        }

        [TestCase(CategoryKey.None)]
        [TestCase(CategoryKey.Main)]
        [TestCase(CategoryKey.Boss)]
        public void InvalidCategoryReturnsTypedFailure(CategoryKey selected)
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.That(MainSpecializationTestSupport.Prepare(game, selected).Decision.FailureReason, Is.EqualTo(MainSpecializationFailureReason.InvalidCategory));
            before.AssertUnchanged(game.Buffers);
        }

        [Test]
        public void PendingStepWithoutOfferBlocksStandaloneApplication()
        {
            using var game = new TraitGrowthTestRuntime(main: true);
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.Null);
            AssertGrowthPending(game);
        }

        [Test]
        public void PendingOfferBlocksStandaloneApplication()
        {
            using var game = new TraitGrowthTestRuntime(main: true);
            game.Open();
            AssertGrowthPending(game);
        }

        [Test]
        public void UnacknowledgedGrowthResultBlocksStandaloneApplication()
        {
            using var game = new TraitGrowthTestRuntime(main: true);
            game.Publish(game.Request(game.Open()));
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.AwaitingAcknowledgement));
            AssertGrowthPending(game);
        }

        [Test]
        public void PureTransitionDoesNotDependOnShopOrGrowthOffer()
        {
            using var game = new TraitGrowthTestRuntime(main: true);
            game.Open();
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.That(MainSpecializationRules.Plan(game.Current, CategoryKey.Rider).Succeeded, Is.True);
            before.AssertUnchanged(game.Buffers);
        }

        [Test]
        public void RejectInvalidProfile()
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            game.Database.SetUnit(new UnitData(EntityKey.Unit_Akimbo, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly,
                1, 1, 10, 0, 2, CategoryKey.Main, 5, 1, new UnitTraitData[] { new PullTraitData(1) },
                targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.That(MainSpecializationTestSupport.Prepare(game).Decision.FailureReason, Is.EqualTo(MainSpecializationFailureReason.InvalidDefinition));
            before.AssertUnchanged(game.Buffers);
        }

        [Test]
        public void SourceChangeDuringReadDoesNotBecomeFreshPreparation()
        {
            using var game = new TraitGrowthTestRuntime(main: true, initialLevel: 1);
            game.Tables.BeforeRead = () =>
            {
                game.Tables.BeforeRead = null;
                ((IRunProgressBufferStore)game.Buffers.RunProgressReadSink).InstallRunId(new RunId(Guid.NewGuid()));
            };
            Assert.That(MainSpecializationTestSupport.Prepare(game).Decision.FailureReason, Is.EqualTo(MainSpecializationFailureReason.SourceChanged));
            Assert.That(game.Current.SpecializationCategory, Is.Null);
        }

        [Test]
        public void MissingRequiredConfigurationRemainsAnExplicitException()
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            typeof(CoreIntegrationTestData).GetField("<Config>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(game.Database, null);
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.Throws<ArgumentNullException>(() => MainSpecializationTestSupport.Prepare(game));
            before.AssertUnchanged(game.Buffers);
        }

        [Test]
        public void RejectUnsupportedProgression()
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false, initialLevel: 1);
            var tables = new MainSpecializationGrowthSource(game.Tables) { EmptyPool = true };
            game.Database.CategoryTraitProgressions = tables;
            game.Database.TraitGrowthDefinitions = tables;
            var before = new MainSpecializationStateCheck(game.Buffers);
            Assert.That(MainSpecializationTestSupport.Prepare(game).Decision.FailureReason, Is.EqualTo(MainSpecializationFailureReason.UnsupportedProgression));
            before.AssertUnchanged(game.Buffers);
        }

        [TestCase(EntityKey.Unit_Akimbo, CategoryKey.Rider)]
        [TestCase(EntityKey.Unit_Akimbo, CategoryKey.Heavy)]
        [TestCase(EntityKey.Unit_Akimbo, CategoryKey.Cannon)]
        [TestCase(EntityKey.Unit_Ballista, CategoryKey.Rider)]
        [TestCase(EntityKey.Unit_Ballista, CategoryKey.Heavy)]
        [TestCase(EntityKey.Unit_Ballista, CategoryKey.Cannon)]
        public void TwoSyntheticMainDefinitionsUseActualPreparationAndApplication(EntityKey key, CategoryKey selected)
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false, initialLevel: 1);
            game.Database.SetUnit(MainSpecializationTestSupport.Unit(key));
            var store = (ICharacterBufferStore)game.Buffers.CharacterReadSink;
            var requests = game.Buffers.CharacterReadSink.Owned.Select(o => o.Role == CharacterOwnershipRole.Main
                ? new RosterAcquireRequest(key, CharacterOwnershipRole.Main, initialMain: new MainCharacterPersistentState(7, 10, 5, 1))
                : new RosterAcquireRequest(o.CharacterKey, o.Role)).ToArray();
            store.ClearRoster(store.PrepareClearRoster());
            store.InstallAcquire(store.PrepareInitialRoster(requests));
            CoreIntegrationTestData.InstallDeck(game.Buffers);
            var before = new MainSpecializationStateCheck(game.Buffers);
            var prepared = MainSpecializationTestSupport.Prepare(game, selected);
            Assert.That(prepared.Decision.Succeeded, Is.True);
            before.AssertUnchanged(game.Buffers);
            Assert.That(game.Service.ApplyMainSpecialization(prepared).Succeeded, Is.True);
            Assert.That(game.Buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main).CharacterKey, Is.EqualTo(key));
            game.EnterBattle();
            var facts = game.Buffers.CharacterReadSink.Characters.Single(c => c.ToFacts().OwnershipRole == CharacterOwnershipRole.Main).ToFacts();
            Assert.That(facts.CharacterKey, Is.EqualTo(key));
            Assert.That(facts.Stats.CombatCategoryKey, Is.EqualTo(selected));
        }

        [Test]
        public void TraitDefinitionComparisonCoversEveryConcreteSpecAndPayload()
        {
            var baseType = typeof(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits.TraitSpec);
            var add = typeof(MainSpecializationDefinitionSnapshot).GetMethod("AddTrait", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            foreach (var type in baseType.Assembly.GetTypes().Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t)))
            {
                var constructors = type.GetConstructors();
                var constructor = constructors.Single(c => c.GetParameters().Length == constructors.Max(
                    candidate => candidate.GetParameters().Length));
                var parameters = constructor.GetParameters();
                var baseline = parameters.Select(p => TraitArgument(p.ParameterType, false)).ToArray();
                var first = new System.Collections.Generic.List<object>();
                Assert.DoesNotThrow(() => add.Invoke(null, new object[] { first, constructor.Invoke(baseline) }), type.Name);
                foreach (var property in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    int index = Array.FindIndex(parameters, p => string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase));
                    Assert.That(index, Is.GreaterThanOrEqualTo(0), type.Name + "." + property.Name + " requires an explicit source comparison decision.");
                    var changed = (object[])baseline.Clone();
                    changed[index] = TraitArgument(parameters[index].ParameterType, true);
                    var second = new System.Collections.Generic.List<object>();
                    add.Invoke(null, new object[] { second, constructor.Invoke(changed) });
                    Assert.That(second.SequenceEqual(first), Is.False, type.Name + "." + property.Name);
                }
            }
        }

        private static object TraitArgument(Type type, bool changed)
        {
            if (type == typeof(bool)) return changed;
            if (type == typeof(int)) return changed ? 2 : 1;
            if (type == typeof(long)) return changed ? 2L : 1L;
            if (type == typeof(System.Collections.Generic.IReadOnlyList<int>)) return changed ? new[] { 100, 50 } : new[] { 100 };
            if (type.IsEnum)
            {
                var values = Enum.GetValues(type).Cast<object>().Where(v => type != typeof(EntityKey) || Convert.ToInt64(v) != 0).ToArray();
                return values[changed ? 1 : 0];
            }
            throw new InvalidOperationException("Unhandled trait test parameter: " + type);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompletedHistoryIsPreservedAndNewNativeDuplicateIsRejected(bool duplicateNative)
        {
            using var game = new TraitGrowthTestRuntime(main: true);
            var acquire = game.Open();
            var selected = acquire.Choices[0].TraitKey;
            var acquired = game.Publish(game.Request(acquire, selected));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(acquired.RunId, acquired.Number), Is.True);
            var enhanced = game.Publish(game.Request(game.Open()));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(enhanced.RunId, enhanced.Number), Is.True);
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.ReadyToContinue));
            if (duplicateNative)
            {
                var definition = ((ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable.ITraitGrowthDefinitionTableQuery)game.Tables).Get(selected);
                game.Database.SetUnit(new UnitData(EntityKey.Unit_Akimbo, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly,
                    1, 1, 10, 0, 2, CategoryKey.Main, 5, 1, new UnitTraitData[] { new KnockbackTraitData(1), definition.Normal },
                    targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));
            }
            var original = game.Current;
            var before = new MainSpecializationStateCheck(game.Buffers);
            var prepared = MainSpecializationTestSupport.Prepare(game);
            before.AssertUnchanged(game.Buffers);
            if (duplicateNative)
            {
                Assert.That(prepared.Decision.FailureReason, Is.EqualTo(MainSpecializationFailureReason.UnsupportedProgression));
                Assert.That(game.Service.ApplyMainSpecialization(prepared).Succeeded, Is.False);
                before.AssertUnchanged(game.Buffers);
            }
            else
            {
                Assert.That(game.Service.ApplyMainSpecialization(prepared).Succeeded, Is.True);
                Assert.That(game.Current.Growth.SameValues(original.Growth), Is.True);
                game.EnterBattle();
            }
        }

        private static void AssertGrowthPending(TraitGrowthTestRuntime game)
        {
            var before = new MainSpecializationStateCheck(game.Buffers);
            var prepared = MainSpecializationTestSupport.Prepare(game);
            Assert.That(prepared.Decision.FailureReason, Is.EqualTo(MainSpecializationFailureReason.GrowthPending));
            Assert.That(game.Service.ApplyMainSpecialization(prepared).FailureReason, Is.EqualTo(MainSpecializationFailureReason.GrowthPending));
            before.AssertUnchanged(game.Buffers);
        }
    }

    internal static class MainSpecializationTestSupport
    {
        internal static MainSpecializationOperationPreparation Prepare(TraitGrowthTestRuntime game, CategoryKey selected = CategoryKey.Rider) =>
            game.Service.PrepareMainSpecialization(game.Buffers.RunProgressReadSink.RunId, game.Revision,
                game.Buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main).Id, selected);

        internal static UnitData Unit(EntityKey key, int attack = 5, int targeting = 1) => new(key, UnitRole.MainCharacter,
            UnitOwnerEligibility.PlayerOnly, 1, 1, 10, 0, 2, CategoryKey.Main, attack, 1,
            new UnitTraitData[] { new KnockbackTraitData(1) }, targetingMethodKey: targeting, killReward: UnitKillRewardSpec.Excluded);

        internal static void SetHealth(TraitGrowthTestRuntime game, int health)
        {
            game.Buffers.CharacterReadSink.TryGetMainState(game.TargetId, out var state);
            var store = (ICharacterBufferStore)game.Buffers.CharacterReadSink;
            store.InstallChanges(store.PrepareChanges(new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                main: new MainCharacterPersistentChange(game.TargetId, state, state.WithCurrentHealth(health))), null));
        }
    }

    internal sealed class MainSpecializationGrowthSource :
        ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable.ICategoryTraitProgressionTableQuery,
        ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable.ITraitGrowthDefinitionTableQuery
    {
        private readonly TraitGrowthTestRuntime.GrowthTables _source;
        internal bool EmptyPool { get; set; }
        internal int KnockbackDistance { get; set; } = 1;
        internal MainSpecializationGrowthSource(TraitGrowthTestRuntime.GrowthTables source) { _source = source; }
        private ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable.ICategoryTraitProgressionTableQuery Categories => _source;
        private ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable.ITraitGrowthDefinitionTableQuery Traits => _source;
        public System.Collections.Generic.IReadOnlyList<ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item.CategoryTraitProgressionData> GetAll() =>
            Categories.GetAll().Select(c => new ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item.CategoryTraitProgressionData(c.Category,
                c.Steps, EmptyPool ? Array.Empty<TraitKey>() : c.Pool)).ToArray();
        public ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item.CategoryTraitProgressionData Get(CategoryKey key) => GetAll().Single(c => c.Category == key);
        public bool TryGet(CategoryKey key, out ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item.CategoryTraitProgressionData data)
        { data = GetAll().SingleOrDefault(c => c.Category == key); return data != null; }
        System.Collections.Generic.IReadOnlyList<ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item.TraitGrowthDefinitionData>
            ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable.ITraitGrowthDefinitionTableQuery.GetAll() =>
            Traits.GetAll().Select(t => new ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item.TraitGrowthDefinitionData(
                t.Key == TraitKey.Knockback ? new KnockbackTraitData(KnockbackDistance) : t.Normal, t.Enhanced,
                t.Name, t.Description, t.EnhancedName, t.EnhancedDescription)).ToArray();
        public ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item.TraitGrowthDefinitionData Get(TraitKey key) =>
            ((ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable.ITraitGrowthDefinitionTableQuery)this).GetAll().Single(t => t.Key == key);
        public bool TryGet(TraitKey key, out ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item.TraitGrowthDefinitionData data)
        { data = ((ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable.ITraitGrowthDefinitionTableQuery)this).GetAll().SingleOrDefault(t => t.Key == key); return data != null; }
    }

    internal sealed class MainSpecializationStateCheck
    {
        private readonly CoreIntegrationSnapshot _all;
        private readonly object[] _growth;
        internal MainSpecializationStateCheck(IBufferQuery b)
        {
            _all = new CoreIntegrationSnapshot(b);
            _growth = Growth(b);
        }
        internal void AssertUnchanged(IBufferQuery b)
        {
            _all.AssertUnchanged(b);
            Assert.That(Growth(b), Is.EqualTo(_growth));
        }
        internal static object[] Growth(IBufferQuery b) => new object[] { b.TraitGrowthStateReadSink.ActiveOffer,
            b.TraitGrowthResultReadSink.LatestResult, b.TraitGrowthResultReadSink.HandledCommandNumber,
            b.TraitGrowthViewReadSink.AcknowledgedResultNumber };
    }
}
