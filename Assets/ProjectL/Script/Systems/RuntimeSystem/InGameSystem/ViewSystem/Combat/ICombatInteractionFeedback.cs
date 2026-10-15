using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 입력 중 표시 의도(Drag 고스트, 원본 숨김, 호버 칸, 미리보기 응답의 범위·영향 칸)만 전하는 계약입니다
    /// (설계 §5.4·§7·§21.1, E.2·E2).
    /// 부르는 쪽은 원하는 표시 상태만 말하고, View를 찾는 일은 Presenter가 합니다(설계 §3).
    /// </summary>
    /// <remarks>구현: 씬 표시(<see cref="CombatSceneDisplay"/>), 테스트 가짜. 모두 멱등적이어야 합니다.</remarks>
    internal interface ICombatInteractionFeedback
    {
        /// <summary>해당 종류의 Drag 고스트를 띄웁니다. 위치는 이어지는 Move 호출이 정합니다.</summary>
        void ShowGhost(EntityKey entityKey);

        void MoveGhostToCell(CellCoord cell);

        /// <summary>칸 밖에서도 고스트가 포인터를 따라가도록 필드 평면 위로 옮깁니다.</summary>
        void MoveGhostToPointer(float screenX, float screenY);

        /// <summary>고스트의 배치 가능·불가 색입니다. Core 배치 응답으로 정합니다.</summary>
        void SetGhostPlaceable(bool placeable);

        void HideGhost();

        /// <summary>Drag 중인 원본 View를 숨깁니다(필드·손패 중 그 개체를 가진 쪽).</summary>
        void HideOriginal(EntityId entityId);

        void ShowOriginal();

        /// <summary>포인터가 올라간 칸과 그 칸의 가능 여부 표시입니다.</summary>
        void SetHoverCell(CellCoord cell, CombatHoverState state);

        void ClearHoverCell();

        /// <summary>이동·공격 범위 응답의 칸을 표시합니다. 이전 범위 표시는 바뀝니다.</summary>
        void ShowRangeCells(IReadOnlyList<CellCoord> cells);

        void ClearRangeCells();

        /// <summary>호버한 공격 조준의 영향 칸을 표시합니다.</summary>
        void ShowAffectedCells(IReadOnlyList<CellCoord> cells);

        void ClearAffectedCells();
        /// <summary>Core가 제공한 차단 벽 증거를 표시한다. View에서 벽 형상을 계산하지 않는다.</summary>
        void ShowAttackBlockers(IReadOnlyList<AttackBlockedContact> contacts);
        void ClearAttackBlockers();
        void ShowAttackCostPreview(EntityId actorId, int cost);
    }
}
