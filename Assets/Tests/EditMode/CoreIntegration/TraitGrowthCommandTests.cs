using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitGrowthCommandTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void CategoryDefault_InvalidatedChoiceKeepsOriginalRecord(int changeOnRead)
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            var request = game.Request(offer);
            void ChangeDefault()
            {
                game.Tables.BeforeRead = null;
                game.SetCategoryDefault(CategoryKey.Cannon, request.TraitKey);
            }

            int reads = 0;
            if (changeOnRead > 0) game.Tables.BeforeRead = () => { if (++reads == changeOnRead) ChangeDefault(); };
            else ChangeDefault();
            var before = game.Current;
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(request);
            Assert.That(Process(game, number), Is.True);
            var expected = changeOnRead == 0 ? TraitGrowthFailureReason.NotCandidate : TraitGrowthFailureReason.SourceChanged;
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult.Result.FailureReason, Is.EqualTo(expected));
            Assert.That(game.Current, Is.SameAs(before));
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.SameAs(offer));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CategoryDefault_InvalidReloadPreventsAnotherUnitsGrowthPublication(bool offerAlreadyOpen)
        {
            using var game = new TraitGrowthTestRuntime(lastOnly: true, secondCategory: true);
            game.SetCategoryDefault(CategoryKey.Cannon, TraitKey.SoloAction);
            var first = game.Open();
            int firstNumber = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(first, TraitKey.SoloAction));
            Assert.That(Process(game, firstNumber), Is.True);
            Assert.That(game.Current.Growth.Selections.Single().TraitKey, Is.EqualTo(TraitKey.SoloAction));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(first.RunId, firstNumber), Is.True);
            Assert.That(game.Core.Characters.AcquireSub(EntityKey.Unit_Ballista, 2).Succeeded, Is.True);
            var offer = offerAlreadyOpen ? game.Open() : null;
            game.SetCategoryDefault(CategoryKey.Cannon, TraitKey.Vanguard);
            long revision = game.Revision;
            InvalidOperationException failure;
            if (offerAlreadyOpen)
            {
                int number = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(offer));
                failure = Assert.Throws<InvalidOperationException>(() => Process(game, number));
                Assert.That(game.Buffers.TraitGrowthResultReadSink.HandledCommandNumber, Is.EqualTo(firstNumber));
            }
            else failure = Assert.Throws<InvalidOperationException>(() => game.Core.Characters.PrepareNextTraitGrowthOffer());
            Assert.That(failure.Message, Does.Contain("SoloAction").And.Contain("Cannon"));
            Assert.That(game.Revision, Is.EqualTo(revision));
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.SameAs(offer));
        }

        internal static bool Process(TraitGrowthTestRuntime game, int number) => game.Core.TraitGrowthCommands.ProcessNextTraitGrowthCommand(number);

        [Test]
        public void UnacknowledgedResult_PreventsNextPublication()
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            var request = game.Request(offer);
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(request);
            Assert.That(Process(game, number + 1), Is.False);
            Assert.That(Process(game, number), Is.True);
            var receipt = game.Buffers.TraitGrowthResultReadSink.LatestResult;
            Assert.That(receipt.Request, Is.SameAs(request));
            Assert.That(receipt.Result.Succeeded, Is.True);
            Assert.That(game.Buffers.TraitGrowthViewReadSink.TryGetCommand(number, out _), Is.True);
            Assert.That(Process(game, number), Is.False);
            Assert.Throws<InvalidOperationException>(() => game.Buffers.TraitGrowthViewWriteSink.Submit(request));
            Assert.That(game.Core.Characters.PrepareNextTraitGrowthOffer().Status, Is.EqualTo(TraitGrowthProgressStatus.AwaitingAcknowledgement));
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult, Is.SameAs(receipt));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, number + 1), Is.False);
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, number), Is.True);
            var next = game.Open();
            int second = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(next, TraitKey.Knockback));
            Assert.That(Process(game, number), Is.False);
            Assert.That(Process(game, second), Is.True);
            Assert.That(game.Current.Growth.Selections.Count, Is.EqualTo(2));
        }

        [Test]
        public void DuplicateNumber_DoesNotReapply()
        {
            using var game = new TraitGrowthTestRuntime(lastOnly: true);
            var offer = game.Open();
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(offer));
            Assert.That(Process(game, number), Is.True);
            var record = game.Current;
            var receipt = game.Buffers.TraitGrowthResultReadSink.LatestResult;
            long revision = game.Revision;
            Assert.That(Process(game, number), Is.False);
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, number), Is.True);
            Assert.That(Process(game, number), Is.False);
            Assert.That(game.Current, Is.SameAs(record));
            Assert.That(game.Revision, Is.EqualTo(revision));
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult, Is.SameAs(receipt));
            int duplicate = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(offer));
            Assert.That(Process(game, duplicate), Is.True);
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult.Result.FailureReason, Is.EqualTo(TraitGrowthFailureReason.StaleOffer));
            Assert.That(game.Current, Is.SameAs(record));
        }

        [Test]
        public void OldRunCommand_CannotAffectNewRun()
        {
            using var game = new TraitGrowthTestRuntime();
            var old = game.Open();
            var request = game.Request(old);
            int oldNumber = game.Buffers.TraitGrowthViewWriteSink.Submit(request);
            game.Buffers.RunProgressWriteSink.ResetRunProgress();
            var run = new RunId(Guid.NewGuid());
            ((IRunProgressBufferStore)game.Buffers.RunProgressReadSink).InstallRunId(run);
            Assert.That(Process(game, oldNumber), Is.False);
            Assert.That(game.Core.Characters.InitializeRoster(1).Succeeded, Is.True);
            var records = game.Buffers.CharacterReadSink.Owned.ToArray();
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(request);
            Assert.That(number, Is.GreaterThan(oldNumber));
            Assert.That(Process(game, number), Is.True);
            var receipt = game.Buffers.TraitGrowthResultReadSink.LatestResult;
            Assert.That(receipt.RunId, Is.EqualTo(run));
            Assert.That(receipt.Request, Is.SameAs(request));
            Assert.That(receipt.Result.FailureReason, Is.EqualTo(TraitGrowthFailureReason.SourceChanged));
            Assert.That(game.Buffers.CharacterReadSink.Owned, Is.EqualTo(records));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(old.RunId, number), Is.False);
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(run, number), Is.True);
        }

        [Test]
        public void FailedChoice_RetainsFrozenOfferAndRetriesAfterAcknowledgement()
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            var absent = new[] { TraitKey.SoloAction, TraitKey.HuntersMark, TraitKey.Vanguard, TraitKey.TrackingShot }
                .First(key => !offer.Choices.Any(c => c.TraitKey == key));
            var before = game.Current;
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(offer, absent));
            Assert.That(Process(game, number), Is.True);
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult.Result.FailureReason, Is.EqualTo(TraitGrowthFailureReason.NotCandidate));
            Assert.That(game.Current, Is.SameAs(before));
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.SameAs(offer));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, number), Is.True);
            int retry = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(game.Open()));
            Assert.That(Process(game, retry), Is.True);
            Assert.That(game.Current.Growth.Selections.Count, Is.EqualTo(1));
        }

        [Test]
        public void AcquisitionDuringFrozenOffer_JoinsQueueWithoutRerolling()
        {
            using var game = new TraitGrowthTestRuntime(secondCategory: true);
            var offer = game.Open();
            var acquisition = game.Core.Characters.AcquireSub(EntityKey.Unit_Ballista, 2);
            Assert.That(acquisition.Succeeded, Is.True, acquisition.FailureReason.ToString());
            var later = acquisition.Acquired.Single();
            Assert.That(game.Open(), Is.SameAs(offer));
            for (int step = 0; step < 2; step++)
            {
                var current = game.Open();
                Assert.That(current.OwnedId, Is.EqualTo(offer.OwnedId));
                int number = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(current,
                    current.Kind == TraitGrowthKind.Enhance ? TraitKey.Knockback : (TraitKey?)null));
                Assert.That(Process(game, number), Is.True);
                Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(current.RunId, number), Is.True);
            }
            Assert.That(game.Open().OwnedId, Is.EqualTo(later.Id));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void DefinitionRead_SourceMutationCannotAppendStaleSelection(int mutation)
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            var before = game.Current;
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(offer));
            game.Tables.BeforeRead = () =>
            {
                game.Tables.BeforeRead = null;
                if (mutation == 0)
                    Assert.That(game.Core.Characters.AcquireSub(EntityKey.Unit_Ballista).Succeeded, Is.True);
                else if (mutation == 1)
                    game.EndRun();
                else
                    game.Buffers.TraitGrowthOperations.Clear();
            };
            Assert.That(Process(game, number), Is.EqualTo(mutation != 2));
            Assert.That(game.Current, Is.SameAs(before));
            if (mutation == 2)
                Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult, Is.Null);
            else
            {
                var receipt = game.Buffers.TraitGrowthResultReadSink.LatestResult;
                Assert.That(receipt.Number, Is.EqualTo(number));
                Assert.That(receipt.Result.FailureReason, Is.EqualTo(mutation == 0 ? TraitGrowthFailureReason.SourceChanged : TraitGrowthFailureReason.RunEnded));
                Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.SameAs(offer));
            }
        }

        [Test]
        public void NestedReadProcessingAndAcknowledgement_CannotRepublishSameInput()
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(offer));
            game.Tables.BeforeRead = () =>
            {
                game.Tables.BeforeRead = null;
                Assert.That(Process(game, number), Is.True);
                Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, number), Is.True);
            };
            Assert.That(Process(game, number), Is.False);
            Assert.That(game.Current.Growth.Selections.Count, Is.EqualTo(1));
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult.Number, Is.EqualTo(number));
            Assert.That(game.Buffers.TraitGrowthViewReadSink.TryGetCommand(number, out _), Is.False);
        }
    }
}
