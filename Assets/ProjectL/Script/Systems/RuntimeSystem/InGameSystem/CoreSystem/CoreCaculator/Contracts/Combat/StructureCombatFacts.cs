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
        private readonly HealthStateSnapshot _health;
        /// <summary>장애물에서는 생존 여부만 1/1 또는 0/1로 투영한다.</summary>
        public HealthStateSnapshot Health => ObstacleDurability == null
            ? _health : new HealthStateSnapshot(IsDestroyed ? 0 : 1, 1);
        public ObstacleDurabilitySnapshot ObstacleDurability { get; }
        public bool IsDestroyed => ObstacleDurability?.IsDestroyed ?? _health.IsDead;
        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }
        public IReadOnlyList<CellCoord> OccupiedCells { get; }

        public StructureCombatFacts(EntityId entityId, StructureSpec definition,
            HealthStateSnapshot health, CellCoord anchor, Rotation4 rotation,
            IReadOnlyList<CellCoord> occupiedCells, Owner owner = Owner.Enemy)
            : this(entityId, definition, health, null, anchor, rotation, occupiedCells, owner)
        {
        }

        public StructureCombatFacts(EntityId entityId, StructureSpec definition,
            ObstacleDurabilitySnapshot durability, CellCoord anchor, Rotation4 rotation,
            IReadOnlyList<CellCoord> occupiedCells, Owner owner = Owner.Neutral)
            : this(entityId, definition, default, durability ?? throw new ArgumentNullException(nameof(durability)),
                anchor, rotation, occupiedCells, owner)
        {
        }

        private StructureCombatFacts(EntityId entityId, StructureSpec definition,
            HealthStateSnapshot health, ObstacleDurabilitySnapshot durability, CellCoord anchor, Rotation4 rotation,
            IReadOnlyList<CellCoord> occupiedCells, Owner owner)
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
            if (durability != null && !ObstacleContentProfile.IsNativeObstacleKey(Definition.Key))
                throw new ArgumentException("타수 내구도는 장애물 정의에만 적용합니다.", nameof(durability));
            _health = health;
            ObstacleDurability = durability;
            Anchor = anchor;
            Rotation = rotation;
            OccupiedCells = Array.AsReadOnly(copy);
        }

        public StructureCombatFacts WithObstacleDurability(ObstacleDurabilitySnapshot durability)
        {
            if (ObstacleDurability == null || durability == null || durability.RequiredHits != ObstacleDurability.RequiredHits)
                throw new ArgumentException("같은 장애물의 필요 타수를 유지해야 합니다.", nameof(durability));
            return new StructureCombatFacts(EntityId, Definition, durability, Anchor, Rotation, OccupiedCells, Owner);
        }

        internal StructureCombatFacts WithPlacement(CellCoord anchor, IReadOnlyList<CellCoord> cells) =>
            new StructureCombatFacts(EntityId, Definition, _health, ObstacleDurability, anchor, Rotation, cells, Owner);

        internal bool SameValues(StructureCombatFacts other) => other != null && EntityId == other.EntityId &&
            Definition == other.Definition && Owner == other.Owner && _health.Equals(other._health) &&
            Equals(ObstacleDurability, other.ObstacleDurability) && Anchor == other.Anchor && Rotation == other.Rotation &&
            System.Linq.Enumerable.SequenceEqual(OccupiedCells, other.OccupiedCells);
    }
}
