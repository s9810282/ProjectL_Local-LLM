using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>쓸기 그룹의 표시 근거입니다. 실제 이동과 지형·피해는 이어지는 사건이 나타냅니다.</summary>
    public sealed class CombatSweepEvent : CombatEvent
    {
        public AttackSweepSnapshot Sweep { get; }
        /// <summary>이 결과의 공격 및 후속 피해 출처와 대응하는 공격 그룹입니다.</summary>
        public long AttackGroup { get; }

        public CombatSweepEvent(AttackSweepSnapshot sweep, long attackGroup) : base(CombatEventKind.Swept)
        {
            Sweep = sweep ?? throw new ArgumentNullException(nameof(sweep));
            if (attackGroup <= 0) throw new ArgumentOutOfRangeException(nameof(attackGroup));
            AttackGroup = attackGroup;
        }
    }
}
