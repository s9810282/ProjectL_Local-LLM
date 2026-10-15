using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>구조물은 보유 캐릭터 연결 없이 전투 개체 ID와 배치 상태만 가진다.</summary>
    public sealed class StructureCombatFacts
    {
        public EntityId EntityId { get; }
        public StructureSpec Definition { get; }
        public Owner Owner { get; }
        public HealthStateSnapshot Health { get; }
        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }
        public IReadOnlyList<CellCoord> OccupiedCells { get; }

        public StructureCombatFacts(EntityId entityId, StructureSpec definition,
            HealthStateSnapshot health, CellCoord anchor, Rotation4 rotation,
            IReadOnlyList<CellCoord> occupiedCells, Owner owner = Owner.Enemy)
        {
            if (entityId.IsEmpty)
                throw new ArgumentException("구조물 ID가 필요합니다.", nameof(entityId));
            if (Enum.IsDefined(typeof(Rotation4), rotation) == false)
                throw new ArgumentOutOfRangeException(nameof(rotation));
            if (occupiedCells == null || occupiedCells.Count == 0)
                throw new ArgumentException("구조물 점유 셀이 필요합니다.", nameof(occupiedCells));

            var copy = new CellCoord[occupiedCells.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = occupiedCells[i];

            EntityId = entityId;
            if (owner != Owner.Player && owner != Owner.Enemy && !(owner == Owner.Neutral &&
                definition != null && ObstacleContentProfile.IsNativeObstacleKey(definition.Key)))
                throw new ArgumentOutOfRangeException(nameof(owner));
            Owner = owner;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Health = health;
            Anchor = anchor;
            Rotation = rotation;
            OccupiedCells = Array.AsReadOnly(copy);
        }
    }
}
