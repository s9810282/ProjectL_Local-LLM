using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item
{
    public sealed class CategoryTraitProgressionData : IData
    {
        private readonly CategoryTraitProgressionSpec _spec;
        public CategoryKey Category => _spec.Category;
        public IReadOnlyList<TraitGrowthStepSpec> Steps => _spec.Steps;
        public IReadOnlyList<TraitKey> Pool => _spec.Pool;
        public int CommonWeight => _spec.CommonWeight;
        public int CategoryWeight => _spec.CategoryWeight;

        public CategoryTraitProgressionData(CategoryKey category, IReadOnlyList<TraitGrowthStepSpec> steps,
            IReadOnlyList<TraitKey> pool)
            : this(category, steps, pool, CategoryTraitProgressionSpec.DefaultCommonWeight,
                CategoryTraitProgressionSpec.DefaultCategoryWeight)
        {
        }

        public CategoryTraitProgressionData(CategoryKey category, IReadOnlyList<TraitGrowthStepSpec> steps,
            IReadOnlyList<TraitKey> pool, int commonWeight, int categoryWeight)
        {
            _spec = new CategoryTraitProgressionSpec(category, steps, pool, commonWeight, categoryWeight);
        }
    }
}
