namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common.ScrollPanel
{
    /// <summary>기존 팝업 수명 소유자가 선택적으로 사용하는 두루마리 연출 참조입니다.</summary>
    public interface IScrollPopupDisplay
    {
        ScrollPanelPresentation ScrollPresentation { get; }
    }
}
