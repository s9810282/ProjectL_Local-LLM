namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum CombatEntryFailureReason
    {
        None = 0,
        StageEnded,
        WrongPhase,
        InvalidHeadquarters, // 살아있는 배치 적 HQ수가 정확히 하나가 아닌 상태
        InvalidTimelineConfiguration, // 타임라인 참가자 초기화 플랜을 만들 수 없는 상태
        EnemyRedeploymentBlocked,
        InvalidMainCharacter,
        ArtifactLoadoutInactive,
    }
}
