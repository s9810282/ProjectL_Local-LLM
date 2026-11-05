using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum CombatChangeEvidenceKind
    {
        OrdinaryMove,
        Return,
        Action,
        Effect,
        Lifecycle,
        Observation
    }

    public sealed class CombatChangeEvidence
    {
        public CombatChangeEvidenceKind Kind { get; }
        public IReadOnlyList<EntityId> AffectedEntityIds { get; }
        public IReadOnlyList<CellCoord> ChangedSourceCells { get; }
        public IReadOnlyList<MovementEffectId> UsedMovements { get; }
        public IReadOnlyList<CharacterStateChange> CharacterChanges { get; }
        public IReadOnlyList<long> ChangedSourceIds { get; }

        public CombatChangeEvidence(CombatChangeEvidenceKind kind, IReadOnlyList<EntityId> affectedEntityIds,
            IReadOnlyList<CellCoord> changedSourceCells, IReadOnlyList<MovementEffectId> usedMovements = null,
            IReadOnlyList<CharacterStateChange> characterChanges = null, IReadOnlyList<long> changedSourceIds = null)
        {
            if (!Enum.IsDefined(typeof(CombatChangeEvidenceKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (affectedEntityIds == null) throw new ArgumentNullException(nameof(affectedEntityIds));
            if (changedSourceCells == null) throw new ArgumentNullException(nameof(changedSourceCells));
            if (affectedEntityIds.Any(id => id.IsEmpty)) throw new ArgumentException("빈 개체 식별자는 변경 범위가 아닙니다.", nameof(affectedEntityIds));
            Kind = kind;
            AffectedEntityIds = Array.AsReadOnly(affectedEntityIds.Distinct().OrderBy(id => id).ToArray());
            ChangedSourceCells = Array.AsReadOnly(changedSourceCells.Distinct().OrderBy(c => c.X).ThenBy(c => c.Y).ToArray());
            var uses = (usedMovements ?? Array.Empty<MovementEffectId>()).ToArray();
            if (uses.Any(m => !m.IsValid)) throw new ArgumentException("사용된 이동 출처가 유효해야 합니다.", nameof(usedMovements));
            UsedMovements = Array.AsReadOnly(uses.Distinct().ToArray());
            var changes = (characterChanges ?? Array.Empty<CharacterStateChange>()).ToArray();
            var sources = (changedSourceIds ?? Array.Empty<long>()).ToArray();
            if (changes.Any(c => c == null) || sources.Any(id => id < 1)) throw new ArgumentException("변경 근거가 유효해야 합니다.");
            CharacterChanges = Array.AsReadOnly(changes);
            ChangedSourceIds = Array.AsReadOnly(sources.Distinct().ToArray());
        }
    }
}
