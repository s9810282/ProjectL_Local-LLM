#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Development;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Development;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.StageMap;

namespace ProjectL.Tests.Core
{
    public sealed class DevelopmentNavigationPlannerTests
    {
        [Test]
        public void PlanSkip_BypassesOnlyTheAdjacentNodeWithoutChangingInput()
        {
            DevelopmentNavigationInput input = Input(LinearGraph());

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanSkip(input, Id(2));

            AssertSuccess(plan, 2, new[] { 2 }, 2);
            Assert.That(input.Expected.HasSelection, Is.False);
            Assert.That(input.Nodes[1].Progress.IsClear, Is.False);
            Assert.That(input.Nodes[2].Progress.IsClear, Is.False);
            Assert.That(input.Nodes[3].Progress.IsClear, Is.False);
        }

        [Test]
        public void PlanJump_BypassesOnlyPredecessorsAndLeavesTargetForNormalEntry()
        {
            DevelopmentNavigationInput input = Input(LinearGraph());

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(input, Id(4));

            AssertSuccess(plan, 4, new[] { 2, 3 }, 3);
            Assert.That(input.Nodes[1].Progress.IsClear, Is.False);
            Assert.That(input.Nodes[2].Progress.IsClear, Is.False);
            Assert.That(input.Nodes[3].Progress.IsClear, Is.False);

            ChapterSelectionResult entry = Evaluate(input, Id(4), plan.FinalSelectedNodeId.Value,
                SimulateBypasses(input, plan.BypassedNodes));
            Assert.That(entry.Accepted, Is.True);
            Assert.That(entry.TargetNodeId, Is.EqualTo(Id(4)));
            Assert.That(entry.TargetStageKey, Is.EqualTo(new StageKey(40)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PlanJump_AdjacentTargetNeedsNoBypassOrNewSelection(bool hasSelection)
        {
            DevelopmentNavigationInput input = Input(LinearGraph(clear2: hasSelection),
                current: hasSelection ? 2 : 0);
            int target = hasSelection ? 3 : 2;

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(input, Id(target));

            AssertSuccess(plan, target, Array.Empty<int>(), null);
            Assert.That(input.Expected.HasSelection, Is.EqualTo(hasSelection));
            Assert.That(input.Nodes[1].Progress.IsClear, Is.EqualTo(hasSelection));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PlanJump_BranchingAndMergingChooseLexicographicallySmallestPath(bool reverseNodes)
        {
            DevelopmentNode[] nodes =
            {
                Node(1, 0, false, false, 9, 2),
                Node(9, 1, false, true, 3),
                Node(2, 1, false, true, 7, 5),
                Node(7, 2, false, true, 8),
                Node(3, 2, false, true, 8),
                Node(5, 2, false, true, 8),
                Node(8, 3, false, true, 10),
                Node(10, 4, false, false)
            };
            if (reverseNodes) Array.Reverse(nodes);

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(Input(nodes), Id(10));

            AssertSuccess(plan, 10, new[] { 2, 5, 8 }, 8);
            foreach (DevelopmentNode node in nodes) Assert.That(node.Progress.IsClear, Is.False);
        }

        [Test]
        public void PlanJump_UsesSupportedRouteAfterAnEarlierUnsupportedBranch()
        {
            DevelopmentNode[] nodes =
            {
                Node(1, 0, false, false, 2, 3),
                Node(2, 1, false, false, 6),
                Node(3, 1, false, true, 5),
                Node(6, 2, false, true, 8),
                Node(5, 2, false, true, 8),
                Node(8, 3, false, false)
            };

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(Input(nodes), Id(8));

            AssertSuccess(plan, 8, new[] { 3, 5 }, 5);
        }

        [Test]
        public void PlanJump_AllRoutesThroughUnsupportedNodesHaveNoSupportedPath()
        {
            DevelopmentNode[] nodes =
            {
                Node(1, 0, false, false, 2, 3),
                Node(2, 1, false, false, 4),
                Node(3, 1, false, false, 4),
                Node(4, 2, false, false)
            };

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(Input(nodes), Id(4));

            AssertRefused(plan, DevelopmentFailureReason.NoSupportedPath, null);
        }

        [Test]
        public void PlanJump_FinalTargetDoesNotNeedBypassCapability()
        {
            DevelopmentNode[] nodes =
            {
                Node(1, 0, false, false, 2),
                Node(2, 1, false, true, 3),
                Node(3, 2, false, false, 4),
                Node(4, 3, false, false)
            };

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(Input(nodes), Id(3));

            AssertSuccess(plan, 3, new[] { 2 }, 2);
            Assert.That(nodes[2].Progress.IsClear, Is.False);
        }

        [Test]
        public void PlanSkip_UnsupportedAdjacentNodeIsRefusedAfterNormalAdmission()
        {
            DevelopmentNode[] nodes = LinearGraph();
            nodes[1] = Node(2, 1, false, false, 3);
            DevelopmentNavigationInput input = Input(nodes);
            Assert.That(Evaluate(input, Id(2)).Accepted, Is.True);

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanSkip(input, Id(2));

            AssertRefused(plan, DevelopmentFailureReason.UnsupportedNode, null);
        }

        [Test]
        public void PlanSkip_TerminalNodeIsRefusedEvenWhenBypassCapabilityIsIncorrectlyTrue()
        {
            DevelopmentNode[] nodes =
            {
                Node(1, 0, false, false, 2),
                Node(2, 1, false, true)
            };
            DevelopmentNavigationInput input = Input(nodes);
            Assert.That(Evaluate(input, Id(2)).Accepted, Is.True);

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanSkip(input, Id(2));

            AssertRefused(plan, DevelopmentFailureReason.UnsupportedNode, null);
        }

        [TestCase(ChapterSelectionFailureReason.ChapterCompleted)]
        [TestCase(ChapterSelectionFailureReason.CurrentNodeNotFound)]
        [TestCase(ChapterSelectionFailureReason.CurrentStageUncleared)]
        [TestCase(ChapterSelectionFailureReason.NodeNotFound)]
        [TestCase(ChapterSelectionFailureReason.AlreadyCleared)]
        [TestCase(ChapterSelectionFailureReason.FinalNodeReached)]
        [TestCase(ChapterSelectionFailureReason.NotConnected)]
        public void PlanSkip_NormalRefusalPreservesExactNavigationReason(ChapterSelectionFailureReason reason)
        {
            DevelopmentNavigationInput input = RefusedInput(reason, out StageNodeId target);
            Assert.That(Evaluate(input, target).FailureReason, Is.EqualTo(reason));

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanSkip(input, target);

            AssertRefused(plan, reason == ChapterSelectionFailureReason.AlreadyCleared
                ? DevelopmentFailureReason.AlreadyPassed : DevelopmentFailureReason.InvalidTarget, reason);
        }

        [TestCase(ChapterSelectionFailureReason.ChapterCompleted)]
        [TestCase(ChapterSelectionFailureReason.CurrentNodeNotFound)]
        [TestCase(ChapterSelectionFailureReason.CurrentStageUncleared)]
        [TestCase(ChapterSelectionFailureReason.NodeNotFound)]
        [TestCase(ChapterSelectionFailureReason.AlreadyCleared)]
        [TestCase(ChapterSelectionFailureReason.FinalNodeReached)]
        public void PlanJump_PreflightRefusalPreservesExactNavigationReason(ChapterSelectionFailureReason reason)
        {
            DevelopmentNavigationInput input = RefusedInput(reason, out StageNodeId target);
            Assert.That(Evaluate(input, target).FailureReason, Is.EqualTo(reason));

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(input, target);

            AssertRefused(plan, reason == ChapterSelectionFailureReason.AlreadyCleared
                ? DevelopmentFailureReason.AlreadyPassed : DevelopmentFailureReason.InvalidTarget, reason);
        }

        [Test]
        public void PlanJump_MissingSyntheticStartPreservesCurrentNodeNotFound()
        {
            DevelopmentNavigationInput input = Input(new[]
            {
                Node(2, 1, false, true, 3),
                Node(3, 2, false, false)
            });

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(input, Id(3));

            AssertRefused(plan, DevelopmentFailureReason.InvalidTarget,
                ChapterSelectionFailureReason.CurrentNodeNotFound);
        }

        [TestCase(1)]
        [TestCase(3)]
        public void PlanSkip_InvalidDepthEdgePreservesNotConnected(int targetDepth)
        {
            DevelopmentNavigationInput input = Input(new[]
            {
                Node(1, 0, false, false, 2),
                Node(2, 1, true, true, 3),
                Node(3, targetDepth, false, true, 4),
                Node(4, 4, false, false)
            }, current: 2);

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanSkip(input, Id(3));

            AssertRefused(plan, DevelopmentFailureReason.InvalidTarget,
                ChapterSelectionFailureReason.NotConnected);
        }

        [Test]
        public void PlanJump_InvalidDepthRouteHasNoSupportedPath()
        {
            DevelopmentNavigationInput input = Input(new[]
            {
                Node(1, 0, false, false, 2),
                Node(2, 1, false, true, 4),
                Node(4, 3, false, false)
            });

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(input, Id(4));

            AssertRefused(plan, DevelopmentFailureReason.NoSupportedPath, null);
        }

        [Test]
        public void PlanJump_MissingIntermediateNodeHasNoSupportedPath()
        {
            DevelopmentNavigationInput input = Input(new[]
            {
                Node(1, 0, false, false, 2),
                Node(2, 1, false, true, 99),
                Node(4, 3, false, false)
            });

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(input, Id(4));

            AssertRefused(plan, DevelopmentFailureReason.NoSupportedPath, null);
        }

        [Test]
        public void PlanJump_AlreadyClearedIntermediateCannotBeBypassedAgain()
        {
            DevelopmentNavigationInput input = Input(LinearGraph(clear2: true));

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(input, Id(4));

            AssertRefused(plan, DevelopmentFailureReason.NoSupportedPath, null);
            Assert.That(input.Nodes[1].Progress.IsClear, Is.True);
            Assert.That(input.Nodes[2].Progress.IsClear, Is.False);
        }

        [Test]
        public void PlanJump_BackwardTargetHasNoSupportedPath()
        {
            DevelopmentNode[] nodes = LinearGraph();
            nodes[2] = Node(3, 2, true, true, 4);

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(Input(nodes, current: 3), Id(2));

            AssertRefused(plan, DevelopmentFailureReason.NoSupportedPath, null);
        }

        [Test]
        [Timeout(1000)]
        public void PlanJump_MalformedBackwardCyclesDoNotHideAValidForwardPath()
        {
            DevelopmentNavigationInput input = Input(new[]
            {
                Node(1, 0, false, false, 2),
                Node(2, 1, false, true, 1, 3),
                Node(3, 2, false, true, 2, 4),
                Node(4, 3, false, false)
            });

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(input, Id(4));

            AssertSuccess(plan, 4, new[] { 2, 3 }, 3);
        }

        [Test]
        [Timeout(1000)]
        public void PlanJump_CycleWithoutForwardRouteTerminatesWithoutAPlanWrite()
        {
            DevelopmentNavigationInput input = Input(new[]
            {
                Node(1, 0, false, false, 2),
                Node(2, 1, false, true, 3),
                Node(3, 2, false, true, 2),
                Node(4, 3, false, false)
            });

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(input, Id(4));

            AssertRefused(plan, DevelopmentFailureReason.NoSupportedPath, null);
        }

        [Test]
        public void PlanJump_InputOwnsNodeListAndLinksAfterCallerCollectionsChange()
        {
            var links = new List<StageNodeId> { Id(2) };
            var nodes = new List<DevelopmentNode>
            {
                new DevelopmentNode(new ChapterNodeProgress(Id(1), StageKey.Empty, 0, false, links), false),
                Node(2, 1, false, true, 3),
                Node(3, 2, false, false)
            };
            DevelopmentNavigationInput input = Input(nodes);
            links.Clear();
            nodes[1] = Node(2, 1, true, false, 3);
            nodes.Clear();

            DevelopmentNavigationPlan plan = DevelopmentNavigationPlanner.PlanJump(input, Id(3));

            AssertSuccess(plan, 3, new[] { 2 }, 2);
        }

        [Test]
        public void PlanJump_LaterPlanningDoesNotChangeAnEarlierBypassList()
        {
            DevelopmentNavigationInput input = Input(LinearGraph());
            DevelopmentNavigationPlan first = DevelopmentNavigationPlanner.PlanJump(input, Id(4));

            DevelopmentNavigationPlan second = DevelopmentNavigationPlanner.PlanSkip(input, Id(2));

            AssertSuccess(first, 4, new[] { 2, 3 }, 3);
            AssertSuccess(second, 2, new[] { 2 }, 2);
        }

        private static void AssertSuccess(DevelopmentNavigationPlan plan, int target,
            int[] bypassed, int? selected)
        {
            Assert.That(plan.Reason, Is.EqualTo(DevelopmentFailureReason.None));
            Assert.That(plan.NavigationFailureReason, Is.Null);
            Assert.That(plan.TargetNodeId, Is.EqualTo(Id(target)));
            Assert.That(plan.TargetStageKey, Is.EqualTo(new StageKey(target * 10)));
            Assert.That(plan.BypassedNodes, Is.EqualTo(Array.ConvertAll(bypassed, Id)));
            Assert.That(plan.FinalSelectedNodeId,
                Is.EqualTo(selected.HasValue ? (StageNodeId?)Id(selected.Value) : null));
        }

        private static void AssertRefused(DevelopmentNavigationPlan plan, DevelopmentFailureReason reason,
            ChapterSelectionFailureReason? navigationReason)
        {
            Assert.That(plan.Reason, Is.EqualTo(reason));
            Assert.That(plan.NavigationFailureReason, Is.EqualTo(navigationReason));
            Assert.That(plan.BypassedNodes, Is.Empty);
            Assert.That(plan.FinalSelectedNodeId, Is.Null);
        }

        private static DevelopmentNavigationInput RefusedInput(ChapterSelectionFailureReason reason,
            out StageNodeId target)
        {
            DevelopmentNode[] nodes = LinearGraph();
            int current = 0;
            target = Id(2);
            switch (reason)
            {
                case ChapterSelectionFailureReason.ChapterCompleted:
                    return Input(nodes, isChapterClear: true);
                case ChapterSelectionFailureReason.CurrentNodeNotFound:
                    current = 99;
                    break;
                case ChapterSelectionFailureReason.CurrentStageUncleared:
                    current = 2;
                    target = Id(3);
                    break;
                case ChapterSelectionFailureReason.NodeNotFound:
                    target = Id(99);
                    break;
                case ChapterSelectionFailureReason.AlreadyCleared:
                    nodes[1] = Node(2, 1, true, true, 3);
                    break;
                case ChapterSelectionFailureReason.FinalNodeReached:
                    nodes[3] = Node(4, 3, true, false);
                    current = 4;
                    break;
                case ChapterSelectionFailureReason.NotConnected:
                    target = Id(4);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason));
            }
            return Input(nodes, current);
        }

        private static DevelopmentNavigationInput Input(IReadOnlyList<DevelopmentNode> nodes,
            int current = 0, bool isChapterClear = false)
        {
            var expected = new DevelopmentContext(
                new RunId(new Guid("78bf8051-cff6-4227-8008-1c0722c1539a")), 7, current != 0,
                current == 0 ? StageNodeId.Empty : Id(current),
                current == 0 ? StageKey.Empty : new StageKey(current * 10), DevelopmentFailureReason.None);
            return new DevelopmentNavigationInput(expected, isChapterClear, nodes);
        }

        private static DevelopmentNode[] LinearGraph(bool clear2 = false)
        {
            return new[]
            {
                Node(1, 0, false, false, 2),
                Node(2, 1, clear2, true, 3),
                Node(3, 2, false, true, 4),
                Node(4, 3, false, false)
            };
        }

        private static DevelopmentNode Node(int id, int depth, bool clear, bool canBypass,
            params int[] next)
        {
            return new DevelopmentNode(new ChapterNodeProgress(Id(id),
                depth == 0 ? StageKey.Empty : new StageKey(id * 10), depth, clear,
                Array.ConvertAll(next, Id)), canBypass);
        }

        private static StageNodeId Id(int value) => new StageNodeId(value);

        private static ChapterSelectionResult Evaluate(DevelopmentNavigationInput input, StageNodeId target,
            StageNodeId? source = null, IReadOnlyList<ChapterNodeProgress> progress = null)
        {
            if (progress == null)
            {
                var nodes = new ChapterNodeProgress[input.Nodes.Count];
                for (int i = 0; i < nodes.Length; i++) nodes[i] = input.Nodes[i].Progress;
                progress = nodes;
            }
            return ChapterNavigationRules.Evaluate(7, 7, 7, source ?? input.Expected.NodeId,
                input.IsChapterClear, progress, target);
        }

        private static ChapterNodeProgress[] SimulateBypasses(DevelopmentNavigationInput input,
            IReadOnlyList<StageNodeId> bypassed)
        {
            var cleared = new HashSet<StageNodeId>(bypassed);
            var progress = new ChapterNodeProgress[input.Nodes.Count];
            for (int i = 0; i < progress.Length; i++)
            {
                ChapterNodeProgress node = input.Nodes[i].Progress;
                progress[i] = new ChapterNodeProgress(node.NodeId, node.StageKey, node.Depth,
                    node.IsClear || cleared.Contains(node.NodeId), node.NextNodeIds);
            }
            return progress;
        }
    }
}
#endif
