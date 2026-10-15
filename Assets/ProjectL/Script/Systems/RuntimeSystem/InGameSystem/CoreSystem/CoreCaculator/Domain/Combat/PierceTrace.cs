using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>피해량과 분리된 한 번의 공간 탐색 결과다. 자격 없는 대상은 조우 순서를 소비하지 않는다.</summary>
    internal sealed class PierceTrace
    {
        internal IReadOnlyList<(EntityId Id, int Encounter, int Ray)> Encounters { get; }
        internal IReadOnlyList<AttackBlockedContact> BlockedContacts { get; }
        internal IReadOnlyList<CellCoord> ReachableCells { get; }
        internal bool HasWallBlockedEligibleTargets => BlockedContacts.Count != 0;

        internal PierceTrace(IEnumerable<(EntityId Id, int Encounter, int Ray)> encounters,
            IEnumerable<AttackBlockedContact> blockedContacts, IEnumerable<CellCoord> reachableCells = null)
        {
            if (encounters == null) throw new ArgumentNullException(nameof(encounters));
            if (blockedContacts == null) throw new ArgumentNullException(nameof(blockedContacts));
            Encounters = Array.AsReadOnly(encounters.ToArray());
            BlockedContacts = Array.AsReadOnly(blockedContacts.ToArray());
            ReachableCells = Array.AsReadOnly((reachableCells ?? Array.Empty<CellCoord>()).Distinct().OrderBy(c => c.Y).ThenBy(c => c.X).ToArray());
        }
    }
}
