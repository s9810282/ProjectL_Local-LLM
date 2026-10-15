using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;

namespace ProjectL.Tests.Framework
{
    /// <summary>View 계획 C.3: 변경 신호 표(설계 §5.3, W9). 전투 설치·정리는 신호가 아니다(개선 설계 D9·D13).</summary>
    public sealed class CombatViewRefreshTests
    {
        [TestCase(0)]
        [TestCase(3)]
        public void SameSequence_WithoutResult_IsNormalAndHasNoResult(int sequence)
        {
            var refresh = new CombatViewRefresh();

            CombatRefreshSignal signal = refresh.Observe(sequence, sequence, null, 0);

            Assert.That(signal.Violation, Is.Null);
            Assert.That(signal.UnpresentedResult, Is.Null);
        }

        [Test]
        public void SameSequence_WithPresentedResult_DoesNotOfferItAgain()
        {
            var refresh = new CombatViewRefresh();

            CombatRefreshSignal signal = refresh.Observe(2, 2, Result(2), 0);

            Assert.That(signal.Violation, Is.Null);
            Assert.That(signal.UnpresentedResult, Is.Null);
        }

        [Test]
        public void OneNewResult_IsOffered()
        {
            var refresh = new CombatViewRefresh();
            CombatResult result = Result(3);

            CombatRefreshSignal signal = refresh.Observe(3, 2, result, 0);

            Assert.That(signal.Violation, Is.Null);
            Assert.That(signal.UnpresentedResult, Is.SameAs(result));
        }

        [TestCase(1, 0, false, 0, TestName = "Unpresented result missing")]
        [TestCase(3, 2, true, 4, TestName = "Result sequence differs from published sequence")]
        [TestCase(2, 2, true, 1, TestName = "Presented result sequence differs")]
        [TestCase(1, 2, false, 0, TestName = "Published sequence went backwards")]
        [TestCase(3, 1, true, 3, TestName = "Result skipped")]
        public void ContractViolation_IsReported_WithoutOfferingAResult(
            int latest, int presented, bool hasResult, int resultSequence)
        {
            var refresh = new CombatViewRefresh();

            CombatRefreshSignal signal = refresh.Observe(latest, presented, hasResult ? Result(resultSequence) : null, 0);

            Assert.That(signal.Violation, Is.Not.Null.And.Not.Empty);
            Assert.That(signal.UnpresentedResult, Is.Null);
        }

        [Test]
        public void Violation_DoesNotConsumeThePreviewAnswer()
        {
            var refresh = new CombatViewRefresh();

            Assert.That(refresh.Observe(1, 0, null, 2).Violation, Is.Not.Null);

            Assert.That(refresh.Observe(0, 0, null, 2).PreviewAnswered, Is.True, "위반이면 확인한 응답 번호를 바꾸지 않는다.");
        }

        [Test]
        public void Reset_ObservesTheCurrentPreviewAnswerAgain()
        {
            var refresh = new CombatViewRefresh();
            refresh.Observe(0, 0, null, 2);

            refresh.Reset();

            Assert.That(refresh.Observe(0, 0, null, 2).PreviewAnswered, Is.True);
        }

        [Test]
        public void PreviewAnswer_IsObservedOncePerNewNumber()
        {
            var refresh = new CombatViewRefresh();

            Assert.That(refresh.Observe(0, 0, null, 2).PreviewAnswered, Is.True);
            Assert.That(refresh.Observe(0, 0, null, 2).PreviewAnswered, Is.False);
            Assert.That(refresh.Observe(0, 0, null, 5).PreviewAnswered, Is.True);
        }

        private static CombatResult Result(int sequence)
        {
            return new CombatResult(sequence, CombatProcessingKind.Advance, 0, true, string.Empty);
        }
    }
}
