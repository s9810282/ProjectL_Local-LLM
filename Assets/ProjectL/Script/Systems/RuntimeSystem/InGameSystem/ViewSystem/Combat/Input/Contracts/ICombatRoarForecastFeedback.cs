using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 포효 예고의 표시입니다(포효 예고 설계 RF4·RF5, 계획 TP-16·TP-19). 바닥 범위 칸과, 범위 안에 밀릴 대상이 있으면 그 넉백 미리보기를 함께 받습니다.
    /// 포효 예고 조회(<c>CombatRoarForecastTracker</c>)만 부릅니다. 구현: 공격 피드백(<c>CombatAttackFeedback</c>, 넉백 미리보기의 주인).
    /// </summary>
    /// <remarks>
    /// 넉백은 공격 조준의 넉백 미리보기와 같은 화살표·도착 칸으로 그리고, 공격 조준이 넉백을 그리는 동안에는 그쪽이 먼저입니다(Q-RF4 A).
    /// 언제 보일지·어느 출처를 그릴지는 구현이 정하며, 부르는 쪽은 Core 응답을 그대로 넘깁니다. 해제 뒤 호출은 무시합니다.
    /// </remarks>
    internal interface ICombatRoarForecastFeedback
    {
        /// <summary>
        /// 포효 예고를 교체합니다. <paramref name="cells"/>는 범위 칸(보스가 여럿이면 합친 칸), <paramref name="attacks"/>는 대상이 있는
        /// 예고의 공격 미리보기입니다. 호출자가 재사용하는 목록이므로 구현은 필요하면 복사합니다.
        /// </summary>
        void ShowRoarForecast(IReadOnlyList<CellCoord> cells, IReadOnlyList<AttackPreview> attacks);

        /// <summary>포효 예고의 범위와 넉백을 지웁니다. 반복 호출에 안전합니다.</summary>
        void ClearRoarForecast();
    }
}
