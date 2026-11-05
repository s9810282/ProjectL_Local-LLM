using System;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Runtime
{
    /// <summary>표현 전환의 유일한 단계 원본이다.</summary>
    public enum SandstormPhase
    {
        Hidden,
        Covering,
        Covered,
        Revealing
    }

    /// <summary>로딩 실행과 무관하게 가림·대기·걷기 및 표시 시간을 관리한다.</summary>
    public sealed class SandstormPlayback
    {
        private double _phaseTime;
        private double _duration;
        private float _revealStartOpacity;

        public SandstormPhase Phase { get; private set; }
        public double PresentationTime { get; private set; }
        public float Progress => _duration > 0.0 ? (float)Math.Min(_phaseTime / _duration, 1.0) : 0f;
        public bool IsCovered => Phase == SandstormPhase.Covered;
        public bool IsHidden => Phase == SandstormPhase.Hidden;
        /// <summary>완료 후 전체 화면이 같은 시간으로 부드럽게 사라지는 농도.</summary>
        public float VisibilityOpacity => IsHidden ? 0f :
            Phase == SandstormPhase.Revealing ? 1f - Ease(Progress) : 1f;

        /// <summary>숨김 상태에서 가리기를 시작한다. 진행 중 중복 요청은 유지한다.</summary>
        public void Cover(float duration)
        {
            ValidateDuration(duration);
            if (Phase == SandstormPhase.Revealing)
            {
                throw new InvalidOperationException("걷어내는 중에는 Cover를 호출할 수 없다. Reset 후 다시 시작해야 한다.");
            }
            if (Phase != SandstormPhase.Hidden)
            {
                return;
            }
            Begin(SandstormPhase.Covering, duration);
        }

        /// <summary>완전히 가려진 뒤 걷어내기를 시작한다.</summary>
        public void Reveal(float duration)
        {
            ValidateDuration(duration);
            if (Phase == SandstormPhase.Covering)
            {
                throw new InvalidOperationException("가림 완료 전에 Reveal을 호출할 수 없다. IsCovered를 먼저 확인해야 한다.");
            }
            if (Phase != SandstormPhase.Covered)
            {
                return;
            }
            _revealStartOpacity = JourneyOpacity;
            Begin(SandstormPhase.Revealing, duration);
        }

        /// <summary>완전 가림으로 즉시 전환하되 여정 시간은 보존한다.</summary>
        public void ShowCovered()
        {
            Begin(SandstormPhase.Covered, 1.0);
            _phaseTime = 0.28;
        }

        /// <summary>비음수 시간을 진행한다. 대기에는 자동 종료가 없다.</summary>
        public void Advance(double deltaTime)
        {
            if (double.IsNaN(deltaTime) || double.IsInfinity(deltaTime) || deltaTime < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }
            if (IsHidden)
            {
                return;
            }
            PresentationTime += deltaTime;
            if (Phase == SandstormPhase.Covered)
            {
                _phaseTime += deltaTime;
                return;
            }
            _phaseTime = Math.Min(_phaseTime + deltaTime, _duration);
            if (_phaseTime < _duration)
            {
                return;
            }
            if (Phase == SandstormPhase.Covering)
            {
                Begin(SandstormPhase.Covered, 1.0);
            }
            else
            {
                Reset();
            }
        }

        /// <summary>입력을 가리지 않는 초기 상태로 되돌린다.</summary>
        public void Reset()
        {
            Phase = SandstormPhase.Hidden;
            _phaseTime = 0.0;
            _duration = 0.0;
            PresentationTime = 0.0;
            _revealStartOpacity = 0f;
        }

        /// <summary>마스크 이동 시간과 독립적인 여정 표시 농도를 반환한다.</summary>
        public float JourneyOpacity
        {
            get
            {
                if (Phase == SandstormPhase.Covered)
                {
                    return Ease((float)Math.Min(_phaseTime / 0.28, 1.0));
                }
                if (Phase == SandstormPhase.Revealing)
                {
                    // 완료 페이드는 부모 CanvasGroup이 한 번만 적용한다.
                    return _revealStartOpacity;
                }
                return 0f;
            }
        }

        private void Begin(SandstormPhase phase, double duration)
        {
            Phase = phase;
            _phaseTime = 0.0;
            _duration = duration;
        }

        private static float Ease(float value) => value * value * (3f - 2f * value);

        private static void ValidateDuration(float duration)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), "전환 시간은 양수인 유한 값이어야 한다.");
            }
        }
    }
}
