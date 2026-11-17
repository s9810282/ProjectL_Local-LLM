using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    public sealed class LavaChainGimmickData
    {
        public CellCoord InitialHeadCell { get; }
        public int CycleControllerTurns { get; }
        public int WarningLeadControllerTurns { get; }
        public int PushDistance { get; }

        public LavaChainGimmickData(CellCoord initialHeadCell, int cycleControllerTurns, int warningLeadControllerTurns, int pushDistance)
        {
            var validated = new LavaChainGimmickSpec(initialHeadCell, cycleControllerTurns, warningLeadControllerTurns, pushDistance);
            InitialHeadCell = validated.InitialHeadCell;
            CycleControllerTurns = validated.CycleControllerTurns;
            WarningLeadControllerTurns = validated.WarningLeadControllerTurns;
            PushDistance = validated.PushDistance;
        }
    }
}
