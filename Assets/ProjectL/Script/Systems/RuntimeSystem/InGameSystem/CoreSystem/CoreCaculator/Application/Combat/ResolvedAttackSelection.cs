using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 번 검증한 지정과 대상 자격을 실행 및 AI에 전달한다. 전투 상태로 저장하지 않는다.</summary>
    internal sealed class ResolvedAttackSelection
    {
        internal AttackCellSelection Cells { get; }
        internal AttackCellSelection ReachableCells { get; }
        internal IReadOnlyList<KeyValuePair<EntityId, AttackTargetRole>> Targets { get; }
        internal IReadOnlyList<CharacterCombatFacts> EligibleCharacters { get; }
        internal IReadOnlyList<StructureCombatFacts> EligibleStructures { get; }
        internal AttackTargetingPolicy Policy { get; }
        internal bool IsPiercing => PierceTrace != null;
        internal PierceTrace PierceTrace { get; }

        internal ResolvedAttackSelection(AttackCellSelection cells, AttackCellSelection reachableCells,
            IReadOnlyList<KeyValuePair<EntityId, AttackTargetRole>> targets,
            IReadOnlyList<CharacterCombatFacts> eligibleCharacters,
            IReadOnlyList<StructureCombatFacts> eligibleStructures, AttackTargetingPolicy policy,
            PierceTrace pierceTrace = null)
        {
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
            ReachableCells = reachableCells ?? throw new ArgumentNullException(nameof(reachableCells));
            Targets = Array.AsReadOnly((targets ?? throw new ArgumentNullException(nameof(targets))).ToArray());
            EligibleCharacters = Array.AsReadOnly((eligibleCharacters ?? throw new ArgumentNullException(nameof(eligibleCharacters))).ToArray());
            EligibleStructures = Array.AsReadOnly((eligibleStructures ?? throw new ArgumentNullException(nameof(eligibleStructures))).ToArray());
            Policy = policy;
            PierceTrace = pierceTrace;
        }
    }
}
