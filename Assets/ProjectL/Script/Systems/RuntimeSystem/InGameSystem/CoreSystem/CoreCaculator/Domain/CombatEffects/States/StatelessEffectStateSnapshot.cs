namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States
{
    /// <summary>
    /// 상태 없음이라는 의미를 나타내는 sentinel 객체
    /// </summary>
    internal sealed class StatelessEffectStateSnapshot : IEffectStateSnapshot
    {
        public static StatelessEffectStateSnapshot Instance { get; } = new StatelessEffectStateSnapshot();

        public bool ValueEquals(IEffectStateSnapshot other)
        {
            return other is StatelessEffectStateSnapshot;
        }
    }
}