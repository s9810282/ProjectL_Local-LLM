using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection
{
    /// <summary>선택 읽기와 화면 좌표의 검사 입력, 결과에 따른 선택 갱신을 제공합니다.</summary>
    internal interface ICombatUnitInspection
    {
        ICombatSelectionReader Selection { get; }
        void InspectUnit(float screenX, float screenY);

        /// <summary>현재 정보 선택을 해제하고 표시를 갱신합니다.</summary>
        void Deselect();

        /// <summary>결과 연출 뒤 최종 표시를 그리기 전에 선택 상태만 갱신합니다.</summary>
        void ApplyResult(CombatResult result);
    }
}
