using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TrainingGroundCommandTests
    {
        private static ShopVisitContext Context => new(1, new StageNodeId(2), new StageKey(1202),
            new StageEntryId(new Guid("034c0995-4eb8-4c90-8b5d-46d0eef6285a")));
        private static CharacterInstanceId OwnedId => new(7);

        [TestCase(true)]
        [TestCase(false)]
        public void TrainingCommand_DispatchesTargetAndCompletesOnlyAfterSuccess(bool succeeds)
        {
            var service = new ShopStub { Succeeds = succeeds };
            var input = new ShopViewBuffer();
            var results = new CurrentProgressBuffer();
            var executor = new ShopCommandExecutor(service);
            executor.Bind(input, results, results);
            int request = input.Submit(ShopViewCommandData.PurchaseTraining(Context, OwnedId, TraitKey.Taunt, 2));

            Assert.That(executor.ProcessNext(request), Is.True);
            Assert.That(service.TrainingCalls, Is.EqualTo(1));
            Assert.That(service.OrdinaryCalls, Is.Zero);
            Assert.That(service.CompleteCalls, Is.EqualTo(succeeds ? 1 : 0));
            Assert.That(service.Target, Is.EqualTo((Context, OwnedId, TraitKey.Taunt, 2)));
            Assert.That(results.TryGetShopResult(out var result), Is.True);
            Assert.That(result.Kind, Is.EqualTo(ShopCommandKind.PurchaseTraining));
            Assert.That(result.OfferId.IsEmpty, Is.True);
            Assert.That((result.OwnedId, result.TraitKey, result.ExpectedRank), Is.EqualTo((OwnedId, TraitKey.Taunt, 2)));
            Assert.That(result.Accepted, Is.EqualTo(succeeds));
            Assert.That(result.PurchaseResult, Is.SameAs(service.PurchaseResult));
            Assert.That(result.CompletionResult != null, Is.EqualTo(succeeds));
            Assert.That(executor.ProcessNext(request), Is.False);
            Assert.That(service.TrainingCalls, Is.EqualTo(1));
        }

        [Test]
        public void CompletionFailure_PreservesAcceptedTrainingReceipt()
        {
            var service = new ShopStub { Succeeds = true, ThrowOnComplete = true };
            var input = new ShopViewBuffer();
            var results = new CurrentProgressBuffer();
            var executor = new ShopCommandExecutor(service);
            executor.Bind(input, results, results);
            int request = input.Submit(ShopViewCommandData.PurchaseTraining(Context, OwnedId, TraitKey.Taunt, 2));

            Assert.Throws<InvalidOperationException>(() => executor.ProcessNext(request));
            Assert.That(results.TryGetShopResult(out var result), Is.True);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.CompletionResult, Is.Null);
            Assert.That(result.PurchaseResult, Is.SameAs(service.PurchaseResult));
            Assert.That(executor.ProcessNext(request), Is.False);
            Assert.That(service.TrainingCalls, Is.EqualTo(1));
        }

        [Test]
        public void CommandValidation_PreservesOrdinaryGuardsAndRequiresTrainingTarget()
        {
            Assert.Throws<ArgumentException>(() => ShopViewCommandData.Purchase(Context, ShopOfferId.Empty));
            Assert.Throws<ArgumentException>(() => ShopViewCommandData.PurchaseTraining(Context, default, TraitKey.Taunt, 1));
            Assert.Throws<ArgumentException>(() => ShopViewCommandData.PurchaseTraining(Context, OwnedId, TraitKey.None, 1));
            Assert.Throws<ArgumentException>(() => ShopViewCommandData.PurchaseTraining(Context, OwnedId, (TraitKey)99999, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ShopViewCommandData.PurchaseTraining(Context, OwnedId, TraitKey.Taunt, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ShopViewCommandData.PurchaseTraining(Context, OwnedId, TraitKey.Taunt, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => ShopViewCommandData.PurchaseTraining(default, OwnedId, TraitKey.Taunt, 1));
            Assert.That(ShopViewCommandData.Complete(Context).Kind, Is.EqualTo(ShopViewCommandKind.Complete));
        }

        private sealed class ShopStub : IShopService
        {
            internal bool Succeeds;
            internal bool ThrowOnComplete;
            internal int TrainingCalls;
            internal int OrdinaryCalls;
            internal int CompleteCalls;
            internal (ShopVisitContext, CharacterInstanceId, TraitKey, int) Target;
            internal ShopPurchaseResult PurchaseResult;

            public ChapterProgressionResult EnterShop(int chapterKey, StageNodeId targetNodeId, int seed)
                => throw new NotSupportedException();

            public ShopPurchaseResult Purchase(ShopVisitContext context, ShopOfferId offerId)
            {
                OrdinaryCalls++;
                throw new InvalidOperationException("훈련 요청은 일반 상품 구매로 전달하면 안 됩니다.");
            }

            public ShopPurchaseResult PurchaseTraining(ShopVisitContext context, CharacterInstanceId ownedId,
                TraitKey traitKey, int expectedRank)
            {
                TrainingCalls++;
                Target = (context, ownedId, traitKey, expectedRank);
                PurchaseResult = Succeeds
                    ? ShopPurchaseResult.ForTraining(new TrainingReceiptSnapshot(ownedId, traitKey, expectedRank, 20))
                    : ShopPurchaseResult.Failure(ShopFailureReason.InsufficientFunds);
                return PurchaseResult;
            }

            public ChapterProgressionResult CompleteShop(ShopVisitContext context)
            {
                CompleteCalls++;
                if (ThrowOnComplete) throw new InvalidOperationException("완료 오류 주입");
                return ChapterProgressionResult.FinishStage(context.ChapterKey, context.NodeId, context.StageKey, false);
            }
        }
    }
}
