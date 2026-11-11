using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 출처의 대상을 모두 해제하고 타격 신호(<see cref="CombatPieceSignal.Hit"/>)를 내는 시간 없는 조각입니다(클립 설계 AC7, 계획 T22).
    /// 해제가 대상의 피격 사건을 열고(Gate 정책), 해제 뒤에 타격 신호를 냅니다. 순서 규칙의 원본은 <see cref="CombatPieceSignal.Hit"/> 문서입니다.
    /// </summary>
    internal sealed class CombatReleasePiece : ICombatPresentationPiece
    {
        private readonly Action _release;
        private readonly Func<bool> _canRelease;
        private Action _releaseHit;

        /// <param name="release">출처의 대상을 모두 해제하는 입구입니다(<see cref="ICombatSourceLifetime.ReleaseAll"/>).</param>
        internal CombatReleasePiece(Action release, Func<bool> canRelease = null)
        {
            _release = release ?? throw new ArgumentNullException(nameof(release));
            _canRelease = canRelease;
        }

        public bool IsEnded { get; private set; }

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _releaseHit = CombatHitMoment.ReleaseThenSignal(_release, context.SignalCallback(CombatPieceSignal.Hit));
            TryRelease();
        }

        public void Tick(float deltaTime)
        {
            TryRelease();
        }

        public void Cancel()
        {
            _releaseHit = null;
        }

        private void TryRelease()
        {
            if (_releaseHit == null || IsEnded || _canRelease?.Invoke() == false) return;
            Action releaseHit = _releaseHit;
            _releaseHit = null;
            IsEnded = true;
            releaseHit();
        }
    }
}
