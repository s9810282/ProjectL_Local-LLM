using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 필드 칸 강조 상태의 읽기 계약입니다(SOLID·SSOT 개선 설계 D4). 표시기와 테스트는 이 계약으로만 상태를 봅니다.
    /// </summary>
    /// <remarks>
    /// 상태를 바꾸는 것은 <see cref="FieldView"/>의 층 API뿐입니다. 구현은 <see cref="FieldHighlightState"/> 하나입니다.
    /// </remarks>
    internal interface IFieldHighlightStateReader
    {
        int ColumnCount { get; }

        int RowCount { get; }

        int CellCount { get; }

        /// <summary>호버 칸 index입니다. 없으면 -1입니다.</summary>
        int HoverIndex { get; }

        CombatHoverState HoverState { get; }

        /// <summary>손패 배치 드래그 중인지입니다.</summary>
        bool IsPlacementActive { get; }

        /// <summary>Q로 이동 경로를 지정하는 중인지입니다(시작 칸 대기 포함).</summary>
        bool IsPathPlanning { get; }

        /// <summary>사용자가 행동 중인지입니다(행동 층에 칸이 있거나, 배치 드래그 중이거나, 경로 지정 중).</summary>
        bool IsActing { get; }

        FieldHighlightLayer LayersAt(int index);

        FieldStaticZone ZoneAt(int index);

        /// <summary>주어진 층 중 하나라도 가진 칸이 있는지입니다.</summary>
        bool HasAnyCell(FieldHighlightLayer mask);

        /// <summary>격자 안이면 칸 index를 돌려줍니다.</summary>
        bool TryGetIndex(CellCoord cell, out int index);
    }
}
