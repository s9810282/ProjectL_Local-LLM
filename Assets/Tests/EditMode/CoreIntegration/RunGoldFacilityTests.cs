using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;
using UnityEditor;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class RunGoldFacilityTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void RestAtCapacity_RejectsGrowthButStillAllowsHealingAndExit(bool heal)
        {
            using var game = new Harness();
            game.SetMain(game.Main.With(persistentMaxHealth: int.MaxValue));
            game.EnterRest();
            Assert.That(game.Core.RecoveryState.TryGetCurrent(out var state), Is.True);
            Assert.That(state.Options.Single(o => o.Option.Id.Value == 4).UnavailableReason, Is.EqualTo(RecoveryFailureReason.NoEffect));
            Assert.That(game.Publish(4).Result.FailureReason, Is.EqualTo(RecoveryFailureReason.NoEffect));
            if (heal)
            {
                Assert.That(game.Publish(2).Result.Succeeded, Is.True);
                Assert.That(game.Main.CurrentHealth, Is.EqualTo(644245134));
            }
            Assert.That(game.Main.PersistentMaxHealth, Is.EqualTo(int.MaxValue));
            Assert.That(game.CompleteRest().Result.Succeeded, Is.True);
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(heal ? 5 : 0));
        }

        [Test]
        public void RestGrowth_ReachingIntegerMaximumStillPublishesAndCompletes()
        {
            using var game = new Harness();
            game.SetMain(game.Main.With(persistentMaxHealth: int.MaxValue - 10));
            game.EnterRest();
            var result = game.Publish(4).Result;
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.StateAfter.MainCharacterHealth.MaxHealth, Is.EqualTo(int.MaxValue));
            Assert.That(result.StateAfter.Receipt.MaxHealthIncrease, Is.EqualTo(10));
            Assert.That(game.CompleteRest().Result.Succeeded, Is.True);
            Assert.That(game.Core.Recovery.EnterRecovery(1, new StageNodeId(3)).Accepted, Is.True);
            Assert.That(game.Core.RecoveryState.TryGetCurrent(out var next), Is.True);
            Assert.That(next.Options.Single(o => o.Option.Id.Value == 4).CanSelect, Is.False);
            Assert.That(game.CompleteRest().Result.Succeeded, Is.True);
        }

        [TestCase(40, 0, 2, 58, 60, 5)]
        [TestCase(40, 0, 4, 40, 70, 10)]
        [TestCase(60, 0, 4, 60, 70, 10)]
        [TestCase(70, 10, 4, 70, 80, 10)]
        public void RestPurchase_PublishesExactHealthPaymentAndSingleVisitReceipt(int hp, int souls, int optionId,
            int finalHp, int effectiveMaximum, int cost)
        {
            using var game = new Harness(hp: hp, souls: souls);
            game.EnterRest();
            var result = game.Publish(optionId).Result;
            Assert.That(result.Succeeded, Is.True);
            Assert.That((result.StateAfter.MainCharacterHealth.CurrentHealth, result.StateAfter.MainCharacterHealth.MaxHealth),
                Is.EqualTo((finalHp, effectiveMaximum)));
            Assert.That((game.Main.PersistentAttack, game.Main.PersistentDefense), Is.EqualTo((7, 9)));
            Assert.That(game.Buffers.CharacterReadSink.GetSoulCount(game.MainId), Is.EqualTo(souls));
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(20 - cost));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(cost));
            Assert.That(result.StateAfter.Receipt.SpentAmount, Is.EqualTo(cost));
            Assert.That(result.StateAfter.Receipt.HealedAmount, Is.EqualTo(optionId == 2 ? finalHp - hp : 0));
            Assert.That(result.StateAfter.Receipt.MaxHealthIncrease, Is.EqualTo(optionId == 4 ? 10 : 0));
            Assert.That(game.Buffers.CurrentProgressReadSink.Recovery.MaxHealthIncrease, Is.EqualTo(optionId == 4 ? 10 : 0));
            Assert.That(result.StateAfter.Options.All(o => !o.CanSelect), Is.True);
            var main = game.Main;
            var receipt = game.Buffers.CurrentProgressReadSink.Recovery;
            Assert.That(game.Publish(optionId).Result.AlreadyApplied, Is.True);
            Assert.That(game.Publish(optionId == 2 ? 4 : 2).Result.FailureReason, Is.EqualTo(RecoveryFailureReason.AlreadyResolved));
            Assert.That(game.Main, Is.SameAs(main));
            Assert.That(game.Buffers.CurrentProgressReadSink.Recovery, Is.SameAs(receipt));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(cost));
            var completed = game.CompleteRest();
            Assert.That(completed.Result.Succeeded, Is.True);
            Assert.That(completed.Result.StateAfter.IsCompleted, Is.True);
            Assert.That(game.Core.ChapterProgression.PrepareStageEntry(1, new StageNodeId(3)).Accepted, Is.True);
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(cost));
        }

        [TestCase(60, 20, 2, RecoveryFailureReason.NoEffect)]
        [TestCase(40, 9, 4, RecoveryFailureReason.InsufficientFunds)]
        public void UnavailableRestPurchase_DoesNotConsumeVisitOrChangeState(int hp, int gold, int optionId, RecoveryFailureReason reason)
        {
            using var game = new Harness(hp: hp, gold: gold);
            game.EnterRest();
            var before = game.Main;
            Assert.That(game.Publish(optionId).Result.FailureReason, Is.EqualTo(reason));
            Assert.That(game.Main, Is.SameAs(before));
            Assert.That(game.Buffers.CurrentProgressReadSink.Recovery.AppliedOptionId, Is.Null);
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(gold));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.Zero);
            Assert.That(game.CompleteRest().Result.Succeeded, Is.True);
        }

        [Test]
        public void SameCommandNumber_CannotPublishOrSpendTwice()
        {
            using var game = new Harness();
            game.EnterRest();
            var first = game.Publish(4);
            var main = game.Main;
            Assert.That(game.Core.RecoveryCommands.ProcessNextRecoveryCommand(first.Number), Is.False);
            Assert.That(game.Main, Is.SameAs(main));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(10));
            Assert.That(game.Buffers.RecoveryResultReadSink.HandledCommandNumber, Is.EqualTo(first.Number));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void PreparedRestMutation_RejectsChangedSoulsRosterMainOrWalletAtomically(int source)
        {
            using var game = new Harness();
            game.EnterRest();
            var decision = game.GrowthDecision();
            var prepared = game.Buffers.RecoveryOperations.PrepareSelection(game.Context, decision);
            if (source == 0) game.AddSoul();
            else if (source == 1) Assert.That(game.Core.Characters.AcquireSub(EntityKey.Unit_Ballista).Succeeded, Is.True);
            else if (source == 2) game.SetMain(game.Main.With(persistentAttack: 8));
            else game.Buffers.CurrencyWriteSink.SetAmount(19);
            var before = game.Main;
            int balance = game.Buffers.CurrencyReadSink.Amount;
            var receipt = game.Buffers.CurrentProgressReadSink.Recovery;
            Assert.Throws<InvalidOperationException>(() => game.Buffers.RecoveryOperations.InstallMutation(prepared));
            Assert.That(game.Main, Is.SameAs(before));
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(balance));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.Zero);
            Assert.That(game.Buffers.CurrentProgressReadSink.Recovery, Is.SameAs(receipt));
            Assert.That(game.Buffers.RecoveryResultReadSink.HandledCommandNumber, Is.Zero);
        }

        [Test]
        public void PreparedRestPublication_RejectsChangedSoulSourceWithoutPublishingResult()
        {
            using var game = new Harness();
            game.EnterRest();
            var service = (RecoveryStageService)game.Core.Recovery;
            using (service.BeginOperation())
            {
                var command = RecoveryCommandData.Select(game.Context, new RecoveryOptionId(4));
                int number = game.Buffers.RecoveryViewWriteSink.Submit(command);
                var operation = service.PrepareOperation(command);
                var publication = game.Buffers.RecoveryOperations.PreparePublication(operation.Mutation,
                    new RecoveryProcessedResult(number, command, operation.Result), 0);
                game.AddSoul();
                var before = game.Main;
                Assert.Throws<InvalidOperationException>(() => game.Buffers.RecoveryOperations.InstallPublication(publication));
                Assert.That(game.Main, Is.SameAs(before));
                Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(20));
                Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.Zero);
                Assert.That(game.Buffers.RecoveryResultReadSink.HandledCommandNumber, Is.Zero);
                Assert.That(game.Buffers.CurrentProgressReadSink.Recovery.AppliedOptionId, Is.Null);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RestMutation_RejectsChangedAuthoredEffectOrMagnitude(bool changeKind)
        {
            using var game = new Harness();
            game.EnterRest();
            var option = changeKind ? new RecoveryOptionSpec(new RecoveryOptionId(4), 10, new RecoveryHealSpec(RecoveryHealMode.Flat, 1))
                : new RecoveryOptionSpec(new RecoveryOptionId(4), 10, 11);
            var decision = CharacterRecoveryCalculator.Plan(game.Main, game.MainId, 0, option, 20);
            Assert.Throws<InvalidOperationException>(() => game.Buffers.RecoveryOperations.PrepareSelection(game.Context, decision));
            Assert.That(game.Main.PersistentMaxHealth, Is.EqualTo(60));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.Zero);
        }

        [Test]
        public void RestMutation_RevalidatesSpendingOverflowBeforeAnyWrite()
        {
            using var game = new Harness();
            game.EnterRest();
            var prepared = game.Buffers.RecoveryOperations.PrepareSelection(game.Context, game.GrowthDecision());
            var currency = (ICurrencyBufferStore)game.Buffers.CurrencyReadSink;
            game.Buffers.CurrencyWriteSink.SetAmount(int.MaxValue);
            currency.InstallAmount(currency.PrepareAmount(int.MaxValue, 20));
            currency.InstallAmount(currency.PrepareAmount(20, 40));
            currency.InstallAmount(currency.PrepareAmount(40, 20));
            var before = game.Main;
            Assert.Throws<OverflowException>(() => game.Buffers.RecoveryOperations.InstallMutation(prepared));
            Assert.That(game.Main, Is.SameAs(before));
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(20));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(int.MaxValue));
            Assert.That(game.Buffers.CurrentProgressReadSink.Recovery.AppliedOptionId, Is.Null);
        }

        [Test]
        public void ProductionMercenaryOffice_OffersOnlyUnitsAndAllowsOneHire()
        {
            using var game = new Harness(production: true, shop: true);
            var definition = (ShopStageDetail)game.Definition.StageData;
            int cost = definition.UnitRecruitCostGold;
            game.Buffers.CurrencyWriteSink.SetAmount(cost);
            int spentBefore = game.Buffers.CurrencyReadSink.RunSpent;
            Assert.That(definition.ItemKeys, Is.Empty);
            Assert.That(game.Core.Shop.EnterShop(1, new StageNodeId(2), 17).Accepted, Is.True);
            var visit = game.Buffers.CurrentProgressReadSink.Shop;
            Assert.That(visit.Offers, Is.Not.Empty);
            Assert.That(visit.Offers.All(o => o.Kind == ShopProductKind.Unit), Is.True);
            var offer = visit.Offers[0];
            Assert.That(offer.Cost, Is.EqualTo(cost));
            int count = game.Buffers.CharacterReadSink.Owned.Count;
            var context = new ShopVisitContext(1, new StageNodeId(2), new StageKey(game.Definition.StageKey), visit.EntryId);
            Assert.That(game.Core.Shop.Purchase(context, offer.OfferId).Succeeded, Is.True);
            Assert.That(game.Buffers.CharacterReadSink.Owned.Count, Is.EqualTo(count + 1));
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.Zero);
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(spentBefore + cost));
            Assert.That(game.Core.Shop.Purchase(context, offer.OfferId).Succeeded, Is.False);
            Assert.That(game.Buffers.CharacterReadSink.Owned.Count, Is.EqualTo(count + 1));
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.Zero);
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(spentBefore + cost));
            Assert.That(game.Core.Shop.CompleteShop(context).Accepted, Is.True);
        }

        private sealed class Harness : IDisposable
        {
            private readonly FrameworkRuntime _runtime;
            internal IInGameCoreSystemGroupService Core { get; }
            internal IBufferQuery Buffers { get; }
            internal StageRootData Definition { get; }
            internal CharacterInstanceId MainId { get; }
            internal MainCharacterPersistentState Main
            {
                get
                {
                    Buffers.CharacterReadSink.TryGetMainState(MainId, out var main);
                    return main;
                }
            }
            internal RecoveryEntryContext Context => Buffers.CurrentProgressReadSink.Recovery.Context;

            internal Harness(bool production = false, int hp = 40, int souls = 0, int gold = 20, bool shop = false)
            {
                IDataBaseQueryContext database;
                if (production)
                {
                    var catalog = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset");
                    database = (IDataBaseQueryContext)DataBaseFactory.Create(catalog);
                    Definition = database.InGameQuery.StageTableQuery.GetStages(1).First(s => shop
                        ? s.StageData is ShopStageDetail : s.StageData is EventStageDetail);
                    if (shop)
                    {
                        // 운영 후보와 유닛 정의를 쓰되, 아직 후보를 보유하지 않은 Run에서 모집을 검증한다.
                        var eligibleRun = new CoreIntegrationTestData(new CategoryConfigData(3, 4, 3, 2, 0));
                        eligibleRun.SetStage(Definition);
                        foreach (var key in ((ShopStageDetail)Definition.StageData).UnitKeys)
                            eligibleRun.SetUnit(database.InGameQuery.UnitTableQuery.GetUnit(key));
                        database = eligibleRun;
                    }
                }
                else
                {
                    var fixture = new CoreIntegrationTestData(new CategoryConfigData(3, 4, 3, 2, 0));
                    Definition = new StageRootData(1301, 1, StageMapRole.Regular, new EventStageDetail(new[] { 2, 4 }));
                    fixture.SetStage(Definition);
                    fixture.RecoveryOptions = new RecoveryTable();
                    fixture.SetUnit(new UnitData(EntityKey.Unit_Ballista, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                        1, 1, 10, 0, 2, CategoryKey.Heavy, 5, 1, Array.Empty<UnitTraitData>(), targetingMethodKey: 1,
                        killReward: UnitKillRewardSpec.Excluded));
                    database = fixture;
                }
                var builder = new SystemBuilderHelper();
                BufferSystemFactory.Create().ExportServices(builder);
                InGameCoreSystemFactory.Create().ExportServices(builder);
                _runtime = new FrameworkRuntime(builder.BuildSystemReader());
                try
                {
                    _runtime.InitData(database);
                    _runtime.PostBindService();
                    Core = _runtime.GetService(InGameSystemGroupServiceKey.Core);
                    Buffers = _runtime.GetService(BufferSystemKey.Buffer);
                    Assert.That(Core.Characters.InitializeRoster(1).Succeeded, Is.True);
                    CoreIntegrationTestData.InstallDeck(Buffers);
                    MainId = Buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main).Id;
                    var store = (ICharacterBufferStore)Buffers.CharacterReadSink;
                    store.InstallChanges(store.PrepareChanges(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                        new[] { new SoulCountChange(MainId, Buffers.CharacterReadSink.GetSoulCount(MainId), souls) },
                        new MainCharacterPersistentChange(MainId, Main, new MainCharacterPersistentState(hp, 60, 7, 9))), null));
                    Buffers.CurrencyWriteSink.SetAmount(gold);
                    Buffers.StageProgressWriteSink.RegisterStages(1, new[]
                    {
                        new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                        new StageProgressData(2, Definition.StageKey, 1, 0, new[] { 3 }, stageType: shop ? StageType.Shop : StageType.Event),
                        new StageProgressData(3, Definition.StageKey, 2, 0, Array.Empty<int>(), stageType: shop ? StageType.Shop : StageType.Event)
                    });
                    Assert.That(Core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
                }
                catch { _runtime.Shutdown(); throw; }
            }

            internal void EnterRest() => Assert.That(Core.Recovery.EnterRecovery(1, new StageNodeId(2)).Accepted, Is.True);
            internal CharacterRecoveryDecision GrowthDecision() => CharacterRecoveryCalculator.Plan(Main, MainId,
                Buffers.CharacterReadSink.GetSoulCount(MainId), new RecoveryOptionSpec(new RecoveryOptionId(4), 10, 10), Buffers.CurrencyReadSink.Amount);
            internal void SetMain(MainCharacterPersistentState main)
            {
                var store = (ICharacterBufferStore)Buffers.CharacterReadSink;
                store.InstallChanges(store.PrepareChanges(new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                    new MainCharacterPersistentChange(MainId, Main, main)), null));
            }
            internal void AddSoul()
            {
                int souls = Buffers.CharacterReadSink.GetSoulCount(MainId);
                var store = (ICharacterBufferStore)Buffers.CharacterReadSink;
                store.InstallChanges(store.PrepareChanges(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                    new[] { new SoulCountChange(MainId, souls, souls + 1) }), null));
            }
            internal RecoveryProcessedResult Publish(int optionId) => Publish(RecoveryCommandData.Select(Context, new RecoveryOptionId(optionId)));
            internal RecoveryProcessedResult CompleteRest() => Publish(RecoveryCommandData.Complete(Context));
            private RecoveryProcessedResult Publish(RecoveryCommandData command)
            {
                int number = Buffers.RecoveryViewWriteSink.Submit(command);
                Assert.That(Core.RecoveryCommands.ProcessNextRecoveryCommand(number), Is.True);
                Assert.That(Buffers.RecoveryResultReadSink.TryGetLatest(out var result), Is.True);
                Buffers.RecoveryViewWriteSink.AcknowledgeResult(number);
                Assert.That(Buffers.RecoveryViewWriteSink.RemoveCommand(number), Is.True);
                return result;
            }
            public void Dispose() => _runtime.Shutdown();
        }

        private sealed class RecoveryTable : IRecoveryOptionTableQuery
        {
            private readonly RecoveryOptionData[] _rows =
            {
                new(2, 5, RecoveryHealMode.PercentOfMax, 30),
                new(4, 10, RecoveryHealMode.Flat, 0, RecoveryEffectKind.MaxHealthGrowth, 10)
            };
            public RecoveryOptionData GetRecoveryOption(int key) => _rows.Single(r => r.OptionId == key);
            public bool TryGetRecoveryOption(int key, out RecoveryOptionData data)
            {
                data = _rows.SingleOrDefault(r => r.OptionId == key);
                return data != null;
            }
            public IReadOnlyCollection<RecoveryOptionData> GetAllRecoveryOptions() => _rows;
        }
    }
}
