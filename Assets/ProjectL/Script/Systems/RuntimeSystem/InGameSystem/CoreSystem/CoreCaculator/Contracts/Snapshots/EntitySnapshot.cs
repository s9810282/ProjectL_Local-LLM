using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>
    /// View가 Entity 하나의 종류, 체력, 위치를 그릴 때 읽는 복사본이다.
    /// </summary>
    public sealed class EntitySnapshot
    {
        public EntityId Id { get; }
        public EntityKey Key { get; }
        public EntityKind Kind { get; }
        public Owner Owner { get; }
        public HealthStateSnapshot Health { get; }
        public int MaxHealth => Health.MaxHealth;
        public int CurrentHealth => Health.CurrentHealth;
        public bool IsDead => Health.IsDead;
        public UnitRole? UnitRole { get; }
        public EntityArea Area { get; }
        public int? ReserveSlotIndex { get; }
        public CellCoord? CombatAnchor { get; }
        public Rotation4? CombatRotation { get; }
        public ReadOnlyMemory<CellCoord> OccupiedCells { get; }
        public UnitStatsSnapshot? UnitStats { get; }

        public EntitySnapshot(
            EntityId id,
            EntityKey key,
            EntityKind kind,
            Owner owner,
            int maxHealth,
            int currentHealth,
            bool isDead,
            EntityArea area,
            int? reserveSlotIndex,
            CellCoord? combatAnchor,
            Rotation4? combatRotation,
            CellCoord[] occupiedCells,
            UnitStatsSnapshot? unitStats,
            UnitRole? unitRole = null)
        {
            if (id == EntityId.Empty)
                throw new ArgumentException("Entity id cannot be empty.", nameof(id));
            if (key == EntityKey.None)
                throw new ArgumentException("Entity key cannot be None.", nameof(key));
            if (kind == EntityKind.None)
                throw new ArgumentException("Entity kind cannot be None.", nameof(kind));
            if (owner == Owner.None)
                throw new ArgumentException("Owner cannot be None.", nameof(owner));
            if (maxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (currentHealth < 0 || currentHealth > maxHealth)
                throw new ArgumentOutOfRangeException(nameof(currentHealth));
            if (isDead != (currentHealth == 0))
                throw new ArgumentException("IsDead와 CurrentHealth가 일치하지 않습니다.", nameof(isDead));
            if (occupiedCells == null)
                throw new ArgumentNullException(nameof(occupiedCells));

            UnitRole? resolvedUnitRole = unitRole;
            if (kind == EntityKind.Unit)
            {
                resolvedUnitRole ??= global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.UnitRole.Regular;
                if (Enum.IsDefined(
                        typeof(global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.UnitRole),
                        resolvedUnitRole.Value) == false)
                    throw new ArgumentOutOfRangeException(nameof(unitRole));
            }
            else if (resolvedUnitRole.HasValue)
            {
                throw new ArgumentException("Unit이 아닌 Entity에는 UnitRole을 지정할 수 없습니다.", nameof(unitRole));
            }

            if (area == EntityArea.Reserve)
            {
                if (reserveSlotIndex == null || reserveSlotIndex.Value < 0)
                    throw new ArgumentException("Reserve snapshot requires a valid slot.", nameof(reserveSlotIndex));
                if (combatAnchor != null || combatRotation != null || occupiedCells.Length != 0)
                    throw new ArgumentException("Reserve snapshot cannot contain combat placement.");
            }
            else if (area == EntityArea.Combat)
            {
                if (reserveSlotIndex != null || combatAnchor == null || combatRotation == null)
                    throw new ArgumentException("Combat snapshot requires anchor and rotation.");
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(area));
            }

            CellCoord[] copiedCells = new CellCoord[occupiedCells.Length];
            Array.Copy(occupiedCells, copiedCells, occupiedCells.Length);

            Id = id;
            Key = key;
            Kind = kind;
            Owner = owner;
            Health = new HealthStateSnapshot(currentHealth, maxHealth);
            UnitRole = resolvedUnitRole;
            Area = area;
            ReserveSlotIndex = reserveSlotIndex;
            CombatAnchor = combatAnchor;
            CombatRotation = combatRotation;
            OccupiedCells = copiedCells;
            UnitStats = unitStats;
        }
    }
}
