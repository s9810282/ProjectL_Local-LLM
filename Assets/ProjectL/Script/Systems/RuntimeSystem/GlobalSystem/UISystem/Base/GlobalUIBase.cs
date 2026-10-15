namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    /// <summary>
    /// OpenGlobalUI로 함께 열리는 유지 UI의 기반 클래스.
    /// 유지 UI는 루트로 등록되며 뒤로가기 이력에 포함되지 않는다.
    /// </summary>
    public abstract class GlobalUIBase : UIBase
    {
        public sealed override UIId ParentID => UIId.None;
    }
}
