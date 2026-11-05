using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    public sealed class TraitGrowthCatalog
    {
        public static TraitGrowthCatalog Disabled { get; } = new(new[] { CategoryKey.Main, CategoryKey.Heavy, CategoryKey.Rider, CategoryKey.Cannon }
            .Select(c => new CategoryTraitProgressionSpec(c, Array.Empty<TraitGrowthStepSpec>(), Array.Empty<TraitKey>())).ToArray(),
                Array.Empty<TraitGrowthDefinition>());
        private readonly Dictionary<CategoryKey, TraitSpec> _defaults = new();
        public TraitSpec GetDefaultTrait(CategoryKey category) => _defaults.TryGetValue(category, out var trait) ? trait : null;
        public bool MatchesDefaultTraits(IReadOnlyDictionary<CategoryKey, TraitKey> assignments) =>
            assignments != null && assignments.Count == _defaults.Count &&
            assignments.All(pair => GetDefaultTrait(pair.Key)?.TraitKey == pair.Value);
        public IReadOnlyList<CategoryTraitProgressionSpec> Categories { get; }
        public IReadOnlyList<TraitGrowthDefinition> Definitions { get; }
        public TraitGrowthCatalog(IReadOnlyList<CategoryTraitProgressionSpec> categories, IReadOnlyList<TraitGrowthDefinition> definitions,
            IReadOnlyDictionary<CategoryKey, TraitKey> defaultTraits = null)
        {
            if (categories == null) throw new ArgumentNullException(nameof(categories));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (categories.Any(c => c == null) || categories.Count != 4 ||
                categories.Select(c => c.Category).Distinct().Count() != 4)
                throw new ArgumentException("Main/Heavy/Rider/Cannon 병과는 각각 하나씩 필요합니다.", nameof(categories));
            if (definitions.Any(d => d == null) || definitions.Select(d => d.Key).Distinct().Count() != definitions.Count)
                throw new ArgumentException("특성 정의가 비어 있거나 키가 중복됩니다.", nameof(definitions));
            var keys = new HashSet<TraitKey>(definitions.Select(d => d.Key));
            if (categories.Any(c => c.Pool.Any(k => !keys.Contains(k))))
                throw new ArgumentException("병과 후보에 대응하는 특성 정의가 없습니다.", nameof(categories));
            if (defaultTraits != null)
            {
                foreach (var pair in defaultTraits)
                {
                    if (pair.Key == CategoryKey.None || !Enum.IsDefined(typeof(CategoryKey), pair.Key) || !keys.Contains(pair.Value))
                        throw new ArgumentException($"병과 기본 특성 정의가 없습니다: {pair.Key}, {pair.Value}.");
                    _defaults.Add(pair.Key, definitions.Single(d => d.Key == pair.Value).Normal);
                }
            }
            Categories = Array.AsReadOnly(categories.ToArray());
            Definitions = Array.AsReadOnly(definitions.ToArray());
        }
        public CategoryTraitProgressionSpec GetCategory(CategoryKey key) => Categories.FirstOrDefault(c => c.Category == key)
            ?? throw new KeyNotFoundException($"성장 병과를 찾을 수 없습니다: {key}");
        public TraitGrowthDefinition GetTrait(TraitKey key) => Definitions.FirstOrDefault(t => t.Key == key)
            ?? throw new KeyNotFoundException($"성장 특성을 찾을 수 없습니다: {key}");
    }
}
