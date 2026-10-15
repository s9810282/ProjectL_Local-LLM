using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 필드 강조 상태(<see cref="FieldHighlightState"/>)를 화면에 그리는 방법 하나입니다(격자 아웃라인 설계 F4).
    /// 표시 모드마다 구현이 하나씩 있고, 자기가 만든 자원(셀 풀·핸들·데칼)을 직접 반환합니다.
    /// </summary>
    internal interface IFieldHighlightRenderer : IDisposable
    {
        /// <summary>같은 표시기를 이 설정으로 다시 써도 되는지입니다. 한 씬에서 모드·프리팹은 바뀌지 않습니다.</summary>
        bool CanReuse(StageFieldViewData config);

        /// <summary>칸 중심 간격입니다. <see cref="Build"/> 전에 부릅니다.</summary>
        FieldCellSpacing MeasureSpacing();

        /// <summary>새 좌표 변환으로 필드를 구성합니다. 상태는 막 비워진 상태이며, 표시기는 읽기만 합니다.</summary>
        void Build(FieldGridMapper mapper, IFieldHighlightStateReader state);

        /// <summary>상태가 바뀐 칸을 다시 그립니다.</summary>
        void Repaint(IReadOnlyList<int> changedCells);

        /// <summary>칸과 무관한 상태(배치 드래그·경로 지정 여부)가 바뀌었을 때 다시 그립니다.</summary>
        void Refresh();

        /// <summary>
        /// 구성한 표시를 거둡니다. 여러 번 불려도 안전하고, 다시 <see cref="Build"/>할 수 있습니다.
        /// 자원 하나의 반환이 실패해도 나머지 반환과 상태 초기화를 끝까지 한 뒤, 실패를 모아 한 번 던집니다(D5).
        /// </summary>
        void Release();
    }
}
