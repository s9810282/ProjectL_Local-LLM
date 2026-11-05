using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public readonly struct ThornFieldCellSnapshot
    {
        public CellCoord Cell { get; }
        public int RemainingUnitTurns { get; }
        public int ThornDamage { get; }
        public int DefenseReductionPercent { get; }
        public UnitTurnTickPhase TickPhase { get; }

        public ThornFieldCellSnapshot(
            CellCoord cell,
            int remainingUnitTurns,
            int thornDamage,
            int defenseReductionPercent = 0, UnitTurnTickPhase tickPhase = UnitTurnTickPhase.Ended)
        {
            if (remainingUnitTurns < 1)
                throw new ArgumentOutOfRangeException(nameof(remainingUnitTurns));
            if (thornDamage < 1)
                throw new ArgumentOutOfRangeException(nameof(thornDamage));

            Cell = cell;
            RemainingUnitTurns = remainingUnitTurns;
            if (defenseReductionPercent < 0 || defenseReductionPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(defenseReductionPercent));
            if (!Enum.IsDefined(typeof(UnitTurnTickPhase), tickPhase))
                throw new ArgumentOutOfRangeException(nameof(tickPhase));
            DefenseReductionPercent = defenseReductionPercent;
            TickPhase = tickPhase;
            ThornDamage = thornDamage;
        }
    }
}
