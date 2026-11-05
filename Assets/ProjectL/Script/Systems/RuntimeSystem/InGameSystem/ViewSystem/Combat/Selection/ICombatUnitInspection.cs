namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection
{
    /// <summary>선택 읽기와 화면 좌표의 명시적인 검사 입력을 제공합니다.</summary>
    internal interface ICombatUnitInspection
    {
        ICombatSelectionReader Selection { get; }
        void InspectUnit(float screenX, float screenY);
    }
}
