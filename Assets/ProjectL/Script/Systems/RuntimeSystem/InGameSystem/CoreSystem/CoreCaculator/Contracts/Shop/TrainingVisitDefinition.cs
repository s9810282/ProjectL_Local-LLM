using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop
{
    public sealed class TrainingVisitDefinition
    {
        public TrainingPrices Prices { get; }
        public TraitGrowthCatalog Catalog { get; }
        public IReadOnlyList<UnitSpec> Units { get; }
        public TrainingVisitDefinition(TrainingPrices prices, TraitGrowthCatalog catalog, IReadOnlyList<UnitSpec> units)
        {
            Prices = prices ?? throw new ArgumentNullException(nameof(prices));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (units == null || units.Any(u => u == null) || units.Select(u => u.Key).Distinct().Count() != units.Count)
                throw new ArgumentException("훈련 원본 유닛 정의는 키별로 하나씩 필요합니다.", nameof(units));
            Units = Array.AsReadOnly(units.ToArray());
        }
    }
}
