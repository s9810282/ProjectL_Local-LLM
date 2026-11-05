using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
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

        /// <summary>호버를 띄운 입력의 종류입니다. 호버 색만 가릅니다.</summary>
        CombatHoverKind HoverKind { get; }

        /// <summary>현재 공격 영향 영역을 원점 쪽 생략 없이 닫힌 테두리로 그리는지입니다.</summary>
        bool IsAffectedOutlineClosed { get; }

        /// <summary>손패 배치 드래그 중인지입니다.</summary>
        bool IsPlacementActive { get; }

        /// <summary>Q로 이동 경로를 지정하는 중인지입니다(시작 칸 대기 포함).</summary>
        bool IsPathPlanning { get; }

        /// <summary>사용자가 행동 중인지입니다(행동 층에 칸이 있거나, 배치 드래그 중이거나, 경로 지정 중).</summary>
        bool IsActing { get; }

        /// <summary>받은 순서를 보존한 이동 표시 경로입니다. 칸 비트에서 복원하지 않습니다.</summary>
        IReadOnlyList<CellCoord> MovementPath { get; }

        /// <summary>현재 유닛 호버 범위의 원점이 점유한 칸입니다. 환경 위험/호버 해제 시 비어 있습니다.</summary>
        IReadOnlyList<CellCoord> HoverRangeSourceCells { get; }

        FieldHighlightLayer LayersAt(int index);

        FieldStaticZone ZoneAt(int index);

        /// <summary>주어진 층 중 하나라도 가진 칸이 있는지입니다.</summary>
        bool HasAnyCell(FieldHighlightLayer mask);

        /// <summary>격자 안이면 칸 index를 돌려줍니다.</summary>
        bool TryGetIndex(CellCoord cell, out int index);
    }
}
