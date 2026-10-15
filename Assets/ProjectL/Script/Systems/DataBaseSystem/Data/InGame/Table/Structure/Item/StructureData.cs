using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure.Item
{
    /// <summary>
    /// 구조물(본부 포함) 한 종류의 규칙 데이터입니다. Core가 받는 순수 C# 데이터이며 StructureDataSO가 생성합니다.
    /// 표시 이름은 ViewGroup의 UnitVisualTable이 가집니다.
    /// </summary>
    public sealed class StructureData : IData
    {
        public EntityKey Key { get; }
        public int FootprintWidth { get; }
        public int FootprintHeight { get; }
        public int MaxHealth { get; }
        public bool IsAttackable { get; }
        public int AttackPower { get; }

        public StructureData(
            EntityKey key,
            int footprintWidth,
            int footprintHeight,
            int maxHealth,
            bool isAttackable,
            int attackPower)
        {
            Key = key;
            FootprintWidth = footprintWidth;
            FootprintHeight = footprintHeight;
            MaxHealth = maxHealth;
            IsAttackable = isAttackable;
            AttackPower = attackPower;
        }

        public void Validate()
        {
            if (Key == EntityKey.None || !System.Enum.IsDefined(typeof(EntityKey), Key))
                throw new InvalidOperationException($"[StructureData] Structure key is invalid. Key: {Key}");
            if (FootprintWidth < 1 || FootprintHeight < 1 || MaxHealth < 1)
                throw new InvalidOperationException($"[StructureData] Value must be at least 1. Key: {Key}");
            if (AttackPower < 0)
                throw new InvalidOperationException($"[StructureData] AttackPower must not be negative. Key: {Key}");
            // Core StructureSpec과 같은 규칙입니다.
            if (IsAttackable && AttackPower < 1)
                throw new InvalidOperationException($"[StructureData] Attackable structure requires AttackPower. Key: {Key}");
        }
    }
}
