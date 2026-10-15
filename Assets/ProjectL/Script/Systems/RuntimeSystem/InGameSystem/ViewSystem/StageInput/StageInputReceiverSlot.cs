using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput
{
    #region 서비스

    /// <summary>스테이지 내부 입력을 받을 View가 붙었다 떨어지는 자리입니다.</summary>
    public interface IStageInputReceiverSlot : IServiceModule
    {
        /// <summary>입력을 받을 View를 연결합니다. 이미 연결되어 있으면 같은 대상이어도 거부합니다.</summary>
        void Bind(IStageInputReceiver receiver);

        /// <summary>
        /// 연결을 해제합니다. 진행 중인 선택이 있었다면 해제한 대상에게 SelectCanceled를 한 번 보냅니다.
        /// 현재 연결된 대상이 아니면 무시합니다. View는 파괴하기 전에 호출합니다.
        /// </summary>
        void Unbind(IStageInputReceiver receiver);
    }

    #endregion

    #region 시스템 모듈

    /// <summary>
    /// Controller가 보는 수신 대상입니다. 연결된 View에 입력을 넘기고,
    /// SelectPressed를 받은 View만 그 선택의 완료·취소를 받도록 선택 소유자를 기억합니다.
    /// 연결된 View가 없으면 입력을 버립니다.
    /// </summary>
    internal sealed class StageInputReceiverSlot :
        IStageInputReceiverSlot,
        IStageInputReceiver,
        IExportSystemServices
    {
        private IStageInputReceiver _receiver;
        private IStageInputReceiver _selectOwner;

        public void Bind(IStageInputReceiver receiver)
        {
            if (receiver == null)
                throw new ArgumentNullException(nameof(receiver));
            if (_receiver != null)
                throw new InvalidOperationException("입력 수신 대상이 이미 연결되어 있습니다. 먼저 Unbind하세요.");

            _receiver = receiver;
        }

        public void Unbind(IStageInputReceiver receiver)
        {
            if (receiver == null)
                throw new ArgumentNullException(nameof(receiver));
            if (!ReferenceEquals(_receiver, receiver))
                return;

            bool hadSelection = ReferenceEquals(_selectOwner, receiver);

            // 콜백 안에서 새로 연결한 대상의 상태를 덮어쓰지 않도록 상태를 먼저 비웁니다.
            _receiver = null;
            _selectOwner = null;

            if (hadSelection)
                receiver.SelectCanceled();
        }

        public void PointerMoved(float screenX, float screenY) => _receiver?.PointerMoved(screenX, screenY);

        public void SelectPressed(float screenX, float screenY)
        {
            IStageInputReceiver receiver = _receiver;
            if (receiver == null)
                return;

            // 누름 콜백 안에서 Unbind해도 취소할 수 있도록 소유자를 먼저 기록합니다.
            _selectOwner = receiver;
            receiver.SelectPressed(screenX, screenY);
        }

        public void SelectReleased(float screenX, float screenY)
        {
            IStageInputReceiver owner = TakeSelectOwner();
            owner?.SelectReleased(screenX, screenY);
        }

        public void SelectCanceled()
        {
            IStageInputReceiver owner = TakeSelectOwner();
            owner?.SelectCanceled();
        }

        public void Cancel() => _receiver?.Cancel();

        public void EnterCombatRequested() => _receiver?.EnterCombatRequested();

        public void DrawRequested() => _receiver?.DrawRequested();

        public void EndTurnRequested() => _receiver?.EndTurnRequested();

        public void MoveModeRequested() => _receiver?.MoveModeRequested();

        public void AttackModeRequested() => _receiver?.AttackModeRequested();

        public void RequestReturn() => _receiver?.RequestReturn();

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddService<IStageInputReceiverSlot>(this);
        }

        /// <summary>선택의 완료·취소를 받을 소유자를 꺼내고 지웁니다. 콜백 전에 지워 중복 전달을 막습니다.</summary>
        private IStageInputReceiver TakeSelectOwner()
        {
            IStageInputReceiver owner = _selectOwner;
            _selectOwner = null;
            return owner;
        }
    }

    #endregion
}
