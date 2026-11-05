using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks
{
    public sealed class RockfallGimmickSpec : StageGimmickSpec
    {
        public int CooldownControllerTurns { get; }
        public int WarningControllerTurns { get; }
        public int MinimumRockCount { get; }
        public int MaximumRockCount { get; }
        public int MaximumHealthLossPercent { get; }
        public int BindAffectedSideTurns { get; }
        public IReadOnlyList<CellCoord> FootprintOffsets { get; }

        public RockfallGimmickSpec(int cooldownControllerTurns = 2, int warningControllerTurns = 2,
            int minimumRockCount = 3, int maximumRockCount = 5, int maximumHealthLossPercent = 20,
            int bindAffectedSideTurns = 1, IReadOnlyList<CellCoord> footprintOffsets = null) : base(StageGimmickKey.Rockfall)
        {
            if (cooldownControllerTurns < 1) throw new ArgumentOutOfRangeException(nameof(cooldownControllerTurns));
            if (warningControllerTurns < 1) throw new ArgumentOutOfRangeException(nameof(warningControllerTurns));
            if (minimumRockCount < 1 || maximumRockCount < minimumRockCount) throw new ArgumentOutOfRangeException(nameof(minimumRockCount));
            if (maximumHealthLossPercent < 0 || maximumHealthLossPercent > 100) throw new ArgumentOutOfRangeException(nameof(maximumHealthLossPercent));
            if (bindAffectedSideTurns < 1 || bindAffectedSideTurns == int.MaxValue) throw new ArgumentOutOfRangeException(nameof(bindAffectedSideTurns));
            CooldownControllerTurns = cooldownControllerTurns;
            WarningControllerTurns = warningControllerTurns;
            MinimumRockCount = minimumRockCount;
            MaximumRockCount = maximumRockCount;
            MaximumHealthLossPercent = maximumHealthLossPercent;
            BindAffectedSideTurns = bindAffectedSideTurns;
            FootprintOffsets = StageGimmickMask.CopyNormalized(footprintOffsets ?? new[] { CellCoord.Zero });
        }
    }

}
