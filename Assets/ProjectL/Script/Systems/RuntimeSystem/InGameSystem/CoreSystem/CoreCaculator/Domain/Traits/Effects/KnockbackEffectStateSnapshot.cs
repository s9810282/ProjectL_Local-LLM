using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class KnockbackEffectStateSnapshot : IEffectStateSnapshot
    {
        public bool ValueEquals(IEffectStateSnapshot other)
        {
            return other is KnockbackEffectStateSnapshot;
        }
    }
}
