namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>기존 View Tick에서 연출을 진행하고 취소하는 계약입니다. 취소는 완료 통지가 아닙니다.</summary>
    public interface ICombatPresentationControl
    {
        void TickPresentation(float deltaTime);
        void CancelPresentation();
    }
}
