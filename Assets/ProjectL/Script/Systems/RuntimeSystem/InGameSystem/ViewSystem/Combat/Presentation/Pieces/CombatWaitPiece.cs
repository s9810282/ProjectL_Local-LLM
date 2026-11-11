using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 정해진 시간만 기다리는 조각입니다. 자원을 빌리지 않고 아무것도 움직이지 않습니다. 끝 신호가 그 시간 뒤라, 조립의 끝 기준 시점을 이 조각에 걸 수 있습니다.
    /// 밀림과 함께 가는 피격(넉백 피격 연출, 임시)이 피격 반응 대신 뼈대로 씁니다. 반응이 끝나는 시간(피격 시간)과 같은 시점에 끝 큐가 납니다.
    /// </summary>
    internal sealed class CombatWaitPiece : ICombatPresentationPiece
    {
        private readonly float _seconds;
        private float _elapsed;
        private bool _began;

        /// <param name="seconds">기다리는 시간(초)입니다. 0이면 시작 안에서 끝납니다.</param>
        internal CombatWaitPiece(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            _seconds = seconds;
        }

        public bool IsEnded => _began && _elapsed >= _seconds;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _elapsed = 0f;
            _began = true;
        }

        public void Tick(float deltaTime) => _elapsed += Math.Max(0f, deltaTime);

        public void Cancel()
        {
        }
    }
}
