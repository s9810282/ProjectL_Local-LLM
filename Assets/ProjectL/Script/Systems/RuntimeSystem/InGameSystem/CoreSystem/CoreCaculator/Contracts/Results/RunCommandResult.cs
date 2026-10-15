using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct RunCommandResult
    {
        public bool Succeeded { get; }
        public RunFailureReason FailureReason { get; }
        public bool AlreadyApplied { get; }
        /// <summary>Disposed/활성 Stage 조회 실패 시 null. 과거 HP를 대신 반환하지 않는다.</summary>
        public RunProgressSnapshot CurrentRun { get; }

        private RunCommandResult(bool succeeded, RunFailureReason reason, bool alreadyApplied,
            RunProgressSnapshot currentRun)
        {
            Succeeded = succeeded;
            FailureReason = reason;
            AlreadyApplied = alreadyApplied;
            CurrentRun = currentRun;
        }

        public static RunCommandResult Success(RunProgressSnapshot currentRun, bool alreadyApplied = false) =>
            new RunCommandResult(true, RunFailureReason.None, alreadyApplied,
                currentRun ?? throw new ArgumentNullException(nameof(currentRun)));

        public static RunCommandResult Failure(RunFailureReason reason, RunProgressSnapshot currentRun = null)
        {
            if (reason == RunFailureReason.None || !Enum.IsDefined(typeof(RunFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new RunCommandResult(false, reason, false, currentRun);
        }
    }
}
