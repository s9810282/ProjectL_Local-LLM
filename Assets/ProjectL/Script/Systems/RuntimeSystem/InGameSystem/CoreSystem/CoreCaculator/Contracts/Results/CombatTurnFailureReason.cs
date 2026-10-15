namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum CombatTurnFailureReason
    {
        None = 0,
        WrongPhase,
        ActiveUnitAlreadyExists,
        EntityNotFound,
        NotUnit,
        NotInCombat,
        Dead,
        NoActiveUnit,
        NoEligibleUnit, // 타임라인에 다음 행동 후보가 하나도 없음
        StageEnded, // 스테이지 종료
    }
}
