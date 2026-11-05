using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitGrowthChoiceServiceTests
    {
        [Test]
        public void RestoredImpossibleHistory_IsRejectedBeforeAnOfferCanBePublished()
        {
            using var game = new TraitGrowthTestRuntime(initialLevel: 3);
            game.Tables.ReplaceDefinition(TraitKey.TrackingShot,
                new TraitGrowthDefinitionData(new ImpactBoltTraitData(1), null, "impact", "normal", "", ""));
            game.Tables.ReplaceCategory(new CategoryTraitProgressionData(CategoryKey.Cannon, new[]
            {
                new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Acquire), new TraitGrowthStepSpec(30, 3, TraitGrowthKind.Acquire)
            }, new[] { TraitKey.SoloAction, TraitKey.Vanguard }));
            game.RestoreTargetGrowth(new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, TraitKey.ImpactBolt) }));
            long revision = game.Revision;
            Assert.Throws<System.InvalidOperationException>(() => game.Core.Characters.ReadTraitGrowthProgress());
            Assert.Throws<System.InvalidOperationException>(() => game.Core.Characters.PrepareNextTraitGrowthOffer());
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.Null);
            Assert.That(game.Revision, Is.EqualTo(revision));
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(4, 3)]
        public void VariableOffer_IsStableAndRequiresAnExplicitSelection(int available, int expected)
        {
            using var game = new TraitGrowthTestRuntime(eligibleChoices: available);
            var before = game.Current;
            long revision = game.Revision;
            var offer = game.Open();
            Assert.That(offer.Choices.Count, Is.EqualTo(expected));
            Assert.That(offer.Choices.Select(c => c.TraitKey).Distinct().Count(), Is.EqualTo(expected));
            Assert.That(offer.Choices.Select(c => c.TraitKey), Is.Ordered);
            var repeated = game.Core.Characters.PrepareNextTraitGrowthOffer().ActiveOffer;
            Assert.That(repeated, Is.SameAs(offer));
            Assert.That(repeated.OfferId, Is.EqualTo(offer.OfferId));
            Assert.That(game.Current, Is.SameAs(before));
            Assert.That(game.Revision, Is.EqualTo(revision));
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.ChoiceRequired));
            var receipt = game.Publish(game.Request(offer));
            Assert.That(receipt.Result.Succeeded, Is.True);
            Assert.That(game.Current.Growth.Selections.Single().TraitKey, Is.EqualTo(offer.Choices[0].TraitKey));
            Assert.That(game.Revision, Is.EqualTo(revision + 1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConfiguredMain_UsesTheCommonChoiceAndCompositionPath(bool main)
        {
            using var game = new TraitGrowthTestRuntime(main);
            long revision = game.Revision;
            var before = game.Current;
            game.Buffers.CharacterReadSink.TryGetMainState(before.Id, out var persistent);
            var offer = game.Open();
            var request = game.Request(offer);
            var receipt = game.Publish(request);
            Assert.That(receipt.Result.Succeeded, Is.True);
            Assert.That(game.Revision, Is.EqualTo(revision + 1));
            Assert.That((game.Current.Id, game.Current.Level, game.Current.Experience), Is.EqualTo((before.Id, before.Level, before.Experience)));
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.AwaitingAcknowledgement));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, receipt.Number), Is.True);
            var second = game.Open();
            var enhanced = game.Publish(game.Request(second, TraitKey.Knockback));
            Assert.That(enhanced.Result.Succeeded, Is.True);
            Assert.That(game.Current.Growth.Selections.Count, Is.EqualTo(2));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(second.RunId, enhanced.Number), Is.True);
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.ReadyToContinue));
            Assert.That(game.Buffers.CharacterReadSink.TryGetMainState(before.Id, out var afterPersistent), Is.EqualTo(main));
            if (main)
                Assert.That(afterPersistent, Is.SameAs(persistent));
            var stats = game.Core.CharacterStats.ReadOwned().Items.Single(i => i.OwnedId == before.Id);
            Assert.That(stats.Stats.TraitKeys.ToArray(), Does.Contain(request.TraitKey));
            game.EnterBattle();
            var actor = game.Buffers.CharacterReadSink.Characters.Single(c => c.OwnedId == before.Id);
            Assert.That(actor.Stats.TraitStatuses.ToArray().Select(t => t.Key), Is.EquivalentTo(stats.Stats.TraitKeys.ToArray()));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void EmptyTables_PreserveInitializationAndAcquisition(bool main)
        {
            using var game = new TraitGrowthTestRuntime(main, enabled: false);
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.ReadyToContinue));
            Assert.That(game.Core.Characters.PrepareNextTraitGrowthOffer().ActiveOffer, Is.Null);
        }
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Preparation_RechecksExternalDefinitionRead(int mutation)
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            var before = game.Current;
            long revision = game.Revision;
            game.Tables.BeforeRead = () =>
            {
                game.Tables.BeforeRead = null;
                if (mutation == 0)
                    game.Core.Characters.AcquireSub(EntityKey.Unit_Ballista);
                else if (mutation == 1)
                    game.EndRun();
                else
                    game.InjectBattleDuringDefinitionRead();
            };
            var op = game.Service.PrepareTraitGrowthChoice(game.Request(offer));
            Assert.That(op.Decision.FailureReason, Is.EqualTo(mutation == 0 ? TraitGrowthFailureReason.SourceChanged : mutation == 1 ? TraitGrowthFailureReason.RunEnded : TraitGrowthFailureReason.InBattle));
            Assert.That(game.Current, Is.SameAs(before));
            Assert.That(game.Revision, Is.EqualTo(revision + (mutation == 0 ? 1 : 0)));
        }
    }
}
