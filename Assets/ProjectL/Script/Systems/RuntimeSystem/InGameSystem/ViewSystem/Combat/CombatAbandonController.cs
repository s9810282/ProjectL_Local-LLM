using System;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 포기 버튼·확인창의 View 쪽 수명입니다(포기 UI 설계 C2~C7, SOLID·SSOT 개선 설계 D2).
    /// 확인창 열림 상태를 소유하고, 버튼 가능 여부를 현재 사실로 맞추며, 확인되면 골격에 한 번 요청합니다.
    /// </summary>
    /// <remarks>
    /// 가능 조건은 <see cref="CombatInputPolicy.CanRequestAbandon"/> 한 곳이고, 최종 허용과 이탈 순서는 골격
    /// <see cref="ICombatAbandonRequest"/>가 정합니다. 확인창이 열린 동안의 전투 입력 차단은 입력 입구(CombatViewSystem)가
    /// <see cref="IsConfirmOpen"/>을 읽어 합니다. 버튼 콜백은 Unity EventSystem에서 오므로 예외를 오류 경계로 보고합니다.
    /// </remarks>
    internal sealed class CombatAbandonController
    {
        private readonly CombatExecutionMode _executionMode;
        private readonly RuntimeFaultBoundary _faults;
        private readonly Func<CombatInputFacts> _readFacts;
        // 오류 아님·진입 활성·씬 연결을 조정자가 판단해 돌려준다.
        private readonly Func<bool> _canAct;
        // 확인창을 열기 전에 Drag·조준·경로 표시를 정리한다(P3).
        private readonly Action _cancelInputPresentation;
        // 버튼 연결·해제에 같은 대리자를 쓰도록 한 번만 만든다.
        private readonly Action _onOpen;
        private readonly Action _onConfirm;
        private readonly Action _onCancel;
        private ICombatAbandonDisplay _display;
        private ICombatAbandonRequest _request;

        internal CombatAbandonController(
            CombatExecutionMode executionMode,
            RuntimeFaultBoundary faults,
            Func<CombatInputFacts> readFacts,
            Func<bool> canAct,
            Action cancelInputPresentation)
        {
            _executionMode = executionMode;
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _readFacts = readFacts ?? throw new ArgumentNullException(nameof(readFacts));
            _canAct = canAct ?? throw new ArgumentNullException(nameof(canAct));
            _cancelInputPresentation = cancelInputPresentation ?? throw new ArgumentNullException(nameof(cancelInputPresentation));
            _onOpen = Open;
            _onConfirm = Confirm;
            _onCancel = Cancel;
        }

        /// <summary>확인창이 열려 있는지입니다. 열린 동안 전투 입력은 받지 않습니다(P3).</summary>
        internal bool IsConfirmOpen { get; private set; }

        private bool IsLive => _executionMode == CombatExecutionMode.Live;

        /// <summary>
        /// 씬 연결 때 버튼 입구를 연결하고 확인창을 닫습니다. RecordInputOnly는 포기할 수 없으므로 버튼을 숨깁니다(C7).
        /// 누를 수 있는지는 Tick의 <see cref="Sync"/>가 맞춥니다.
        /// </summary>
        internal void Attach(ICombatAbandonDisplay display, ICombatAbandonRequest request)
        {
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _request = request ?? throw new ArgumentNullException(nameof(request));
            IsConfirmOpen = false;
            display.BindAbandon(_onOpen, _onConfirm, _onCancel);
            display.HideAbandonConfirm();
            display.SetAbandonAvailable(IsLive, false);
        }

        /// <summary>
        /// 버튼 가능 여부를 현재 사실로 맞춥니다(C2·C3). 이탈 상태는 View 밖(LifeCycle)에서 바뀌므로 Tick마다 부르고,
        /// 실제 쓰기는 패널이 값이 바뀔 때만 합니다. 가능 조건이 깨지면 열린 확인창을 닫습니다(C6).
        /// </summary>
        internal void Sync()
        {
            if (_display == null)
                return;

            bool available = IsLive && CanRequestNow();
            _display.SetAbandonAvailable(IsLive, available);
            if (!available)
                Close();
        }

        /// <summary>열린 확인창을 닫습니다. 열려 있지 않으면 아무것도 하지 않습니다(이탈 시작 때, C6).</summary>
        internal void Close()
        {
            if (!IsConfirmOpen)
                return;

            IsConfirmOpen = false;
            _display?.HideAbandonConfirm();
        }

        /// <summary>오류 정지·입력 정리: 확인창을 닫고 버튼을 누를 수 없게 합니다. 연결은 유지합니다.</summary>
        internal void Stop()
        {
            IsConfirmOpen = false;
            if (_display == null)
                return;

            _display.HideAbandonConfirm();
            _display.SetAbandonAvailable(IsLive, false);
        }

        /// <summary>씬 연결 해제: 참조만 놓습니다. 버튼 연결은 표시의 Release가 끊습니다.</summary>
        internal void Detach()
        {
            IsConfirmOpen = false;
            _display = null;
            _request = null;
        }

        private bool CanRequestNow()
        {
            return CombatInputPolicy.CanRequestAbandon(_readFacts(), _request.ExitStatus == CombatExitStatus.Idle);
        }

        /// <summary>포기 버튼의 입구입니다. UI EventSystem으로 들어오므로 입력 Layer와 무관합니다.</summary>
        private void Open()
        {
            if (IsConfirmOpen || _display == null || !_canAct())
                return;

            try
            {
                if (!CanRequestNow())
                    return;

                IsConfirmOpen = true;
                _cancelInputPresentation();
                _display.ShowAbandonConfirm();
            }
            catch (Exception exception)
            {
                _faults.Report(exception);
            }
        }

        /// <summary>확인창의 확인입니다. 확인창을 닫고, 아직 가능하면 골격에 한 번 요청합니다. false면 아무것도 하지 않습니다(C4).</summary>
        private void Confirm()
        {
            if (!IsConfirmOpen)
                return;

            try
            {
                Close();
                if (_display == null || !_canAct() || !CanRequestNow())
                    return;

                _request.RequestAbandon();
            }
            catch (Exception exception)
            {
                _faults.Report(exception);
            }
        }

        private void Cancel()
        {
            if (!IsConfirmOpen)
                return;

            try
            {
                Close();
            }
            catch (Exception exception)
            {
                _faults.Report(exception);
            }
        }
    }
}
