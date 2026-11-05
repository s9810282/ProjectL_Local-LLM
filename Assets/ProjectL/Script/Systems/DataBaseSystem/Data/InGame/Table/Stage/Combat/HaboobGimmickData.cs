using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    public sealed class HaboobGimmickData
    {
        public int RelocationIntervalControllerTurns { get; }
        public int WarningLeadControllerTurns { get; }
        public IReadOnlyList<CellCoord> FootprintOffsets { get; }
        public CellCoord? InitialAnchor { get; }

        public HaboobGimmickData(int relocationIntervalControllerTurns, int warningLeadControllerTurns,
            IReadOnlyList<CellCoord> footprintOffsets, CellCoord? initialAnchor = null)
        {
            var validated = new HaboobGimmickSpec(relocationIntervalControllerTurns, warningLeadControllerTurns, footprintOffsets, initialAnchor);
            RelocationIntervalControllerTurns = validated.RelocationIntervalControllerTurns;
            WarningLeadControllerTurns = validated.WarningLeadControllerTurns;
            FootprintOffsets = validated.FootprintOffsets;
            InitialAnchor = validated.InitialAnchor;
        }
    }
}
