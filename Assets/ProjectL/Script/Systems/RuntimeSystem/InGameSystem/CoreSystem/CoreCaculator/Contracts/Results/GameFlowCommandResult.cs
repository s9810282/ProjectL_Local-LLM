using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public sealed class GameFlowCommandResult
    {
        public bool Succeeded { get; }
        public GameFlowFailureReason FailureReason { get; }
        public GameFlowSnapshot CurrentFlow { get; }

        private GameFlowCommandResult(
            bool succeeded,
            GameFlowFailureReason failureReason,
            GameFlowSnapshot currentFlow)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            CurrentFlow = currentFlow;
        }

        public static GameFlowCommandResult Success(GameFlowSnapshot currentFlow)
        {
            if (currentFlow == null)
                throw new ArgumentNullException(nameof(currentFlow));

            return new GameFlowCommandResult(true, GameFlowFailureReason.None, currentFlow);
        }

        public static GameFlowCommandResult Failure(
            GameFlowFailureReason failureReason,
            GameFlowSnapshot currentFlow)
        {
            if (Enum.IsDefined(typeof(GameFlowFailureReason), failureReason) == false ||
                failureReason == GameFlowFailureReason.None)
            {
                throw new ArgumentOutOfRangeException(nameof(failureReason));
            }

            if (currentFlow == null)
                throw new ArgumentNullException(nameof(currentFlow));

            return new GameFlowCommandResult(false, failureReason, currentFlow);
        }
    }
}
