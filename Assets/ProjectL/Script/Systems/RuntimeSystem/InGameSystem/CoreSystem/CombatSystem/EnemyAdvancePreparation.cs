namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>자동 진행의 준비 상태다. FrameDeferred는 이번 프레임에 더 진행하지 않는다는 뜻이다.</summary>
    public enum AdvancePreparationState { NotRequired, Required, Preparing, Ready, FrameDeferred }

    /// <summary>준비된 행동 한 번의 실행 결과다. Published만 전투 결과를 게시한다.</summary>
    public enum PreparedAdvanceResult { Published, Stale, RetryPreparation, Cancelled }
}
