using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    /// <summary>Main 성장 단계를 유지하고 후보를 합치며 선택한 병과의 가중치를 적용한다.</summary>
    public static class EffectiveTraitProgression
    {
        public static CategoryTraitProgressionSpec Resolve(UnitSpec basis, OwnedCharacterRecord owned, TraitGrowthCatalog catalog)
        {
            if (basis == null) throw new ArgumentNullException(nameof(basis));
            if (owned == null) throw new ArgumentNullException(nameof(owned));
            if (basis.Key != owned.CharacterKey)
                throw new InvalidOperationException("성장 대상과 기본 정의 키가 다릅니다.");
            return Resolve(basis, owned.SpecializationCategory, catalog);
        }

        internal static CategoryTraitProgressionSpec Resolve(UnitSpec basis, CategoryKey? specialization, TraitGrowthCatalog catalog)
        {
            if (basis == null) throw new ArgumentNullException(nameof(basis));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            MainSpecializationRules.ResolveCombatCategory(basis.CategoryKey, specialization);
            var original = catalog.GetCategory(basis.CategoryKey);
            if (!specialization.HasValue) return original;
            if (basis.Role != UnitRole.MainCharacter || basis.OwnerEligibility != UnitOwnerEligibility.PlayerOnly)
                throw new ArgumentException("플레이어 Main 정의만 전직 성장 후보를 사용할 수 있습니다.", nameof(basis));
            PlayerMainDefinitionPolicy.Validate(basis.Key, basis.Role, basis.CategoryKey, basis.OwnerEligibility);
            var selected = catalog.GetCategory(specialization.Value);
            return new CategoryTraitProgressionSpec(original.Category, original.Steps,
                original.Pool.Concat(selected.Pool).Distinct().ToArray(), selected.CommonWeight, selected.CategoryWeight);
        }
    }
}
