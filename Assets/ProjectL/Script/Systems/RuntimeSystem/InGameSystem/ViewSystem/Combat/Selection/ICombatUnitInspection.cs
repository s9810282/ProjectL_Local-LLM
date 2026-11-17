using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection
{
    /// <summary>선택 읽기와 화면 좌표·유닛 ID의 선택 입력, 결과에 따른 선택 갱신을 제공합니다.</summary>
    internal interface ICombatUnitInspection
    {
        ICombatSelectionReader Selection { get; }
        bool TryPickInspection(float screenX, float screenY, out CellCoord? cell, out EntityId entity);
        void InspectUnit(EntityId entity);

        /// <summary>필드에 표시된 아군을 선택하고 정보창을 갱신합니다.</summary>
        bool TrySelectActor(EntityId actor);

        /// <summary>현재 정보 선택을 해제하고 표시를 갱신합니다.</summary>
        void Deselect();

        /// <summary>결과 연출 뒤 최종 표시를 그리기 전에 선택 상태만 갱신합니다.</summary>
        void ApplyResult(CombatResult result);
    }
}
