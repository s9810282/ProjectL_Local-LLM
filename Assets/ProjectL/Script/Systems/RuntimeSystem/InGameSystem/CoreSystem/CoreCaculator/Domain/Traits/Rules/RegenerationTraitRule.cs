using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    /// <summary>재생의 주기와 순수 회복 값을 계산합니다. 발동 시점·대상·실제 회복 정산은 호출자가 담당합니다.</summary>
    internal sealed class RegenerationTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.Regeneration;

        internal static int EndOwnSide(RegenerationTraitStatusSnapshot status, out bool due)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            int completed = checked(status.CompletedEligibleTurnEnds + 1);
            due = completed == status.HealEveryOwnSideTurnEnds;
            return due ? 0 : completed;
        }

        internal static HealthStateSnapshot Heal(HealthStateSnapshot health, int healAmount)
        {
            if (healAmount < 1)
                throw new ArgumentOutOfRangeException(nameof(healAmount));

            if (health.IsDead)
                return health;

            int healedHealth = (int)Math.Min((long)health.CurrentHealth + healAmount, health.MaxHealth);
            return new HealthStateSnapshot(healedHealth, health.MaxHealth);
        }
    }
}
