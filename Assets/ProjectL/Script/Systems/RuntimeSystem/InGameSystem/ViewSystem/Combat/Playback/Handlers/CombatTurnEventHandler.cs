using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 진영 경계 사건을 표시합니다. 적 턴 시작 안내는 간격 전에 열고, 유닛 재장전은 실제 공격 연출 완료가 담당합니다.
    /// </summary>
    /// <remarks>옛 Ballista 재장전 대기(턴 끝을 간격 뒤에 보이고 재장전 끝에 완료)는 코드 모션 길과 함께 걷었습니다(View 3라운드 V3-e).</remarks>
    internal sealed class CombatTurnEventHandler : CombatEventHandler
    {
        private readonly Action<CombatTurnEvent> _showEnemyTurnStart;

        internal CombatTurnEventHandler(Action<CombatTurnEvent> showEnemyTurnStart = null) =>
            _showEnemyTurnStart = showEnemyTurnStart;

        // 진영 경계는 특정 유닛 재장전과 연결하지 않는다.
        internal override bool ShowsBeforeGap(CombatEvent e) =>
            e.Kind == CombatEventKind.PlayerTurnEnded || e.Kind == CombatEventKind.EnemyPhaseEnded
            || (_showEnemyTurnStart != null && IsEnemyTurnStart(e));

        internal override void ApplyFacts(CombatEvent e)
        {
            if (IsEnemyTurnStart(e))
                _showEnemyTurnStart?.Invoke((CombatTurnEvent)e);
        }

        private static bool IsEnemyTurnStart(CombatEvent e) =>
            e is CombatTurnEvent turn && turn.Kind == CombatEventKind.ControllerTurnStarted
            && turn.Final.ControllerPhase == CombatControllerPhase.Enemy;
    }
}
