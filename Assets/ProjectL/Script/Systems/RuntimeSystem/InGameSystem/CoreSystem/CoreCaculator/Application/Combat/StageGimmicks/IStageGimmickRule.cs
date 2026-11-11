using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    internal interface IStageGimmickRule
    {
        StageGimmickKey Key { get; }
        IEnumerator<HazardCellFact?> EnumerateForecast(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, long lastBoundaryCount);
        CombatCommandTransition TryAdvance(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, StageGimmickBoundary? boundary);
    }
}
