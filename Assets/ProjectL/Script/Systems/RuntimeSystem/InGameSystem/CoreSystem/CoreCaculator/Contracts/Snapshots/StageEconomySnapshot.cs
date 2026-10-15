using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class StageEconomySnapshot
    {
        public int Gold { get; }
        public int ManualDrawGoldCost { get; }

        public StageEconomySnapshot(int gold, int manualDrawGoldCost)
        {
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            if (manualDrawGoldCost < 0) throw new ArgumentOutOfRangeException(nameof(manualDrawGoldCost));
            Gold = gold;
            ManualDrawGoldCost = manualDrawGoldCost;
        }
    }
}
