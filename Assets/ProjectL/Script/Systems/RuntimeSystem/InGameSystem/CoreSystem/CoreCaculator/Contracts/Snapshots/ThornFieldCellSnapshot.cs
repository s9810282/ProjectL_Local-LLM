using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public readonly struct ThornFieldCellSnapshot
    {
        public CellCoord Cell { get; }
        public int RemainingUnitTurns { get; }
        public int ThornDamage { get; }

        public ThornFieldCellSnapshot(
            CellCoord cell,
            int remainingUnitTurns,
            int thornDamage)
        {
            if (remainingUnitTurns < 1)
                throw new ArgumentOutOfRangeException(nameof(remainingUnitTurns));
            if (thornDamage < 1)
                throw new ArgumentOutOfRangeException(nameof(thornDamage));

            Cell = cell;
            RemainingUnitTurns = remainingUnitTurns;
            ThornDamage = thornDamage;
        }
    }
}
