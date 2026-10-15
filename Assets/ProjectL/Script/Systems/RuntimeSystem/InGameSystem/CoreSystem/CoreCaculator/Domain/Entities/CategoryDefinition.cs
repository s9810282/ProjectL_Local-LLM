using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities
{
    public sealed class CategoryDefinition
    {
        public CategoryKey Key { get; }
        public ReadOnlyMemory<CategoryRuleDefinition> RuleDefinitions { get; }

        internal CategoryDefinition(CategoryKey key, CategoryRuleDefinition[] ruleDefinitions)
        {
            if (key == CategoryKey.None || Enum.IsDefined(typeof(CategoryKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));
            if (ruleDefinitions == null)
                throw new ArgumentNullException(nameof(ruleDefinitions));

            var copied = new CategoryRuleDefinition[ruleDefinitions.Length];
            var seen = new HashSet<CategoryRuleKey>();
            for (int i = 0; i < copied.Length; i++)
            {
                CategoryRuleDefinition definition = ruleDefinitions[i];
                if (definition == null)
                    throw new ArgumentException("Category rule definition cannot be null.", nameof(ruleDefinitions));
                if (seen.Add(definition.Key) == false)
                    throw new ArgumentException($"Duplicate Category rule: {definition.Key}.", nameof(ruleDefinitions));
                copied[i] = definition;
            }

            Array.Sort(copied, (left, right) => left.Key.CompareTo(right.Key));
            ValidateComposition(key, copied);
            Key = key;
            RuleDefinitions = copied;
        }

        private static void ValidateComposition(
            CategoryKey key,
            IReadOnlyList<CategoryRuleDefinition> rules)
        {
            CategoryRuleKey expected = key switch
            {
                CategoryKey.Base => CategoryRuleKey.None,
                CategoryKey.Rider => CategoryRuleKey.ManeuverRefund,
                CategoryKey.Heavy => CategoryRuleKey.DirectionalArmor,
                CategoryKey.Cannon => CategoryRuleKey.SafeBombardment,
                _ => throw new ArgumentOutOfRangeException(nameof(key)),
            };

            if (expected == CategoryRuleKey.None)
            {
                if (rules.Count != 0)
                    throw new ArgumentException("Base Category cannot contain combat rules.", nameof(rules));
                return;
            }

            if (rules.Count != 1 || rules[0].Key != expected)
            {
                throw new ArgumentException(
                    $"Category {key} requires exactly the {expected} rule.", nameof(rules));
            }
        }
    }
}
