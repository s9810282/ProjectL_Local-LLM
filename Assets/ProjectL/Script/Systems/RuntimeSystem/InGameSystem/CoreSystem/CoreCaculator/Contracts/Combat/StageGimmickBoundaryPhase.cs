namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>한 번 수락한 진영 종료 경계에서 기믹이 실행되는 단계입니다.</summary>
    public enum StageGimmickBoundaryPhase
    {
        Expiry,
        Scheduled,
        EndOccupancy,
        AfterCleanup,
        Warning
    }
}
