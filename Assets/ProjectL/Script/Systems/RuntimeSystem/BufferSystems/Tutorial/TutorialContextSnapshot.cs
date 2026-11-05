using System;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>
    /// 현재 실행 환경의 불변 조회 결과입니다(튜토리얼 설계 §5).
    /// 다른 세션이 실행 중인지는 <see cref="ActiveSessionId"/>로 구분하며 사용 가능 여부를 바꾸지 않습니다.
    /// </summary>
    public sealed class TutorialContextSnapshot
    {
        /// <summary>지원 화면 연결마다 새로 발급한 번호입니다. 연결이 없으면 0입니다.</summary>
        public int ContextId { get; }

        public TutorialUnavailableReason UnavailableReason { get; }

        /// <summary>Starting·Running·Ending 세션입니다. 없으면 0입니다.</summary>
        public int ActiveSessionId { get; }

        public bool IsAvailable => UnavailableReason == TutorialUnavailableReason.None;

        internal TutorialContextSnapshot(int contextId, TutorialUnavailableReason unavailableReason, int activeSessionId)
        {
            if (contextId < 0)
                throw new ArgumentOutOfRangeException(nameof(contextId), contextId, "0 이상이어야 합니다.");
            if (!System.Enum.IsDefined(typeof(TutorialUnavailableReason), unavailableReason))
                throw new ArgumentOutOfRangeException(nameof(unavailableReason), unavailableReason, "알 수 없는 사유입니다.");
            if (unavailableReason == TutorialUnavailableReason.None && contextId == 0)
                throw new ArgumentException("사용할 수 있는 환경에는 화면 연결 번호가 있어야 합니다.", nameof(contextId));
            if (activeSessionId < 0)
                throw new ArgumentOutOfRangeException(nameof(activeSessionId), activeSessionId, "0 이상이어야 합니다.");

            ContextId = contextId;
            UnavailableReason = unavailableReason;
            ActiveSessionId = activeSessionId;
        }
    }
}
