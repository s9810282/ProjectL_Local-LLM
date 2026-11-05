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

        public CategoryTraitProgressionData(CategoryKey category, IReadOnlyList<TraitGrowthStepSpec> steps,
            IReadOnlyList<TraitKey> pool)
        {
            _spec = new CategoryTraitProgressionSpec(category, steps, pool);
        }
    }
}
