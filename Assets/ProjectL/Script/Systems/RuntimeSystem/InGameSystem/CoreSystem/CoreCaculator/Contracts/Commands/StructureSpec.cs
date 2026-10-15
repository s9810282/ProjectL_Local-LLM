using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class StructureSpec
    {
        public EntityKey Key { get; }
        public int FootprintWidth { get; }
        public int FootprintHeight { get; }
        public ReadOnlyMemory<CellCoord> FootprintLocalCells { get; }
        public int MaxHealth { get; }
        public bool IsAttackable { get; }
        public int AttackPower { get; }

        public StructureSpec(EntityKey key, int footprintWidth, int footprintHeight, CellCoord[] footprintLocalCells, int maxHealth, bool isAttackable, int attackPower)
        {
            if (key == EntityKey.None || Enum.IsDefined(typeof(EntityKey), key) == false) 
                throw new ArgumentOutOfRangeException(nameof(key));
            if (footprintWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(footprintWidth));
            if (footprintHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(footprintHeight));
            if (footprintLocalCells == null || footprintLocalCells.Length == 0)
                throw new ArgumentException(nameof(footprintLocalCells));
            if (maxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (isAttackable && attackPower <= 0)
                throw new ArgumentOutOfRangeException("공격 가능한 건물임에도 공격력이 없습니다.");

            CellCoord[] copiedCells = new CellCoord[footprintLocalCells.Length];
            Array.Copy(footprintLocalCells, copiedCells, footprintLocalCells.Length);

            Key = key;
            FootprintWidth = footprintWidth;
            FootprintHeight = footprintHeight;
            FootprintLocalCells = copiedCells;
            MaxHealth = maxHealth;
            IsAttackable = isAttackable;
            AttackPower = isAttackable == true ? attackPower : 0;
        }
    }
}
