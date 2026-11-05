using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class RoarEffectStateSnapshot : IEffectStateSnapshot
    {
        public int HitCount { get; }

        internal RoarEffectStateSnapshot(int hitCount)
        {
            if (hitCount < 0) throw new ArgumentOutOfRangeException(nameof(hitCount));
            HitCount = hitCount;
        }

        public bool ValueEquals(IEffectStateSnapshot other)
        {
            return other is RoarEffectStateSnapshot snapshot && snapshot.HitCount == HitCount;
        }
    }
}
