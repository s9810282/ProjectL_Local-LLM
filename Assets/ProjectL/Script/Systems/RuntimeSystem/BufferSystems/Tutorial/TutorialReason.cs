namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>요청 결과의 안정적인 사유 코드입니다(튜토리얼 설계 §7). 내부 예외 메시지를 대신하지 않습니다.</summary>
    public enum TutorialReason
    {
        /// <summary>받아들인 결과의 사유입니다.</summary>
        None = 0,
        /// <summary>거절: 실행 환경을 쓸 수 없습니다.</summary>
        Unavailable = 1,
        /// <summary>거절: 제출 뒤 화면 연결이 바뀌었습니다.</summary>
        ContextChanged = 2,
        /// <summary>거절: 사용할 수 있는 정의가 아닙니다.</summary>
        UnknownTutorial = 3,
        /// <summary>거절: 이미 활성 세션이 있습니다.</summary>
        Busy = 4,
        /// <summary>거절: 없는 세션입니다.</summary>
        UnknownSession = 5,
        /// <summary>거절: 세션이 종료 정리 중입니다.</summary>
        SessionEnding = 6,
        /// <summary>거절: 세션이 이미 끝났습니다.</summary>
        SessionEnded = 7,
        /// <summary>거절: 제출할 때 본 단계가 이미 바뀌었습니다.</summary>
        StepChanged = 8,
        /// <summary>거절: 같은 단계지만 지금은 받을 수 없습니다.</summary>
        NotReady = 9,
        /// <summary>실패: 실행 중 오류입니다.</summary>
        ExecutionFault = 10,
        /// <summary>실패: 런타임이 멈췄습니다.</summary>
        RuntimeStopped = 11
    }
}
