using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class StageEconomySpec
    {
        public int InitialGold { get; }
        public int ManualDrawGoldCost { get; }

        public StageEconomySpec(int initialGold, int manualDrawGoldCost = 0)
        {
            if (initialGold < 0) throw new ArgumentOutOfRangeException(nameof(initialGold));
            if (manualDrawGoldCost < 0) throw new ArgumentOutOfRangeException(nameof(manualDrawGoldCost));
            InitialGold = initialGold;
            ManualDrawGoldCost = manualDrawGoldCost;
        }
    }
}
