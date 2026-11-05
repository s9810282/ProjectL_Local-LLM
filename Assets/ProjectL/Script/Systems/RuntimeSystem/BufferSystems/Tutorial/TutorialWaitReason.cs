namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>세션이 지금 기다리는 것입니다(튜토리얼 설계 §8). Job이 Runner를 점유한다는 뜻이 아닙니다.</summary>
    public enum TutorialWaitReason
    {
        /// <summary>기다리는 것이 없습니다.</summary>
        None = 0,
        /// <summary>설명 구간의 첫 설명이 시작 조건을 기다립니다(설계 §8.1).</summary>
        StartCondition = 1,
        /// <summary>표시나 다음 단계로 넘어가는 전환을 기다립니다.</summary>
        Presentation = 2,
        /// <summary>표시된 설명이 사용자 확인이나 5초 만료를 기다립니다.</summary>
        ContinueOrTimeout = 3,
        /// <summary>실습 단계가 통과 조건이 되는 게임 결과를 기다립니다.</summary>
        PracticeGoal = 4,
        /// <summary>종료 정리를 기다립니다.</summary>
        Cleanup = 5
    }
}
