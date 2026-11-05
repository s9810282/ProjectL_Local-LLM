using System;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>요청 하나의 처리 결과입니다(튜토리얼 설계 §7). 요청마다 하나만 게시하며 바뀌지 않습니다.</summary>
    public sealed class TutorialRequestResult
    {
        public int RequestNumber { get; }
        public TutorialCommandKind CommandKind { get; }
        public TutorialDisposition Disposition { get; }

        /// <summary>받아들인 Start면 만든 세션, 후속 명령이면 명령이 지정한 대상입니다. 거절된 Start는 0입니다.</summary>
        public int SessionId { get; }

        public TutorialReason Reason { get; }

        private TutorialRequestResult(int requestNumber, TutorialCommandKind commandKind,
            TutorialDisposition disposition, int sessionId, TutorialReason reason)
        {
            if (requestNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(requestNumber), requestNumber, "0보다 커야 합니다.");
            if (commandKind == TutorialCommandKind.None || !System.Enum.IsDefined(typeof(TutorialCommandKind), commandKind))
                throw new ArgumentOutOfRangeException(nameof(commandKind), commandKind, "알 수 없는 명령 종류입니다.");
            if (sessionId < 0)
                throw new ArgumentOutOfRangeException(nameof(sessionId), sessionId, "0 이상이어야 합니다.");

            RequestNumber = requestNumber;
            CommandKind = commandKind;
            Disposition = disposition;
            SessionId = sessionId;
            Reason = reason;
        }

        /// <summary>받아들인 결과입니다. 대상 세션이 있어야 합니다.</summary>
        internal static TutorialRequestResult Accepted(int requestNumber, TutorialCommandKind commandKind, int sessionId)
        {
            if (sessionId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sessionId), sessionId, "받아들인 결과에는 세션이 있어야 합니다.");

            return new TutorialRequestResult(requestNumber, commandKind, TutorialDisposition.Accepted, sessionId,
                TutorialReason.None);
        }

        /// <summary>정상 거절입니다. 사유는 거절 사유(Unavailable~NotReady)여야 합니다.</summary>
        internal static TutorialRequestResult Rejected(int requestNumber, TutorialCommandKind commandKind, int sessionId,
            TutorialReason reason)
        {
            if (!IsRejectionReason(reason))
                throw new ArgumentOutOfRangeException(nameof(reason), reason, "거절 사유가 아닙니다.");

            return new TutorialRequestResult(requestNumber, commandKind, TutorialDisposition.Rejected, sessionId, reason);
        }

        /// <summary>처리 실패입니다. 사유는 ExecutionFault 또는 RuntimeStopped여야 합니다.</summary>
        internal static TutorialRequestResult Failed(int requestNumber, TutorialCommandKind commandKind, int sessionId,
            TutorialReason reason)
        {
            if (reason != TutorialReason.ExecutionFault && reason != TutorialReason.RuntimeStopped)
                throw new ArgumentOutOfRangeException(nameof(reason), reason, "실패 사유가 아닙니다.");

            return new TutorialRequestResult(requestNumber, commandKind, TutorialDisposition.Failed, sessionId, reason);
        }

        // 사유를 더할 때 거절인지 여기서 명시적으로 고른다. 숫자 범위에 기대지 않는다.
        private static bool IsRejectionReason(TutorialReason reason)
        {
            switch (reason)
            {
                case TutorialReason.Unavailable:
                case TutorialReason.ContextChanged:
                case TutorialReason.UnknownTutorial:
                case TutorialReason.Busy:
                case TutorialReason.UnknownSession:
                case TutorialReason.SessionEnding:
                case TutorialReason.SessionEnded:
                case TutorialReason.StepChanged:
                case TutorialReason.NotReady:
                    return true;
                default:
                    return false;
            }
        }
    }
}
