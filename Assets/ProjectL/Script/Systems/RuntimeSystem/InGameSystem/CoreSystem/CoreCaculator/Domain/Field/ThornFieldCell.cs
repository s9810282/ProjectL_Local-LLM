using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Field
{
    internal readonly struct ThornFieldCell
    {
        public CellCoord Cell { get; }
        public int RemainingUnitTurns { get; }
        public int ThornDamage { get; }

        internal ThornFieldCell(
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
