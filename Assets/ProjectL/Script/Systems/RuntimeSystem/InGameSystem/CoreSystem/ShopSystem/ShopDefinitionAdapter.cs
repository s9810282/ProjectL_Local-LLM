using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem
{
    /// <summary>정의 전체를 검증한 뒤 추첨할 수 있는 순수 후보로 바꾼다.</summary>
    internal static class ShopDefinitionAdapter
    {
        internal static IReadOnlyList<ShopCandidateSpec> Build(ShopStageDetail detail,
            IUnitTableQuery units, IConsumableItemTableQuery items)
        {
            if (detail == null) throw new ArgumentNullException(nameof(detail));
            if (units == null) throw new ArgumentNullException(nameof(units));
            detail.Validate();
            var result = new List<ShopCandidateSpec>();
            foreach (var key in detail.UnitKeys)
            {
                var definition = CharacterDefinitionAdapter.Read(units, key);
                if (!CharacterRosterRules.PlanAcquire(definition, Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.CharacterDefinitionFacts>()).Succeeded || definition.Key != key)
                    throw new InvalidOperationException("상점 유닛은 획득 가능한 플레이어 유닛이어야 합니다.");
                result.Add(ShopCandidateSpec.ForUnit(key, detail.UnitRecruitCostGold));
            }
            var rows = new List<ConsumableItemData>();
            var itemRules = CoreCaculator.Application.Combat.Items.ItemRuleRegistry.CreateDefault();
            foreach (int key in detail.ItemKeys)
            {
                if (items == null || !items.TryGet(key, out var row) || row == null || row.Key != key)
                    throw new InvalidOperationException("상점 아이템 정의가 없습니다.");
                rows.Add(row);
                if (itemRules.IsAvailable(row.Effect))
                    result.Add(ShopCandidateSpec.ForItem(key, row.CostGold, row.Name, row.Description));
            }
            ItemDefinitionAdapter.Build(rows);
            return result.AsReadOnly();
        }
    }
}
