using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 호버 범위의 표시 의도입니다(호버 공격 범위 설계 UR5·Q-UR3 B·§8 HZ5, 호버 이동 범위 설계 EM2). 호버 입력 기능만 씁니다.
    /// 유닛의 이동 칸·공격 범위(아군·적)와 환경 위험의 피해 칸을 색조별로 그립니다. 무엇이 어느 색조인지는 호출자가 정하고, 층·색은 표시가 정합니다.
    /// </summary>
    internal interface ICombatUnitRangeFeedback
    {
        /// <summary>
        /// 호버 범위 칸을 <paramref name="tone"/>의 층·색으로 그립니다. 이동 칸은 아래, 공격 칸은 그 위입니다. 앞서 다른 색조로 그렸으면 그 층은 지웁니다.
        /// 환경 위험 색조는 이동 칸이 없어야 합니다(빈 목록).
        /// 유닛 범위는 source에 원점 유닛을 넘깁니다. 환경 위험은 빈 ID이며 원점 칸을 표시하지 않습니다.
        /// </summary>
        void ShowHoverRange(IReadOnlyList<CellCoord> attackCells, IReadOnlyList<CellCoord> moveCells, CombatRangeTone tone, EntityId source = default);

        /// <summary>호버 범위로 그린 칸만 지웁니다.</summary>
        void ClearHoverRange();
    }
}
