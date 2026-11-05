namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>세션이 끝난(또는 끝나려는) 이유입니다(튜토리얼 설계 §8).</summary>
    public enum TutorialEndReason
    {
        /// <summary>아직 끝나지 않았습니다.</summary>
        None = 0,
        /// <summary>마지막 설명까지 마쳤습니다.</summary>
        Finished = 1,
        /// <summary>외부 Cancel로 중단했습니다.</summary>
        ExternalCancelled = 2,
        /// <summary>화면 연결이 해제됐습니다.</summary>
        ContextDetached = 3,
        /// <summary>전투 승패가 확정돼 중단했습니다. 정상 완료가 아닙니다.</summary>
        BattleEnded = 4,
        /// <summary>런타임이 멈췄습니다.</summary>
        RuntimeStopped = 5,
        /// <summary>시작·실행 중 오류입니다.</summary>
        ExecutionFault = 6,
        /// <summary>정리 중 오류입니다.</summary>
        CleanupFault = 7
    }
}
