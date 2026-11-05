namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>실행 환경을 쓸 수 없는 이유입니다(튜토리얼 설계 §5). 다른 세션이 실행 중인 것은 여기 넣지 않습니다.</summary>
    public enum TutorialUnavailableReason
    {
        /// <summary>쓸 수 있습니다.</summary>
        None = 0,
        /// <summary>튜토리얼이 아직 초기화되지 않았습니다.</summary>
        NotInitialized = 1,
        /// <summary>연결된 지원 화면이 없습니다.</summary>
        NoContext = 2,
        /// <summary>연결된 화면이 튜토리얼을 지원하지 않습니다.</summary>
        UnsupportedContext = 3,
        /// <summary>런타임이 멈췄습니다.</summary>
        RuntimeStopped = 4
    }
}
