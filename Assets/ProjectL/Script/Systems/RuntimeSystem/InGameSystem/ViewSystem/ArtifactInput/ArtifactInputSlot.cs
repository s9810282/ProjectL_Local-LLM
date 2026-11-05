using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput
{
    #region 서비스

    /// <summary>아티팩트 입력을 받을 View가 붙었다 떨어지는 자리입니다.</summary>
    public interface IArtifactInputSlot : IServiceModule
    {
        /// <summary>입력을 받을 View를 연결합니다. 이미 연결되어 있으면 같은 대상이어도 거부합니다.</summary>
        void Bind(IArtifactInputReceiver receiver);

        /// <summary>
        /// 연결을 해제합니다. 진행 중인 선택이 있었다면 해제한 대상에게 SelectCanceled를 한 번 보냅니다.
        /// 현재 연결된 대상이 아니면 무시합니다. View는 파괴하기 전에 호출합니다.
        /// </summary>
        void Unbind(IArtifactInputReceiver receiver);
    }

    #endregion

    #region 시스템 모듈

    /// <summary>
    /// Controller가 보는 수신 대상입니다. 연결된 View에 입력을 넘기고,
    /// SelectPressed를 받은 View만 그 선택의 완료·취소를 받도록 선택 소유자를 기억합니다.
    /// 연결된 View가 없으면 입력을 버립니다. 연결·소유자 규칙은 공용 <see cref="SelectOwnerSlot{T}"/>가 맡습니다(로비 리팩토링 2차 설계 M-10).
    /// </summary>
    internal sealed class ArtifactInputSlot :
        IArtifactInputSlot,
        IArtifactInputReceiver,
        IExportSystemServices
    {
        private readonly SelectOwnerSlot<IArtifactInputReceiver> _slot = new(nameof(ArtifactInputSlot));

        public void Bind(IArtifactInputReceiver receiver) => _slot.Bind(receiver);

        public void Unbind(IArtifactInputReceiver receiver)
        {
            if (_slot.Unbind(receiver)) receiver.SelectCanceled();
        }

        public void PointerMoved(float screenX, float screenY) => _slot.Receiver?.PointerMoved(screenX, screenY);

        public void SelectPressed(float screenX, float screenY) => _slot.BeginSelect()?.SelectPressed(screenX, screenY);

        public void SelectReleased(float screenX, float screenY) => _slot.TakeSelectOwner()?.SelectReleased(screenX, screenY);

        public void SelectCanceled() => _slot.TakeSelectOwner()?.SelectCanceled();

        public void RotateLeft() => _slot.Receiver?.RotateLeft();

        public void RotateRight() => _slot.Receiver?.RotateRight();

        public void Cancel() => _slot.Receiver?.Cancel();

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddService<IArtifactInputSlot>(this);
        }
    }

    #endregion
}
