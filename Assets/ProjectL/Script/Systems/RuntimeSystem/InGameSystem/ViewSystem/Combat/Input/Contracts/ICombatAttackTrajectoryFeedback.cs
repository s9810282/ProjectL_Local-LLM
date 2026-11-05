using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 공격 조준이 호버 응답·범위 증거로 궤적 표시 의도만 전하는 좁은 계약입니다(공격 궤적 설계 TJ2·TJ5, 결정 Q-T6 A).
    /// 공격 조준만 받고, 배치·아이템 입력은 이 계약을 모릅니다. 구현: 씬 표시(<see cref="CombatSceneDisplay"/>), 시험 가짜.
    /// </summary>
    /// <remarks>
    /// 부르는 쪽은 Core 응답·증거를 그대로 넘기고 좌표·모양·끊는 위치를 계산하지 않습니다(<c>CombatAttackTrajectory</c>가 정함).
    /// 구현은 멱등이어야 하고, 해제 뒤 늦게 온 호출은 무시합니다.
    /// </remarks>
    internal interface ICombatAttackTrajectoryFeedback
    {
        /// <summary>공격 가능 응답의 궤적입니다. 끝점은 실제 공격과 같은 해석입니다.</summary>
        void ShowAttackTrajectory(EntityId attackerId, in AttackPreview preview);

        /// <summary>막힌 칸의 궤적입니다. Core 막힘 증거 중 Direct가 있으면 그 벽에서 끊고, 없으면 지웁니다.</summary>
        void ShowBlockedAttackTrajectory(EntityId attackerId, CellCoord target, IReadOnlyList<AttackBlockedContact> contacts);

        void ClearAttackTrajectory();
    }
}
