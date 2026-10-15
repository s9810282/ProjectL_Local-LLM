using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Chapter;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class ChapterContinuationCommandsTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ClearedBoss_ChoosesExplicitRoute(bool final)
        {
            var result = Decide(final);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Kind.ToString(), Is.EqualTo(final ? "RunCompleted" : "NextChapterSelected"));
            Assert.That(result.NextChapterKey, Is.EqualTo(final ? 0 : 2));
            Assert.That(result.ChapterKey, Is.EqualTo(1));
            Assert.That(result.NodeId, Is.EqualTo(new StageNodeId(4)));
            Assert.That(result.StageKey, Is.EqualTo(new StageKey(102)));
        }

        [TestCase("RunEnded")]
        [TestCase("BattleRetained")]
        [TestCase("ChapterMismatch")]
        [TestCase("StageNotFound")]
        [TestCase("StageMismatch")]
        [TestCase("ChapterNotCleared")]
        [TestCase("InvalidBoss")]
        [TestCase("CompletionMismatch")]
        [TestCase("DefinitionUnavailable")]
        [TestCase("InvalidDefinition")]
        [TestCase("TargetNotRegistered")]
        [TestCase("TargetAlreadyCleared")]
        public void InvalidFacts_RejectWithoutAcceptedRoute(string reason)
        {
            var result = Decide(false, reason);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.FailureReason.ToString(), Is.EqualTo(reason));
            Assert.That(result.Kind.ToString(), Is.EqualTo("None"));
            Assert.That(result.NextChapterKey, Is.EqualTo(0));
        }

        [Test]
        public void ExplicitFinal_DoesNotRequireTargetRegistration()
        {
            Assert.That(Decide(true, "TargetNotRegistered").Accepted, Is.True);
        }

        [TestCase(0, ChapterContinuationDefinitionKind.NextChapter, 2)]
        [TestCase(1, ChapterContinuationDefinitionKind.NextChapter, 0)]
        [TestCase(1, ChapterContinuationDefinitionKind.NextChapter, 1)]
        [TestCase(1, ChapterContinuationDefinitionKind.FinalChapter, 2)]
        [TestCase(1, (ChapterContinuationDefinitionKind)99, 0)]
        public void InvalidDefinitionValues_AreRejected(int chapter, ChapterContinuationDefinitionKind kind, int target)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new ChapterContinuationDefinition(chapter, kind, target));

        [Test]
        public void ExplicitFinal_SecondChapterProducesSourceIdentity()
        {
            var result = ChapterContinuationCommands.Complete(new ChapterContinuationFacts(RunOutcome.InProgress,
                2, 2, new StageNodeId(3), new StageNodeId(3), new StageKey(202), true, false, true, true,
                true, true, false, false), ChapterContinuationDefinition.Final(2));
            Assert.That(result.Kind, Is.EqualTo(ChapterContinuationKind.RunCompleted));
            Assert.That(result.ChapterKey, Is.EqualTo(2));
            Assert.That(result.NodeId, Is.EqualTo(new StageNodeId(3)));
            Assert.That(result.StageKey, Is.EqualTo(new StageKey(202)));
        }

        private static ChapterContinuationResult Decide(bool final, string reason = null)
        {
            var definition = reason == "DefinitionUnavailable" ? null : final
                ? ChapterContinuationDefinition.Final(1)
                : ChapterContinuationDefinition.Next(reason == "InvalidDefinition" ? 3 : 1, 2);
            var facts = new ChapterContinuationFacts(reason == "RunEnded" ? RunOutcome.Defeat : RunOutcome.InProgress,
                1, reason == "ChapterMismatch" ? 2 : 1, new StageNodeId(4),
                new StageNodeId(reason == "StageMismatch" ? 3 : 4), new StageKey(102),
                reason != "StageNotFound", reason == "BattleRetained", reason != "ChapterNotCleared",
                true, reason != "InvalidBoss", reason != "CompletionMismatch", reason != "TargetNotRegistered",
                reason == "TargetAlreadyCleared");
            return ChapterContinuationCommands.Complete(facts, definition);
        }

    }
}
