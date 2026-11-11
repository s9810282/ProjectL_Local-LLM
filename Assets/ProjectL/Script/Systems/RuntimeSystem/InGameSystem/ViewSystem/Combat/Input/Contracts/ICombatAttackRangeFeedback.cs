using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 공격 조준 중 미리보기 응답의 범위·영향 칸·막힌 벽·예상 체력 표시 의도만 전하는 계약입니다(설계 §21.1, E2).
    /// 부르는 쪽은 원하는 표시 상태만 말하고, View를 찾는 일은 구현이 합니다(설계 §3).
    /// </summary>
    /// <remarks>
    /// 옛 <c>ICombatInteractionFeedback</c>(17)을 쓰는 쪽마다 셋으로 나눈 가운데 공격 조준 몫입니다(씬 표시 나누기 설계 DS2, 결정 Q-DS2 A).
    /// 고스트는 <see cref="ICombatPlacementFeedback"/>, 호버 칸은 <see cref="ICombatHoverCellFeedback"/>입니다.
    /// 구현: 공격 피드백(<c>CombatAttackFeedback</c>). 모두 멱등적이어야 하고, 표시 해제 뒤에는 아무것도 하지 않습니다.
    /// </remarks>
    internal interface ICombatAttackRangeFeedback
    {
        /// <summary>공격 범위 응답의 칸을 표시합니다(이동 범위는 <see cref="ICombatMovementRangeFeedback"/>). 이전 범위 표시는 바뀝니다.</summary>
        void ShowRangeCells(IReadOnlyList<CellCoord> cells, IReadOnlyList<CellCoord> basicCells = null);

        void ClearRangeCells();

        /// <summary>호버한 공격 조준의 영향 칸을 표시합니다. 독립된 피해 영역은 closeOutline으로 네 변을 닫습니다.</summary>
        void ShowAffectedCells(IReadOnlyList<CellCoord> cells, AttackPullSnapshot pull = null, bool closeOutline = false);

        void ClearAffectedCells();

        /// <summary>Core가 제공한 차단 벽 증거를 표시한다. View에서 벽 형상을 계산하지 않는다.</summary>
        void ShowAttackBlockers(IReadOnlyList<AttackBlockedContact> contacts);

        void ClearAttackBlockers();

        /// <summary>Core 응답의 대상별 예상 잔여 체력으로 이전 프리뷰를 교체합니다.</summary>
        void ShowAttackHealthPreview(ReadOnlyMemory<AttackTargetPreview> targets);

        /// <summary>예상 체력을 실제 체력 표시로 복원합니다.</summary>
        void ClearAttackHealthPreview();
    }
}
