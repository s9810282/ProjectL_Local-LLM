using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;

namespace ProjectL.Tests.CoreIntegration
{
    internal sealed class TrainingGroundTestRuntime : IDisposable
    {
        private readonly FrameworkRuntime _runtime;
        private int _nodeId;
        internal CoreIntegrationTestData Database { get; }
        internal IInGameCoreSystemGroupService Core { get; }
        internal IBufferQuery Buffers { get; }
        internal CharacterInstanceId MainId { get; }
        internal CharacterInstanceId TargetId { get; }
        internal CharacterInstanceId SiblingId { get; }
        internal CharacterInstanceId CappedId { get; }
        internal ShopVisitContext Context => new(1, new StageNodeId(_nodeId), new StageKey(1202),
            Buffers.CurrentProgressReadSink.Shop.EntryId);
        internal OwnedCharacterRecord Owned(CharacterInstanceId id) => Buffers.CharacterReadSink.Owned.Single(o => o.Id == id);

        internal TrainingGroundTestRuntime(int gold = 100, bool noEligible = false, string source = "native",
            bool vanguardStopsAtTwo = false)
        {
            var defaults = source == "default" ? new[] { CategoryKey.Main, CategoryKey.Rider, CategoryKey.Heavy, CategoryKey.Cannon }
                .ToDictionary(k => k, _ => TraitKey.Vanguard) : null;
            var data = Database = new CoreIntegrationTestData(new CategoryConfigData(3, 4, 3, 2, 0, defaultTraits: defaults));
            var tables = new TrainingTables(noEligible, source, vanguardStopsAtTwo);
            data.CategoryTraitProgressions = tables;
            data.TraitGrowthDefinitions = tables;
            foreach (var unit in data.GetAllUnits().Where(u => u.OwnerEligibility != UnitOwnerEligibility.EnemyOnly).ToArray())
                data.SetUnit(new UnitData(unit.Key, unit.Role, unit.OwnerEligibility,
                    1, 1, 10, 0, 2, unit.Category, 5, 1,
                    source == "default" || source == "acquired" ? new UnitTraitData[] { new KnockbackTraitData(1) }
                        : new UnitTraitData[] { new VanguardTraitData(1, 1), new KnockbackTraitData(1) },
                    targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));
            data.SetStage(new StageRootData(1202, 1, StageMapRole.Regular,
                new EnhancementStageDetail(new TrainingPrices(10, 20))));
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            InGameCoreSystemFactory.Create().ExportServices(builder);
            _runtime = new FrameworkRuntime(builder.BuildSystemReader());
            try
            {
                _runtime.InitData(data);
                _runtime.PostBindService();
                Core = _runtime.GetService(InGameSystemGroupServiceKey.Core);
                Buffers = _runtime.GetService(BufferSystemKey.Buffer);
                Assert.That(Core.Characters.InitializeRoster(1).Succeeded, Is.True);
                CoreIntegrationTestData.InstallDeck(Buffers);
                MainId = Buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main).Id;
                SiblingId = Buffers.CharacterReadSink.Owned.Single(o => o.CharacterKey == EntityKey.Unit_Sniper).Id;
                CappedId = Buffers.CharacterReadSink.Owned.Single(o => o.CharacterKey == EntityKey.Unit_Cowboy).Id;
                // 정상 모집 한도와 별개로 복구된 동일 유닛의 서로 다른 보유 ID를 구성한다.
                TargetId = Buffers.CharacterWriteSink.Acquire(new[]
                    { new RosterAcquireRequest(EntityKey.Unit_Sniper, CharacterOwnershipRole.Sub) }).Acquired.Single().Id;
                Assert.That(Buffers.CharacterWriteSink.Acquire(new[]
                    { new RosterAcquireRequest(EntityKey.Unit_Sniper, CharacterOwnershipRole.Sub) }).Succeeded, Is.True);
                foreach (var owned in Buffers.CharacterReadSink.Owned.ToArray())
                {
                    var growth = new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10,
                        source == "acquired" ? TraitKey.Vanguard : TraitKey.SoloAction, TraitGrowthKind.Acquire) });
                    if (!noEligible && owned.Id == MainId) growth = growth.WithPurchase(TraitKey.Vanguard);
                    if (!noEligible && owned.Id == CappedId)
                    {
                        growth = growth.WithPurchase(TraitKey.Vanguard);
                        if (!vanguardStopsAtTwo) growth = growth.WithPurchase(TraitKey.Vanguard);
                    }
                    Seed(owned.Id, new OwnedCharacterRecord(owned.Id, owned.CharacterKey, owned.Role, 2, 17,
                        growth, owned.HunterAttackBonus, owned.SpecializationCategory));
                }
                Buffers.CurrencyWriteSink.SetAmount(gold);
                Buffers.StageProgressWriteSink.RegisterStages(1, new[]
                {
                    new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                    new StageProgressData(2, 1202, 1, 0, new[] { 3 }, stageType: StageType.Shop),
                    new StageProgressData(3, 1202, 2, 0, new[] { 4 }, stageType: StageType.Shop),
                    new StageProgressData(4, 1202, 3, 0, Array.Empty<int>(), stageType: StageType.Shop)
                });
                Assert.That(Core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
                Assert.That(Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.ReadyToContinue));
            }
            catch { _runtime.Shutdown(); throw; }
        }

        internal void Enter(int nodeId = 2)
        {
            _nodeId = nodeId;
            Assert.That(Core.ChapterProgression.PrepareStageEntry(1, new StageNodeId(nodeId)).Accepted, Is.True);
            Assert.That(Core.Shop.EnterShop(1, new StageNodeId(nodeId), 7).Accepted, Is.True);
        }

        internal void Seed(CharacterInstanceId id, OwnedCharacterRecord final)
        {
            // 방문 전 저장 이력을 복구하는 픽스처이며 실제 구매는 서비스·거래 경계로만 검증한다.
            var roster = Buffers.CharacterReadSink.Owned.ToList();
            long revision = Buffers.CharacterReadSink.RosterRevision;
            ((ICharacterBufferStore)Buffers.CharacterReadSink).InstallGrowth(new PreparedTraitGrowthChange(
                roster.FindIndex(o => o.Id == id), final, revision + 1, revision));
        }

        internal ShopCommandResult Publish(ShopViewCommandData command)
        {
            int number = Buffers.ShopViewWriteSink.Submit(command);
            Assert.That(Core.StageCommands.ProcessNextShopCommand(number), Is.True);
            Assert.That(Buffers.StageCommandResults.TryGetShopResult(out var result), Is.True);
            Assert.That(result.RequestNumber, Is.EqualTo(number));
            Buffers.ShopViewWriteSink.RemoveCommandsThrough(number);
            return result;
        }

        public void Dispose() => _runtime.Shutdown();

        internal sealed class TrainingTables : ICategoryTraitProgressionTableQuery, ITraitGrowthDefinitionTableQuery
        {
            private readonly CategoryTraitProgressionData[] _categories;
            private readonly TraitGrowthDefinitionData[] _traits;

            internal TrainingTables(bool noEligible, string source, bool vanguardStopsAtTwo = false)
            {
                _categories = new[] { CategoryKey.Main, CategoryKey.Rider, CategoryKey.Heavy, CategoryKey.Cannon }
                    .Select(category => new CategoryTraitProgressionData(category,
                        new[] { new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Acquire) },
                        source == "acquired" ? new[] { TraitKey.Vanguard } : new[] { TraitKey.SoloAction })).ToArray();
                _traits = new[]
                {
                    new TraitGrowthDefinitionData(new VanguardTraitData(1, 1), null, "vanguard", "", "", "",
                        noEligible ? null : new TraitRankValues(TraitKey.Vanguard, 2),
                        noEligible || vanguardStopsAtTwo ? null : new TraitRankValues(TraitKey.Vanguard, 3)),
                    new TraitGrowthDefinitionData(new SoloActionTraitData(1, 1, 0, 1), null, "solo", "", "", ""),
                    new TraitGrowthDefinitionData(new StationaryFireTraitData(10), null, "stationary", "", "", "",
                        new TraitRankValues(TraitKey.StationaryFire, 20), new TraitRankValues(TraitKey.StationaryFire, 30))
                };
            }

            CategoryTraitProgressionData ICategoryTraitProgressionTableQuery.Get(CategoryKey key) => _categories.Single(c => c.Category == key);
            bool ICategoryTraitProgressionTableQuery.TryGet(CategoryKey key, out CategoryTraitProgressionData data)
            {
                data = _categories.SingleOrDefault(c => c.Category == key);
                return data != null;
            }
            IReadOnlyList<CategoryTraitProgressionData> ICategoryTraitProgressionTableQuery.GetAll() => _categories;
            TraitGrowthDefinitionData ITraitGrowthDefinitionTableQuery.Get(TraitKey key) => _traits.Single(t => t.Key == key);
            bool ITraitGrowthDefinitionTableQuery.TryGet(TraitKey key, out TraitGrowthDefinitionData data)
            {
                data = _traits.SingleOrDefault(t => t.Key == key);
                return data != null;
            }
            IReadOnlyList<TraitGrowthDefinitionData> ITraitGrowthDefinitionTableQuery.GetAll() => _traits;
        }
    }
}
