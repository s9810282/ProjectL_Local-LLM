using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players
{
    /// <summary>
    /// 출처 수명 창구입니다(조각·조립 설계 BP6). 출처 완료 조립의 실행(<see cref="CombatPieceRun"/>)이 시작·전체 종료·취소 때 부릅니다.
    /// 원본은 공격 부품의 Gate(<see cref="CombatHitGate"/>)이고, 공격 재생(<see cref="CombatAttackPlayback"/>)이 구현합니다.
    /// </summary>
    internal interface ICombatSourceLifetime
    {
        /// <summary>
        /// 출처 하나의 수명을 시작합니다. 연출을 시작하기 전에 부릅니다. <paramref name="completed"/>는 정책(<paramref name="completion"/>)에 따른
        /// 사건 완료이며, <paramref name="version"/>이 지금 재생 세대와 같을 때만 전합니다.
        /// </summary>
        void BeginSource(CombatEvent source, EntityId actorId, CombatHitCompletion completion, Action completed, int version);

        /// <summary>출처의 연출과 자원 반환이 정상으로 끝났음을 알립니다. 아직 사건 완료 전이면 지금 알립니다. 세대가 다르면 무시합니다.</summary>
        void EndSource(CombatEvent source, int version);

        /// <summary>시작 실패·취소 때 그 출처만 비웁니다. 완료를 알리지 않습니다.</summary>
        void AbortSource(CombatEvent source);

        /// <summary>
        /// 세대가 같을 때만 이 출처의 대상을 모두 해제합니다(타격 순간, 클립 설계 AC7). 해제 규칙(대상별 피격 사건 열기·첫 해제 완료)은 Gate가 가집니다.
        /// 클립 공격의 해제 조각이 부릅니다. 코드 모션 공격은 같은 입구를 모션의 타격 콜백으로 씁니다.
        /// </summary>
        void ReleaseAll(CombatEvent source, int version);

        /// <summary>
        /// 세대가 같을 때만 이 출처의 대상 하나를 해제합니다(투사체가 그 대상에 닿는 순간, 관통·죽음의 손 View 설계 D6). 그 출처의 히트 대상이 아니거나
        /// 이미 풀린 대상이면 아무 일도 없습니다(규칙은 Gate). 원거리 접촉 해제 규칙(<see cref="CombatContactRelease"/>)이 부릅니다.
        /// </summary>
        void Release(CombatEvent source, EntityId target, int version);
    }
}
