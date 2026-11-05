using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem
{
    /// <summary>
    /// Scop이 시스템에 Tick 참여와 중지를 알리는 내부 계약입니다.
    /// 이 계약을 통해 활성화해도 Scop을 Framework 모듈로 등록하지 않습니다.
    /// </summary>
    internal interface IScreenTransitionScopService
    {
        void Activate(ISystemTick scop);
        void Deactivate(ISystemTick scop);
    }
}
