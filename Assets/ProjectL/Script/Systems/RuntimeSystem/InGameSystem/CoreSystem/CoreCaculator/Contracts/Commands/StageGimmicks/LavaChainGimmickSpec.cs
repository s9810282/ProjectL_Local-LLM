using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks
{
    public sealed class LavaChainGimmickSpec : StageGimmickSpec
    {
        public CellCoord InitialHeadCell { get; }
        public int CycleControllerTurns { get; }
        public int WarningLeadControllerTurns { get; }
        public int PushDistance { get; }

        public LavaChainGimmickSpec(CellCoord initialHeadCell, int cycleControllerTurns, int warningLeadControllerTurns,
            int pushDistance) : base(StageGimmickKey.LavaChain)
        {
            if (initialHeadCell.X < 0 || initialHeadCell.Y < 0) throw new ArgumentOutOfRangeException(nameof(initialHeadCell));
            if (cycleControllerTurns < 2) throw new ArgumentOutOfRangeException(nameof(cycleControllerTurns));
            if (warningLeadControllerTurns < 1 || warningLeadControllerTurns >= cycleControllerTurns)
                throw new ArgumentOutOfRangeException(nameof(warningLeadControllerTurns));
            if (pushDistance < 1) throw new ArgumentOutOfRangeException(nameof(pushDistance));
            InitialHeadCell = initialHeadCell;
            CycleControllerTurns = cycleControllerTurns;
            WarningLeadControllerTurns = warningLeadControllerTurns;
            PushDistance = pushDistance;
        }
    }
}
