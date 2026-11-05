using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class SubTraitShopRetirementTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void LegacySubOffer_CannotSpendOrUsePurchase(bool directTransaction)
        {
            using var game = new Harness(1202);
            var owned = game.Buffers.CharacterReadSink.Owned.ToArray();
            var main = owned.Single(x => x.Role == CharacterOwnershipRole.Main);
            var target = owned.First(x => x.Role == CharacterOwnershipRole.Sub);
            var offer = ShopPurchaseSnapshot.ForEnhancement(new ShopOfferId(1), target.Id, target.CharacterKey,
                new OwnedEnhancement(EnhancementKey.DoubleShot, 1), 700, "Legacy", "Legacy");
            var current = game.Buffers.CurrentProgressReadSink;
            var store = (ICurrentProgressBufferStore)current;
            // 과거 방문 스냅샷을 복구해 서비스와 거래 경계가 각각 거절하는지 확인한다.
            store.InstallSelection(store.PrepareShopSelection(new ShopEntryPreparation(1, false, current.CurrentStage,
                new StageNodeId(2), new StageKey(1202), "Legacy", new[] { offer }, StageType.Shop, StageMapRole.Regular,
                owned, ShopVisitKind.Enhancement, main.Id)));
            var shop = current.Shop;
            var context = new ShopVisitContext(1, new StageNodeId(2), new StageKey(1202), shop.EntryId);
            int balance = game.Buffers.CurrencyReadSink.Amount;
            long revision = game.Buffers.CharacterReadSink.RosterRevision;
            game.Buffers.CharacterReadSink.TryGetMainState(main.Id, out var mainState);

            var result = directTransaction
                ? game.Buffers.ShopPurchaseWriteSink.PublishPurchase(new ShopPurchasePreparation(context, shop,
                    balance, owned, mainState, offer.OfferId, ShopPurchaseDecision.ForUnit(balance - 700,
                        new RosterAcquireRequest(target.CharacterKey, CharacterOwnershipRole.Sub))))
                : game.Core.Shop.Purchase(context, offer.OfferId);

            Assert.That(result.FailureReason, Is.EqualTo(ShopFailureReason.SubTraitPurchaseUnavailable));
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(balance));
            Assert.That(current.Shop, Is.SameAs(shop));
            Assert.That(current.Shop.HasPurchase, Is.False);
            Assert.That(game.Buffers.CharacterReadSink.RosterRevision, Is.EqualTo(revision));
            Assert.That(game.Buffers.CharacterReadSink.Owned, Is.EqualTo(owned));
            Assert.That(game.Buffers.CharacterReadSink.TryGetMainState(main.Id, out var afterMain), Is.True);
            Assert.That(afterMain, Is.SameAs(mainState));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LegacyVisit_NewTrainingApisCannotSpend(bool direct)
        {
            using var game = new Harness(1202);
            var owned = game.Buffers.CharacterReadSink.Owned.ToArray();
            var main = owned.Single(o => o.Role == CharacterOwnershipRole.Main);
            var target = owned.First(o => o.Role == CharacterOwnershipRole.Sub);
            var current = game.Buffers.CurrentProgressReadSink;
            var store = (ICurrentProgressBufferStore)current;
            store.InstallSelection(store.PrepareShopSelection(new ShopEntryPreparation(1, false, current.CurrentStage,
                new StageNodeId(2), new StageKey(1202), "Legacy", Array.Empty<ShopPurchaseSnapshot>(),
                StageType.Shop, StageMapRole.Regular, owned, ShopVisitKind.Enhancement, main.Id)));
            var shop = current.Shop;
            var context = new ShopVisitContext(1, new StageNodeId(2), new StageKey(1202), shop.EntryId);
            var before = new CoreIntegrationSnapshot(game.Buffers);
            var result = direct
                ? game.Buffers.ShopPurchaseWriteSink.PublishTraining(new TrainingPurchasePreparation(context, shop,
                    game.Buffers.CurrencyReadSink.Amount, game.Buffers.CharacterReadSink.RosterRevision, target.Id, TraitKey.Vanguard, 1))
                : game.Core.Shop.PurchaseTraining(context, target.Id, TraitKey.Vanguard, 1);
            Assert.That(result.FailureReason, Is.EqualTo(ShopFailureReason.SubTraitPurchaseUnavailable));
            before.AssertUnchanged(game.Buffers);
            Assert.That(current.Shop.TrainingReceipt, Is.Null);
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.Zero);
        }

        [Test]
        public void EnhancementNode_DoesNotPublishAnEmptyShop()
        {
            using var game = new Harness(1202);
            Assert.That(game.Core.ChapterProgression.PrepareStageEntry(1, new StageNodeId(2)).FailureReason,
                Is.EqualTo(ChapterProgressionFailureReason.EnhancementShopUnavailable));
            Assert.That(game.Core.ChapterProgression.ConfirmStageEntry(1, new StageNodeId(2)).FailureReason,
                Is.EqualTo(ChapterProgressionFailureReason.EnhancementShopUnavailable));
            Assert.That(game.Core.Shop.EnterShop(1, new StageNodeId(2), 7).FailureReason,
                Is.EqualTo(ChapterProgressionFailureReason.EnhancementShopUnavailable));
            Assert.That(game.Buffers.CurrentProgressReadSink.HasSelection, Is.False);
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LegacyEntryTransaction_DoesNotPublishEnhancementShop(bool withOffer)
        {
            using var game = new Harness(1202);
            var owned = game.Buffers.CharacterReadSink.Owned.ToArray();
            var main = owned.Single(x => x.Role == CharacterOwnershipRole.Main);
            var target = owned.First(x => x.Role == CharacterOwnershipRole.Sub);
            var offers = withOffer
                ? new[] { ShopPurchaseSnapshot.ForEnhancement(new ShopOfferId(1), target.Id, target.CharacterKey,
                    new OwnedEnhancement(EnhancementKey.DoubleShot, 1), 700, "Legacy", "Legacy") }
                : Array.Empty<ShopPurchaseSnapshot>();
            var current = game.Buffers.CurrentProgressReadSink;
            var preparation = new ShopEntryPreparation(1, false, current.CurrentStage,
                new StageNodeId(2), new StageKey(1202), "Legacy", offers, StageType.Shop, StageMapRole.Regular,
                owned, ShopVisitKind.Enhancement, main.Id);
            var snapshot = new CoreIntegrationSnapshot(game.Buffers);

            Assert.Throws<InvalidOperationException>(() => game.Buffers.ShopStageWriteSink.PrepareShop(preparation));

            snapshot.AssertUnchanged(game.Buffers);
            Assert.That(current.HasSelection, Is.False);
            Assert.That(current.Shop, Is.Null);
        }

        [Test]
        public void OrdinaryUnitShop_StillEntersAndPurchasesOnce()
        {
            using var game = new Harness(1201);
            Assert.That(game.Core.Shop.EnterShop(1, new StageNodeId(2), 7).Accepted, Is.True);
            var shop = game.Buffers.CurrentProgressReadSink.Shop;
            var offer = shop.Offers.Single();
            int balance = game.Buffers.CurrencyReadSink.Amount;
            int count = game.Buffers.CharacterReadSink.Owned.Count;
            var context = new ShopVisitContext(1, new StageNodeId(2), new StageKey(1201), shop.EntryId);
            Assert.That(game.Core.Shop.Purchase(context, offer.OfferId).Succeeded, Is.True);
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(balance - offer.Cost));
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop.HasPurchase, Is.True);
            Assert.That(game.Buffers.CharacterReadSink.Owned.Count, Is.EqualTo(count + 1));
            Assert.That(game.Core.Shop.Purchase(context, offer.OfferId).Succeeded, Is.False);
        }

        private sealed class Harness : IDisposable
        {
            private readonly FrameworkRuntime _runtime;
            internal IInGameCoreSystemGroupService Core { get; }
            internal IBufferQuery Buffers { get; }

            internal Harness(int stageKey)
            {
                var builder = new SystemBuilderHelper();
                BufferSystemFactory.Create().ExportServices(builder);
                InGameCoreSystemFactory.Create().ExportServices(builder);
                _runtime = new FrameworkRuntime(builder.BuildSystemReader());
                try
                {
                    var data = ChapterDistributionTestData.Standard();
                    ((CoreIntegrationTestData)data.UnitTableQuery).SetUnit(new UnitData(EntityKey.Unit_Ballista,
                        UnitRole.Regular, UnitOwnerEligibility.PlayerOnly, 1, 1, 10, 0, 2, CategoryKey.Heavy, 5, 1,
                        Array.Empty<UnitTraitData>(), targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));
                    data.Add(new StageRootData(1201, 1, StageMapRole.Regular,
                        new ShopStageDetail(new[] { EntityKey.Unit_Ballista }, Array.Empty<int>(), 5)));
                    _runtime.InitData(data);
                    _runtime.PostBindService();
                    Core = _runtime.GetService(InGameSystemGroupServiceKey.Core);
                    Buffers = _runtime.GetService(BufferSystemKey.Buffer);
                    Assert.That(Core.Characters.InitializeRoster(1).Succeeded, Is.True);
                    CoreIntegrationTestData.InstallDeck(Buffers);
                    Buffers.CurrencyWriteSink.SetAmount(10000);
                    Buffers.StageProgressWriteSink.RegisterStages(1, new[]
                    {
                        new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                        new StageProgressData(2, stageKey, 1, 0, Array.Empty<int>(), stageType: StageType.Shop)
                    });
                    Assert.That(Core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
                }
                catch { _runtime.Shutdown(); throw; }
            }

            public void Dispose() => _runtime.Shutdown();
        }
    }
}
