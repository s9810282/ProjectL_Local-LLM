using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TrainingGroundPurchaseTests
    {
        [Test]
        public void SpendingOverflow_RejectsBeforeInstallingRankOrReceipt()
        {
            using var game = new TrainingGroundTestRuntime(gold: int.MaxValue);
            var store = (ProjectL.Systems.RuntimeSystem.BufferSystems.Currency.ICurrencyBufferStore)game.Buffers.CurrencyReadSink;
            store.InstallAmount(store.PrepareAmount(int.MaxValue, 0));
            store.InstallAmount(store.PrepareAmount(0, 100));
            game.Enter();
            var before = new CoreIntegrationSnapshot(game.Buffers);
            Assert.Throws<System.OverflowException>(() => game.Core.Shop.PurchaseTraining(game.Context, game.TargetId, TraitKey.Vanguard, 1));
            before.AssertUnchanged(game.Buffers);
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(int.MaxValue));
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop.TrainingReceipt, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActiveTrainingVisit_PricesRemovedAfterEntryStillAllowsPublishedTradeAndExit(bool purchase)
        {
            using var game = new TrainingGroundTestRuntime();
            game.Enter();
            var asset = UnityEngine.ScriptableObject.CreateInstance<ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement.EnhancementStageData>();
            try
            {
                game.Database.SetStage(new ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item.StageRootData(
                    1202, 1, ProjectL.Systems.DataBaseSystem.Data.Common.StageMapRole.Regular,
                    (ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement.EnhancementStageDetail)asset.GetData()));
                if (purchase)
                {
                    var result = game.Core.Shop.PurchaseTraining(game.Context, game.TargetId, TraitKey.Vanguard, 1);
                    Assert.That(result.Succeeded, Is.True);
                    Assert.That(result.Training.Price, Is.EqualTo(10));
                }
                Assert.That(game.Core.Shop.CompleteShop(game.Context).Accepted, Is.True);
                Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(purchase ? 10 : 0));
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        [TestCase("default", 1)]
        [TestCase("acquired", 1)]
        public void ExistingOwnershipSources_TrainWithoutConsumingSelection(string source, int rank)
        {
            using var game = new TrainingGroundTestRuntime(source: source);
            var before = game.Owned(game.TargetId);
            game.Enter();
            var result = game.Core.Shop.PurchaseTraining(game.Context, game.TargetId, TraitKey.Vanguard, rank);
            Assert.That(result.Succeeded, Is.True, result.FailureReason.ToString());
            Assert.That(game.Owned(game.TargetId).Growth.Selections, Is.EqualTo(before.Growth.Selections));
            Assert.That(game.Owned(game.TargetId).Growth.Purchases[TraitKey.Vanguard], Is.EqualTo(1));
            Assert.That(result.Training.FinalRank, Is.EqualTo(rank + 1));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(rank == 1 ? 10 : 20));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActiveTrainingVisit_PinsPricesAndAlwaysAllowsExitAfterAuthoringChange(bool purchase)
        {
            using var game = new TrainingGroundTestRuntime();
            game.Enter();
            game.Database.SetStage(new ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item.StageRootData(
                1202, 1, ProjectL.Systems.DataBaseSystem.Data.Common.StageMapRole.Regular,
                new ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement.EnhancementStageDetail(new TrainingPrices(15, 35))));
            if (purchase)
            {
                var result = game.Core.Shop.PurchaseTraining(game.Context, game.TargetId, TraitKey.Vanguard, 1);
                Assert.That(result.Succeeded, Is.True, result.FailureReason.ToString());
                Assert.That(result.Training.Price, Is.EqualTo(10));
            }
            Assert.That(game.Core.Shop.CompleteShop(game.Context).Accepted, Is.True);
        }

        [Test]
        public void OffDeckPurchaseThenMainPurchase_PreservesHistoriesAndChargesEachVisitOnce()
        {
            using var game = new TrainingGroundTestRuntime();
            game.Enter();
            var target = game.Owned(game.TargetId);
            var sibling = game.Owned(game.SiblingId);
            var main = game.Owned(game.MainId);
            long revision = game.Buffers.CharacterReadSink.RosterRevision;
            var first = game.Core.Shop.PurchaseTraining(game.Context, game.TargetId, TraitKey.Vanguard, 1);
            Assert.That(first.Succeeded, Is.True);
            AssertReceipt(first, game.TargetId, 1, 2, 10);
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop.TrainingReceipt, Is.SameAs(first.Training));
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop.HasPurchase, Is.True);
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop.TrainingChoices.All(c => !c.CanPurchase), Is.True);
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(90));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(10));
            Assert.That(game.Buffers.CharacterReadSink.RosterRevision, Is.EqualTo(revision + 1));
            Assert.That(game.Owned(game.TargetId).Growth.Purchases[TraitKey.Vanguard], Is.EqualTo(1));
            Assert.That(game.Owned(game.TargetId).Growth.Selections, Is.EqualTo(target.Growth.Selections));
            Assert.That((game.Owned(game.TargetId).Level, game.Owned(game.TargetId).Experience), Is.EqualTo((target.Level, target.Experience)));
            Assert.That(game.Owned(game.SiblingId), Is.SameAs(sibling));
            Assert.That(game.Owned(game.MainId), Is.SameAs(main));
            AssertRejectedWithoutMutation(game, game.TargetId, TraitKey.Vanguard, 1);
            AssertRejectedWithoutMutation(game, game.MainId, TraitKey.Vanguard, 2);
            Assert.That(game.Core.Shop.CompleteShop(game.Context).Accepted, Is.True);
            game.Enter(3);
            var second = game.Core.Shop.PurchaseTraining(game.Context, game.MainId, TraitKey.Vanguard, 2);
            Assert.That(second.Succeeded, Is.True);
            AssertReceipt(second, game.MainId, 2, 3, 20);
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(70));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(30));
            Assert.That(game.Buffers.CharacterReadSink.RosterRevision, Is.EqualTo(revision + 2));
            Assert.That(game.Owned(game.MainId).Growth.Purchases[TraitKey.Vanguard], Is.EqualTo(2));
            Assert.That(game.Owned(game.MainId).Growth.Selections, Is.EqualTo(main.Growth.Selections));
            Assert.That(game.Core.Shop.CompleteShop(game.Context).Accepted, Is.True);
        }

        [Test]
        public void TrainingCommand_PublishesReceiptAndCompletionWithoutReplayingCommandNumber()
        {
            using var game = new TrainingGroundTestRuntime();
            game.Enter();
            long revision = game.Buffers.CharacterReadSink.RosterRevision;
            var result = game.Publish(ShopViewCommandData.PurchaseTraining(game.Context, game.TargetId, TraitKey.Vanguard, 1));
            Assert.That(result.Accepted, Is.True);
            AssertReceipt(result.PurchaseResult, game.TargetId, 1, 2, 10);
            Assert.That(result.CompletionResult.Accepted, Is.True);
            Assert.That(game.Core.StageCommands.ProcessNextShopCommand(result.RequestNumber), Is.False);
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(10));
            Assert.That(game.Buffers.CharacterReadSink.RosterRevision, Is.EqualTo(revision + 1));
            Assert.That(game.Buffers.StageCommandResults.HandledShopCommandNumber, Is.EqualTo(result.RequestNumber));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void InvalidTrainingTargetOrRank_LeavesVisitAvailable(int invalid)
        {
            using var game = new TrainingGroundTestRuntime();
            game.Enter();
            var target = invalid == 0 ? new CharacterInstanceId(99999) : invalid == 4 ? game.CappedId : game.TargetId;
            var trait = invalid == 1 ? TraitKey.StationaryFire : invalid == 2 ? TraitKey.Knockback : TraitKey.Vanguard;
            int rank = invalid == 3 ? 2 : invalid == 4 ? 3 : 1;
            AssertRejectedWithoutMutation(game, target, trait, rank);
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop.HasPurchase, Is.False);
            Assert.That(game.Core.Shop.PurchaseTraining(game.Context, game.TargetId, TraitKey.Vanguard, 1).Succeeded, Is.True);
        }

        [Test]
        public void UnaffordableTraining_DoesNotSpendOrConsumeVisit()
        {
            using var game = new TrainingGroundTestRuntime(gold: 9);
            game.Enter();
            var result = AssertRejectedWithoutMutation(game, game.TargetId, TraitKey.Vanguard, 1);
            Assert.That(result.FailureReason, Is.EqualTo(ShopFailureReason.InsufficientFunds));
            Assert.That(game.Core.Shop.CompleteShop(game.Context).Accepted, Is.True);
        }

        [Test]
        public void PreviousVisitCommand_CannotSpendInNextVisit()
        {
            using var game = new TrainingGroundTestRuntime();
            game.Enter();
            var old = game.Context;
            Assert.That(game.Core.Shop.CompleteShop(old).Accepted, Is.True);
            game.Enter(3);
            var shop = game.Buffers.CurrentProgressReadSink.Shop;
            long revision = game.Buffers.CharacterReadSink.RosterRevision;
            var result = game.Publish(ShopViewCommandData.PurchaseTraining(old, game.TargetId, TraitKey.Vanguard, 1));
            Assert.That(result.PurchaseResult.Succeeded, Is.False);
            Assert.That(result.CompletionResult, Is.Null);
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop, Is.SameAs(shop));
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(100));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.Zero);
            Assert.That(game.Buffers.CharacterReadSink.RosterRevision, Is.EqualTo(revision));
            Assert.That(game.Core.Shop.PurchaseTraining(game.Context, game.TargetId, TraitKey.Vanguard, 1).Succeeded, Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void PreparedTraining_RevalidatesChangedWalletRosterOrVisit(int changed)
        {
            using var game = new TrainingGroundTestRuntime();
            game.Enter();
            var prepared = new TrainingPurchasePreparation(game.Context, game.Buffers.CurrentProgressReadSink.Shop,
                100, game.Buffers.CharacterReadSink.RosterRevision, game.TargetId, TraitKey.Vanguard, 1);
            if (changed == 0) game.Buffers.CurrencyWriteSink.SetAmount(99);
            else if (changed == 1) Assert.That(game.Buffers.CharacterWriteSink.Acquire(new[]
                { new RosterAcquireRequest(EntityKey.Unit_Sniper, CharacterOwnershipRole.Sub) }).Succeeded, Is.True);
            else
            {
                Assert.That(game.Core.Shop.CompleteShop(game.Context).Accepted, Is.True);
                game.Enter(3);
            }
            var before = game.Buffers.CharacterReadSink.Owned.ToArray();
            var shop = game.Buffers.CurrentProgressReadSink.Shop;
            int amount = game.Buffers.CurrencyReadSink.Amount;
            long revision = game.Buffers.CharacterReadSink.RosterRevision;
            var result = game.Buffers.ShopPurchaseWriteSink.PublishTraining(prepared);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(game.Buffers.CharacterReadSink.Owned, Is.EqualTo(before));
            Assert.That(game.Buffers.CharacterReadSink.RosterRevision, Is.EqualTo(revision));
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop, Is.SameAs(shop));
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(amount));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.Zero);
            Assert.That(shop.HasPurchase, Is.False);
            Assert.That(shop.TrainingReceipt, Is.Null);
        }

        private static ShopPurchaseResult AssertRejectedWithoutMutation(TrainingGroundTestRuntime game,
            CharacterInstanceId target, TraitKey trait, int rank)
        {
            var owned = game.Buffers.CharacterReadSink.Owned.ToArray();
            long revision = game.Buffers.CharacterReadSink.RosterRevision;
            int gold = game.Buffers.CurrencyReadSink.Amount;
            int spent = game.Buffers.CurrencyReadSink.RunSpent;
            var shop = game.Buffers.CurrentProgressReadSink.Shop;
            var result = game.Core.Shop.PurchaseTraining(game.Context, target, trait, rank);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureReason, Is.Not.EqualTo(ShopFailureReason.None));
            Assert.That(game.Buffers.CharacterReadSink.Owned, Is.EqualTo(owned));
            Assert.That(game.Buffers.CharacterReadSink.RosterRevision, Is.EqualTo(revision));
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(gold));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(spent));
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop, Is.SameAs(shop));
            return result;
        }

        private static void AssertReceipt(ShopPurchaseResult result, CharacterInstanceId id, int previous, int final, int price)
        {
            Assert.That(result.Training, Is.Not.Null);
            Assert.That((result.Training.OwnedId, result.Training.TraitKey, result.Training.PreviousRank,
                result.Training.FinalRank, result.Training.Price), Is.EqualTo((id, TraitKey.Vanguard, previous, final, price)));
        }
    }
}
