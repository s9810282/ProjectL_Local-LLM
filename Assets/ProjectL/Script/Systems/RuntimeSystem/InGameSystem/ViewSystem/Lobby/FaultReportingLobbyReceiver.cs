using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 로비 시설 입력 콜백(InputSystem, 런타임 Tick 밖)의 예외를 오류 경계로 보냅니다. 이미 정지했으면 안쪽을 부르지 않습니다
    /// (로비 리팩토링 2차 설계 M-01).
    /// </summary>
    internal sealed class FaultReportingLobbyReceiver : ILobbyInputReceiver
    {
        private readonly ILobbyInputReceiver _inner;
        private readonly RuntimeFaultBoundary _faults;

        internal FaultReportingLobbyReceiver(ILobbyInputReceiver inner, RuntimeFaultBoundary faults)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        public void OnPointer(float x, float y)
        {
            if (_faults.IsFaulted) return;
            try { _inner.OnPointer(x, y); }
            catch (Exception error) { _faults.Report(error); }
        }

        public void OnSelectPressed()
        {
            if (_faults.IsFaulted) return;
            try { _inner.OnSelectPressed(); }
            catch (Exception error) { _faults.Report(error); }
        }

        public void OnSelectReleased()
        {
            if (_faults.IsFaulted) return;
            try { _inner.OnSelectReleased(); }
            catch (Exception error) { _faults.Report(error); }
        }

        public void OnSelectCanceled()
        {
            if (_faults.IsFaulted) return;
            try { _inner.OnSelectCanceled(); }
            catch (Exception error) { _faults.Report(error); }
        }
    }
}
