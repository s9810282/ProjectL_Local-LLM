using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>Core가 해당 사건 시점의 사실에서 계산한 표시 능력치입니다. 피해 전용 배율과 수신자별 방어는 포함하지 않습니다.</summary>
    public sealed class CombatEffectiveStatsSnapshot
    {
        public int AttackPower { get; }
        public int PhysicalDefense { get; }
        public CombatEffectiveStatsSnapshot(int attackPower, int physicalDefense)
        {
            if (attackPower < 0) throw new ArgumentOutOfRangeException(nameof(attackPower));
            if (physicalDefense < 0) throw new ArgumentOutOfRangeException(nameof(physicalDefense));
            AttackPower = attackPower;
            PhysicalDefense = physicalDefense;
        }
        public bool SameValues(CombatEffectiveStatsSnapshot other) => other != null &&
            AttackPower == other.AttackPower && PhysicalDefense == other.PhysicalDefense;
    }
}
