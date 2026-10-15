using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class PlayerWalletSnapshot
    {
        public int Balance { get; }

        public PlayerWalletSnapshot(int balance)
        {
            if (balance < 0)
                throw new ArgumentOutOfRangeException(nameof(balance));

            Balance = balance;
        }
    }
}
