using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;

namespace ProjectL.Tests.Framework
{
    /// <summary>View 계획 C.3a·C.5a: 결과 재생 수명(설계 §9.1). 자원 해제 계약은 §8 X3에서 검증한다.</summary>
    public sealed class CombatResultPlaybackTests
    {
        private FakeCombatPresentation _presentation;
        private CombatResultPlayback _playback;

        [SetUp]
        public void SetUp()
        {
            _presentation = new FakeCombatPresentation();
            _playback = new CombatResultPlayback(_presentation);
            _playback.Attach();
        }

        [Test]
        public void Constructor_RejectsNullPresentation()
        {
            Assert.Throws<ArgumentNullException>(() => new CombatResultPlayback(null));
        }

        [Test]
        public void ImmediateCompletion_AllowsOneFinalization_ThenClears()
        {
            _presentation.CompleteImmediately = true;

            CombatPlaybackToken started = _playback.Begin(Result(1));

            Assert.That(_playback.IsCurrent(started), Is.True);
            Assert.That(_playback.TryBeginFinalization(out CombatPlaybackToken token), Is.True);
            Assert.That(_playback.IsFinalizing, Is.True);
            Assert.That(_playback.TryBeginFinalization(out _), Is.False, "후처리 진입은 한 번만 허용한다.");

            _playback.CompleteFinalization(token);

            Assert.That(_playback.IsBusy, Is.False);
            Assert.That(_playback.IsFinalizing, Is.False);
            Assert.That(_playback.IsCurrent(token), Is.False);
        }

        [Test]
        public void DelayedCompletion_AllowsFinalizationOnlyAfterNotification()
        {
            _playback.Begin(Result(1));

            Assert.That(_playback.IsBusy, Is.True);
            Assert.That(_playback.PresentingSequence, Is.EqualTo(1));
            Assert.That(_playback.TryBeginFinalization(out _), Is.False);

            _presentation.Finish(CombatPresentationOutcome.Completed);

            Assert.That(_playback.TryBeginFinalization(out _), Is.True);
        }

        [Test]
        public void Begin_WhileBusy_IsRejected_SoTheSameResultIsNotPlayedTwice()
        {
            _playback.Begin(Result(1));

            Assert.Throws<InvalidOperationException>(() => _playback.Begin(Result(1)));
            Assert.That(_presentation.Played.Count, Is.EqualTo(1));
        }

        [Test]
        public void Begin_WithoutAttach_IsRejected()
        {
            var playback = new CombatResultPlayback(_presentation);

            Assert.Throws<InvalidOperationException>(() => playback.Begin(Result(1)));
            Assert.That(_presentation.Played, Is.Empty);
        }

        [Test]
        public void FailedNotification_IsReadable_AndBlocksFinalization()
        {
            var error = new InvalidOperationException("연출 실패");
            _playback.Begin(Result(1));

            _presentation.Finish(CombatPresentationOutcome.Failure(error));

            Assert.That(_playback.TryGetFailure(out Exception failure), Is.True);
            Assert.That(failure, Is.SameAs(error));
            Assert.That(_playback.TryBeginFinalization(out _), Is.False);
        }

        [Test]
        public void DuplicateNotification_IsRecordedAsFailure_WithoutSecondFinalization()
        {
            _presentation.CompleteImmediately = true;
            _playback.Begin(Result(1));

            _presentation.Finish(CombatPresentationOutcome.Completed);

            Assert.That(_playback.TryGetFailure(out Exception failure), Is.True);
            Assert.That(failure, Is.InstanceOf<InvalidOperationException>());
            Assert.That(_playback.TryBeginFinalization(out _), Is.False);
        }

        [Test]
        public void DefaultOutcome_IsNotTreatedAsCompletion()
        {
            _playback.Begin(Result(1));

            _presentation.Finish(default);

            Assert.That(_playback.TryGetFailure(out _), Is.True);
            Assert.That(_playback.TryBeginFinalization(out _), Is.False);
        }

        [Test]
        public void Abort_InvalidatesFirst_ThenCancels_AndIgnoresLateNotification()
        {
            CombatPlaybackToken token = _playback.Begin(Result(1));

            _playback.Abort();

            Assert.That(_presentation.CancelCount, Is.EqualTo(1));
            Assert.That(_playback.IsAttached, Is.False);
            Assert.That(_playback.IsBusy, Is.False);
            Assert.That(_playback.IsCurrent(token), Is.False);

            _presentation.Finish(CombatPresentationOutcome.Completed);

            Assert.That(_playback.TryGetFailure(out _), Is.False);
            Assert.That(_playback.TryBeginFinalization(out _), Is.False);
            Assert.Throws<InvalidOperationException>(() => _playback.Begin(Result(2)));
        }

        [Test]
        public void RepeatedAbort_IsSafe_AndCancelsOnlyActivePlayback()
        {
            _playback.Abort();
            Assert.That(_presentation.CancelCount, Is.Zero, "재생이 없으면 취소할 연출도 없다.");

            _playback.Attach();
            _playback.Begin(Result(1));
            _playback.Abort();
            _playback.Abort();

            Assert.That(_presentation.CancelCount, Is.EqualTo(1));
        }

        [Test]
        public void AbortDuringPlay_MakesInPlayCompletionStale()
        {
            _presentation.OnPlay = () => _playback.Abort();
            _presentation.CompleteImmediately = true;

            CombatPlaybackToken token = _playback.Begin(Result(1));

            Assert.That(_playback.IsCurrent(token), Is.False);
            Assert.That(_playback.TryBeginFinalization(out _), Is.False);
            Assert.That(_presentation.CancelCount, Is.EqualTo(1));
        }

        [Test]
        public void SynchronousExceptionAfterCompletion_IsPropagated()
        {
            _presentation.CompleteImmediately = true;
            _presentation.ThrowAfterNotify = true;

            Assert.Throws<InvalidOperationException>(() => _playback.Begin(Result(1)));
        }

        [Test]
        public void NewAttach_AbortsPreviousGeneration_AndOldNotificationDoesNotFinishNewPlayback()
        {
            _playback.Begin(Result(1));
            _playback.Attach();
            Assert.That(_presentation.CancelCount, Is.EqualTo(1));

            _playback.Begin(Result(1));
            _presentation.Callbacks[0](CombatPresentationOutcome.Completed);

            Assert.That(_playback.TryBeginFinalization(out _), Is.False, "이전 세대의 늦은 완료를 새 재생에 적용하지 않는다.");

            _presentation.Finish(CombatPresentationOutcome.Completed);
            Assert.That(_playback.TryBeginFinalization(out _), Is.True);
        }

        [Test]
        public void CompleteFinalization_RejectsStaleToken()
        {
            _presentation.CompleteImmediately = true;
            _playback.Begin(Result(1));
            _playback.TryBeginFinalization(out CombatPlaybackToken token);
            _playback.Abort();

            Assert.Throws<InvalidOperationException>(() => _playback.CompleteFinalization(token));
            Assert.Throws<InvalidOperationException>(() => _playback.CompleteFinalization(default));
        }

        private static CombatResult Result(int sequence)
        {
            return new CombatResult(sequence, CombatProcessingKind.Command, sequence, true, string.Empty);
        }
    }
}
