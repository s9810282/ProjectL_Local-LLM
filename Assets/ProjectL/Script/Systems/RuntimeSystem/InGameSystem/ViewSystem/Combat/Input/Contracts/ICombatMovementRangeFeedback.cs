using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.MovePath;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 이동 범위의 표시 의도입니다(경로 입력 분리 설계 RT4). 이동 범위 소유자(<see cref="CombatMovementRangeTracker"/>)만 씁니다.
    /// <see cref="ICombatMovementFeedback"/>이 상속합니다.
    /// </summary>
    internal interface ICombatMovementRangeFeedback
    {
        /// <summary>Core가 반환한 목적지 범위만 표시합니다. 이동 실행 승인은 별도 견적을 따릅니다.</summary>
        void ShowMovementRange(MovementRangeResult range);

        /// <summary>이동 범위로 그린 칸을 지웁니다.</summary>
        void ClearMovementRange();
    }
}
