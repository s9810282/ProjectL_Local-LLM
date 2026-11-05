namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    /// <summary>
    /// Hover용 UI Raycast는 유지하면서 필드 포인터 입력을 통과시키는 표시 계약이다.
    /// Raycast가 맞은 객체의 계약만 확인한다. 자식 컨트롤이나 같은 Canvas 전체에는 전파하지 않는다.
    /// 입력 판정·액션맵·드래그 수명은 소비하는 입력 경로가 소유한다.
    /// </summary>
    public interface IUIWorldPointerPassThrough
    {
        bool AllowsWorldPointerThrough { get; }
    }
}
