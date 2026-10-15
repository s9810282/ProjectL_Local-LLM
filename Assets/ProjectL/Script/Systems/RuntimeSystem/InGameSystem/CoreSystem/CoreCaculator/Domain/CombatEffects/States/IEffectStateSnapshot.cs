namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States
{
    public interface IEffectStateSnapshot
    {
        bool ValueEquals(IEffectStateSnapshot other);
    }
}