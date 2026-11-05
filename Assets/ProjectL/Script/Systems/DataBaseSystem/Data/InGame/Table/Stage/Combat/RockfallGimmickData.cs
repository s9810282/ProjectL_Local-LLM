using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    public sealed class RockfallGimmickData
    {
        public int CooldownControllerTurns { get; }
        public int WarningControllerTurns { get; }
        public int MinimumRockCount { get; }
        public int MaximumRockCount { get; }
        public int MaximumHealthLossPercent { get; }
        public int BindAffectedSideTurns { get; }
        public IReadOnlyList<CellCoord> FootprintOffsets { get; }

        public RockfallGimmickData(int cooldownControllerTurns, int warningControllerTurns,
            int minimumRockCount, int maximumRockCount, int maximumHealthLossPercent,
            int bindAffectedSideTurns, IReadOnlyList<CellCoord> footprintOffsets)
        {
            // 준비 명세의 검증을 재사용하며 입력 배열은 불변 사본으로 보관한다.
            var validated = new RockfallGimmickSpec(cooldownControllerTurns, warningControllerTurns,
                minimumRockCount, maximumRockCount, maximumHealthLossPercent, bindAffectedSideTurns, footprintOffsets);
            CooldownControllerTurns = validated.CooldownControllerTurns;
            WarningControllerTurns = validated.WarningControllerTurns;
            MinimumRockCount = validated.MinimumRockCount;
            MaximumRockCount = validated.MaximumRockCount;
            MaximumHealthLossPercent = validated.MaximumHealthLossPercent;
            BindAffectedSideTurns = validated.BindAffectedSideTurns;
            FootprintOffsets = validated.FootprintOffsets;
        }
    }
}
