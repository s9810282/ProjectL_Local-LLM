using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;

namespace ProjectL.Tests.CoreIntegration
{
    internal sealed class TraitGrowthTestRuntime : IDisposable
    {
        private readonly FrameworkRuntime _runtime;
        internal CoreIntegrationTestData Database { get; }
        internal GrowthTables Tables { get; }
        internal IInGameCoreSystemGroupService Core { get; }
        internal IBufferQuery Buffers { get; }
        internal CharacterRosterService Service => (CharacterRosterService)Core.Characters;
        internal CharacterInstanceId TargetId { get; }
        internal OwnedCharacterRecord Current => Buffers.CharacterReadSink.Owned.Single(o => o.Id == TargetId);
        internal long Revision => Buffers.CharacterReadSink.RosterRevision;
        internal Action AfterBattleDefinitionRead { get; set; }
        internal TraitGrowthTestRuntime(bool main = false, bool enabled = true, bool lastOnly = false, int initialLevel = 2, bool multiLevel = false, bool secondCategory = false, bool observeBattleDefinition = false, int? eligibleChoices = null, IDeckEditAvailabilityRead deckEditAvailability = null)
        {
            Database = new CoreIntegrationTestData(new CategoryConfigData(3,5,3,2,0,                levelProgression: new LevelProgressionSpec(150, 100, 3, 1, 1)));
            Tables = new GrowthTables(main ? CategoryKey.Main : CategoryKey.Cannon, lastOnly, multiLevel, secondCategory, eligibleChoices);
            if (enabled)
            {
                Database.CategoryTraitProgressions = Tables;
                Database.TraitGrowthDefinitions = Tables;
            }
            var key = main ? EntityKey.Unit_Akimbo : EntityKey.Unit_ShotGun;
            Database.SetUnit(new UnitData(key, main ? UnitRole.MainCharacter : UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                1, 1, 10, 0, 2, main ? CategoryKey.Main : CategoryKey.Cannon, 5, 1, new UnitTraitData[] { new KnockbackTraitData(1) }, targetingMethodKey: 1,
                killReward: UnitKillRewardSpec.Excluded));
            Database.SetUnit(new UnitData(EntityKey.Unit_Ballista, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                1, 1, 10, 0, 2, CategoryKey.Heavy, 5, 1, secondCategory ? new UnitTraitData[] { new KnockbackTraitData(1) } : Array.Empty<UnitTraitData>(), targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            InGameCoreSystemFactory.Create(battleSource: observeBattleDefinition ? new ObservedBattleSource(this) : null, deckEditAvailability: deckEditAvailability).ExportServices(builder);
            _runtime = new FrameworkRuntime(builder.BuildSystemReader());
            _runtime.InitData(Database);
            _runtime.PostBindService();
            Core = _runtime.GetService(InGameSystemGroupServiceKey.Core);
            Buffers = _runtime.GetService(BufferSystemKey.Buffer);
            ((IRunProgressBufferStore)Buffers.RunProgressReadSink).InstallRunId(new RunId(Guid.NewGuid()));
            Assert.That(Core.Characters.InitializeRoster(1).Succeeded, Is.True);
            if (main)
            {
                var before = Buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main);
                TargetId = before.Id;
                var grown = new OwnedCharacterRecord(before.Id, before.CharacterKey, before.Role, initialLevel, 0, before.Growth);
                CoreIntegrationTestData.InstallDeck(Buffers);
                Buffers.RunDeckReadSink.TryGetCurrent(out var membership);
                var store = (ICharacterBufferStore)Buffers.CharacterReadSink;
                store.InstallChanges(store.PrepareCommandChanges(new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                    owned: new[] { new OwnedCharacterChange(before, grown) }), membership));
            }
            else
                TargetId = Core.Characters.AcquireSub(key, initialLevel).Acquired.Single().Id;
            CoreIntegrationTestData.InstallDeck(Buffers);
        }
        internal void SetCategoryDefault(CategoryKey category, TraitKey key)
        {
            Database.Config = new CategoryConfigData(3, 5, 3, 2, 0, levelProgression: Database.Config.LevelProgression,
                defaultTraits: new Dictionary<CategoryKey, TraitKey> { [category] = key });
        }

        internal TraitGrowthOffer Open()
        {
            Assert.That(Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.ChoiceRequired));
            var progress = Core.Characters.PrepareNextTraitGrowthOffer();
            Assert.That(progress.ActiveOffer, Is.Not.Null);
            return progress.ActiveOffer;
        }
        internal TraitGrowthChoiceRequest Request(TraitGrowthOffer offer, TraitKey? key = null) => new(offer.RunId, offer.OfferId, offer.OwnedId, offer.StepId, key ?? offer.Choices[0].TraitKey);
        internal void SetTargetLevel(int level)
        {
            var before = Current;
            var grown = new OwnedCharacterRecord(before.Id, before.CharacterKey, before.Role, level, before.Experience, before.Growth);
            Buffers.RunDeckReadSink.TryGetCurrent(out var deck);
            var store = (ICharacterBufferStore)Buffers.CharacterReadSink;
            store.InstallChanges(store.PrepareCommandChanges(new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                owned: new[] { new OwnedCharacterChange(before, grown) }), deck));
        }
        internal void RestoreTargetGrowth(OwnedTraitGrowth growth)
        {
            var store = (ICharacterBufferStore)Buffers.CharacterReadSink;
            foreach (var selection in growth.Selections)
            {
                var before = Current;
                var restored = new OwnedCharacterRecord(before.Id, before.CharacterKey, before.Role, before.Level,
                    before.Experience, before.Growth.Append(selection), before.HunterAttackBonus, before.SpecializationCategory);
                store.InstallGrowth(store.PrepareGrowth(new TraitGrowthChange(before, restored)));
            }
        }
        internal TraitGrowthProcessedResult Publish(TraitGrowthChoiceRequest request)
        {
            int number = Buffers.TraitGrowthViewWriteSink.Submit(request);
            return Publish(Service.PrepareTraitGrowthChoice(request), number, request);
        }
        internal TraitGrowthProcessedResult Publish(TraitGrowthOperationPreparation operation, int number, TraitGrowthChoiceRequest request)
        {
            var result = operation.Decision.Succeeded ? TraitGrowthResult.Success(operation.Decision.Change.Final) : TraitGrowthResult.Failure(operation.Decision.FailureReason);
            var receipt = new TraitGrowthProcessedResult(operation.RunId, number, request, result);
            var prepared = Buffers.TraitGrowthOperations.PrepareChoicePublication(operation.RunId, operation.Revision, operation.Offer, operation.Decision.Change, receipt);
            Buffers.TraitGrowthOperations.InstallChoicePublication(prepared);
            return receipt;
        }
        internal void EnterBattle()
        {
            Buffers.StageProgressWriteSink.RegisterStages(1, new[] { new StageProgressData(1, 0, 0, 0, new[] { 2 }), new StageProgressData(2, 1101, 1, 0, Array.Empty<int>()) });
            Assert.That(Core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
            Assert.That(Core.BattleEntry.EnterBattle(1, new StageNodeId(2)).Accepted, Is.True);
        }
        internal void InjectBattleDuringDefinitionRead()
        {
            // 외부 조회 중 지원하지 않는 전투 설치가 일어난 경우의 원본 재검증만 검사한다.
            var initialization = CoreIntegrationTestData.DatabaseSource(Buffers).Read(Database, 1, new StageKey(1101));
            Buffers.RunDeckReadSink.TryGetCurrent(out var deck);
            var owned = deck.OrderedIds.Select(id => Buffers.CharacterReadSink.Owned.Single(o => o.Id == id)).ToArray();
            Buffers.CharacterReadSink.TryGetMainState(deck.MainId, out var main);
            var preparation = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.BattlePreparationCalculator.Prepare(
                initialization, owned, main, owned.ToDictionary(o => o.Id, o => Buffers.CharacterReadSink.GetSoulCount(o.Id)),
                Core.Characters.PlanInitialDraw(initialization.Stage.TimelineSeed), Array.Empty<CharacterCombatFacts>(), deck, Revision);
            Buffers.BattleWriteSink.PrepareBattle(preparation);
        }
        internal void EndRun()
        {
            var store = (IRunProgressBufferStore)Buffers.RunProgressReadSink;
            store.InstallCompletion(store.PrepareCompletion(new RunCompletionReceipt(RunOutcome.Defeat, 1, new StageNodeId(2), new StageKey(1101))), 0, 0);
        }
        public void Dispose() => _runtime.Shutdown();
        private sealed class ObservedBattleSource : ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem.IBattleInitializationSource
        {
            private readonly TraitGrowthTestRuntime _owner;
            internal ObservedBattleSource(TraitGrowthTestRuntime owner) { _owner = owner; }
            public BattleInitialization Read(ProjectL.Systems.DataBaseSystem.IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var definition = CoreIntegrationTestData.DatabaseSource(_owner.Buffers).Read(database, chapterKey, stageKey);
                _owner.AfterBattleDefinitionRead?.Invoke();
                return definition;
            }
        }
        internal sealed class GrowthTables : ICategoryTraitProgressionTableQuery, ITraitGrowthDefinitionTableQuery
        {
            private readonly CategoryTraitProgressionData[] _categories;
            private readonly TraitGrowthDefinitionData[] _definitions;
            internal Action BeforeRead
            {
                get; set;
            }
            internal void ReplaceCategory(CategoryTraitProgressionData category)
            {
                _categories[Array.FindIndex(_categories, c => c.Category == category.Category)] = category;
            }
            internal void ReplaceDefinition(TraitKey previous, TraitGrowthDefinitionData definition)
            {
                _definitions[Array.FindIndex(_definitions, d => d.Key == previous)] = definition;
            }
            internal GrowthTables(CategoryKey category, bool lastOnly, bool multiLevel = false, bool secondCategory = false, int? eligibleChoices = null)
            {
                _definitions = new[] {
                    new TraitGrowthDefinitionData(new KnockbackTraitData(1), new KnockbackTraitData(3), "knock", "normal", "knock+", "enhanced"),
                    new TraitGrowthDefinitionData(new SoloActionTraitData(1, 1, 0, 1), new SoloActionTraitData(3, 1, 0, 1), "solo", "normal", "solo+", "enhanced"),
                    new TraitGrowthDefinitionData(new HuntersMarkTraitData(20, 1), new HuntersMarkTraitData(40, 1), "mark", "normal", "mark+", "enhanced"),
                    new TraitGrowthDefinitionData(new VanguardTraitData(2, 1), new VanguardTraitData(4, 2), "vanguard", "normal", "vanguard+", "enhanced"),
                    new TraitGrowthDefinitionData(new TrackingShotTraitData(50, 10), null, "tracking", "normal", "", "") };
                if (eligibleChoices.HasValue)
                    _definitions = _definitions.Take(eligibleChoices.Value + 1).ToArray();
                var steps = lastOnly ? new[] { new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Enhance) }
                    : new[] { new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Acquire), new TraitGrowthStepSpec(20, 2, TraitGrowthKind.Enhance) };
                if (multiLevel)
                    steps = steps.Append(new TraitGrowthStepSpec(30, 4, TraitGrowthKind.Acquire)).ToArray();
                _categories = new[] { CategoryKey.Main, CategoryKey.Heavy, CategoryKey.Rider, CategoryKey.Cannon }.Select(k => new CategoryTraitProgressionData(k,
                    k == category || secondCategory && k == CategoryKey.Heavy ? steps : Array.Empty<TraitGrowthStepSpec>(),
                    k == category || secondCategory && k == CategoryKey.Heavy ? _definitions.Select(d => d.Key).ToArray() : Array.Empty<TraitKey>())).ToArray();
            }
            CategoryTraitProgressionData ICategoryTraitProgressionTableQuery.Get(CategoryKey key)
            {
                BeforeRead?.Invoke();
                return _categories.Single(c => c.Category == key);
            }
            bool ICategoryTraitProgressionTableQuery.TryGet(CategoryKey key, out CategoryTraitProgressionData data)
            {
                data = _categories.SingleOrDefault(c => c.Category == key);
                return data != null;
            }
            IReadOnlyList<CategoryTraitProgressionData> ICategoryTraitProgressionTableQuery.GetAll()
            {
                BeforeRead?.Invoke();
                return _categories;
            }
            TraitGrowthDefinitionData ITraitGrowthDefinitionTableQuery.Get(TraitKey key)
            {
                BeforeRead?.Invoke();
                return _definitions.Single(d => d.Key == key);
            }
            bool ITraitGrowthDefinitionTableQuery.TryGet(TraitKey key, out TraitGrowthDefinitionData data)
            {
                data = _definitions.SingleOrDefault(d => d.Key == key);
                return data != null;
            }
            IReadOnlyList<TraitGrowthDefinitionData> ITraitGrowthDefinitionTableQuery.GetAll()
            {
                BeforeRead?.Invoke();
                return _definitions;
            }
        }
    }
}
