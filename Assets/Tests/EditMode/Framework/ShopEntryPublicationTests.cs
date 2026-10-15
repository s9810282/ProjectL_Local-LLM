using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class ShopEntryPublicationTests
    {
        private IBufferQuery _buffers;

        [SetUp]
        public void SetUp()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            _buffers = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            _buffers.MapProgressWriteSink.RegisterMap(1);
            _buffers.CurrentProgressWriteSink.SelectMap(1);
            Register();
            _buffers.CharacterWriteSink.Acquire(new[] { new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main) },
                CharacterBufferTestData.MainState(7));
            _buffers.CurrencyWriteSink.SetAmount(50);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Publication_InstallsDetachedShopAndClearsOldRecovery(bool recovery)
        {
            if (recovery) SelectRecovery();
            var offers = Offers();
            var preparation = Preparation(offers);
            offers[0] = new ShopPurchaseSnapshot(new ShopOfferId(1), EntityKey.Unit_Sniper, 99);
            var before = new RecoveryBufferSnapshot(_buffers);
            Publish(preparation);
            var current = _buffers.CurrentProgressReadSink;
            Assert.That(current.CurrentStage.NodeId, Is.EqualTo(3));
            Assert.That(current.CurrentStage.StageKey, Is.EqualTo(100));
            Assert.That(current.CurrentStage.StageName, Is.EqualTo("Shop"));
            Assert.That(current.Shop.Offers.Select(x => x.Cost), Is.EqualTo(new[] { 5, 5, 5 }));
            Assert.That(current.Shop.Offers.Select(x => x.CharacterKey), Is.All.EqualTo(EntityKey.Unit_Akimbo));
            Assert.That(current.Shop.PurchasedMask, Is.Zero);
            Assert.That(current.Recovery, Is.Null);
            before.AssertUnchanged(_buffers, selection: false);
        }

        [TestCase("chapter")]
        [TestCase("presence")]
        [TestCase("node")]
        [TestCase("key")]
        [TestCase("name")]
        [TestCase("binding")]
        [TestCase("missing-target")]
        [TestCase("cleared-target")]
        [TestCase("cleared-map")]
        [TestCase("graph-type")]
        [TestCase("graph-boss")]
        [TestCase("graph-invalid-role")]
        public void Publication_RejectsStaleOrInvalidStorageWithoutWrites(string fault)
        {
            SelectRecovery();
            var preparation = Preparation();
            switch (fault)
            {
                case "chapter": _buffers.CurrentProgressWriteSink.SelectMap(2); break;
                case "presence": _buffers.CurrentProgressWriteSink.Clear(); _buffers.CurrentProgressWriteSink.SelectMap(1); break;
                case "node": _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(4), new StageKey(20), "Recovery"); break;
                case "key": _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(21), "Recovery"); break;
                case "name": _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(20), "Renamed"); break;
                case "binding": Register(101); break;
                case "missing-target": _buffers.StageProgressWriteSink.Clear(); break;
                case "cleared-target": _buffers.StageProgressWriteSink.SetClear(1, 3, true); break;
                case "cleared-map": _buffers.MapProgressWriteSink.SetClear(1, true); break;
                case "graph-type": Register(type: StageType.Event); break;
                case "graph-boss": Register(role: StageMapRole.Boss); break;
                case "graph-invalid-role": Register(role: (StageMapRole)99); break;
            }
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => Publish(preparation));
            before.AssertUnchanged(_buffers);
        }

        [Test]
        public void Publication_SameSelectedTargetPreservesPurchasedSlots()
        {
            _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(3), new StageKey(100), "Shop");
            _buffers.CurrentProgressWriteSink.SetShopOffers(Offers());
            var store = (ICurrentProgressBufferStore)_buffers.CurrentProgressReadSink;
            store.InstallPurchase(store.PreparePurchase(new StageNodeId(3), new StageKey(100), new ShopOfferId(1)));
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => Publish(Preparation()));
            before.AssertUnchanged(_buffers);
        }

        [Test]
        public void Publication_NullPreparationRejectsWithoutWrites()
        {
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<ArgumentNullException>(() => Publish(null));
            before.AssertUnchanged(_buffers);
        }

        [TestCase("null-offers")]
        [TestCase("wrong-count")]
        [TestCase("null-offer")]
        [TestCase("slot-order")]
        [TestCase("chapter")]
        [TestCase("node")]
        [TestCase("key")]
        [TestCase("name")]
        [TestCase("invalid-origin")]
        [TestCase("unexpected-origin")]
        public void Preparation_RejectsMalformedValuesBeforePublication(string fault)
        {
            var before = new RecoveryBufferSnapshot(_buffers);
            var offers = Offers();
            int chapter = 1, node = 3, key = 100;
            string name = "Shop";
            bool selected = false;
            StageSelectionData origin = default;
            switch (fault)
            {
                case "null-offers": offers = null; break;
                case "wrong-count": offers = Array.Empty<ShopPurchaseSnapshot>(); break;
                case "null-offer": offers[1] = null; break;
                case "slot-order": offers[1] = offers[0]; break;
                case "chapter": chapter = 0; break;
                case "node": node = 0; break;
                case "key": key = 0; break;
                case "name": name = " "; break;
                case "invalid-origin": selected = true; break;
                case "unexpected-origin": origin = new StageSelectionData(2, 20, "Recovery"); break;
            }
            Assert.Catch<ArgumentException>(() => CreatePreparation(chapter, selected, origin, node, key, name, offers));
            before.AssertUnchanged(_buffers);
        }

        private ShopEntryPreparation Preparation(ShopPurchaseSnapshot[] offers = null)
            => CreatePreparation(_buffers.CurrentProgressReadSink.CurrentMapKey, _buffers.CurrentProgressReadSink.HasSelection,
                _buffers.CurrentProgressReadSink.CurrentStage, 3, 100, "Shop", offers ?? Offers());

        private static ShopEntryPreparation CreatePreparation(int chapter, bool selected, StageSelectionData origin, int node, int key,
            string name, ShopPurchaseSnapshot[] offers)
            => new ShopEntryPreparation(chapter, selected, origin, new StageNodeId(node), new StageKey(key), name, offers,
                StageType.Shop, StageMapRole.Regular);

        private void Publish(ShopEntryPreparation preparation)
            => _buffers.ShopStageWriteSink.PrepareShop(preparation);

        private static ShopPurchaseSnapshot[] Offers() => Enumerable.Range(1, 3).Select(i =>
            new ShopPurchaseSnapshot(new ShopOfferId(i), EntityKey.Unit_Akimbo, 5)).ToArray();
        private void SelectRecovery()
        {
            _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(20), "Recovery");
            _buffers.CurrentProgressWriteSink.SetRecoveryOptions(new[] {
                new RecoveryOptionSpec(new RecoveryOptionId(1), 0, new RecoveryHealSpec(RecoveryHealMode.Full, 0)) });
        }
        private void Register(int key = 100, StageType type = StageType.Shop, StageMapRole role = StageMapRole.Regular)
        {
            _buffers.StageProgressWriteSink.Clear();
            _buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 20, 1, 0, new[] { 3 }, true, StageType.Event),
                new StageProgressData(3, key, 2, 0, Array.Empty<int>(), false, type, role) });
        }
    }
}
