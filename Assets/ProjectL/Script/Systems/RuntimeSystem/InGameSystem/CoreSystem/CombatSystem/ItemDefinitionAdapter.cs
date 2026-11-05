using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Item;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    internal static class ItemDefinitionAdapter
    {
        internal static IReadOnlyList<ItemDefinition> Build(IReadOnlyList<ConsumableItemData> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var rules = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items.ItemRuleRegistry.CreateDefault();
            var keys = new HashSet<int>();
            var result = new ItemDefinition[rows.Count];
            for (int i = 0; i < result.Length; i++)
            {
                var row = rows[i] ?? throw new InvalidOperationException("아이템 행이 없습니다.");
                if (!keys.Add(row.Key)) throw new InvalidOperationException("아이템 키가 중복됩니다.");
                rules.Validate(row.Effect);
                result[i] = new ItemDefinition(row.Key, row.Effect);
            }
            return Array.AsReadOnly(result);
        }
    }
}
