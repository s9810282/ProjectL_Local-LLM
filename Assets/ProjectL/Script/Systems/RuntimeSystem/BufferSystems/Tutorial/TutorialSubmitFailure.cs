namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>제출 자체가 보관되지 않은 이유입니다(튜토리얼 설계 §4). 의미상 거절은 결과로 게시합니다.</summary>
    public enum TutorialSubmitFailure
    {
        /// <summary>보관했습니다.</summary>
        None = 0,
        /// <summary>튜토리얼이 아직 초기화되지 않았습니다.</summary>
        NotInitialized = 1,
        /// <summary>런타임이 멈췄습니다.</summary>
        RuntimeStopped = 2
    }
}
