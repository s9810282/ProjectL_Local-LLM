using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Framework
{
    public sealed partial class ShopStageServiceTests
    {
        [Test]
        public void Purchase_ExplicitIntentUsesFrozenOfferAndNewOwnedIdentity()
        {
            PrepareShopOperations();
            var owned = _buffers.CharacterReadSink.Owned.ToArray();
            var souls = owned.Select(x => _buffers.CharacterReadSink.GetSoulCount(x.Id)).ToArray();
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            _db.Stages[100] = ShopStageTestDatabase.Shop(new ShopCandidate(EntityKey.Unit_Demon, 99));
            _db.UnitReads.Clear();
            var result = Buy();
            Assert.That(result.Succeeded, Is.True);
            var acquired = result.Acquisition.Acquired.Single();
            Assert.That(acquired.CharacterKey, Is.EqualTo(EntityKey.Unit_Akimbo));
            Assert.That(acquired.Role, Is.EqualTo(CharacterOwnershipRole.Sub));
            Assert.That(owned.Any(x => x.Id == acquired.Id), Is.False);
            Assert.That(_buffers.CharacterReadSink.GetSoulCount(acquired.Id), Is.Zero);
            Assert.That(_buffers.CharacterReadSink.Owned.Count, Is.EqualTo(5));
            Assert.That(owned.Select(x => _buffers.CharacterReadSink.GetSoulCount(x.Id)), Is.EqualTo(souls));
            _buffers.CharacterReadSink.TryGetMainState(out var afterMain);
            Assert.That(afterMain, Is.SameAs(main));
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(45));
            Assert.That(_buffers.CurrentProgressReadSink.Shop.PurchasedMask, Is.EqualTo(1));
            Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 2), Is.False);
            Assert.That(_db.UnitReads, Is.EqualTo(new[] { EntityKey.Unit_Akimbo }));
        }

        [TestCase("unknown", ShopFailureReason.UnknownOffer)]
        [TestCase("funds", ShopFailureReason.InsufficientFunds)]
        [TestCase("sold", ShopFailureReason.SoldOut)]
        public void Purchase_RuleRejectionAddsNoWrites(string fault, ShopFailureReason expected)
        {
            PrepareShopOperations();
            if (fault == "funds") _buffers.CurrencyWriteSink.SetAmount(4);
            if (fault == "sold") Assert.That(Buy().Succeeded, Is.True);
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.That(Buy(offer: fault == "unknown" ? 99 : 1).FailureReason, Is.EqualTo(expected));
            before.AssertUnchanged(_buffers);
        }

        [TestCase("chapter", ShopFailureReason.EntryMismatch, ChapterProgressionFailureReason.ChapterMismatch)]
        [TestCase("node", ShopFailureReason.EntryMismatch, ChapterProgressionFailureReason.StageMismatch)]
        [TestCase("binding", ShopFailureReason.EntryMismatch, ChapterProgressionFailureReason.StageMismatch)]
        [TestCase("missing-selection", ShopFailureReason.WrongPhase, ChapterProgressionFailureReason.StageNotFound)]
        [TestCase("missing-node", ShopFailureReason.EntryMismatch, ChapterProgressionFailureReason.StageNotFound)]
        [TestCase("map-clear", ShopFailureReason.StaleEntry, ChapterProgressionFailureReason.ChapterCompleted)]
        [TestCase("node-clear", ShopFailureReason.StaleEntry, ChapterProgressionFailureReason.StageAlreadyCleared)]
        public void Operations_InvalidIdentityOrProgressPreserveFacts(string fault, ShopFailureReason purchase,
            ChapterProgressionFailureReason completion)
        {
            PrepareShopOperations();
            if (fault == "binding") ReplaceShopNode(key: 101);
            if (fault == "missing-selection") { _buffers.CurrentProgressWriteSink.Clear(); _buffers.CurrentProgressWriteSink.SelectMap(1); }
            if (fault == "missing-node") _buffers.StageProgressWriteSink.Clear();
            if (fault == "map-clear") _buffers.MapProgressWriteSink.SetClear(1, true);
            if (fault == "node-clear") _buffers.StageProgressWriteSink.SetClear(1, 2, true);
            int chapter = fault == "chapter" ? 2 : 1, node = fault == "node" ? 3 : 2;
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.That(Buy(chapter, node).FailureReason, Is.EqualTo(purchase));
            Assert.That(FinishShop(chapter, node).FailureReason, Is.EqualTo(completion));
            before.AssertUnchanged(_buffers);
        }

        [TestCase("unprepared")]
        [TestCase("graph-type")]
        [TestCase("graph-boss")]
        [TestCase("graph-invalid-role")]
        [TestCase("root-type")]
        [TestCase("root-boss")]
        [TestCase("root-key")]
        [TestCase("root-map")]
        public void Operations_InvalidPhaseRejectsWithoutWrites(string fault)
        {
            PrepareShopOperations();
            if (fault == "unprepared")
            {
                _buffers.CurrentProgressWriteSink.Clear(); _buffers.CurrentProgressWriteSink.SelectMap(1);
                _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(100), "Unprepared");
            }
            if (fault == "graph-type") ReplaceShopNode(type: StageType.Event);
            if (fault == "graph-boss") ReplaceShopNode(role: StageMapRole.Boss);
            if (fault == "graph-invalid-role") ReplaceShopNode(role: (StageMapRole)99);
            if (fault.StartsWith("root-")) _db.Stages[100] = new StageRootData(fault == "root-key" ? 101 : 100,
                fault == "root-map" ? 2 : 1, fault == "root-boss" ? StageMapRole.Boss : StageMapRole.Regular,
                fault == "root-type" ? new EventStageDetail(new[] { 1 }) : ShopStageTestDatabase.Shop().StageData);
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.That(Buy().FailureReason, Is.EqualTo(ShopFailureReason.WrongPhase));
            Assert.Throws<InvalidOperationException>(() => FinishShop());
            before.AssertUnchanged(_buffers);
        }

        [TestCase("node", ShopFailureReason.EntryMismatch)]
        [TestCase("key", ShopFailureReason.EntryMismatch)]
        [TestCase("shop", ShopFailureReason.StaleEntry)]
        [TestCase("clear", ShopFailureReason.StaleEntry)]
        [TestCase("map", ShopFailureReason.StaleEntry)]
        [TestCase("type", ShopFailureReason.WrongPhase)]
        [TestCase("boss", ShopFailureReason.WrongPhase)]
        [TestCase("active-battle", ShopFailureReason.WrongPhase)]
        [TestCase("terminal-battle", ShopFailureReason.WrongPhase)]
        public void Purchase_StateChangesDuringFinalUnitLookupAddNoWrites(string change, ShopFailureReason expected)
        {
            PrepareShopOperations();
            RecoveryBufferSnapshot afterMutation = null;
            _db.OnUnitRead = _ => { MutateShopOperation(change); afterMutation = new RecoveryBufferSnapshot(_buffers); };
            Assert.That(Buy().FailureReason, Is.EqualTo(expected));
            Assert.That(afterMutation, Is.Not.Null);
            afterMutation.AssertUnchanged(_buffers);
        }

        [TestCase("node", ShopFailureReason.EntryMismatch)]
        [TestCase("shop", ShopFailureReason.StaleEntry)]
        [TestCase("active-battle", ShopFailureReason.WrongPhase)]
        public void Purchase_StateChangesDuringRootLookupAddNoWrites(string change, ShopFailureReason expected)
        {
            PrepareShopOperations();
            RecoveryBufferSnapshot afterMutation = null;
            _db.OnStageRead = _ => { MutateShopOperation(change); afterMutation = new RecoveryBufferSnapshot(_buffers); };
            Assert.That(Buy().FailureReason, Is.EqualTo(expected));
            afterMutation.AssertUnchanged(_buffers);
        }

        [TestCase("root")]
        [TestCase("unit")]
        public void Purchase_LookupFailureAddsNoWrites(string fault)
        {
            PrepareShopOperations();
            if (fault == "root") _db.OnStageRead = _ => throw new InvalidOperationException("상점 정의 조회 실패");
            else _db.OnUnitRead = _ => throw new InvalidOperationException("유닛 정의 조회 실패");
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => Buy());
            before.AssertUnchanged(_buffers);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void CompleteShop_AllowsZeroThroughThreePurchases(int count)
        {
            PrepareShopOperations();
            for (int slot = 1; slot <= count; slot++) Assert.That(Buy(offer: slot).Succeeded, Is.True);
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(50 - 5 * count));
            Assert.That(_buffers.CharacterReadSink.Owned.Count, Is.EqualTo(4 + count));
            Assert.That(_buffers.CurrentProgressReadSink.Shop.PurchasedMask, Is.EqualTo((1 << count) - 1));
            Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 2), Is.False);
            var before = new RecoveryBufferSnapshot(_buffers);
            var nodes = _buffers.StageProgressReadSink.GetStages(1).ToArray();
            AssertShopCompletedOnlyTarget(FinishShop(), nodes);
            before.AssertUnchanged(_buffers, progress: false);
            var committed = new RecoveryBufferSnapshot(_buffers);
            Assert.That(FinishShop().FailureReason, Is.EqualTo(ChapterProgressionFailureReason.StageAlreadyCleared));
            Assert.That(Buy().FailureReason, Is.EqualTo(ShopFailureReason.StaleEntry));
            committed.AssertUnchanged(_buffers);
        }

        [Test]
        public void CompleteShop_AfterUnaffordablePurchaseAllowsExit()
        {
            PrepareShopOperations(); _buffers.CurrencyWriteSink.SetAmount(0);
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.That(Buy().FailureReason, Is.EqualTo(ShopFailureReason.InsufficientFunds));
            before.AssertUnchanged(_buffers);
            AssertShopCompletedOnlyTarget(FinishShop(), _buffers.StageProgressReadSink.GetStages(1).ToArray());
            before.AssertUnchanged(_buffers, progress: false);
        }

        [Test]
        public void CompleteShop_PreWriteFailureRetainsPurchaseAndRetriesOnlyCompletion()
        {
            PrepareShopOperations();
            var bought = Buy(); Assert.That(bought.Succeeded, Is.True);
            var acquired = bought.Acquisition.Acquired.Single();
            var receipt = _buffers.CurrentProgressReadSink.Shop;
            var committed = new RecoveryBufferSnapshot(_buffers);
            _db.OnStageRead = _ => throw new InvalidOperationException("완료 전 조회 실패");
            Assert.Throws<InvalidOperationException>(() => FinishShop());
            committed.AssertUnchanged(_buffers);
            Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 2), Is.False);
            _db.OnStageRead = null;
            AssertShopCompletedOnlyTarget(FinishShop(), _buffers.StageProgressReadSink.GetStages(1).ToArray());
            committed.AssertUnchanged(_buffers, progress: false);
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(45));
            Assert.That(_buffers.CharacterReadSink.Owned.Count, Is.EqualTo(5));
            Assert.That(_buffers.CharacterReadSink.Owned.Count(x => x.Id == acquired.Id), Is.EqualTo(1));
            Assert.That(_buffers.CurrentProgressReadSink.Shop, Is.SameAs(receipt));
            Assert.That(receipt.PurchasedMask, Is.EqualTo(1));
            var completed = new RecoveryBufferSnapshot(_buffers);
            Assert.That(FinishShop().FailureReason, Is.EqualTo(ChapterProgressionFailureReason.StageAlreadyCleared));
            completed.AssertUnchanged(_buffers);
        }

        [Test]
        public void CompleteShop_AllowsSuccessorAndRejectsOldIntentWithSameStageKey()
        {
            PrepareShopOperations(); Assert.That(FinishShop().Accepted, Is.True);
            Assert.That(Enter(node: 3).Accepted, Is.True);
            var successor = new RecoveryBufferSnapshot(_buffers);
            Assert.That(Buy(node: 2).FailureReason, Is.EqualTo(ShopFailureReason.EntryMismatch));
            Assert.That(FinishShop(node: 2).FailureReason, Is.EqualTo(ChapterProgressionFailureReason.StageMismatch));
            successor.AssertUnchanged(_buffers);
            Assert.That(FinishShop(node: 3).Accepted, Is.True);
            Assert.That(_buffers.MapProgressReadSink.GetMap(1).IsClear, Is.False);
        }

        [TestCase("node", ChapterProgressionFailureReason.StageMismatch)]
        [TestCase("key", ChapterProgressionFailureReason.StageMismatch)]
        [TestCase("clear", ChapterProgressionFailureReason.StageAlreadyCleared)]
        [TestCase("map", ChapterProgressionFailureReason.ChapterCompleted)]
        [TestCase("shop", ChapterProgressionFailureReason.StageMismatch)]
        public void CompleteShop_StateChangesDuringRootLookupAddNoWrites(string change, ChapterProgressionFailureReason reason)
        {
            PrepareShopOperations(); RecoveryBufferSnapshot afterMutation = null;
            _db.OnStageRead = _ => { MutateShopOperation(change); afterMutation = new RecoveryBufferSnapshot(_buffers); };
            Assert.That(FinishShop().FailureReason, Is.EqualTo(reason));
            afterMutation.AssertUnchanged(_buffers);
        }

        [TestCase("type")]
        [TestCase("boss")]
        [TestCase("active-battle")]
        [TestCase("terminal-battle")]
        public void CompleteShop_PhaseChangesDuringRootLookupCannotClear(string change)
        {
            PrepareShopOperations(); RecoveryBufferSnapshot afterMutation = null;
            _db.OnStageRead = _ => { MutateShopOperation(change); afterMutation = new RecoveryBufferSnapshot(_buffers); };
            Assert.Throws<InvalidOperationException>(() => FinishShop());
            afterMutation.AssertUnchanged(_buffers);
        }

        [TestCase(0, 2)]
        [TestCase(1, 0)]
        public void Operations_InvalidArgumentsDoNotReadOrWrite(int chapter, int node)
        {
            PrepareShopOperations(); _db.StageReads = 0; _db.UnitReads.Clear();
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Catch<ArgumentException>(() => Buy(chapter, node));
            Assert.Catch<ArgumentException>(() => FinishShop(chapter, node));
            Assert.That(_db.StageReads, Is.Zero); Assert.That(_db.UnitReads, Is.Empty);
            before.AssertUnchanged(_buffers);
        }

        private void PrepareShopOperations() => Assert.That(Enter().Accepted, Is.True);

        private void MutateShopOperation(string change)
        {
            if (change == "node")
            {
                _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(3), new StageKey(100), "Next shop");
                _buffers.CurrentProgressWriteSink.SetShopOffers(ShopEntryOffers());
            }
            if (change == "key") ReplaceShopNode(key: 101);
            if (change == "shop")
            {
                var offers = _buffers.CurrentProgressReadSink.Shop.Offers;
                _buffers.CurrentProgressWriteSink.Clear(); _buffers.CurrentProgressWriteSink.SelectMap(1);
                _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(100), "Replacement shop");
                _buffers.CurrentProgressWriteSink.SetShopOffers(offers);
            }
            if (change == "clear") _buffers.StageProgressWriteSink.SetClear(1, 2, true);
            if (change == "map") _buffers.MapProgressWriteSink.SetClear(1, true);
            if (change == "type") ReplaceShopNode(type: StageType.Event);
            if (change == "boss") ReplaceShopNode(role: StageMapRole.Boss);
            if (change == "active-battle" || change == "terminal-battle") InstallBattle(change == "terminal-battle");
        }

        private static ShopPurchaseSnapshot[] ShopEntryOffers()
            => Enumerable.Range(1, 3).Select(i => new ShopPurchaseSnapshot(
                new ShopOfferId(i), EntityKey.Unit_Akimbo, 5)).ToArray();

        private void ReplaceShopNode(int key = 100, StageType type = StageType.Shop, StageMapRole role = StageMapRole.Regular)
        {
            var nodes = _buffers.StageProgressReadSink.GetStages(1).Select(n => n.NodeId == 2
                ? new StageProgressData(n.NodeId, key, n.Depth, n.WidthIndex, n.NextNodeIds, n.IsClear, type, role) : n).ToArray();
            _buffers.StageProgressWriteSink.Clear(); _buffers.StageProgressWriteSink.RegisterStages(1, nodes);
        }

        private void AssertShopCompletedOnlyTarget(ChapterProgressionResult result, StageProgressData[] previous)
        {
            Assert.That(result.Accepted && result.ClearStage, Is.True);
            Assert.That(result.ClearMap || result.EndAttempt || result.SelectMap || result.SelectStage, Is.False);
            Assert.That(result.NodeId.Value, Is.EqualTo(2));
            foreach (var node in previous)
            {
                var after = _buffers.StageProgressReadSink.GetStages(1).Single(x => x.NodeId == node.NodeId);
                Assert.That(after.IsClear, Is.EqualTo(node.NodeId == 2 || node.IsClear));
                Assert.That(after.StageKey, Is.EqualTo(node.StageKey));
                Assert.That(after.NextNodeIds, Is.EqualTo(node.NextNodeIds));
            }
        }

        private CharacterPurchaseResult Buy(int chapter = 1, int node = 2, int offer = 1)
            => _core.Shop.Purchase(chapter, new StageNodeId(node), new ShopOfferId(offer));

        private ChapterProgressionResult FinishShop(int chapter = 1, int node = 2)
            => _core.Shop.CompleteShop(chapter, new StageNodeId(node));
    }
}
