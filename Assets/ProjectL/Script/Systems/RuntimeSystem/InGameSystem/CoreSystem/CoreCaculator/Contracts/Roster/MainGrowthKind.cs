namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>메인이 레벨업할 때 고르는 랜덤 성장 하나다. 증가량은 경험치 설정(LevelProgressionSpec)이 가진다.</summary>
    public enum MainGrowthKind
    {
        None = 0,
        MaxHealth = 1,
        Attack = 2,
        Defense = 3
    }
}
