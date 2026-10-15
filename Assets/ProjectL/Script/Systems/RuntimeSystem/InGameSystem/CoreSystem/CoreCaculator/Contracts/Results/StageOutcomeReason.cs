namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum StageOutcomeReason
    {
        None = 0,
        EnemyHeadquartersDestroyed = 1, // 적 패배 이유
        PlayerHeadquartersDestroyed = 2, // 유저 패배 이유
        NoPlayerUnitsRemaining = 3, // 유저 패배 이유
        PlayerMainCharacterDied = 4,
        VoluntaryAbandonment = 5,
    }
}
