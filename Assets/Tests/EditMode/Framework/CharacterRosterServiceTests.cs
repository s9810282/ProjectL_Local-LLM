using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class CharacterRosterServiceTests
    {
        [Test]
        public void InitialRoster_IssuesMainThenSubsInSpecOrder_AndDrawExcludesMain()
        {
            var fixture = Create();
            Assert.That(fixture.Core.Characters.InitializeRoster(1).Succeeded, Is.True);
            Assert.That(fixture.Buffers.CharacterReadSink.Owned.Select(x => x.Id.Value),
                Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(fixture.Buffers.CharacterReadSink.Owned.Select(x => x.CharacterKey), Is.EqualTo(new[]
            {
                EntityKey.Unit_MainCharacter, EntityKey.Unit_Sniper, EntityKey.Unit_Akimbo, EntityKey.Unit_Akimbo
            }));
            Assert.That(fixture.Core.Characters.PlanInitialDraw(42).Order.Select(x => x.Value),
                Is.EquivalentTo(new[] { 2, 3, 4 }));
            Assert.That(fixture.Core.Characters.InitializeRoster(1).Succeeded, Is.False);
            Assert.That(fixture.Buffers.CharacterReadSink.Owned.Count, Is.EqualTo(4));
        }

        [Test]
        public void Purchase_SameKindCreatesNewInstanceWithoutCopyingSouls_AndRejectsSecondPayment()
        {
            var fixture = Create();
            fixture.Core.Characters.InitializeRoster(1);
            var existing = fixture.Buffers.CharacterReadSink.Owned.First(x => x.CharacterKey == EntityKey.Unit_Akimbo);
            fixture.Buffers.CharacterWriteSink.Apply(new CharacterChanges(
                Array.Empty<CharacterStateChange>(), new[] { new SoulCountChange(existing.Id, 0, 2) }));
            PrepareShop(fixture, 10);

            CharacterPurchaseResult bought = fixture.Core.Shop.Purchase(new ShopOfferId(1));
            Assert.That(bought.Succeeded, Is.True);
            Assert.That(bought.Acquisition.Acquired.Single().Id.Value, Is.EqualTo(5));
            Assert.That(fixture.Buffers.CharacterReadSink.GetSoulCount(existing.Id), Is.EqualTo(2));
            Assert.That(fixture.Buffers.CharacterReadSink.GetSoulCount(bought.Acquisition.Acquired.Single().Id), Is.Zero);
            Assert.That(fixture.Buffers.CurrencyReadSink.Amount, Is.EqualTo(5));
            Assert.That(fixture.Buffers.CurrentProgressReadSink.Shop.IsPurchased(new ShopOfferId(1)), Is.True);

            Assert.That(fixture.Core.Shop.Purchase(new ShopOfferId(1)).FailureReason, Is.EqualTo(ShopFailureReason.SoldOut));
            Assert.That(fixture.Buffers.CurrencyReadSink.Amount, Is.EqualTo(5));
            Assert.That(fixture.Buffers.CharacterReadSink.Owned.Count, Is.EqualTo(5));
        }

        [TestCase(0)]
        [TestCase(4)]
        public void RejectedPurchase_ChangesNeitherCurrencyNorRosterNorOffer(int balance)
        {
            var fixture = Create();
            fixture.Core.Characters.InitializeRoster(1);
            PrepareShop(fixture, balance);

            Assert.That(fixture.Core.Shop.Purchase(new ShopOfferId(1)).FailureReason,
                Is.EqualTo(ShopFailureReason.InsufficientFunds));
            Assert.That(fixture.Buffers.CurrencyReadSink.Amount, Is.EqualTo(balance));
            Assert.That(fixture.Buffers.CharacterReadSink.Owned.Count, Is.EqualTo(4));
            Assert.That(fixture.Buffers.CurrentProgressReadSink.Shop.PurchasedMask, Is.Zero);

            fixture.Buffers.CurrencyWriteSink.SetAmount(5);
            Assert.That(fixture.Core.Shop.Purchase(new ShopOfferId(1)).Acquisition.Acquired.Single().Id.Value,
                Is.EqualTo(5));
        }

        [Test]
        public void RejectedPurchase_IdOverflow_ChangesNeitherCurrencyNorRosterNorOffer()
        {
            var fixture = Create();
            // 기존 생성자 경계로 ID 공간이 소진된 실제 CharacterBuffer를 조립한다.
            var character = new ProjectL.Systems.RuntimeSystem.BufferSystems.Character.CharacterBuffer(int.MaxValue);
            var nested = new SystemBuilderHelper();
            new ProjectL.Systems.RuntimeSystem.BufferSystems.Run.RunProgressBuffer().ExportServices(nested);
            character.ExportServices(nested);
            new ProjectL.Systems.RuntimeSystem.BufferSystems.Current.CurrentProgressBuffer().ExportServices(nested);
            new ProjectL.Systems.RuntimeSystem.BufferSystems.Currency.CurrencyBuffer().ExportServices(nested);
            new ProjectL.Systems.RuntimeSystem.BufferSystems.Combat.CombatStateBuffer().ExportServices(nested);
            new ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView.CombatViewBuffer().ExportServices(nested);
            new ProjectL.Systems.RuntimeSystem.BufferSystems.Map.MapProgressBuffer().ExportServices(nested);
            new ProjectL.Systems.RuntimeSystem.BufferSystems.Stage.StageProgressBuffer().ExportServices(nested);
            new ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact.ArtifactBuffer().ExportServices(nested);
            new ProjectL.Systems.RuntimeSystem.BufferSystems.StageView.StageViewBuffer().ExportServices(nested);
            new ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView.ArtifactViewBuffer().ExportServices(nested);
            var root = new SystemBuilderHelper();
            new BufferSystemGroup(nested.BuildSystemReader()).ExportServices(root);
            var resolver = root.BuildSystemReader().ServiceStoreReader;
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            var core = (ISystemPostBindService)fixture.Core;
            core.PostBindService(resolver);
            buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, CharacterBufferTestData.MainState());
            buffers.CurrentProgressWriteSink.SelectMap(1);
            buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(100), "Shop");
            buffers.CurrencyWriteSink.SetAmount(10);
            Assert.That(fixture.Core.Shop.PrepareShop(42), Is.EqualTo(ShopFailureReason.None));

            Assert.That(fixture.Core.Shop.Purchase(new ShopOfferId(1)).FailureReason,
                Is.EqualTo(ShopFailureReason.IdentityOverflow));
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(10));
            Assert.That(buffers.CharacterReadSink.Owned.Single().Id.Value, Is.EqualTo(int.MaxValue));
            Assert.That(buffers.CurrentProgressReadSink.Shop.PurchasedMask, Is.Zero);
        }

        [Test]
        public void ShopSelection_PreservesFrozenOffersAndPurchasesUntilSelectionChanges()
        {
            var fixture = Create();
            fixture.Core.Characters.InitializeRoster(1);
            PrepareShop(fixture, 10);
            fixture.Core.Shop.Purchase(new ShopOfferId(1));
            var first = fixture.Buffers.CurrentProgressReadSink.Shop;

            fixture.Buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(100), "Same shop");
            Assert.That(fixture.Core.Shop.PrepareShop(999), Is.EqualTo(ShopFailureReason.None));
            Assert.That(fixture.Buffers.CurrentProgressReadSink.Shop, Is.SameAs(first));

            fixture.Buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(3), new StageKey(100), "New shop");
            Assert.That(fixture.Buffers.CurrentProgressReadSink.Shop, Is.Null);
            Assert.That(fixture.Core.Shop.PrepareShop(42), Is.EqualTo(ShopFailureReason.None));
            Assert.That(fixture.Buffers.CurrentProgressReadSink.Shop.PurchasedMask, Is.Zero);
            fixture.Buffers.CurrentProgressWriteSink.SelectMap(2);
            Assert.That(fixture.Buffers.CurrentProgressReadSink.Shop, Is.Null);
        }

        [Test]
        public void InvalidInitialDefinition_ConsumesNoIdAndInstallsNoRoster()
        {
            var fixture = Create(new CharacterRosterDatabase(enemyStartingSub: true));
            Assert.That(fixture.Core.Characters.InitializeRoster(1).FailureReason,
                Is.EqualTo(RosterAcquireFailureReason.InvalidDefinition));
            Assert.That(fixture.Buffers.CharacterReadSink.Owned, Is.Empty);
            Assert.That(fixture.Core.Characters.AcquireSub(EntityKey.Unit_Akimbo).Acquired.Single().Id.Value,
                Is.EqualTo(1));
        }

        private static void PrepareShop((IInGameCoreSystemGroupService Core, IBufferQuery Buffers) fixture,
            int balance)
        {
            fixture.Buffers.CurrentProgressWriteSink.SelectMap(1);
            fixture.Buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(100), "Shop");
            fixture.Buffers.CurrencyWriteSink.SetAmount(balance);
            Assert.That(fixture.Core.Shop.PrepareShop(42), Is.EqualTo(ShopFailureReason.None));
        }

        [TestCase(0)]
        [TestCase(2)]
        public void DuplicateShopSlots_AreIndependentAndEachGrantsOneInstance(int cost)
        {
            var fixture = Create(new CharacterRosterDatabase(shopCost: cost));
            fixture.Core.Characters.InitializeRoster(1);
            PrepareShop(fixture, 6);
            for (int id = 1; id <= 3; id++)
            {
                var bought = fixture.Core.Shop.Purchase(new ShopOfferId(id));
                Assert.That(bought.Succeeded, Is.True);
                Assert.That(bought.Acquisition.Acquired.Single().Id.Value, Is.EqualTo(4 + id));
                Assert.That(fixture.Core.Shop.Purchase(new ShopOfferId(id)).FailureReason, Is.EqualTo(ShopFailureReason.SoldOut));
            }
            Assert.That(fixture.Buffers.CurrencyReadSink.Amount, Is.EqualTo(6 - 3 * cost));
            Assert.That(fixture.Buffers.CharacterReadSink.Owned.Count, Is.EqualTo(7));
            Assert.That(fixture.Buffers.CharacterReadSink.Owned.Select(x => x.Id).Distinct().Count(), Is.EqualTo(7));
        }

        [Test]
        public void UnknownShopSlot_DoesNotConsumeValidSlot_AndFreeUnownedPurchaseIsOneUse()
        {
            var fixture = Create(new CharacterRosterDatabase(shopCost: 0, shopCharacter: EntityKey.Unit_Demon));
            fixture.Core.Characters.InitializeRoster(1);
            PrepareShop(fixture, 0);
            Assert.That(fixture.Core.Shop.Purchase(ShopOfferId.Empty).FailureReason, Is.EqualTo(ShopFailureReason.UnknownOffer));
            Assert.That(fixture.Buffers.CharacterReadSink.Owned.Count, Is.EqualTo(4));
            Assert.That(fixture.Buffers.CurrentProgressReadSink.Shop.PurchasedMask, Is.Zero);
            Assert.That(fixture.Core.Shop.Purchase(new ShopOfferId(1)).Succeeded, Is.True);
            Assert.That(fixture.Buffers.CharacterReadSink.Owned.Count(x => x.CharacterKey == EntityKey.Unit_Demon), Is.EqualTo(1));
            Assert.That(fixture.Core.Shop.Purchase(new ShopOfferId(1)).FailureReason, Is.EqualTo(ShopFailureReason.SoldOut));
            Assert.That(fixture.Buffers.CurrencyReadSink.Amount, Is.Zero);
        }

        private static (IInGameCoreSystemGroupService Core, IBufferQuery Buffers) Create(
            CharacterRosterDatabase database = null)
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var core = InGameCoreSystemFactory.Create();
            core.ExportServices(builder);
            var services = builder.BuildSystemReader().ServiceStoreReader;
            ((ISystemInitData)core).InitData(database ?? new CharacterRosterDatabase());
            ((ISystemPostBindService)core).PostBindService(services);
            return ((IInGameCoreSystemGroupService)core, services.GetService(BufferSystemKey.Buffer));
        }
    }
}
