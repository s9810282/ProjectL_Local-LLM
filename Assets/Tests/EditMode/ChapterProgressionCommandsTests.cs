using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Chapter;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Tests.Core
{
    public sealed class ChapterProgressionCommandsTests
    {
        [Test]
        public void EntryRequiresGraphAndDoesNotSwitchAnUnfinishedChapter()
        {
            ChapterProgressionResult missingGraph = ChapterProgressionCommands.EnterChapter(
                7, 0, false, false, false);
            ChapterProgressionResult activeOtherChapter = ChapterProgressionCommands.EnterChapter(
                8, 7, false, false, true);
            ChapterProgressionResult accepted = ChapterProgressionCommands.EnterChapter(
                7, 0, false, false, true);
            ChapterProgressionResult completed = ChapterProgressionCommands.EnterChapter(
                7, 0, false, true, false);

            Assert.That(missingGraph.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.StageNotFound));
            Assert.That(activeOtherChapter.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.ChapterMismatch));
            Assert.That(accepted.Accepted && accepted.SelectMap, Is.True);
            Assert.That(completed.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.ChapterCompleted));
        }

        [Test]
        public void ClickUsesNavigationRulesAndReturnsOnlyEntryEffect()
        {
            ChapterProgressionResult accepted = ChapterProgressionCommands.ClickNode(
                7, 7, 7, StageNodeId.Empty, false, Graph(), new StageNodeId(2));
            ChapterProgressionResult rejected = ChapterProgressionCommands.ClickNode(
                7, 0, 7, StageNodeId.Empty, false, Graph(), new StageNodeId(2));

            Assert.That(accepted.Accepted && accepted.SelectStage, Is.True);
            Assert.That(accepted.ClearStage || accepted.ClearMap, Is.False);
            Assert.That(rejected.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.ChapterMismatch));
        }

        [Test]
        public void OutcomeNeedsAnEnteredStageAndPreservesBossAndDefeatEffects()
        {
            ChapterProgressionResult noSelection = ChapterProgressionCommands.ResolveStage(
                7, 7, 7, false, StageNodeId.Empty, StageKey.Empty,
                false, Graph(), StageOutcome.Victory, false);
            ChapterProgressionResult boss = ChapterProgressionCommands.ResolveStage(
                7, 7, 7, true, new StageNodeId(3), new StageKey(900),
                false, Graph(), StageOutcome.Victory, true);
            ChapterProgressionResult defeat = ChapterProgressionCommands.ResolveStage(
                7, 7, 7, true, new StageNodeId(2), new StageKey(100),
                false, Graph(), StageOutcome.Defeat, false);

            Assert.That(noSelection.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.StageNotFound));
            Assert.That(boss.Accepted && boss.ClearStage && boss.ClearMap, Is.True);
            Assert.That(defeat.Accepted && defeat.EndAttempt, Is.True);
            Assert.That(defeat.ClearStage || defeat.ClearMap, Is.False);
        }

        [Test]
        public void RecoveryCompletion_ProducesOnlyNodeClearWithoutCombatOutcome()
        {
            var nodes = Graph();
            var result = CompleteRecovery(nodes: nodes);
            Assert.That(result.Accepted && result.ClearStage, Is.True);
            Assert.That(result.ClearMap || result.EndAttempt || result.SelectMap || result.SelectStage, Is.False);
            Assert.That(result.ChapterKey, Is.EqualTo(7));
            Assert.That(result.NodeId.Value, Is.EqualTo(2));
            Assert.That(result.StageKey.Value, Is.EqualTo(100));
            Assert.That(nodes[1].IsClear, Is.False);
        }

        [TestCase("requested-chapter", ChapterProgressionFailureReason.ChapterMismatch)]
        [TestCase("current-chapter", ChapterProgressionFailureReason.ChapterMismatch)]
        [TestCase("graph-chapter", ChapterProgressionFailureReason.ChapterMismatch)]
        [TestCase("missing-selection", ChapterProgressionFailureReason.StageNotFound)]
        [TestCase("missing-node", ChapterProgressionFailureReason.StageNotFound)]
        [TestCase("empty-graph", ChapterProgressionFailureReason.StageNotFound)]
        [TestCase("start-node", ChapterProgressionFailureReason.StageNotFound)]
        [TestCase("stage-key", ChapterProgressionFailureReason.StageMismatch)]
        [TestCase("chapter-complete", ChapterProgressionFailureReason.ChapterCompleted)]
        [TestCase("stage-complete", ChapterProgressionFailureReason.StageAlreadyCleared)]
        public void RecoveryCompletion_RejectsInvalidProgress(string fault, ChapterProgressionFailureReason expected)
        {
            var nodes = Graph();
            if (fault == "stage-complete") nodes[1] = new ChapterNodeProgress(new StageNodeId(2), new StageKey(100),
                1, true, new[] { new StageNodeId(3) });
            var result = CompleteRecovery(fault == "requested-chapter" ? 8 : 7,
                fault == "current-chapter" ? 0 : 7, fault == "graph-chapter" ? 8 : 7,
                fault != "missing-selection", new StageNodeId(fault == "missing-node" ? 99 : fault == "start-node" ? 1 : 2),
                new StageKey(fault == "stage-key" ? 101 : 100), fault == "chapter-complete",
                fault == "empty-graph" ? Array.Empty<ChapterNodeProgress>() : nodes);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(expected));
            Assert.That(result.ClearStage || result.ClearMap || result.EndAttempt || result.SelectMap || result.SelectStage, Is.False);
        }

        [TestCase("requested-chapter")]
        [TestCase("current-chapter")]
        [TestCase("graph-chapter")]
        [TestCase("null-graph")]
        [TestCase("null-node")]
        public void RecoveryCompletion_RejectsMalformedInputs(string fault)
        {
            var nodes = fault == "null-graph" ? null : Graph();
            if (fault == "null-node") nodes[0] = null;
            Assert.Catch<ArgumentException>(() => CompleteRecovery(fault == "requested-chapter" ? 0 : 7,
                fault == "current-chapter" ? -1 : 7, fault == "graph-chapter" ? 0 : 7,
                nodes: nodes));
        }

        [Test]
        public void ShopCompletion_ProducesOnlyNodeClearWithoutCombatOutcome()
        {
            var nodes = Graph();
            var result = CompleteShop(nodes: nodes);
            Assert.That(result.Accepted && result.ClearStage, Is.True);
            Assert.That(result.ClearMap || result.EndAttempt || result.SelectMap || result.SelectStage, Is.False);
            Assert.That(result.ChapterKey, Is.EqualTo(7));
            Assert.That(result.NodeId.Value, Is.EqualTo(2));
            Assert.That(result.StageKey.Value, Is.EqualTo(100));
            Assert.That(nodes[1].IsClear, Is.False);
        }

        [TestCase("requested-chapter", ChapterProgressionFailureReason.ChapterMismatch)]
        [TestCase("current-chapter", ChapterProgressionFailureReason.ChapterMismatch)]
        [TestCase("graph-chapter", ChapterProgressionFailureReason.ChapterMismatch)]
        [TestCase("missing-selection", ChapterProgressionFailureReason.StageNotFound)]
        [TestCase("missing-node", ChapterProgressionFailureReason.StageNotFound)]
        [TestCase("empty-graph", ChapterProgressionFailureReason.StageNotFound)]
        [TestCase("start-node", ChapterProgressionFailureReason.StageNotFound)]
        [TestCase("stage-key", ChapterProgressionFailureReason.StageMismatch)]
        [TestCase("chapter-complete", ChapterProgressionFailureReason.ChapterCompleted)]
        [TestCase("stage-complete", ChapterProgressionFailureReason.StageAlreadyCleared)]
        public void ShopCompletion_RejectsInvalidProgress(string fault, ChapterProgressionFailureReason expected)
        {
            var nodes = Graph();
            if (fault == "stage-complete") nodes[1] = new ChapterNodeProgress(new StageNodeId(2), new StageKey(100),
                1, true, new[] { new StageNodeId(3) });
            var result = CompleteShop(fault == "requested-chapter" ? 8 : 7,
                fault == "current-chapter" ? 0 : 7, fault == "graph-chapter" ? 8 : 7,
                fault != "missing-selection", new StageNodeId(fault == "missing-node" ? 99 : fault == "start-node" ? 1 : 2),
                new StageKey(fault == "stage-key" ? 101 : 100), fault == "chapter-complete",
                fault == "empty-graph" ? Array.Empty<ChapterNodeProgress>() : nodes);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(expected));
            Assert.That(result.ClearStage || result.ClearMap || result.EndAttempt || result.SelectMap || result.SelectStage, Is.False);
        }

        [TestCase("requested-chapter")]
        [TestCase("current-chapter")]
        [TestCase("graph-chapter")]
        [TestCase("null-graph")]
        [TestCase("null-node")]
        public void ShopCompletion_RejectsMalformedInputs(string fault)
        {
            var nodes = fault == "null-graph" ? null : Graph();
            if (fault == "null-node") nodes[0] = null;
            Assert.Catch<ArgumentException>(() => CompleteShop(fault == "requested-chapter" ? 0 : 7,
                fault == "current-chapter" ? -1 : 7, fault == "graph-chapter" ? 0 : 7,
                nodes: nodes));
        }

        private static ChapterProgressionResult CompleteShop(int requested = 7, int current = 7, int graph = 7,
            bool selected = true, StageNodeId? node = null, StageKey? key = null, bool cleared = false,
            ChapterNodeProgress[] nodes = null)
            => ChapterProgressionCommands.CompleteShopStage(requested, current, graph, selected,
                node ?? new StageNodeId(2), key ?? new StageKey(100), cleared, nodes);

        private static ChapterProgressionResult CompleteRecovery(int requested = 7, int current = 7, int graph = 7,
            bool selected = true, StageNodeId? node = null, StageKey? key = null, bool cleared = false,
            ChapterNodeProgress[] nodes = null)
            => ChapterProgressionCommands.CompleteRecoveryStage(requested, current, graph, selected,
                node ?? new StageNodeId(2), key ?? new StageKey(100), cleared, nodes);

        private static ChapterNodeProgress[] Graph() => new[]
        {
            new ChapterNodeProgress(new StageNodeId(1), StageKey.Empty, 0, false,
                new[] { new StageNodeId(2) }),
            new ChapterNodeProgress(new StageNodeId(2), new StageKey(100), 1, false,
                new[] { new StageNodeId(3) }),
            new ChapterNodeProgress(new StageNodeId(3), new StageKey(900), 2, false,
                System.Array.Empty<StageNodeId>())
        };
    }
}
