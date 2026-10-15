using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public sealed class WalletCommandResult
    {
        public bool Succeeded { get; }
        public WalletCommandFailureReason FailureReason { get; }
        public bool Changed { get; }
        public RunId RunId { get; }
        public PlayerWalletSnapshot Wallet { get; }

        private WalletCommandResult(bool succeeded, WalletCommandFailureReason failureReason, bool changed,
            RunId runId, PlayerWalletSnapshot wallet)
        {
            if (runId.IsEmpty) throw new ArgumentException("RunId cannot be empty.", nameof(runId));
            if (!Enum.IsDefined(typeof(WalletCommandFailureReason), failureReason))
                throw new ArgumentOutOfRangeException(nameof(failureReason));
            if (succeeded)
            {
                if (failureReason != WalletCommandFailureReason.None)
                    throw new ArgumentException("Success cannot have a failure reason.", nameof(failureReason));
                if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            }
            else
            {
                if (failureReason == WalletCommandFailureReason.None)
                    throw new ArgumentException("Failure requires a failure reason.", nameof(failureReason));
                if (wallet == null && failureReason != WalletCommandFailureReason.Disposed)
                    throw new ArgumentNullException(nameof(wallet));
            }

            Succeeded = succeeded;
            FailureReason = failureReason;
            Changed = succeeded && changed;
            RunId = runId;
            Wallet = wallet;
        }

        public static WalletCommandResult Success(RunId runId, PlayerWalletSnapshot wallet, bool changed) =>
            new WalletCommandResult(true, WalletCommandFailureReason.None, changed, runId, wallet);

        public static WalletCommandResult Failure(
            RunId runId, WalletCommandFailureReason reason, PlayerWalletSnapshot wallet = null) =>
            new WalletCommandResult(false, reason, false, runId, wallet);
    }
}
