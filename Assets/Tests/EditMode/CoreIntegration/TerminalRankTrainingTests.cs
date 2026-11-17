using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TerminalRankTrainingTests
    {
        [TestCase(TraitKey.Sacrifice)]
        [TestCase(TraitKey.DustScreen)]
        public void Rank2UpgradePublishesToOwnedAndBattleSettings(TraitKey key)
        {
            using var game = new TraitGrowthTestRuntime(main: true, initialLevel: 1);
            var core = game.Core;
            var buffers = game.Buffers;
            var combat = game.Database.GetStage(1101);
            UnitTraitData normal = key == TraitKey.Sacrifice
                ? new SacrificeTraitData(2, 1)
                : new DustScreenTraitData(1, 20, 1);
            var rank2 = new TraitRankValues(key, key == TraitKey.Sacrifice ? 10 : 0,
                preventsLethalSacrificeDamage: key == TraitKey.Sacrifice,
                exemptsAlliedAttackRangePenalty: key == TraitKey.DustScreen);
            game.Tables.ReplaceDefinition(TraitKey.Vanguard,
                new TraitGrowthDefinitionData(normal, null, "upgrade", "", "", "", rank2));
            game.Tables.ReplaceCategory(new CategoryTraitProgressionData(CategoryKey.Main,
                Array.Empty<TraitGrowthStepSpec>(), Array.Empty<TraitKey>()));
            game.Database.SetUnit(new UnitData(game.Current.CharacterKey, UnitRole.MainCharacter,
                UnitOwnerEligibility.PlayerOnly, 1, 1, 10, 0, 2, CategoryKey.Main, 5, 1,
                new[] { normal }, targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));
            game.Database.SetStage(new StageRootData(1202, 1, StageMapRole.Regular,
                new EnhancementStageDetail(new TrainingPrices(10, 20))));
            buffers.StageProgressWriteSink.RegisterStages(1, new[]
            {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 1202, 1, 0, new[] { 3 }, stageType: StageType.Shop),
                new StageProgressData(3, 1202, 2, 0, new[] { 4 }, stageType: StageType.Shop),
                new StageProgressData(4, 1101, 3, 0, Array.Empty<int>())
            });
            buffers.CurrencyWriteSink.SetAmount(100);
            Assert.That(core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
            var context = EnterTraining(2);
            var offer = buffers.CurrentProgressReadSink.Shop.TrainingChoices.Single(
                c => c.OwnedId == game.TargetId && c.TraitKey == key);
            Assert.That((offer.CurrentRank, offer.NextRank, offer.Price, offer.CanPurchase), Is.EqualTo((1, 2, 10, true)));
            AssertRankValues(offer.CurrentValues, key, upgraded: false);
            AssertRankValues(offer.NextValues, key, upgraded: true);
            AssertRejected(context, 2, ShopFailureReason.StaleRank);

            var result = core.Shop.PurchaseTraining(context, game.TargetId, key, 1);
            Assert.That(result.Succeeded, Is.True, result.FailureReason.ToString());
            Assert.That((result.Training.OwnedId, result.Training.PreviousRank, result.Training.FinalRank, result.Training.Price),
                Is.EqualTo((game.TargetId, 1, 2, 10)));
            Assert.That(buffers.CurrentProgressReadSink.Shop.TrainingReceipt, Is.SameAs(result.Training));
            Assert.That(game.Current.Growth.Purchases.Count, Is.EqualTo(1));
            Assert.That(game.Current.Growth.Purchases[key], Is.EqualTo(1));
            Assert.That(game.Current.Growth.Selections, Is.Empty);
            Assert.That((buffers.CurrencyReadSink.Amount, buffers.CurrencyReadSink.RunSpent), Is.EqualTo((90, 10)));
            AssertRejected(context, 1, ShopFailureReason.PurchaseAlreadyUsed);
            Assert.That(core.Shop.CompleteShop(context).Accepted, Is.True);

            var next = EnterTraining(3);
            Assert.That(buffers.CurrentProgressReadSink.Shop.TrainingChoices
                .Any(c => c.OwnedId == game.TargetId && c.TraitKey == key), Is.False);
            AssertRejected(next, 1, ShopFailureReason.RankLimitReached);
            AssertRejected(next, 2, ShopFailureReason.RankLimitReached);
            Assert.That(core.Shop.CompleteShop(next).Accepted, Is.True);

            buffers.CharacterStatsRequestSink.RequestRefresh();
            ((ISystemTick)core).Tick(0);
            var owned = buffers.CharacterStatsReadSink.ReadOwned().Items.Single(c => c.OwnedId == game.TargetId);
            Assert.That(owned.TraitRanks[key], Is.EqualTo(2));
            Assert.That(owned.TraitSettings.TryGet(key, out var ownedSpec), Is.True);
            AssertEffectiveSpec(ownedSpec, key, upgraded: true);

            game.Database.SetStage(combat);
            Assert.That(core.BattleEntry.EnterBattle(1, new StageNodeId(4)).Accepted, Is.True);
            var actor = buffers.CharacterReadSink.Characters.Single(c => c.OwnedId == game.TargetId).ToFacts();
            var settings = buffers.CombatStateReadSink.Current.TraitSettings.For(actor);
            Assert.That(settings.TryGet(key, out var battleSpec), Is.True);
            AssertEffectiveSpec(battleSpec, key, upgraded: true);
            Assert.That(settings.SameValues(owned.TraitSettings), Is.True);
            Assert.That(buffers.CombatStateReadSink.Current.TraitSettings.ForUnit(actor.CharacterKey)
                .TryGet(key, out var baseSpec), Is.True);
            AssertEffectiveSpec(baseSpec, key, upgraded: false);

            ShopVisitContext EnterTraining(int node)
            {
                var id = new StageNodeId(node);
                Assert.That(core.Shop.EnterShop(1, id, 17).Accepted, Is.True);
                return new ShopVisitContext(1, id, new StageKey(1202), buffers.CurrentProgressReadSink.Shop.EntryId);
            }

            void AssertRejected(ShopVisitContext visit, int expectedRank, ShopFailureReason reason)
            {
                var before = new CoreIntegrationSnapshot(buffers);
                var rejected = core.Shop.PurchaseTraining(visit, game.TargetId, key, expectedRank);
                Assert.That(rejected.Succeeded, Is.False);
                Assert.That(rejected.FailureReason, Is.EqualTo(reason));
                before.AssertUnchanged(buffers);
            }
        }

        [TestCase(TraitKey.Sacrifice)]
        [TestCase(TraitKey.DustScreen)]
        public void ProductionTerminalRankPreservesAuthoredValuesThroughAssetRoundTrip(TraitKey key)
        {
            var definition = DataBaseAssetTests.ActiveGrowthCatalog().GetTrait(key);
            Assert.That(definition.MaxRank, Is.EqualTo(2));
            Assert.That(definition.Enhanced, Is.Null);
            Assert.That(definition.Rank3, Is.Null);
            Assert.That((definition.Rank2.PreventsLethalSacrificeDamage, definition.Rank2.ExemptsAlliedAttackRangePenalty),
                Is.EqualTo((key == TraitKey.Sacrifice, key == TraitKey.DustScreen)));

            const string sourceFolder = "Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/Item";
            var original = AssetDatabase.LoadAssetAtPath<TraitGrowthDefinitionDataSO>(
                $"{sourceFolder}/TraitGrowth_{key}.asset");
            var authored = (TraitGrowthDefinitionData)original.GetData();
            AssertAuthoredRankValues(definition.Rank2, authored.Rank2);
            var path = $"Assets/__TraitGrowthRank2_{Guid.NewGuid():N}.asset";
            try
            {
                var copy = UnityEngine.Object.Instantiate(original);
                AssetDatabase.CreateAsset(copy, path);
                Resources.UnloadAsset(copy);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var reloaded = AssetDatabase.LoadAssetAtPath<TraitGrowthDefinitionDataSO>(path);
                var serialized = new SerializedObject(reloaded);
                Assert.That(serialized.FindProperty("rank2").managedReferenceValue,
                    Is.TypeOf(key == TraitKey.Sacrifice ? typeof(SacrificeRankAuthoring) : typeof(DustScreenRankAuthoring)));
                var data = (TraitGrowthDefinitionData)reloaded.GetData();
                AssertAuthoredRankValues(data.Rank2, authored.Rank2);
                Assert.That(data.Enhanced, Is.Null);
                Assert.That(data.Rank3, Is.Null);
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void AssertAuthoredRankValues(TraitRankValues actual, TraitRankValues authored)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That((actual.Key, actual.Amount, actual.SecondAmount,
                actual.PreventsLethalSacrificeDamage, actual.ExemptsAlliedAttackRangePenalty),
                Is.EqualTo((authored.Key, authored.Amount, authored.SecondAmount,
                    authored.PreventsLethalSacrificeDamage, authored.ExemptsAlliedAttackRangePenalty)));
        }

        private static void AssertRankValues(TraitRankValues values, TraitKey key, bool upgraded)
        {
            Assert.That(values, Is.Not.Null);
            bool sacrifice = upgraded && key == TraitKey.Sacrifice;
            bool dustScreen = upgraded && key == TraitKey.DustScreen;
            Assert.That((values.Key, values.Amount, values.SecondAmount,
                values.PreventsLethalSacrificeDamage, values.ExemptsAlliedAttackRangePenalty),
                Is.EqualTo((key, sacrifice ? 10 : 0, (int?)null, sacrifice, dustScreen)));
        }

        private static void AssertEffectiveSpec(TraitSpec spec, TraitKey key, bool upgraded)
        {
            if (key == TraitKey.Sacrifice)
            {
                var sacrifice = (SacrificeTraitSpec)spec;
                Assert.That((sacrifice.RadiusCells, sacrifice.MaxTriggersPerEnemyTurn,
                    sacrifice.PreventsLethalSacrificeDamage, sacrifice.ShieldOnActivation),
                    Is.EqualTo((2, 1, upgraded, upgraded ? 10 : 0)));
            }
            else
            {
                var dustScreen = (DustScreenTraitSpec)spec;
                Assert.That((dustScreen.AttackRangeReduction, dustScreen.AttackDamageReductionPercent,
                    dustScreen.OpposingTurnEndsToExpire, dustScreen.ExemptsAlliedAttackRangePenalty),
                    Is.EqualTo((1, 20, 1, upgraded)));
            }
        }

        [Test]
        public void Rank2PurchaseRemovesNextVisitOffer()
        {
            using var game = new TrainingGroundTestRuntime(vanguardStopsAtTwo: true);
            game.Enter();
            var offer = game.Buffers.CurrentProgressReadSink.Shop.TrainingChoices.Single(c => c.OwnedId == game.TargetId);
            Assert.That((offer.CurrentRank, offer.NextRank, offer.Price), Is.EqualTo((1, 2, 10)));

            var result = game.Core.Shop.PurchaseTraining(game.Context, game.TargetId, TraitKey.Vanguard, 1);
            Assert.That(result.Succeeded, Is.True, result.FailureReason.ToString());
            Assert.That((result.Training.FinalRank, result.Training.Price), Is.EqualTo((2, 10)));
            Assert.That(game.Owned(game.TargetId).Growth.Purchases[TraitKey.Vanguard], Is.EqualTo(1));
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop.TrainingReceipt, Is.SameAs(result.Training));

            Assert.That(game.Core.Shop.CompleteShop(game.Context).Accepted, Is.True);
            game.Enter(3);
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop.TrainingChoices.Any(c => c.OwnedId == game.TargetId),
                Is.False);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void TerminalRequestDoesNotMutateBuffers(int expectedRank)
        {
            using var game = new TrainingGroundTestRuntime(vanguardStopsAtTwo: true);
            game.Enter();
            var shop = game.Buffers.CurrentProgressReadSink.Shop;
            var owned = game.Buffers.CharacterReadSink.Owned.ToArray();
            var revision = game.Buffers.CharacterReadSink.RosterRevision;
            var currency = game.Buffers.CurrencyReadSink;
            var before = (currency.Amount, currency.RunSpent);

            var result = game.Core.Shop.PurchaseTraining(game.Context, game.CappedId, TraitKey.Vanguard, expectedRank);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(ShopFailureReason.RankLimitReached));
            Assert.That((currency.Amount, currency.RunSpent), Is.EqualTo(before));
            Assert.That(game.Buffers.CharacterReadSink.Owned, Is.EqualTo(owned));
            Assert.That(game.Buffers.CharacterReadSink.RosterRevision, Is.EqualTo(revision));
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop, Is.SameAs(shop));
            Assert.That(shop.TrainingReceipt, Is.Null);
        }

        [Test]
        public void ActiveVisitRetainsCapturedRankDefinition()
        {
            using var game = new TrainingGroundTestRuntime();
            game.Enter();
            game.Database.TraitGrowthDefinitions = new TrainingGroundTestRuntime.TrainingTables(
                noEligible: false, source: "native", vanguardStopsAtTwo: true);
            Assert.That(game.Database.TraitGrowthDefinitions.Get(TraitKey.Vanguard).Rank3, Is.Null);

            var result = game.Core.Shop.PurchaseTraining(game.Context, game.MainId, TraitKey.Vanguard, 2);
            Assert.That(result.Succeeded, Is.True, result.FailureReason.ToString());
            Assert.That((result.Training.FinalRank, result.Training.Price), Is.EqualTo((3, 20)));
            Assert.That(game.Buffers.CurrentProgressReadSink.Shop.TrainingReceipt, Is.SameAs(result.Training));
            Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.EqualTo(20));
        }

        [Test]
        public void ExistingRank3OfferAndPricesUnchanged()
        {
            using var game = new TrainingGroundTestRuntime();
            game.Enter();
            var choices = game.Buffers.CurrentProgressReadSink.Shop.TrainingChoices;
            var first = choices.Single(c => c.OwnedId == game.TargetId);
            var second = choices.Single(c => c.OwnedId == game.MainId);
            Assert.That((first.CurrentRank, first.NextRank, first.Price), Is.EqualTo((1, 2, 10)));
            Assert.That((second.CurrentRank, second.NextRank, second.Price), Is.EqualTo((2, 3, 20)));
        }
    }
}
