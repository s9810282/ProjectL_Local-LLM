using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class ThornFieldEffectStateSnapshot : IEffectStateSnapshot
    {
        public bool ValueEquals(IEffectStateSnapshot other)
        {
            return other is ThornFieldEffectStateSnapshot;
        }
    }
}
