using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels
{
    internal sealed class CategoryRegistry
    {
        private static readonly CategoryKey[] RequiredKeys =
        {
            CategoryKey.Main,
            CategoryKey.Rider,
            CategoryKey.Heavy,
            CategoryKey.Cannon,
        };

        /// <summary>전투 규칙이 없는 선택 분류다. 등록하지 않은 정의에서 쓰면 GetRequired가 거절한다.</summary>
        private static readonly CategoryKey[] OptionalKeys =
        {
            CategoryKey.Boss,
        };

        private readonly Dictionary<CategoryKey, CategoryDefinition> _definitions;

        internal CategoryRegistry(IReadOnlyList<CategorySpec> specs)
        {
            if (specs == null) throw new ArgumentNullException(nameof(specs));

            _definitions = new Dictionary<CategoryKey, CategoryDefinition>(specs.Count);
            for (int i = 0; i < specs.Count; i++)
            {
                CategorySpec spec = specs[i];
                if (spec == null)
                    throw new ArgumentException($"Category spec {i} is null.", nameof(specs));
                if (_definitions.ContainsKey(spec.Key))
                    throw new ArgumentException($"Duplicate Category key: {spec.Key}.", nameof(specs));

                ReadOnlySpan<CategoryRuleSpec> ruleSpecs = spec.RuleSpecs.Span;
                var definitions = new CategoryRuleDefinition[ruleSpecs.Length];
                for (int ruleIndex = 0; ruleIndex < ruleSpecs.Length; ruleIndex++)
                    definitions[ruleIndex] = CategoryRuleFactories.CreateDefinition(ruleSpecs[ruleIndex]);

                try
                {
                    _definitions.Add(spec.Key, new CategoryDefinition(spec.Key,definitions,spec.LevelGrowth,                        spec.MovementAllowance));
                }
                catch (ArgumentException exception)
                {
                    throw new ArgumentException(
                        $"Category {spec.Key} has an invalid rule composition.",
                        nameof(specs),
                        exception);
                }
            }

            for (int i = 0; i < RequiredKeys.Length; i++)
            {
                if (_definitions.ContainsKey(RequiredKeys[i]) == false)
                    throw new ArgumentException($"Missing Category key: {RequiredKeys[i]}.", nameof(specs));
            }

            foreach (CategoryKey key in _definitions.Keys)
            {
                if (Array.IndexOf(RequiredKeys, key) < 0 && Array.IndexOf(OptionalKeys, key) < 0)
                    throw new ArgumentException("Category registry contains an unsupported key.", nameof(specs));
            }
        }

        internal CategoryDefinition GetRequired(CategoryKey key)
        {
            if (key == CategoryKey.None || Enum.IsDefined(typeof(CategoryKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));
            if (_definitions.TryGetValue(key, out CategoryDefinition definition) == false)
                throw new InvalidOperationException($"Category {key} is not registered.");
            return definition;
        }

        internal static CategoryRegistry CreateDefault()
        {
            return new CategoryRegistry(new[]
            {
                new CategorySpec(CategoryKey.Main,Array.Empty<CategoryRuleSpec>(),3),
                new CategorySpec(CategoryKey.Rider,Array.Empty<CategoryRuleSpec>(),4),
                new CategorySpec(CategoryKey.Heavy,Array.Empty<CategoryRuleSpec>(),3),
                new CategorySpec(CategoryKey.Cannon,new CategoryRuleSpec[]
                {
                    new SafeBombardmentCategoryRuleSpec(),
                },2),
                new CategorySpec(CategoryKey.Boss,Array.Empty<CategoryRuleSpec>(),0),
            });
        }
    }
}
