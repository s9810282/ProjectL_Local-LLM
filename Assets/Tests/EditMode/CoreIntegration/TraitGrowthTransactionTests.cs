using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitGrowthTransactionTests
    {
        [Test]
        public void OldRunRequest_CannotBePackagedAsCurrentRunSuccess()
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            var valid = game.Request(offer);
            var operation = game.Service.PrepareTraitGrowthChoice(valid);
            var stale = new TraitGrowthChoiceRequest(new RunId(Guid.NewGuid()), offer.OfferId, offer.OwnedId, offer.StepId, valid.TraitKey);
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(stale);
            var before = game.Current;
            long revision = game.Revision;
            Assert.Throws<ArgumentException>(() => new TraitGrowthProcessedResult(offer.RunId, number, stale,
                TraitGrowthResult.Success(operation.Decision.Change.Final)));
            Assert.That(game.Current, Is.SameAs(before));
            Assert.That(game.Revision, Is.EqualTo(revision));
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.SameAs(offer));
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult, Is.Null);
            Assert.That(game.Buffers.TraitGrowthViewReadSink.TryGetCommand(number, out var pending), Is.True);
            Assert.That(pending.Request, Is.SameAs(stale));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void PrepareFailure_LeavesOfferRecordAndReceiptUntouched(int mutation)
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            var request = game.Request(offer);
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(request);
            var op = game.Service.PrepareTraitGrowthChoice(request);
            var result = new TraitGrowthProcessedResult(op.RunId, number, request, TraitGrowthResult.Success(op.Decision.Change.Final));
            var prepared = game.Buffers.TraitGrowthOperations.PrepareChoicePublication(op.RunId, op.Revision, op.Offer, op.Decision.Change, result);
            var before = game.Current;
            if (mutation == 0)
                game.Core.Characters.AcquireSub(EntityKey.Unit_Ballista);
            else if (mutation == 1)
                ((IRunProgressBufferStore)game.Buffers.RunProgressReadSink).InstallRunId(new RunId(Guid.NewGuid()));
            else
                game.Buffers.TraitGrowthOperations.Clear();
            Assert.Throws<InvalidOperationException>(() => game.Buffers.TraitGrowthOperations.InstallChoicePublication(prepared));
            Assert.That(game.Current, Is.SameAs(before));
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult, Is.Null);
            if (mutation != 2)
                Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.SameAs(offer));
        }
        [Test]
        public void SecondChoice_ChangesNothing()
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            var request = game.Request(offer);
            var first = game.Publish(request);
            long revision = game.Revision;
            var before = game.Current;
            var op = game.Service.PrepareTraitGrowthChoice(request);
            Assert.That(op.Decision.Succeeded, Is.False);
            Assert.Throws<InvalidOperationException>(() => game.Publish(op, game.Buffers.TraitGrowthViewWriteSink.Submit(request), request));
            Assert.That(game.Current, Is.SameAs(before));
            Assert.That(game.Revision, Is.EqualTo(revision));
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult, Is.SameAs(first));
        }
        [Test]
        public void Reset_PreservesAllocationAndRejectsOldRunAcknowledgement()
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            var request = game.Request(offer);
            var first = game.Publish(request);
            game.Buffers.RunProgressWriteSink.ResetRunProgress();
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.Null);
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult, Is.Null);
            Assert.That(game.Buffers.TraitGrowthViewReadSink.TryGetCommand(first.Number, out _), Is.False);
            ((IRunProgressBufferStore)game.Buffers.RunProgressReadSink).InstallRunId(new RunId(Guid.NewGuid()));
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(request);
            Assert.That(number, Is.GreaterThan(first.Number));
            var op = game.Service.PrepareTraitGrowthChoice(request);
            var receipt = game.Publish(op, number, request);
            Assert.That(receipt.Result.Succeeded, Is.False);
            Assert.That(receipt.Request, Is.SameAs(request));
            Assert.That(receipt.RunId, Is.Not.EqualTo(request.RunId));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(request.RunId, number), Is.False);
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(receipt.RunId, number), Is.True);
        }
        [Test]
        public void Dispose_ClearsOfferReceiptAndActualInput()
        {
            var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            var result = game.Publish(game.Request(offer));
            var buffers = game.Buffers;
            game.Dispose();
            Assert.That(buffers.TraitGrowthStateReadSink.ActiveOffer, Is.Null);
            Assert.That(buffers.TraitGrowthResultReadSink.LatestResult, Is.Null);
            Assert.That(buffers.TraitGrowthViewReadSink.TryGetCommand(result.Number, out _), Is.False);
        }
    }
}
