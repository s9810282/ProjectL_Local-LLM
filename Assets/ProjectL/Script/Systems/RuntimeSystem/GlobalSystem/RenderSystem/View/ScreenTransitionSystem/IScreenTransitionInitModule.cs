namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem
{
    /// <summary>시스템 초기화 때 한 번 읽는 화면 전환 설정 원본입니다.</summary>
    internal interface IScreenTransitionInitModule
    {
        ScreenTransitionInitData Load();
    }
}
