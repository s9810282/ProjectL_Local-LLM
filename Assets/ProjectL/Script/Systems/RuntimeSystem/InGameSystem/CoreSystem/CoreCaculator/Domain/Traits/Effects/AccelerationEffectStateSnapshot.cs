using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class AccelerationEffectStateSnapshot : IEffectStateSnapshot
    {
        public int HeldAttackBonus { get; }
        internal AccelerationEffectStateSnapshot(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            HeldAttackBonus = value;
        }
        public bool ValueEquals(IEffectStateSnapshot other) => other is AccelerationEffectStateSnapshot state && HeldAttackBonus == state.HeldAttackBonus;
    }
}
