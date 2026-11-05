using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Item
{
    public sealed class ConsumableItemData : IData
    {
        public int Key { get; }
        public int CostGold { get; }
        public string Name { get; }
        public string Description { get; }
        public ItemEffectSpec Effect { get; }

        public ConsumableItemData(int key, int costGold, string name, string description, ItemEffectSpec effect)
        {
            if (key <= 0 || costGold < 0 || string.IsNullOrWhiteSpace(name) || description == null || effect == null)
                throw new InvalidOperationException("아이템 정의의 키·가격·텍스트·효과가 올바르지 않습니다.");
            Key = key;
            CostGold = costGold;
            Name = name;
            Description = description;
            Effect = effect;
        }
    }
}
