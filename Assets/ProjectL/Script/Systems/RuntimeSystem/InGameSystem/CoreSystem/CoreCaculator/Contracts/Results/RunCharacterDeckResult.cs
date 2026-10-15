using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public sealed class RunCharacterDeckResult
    {
        public bool Succeeded { get; }
        public RunCharacterDeckFailureReason FailureReason { get; }
        public RunCharacterDeckSnapshot CurrentDeck { get; }

        private RunCharacterDeckResult(
            bool succeeded,
            RunCharacterDeckFailureReason failureReason,
            RunCharacterDeckSnapshot currentDeck)
        {
            if (Enum.IsDefined(typeof(RunCharacterDeckFailureReason), failureReason) == false)
                throw new ArgumentOutOfRangeException(nameof(failureReason));

            if (succeeded)
            {
                if (failureReason != RunCharacterDeckFailureReason.None)
                    throw new ArgumentException("Success cannot have a failure reason.", nameof(failureReason));
                if (currentDeck == null)
                    throw new ArgumentNullException(nameof(currentDeck));
            }
            else
            {
                if (failureReason == RunCharacterDeckFailureReason.None)
                    throw new ArgumentException("Failure requires a failure reason.", nameof(failureReason));
                if (currentDeck == null && failureReason != RunCharacterDeckFailureReason.Disposed)
                    throw new ArgumentNullException(nameof(currentDeck));
            }

            Succeeded = succeeded;
            FailureReason = failureReason;
            CurrentDeck = currentDeck;
        }

        public static RunCharacterDeckResult Success(RunCharacterDeckSnapshot currentDeck) =>
            new RunCharacterDeckResult(true, RunCharacterDeckFailureReason.None, currentDeck);

        public static RunCharacterDeckResult Failure(
            RunCharacterDeckFailureReason failureReason,
            RunCharacterDeckSnapshot currentDeck = null) =>
            new RunCharacterDeckResult(false, failureReason, currentDeck);
    }
}
