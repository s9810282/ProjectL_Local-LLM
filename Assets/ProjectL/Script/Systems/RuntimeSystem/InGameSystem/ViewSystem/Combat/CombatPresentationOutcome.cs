using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 결과 하나의 연출이 끝난 방식입니다(설계 §9). 완료 또는 실패만 있으며 취소는 통지하지 않습니다.
    /// </summary>
    /// <remarks>
    /// 기본값(default)은 완료가 아니라 잘못된 통지입니다. 아직 통지가 없다는 사실은 nullable의 없음으로 구분합니다.
    /// </remarks>
    internal readonly struct CombatPresentationOutcome
    {
        private enum OutcomeKind
        {
            Invalid = 0,
            Completed = 1,
            Failed = 2
        }

        private readonly OutcomeKind _kind;

        private CombatPresentationOutcome(OutcomeKind kind, Exception error)
        {
            _kind = kind;
            Error = error;
        }

        /// <summary>정상 완료 통지입니다.</summary>
        internal static CombatPresentationOutcome Completed { get; } = new(OutcomeKind.Completed, null);

        internal bool IsCompleted => _kind == OutcomeKind.Completed;

        internal bool IsFailed => _kind == OutcomeKind.Failed;

        /// <summary>완료도 실패도 아닌 값(기본값)이면 false입니다.</summary>
        internal bool IsValid => _kind != OutcomeKind.Invalid;

        /// <summary>실패 원인입니다. 실패가 아니면 null입니다.</summary>
        internal Exception Error { get; }

        /// <summary>실패 통지입니다. 연출이 자기 임시 자원을 정리한 뒤 원인과 함께 보냅니다.</summary>
        internal static CombatPresentationOutcome Failure(Exception error)
        {
            return new CombatPresentationOutcome(
                OutcomeKind.Failed,
                error ?? throw new ArgumentNullException(nameof(error)));
        }
    }
}
