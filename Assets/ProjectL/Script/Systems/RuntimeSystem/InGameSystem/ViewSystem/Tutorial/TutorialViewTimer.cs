using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 표시된 설명 하나의 자동 진행 남은 시간입니다. 남은 시간의 유일한 원본이며 실행 소유자만 가집니다(튜토리얼 설계 §15.4.1).
    /// </summary>
    /// <remarks>
    /// 시간 배율이 없는 실제 시간(주입한 시계)으로 창이 활성인 동안만 깎습니다. 비활성이 되면 남은 시간을 그대로 두고,
    /// 다시 활성이 되면 측정 기준을 그 순간으로 다시 잡아 창을 벗어나 있던 시간을 빼지 않습니다. 마감 시각을 따로 두지 않습니다.
    /// </remarks>
    internal sealed class TutorialViewTimer
    {
        private readonly Func<double> _clock;
        private int _sessionId;
        private int _stepRevision;
        private double _remainingSeconds;
        private double _measuredAt;
        private bool _isRunning;
        private bool _isFocused = true;

        internal TutorialViewTimer(Func<double> clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>그 설명의 시간을 처음부터 셉니다. 창 활성 여부는 시작할 때 다시 읽은 값입니다(설계 §15.4.1 1번).</summary>
        internal void Start(int sessionId, int stepRevision, double seconds, bool isFocused)
        {
            if (sessionId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sessionId), sessionId, "0보다 커야 합니다.");
            if (!(seconds > 0d) || double.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "0보다 큰 유한 값이어야 합니다.");

            _sessionId = sessionId;
            _stepRevision = stepRevision;
            _remainingSeconds = seconds;
            _isFocused = isFocused;
            _measuredAt = _clock();
            _isRunning = true;
        }

        /// <summary>마지막으로 잰 뒤 지난 활성 시간을 깎습니다. 실행 소유자 Tick이 부릅니다.</summary>
        internal void Advance()
        {
            if (!_isRunning || !_isFocused)
                return;

            double now = _clock();
            _remainingSeconds -= Math.Max(0d, now - _measuredAt);
            _measuredAt = now;
        }

        /// <summary>
        /// 창 활성 변경을 반영합니다. 비활성이 되면 그때까지의 활성 시간을 깎고 멈추며,
        /// 다시 활성이 되면 측정 기준만 지금으로 다시 잡습니다(설계 §15.4.1 2·3번).
        /// </summary>
        internal void SetFocused(bool isFocused)
        {
            if (isFocused == _isFocused)
                return;

            if (!isFocused)
                Advance();
            _isFocused = isFocused;
            if (isFocused)
                _measuredAt = _clock();
        }

        /// <summary>그 설명의 시간이 다 됐는지입니다. 다른 설명이나 멈춘 시간이면 false입니다.</summary>
        internal bool IsExpired(int sessionId, int stepRevision)
        {
            return _isRunning && _sessionId == sessionId && _stepRevision == stepRevision && _remainingSeconds <= 0d;
        }

        /// <summary>세는 것을 그만둡니다. 창 활성 여부는 그대로 둡니다.</summary>
        internal void Stop()
        {
            _isRunning = false;
            _sessionId = 0;
            _stepRevision = 0;
            _remainingSeconds = 0d;
        }
    }
}
