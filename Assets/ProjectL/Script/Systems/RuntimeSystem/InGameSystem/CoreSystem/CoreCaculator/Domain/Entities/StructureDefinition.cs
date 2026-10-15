using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities
{
    internal sealed class StructureDefinition
    {
        internal EntityKey Key { get; }
        internal EntityKind Kind { get; }
        internal TileFootprint Footprint { get; }
        internal int MaxHealth { get; }
        internal bool IsAttackable { get; }
        internal int AttackPower { get; }

        internal StructureDefinition(
            EntityKey key,
            TileFootprint footprint,
            int maxHealth,
            bool isAttackable,
            int attackPower)
        {
            if (Enum.IsDefined(typeof(EntityKey), key) == false ||
                key.ToString().StartsWith("Structure_", StringComparison.Ordinal) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }

            if (footprint == null)
                throw new ArgumentNullException(nameof(footprint));
            if (maxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (isAttackable && attackPower <= 0)
                throw new ArgumentOutOfRangeException(nameof(attackPower), "공격 가능한 건물임에도 공격력이 없습니다.");

            Key = key;
            Kind = key == EntityKey.Structure_Headquarters
                ? EntityKind.Headquarters
                : EntityKind.Structure;
            Footprint = footprint;
            MaxHealth = maxHealth;
            IsAttackable = isAttackable;
            AttackPower = isAttackable ? attackPower : 0;
        }
    }
}
