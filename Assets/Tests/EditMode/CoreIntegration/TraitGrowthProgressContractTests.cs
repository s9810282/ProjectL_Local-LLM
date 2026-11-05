using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitGrowthProgressContractTests
    {
        [Test]
        public void LastSelection_WaitsForAcknowledgement()
        {
            using var game = new TraitGrowthTestRuntime(lastOnly: true);
            var offer = game.Open();
            var receipt = game.Publish(game.Request(offer));
            Assert.That(receipt.Result.Succeeded, Is.True);
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.Null);
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.AwaitingAcknowledgement));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, receipt.Number + 1), Is.False);
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(new RunId(Guid.NewGuid()), receipt.Number), Is.False);
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, receipt.Number), Is.True);
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, receipt.Number), Is.True);
            Assert.That(game.Buffers.TraitGrowthViewReadSink.TryGetCommand(receipt.Number, out _), Is.False);
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.ReadyToContinue));
        }
        [Test]
        public void PrepareNextOffer_CannotCrossReceiptBarrier()
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            var receipt = game.Publish(game.Request(offer));
            for (int i = 0; i < 3; i++)
                Assert.That(game.Core.Characters.PrepareNextTraitGrowthOffer().Status, Is.EqualTo(TraitGrowthProgressStatus.AwaitingAcknowledgement));
            Assert.Throws<InvalidOperationException>(() => game.Buffers.TraitGrowthOperations.PublishOffer(offer.RunId, game.Revision, offer.OwnedId, 20,
                TraitGrowthKind.Enhance, new[] { new TraitGrowthChoice(TraitKey.Knockback, TraitGrowthKind.Enhance, "n", "d") }));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, receipt.Number), Is.True);
            Assert.That(game.Open().OfferId, Is.EqualTo(offer.OfferId + 1));
        }
        [Test]
        public void FailureReceipt_RetainsOfferForRebind()
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            var before = game.Current;
            long revision = game.Revision;
            int amount = game.Buffers.CurrencyReadSink.Amount;
            int spent = game.Buffers.CurrencyReadSink.RunSpent;
            var selectedStage = game.Buffers.CurrentProgressReadSink.CurrentStage;
            var absent = new[] { TraitKey.SoloAction, TraitKey.HuntersMark, TraitKey.Vanguard, TraitKey.TrackingShot }.First(k => !offer.Choices.Any(c => c.TraitKey == k));
            var receipt = game.Publish(game.Request(offer, absent));
            Assert.That(receipt.Result.FailureReason, Is.EqualTo(TraitGrowthFailureReason.NotCandidate));
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.AwaitingAcknowledgement));
            Assert.That(game.Core.Characters.PrepareNextTraitGrowthOffer().ActiveOffer, Is.SameAs(offer));
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult, Is.SameAs(receipt));
            Assert.That(game.Current, Is.SameAs(before));
            Assert.That(game.Revision, Is.EqualTo(revision));
            Assert.That((game.Buffers.CurrencyReadSink.Amount, game.Buffers.CurrencyReadSink.RunSpent), Is.EqualTo((amount, spent)));
            Assert.That(game.Buffers.CurrentProgressReadSink.CurrentStage, Is.EqualTo(selectedStage));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, receipt.Number), Is.True);
            Assert.That(game.Open(), Is.SameAs(offer));
            Assert.That(game.Publish(game.Request(offer)).Result.Succeeded, Is.True);
        }
        [Test]
        public void Reopen_DoesNotConsumeRandomness()
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            var revision = game.Revision;
            for (int i = 0; i < 5; i++)
            {
                Assert.That(game.Core.Characters.ReadTraitGrowthProgress().ActiveOffer, Is.SameAs(offer));
                Assert.That(game.Core.Characters.PrepareNextTraitGrowthOffer().ActiveOffer, Is.SameAs(offer));
            }
            game.Core.Characters.AcquireSub(EntityKey.Unit_Ballista, 2);
            Assert.That(game.Open(), Is.SameAs(offer));
            Assert.That(game.Revision, Is.EqualTo(revision + 1));
        }
        [Test]
        public void BattleAndEndedRun_AreUnavailable()
        {
            using var game = new TraitGrowthTestRuntime(initialLevel: 1);
            game.EnterBattle();
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.Unavailable));
            game.Buffers.BattleWriteSink.EndBattle();
            game.EndRun();
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.Unavailable));
        }
    }
}
