using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.StageMap;

namespace ProjectL.Tests.Core
{
    public sealed class ChapterNavigationRulesTests
    {
        [Test]
        public void AdjacentUnclearedNodeIsAcceptedWithoutChangingSuppliedProgress()
        {
            ChapterNodeProgress[] nodes = Graph();

            ChapterSelectionResult first = Select(nodes, StageNodeId.Empty, 2);
            ChapterSelectionResult second = Select(nodes, StageNodeId.Empty, 3);

            Assert.That(first.Accepted, Is.True);
            Assert.That(first.TargetNodeId, Is.EqualTo(new StageNodeId(2)));
            Assert.That(first.TargetStageKey, Is.EqualTo(new StageKey(100)));
            Assert.That(second.Accepted, Is.True);
            Assert.That(second.TargetStageKey, Is.EqualTo(new StageKey(100)));
            Assert.That(nodes[1].IsClear, Is.False);
        }

        [Test]
        public void ClearedNodeCannotBeReenteredEvenWhenConnected()
        {
            ChapterNodeProgress[] nodes = Graph(clear2: true);

            ChapterSelectionResult result = Select(nodes, StageNodeId.Empty, 2);

            Assert.That(result.FailureReason, Is.EqualTo(ChapterSelectionFailureReason.AlreadyCleared));
            Assert.That(result.TargetNodeId, Is.EqualTo(StageNodeId.Empty));
        }

        [Test]
        public void AdvancingRequiresTheCurrentStageToBeCleared()
        {
            ChapterSelectionResult denied = Select(Graph(), new StageNodeId(2), 4);
            ChapterSelectionResult accepted = Select(Graph(clear2: true), new StageNodeId(2), 4);

            Assert.That(denied.FailureReason, Is.EqualTo(ChapterSelectionFailureReason.CurrentStageUncleared));
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.TargetStageKey, Is.EqualTo(new StageKey(900)));
        }

        [Test]
        public void ChapterReentryUsesRetainedClearsAndAProvidedStartPosition()
        {
            ChapterNodeProgress[] nodes = Graph(clear2: true);

            Assert.That(Select(nodes, StageNodeId.Empty, 2).FailureReason,
                Is.EqualTo(ChapterSelectionFailureReason.AlreadyCleared));
            Assert.That(Select(nodes, StageNodeId.Empty, 3).Accepted, Is.True);
        }

        [Test]
        public void WrongChapterOrCompletedChapterRejectsWithoutAStageResult()
        {
            ChapterNodeProgress[] nodes = Graph();

            ChapterSelectionResult wrong = Select(nodes, StageNodeId.Empty, 2, requestedChapter: 8);
            ChapterSelectionResult wrongGraph = Select(nodes, StageNodeId.Empty, 2, graphChapter: 8);
            ChapterSelectionResult complete = Select(nodes, StageNodeId.Empty, 2, isChapterClear: true);

            Assert.That(wrong.FailureReason, Is.EqualTo(ChapterSelectionFailureReason.ChapterMismatch));
            Assert.That(wrongGraph.FailureReason, Is.EqualTo(ChapterSelectionFailureReason.ChapterMismatch));
            Assert.That(complete.FailureReason, Is.EqualTo(ChapterSelectionFailureReason.ChapterCompleted));
            Assert.That(wrong.TargetStageKey, Is.EqualTo(StageKey.Empty));
            Assert.That(complete.TargetStageKey, Is.EqualTo(StageKey.Empty));
        }

        [Test]
        public void UnknownAndNonadjacentNodesAreRejected()
        {
            ChapterNodeProgress[] nodes = Graph();

            Assert.That(Select(nodes, new StageNodeId(99), 2).FailureReason,
                Is.EqualTo(ChapterSelectionFailureReason.CurrentNodeNotFound));
            Assert.That(Select(nodes, StageNodeId.Empty, 99).FailureReason,
                Is.EqualTo(ChapterSelectionFailureReason.NodeNotFound));
            Assert.That(Select(nodes, StageNodeId.Empty, 4).FailureReason,
                Is.EqualTo(ChapterSelectionFailureReason.NotConnected));

            ChapterNodeProgress[] skippedDepth =
            {
                Node(1, 0, 0, false, 4),
                Node(4, 900, 2, false)
            };
            Assert.That(Select(skippedDepth, StageNodeId.Empty, 4).FailureReason,
                Is.EqualTo(ChapterSelectionFailureReason.NotConnected));
        }

        private static ChapterSelectionResult Select(ChapterNodeProgress[] nodes,
            StageNodeId fromNodeId, int target, int requestedChapter = 7,
            int graphChapter = 7, bool isChapterClear = false)
        {
            return ChapterNavigationRules.Evaluate(requestedChapter, 7, graphChapter, fromNodeId,
                isChapterClear, nodes, new StageNodeId(target));
        }

        private static ChapterNodeProgress[] Graph(bool clear2 = false)
        {
            return new[]
            {
                Node(1, 0, 0, false, 2, 3),
                Node(2, 100, 1, clear2, 4),
                Node(3, 100, 1, false, 4),
                Node(4, 900, 2, false)
            };
        }

        private static ChapterNodeProgress Node(int id, int key, int depth, bool clear,
            params int[] next)
        {
            var nextIds = new StageNodeId[next.Length];
            for (int i = 0; i < next.Length; i++) nextIds[i] = new StageNodeId(next[i]);
            return new ChapterNodeProgress(new StageNodeId(id),
                key == 0 ? StageKey.Empty : new StageKey(key), depth, clear, nextIds);
        }
    }
}
