using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 진영 경계 사건을 표시합니다. 유닛 재장전은 실제 공격 연출 완료가 담당합니다.
    /// </summary>
    /// <remarks>옛 Ballista 재장전 대기(턴 끝을 간격 뒤에 보이고 재장전 끝에 완료)는 코드 모션 길과 함께 걷었습니다(View 3라운드 V3-e).</remarks>
    internal sealed class CombatTurnEventHandler : CombatEventHandler
    {
        // 진영 경계는 특정 유닛 재장전과 연결하지 않는다.
        internal override bool ShowsBeforeGap(CombatEvent e) =>
            e.Kind == CombatEventKind.PlayerTurnEnded || e.Kind == CombatEventKind.EnemyPhaseEnded;
    }
}
