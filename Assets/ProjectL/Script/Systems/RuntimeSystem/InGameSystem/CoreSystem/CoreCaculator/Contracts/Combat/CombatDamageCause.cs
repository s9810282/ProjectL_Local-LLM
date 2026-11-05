namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>숫자 피해와 방어 단계를 우회하는 직접 처치를 구분합니다.</summary>
    public enum CombatDamageCause
    {
        NumericDamage = 1,
        InstantDefeat = 2
    }
}
