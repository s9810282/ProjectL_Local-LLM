using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Items;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items
{
    public sealed class ItemRuleRegistry
    {
        private readonly IReadOnlyDictionary<Type, IItemRule> _rules;

        private ItemRuleRegistry()
        {
            _rules = new ReadOnlyDictionary<Type, IItemRule>(new Dictionary<Type, IItemRule>
            {
                { typeof(WhiskeyItemSpec), new WhiskeyItemRule() },
                { typeof(RoketItemSpec), new RoketItemRule() },
                { typeof(RopeItemSpec), new RopeItemRule() },
                { typeof(LanternItemSpec), new LanternItemRule() },
            });
        }

        public static ItemRuleRegistry CreateDefault() => new ItemRuleRegistry();

        public void Validate(ItemEffectSpec spec) => GetRequired(spec);

        public bool IsAvailable(ItemEffectSpec spec)
        {
            Validate(spec);
            return spec is not RopeItemSpec;
        }

        public string GetUnavailableReason(ItemEffectSpec spec) =>
            IsAvailable(spec) ? "" : "DeferredUnitTurnTiming";

        public IItemRule GetRequired(ItemEffectSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (!_rules.TryGetValue(spec.GetType(), out IItemRule rule))
                throw new InvalidOperationException($"등록되지 않은 아이템 효과 명세입니다: {spec.GetType().Name}");
            return rule;
        }
    }
}
