using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>토네이도·하붑·Rider 먼지 연막 호버의 영역 표시입니다. 유닛 범위·공격 조준과 독립된 층만 소유합니다.</summary>
    internal interface ICombatGimmickRangeFeedback
    {
        /// <summary>현재 영역을 교체합니다. 호출자가 재사용하는 목록이므로 호출 뒤 목록을 보관하지 않습니다.</summary>
        void ShowGimmickRange(IReadOnlyList<CellCoord> cells);

        /// <summary>기믹·먼지 연막 호버 영역만 지웁니다. 반복 호출에 안전합니다.</summary>
        void ClearGimmickRange();
    }
}
