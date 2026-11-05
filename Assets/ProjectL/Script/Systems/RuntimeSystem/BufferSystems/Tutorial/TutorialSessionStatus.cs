namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>세션 한 회차의 상태입니다(튜토리얼 설계 §8).</summary>
    public enum TutorialSessionStatus
    {
        /// <summary>기본값입니다. 게시되는 세션에는 쓰이지 않습니다.</summary>
        None = 0,
        /// <summary>Start 수락부터 첫 단계 진입까지입니다. 활성 세션입니다.</summary>
        Starting = 1,
        /// <summary>단계를 진행 중입니다. 활성 세션입니다.</summary>
        Running = 2,
        /// <summary>종료 의도를 고정하고 정리 중입니다. 활성 세션입니다.</summary>
        Ending = 3,
        /// <summary>마지막 설명까지 마치고 정리했습니다.</summary>
        Completed = 4,
        /// <summary>외부 취소·화면 해제·전투 승패·런타임 정지로 중단했습니다.</summary>
        Cancelled = 5,
        /// <summary>시작·실행·정리 실패로 끝났습니다.</summary>
        Faulted = 6
    }
}
