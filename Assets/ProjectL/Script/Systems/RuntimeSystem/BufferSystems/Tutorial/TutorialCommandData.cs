using System;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>
    /// 튜토리얼 요청 Buffer에 제출하는 값 명령 하나입니다(튜토리얼 설계 §6).
    /// 종류에 쓰이지 않는 값은 0이며 의미가 없습니다. 생성 함수가 필수 값을 검사하므로
    /// 기본값(<see cref="TutorialCommandKind.None"/>)은 제출할 수 없습니다.
    /// </summary>
    public readonly struct TutorialCommandData
    {
        public TutorialCommandKind Kind { get; }

        /// <summary>Start가 시작할 정의 키입니다.</summary>
        public int TutorialKey { get; }

        /// <summary>Start가 제출할 때 읽은 화면 연결 번호입니다.</summary>
        public int ExpectedContextId { get; }

        /// <summary>Continue·Cancel·ReportBlockedInput의 대상 세션입니다.</summary>
        public int SessionId { get; }

        /// <summary>Continue·ReportBlockedInput을 만들 때 본 단계 revision입니다.</summary>
        public int ExpectedStepRevision { get; }

        private TutorialCommandData(TutorialCommandKind kind, int tutorialKey, int expectedContextId,
            int sessionId, int expectedStepRevision)
        {
            Kind = kind;
            TutorialKey = tutorialKey;
            ExpectedContextId = expectedContextId;
            SessionId = sessionId;
            ExpectedStepRevision = expectedStepRevision;
        }

        /// <summary>정의 하나의 세션을 시작합니다. 제출할 때 읽은 Context를 함께 넘깁니다.</summary>
        public static TutorialCommandData Start(int tutorialKey, int expectedContextId)
        {
            RequirePositive(tutorialKey, nameof(tutorialKey));
            RequirePositive(expectedContextId, nameof(expectedContextId));
            return new TutorialCommandData(TutorialCommandKind.Start, tutorialKey, expectedContextId, 0, 0);
        }

        /// <summary>사용자가 실제로 본 설명 하나를 확인합니다.</summary>
        public static TutorialCommandData Continue(int sessionId, int expectedStepRevision)
        {
            RequirePositive(sessionId, nameof(sessionId));
            RequirePositive(expectedStepRevision, nameof(expectedStepRevision));
            return new TutorialCommandData(TutorialCommandKind.Continue, 0, 0, sessionId, expectedStepRevision);
        }

        /// <summary>외부 사유로 세션을 중단합니다. 단계 revision과 무관합니다.</summary>
        public static TutorialCommandData Cancel(int sessionId)
        {
            RequirePositive(sessionId, nameof(sessionId));
            return new TutorialCommandData(TutorialCommandKind.Cancel, 0, 0, sessionId, 0);
        }

        /// <summary>입력 제한 때문에 막힌 조작이 있었음을 알립니다. 단계와 진행은 바뀌지 않습니다.</summary>
        public static TutorialCommandData ReportBlockedInput(int sessionId, int expectedStepRevision)
        {
            RequirePositive(sessionId, nameof(sessionId));
            RequirePositive(expectedStepRevision, nameof(expectedStepRevision));
            return new TutorialCommandData(TutorialCommandKind.ReportBlockedInput, 0, 0, sessionId,
                expectedStepRevision);
        }

        private static void RequirePositive(int value, string name)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(name, value, "0보다 커야 합니다.");
        }
    }
}
