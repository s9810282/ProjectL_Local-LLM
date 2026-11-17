using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    internal readonly struct AttackSweepEffectContext
    {
        public CombatDamageKind DamageKind { get; }

        public AttackSweepEffectContext(CombatDamageKind damageKind)
        {
            if (!Enum.IsDefined(typeof(CombatDamageKind), damageKind)) throw new ArgumentOutOfRangeException(nameof(damageKind));
            DamageKind = damageKind;
        }
    }
}
