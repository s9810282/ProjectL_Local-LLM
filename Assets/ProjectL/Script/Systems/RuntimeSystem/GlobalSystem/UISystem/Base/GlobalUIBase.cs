namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    /// <summary>
    /// OpenGlobalUI로 함께 열리는 유지 UI의 기반 클래스.
    /// 유지 UI는 루트로 등록되며 뒤로가기 이력에 포함되지 않는다.
    /// </summary>
    public abstract class GlobalUIBase : UIBase
    {
        /// <summary>전체 GlobalUI 열기에 참여할지입니다. 요청형 패널은 개별 요청으로만 엽니다.</summary>
        public virtual bool OpenWithGlobalUI => true;

        public sealed override UIId ParentID => UIId.None;
    }
}
