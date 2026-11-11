using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitGrowthWeightedPublicationTests
    {
        private static readonly IReadOnlyDictionary<TraitKey, TraitGrowthGroup> Groups =
            new Dictionary<TraitKey, TraitGrowthGroup>
            {
                [TraitKey.Knockback] = TraitGrowthGroup.Common,
                [TraitKey.SoloAction] = TraitGrowthGroup.Common,
                [TraitKey.Vanguard] = TraitGrowthGroup.Common,
                [TraitKey.HuntersMark] = TraitGrowthGroup.CategorySpecific,
                [TraitKey.TrackingShot] = TraitGrowthGroup.CategorySpecific
            };

        [Test]
        public void PrepareOffer_UsesAuthoredGroupsAndWeights()
        {
            var pool = Groups.Keys.ToArray();
            var eligible = pool.Where(key => key != TraitKey.Knockback).ToArray();
            const int runs = 32;
            int CountCategoryCards(int commonWeight, int categoryWeight)
            {
                int categoryCards = 0;
                for (int index = 1; index <= runs; index++)
                {
                    using var game = new TraitGrowthTestRuntime();
                    AuthorGroups(game);
                    SetRun(game, index);
                    game.Tables.ReplaceCategory(Row(CategoryKey.Cannon, pool, commonWeight, categoryWeight, Step(10)));
                    var before = game.Buffers.CharacterReadSink.Owned.ToArray();
                    long revision = game.Revision;
                    var keys = Keys(game.Open());
                    Assert.That(keys, Is.Unique.And.SubsetOf(eligible));
                    Assert.That(keys.Length, Is.EqualTo(TraitGrowthOffer.MaximumChoices));
                    Assert.That(game.Buffers.CharacterReadSink.Owned, Is.EqualTo(before));
                    Assert.That(game.Revision, Is.EqualTo(revision));
                    categoryCards += keys.Count(IsCategorySpecific);
                }
                return categoryCards;
            }

            Assert.That(CountCategoryCards(1, 9) - CountCategoryCards(9, 1), Is.GreaterThan(runs / 2));
        }

        [Test]
        public void OpenOffer_WeightChangePreservesIdentityAndSelection()
        {
            using var game = new TraitGrowthTestRuntime();
            AuthorGroups(game);
            SetRun(game, 7);
            var pool = Groups.Keys.ToArray();
            var step = Step(10);
            game.Tables.ReplaceCategory(Row(CategoryKey.Cannon, pool, 1, 9, step));
            var first = game.Open();
            var originalChoices = first.Choices.ToArray();
            var choice = originalChoices[0].TraitKey;

            game.Tables.ReplaceCategory(Row(CategoryKey.Cannon, pool, 9, 1, step));
            var repeated = game.Open();
            Assert.That(repeated, Is.SameAs(first));
            Assert.That(repeated.OfferId, Is.EqualTo(first.OfferId));
            Assert.That(repeated.Choices, Is.EqualTo(originalChoices));
            ChooseAndAcknowledge(game, first, choice);
            Assert.That(game.Current.Growth.Selections.Single().TraitKey, Is.EqualTo(choice));
        }

        [Test]
        public void NextOffer_UsesUpdatedWeights()
        {
            using var game = new TraitGrowthTestRuntime();
            AuthorGroups(game);
            SetRun(game, 13);
            RemoveNativeTraits(game, CategoryKey.Cannon);
            var pool = Groups.Keys.ToArray();
            var steps = new[] { Step(10), Step(20) };
            const int dominantWeight = int.MaxValue - 1;
            game.Tables.ReplaceCategory(Row(CategoryKey.Cannon, pool, dominantWeight, 1, steps));
            var first = game.Open();
            var choice = Keys(first).First(key => !IsCategorySpecific(key));
            ChooseAndAcknowledge(game, first, choice);

            game.Tables.ReplaceCategory(Row(CategoryKey.Cannon, pool, 1, dominantWeight, steps));
            var next = game.Open();
            var eligible = pool.Where(key => key != choice).ToArray();
            int seed = TraitGrowthOfferSampler.Seed(next.RunId, game.TargetId, 20);
            var stale = TraitGrowthOfferSampler.Sample(eligible, TraitGrowthKind.Acquire, seed, Groups, dominantWeight, 1);
            var expected = TraitGrowthOfferSampler.Sample(eligible, TraitGrowthKind.Acquire, seed, Groups, 1, dominantWeight);
            Assert.That(next.StepId, Is.EqualTo(20));
            Assert.That(next.OfferId, Is.Not.EqualTo(first.OfferId));
            Assert.That(expected, Is.Not.EqualTo(stale));
            Assert.That(Keys(next), Is.EqualTo(expected));
        }

        [Test]
        public void OwnedCategoryPoolExhausted_FallsBackToCommon()
        {
            using var game = new TraitGrowthTestRuntime();
            AuthorGroups(game);
            SetRun(game, 17);
            var pool = new[] { TraitKey.SoloAction, TraitKey.Vanguard, TraitKey.TrackingShot };
            game.Tables.ReplaceCategory(Row(CategoryKey.Cannon, pool, 1, 9, Step(10), Step(20)));
            game.RestoreTargetGrowth(OwnedTraitGrowth.Empty.Append(new TraitGrowthSelection(10, TraitKey.TrackingShot)));

            var offer = game.Open();
            Assert.That(offer.StepId, Is.EqualTo(20));
            Assert.That(Keys(offer), Is.EquivalentTo(new[] { TraitKey.SoloAction, TraitKey.Vanguard }));
        }

        [Test]
        public void IncompatibleCategoryPool_FallsBackWithoutExpandingAllowlist()
        {
            using var game = new TraitGrowthTestRuntime();
            AuthorGroups(game);
            SetRun(game, 19);
            var incompatible = new TraitGrowthDefinitionData(TraitGrowthGroup.CategorySpecific,
                new DistanceDamageScaleTraitData(new[] { 100 }), null, "distance", "normal", "", "");
            game.Tables.ReplaceDefinition(TraitKey.TrackingShot, incompatible);
            var pool = new[] { TraitKey.SoloAction, TraitKey.Vanguard, TraitKey.DistanceDamageScale };
            game.Tables.ReplaceCategory(Row(CategoryKey.Cannon, pool, 1, 9, Step(10)));

            Assert.That(Keys(game.Open()), Is.EquivalentTo(new[] { TraitKey.SoloAction, TraitKey.Vanguard }));
        }

        [TestCase(CategoryKey.None, 1000, 1)]
        [TestCase(CategoryKey.Rider, 1, 1000)]
        [TestCase(CategoryKey.Heavy, 2, 1500)]
        [TestCase(CategoryKey.Cannon, 3, 2000)]
        public void MainOffer_UsesEffectiveWeightsAndMainStep(CategoryKey selected, int commonWeight, int categoryWeight)
        {
            using var game = new TraitGrowthTestRuntime(main: true);
            AuthorGroups(game);
            SetRun(game, 23);
            RemoveNativeTraits(game, CategoryKey.Main);
            var mainPool = new[] { TraitKey.Knockback, TraitKey.SoloAction, TraitKey.Vanguard, TraitKey.TrackingShot };
            var selectedPool = new[] { TraitKey.Knockback, TraitKey.TrackingShot, TraitKey.HuntersMark };
            game.Tables.ReplaceCategory(Row(CategoryKey.Main, mainPool, 1000, 1, Step(10)));
            if (selected != CategoryKey.None)
            {
                game.Tables.ReplaceCategory(Row(selected, selectedPool, commonWeight, categoryWeight, Step(99)));
                MainSpecializationBattleTests.Seed(game.Buffers, selected);
            }

            var offer = game.Open();
            var eligible = selected == CategoryKey.None ? mainPool : mainPool.Union(selectedPool).ToArray();
            int seed = TraitGrowthOfferSampler.Seed(offer.RunId, game.TargetId, 10);
            var expected = TraitGrowthOfferSampler.Sample(eligible, TraitGrowthKind.Acquire, seed, Groups, commonWeight, categoryWeight);
            var keys = Keys(offer);
            Assert.That(offer.StepId, Is.EqualTo(10));
            Assert.That(keys, Is.Unique.And.EqualTo(expected));
            Assert.That(keys.Count(IsCategorySpecific), Is.EqualTo(selected == CategoryKey.None ? 0 : 2));
            if (selected == CategoryKey.None)
                Assert.That(keys, Does.Contain(TraitKey.Knockback));
        }

        private static void AuthorGroups(TraitGrowthTestRuntime game)
        {
            var definitions = (ITraitGrowthDefinitionTableQuery)game.Tables;
            foreach (var pair in Groups)
            {
                var row = definitions.Get(pair.Key);
                game.Tables.ReplaceDefinition(pair.Key, new TraitGrowthDefinitionData(pair.Value,
                    row.Normal, row.Enhanced, row.Name, row.Description, row.EnhancedName, row.EnhancedDescription, row.Rank2, row.Rank3));
            }
        }

        private static void SetRun(TraitGrowthTestRuntime game, int index)
        {
            var run = new RunId(new Guid(index, 0, 0, new byte[8]));
            ((IRunProgressBufferStore)game.Buffers.RunProgressReadSink).InstallRunId(run);
            CoreIntegrationTestData.InstallDeck(game.Buffers);
        }

        private static void RemoveNativeTraits(TraitGrowthTestRuntime game, CategoryKey category)
        {
            var current = game.Current;
            var role = current.Role == CharacterOwnershipRole.Main ? UnitRole.MainCharacter : UnitRole.Regular;
            game.Database.SetUnit(new UnitData(current.CharacterKey, role, UnitOwnerEligibility.PlayerOnly,
                1, 1, 10, 0, 2, category, 5, 1, Array.Empty<UnitTraitData>(), targetingMethodKey: 1,
                killReward: UnitKillRewardSpec.Excluded));
        }

        private static CategoryTraitProgressionData Row(CategoryKey category, TraitKey[] pool, int commonWeight,
            int categoryWeight, params TraitGrowthStepSpec[] steps) =>
            new(category, steps, pool, commonWeight, categoryWeight);

        private static TraitGrowthStepSpec Step(int id) => new(id, 2, TraitGrowthKind.Acquire);

        private static TraitKey[] Keys(TraitGrowthOffer offer) => offer.Choices.Select(choice => choice.TraitKey).ToArray();

        private static bool IsCategorySpecific(TraitKey key) => Groups[key] == TraitGrowthGroup.CategorySpecific;

        private static void ChooseAndAcknowledge(TraitGrowthTestRuntime game, TraitGrowthOffer offer, TraitKey key)
        {
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(offer, key));
            Assert.That(game.Core.TraitGrowthCommands.ProcessNextTraitGrowthCommand(number), Is.True);
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult.Result.Succeeded, Is.True);
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, number), Is.True);
        }
    }
}
