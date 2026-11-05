using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>
    /// UI 입력 콜백(EventSystem, 런타임 Tick 밖)의 예외를 오류 경계로 보냅니다. 이미 정지했으면 안쪽을 부르지 않습니다.
    /// 로비 덱·아티팩트 닫기처럼 UIInputBinding에 거는 수신자를 감쌉니다(로비 리팩토링 2차 설계 M-01).
    /// </summary>
    internal sealed class FaultReportingUIInputReceiver : IUIInputReceiver
    {
        private readonly IUIInputReceiver _inner;
        private readonly RuntimeFaultBoundary _faults;

        internal FaultReportingUIInputReceiver(IUIInputReceiver inner, RuntimeFaultBoundary faults)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        public void Click(int slot)
        {
            if (_faults.IsFaulted) return;
            try { _inner.Click(slot); }
            catch (Exception error) { _faults.Report(error); }
        }

        public void HoverEnter(int slot)
        {
            if (_faults.IsFaulted) return;
            try { _inner.HoverEnter(slot); }
            catch (Exception error) { _faults.Report(error); }
        }

        public void HoverExit(int slot)
        {
            if (_faults.IsFaulted) return;
            try { _inner.HoverExit(slot); }
            catch (Exception error) { _faults.Report(error); }
        }
    }
}
