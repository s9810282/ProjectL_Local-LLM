using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    internal static class TraitGrowthDefinitionAdapter
    {
        internal static TraitGrowthCatalog Build(ICategoryTraitProgressionTableQuery categories, ITraitGrowthDefinitionTableQuery definitions,
            IReadOnlyDictionary<CategoryKey, TraitKey> defaultTraits = null)
        {
            if (categories == null && definitions == null && !(defaultTraits?.Count > 0)) return TraitGrowthCatalog.Disabled;
            if (categories == null || definitions == null)
                throw new InvalidOperationException("성장 병과·특성 테이블은 함께 등록해야 합니다.");
            try
            {
                var catalog = new TraitGrowthCatalog(categories.GetAll().Select(r =>
                        new CategoryTraitProgressionSpec(r.Category, r.Steps, r.Pool, r.CommonWeight, r.CategoryWeight)).ToArray(),
                    definitions.GetAll().Select(r => new TraitGrowthDefinition(r.Group, TraitSpecAdapter.Read(r.Normal),
                        r.Enhanced == null ? null : TraitSpecAdapter.Read(r.Enhanced), r.Name, r.Description,
                        r.EnhancedName, r.EnhancedDescription, r.Rank2, r.Rank3, r.AllowUnchangedRankValues)).ToArray(), defaultTraits);
                TraitGrowthCatalogValidator.ValidateDefaultTraits(catalog);
                return catalog;
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException("성장 테이블의 정의 또는 참조가 올바르지 않습니다.", exception);
            }
        }
    }
}
