using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TrainingGroundVisitTests
    {
        [Test]
        public void TrainingVisit_IncludesMainAndOffDeckCopiesWithoutThreeChoiceCap()
        {
            using var game = new TrainingGroundTestRuntime();
            game.Enter();
            var shop = game.Buffers.CurrentProgressReadSink.Shop;
            var choices = shop.TrainingChoices;
            Assert.That(shop.Offers, Is.Empty);
            Assert.That(choices.Count, Is.EqualTo(5));
            Assert.That(choices.Select(c => (c.OwnedId, c.TraitKey)).Distinct().Count(), Is.EqualTo(choices.Count));
            Assert.That(game.Buffers.RunDeckReadSink.TryGetCurrent(out var deck), Is.True);
            Assert.That(deck.OrderedIds, Has.No.Member(game.TargetId));
            Assert.That(game.Owned(game.TargetId).CharacterKey, Is.EqualTo(game.Owned(game.SiblingId).CharacterKey));
            Assert.That(choices.Any(c => c.OwnedId == game.TargetId), Is.True);
            Assert.That(choices.Any(c => c.OwnedId == game.SiblingId), Is.True);
            var main = choices.Single(c => c.OwnedId == game.MainId);
            Assert.That((main.CurrentRank, main.NextRank, main.Price, main.CanPurchase), Is.EqualTo((2, 3, 20, true)));
            var sub = choices.Single(c => c.OwnedId == game.TargetId);
            Assert.That((sub.CurrentRank, sub.NextRank, sub.Price, sub.CanPurchase), Is.EqualTo((1, 2, 10, true)));
            Assert.That(choices.All(c => c.TraitKey == TraitKey.Vanguard), Is.True,
                "미보유 StationaryFire, 미등록 Knockback, 비훈련 SoloAction은 목록에 나오지 않아야 한다.");
            Assert.That(choices.Any(c => c.OwnedId == game.CappedId), Is.False);
            Assert.That(shop.HasPurchase, Is.False);
            Assert.That(shop.TrainingReceipt, Is.Null);
        }

        [TestCase(false, 100)]
        [TestCase(false, 0)]
        [TestCase(true, 100)]
        public void TrainingVisit_AlwaysAllowsExitAndNextNode(bool noEligible, int gold)
        {
            using var game = new TrainingGroundTestRuntime(gold, noEligible);
            game.Enter();
            var choices = game.Buffers.CurrentProgressReadSink.Shop.TrainingChoices;
            Assert.That(choices.Count == 0, Is.EqualTo(noEligible));
            Assert.That(choices.Any(c => c.CanPurchase), Is.EqualTo(!noEligible && gold > 0));
            var before = game.Buffers.CharacterReadSink.Owned.ToArray();
            long revision = game.Buffers.CharacterReadSink.RosterRevision;
            var completion = game.Publish(ShopViewCommandData.Complete(game.Context));
            Assert.That(completion.CompletionResult.Accepted, Is.True);
            Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(gold));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.Zero);
            Assert.That(game.Buffers.CharacterReadSink.RosterRevision, Is.EqualTo(revision));
            Assert.That(game.Buffers.CharacterReadSink.Owned, Is.EqualTo(before));
            Assert.That(game.Core.ChapterProgression.PrepareStageEntry(1, new StageNodeId(3)).Accepted, Is.True);
            game.Enter(3);
            Assert.That(game.Core.Shop.CompleteShop(game.Context).Accepted, Is.True);
        }
    }
}
