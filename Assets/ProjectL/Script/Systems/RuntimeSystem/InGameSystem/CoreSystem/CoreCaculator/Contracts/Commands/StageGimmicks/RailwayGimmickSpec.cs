using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks
{
    public sealed class RailwayGimmickSpec : StageGimmickSpec
    {
        public int FirstActivationAfterUnitTurns { get; }
        public int RepeatIntervalUnitTurns { get; }

        public RailwayGimmickSpec(
            int firstActivationAfterUnitTurns,
            int repeatIntervalUnitTurns)
            : base(StageGimmickKey.Railway)
        {
            if (firstActivationAfterUnitTurns <= 0)
                throw new ArgumentOutOfRangeException(nameof(firstActivationAfterUnitTurns));
            if (repeatIntervalUnitTurns <= 0)
                throw new ArgumentOutOfRangeException(nameof(repeatIntervalUnitTurns));

            FirstActivationAfterUnitTurns = firstActivationAfterUnitTurns;
            RepeatIntervalUnitTurns = repeatIntervalUnitTurns;
        }
    }
}