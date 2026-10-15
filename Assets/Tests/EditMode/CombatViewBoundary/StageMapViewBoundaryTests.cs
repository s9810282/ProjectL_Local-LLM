using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Chapter;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Tests.CombatViewBoundary
{
    public sealed class StageMapViewBoundaryTests
    {
        [Test]
        public void NodeIntent_UsesSuppliedProgressFactsWithoutOwningOrMutatingThem()
        {
            var nodes = Facts(false);
            var selected = ChapterProgressionCommands.ClickNode(7, 7, 7,
                StageNodeId.Empty, false, nodes, new StageNodeId(2));
            Assert.That(selected.Accepted, Is.True);
            Assert.That(selected.StageKey, Is.EqualTo(new StageKey(100)));
            Assert.That(nodes[1].IsClear, Is.False);
            Assert.That(nodes[0].NextNodeIds, Is.EqualTo(new[] { new StageNodeId(2) }));
        }

        [Test]
        public void OldDisplayFacts_DoNotOverrideFreshClearedNodeRejection()
        {
            var displayed = Facts(false);
            Assert.That(ChapterProgressionCommands.ClickNode(7, 7, 7,
                StageNodeId.Empty, false, displayed, new StageNodeId(2)).Accepted, Is.True);
            var fresh = Facts(true);
            var rejected = ChapterProgressionCommands.ClickNode(7, 7, 7,
                StageNodeId.Empty, false, fresh, new StageNodeId(2));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.NavigationFailureReason, Is.EqualTo(ChapterSelectionFailureReason.AlreadyCleared));
            Assert.That(displayed[1].IsClear, Is.False);
            Assert.That(fresh[1].IsClear, Is.True);
        }

        private static ChapterNodeProgress[] Facts(bool cleared) => new[]
        {
            new ChapterNodeProgress(new StageNodeId(1), StageKey.Empty, 0, false, new[] { new StageNodeId(2) }),
            new ChapterNodeProgress(new StageNodeId(2), new StageKey(100), 1, cleared, Array.Empty<StageNodeId>())
        };
    }
}
