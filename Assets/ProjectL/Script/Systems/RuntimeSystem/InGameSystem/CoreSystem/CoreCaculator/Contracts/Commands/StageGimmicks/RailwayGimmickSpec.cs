using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks
{
    public sealed class RailwayGimmickSpec : StageGimmickSpec
    {
        public int FirstActivationAfterControllerTurns { get; }
        public int RepeatIntervalControllerTurns { get; }

        public RailwayGimmickSpec(
            int firstActivationAfterControllerTurns,
            int repeatIntervalControllerTurns)
            : base(StageGimmickKey.Railway)
        {
            if (firstActivationAfterControllerTurns <= 0)
                throw new ArgumentOutOfRangeException(nameof(firstActivationAfterControllerTurns));
            if (repeatIntervalControllerTurns <= 0)
                throw new ArgumentOutOfRangeException(nameof(repeatIntervalControllerTurns));

            FirstActivationAfterControllerTurns = firstActivationAfterControllerTurns;
            RepeatIntervalControllerTurns = repeatIntervalControllerTurns;
        }
    }
}