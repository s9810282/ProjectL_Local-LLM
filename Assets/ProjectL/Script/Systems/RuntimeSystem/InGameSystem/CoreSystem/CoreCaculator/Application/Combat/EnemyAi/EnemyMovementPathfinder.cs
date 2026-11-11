using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    internal static class EnemyMovementPathfinder
    {
        internal static bool TryFind(CombatCommandInput input, EntityId targetId, out MovementPath path,
            EnemyDecisionDiagnostics diagnostics = null)
        {
            using var sample = CoreProfiling.Sample(CoreProfileSection.EnemyPathfind);
            using var search = new EnemyPursuitSearch(input, targetId, diagnostics);
            while (!search.IsComplete) search.Step();
            return search.TryGetPath(out path);
        }

        internal static bool CanAttackFrom(CombatCommandInput input, CharacterCombatFacts actor, CellCoord cell, EntityId targetId) =>
            EnemyPursuitSearch.CanAttackFrom(input, actor, cell, targetId);
    }
}
