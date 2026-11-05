using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View
{
    public interface IOptionInputReceiver
    {
        void RequestOpen();
        void RequestClose();
    }

    /// <summary>공용·설정 ESC를 받을 옵션 화면을 연결합니다. 실제 표시는 UI 서비스가 맡습니다.</summary>
    public interface IOptionCloseRequestService : IServiceModule
    {
        void Bind(IOptionInputReceiver receiver);
        void Unbind(IOptionInputReceiver receiver);
    }
}
