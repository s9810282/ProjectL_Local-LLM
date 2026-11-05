using System;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TrainingGroundAuthoringTests
    {
        [Test]
        public void TrainingAsset_PricesAloneConfigureNewImmutableDefinitions()
        {
            var asset = ScriptableObject.CreateInstance<EnhancementStageData>();
            try
            {
                var pending = (EnhancementStageDetail)asset.GetData();
                Assert.That(pending.TrainingPrices, Is.Null);
                pending.Validate();

                SetPrices(asset, 10, 20);
                var first = (EnhancementStageDetail)asset.GetData();
                first.Validate();
                Assert.That(first.TrainingPrices, Is.Not.Null, "가격만 입력하면 훈련 정의가 생성되어야 합니다.");
                Assert.That((first.TrainingPrices.Price1To2, first.TrainingPrices.Price2To3), Is.EqualTo((10, 20)));
                SetPrices(asset, 15, 35);
                var second = (EnhancementStageDetail)asset.GetData();
                Assert.That((second.TrainingPrices.GetPrice(1), second.TrainingPrices.GetPrice(2)), Is.EqualTo((15, 35)));
                Assert.That(first.TrainingPrices.GetPrice(1), Is.EqualTo(10));
                Assert.That(second.TrainingPrices, Is.Not.SameAs(first.TrainingPrices));
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        [TestCase(0, 20)]
        [TestCase(-1, 20)]
        [TestCase(10, 10)]
        [TestCase(10, 9)]
        [TestCase(10, 0)]
        [TestCase(0, -1)]
        public void TrainingAsset_RejectsInvalidPrices(int first, int second)
        {
            var asset = ScriptableObject.CreateInstance<EnhancementStageData>();
            try
            {
                SetPrices(asset, first, second);
                Assert.Throws<ArgumentOutOfRangeException>(() => asset.GetData());
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        [Test]
        public void TrainingAsset_ExposesOnlyPricesWithoutLegacyModeOrCandidates()
        {
            var asset = ScriptableObject.CreateInstance<EnhancementStageData>();
            try
            {
                using var serialized = new SerializedObject(asset);
                Assert.That(serialized.FindProperty("training"), Is.Null);
                Assert.That(serialized.FindProperty("candidates"), Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        [Test]
        public void UnconfiguredTrainingDefinition_IsLoadableWithoutProducts()
        {
            var pending = new EnhancementStageDetail((TrainingPrices)null);
            Assert.That(pending.TrainingPrices, Is.Null);
            Assert.DoesNotThrow(pending.Validate);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AuthoredPrices_AloneControlNewVisitReadiness(bool configured)
        {
            using var game = new TrainingGroundTestRuntime();
            var asset = ScriptableObject.CreateInstance<EnhancementStageData>();
            try
            {
                if (configured) SetPrices(asset, 13, 29);
                game.Database.SetStage(new StageRootData(
                    1202, 1, StageMapRole.Regular,
                    (EnhancementStageDetail)asset.GetData()));
                if (configured)
                {
                    game.Enter();
                    var visit = game.Buffers.CurrentProgressReadSink.Shop;
                    Assert.That(visit.VisitKind, Is.EqualTo(ShopVisitKind.Training));
                    Assert.That(visit.TrainingChoices.Single(c => c.OwnedId == game.TargetId).Price, Is.EqualTo(13));
                    Assert.That(visit.TrainingChoices.Single(c => c.OwnedId == game.MainId).Price, Is.EqualTo(29));
                    Assert.That(game.Core.Shop.CompleteShop(game.Context).Accepted, Is.True);
                }
                else
                {
                    Assert.That(game.Core.ChapterProgression.PrepareStageEntry(1,
                        new StageNodeId(2)).FailureReason,
                        Is.EqualTo(ChapterProgressionFailureReason.EnhancementShopUnavailable));
                    Assert.That(game.Core.Shop.EnterShop(1,
                        new StageNodeId(2), 17).FailureReason,
                        Is.EqualTo(ChapterProgressionFailureReason.EnhancementShopUnavailable));
                    Assert.That(game.Buffers.CurrentProgressReadSink.Shop, Is.Null);
                }
                Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(100));
                Assert.That(game.Buffers.CurrencyReadSink.RunSpent, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        [TestCase(0)]
        [TestCase(3)]
        public void TrainingPrices_RejectNonPurchasableRank(int rank)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new TrainingPrices(10, 20).GetPrice(rank));

        private static void SetPrices(EnhancementStageData asset, int first, int second)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("price1To2").intValue = first;
            serialized.FindProperty("price2To3").intValue = second;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
