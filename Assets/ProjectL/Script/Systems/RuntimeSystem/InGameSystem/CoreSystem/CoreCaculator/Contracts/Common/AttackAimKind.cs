namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    public enum AttackAimKind
    {
        None = 0,
        // 1은 폐기된 개체 조준 값이므로 재사용하지 않는다.
        Cell = 2,
        Direction = 3,
        All = 4,
    }
}
