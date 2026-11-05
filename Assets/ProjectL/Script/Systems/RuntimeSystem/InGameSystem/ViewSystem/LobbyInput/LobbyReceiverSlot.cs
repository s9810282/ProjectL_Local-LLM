using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput
{
    /// <summary>로비 View가 시설 입력 수명 동안 연결하는 명시적 수신 자리입니다.</summary>
    public interface ILobbyReceiverSlot : IServiceModule
    {
        void Bind(ILobbyInputReceiver receiver);

        void Unbind(ILobbyInputReceiver receiver);
    }

    /// <summary>
    /// 누름을 받은 View만 놓음과 취소를 받도록 소유자를 보존합니다. 연결·소유자 규칙은 공용 <see cref="SelectOwnerSlot{T}"/>가
    /// 맡습니다(로비 리팩토링 2차 설계 M-10).
    /// </summary>
    internal sealed class LobbyReceiverSlot : ILobbyReceiverSlot, ILobbyInputReceiver, IExportSystemServices
    {
        private readonly SelectOwnerSlot<ILobbyInputReceiver> _slot = new(nameof(LobbyReceiverSlot));

        public void Bind(ILobbyInputReceiver receiver) => _slot.Bind(receiver);

        public void Unbind(ILobbyInputReceiver receiver)
        {
            if (_slot.Unbind(receiver)) receiver.OnSelectCanceled();
        }

        public void OnPointer(float x, float y) => _slot.Receiver?.OnPointer(x, y);

        public void OnSelectPressed() => _slot.BeginSelect()?.OnSelectPressed();

        public void OnSelectReleased() => _slot.TakeSelectOwner()?.OnSelectReleased();

        public void OnSelectCanceled() => _slot.TakeSelectOwner()?.OnSelectCanceled();

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            sink.AddService<ILobbyReceiverSlot>(this);
        }
    }
}
