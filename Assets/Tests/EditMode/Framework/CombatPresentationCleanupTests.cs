using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;

namespace ProjectL.Tests.Framework
{
    /// <summary>이동 취소 실패가 나머지 연출 정리를 막지 않는지 실제 Presentation으로 검증합니다.</summary>
    public sealed class CombatPresentationCleanupTests
    {
        [Test]
        public void NarrowMovementPresenter_WaitsForStepCompletion_AndDetachesOnce()
        {
            var buffers = CombatPreviewPublicationTests.Create(out _);
            var facts = buffers.CharacterReadSink.Characters[0].ToFacts();
            var display = new MovementOnlyPresenter();
            var playback = new CombatMovementPlayback(display);
            playback.BeginStep(new CombatCharacterEvent(CombatEventKind.Moved, facts, facts));
            playback.Tick(1f);
            Assert.That(playback.IsComplete, Is.False);
            display.IsMovementComplete = true;
            playback.Tick(0f);
            Assert.That(playback.IsComplete, Is.True);
            playback.ShowReturn(new CombatCharacterEvent(CombatEventKind.Returned, facts, facts));
            Assert.That(display.Returns, Is.EqualTo(1));
            playback.Detach();
            playback.Detach();
            Assert.That(display.Cancels, Is.EqualTo(1));
            Assert.Throws<ObjectDisposedException>(() =>
                playback.BeginStep(new CombatCharacterEvent(CombatEventKind.Moved, facts, facts)));
        }

        private sealed class MovementOnlyPresenter : ICombatMovementPlaybackPresenter
        {
            public bool IsMovementComplete { get; set; }
            internal int Returns, Cancels;
            public void BeginStep(CombatCharacterEvent step) => IsMovementComplete = false;
            public void ShowReturn(CombatCharacterEvent returned) => Returns++;
            public void CancelMovement() => Cancels++;
        }

        [Test]
        public void DelayedEvent_CancelRejectsOldCallback_AndNextResultCompletesOnce()
        {
            var display = new DelayedDisplay();
            var presentation = new OrderedCombatPresentation();
            presentation.Attach(display);
            var result = new CombatResult(1, CombatProcessingKind.Command, 1, true, string.Empty,
                events: new[] { new CombatAttackEvent(default) });
            int completed = 0;
            presentation.Play(result, _ => completed++);
            var old = display.Completed;
            presentation.Tick(1f);
            Assert.That(completed, Is.Zero);
            presentation.Cancel();
            old();
            presentation.Play(result, _ => completed++);
            old();
            presentation.Tick(1f);
            Assert.That(completed, Is.Zero);
            display.Completed();
            display.Completed();
            presentation.Tick(0f);
            presentation.Tick(0f);
            Assert.That(completed, Is.EqualTo(1));
            presentation.Detach();
        }

        private sealed class DelayedDisplay : CombatViewSystemTests.FakeDisplay
        {
            internal Action Completed;
            public override void BeginEvent(CombatEvent combatEvent, Action completed) => Completed = completed;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MovementCancelFailure_StillCancelsOtherPresentation_AndCollectsFailures(bool bothFail)
        {
            var display = new CleanupDisplay { FailMovement = true, FailPresentation = bothFail };
            var presentation = new OrderedCombatPresentation();
            presentation.Attach(display);

            var error = Assert.Throws<AggregateException>(presentation.Cancel);

            Assert.That(display.MovementCancels, Is.EqualTo(1));
            Assert.That(display.PresentationCancels, Is.EqualTo(1));
            Assert.That(error.InnerExceptions.Count, Is.EqualTo(bothFail ? 2 : 1));
            Assert.That(error.InnerExceptions[0], Is.SameAs(display.MovementError));
            if (bothFail) Assert.That(error.InnerExceptions[1], Is.SameAs(display.PresentationError));

            display.FailMovement = display.FailPresentation = false;
            Assert.DoesNotThrow(presentation.Detach);
            Assert.DoesNotThrow(presentation.Detach);
            Assert.That(display.MovementCancels, Is.EqualTo(1), "이미 분리한 이동 재생은 다시 취소하지 않는다.");
        }

        [Test]
        public void Cancel_ReentrantCleanup_DoesNotRecursivelyCancelAgain()
        {
            var display = new CleanupDisplay();
            var presentation = new OrderedCombatPresentation();
            presentation.Attach(display);
            display.OnCancel = presentation.Cancel;

            presentation.Cancel();

            Assert.That(display.MovementCancels, Is.EqualTo(1));
            Assert.That(display.PresentationCancels, Is.EqualTo(1));
            display.OnCancel = null;
            presentation.Detach();
        }

        [Test]
        public void Cancel_CanStartAnotherResultWithoutReusingDetachedMovement()
        {
            var buffers = CombatPreviewPublicationTests.Create(out _);
            var facts = buffers.CharacterReadSink.Characters[0].ToFacts();
            var presentation = new OrderedCombatPresentation();
            presentation.Attach(new CleanupDisplay());
            presentation.Cancel();
            int completed = 0;

            presentation.Play(new CombatResult(1, CombatProcessingKind.Command, 1, true, string.Empty,
                    events: new[] { new CombatCharacterEvent(CombatEventKind.Moved, facts, facts) }),
                _ => completed++);

            Assert.That(completed, Is.EqualTo(1));
            presentation.Detach();
        }

        private sealed class CleanupDisplay : CombatViewSystemTests.FakeDisplay
        {
            internal readonly Exception MovementError = new InvalidOperationException("이동 취소 실패");
            internal readonly Exception PresentationError = new InvalidOperationException("나머지 연출 취소 실패");
            internal bool FailMovement, FailPresentation;
            internal int MovementCancels, PresentationCancels;
            internal Action OnCancel;

            public override void CancelMovement()
            {
                MovementCancels++;
                if (FailMovement) throw MovementError;
            }

            public override void CancelPresentation()
            {
                PresentationCancels++;
                // 반복 재진입 결함도 스택 오버플로 대신 카운터 단언으로 검출한다.
                if (PresentationCancels < 3) OnCancel?.Invoke();
                if (FailPresentation) throw PresentationError;
            }
        }
    }
}
