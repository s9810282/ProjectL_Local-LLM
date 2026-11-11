using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput
{
    /// <summary>
    /// 맵 선택 입력 콜백(InputSystem, 런타임 Tick 밖)의 예외를 오류 경계로 보냅니다. 이미 정지했으면 안쪽을 부르지 않습니다
    /// (스테이지 선택 리팩토링 설계 §2.4, 로비 <c>FaultReportingLobbyReceiver</c>와 같은 꼴).
    /// </summary>
    internal sealed class FaultReportingStageSelectionReceiver : IStageSelectionInputReceiver
    {
        private readonly IStageSelectionInputReceiver _inner;
        private readonly RuntimeFaultBoundary _faults;

        internal FaultReportingStageSelectionReceiver(IStageSelectionInputReceiver inner, RuntimeFaultBoundary faults)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        public void PointerMoved(float screenX, float screenY)
        {
            if (_faults.IsFaulted) return;
            try { _inner.PointerMoved(screenX, screenY); }
            catch (Exception error) { _faults.Report(error); }
        }

        public void ScrollRequested(float scrollY, float screenX, float screenY)
        {
            if (_faults.IsFaulted) return;
            try { _inner.ScrollRequested(scrollY, screenX, screenY); }
            catch (Exception error) { _faults.Report(error); }
        }

        public void SelectPressed(float screenX, float screenY)
        {
            if (_faults.IsFaulted) return;
            try { _inner.SelectPressed(screenX, screenY); }
            catch (Exception error) { _faults.Report(error); }
        }

        public void SelectReleased(float screenX, float screenY)
        {
            if (_faults.IsFaulted) return;
            try { _inner.SelectReleased(screenX, screenY); }
            catch (Exception error) { _faults.Report(error); }
        }

        public void SelectCanceled()
        {
            if (_faults.IsFaulted) return;
            try { _inner.SelectCanceled(); }
            catch (Exception error) { _faults.Report(error); }
        }

        public void ConfirmRequested()
        {
            if (_faults.IsFaulted) return;
            try { _inner.ConfirmRequested(); }
            catch (Exception error) { _faults.Report(error); }
        }

        public void CancelRequested()
        {
            if (_faults.IsFaulted) return;
            try { _inner.CancelRequested(); }
            catch (Exception error) { _faults.Report(error); }
        }
    }
}
