namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    /// Hover가 필요한 UI의 진입·이탈 계약입니다.
    /// 입력 연결 코드가 호출하며, 실제 동작은 구현 스크립트가 담당합니다.
    /// 이 인터페이스 자체는 포인터 감지나 입력 등록을 수행하지 않습니다.
    public interface UIHoverAction
    {
        // Hover 대상에 진입했을 때 호출합니다.
        void OnHoverEnter();

        // Hover 대상에서 벗어나거나 Hover가 해제될 때 호출합니다.
        void OnHoverExit();
    }
}
