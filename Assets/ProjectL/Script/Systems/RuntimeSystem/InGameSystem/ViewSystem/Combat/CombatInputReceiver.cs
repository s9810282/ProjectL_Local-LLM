using System;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 전투 입력의 수신 창구입니다(SOLID·SSOT 개선 설계 D11). 스테이지 입력 슬롯에 붙었다 떨어지고,
    /// 들어온 입력을 입구 검사 뒤 Router로 넘깁니다.
    /// </summary>
    /// <remarks>
    /// 소유: 슬롯 연결 상태(<see cref="IsBound"/>)와 입구 검사(오류·연결·활성·포기 확인창)·분배 예외 보고.
    /// 하지 않는 것: 목표 Layer 계산과 Layer 변경(조정자), 입력 해석·허용 판단(Router·Policy·각 입력 소유자).
    /// Unity 입력 콜백은 Runtime Tick 밖에서 오므로 분배 중 예외는 같은 오류 경계로 보고합니다(§10.1).
    /// </remarks>
    internal sealed class CombatInputReceiver : IStageInputReceiver
    {
        private readonly IStageInputReceiverSlot _slot;
        private readonly RuntimeFaultBoundary _faults;
        // 진입 활성·Router 있음을 조정자가 판단해 돌려준다.
        private readonly Func<bool> _canReceive;
        // 포기 확인창이 열린 동안은 표현 정리 신호만 통과시킨다(포기 UI 설계 P3).
        private readonly Func<bool> _isConfirmOpen;
        private readonly Action<CombatInputRouter.Request, float, float> _route;

        internal CombatInputReceiver(
            IStageInputReceiverSlot slot,
            RuntimeFaultBoundary faults,
            Func<bool> canReceive,
            Func<bool> isConfirmOpen,
            Action<CombatInputRouter.Request, float, float> route)
        {
            _slot = slot ?? throw new ArgumentNullException(nameof(slot));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _canReceive = canReceive ?? throw new ArgumentNullException(nameof(canReceive));
            _isConfirmOpen = isConfirmOpen ?? throw new ArgumentNullException(nameof(isConfirmOpen));
            _route = route ?? throw new ArgumentNullException(nameof(route));
        }

        /// <summary>입력 슬롯에 연결돼 있는지입니다. 입력 허용 판단(facts)의 입력 가용성에 쓰입니다.</summary>
        internal bool IsBound { get; private set; }

        /// <summary>입력 슬롯에 연결합니다. 이미 연결돼 있으면 아무것도 하지 않습니다.</summary>
        internal void Bind()
        {
            if (IsBound)
                return;

            _slot.Bind(this);
            IsBound = true;
        }

        /// <summary>
        /// 입력 슬롯에서 떨어집니다. 슬롯은 진행 중인 선택이 있으면 이 호출 안에서 <see cref="SelectCanceled"/>를 보내며,
        /// 그 신호는 아직 연결된 상태로 받아 Router가 Drag·경로 같은 표현을 정리하게 합니다(명령은 기록하지 않음, R2).
        /// 슬롯 해제가 실패해도 연결 상태는 내립니다.
        /// </summary>
        internal void Unbind()
        {
            if (!IsBound)
                return;

            try
            {
                _slot.Unbind(this);
            }
            finally
            {
                IsBound = false;
            }
        }

        /// <summary>창 포커스를 잃었습니다. Drag·조준·경로 표현만 정리합니다.</summary>
        internal void FocusLost() => Receive(CombatInputRouter.Request.FocusLost, 0f, 0f);

        public void PointerMoved(float screenX, float screenY) => Receive(CombatInputRouter.Request.PointerMoved, screenX, screenY);

        public void SelectPressed(float screenX, float screenY) => Receive(CombatInputRouter.Request.SelectPressed, screenX, screenY);

        public void SelectReleased(float screenX, float screenY) => Receive(CombatInputRouter.Request.SelectReleased, screenX, screenY);

        public void SelectCanceled() => Receive(CombatInputRouter.Request.SelectCanceled, 0f, 0f);

        public void Cancel() => Receive(CombatInputRouter.Request.Cancel, 0f, 0f);

        public void EnterCombatRequested() => Receive(CombatInputRouter.Request.EnterCombat, 0f, 0f);

        public void DrawRequested() => Receive(CombatInputRouter.Request.Draw, 0f, 0f);

        public void EndTurnRequested() => Receive(CombatInputRouter.Request.EndTurn, 0f, 0f);

        public void MoveModeRequested() => Receive(CombatInputRouter.Request.MoveMode, 0f, 0f);

        public void AttackModeRequested() => Receive(CombatInputRouter.Request.AttackMode, 0f, 0f);

        public void RequestReturn() => Receive(CombatInputRouter.Request.Return, 0f, 0f);

        /// <summary>
        /// 모든 외부 입력의 입구입니다. 정지 뒤나 연결 해제 뒤 늦게 도착한 입력은 무시합니다.
        /// 허용 여부는 각 입력 소유자가 기록 직전에 Policy·Gate로 판단하며 여기서 같은 조건을 다시 검사하지 않습니다(§5.6·§7.1).
        /// </summary>
        private void Receive(CombatInputRouter.Request request, float screenX, float screenY)
        {
            if (_faults.IsFaulted || !IsBound || !_canReceive())
                return;
            if (_isConfirmOpen() && request != CombatInputRouter.Request.FocusLost
                && request != CombatInputRouter.Request.SelectCanceled)
                return;

            try
            {
                _route(request, screenX, screenY);
            }
            catch (Exception exception)
            {
                _faults.Report(exception);
            }
        }
    }
}
