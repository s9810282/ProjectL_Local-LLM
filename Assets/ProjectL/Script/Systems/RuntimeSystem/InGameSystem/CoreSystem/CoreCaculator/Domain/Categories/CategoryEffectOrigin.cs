using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories
{
    /// <summary>
    /// Category 규칙과 공통 EffectOriginKey 사이의 변환을 담당합니다.
    /// 이 변환은 Categories 폴더 안에만 두고, 공통 CombatEffects는 CategoryKey를 알지 않습니다.
    /// </summary>
    public static class CategoryEffectOrigin
    {
        internal static EffectOriginKey Create(EntityId unitId, CategoryKey categoryKey, CategoryRuleKey ruleKey)
        {
            ValidateUnitId(unitId);
            ValidateCategoryKey(categoryKey);
            ValidateRuleKey(ruleKey);

            return new EffectOriginKey(
                EffectOwnerKind.Category,
                unitId.Value,
                (int)categoryKey,
                GetRuleKeyName(ruleKey));
        }

        internal static CategoryKey GetRequiredCategoryKey(EffectOriginKey origin, EntityId unitId)
        {
            ValidateOrigin(origin, unitId);

            CategoryKey categoryKey = (CategoryKey)origin.DefinitionKey;
            ValidateCategoryKey(categoryKey);
            return categoryKey;
        }

        internal static CategoryRuleKey GetRequiredRuleKey(EffectOriginKey origin, EntityId unitId)
        {
            ValidateOrigin(origin, unitId);

            switch (origin.RuleKey)
            {
                case ManeuverRefundRuleKeyName:
                    return CategoryRuleKey.ManeuverRefund;
                case DirectionalArmorRuleKeyName:
                    return CategoryRuleKey.DirectionalArmor;
                case SafeBombardmentRuleKeyName:
                    return CategoryRuleKey.SafeBombardment;
                default:
                    throw new ArgumentException("Invalid Category effect origin", nameof(origin));
            }
        }

        private const string ManeuverRefundRuleKeyName = "maneuver-refund";
        private const string DirectionalArmorRuleKeyName = "directional-armor";
        private const string SafeBombardmentRuleKeyName = "safe-bombardment";

        private static string GetRuleKeyName(CategoryRuleKey ruleKey)
        {
            switch (ruleKey)
            {
                case CategoryRuleKey.ManeuverRefund:
                    return ManeuverRefundRuleKeyName;
                case CategoryRuleKey.DirectionalArmor:
                    return DirectionalArmorRuleKeyName;
                case CategoryRuleKey.SafeBombardment:
                    return SafeBombardmentRuleKeyName;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ruleKey));
            }
        }

        private static void ValidateOrigin(EffectOriginKey origin, EntityId unitId)
        {
            ValidateUnitId(unitId);

            if (origin.IsValid == false ||
                origin.Kind != EffectOwnerKind.Category ||
                origin.OwnerInstanceId != unitId.Value)
            {
                throw new ArgumentException("Invalid Category effect origin", nameof(origin));
            }
        }

        private static void ValidateUnitId(EntityId unitId)
        {
            if (unitId.IsEmpty)
                throw new ArgumentException("Unit ID cannot be empty.", nameof(unitId));
        }

        private static void ValidateCategoryKey(CategoryKey categoryKey)
        {
            if (categoryKey == CategoryKey.None ||
                Enum.IsDefined(typeof(CategoryKey), categoryKey) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(categoryKey));
            }
        }

        private static void ValidateRuleKey(CategoryRuleKey ruleKey)
        {
            if (ruleKey == CategoryRuleKey.None ||
                Enum.IsDefined(typeof(CategoryRuleKey), ruleKey) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(ruleKey));
            }
        }
    }
}
