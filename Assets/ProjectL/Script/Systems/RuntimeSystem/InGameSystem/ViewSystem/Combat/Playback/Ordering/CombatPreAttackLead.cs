namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>
    /// 공격 하나의 선행 구간 안 사건 하나입니다(클립 칸 다시 짜기 설계 CS11). 그 사건이 속한 공격(<see cref="Attack"/>)과 결과 사건 목록의 위치입니다.
    /// 특성 종류·표시 유무·시작 시각·예약 상태는 보관하지 않습니다.
    /// </summary>
    internal readonly struct CombatPreAttackLead
    {
        internal CombatPreAttackLead(CombatPreAttackSourceKey attack, int eventIndex)
        {
            Attack = attack;
            EventIndex = eventIndex;
        }

        /// <summary>이 사건을 선행 구간으로 가진 공격입니다.</summary>
        internal CombatPreAttackSourceKey Attack { get; }

        /// <summary>이 사건의 결과 사건 목록 위치입니다. 공격 사건보다 앞입니다.</summary>
        internal int EventIndex { get; }
    }
}
