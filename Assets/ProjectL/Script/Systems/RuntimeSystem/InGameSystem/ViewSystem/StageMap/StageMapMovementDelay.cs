using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>한 이동의 Delay 콜백을 현재 문맥에서만 전달합니다. 시간과 예약은 기존 서비스가 소유합니다.</summary>
    internal sealed class StageMapMovementDelay : IDelayService, IDisposable
    {
        private IDelayService _inner;
        private Func<bool> _isCurrent;
        private Action _tick;
        private Action _end;
        private bool _registered;
        private bool _disposed;

        internal StageMapMovementDelay(IDelayService inner, Func<bool> isCurrent)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
        }

        public float CurrentTime => (_inner ?? throw new ObjectDisposedException(nameof(StageMapMovementDelay))).CurrentTime;

        public void AddDelayEvent(DelayDataHandler data)
        {
            if (_disposed || _registered)
                throw new InvalidOperationException("[StageMapMovementDelay] 살아 있는 이동 한 번의 예약만 받을 수 있습니다.");
            _registered = true;
            _tick = data.TickAction;
            _end = data.EndAction;
            try
            {
                _inner.AddDelayEvent(new DelayDataHandler(End, Tick, data.DelayTime));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool CheckCurrent()
        {
            if (_disposed || _isCurrent == null) return false;
            if (_isCurrent() && !_disposed) return true;
            Dispose();
            return false;
        }

        private void Tick()
        {
            if (CheckCurrent()) _tick?.Invoke();
        }

        private void End()
        {
            if (!CheckCurrent()) return;
            Action end = _end;
            // End는 Motion에서 같은 시각을 읽을 수 있으므로 참조를 끊은 뒤 호출하고 시계는 finally에 해제합니다.
            _tick = null;
            _end = null;
            _isCurrent = null;
            try { end?.Invoke(); }
            finally { Dispose(); }
        }

        public void Dispose()
        {
            _disposed = true;
            _tick = null;
            _end = null;
            _isCurrent = null;
            _inner = null;
        }
    }
}
