using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>선택·완료·거절 결과의 일관성을 보장하는 불변 회복 결과다.</summary>
    public sealed class RecoveryOperationResult
    {
        public RecoveryResultKind Kind { get; }
        public RecoveryFailureReason FailureReason { get; }
        public bool AlreadyApplied { get; }
        public RecoveryStateSnapshot StateAfter { get; }
        public ChapterProgressionResult Progression { get; }
        public bool Succeeded => Kind == RecoveryResultKind.Accepted;

        private RecoveryOperationResult(RecoveryResultKind kind, RecoveryFailureReason failureReason,
            bool alreadyApplied, RecoveryStateSnapshot stateAfter, ChapterProgressionResult progression)
        {
            Kind = kind;
            FailureReason = failureReason;
            AlreadyApplied = alreadyApplied;
            StateAfter = stateAfter;
            Progression = progression;
        }

        /// <summary>열린 진입에 적용된 영수증을 선택 수락 결과로 보관한다.</summary>
        public static RecoveryOperationResult Selected(RecoveryStateSnapshot state, bool alreadyApplied = false)
        {
            RequireOpenState(state);
            if (!state.Receipt.AppliedOptionId.HasValue)
            {
                throw new ArgumentException("선택 수락에는 적용된 회복 영수증이 필요합니다.", nameof(state));
            }
            return new RecoveryOperationResult(RecoveryResultKind.Accepted, RecoveryFailureReason.None,
                alreadyApplied, state, null);
        }

        /// <summary>같은 노드만 완료하는 진행 결과와 완료 스냅샷을 함께 보관한다.</summary>
        public static RecoveryOperationResult Completed(RecoveryStateSnapshot state, ChapterProgressionResult progression)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (progression == null)
            {
                throw new ArgumentNullException(nameof(progression));
            }
            if (!state.IsCompleted)
            {
                throw new ArgumentException("완료 결과에는 완료된 회복 상태가 필요합니다.", nameof(state));
            }
            if (!progression.Accepted || !progression.ClearStage || progression.SelectMap ||
                progression.SelectStage || progression.ClearMap || progression.EndAttempt ||
                progression.ChapterKey != state.Context.ChapterKey ||
                progression.NodeId != state.Context.NodeId || progression.StageKey != state.Context.StageKey)
            {
                throw new ArgumentException("회복 완료는 같은 챕터의 해당 노드만 완료해야 합니다.", nameof(progression));
            }
            return new RecoveryOperationResult(RecoveryResultKind.Accepted, RecoveryFailureReason.None,
                false, state, progression);
        }

        /// <summary>열린 현재 상태에서 확인 가능한 거래 거절 사유만 허용한다.</summary>
        public static RecoveryOperationResult TradeRejected(RecoveryFailureReason reason, RecoveryStateSnapshot state)
        {
            if (reason != RecoveryFailureReason.InsufficientFunds && reason != RecoveryFailureReason.NoEffect &&
                reason != RecoveryFailureReason.AlreadyResolved)
            {
                throw new ArgumentOutOfRangeException(nameof(reason));
            }
            RequireOpenState(state);
            if ((reason == RecoveryFailureReason.AlreadyResolved) != state.Receipt.AppliedOptionId.HasValue)
            {
                throw new ArgumentException("거래 거절 사유와 영수증 적용 여부가 일치해야 합니다.", nameof(state));
            }
            foreach (RecoveryOptionState option in state.Options)
            {
                if (option.UnavailableReason == reason)
                {
                    return new RecoveryOperationResult(RecoveryResultKind.TradeRejected, reason, false, state, null);
                }
            }
            throw new ArgumentException("거절 사유에 해당하는 회복 옵션이 필요합니다.", nameof(state));
        }

        /// <summary>요청 진입의 스냅샷 없이 상태 불일치 사유를 보관한다.</summary>
        public static RecoveryOperationResult StateMismatch(RecoveryFailureReason reason)
        {
            if (reason != RecoveryFailureReason.WrongPhase && reason != RecoveryFailureReason.EntryMismatch &&
                reason != RecoveryFailureReason.StaleEntry && reason != RecoveryFailureReason.UnknownOption)
            {
                throw new ArgumentOutOfRangeException(nameof(reason));
            }
            return new RecoveryOperationResult(RecoveryResultKind.StateMismatch, reason, false, null, null);
        }

        private static void RequireOpenState(RecoveryStateSnapshot state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (state.IsCompleted)
            {
                throw new ArgumentException("선택 또는 거래 거절에는 열린 회복 상태가 필요합니다.", nameof(state));
            }
        }
    }
}
