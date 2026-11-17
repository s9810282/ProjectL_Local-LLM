using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks
{
    public sealed class MeteorGimmickSpec : StageGimmickSpec
    {
        public int CycleControllerTurns { get; }
        public int WarningLeadControllerTurns { get; }
        public int MinimumMeteorCount { get; }
        public int MaximumMeteorCount { get; }
        public int FootprintSideLength { get; }
        public int MaximumHealthDamagePercent { get; }
        public int FireAffectedSideStarts { get; }
        public int FlameControllerTurns { get; }

        public MeteorGimmickSpec(int cycleControllerTurns, int warningLeadControllerTurns, int minimumMeteorCount,
            int maximumMeteorCount, int footprintSideLength, int maximumHealthDamagePercent,
            int fireAffectedSideStarts, int flameControllerTurns) : base(StageGimmickKey.Meteor)
        {
            if (cycleControllerTurns < 2) throw new ArgumentOutOfRangeException(nameof(cycleControllerTurns));
            if (warningLeadControllerTurns < 1 || warningLeadControllerTurns >= cycleControllerTurns)
                throw new ArgumentOutOfRangeException(nameof(warningLeadControllerTurns));
            if (minimumMeteorCount < 1) throw new ArgumentOutOfRangeException(nameof(minimumMeteorCount));
            if (maximumMeteorCount < minimumMeteorCount) throw new ArgumentOutOfRangeException(nameof(maximumMeteorCount));
            if (footprintSideLength < 1) throw new ArgumentOutOfRangeException(nameof(footprintSideLength));
            if (maximumHealthDamagePercent < 0 || maximumHealthDamagePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(maximumHealthDamagePercent));
            if (fireAffectedSideStarts < 1) throw new ArgumentOutOfRangeException(nameof(fireAffectedSideStarts));
            if (flameControllerTurns < 1) throw new ArgumentOutOfRangeException(nameof(flameControllerTurns));
            CycleControllerTurns = cycleControllerTurns;
            WarningLeadControllerTurns = warningLeadControllerTurns;
            MinimumMeteorCount = minimumMeteorCount;
            MaximumMeteorCount = maximumMeteorCount;
            FootprintSideLength = footprintSideLength;
            MaximumHealthDamagePercent = maximumHealthDamagePercent;
            FireAffectedSideStarts = fireAffectedSideStarts;
            FlameControllerTurns = flameControllerTurns;
        }
    }
}
