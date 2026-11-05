using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitGrowthBoundaryIntegrationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void PrepareOffer_SourceMutationCannotPublishStaleProposal(bool endRun)
        {
            using var game = new TraitGrowthTestRuntime();
            int reads = 0;
            long revision = game.Revision;
            game.Tables.BeforeRead = () =>
            {
                if (++reads != 3)
                    return;
                game.Tables.BeforeRead = null;
                if (endRun)
                    game.EndRun();
                else
                    Assert.That(game.Core.Characters.AcquireSub(EntityKey.Unit_Ballista).Succeeded, Is.True);
            };
            var progress = game.Core.Characters.PrepareNextTraitGrowthOffer();
            Assert.That(progress.Status, Is.EqualTo(endRun ? TraitGrowthProgressStatus.Unavailable : TraitGrowthProgressStatus.ChoiceRequired));
            Assert.That(progress.ActiveOffer, Is.Null);
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.Null);
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult, Is.Null);
            Assert.That(game.Revision, Is.EqualTo(revision + (endRun ? 0 : 1)));
            if (!endRun)
                Assert.That(game.Open().OfferId, Is.EqualTo(1));
        }
        [Test]
        public void HighLevelInstances_AreResolvedByOwnedIdThenConfiguredStepOrder()
        {
            using var game = new TraitGrowthTestRuntime(initialLevel: 5, multiLevel: true);
            var second = game.Buffers.CharacterWriteSink.Acquire(new[] { new RosterAcquireRequest(EntityKey.Unit_ShotGun, CharacterOwnershipRole.Sub, 3) }).Acquired.Single();
            var expected = new[] { (game.TargetId, 10), (game.TargetId, 20), (game.TargetId, 30), (second.Id, 10), (second.Id, 20) };
            foreach (var pair in expected)
            {
                var offer = game.Open();
                Assert.That((offer.OwnedId, offer.StepId), Is.EqualTo(pair));
                var receipt = game.Publish(game.Request(offer, offer.Kind == TraitGrowthKind.Enhance ? TraitKey.Knockback : (TraitKey?)null));
                Assert.That(receipt.Result.Succeeded, Is.True);
                Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, receipt.Number), Is.True);
            }
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.ReadyToContinue));
            Assert.That(game.Buffers.CharacterReadSink.Owned.Single(o => o.Id == second.Id).Growth.Selections.Count, Is.EqualTo(2));
        }
        [Test]
        public void SameSubCopies_StatsDeckPublicationProfilesAndNextBattleAgree()
        {
            using var game = new TraitGrowthTestRuntime();
            Assert.That(game.Core.Characters.AcquireSub(EntityKey.Unit_ShotGun).FailureReason,
                Is.EqualTo(RosterAcquireFailureReason.OwnershipLimitReached));
            // 정상 획득 한도는 유지한다. 합성 경계 검증을 위한 두 보유 ID만 저장소 fixture로 준비한다.
            var second = game.Buffers.CharacterWriteSink.Acquire(new[] { new RosterAcquireRequest(EntityKey.Unit_ShotGun, CharacterOwnershipRole.Sub) }).Acquired.Single();
            var main = game.Buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main);
            var deckStore = (IRunDeckStore)game.Buffers.RunDeckReadSink;
            deckStore.Install(deckStore.Prepare(game.Buffers.RunProgressReadSink.RunId, new[] { main.Id, game.TargetId, second.Id }));
            var offer = game.Open();
            var acquired = game.Publish(game.Request(offer));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, acquired.Number), Is.True);
            var enhancedOffer = game.Open();
            var enhanced = game.Publish(game.Request(enhancedOffer, TraitKey.Knockback));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, enhanced.Number), Is.True);
            var stats = game.Core.CharacterStats.ReadOwned().Items;
            var deck = game.Core.Deck.Read().Snapshot;
            Assert.That(deck, Is.Not.Null);
            game.Buffers.CharacterStatsRequestSink.RequestRefresh();
            ((ProjectL.Framework.SystemBase.ISystemTick)game.Core).Tick(0);
            var published = game.Buffers.CharacterStatsReadSink.ReadOwned().Items;
            var definition = CoreIntegrationTestData.DatabaseSource(game.Buffers).Read(game.Database, 1, new StageKey(1101));
            var profiles = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements.OwnedUnitSpecResolver.BuildProfiles(
                definition.AttackProfiles, game.Buffers.CharacterReadSink.Owned, definition.TraitGrowth);
            foreach (var id in new[] { game.TargetId, second.Id })
            {
                var expected = stats.Single(s => s.OwnedId == id).Stats;
                Assert.That(deck.Stats.Single(s => s.OwnedId == id).Stats.TraitKeys.ToArray(), Is.EqualTo(expected.TraitKeys.ToArray()));
                Assert.That(published.Single(s => s.OwnedId == id).Stats.TraitKeys.ToArray(), Is.EqualTo(expected.TraitKeys.ToArray()));
                var profile = profiles.Single(p => p.OwnedId == id);
                Assert.That(profile.Definition.Traits.ToArray().Select(t => t.TraitKey), Is.EqualTo(expected.TraitKeys.ToArray()));
                Assert.That(((ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits.KnockbackTraitSpec)
                    profile.Definition.Traits.ToArray().Single(t => t.TraitKey == TraitKey.Knockback)).DistanceCells, Is.EqualTo(id == game.TargetId ? 3 : 1));
            }
            Assert.That(stats.Single(s => s.OwnedId == second.Id).Stats.TraitKeys.ToArray(), Is.EqualTo(new[] { TraitKey.Knockback }));
            game.EnterBattle();
            foreach (var id in new[] { game.TargetId, second.Id })
            {
                var expected = stats.Single(s => s.OwnedId == id).Stats;
                var actual = game.Buffers.CharacterReadSink.Characters.Single(c => c.OwnedId == id);
                Assert.That((actual.Stats.AttackPower, actual.Health.MaxHealth), Is.EqualTo((expected.AttackPower, expected.MaxHealth)));
                Assert.That(actual.Stats.TraitStatuses.ToArray().Select(t => t.Key), Is.EqualTo(expected.TraitKeys.ToArray()));
            }
        }
        [Test]
        public void FinalChapterPublication_ClearsRetainedFailureOfferAndInput()
        {
            using var game = new TraitGrowthTestRuntime(initialLevel: 1);
            game.EnterBattle();
            // 최종 챕터를 승인한 Core 결과를 저장 경계에 전달한다. 경로 정책은 이 검사의 대상이 아니다.
            var completion = new BattleCompletionChanges(new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed),
                ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(1101), true));
            Assert.That(game.Buffers.BattleWriteSink.CompleteBattle(completion), Is.True);
            game.Buffers.BattleWriteSink.EndBattle();
            game.SetTargetLevel(2);
            var offer = game.Open();
            var wrong = new TraitGrowthChoiceRequest(offer.RunId, offer.OfferId + 1, offer.OwnedId, offer.StepId, offer.Choices[0].TraitKey);
            var receipt = game.Publish(wrong);
            Assert.That(receipt.Result.Succeeded, Is.False);
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.SameAs(offer));
            var preparation = new ProjectL.Systems.RuntimeSystem.BufferSystems.Current.ChapterContinuationPreparation(
                ChapterContinuationResult.FinishRun(1, new StageNodeId(2), new StageKey(1101)), game.Buffers.CurrentProgressReadSink.CurrentStage,
                game.Buffers.CombatStateReadSink.Completion, game.Buffers.StageProgressReadSink.GetStages(1), Array.Empty<StageProgressData>(),
                Array.Empty<StageProgressData>(), true, false);
            game.Buffers.ChapterContinuationWriteSink.PublishContinuation(preparation);
            Assert.That(game.Buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.Null);
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult, Is.Null);
            Assert.That(game.Buffers.TraitGrowthViewReadSink.TryGetCommand(receipt.Number, out _), Is.False);
        }
        [Test]
        public void RunEndingBattle_DiscardsOfferAndPendingInput()
        {
            using var game = new TraitGrowthTestRuntime(initialLevel: 1);
            game.EnterBattle();
            // 정상 진입은 먼저 끝낸다. 남은 제안 정리는 내부 저장 fixture로 별도 검증한다.
            var store = (ProjectL.Systems.RuntimeSystem.BufferSystems.Character.ITraitGrowthStateStore)game.Buffers.TraitGrowthStateReadSink;
            var offer = store.PublishOffer(game.Buffers.RunProgressReadSink.RunId, game.TargetId, 10, TraitGrowthKind.Acquire,
                new[] { new TraitGrowthChoice(TraitKey.SoloAction, TraitGrowthKind.Acquire, "solo", "normal"),
                    new TraitGrowthChoice(TraitKey.HuntersMark, TraitGrowthKind.Acquire, "mark", "normal"),
                    new TraitGrowthChoice(TraitKey.TrackingShot, TraitGrowthKind.Acquire, "tracking", "normal") });
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(offer));
            Assert.That(game.Core.BattleCompletion.AbandonBattle(), Is.EqualTo(BattleAbandonResult.Abandoned));
            Assert.That(game.Buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Defeat));
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.Null);
            Assert.That(game.Buffers.TraitGrowthViewReadSink.TryGetCommand(number, out _), Is.False);
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult, Is.Null);
        }
    }
}
