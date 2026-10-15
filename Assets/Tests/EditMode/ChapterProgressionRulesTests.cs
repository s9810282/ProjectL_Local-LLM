using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.StageMap;

namespace ProjectL.Tests.Core
{
    public sealed class ChapterProgressionRulesTests
    {
        [Test]
        public void ChapterEntryAndClickRequestOnlyTheirOwnBufferWrites()
        {
            ChapterProgressionResult entry = ChapterProgressionRules.EvaluateChapterEntry(7, false);
            ChapterProgressionResult click = ChapterProgressionRules.EvaluateNodeClick(
                7, 7, 7, StageNodeId.Empty, false, Graph(), new StageNodeId(2));

            Assert.That(entry.Accepted && entry.SelectMap, Is.True);
            Assert.That(entry.SelectStage || entry.ClearStage || entry.ClearMap, Is.False);
            Assert.That(click.Accepted && click.SelectStage, Is.True);
            Assert.That(click.NodeId, Is.EqualTo(new StageNodeId(2)));
            Assert.That(click.StageKey, Is.EqualTo(new StageKey(100)));
            Assert.That(click.ClearStage || click.ClearMap || click.EndAttempt, Is.False);
        }

        [Test]
        public void RejectedClickAndCompletedChapterHaveNoEffects()
        {
            ChapterProgressionResult click = ChapterProgressionRules.EvaluateNodeClick(
                7, 7, 7, StageNodeId.Empty, false, Graph(), new StageNodeId(3));
            ChapterProgressionResult entry = ChapterProgressionRules.EvaluateChapterEntry(7, true);

            Assert.That(click.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.NavigationRejected));
            Assert.That(click.NavigationFailureReason, Is.EqualTo(ChapterSelectionFailureReason.NotConnected));
            Assert.That(click.SelectMap || click.SelectStage || click.ClearStage || click.ClearMap, Is.False);
            Assert.That(entry.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.ChapterCompleted));
            Assert.That(entry.SelectMap, Is.False);
        }

        [Test]
        public void VictoryClearsStageAndBossVictoryAlsoClearsChapter()
        {
            ChapterProgressionResult normal = Outcome(2, 100, false, StageOutcome.Victory);
            ChapterProgressionResult boss = Outcome(3, 900, true, StageOutcome.Victory);

            Assert.That(normal.Accepted && normal.ClearStage, Is.True);
            Assert.That(normal.ClearMap || normal.EndAttempt, Is.False);
            Assert.That(boss.Accepted && boss.ClearStage && boss.ClearMap, Is.True);
            Assert.That(boss.SelectMap || boss.SelectStage, Is.False);
        }

        [Test]
        public void DefeatEndsAttemptWithoutClearingAndRepeatVictoryIsRejected()
        {
            ChapterProgressionResult defeat = Outcome(2, 100, false, StageOutcome.Defeat);
            ChapterNodeProgress[] cleared =
            {
                new ChapterNodeProgress(new StageNodeId(1), StageKey.Empty, 0, false,
                    new[] { new StageNodeId(2) }),
                new ChapterNodeProgress(new StageNodeId(2), new StageKey(100), 1, true,
                    new[] { new StageNodeId(3) })
            };
            ChapterProgressionResult repeat = ChapterProgressionRules.EvaluateStageOutcome(
                7, 7, 7, new StageNodeId(2), new StageKey(100),
                false, cleared, StageOutcome.Victory, false);

            Assert.That(defeat.Accepted && defeat.EndAttempt, Is.True);
            Assert.That(defeat.ClearStage || defeat.ClearMap, Is.False);
            Assert.That(repeat.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.StageAlreadyCleared));
            Assert.That(repeat.ClearStage, Is.False);
        }

        [Test]
        public void CompletionRequiresTheSelectedNodeAndValidBossPosition()
        {
            ChapterProgressionResult wrongStage = ChapterProgressionRules.EvaluateStageOutcome(
                7, 7, 7, new StageNodeId(2), new StageKey(900),
                false, Graph(), StageOutcome.Victory, false);
            ChapterProgressionResult wrongBoss = Outcome(2, 100, true, StageOutcome.Victory);

            Assert.That(wrongStage.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.StageMismatch));
            Assert.That(wrongBoss.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.BossNodeHasSuccessors));
            Assert.That(wrongBoss.ClearStage || wrongBoss.ClearMap, Is.False);
        }

        private static ChapterProgressionResult Outcome(int node, int stage, bool boss, StageOutcome outcome) =>
            ChapterProgressionRules.EvaluateStageOutcome(7, 7, 7, new StageNodeId(node),
                new StageKey(stage), false, Graph(), outcome, boss);

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
