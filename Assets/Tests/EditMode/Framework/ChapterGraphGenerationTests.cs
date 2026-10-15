using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Framework
{
    public sealed partial class ChapterGraphPreparationTests
    {
        [TestCase("Nearest")]
        [TestCase("Random")]
        [TestCase("WeightedRandom")]
        public void PrepareGraph_DoesNotRegisterUntilPublished(string algorithm)
        {
            var setup = Prepare(algorithm, true, seed: 51);
            var buffers = (IBufferQuery)setup.buffers;
            var snapshot = new RecoveryBufferSnapshot(buffers);
            var detached = setup.core.StageGeneratorSystem.PrepareGraph(7);
            Assert.That(setup.stages.GetStages(7), Is.Empty);
            snapshot.AssertUnchanged(buffers);
            Assert.That(setup.core.ChapterProgression.EnterChapter(7).Accepted, Is.True);
            Assert.That(Signature(setup.stages.GetStages(7)), Is.EqualTo(Signature(detached)));
            AssertGraph(detached, 5, 3);
        }

        [Test]
        public void ChapterEntry_PublishesGraphWithSelection_AndStaleGraphInstallsNothing()
        {
            // 진입 판정은 Core가 하고, Buffer는 판정에 쓴 위치·챕터 상태·그래프가 그대로일 때만 함께 설치한다.
            var setup = Prepare("Nearest", true, seed: 51);
            var b = (IBufferQuery)setup.buffers;
            int current = b.CurrentProgressReadSink.CurrentMapKey;
            bool currentClear = current > 0 && b.MapProgressReadSink.GetMap(current).IsClear;
            var generated = setup.core.StageGeneratorSystem.PrepareGraph(7);
            var proposal = new ProjectL.Systems.RuntimeSystem.BufferSystems.Current.ChapterEntryPreparation(
                current, currentClear, 7, b.MapProgressReadSink.GetMap(7).IsClear, Array.Empty<StageProgressData>(), generated);

            b.StageProgressWriteSink.RegisterStages(7, generated);
            Assert.Throws<InvalidOperationException>(() => b.ChapterProgressWriteSink.PublishChapterEntry(proposal));
            Assert.That(b.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(current));

            var fresh = Prepare("Nearest", true, seed: 51);
            var fb = (IBufferQuery)fresh.buffers;
            fb.ChapterProgressWriteSink.PublishChapterEntry(proposal);
            Assert.That(fb.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(7));
            Assert.That(Signature(fresh.stages.GetStages(7)), Is.EqualTo(Signature(generated)));
        }

        [TestCase("Nearest", 0f)]
        [TestCase("Nearest", 1f)]
        [TestCase("Random", 0f)]
        [TestCase("Random", 1f)]
        [TestCase("WeightedRandom", 0f)]
        [TestCase("WeightedRandom", 1f)]
        public void ActualGenerator_SeedRangePreservesLayeredReachableGraph(string algorithm, float chance)
        {
            var signatures = new HashSet<string>();
            for (int seed = 0; seed < 8; seed++)
            {
                var setup = Prepare(algorithm, true, seed: seed, additionalChance: chance);
                Assert.That(setup.core.ChapterProgression.EnterChapter(7).Accepted, Is.True);
                var nodes = setup.stages.GetStages(7);
                AssertGraph(nodes, 5, 3);
                signatures.Add(Signature(nodes));
            }
            Assert.That(signatures.Count, Is.GreaterThan(1));
        }

        [TestCase("Nearest")]
        [TestCase("Random")]
        [TestCase("WeightedRandom")]
        public void ActualGenerator_FixedSeedIsDeterministic_AndChapterReturnReusesGraph(string algorithm)
        {
            var first = Prepare(algorithm, true, seed: 51);
            var second = Prepare(algorithm, true, seed: 51);
            first.core.ChapterProgression.EnterChapter(7);
            second.core.ChapterProgression.EnterChapter(7);
            Assert.That(Signature(second.stages.GetStages(7)), Is.EqualTo(Signature(first.stages.GetStages(7))));
            if (algorithm == "Nearest")
            {
                var nodes = first.stages.GetStages(7);
                for (int depth = 0; depth < 4; depth++)
                {
                    var sources = nodes.Where(n => n.Depth == depth).OrderBy(n => n.WidthIndex).ToArray();
                    var targets = nodes.Where(n => n.Depth == depth + 1).OrderBy(n => n.WidthIndex).ToArray();
                    for (int index = 0; index < sources.Length; index++)
                        Assert.That(sources[index].NextNodeIds,
                            Does.Contain(targets[index * targets.Length / sources.Length].NodeId));
                }
            }
            var oldNodes = first.stages.GetStages(7).ToArray();
            first.core.ChapterProgression.EnterChapter(7);
            Assert.That(first.stages.GetStages(7), Is.EqualTo(oldNodes));
        }

        [Test]
        public void ActualGenerator_TwoDepthsConnectStartDirectlyToBoss()
        {
            var setup = Prepare("Nearest", true, depthCount: 2);
            setup.core.ChapterProgression.EnterChapter(7);
            AssertGraph(setup.stages.GetStages(7), 2, 3);
            Assert.That(setup.stages.GetStages(7).Single(n => n.Depth == 0).NextNodeIds,
                Is.EqualTo(new[] { setup.stages.GetStages(7).Single(n => n.Depth == 1).NodeId }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StageEntry_MissingOrDisconnectedNodePreservesSelectionAndHistory(bool missing)
        {
            var setup = Prepare("Nearest", true);
            setup.core.ChapterProgression.EnterChapter(7);
            var before = setup.stages.GetStages(7).ToArray();
            int target = missing ? int.MaxValue : before.Single(n => n.Depth == 4).NodeId;
            Assert.That(setup.core.ChapterProgression.PrepareStageEntry(7, new StageNodeId(target)).Accepted, Is.False);
            Assert.That(setup.core.ChapterProgression.ConfirmStageEntry(7, new StageNodeId(target)).Accepted, Is.False);
            Assert.That(setup.current.HasSelection, Is.False);
            Assert.That(setup.stages.GetStages(7), Is.EqualTo(before));
            Assert.That(setup.maps.GetMap(7).IsClear, Is.False);
        }

        [Test]
        public void StageEntry_UnclearedPredecessorCannotBeSkipped()
        {
            var setup = Prepare("Nearest", true);
            setup.core.ChapterProgression.EnterChapter(7);
            var first = setup.stages.GetStages(7).First(n => n.Depth == 1);
            Assert.That(setup.core.ChapterProgression.ConfirmStageEntry(7, new StageNodeId(first.NodeId)).Accepted, Is.True);
            int next = first.NextNodeIds[0];
            Assert.That(setup.core.ChapterProgression.ConfirmStageEntry(7, new StageNodeId(next)).Accepted, Is.False);
            Assert.That(setup.current.CurrentStage.NodeId, Is.EqualTo(first.NodeId));
            Assert.That(setup.stages.IsCleared(7, first.NodeId), Is.False);
            setup.stageWrite.SetClear(7, first.NodeId, true);
            Assert.That(setup.core.ChapterProgression.ConfirmStageEntry(7, new StageNodeId(next)).Accepted, Is.True);
        }

        [Test]
        public void ProgressData_DefensivelyCopiesEdges()
        {
            var edges = new[] { 2 };
            var node = new StageProgressData(1, 0, 0, 0, edges);
            edges[0] = 99;
            Assert.That(node.NextNodeIds, Is.EqualTo(new[] { 2 }));
            Assert.Throws<NotSupportedException>(() => ((IList<int>)node.NextNodeIds)[0] = 99);
        }

        [Test]
        public void RecoveryService_UsesCurrentBufferOptions_PaysOnceAndRespectsNextSelection()
        {
            var setup = Prepare("Nearest", true);
            var b = (IBufferQuery)setup.buffers;
            setup.core.ChapterProgression.EnterChapter(7);
            var node = setup.stages.GetStages(7).First(n => n.Depth == 1);
            setup.core.ChapterProgression.ConfirmStageEntry(7, new StageNodeId(node.NodeId));
            b.CharacterWriteSink.Acquire(new[] { new RosterAcquireRequest(
                EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main) }, CharacterBufferTestData.MainState(7));
            b.CurrencyWriteSink.SetAmount(8);
            var option = new RecoveryOptionSpec(new RecoveryOptionId(1), 3,
                new RecoveryHealSpec(RecoveryHealMode.Flat, 1));
            b.CurrentProgressWriteSink.SetRecoveryOptions(new[] { option });
            Assert.Throws<InvalidOperationException>(() => setup.core.Combat.Recover(new RecoveryOptionId(2)));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(8));
            Assert.That(b.CurrentProgressReadSink.Recovery.AppliedOptionId.HasValue, Is.False);
            Assert.That(setup.core.Combat.Recover(option.Id), Is.True);
            Assert.That(setup.core.Combat.Recover(option.Id), Is.False);
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(5));
            b.CharacterReadSink.TryGetMainState(out var healed);
            Assert.That(healed.CurrentHealth, Is.EqualTo(8));
            Assert.That(setup.core.ChapterProgression.ResolveStage(7, StageOutcome.Victory).Accepted, Is.True);
            Assert.That(setup.core.ChapterProgression.ConfirmStageEntry(7,
                new StageNodeId(node.NextNodeIds[0])).Accepted, Is.True);
            Assert.That(b.CurrentProgressReadSink.Recovery, Is.Null);
            var nextOption = new RecoveryOptionSpec(new RecoveryOptionId(2), 1,
                new RecoveryHealSpec(RecoveryHealMode.Flat, 1));
            b.CurrentProgressWriteSink.SetRecoveryOptions(new[] { nextOption });
            Assert.Throws<InvalidOperationException>(() => setup.core.Combat.Recover(option.Id));
            Assert.That(setup.core.Combat.Recover(nextOption.Id), Is.True);
            b.CharacterReadSink.TryGetMainState(out var final);
            Assert.That(final.CurrentHealth, Is.EqualTo(9));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(4));
            Assert.That(b.StageProgressReadSink.IsCleared(7, node.NodeId), Is.True);
        }

        [TestCase(7, 0)]
        [TestCase(10, 8)]
        public void RecoveryStage_NoAvailablePurchaseCanStillExitWithoutChangingHealthOrCurrency(int hp, int balance)
        {
            var setup = Prepare("Nearest", true);
            var b = (IBufferQuery)setup.buffers;
            setup.core.ChapterProgression.EnterChapter(7);
            var node = setup.stages.GetStages(7).First(n => n.Depth == 1);
            setup.core.ChapterProgression.ConfirmStageEntry(7, new StageNodeId(node.NodeId));
            var persistent = CharacterBufferTestData.MainState(hp);
            b.CharacterWriteSink.Acquire(new[] { new RosterAcquireRequest(
                EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main) }, persistent);
            b.CurrencyWriteSink.SetAmount(balance);
            var option = new RecoveryOptionSpec(new RecoveryOptionId(1), 3,
                new RecoveryHealSpec(RecoveryHealMode.Full, 0));
            Assert.Throws<InvalidOperationException>(() => setup.core.Combat.Recover(option.Id));
            b.CurrentProgressWriteSink.SetRecoveryOptions(new[] { option });
            Assert.Throws<InvalidOperationException>(() => setup.core.Combat.Recover(option.Id));
            b.CharacterReadSink.TryGetMainState(out var unchanged);
            Assert.That(unchanged, Is.SameAs(persistent));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(balance));
            Assert.That(b.CurrentProgressReadSink.Recovery.AppliedOptionId.HasValue, Is.False);
            Assert.That(setup.core.ChapterProgression.ResolveStage(7, StageOutcome.Victory).Accepted, Is.True);
            Assert.That(b.StageProgressReadSink.IsCleared(7, node.NodeId), Is.True);
            Assert.That(setup.core.ChapterProgression.ConfirmStageEntry(7,
                new StageNodeId(node.NextNodeIds[0])).Accepted, Is.True);
            b.CharacterReadSink.TryGetMainState(out unchanged);
            Assert.That(unchanged, Is.SameAs(persistent));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(balance));
        }

        private static string Signature(IReadOnlyList<StageProgressData> nodes) => string.Join("|",
            nodes.Select(n => $"{n.NodeId}:{n.StageKey}:{n.Depth}:{n.WidthIndex}:{string.Join(",", n.NextNodeIds)}"));

        private static void AssertGraph(IReadOnlyList<StageProgressData> nodes, int depthCount, int widthMax)
        {
            Assert.That(nodes.Select(n => n.NodeId).Distinct().Count(), Is.EqualTo(nodes.Count));
            var start = nodes.Single(n => n.Depth == 0);
            var boss = nodes.Single(n => n.Depth == depthCount - 1);
            Assert.That(start.StageKey, Is.Zero);
            Assert.That(boss.StageKey, Is.EqualTo(900));
            Assert.That(boss.NextNodeIds, Is.Empty);
            var reachable = new HashSet<int> { start.NodeId };
            foreach (var node in nodes.OrderBy(n => n.Depth))
            {
                Assert.That(reachable.Contains(node.NodeId), Is.True, $"Unreachable node {node.NodeId}");
                Assert.That(node.Depth, Is.InRange(0, depthCount - 1));
                Assert.That(node.WidthIndex, Is.InRange(0, widthMax - 1));
                if (node.Depth > 0 && node.Depth < depthCount - 1) Assert.That(node.StageKey, Is.EqualTo(100));
                if (node.Depth < depthCount - 1) Assert.That(node.NextNodeIds, Is.Not.Empty);
                Assert.That(node.NextNodeIds.Distinct().Count(), Is.EqualTo(node.NextNodeIds.Count));
                foreach (int edge in node.NextNodeIds)
                {
                    Assert.That(nodes.Single(n => n.NodeId == edge).Depth, Is.EqualTo(node.Depth + 1));
                    reachable.Add(edge);
                }
            }
            Assert.That(reachable.Count, Is.EqualTo(nodes.Count));
            for (int depth = 1; depth < depthCount - 1; depth++)
                Assert.That(nodes.Count(n => n.Depth == depth), Is.InRange(1, widthMax));
        }
    }
}
