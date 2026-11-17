using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    public static class TraitGrowthCatalogValidator
    {
        public static void Validate(TraitGrowthCatalog catalog, IReadOnlyList<CombatAttackProfile> units,
            IReadOnlyList<PlayerSkillDefinition> playerSkills = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (units == null) throw new ArgumentNullException(nameof(units));
            ValidateAvailability(catalog);
            foreach (var profile in units)
            {
                if (profile == null) throw new ArgumentException("유닛 프로필이 비어 있습니다.", nameof(units));
                var unit = OwnedUnitSpecResolver.ResolveBase(profile.Definition, catalog);
                var playerSkill = playerSkills?.SingleOrDefault(s => s.MainCharacterKey == unit.Key);
                playerSkill?.ValidateTraits(unit.Traits.ToArray().Select(t => t.TraitKey));
                var effective = ValidateProfile(unit, profile, catalog);
                PlayerMainDefinitionPolicy.Validate(unit.Key, unit.Role, unit.CategoryKey, unit.OwnerEligibility);
                if (unit.OwnerEligibility == UnitOwnerEligibility.EnemyOnly ||
                    unit.Role != UnitRole.Regular && unit.Role != UnitRole.MainCharacter ||
                    unit.CategoryKey == CategoryKey.Boss || unit.Role == UnitRole.Regular && unit.CategoryKey == CategoryKey.Main)
                    continue;
                if (unit.Role == UnitRole.MainCharacter && !unit.SpecializationCategory.HasValue)
                    foreach (var selected in new[] { CategoryKey.Rider, CategoryKey.Heavy, CategoryKey.Cannon })
                        if (catalog.GetDefaultTrait(selected) != null || catalog.GetDefaultTrait(CategoryKey.Main) != null)
                            ValidateSpecialization(catalog, profile, selected, playerSkill);
                var progression = EffectiveTraitProgression.Resolve(unit, unit.SpecializationCategory, catalog);
                ValidatePaths(effective, progression, catalog, playerSkill);
            }
        }

        /// <summary>원래 무기·고유 특성에서 선택 병과와 Main의 모든 성장 경로를 검사한다.</summary>
        public static void ValidateSpecialization(TraitGrowthCatalog catalog, CombatAttackProfile basis, CategoryKey selected,
            PlayerSkillDefinition playerSkill = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (basis == null) throw new ArgumentNullException(nameof(basis));
            var progression = EffectiveTraitProgression.Resolve(basis.Definition, selected, catalog);
            ValidateAvailability(catalog);
            var unit = OwnedUnitSpecResolver.ResolveSpecialization(basis.Definition, selected, catalog);
            playerSkill?.ValidateTraits(unit.Traits.ToArray().Select(t => t.TraitKey));
            var effective = ValidateProfile(unit, basis, catalog);
            ValidatePaths(effective, progression, catalog, playerSkill);
        }

        private static void ValidatePaths(CombatAttackProfile profile, CategoryTraitProgressionSpec progression, TraitGrowthCatalog catalog,
            PlayerSkillDefinition playerSkill)
        {
            var owned = profile.Definition.Traits.ToArray().Select(t => t.TraitKey).ToHashSet();
            var enhanced = new HashSet<TraitKey>();
            var path = new List<TraitKey>();
            var visited = new HashSet<string>();
            Visit(profile, progression, catalog, 0, owned, enhanced, path, visited, playerSkill);
        }

        private static CombatAttackProfile ValidateProfile(UnitSpec unit, CombatAttackProfile basis, TraitGrowthCatalog catalog)
        {
            if (catalog.GetDefaultTrait(unit.CombatCategoryKey) != null && !TraitGrowthRules.HasDistanceDamageCoverage(unit, basis.Method))
                throw new InvalidOperationException($"병과 기본 특성의 사거리 피해 정의가 부족합니다: {unit.Key}, {unit.CombatCategoryKey}.");
            try
            {
                return ReferenceEquals(unit, basis.Definition) ? basis : new CombatAttackProfile(unit, basis.Method);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"병과 특성과 공격 방식이 맞지 않습니다: {unit.Key}, {unit.CombatCategoryKey}.", exception);
            }
        }

        public static void ValidateDefaultTraits(TraitGrowthCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            foreach (CategoryKey category in Enum.GetValues(typeof(CategoryKey)))
                OwnedUnitSpecResolver.RequireAvailableDefault(catalog, category);
        }


        private static void ValidateAvailability(TraitGrowthCatalog catalog)
        {
            ValidateDefaultTraits(catalog);
            foreach (var key in catalog.Definitions.Select(d => d.Key)
                .Concat(catalog.Categories.SelectMany(c => c.Pool)).Distinct())
            {
                if (TraitOwnerPolicy.IsEnemyOnly(key))
                    throw new InvalidOperationException($"Trait {key}: EnemyOnly. 성장 정의·후보에 넣을 수 없습니다.");
                if (Combat.DeferredCombatFeatures.IsTraitRetired(key))
                    throw new InvalidOperationException($"Trait {key}: Retired.");
                if (Combat.DeferredCombatFeatures.IsImplementationPending(key))
                    throw new InvalidOperationException($"Trait {key}: ImplementationPending.");
            }
        }

        private static void Visit(CombatAttackProfile profile, CategoryTraitProgressionSpec progression, TraitGrowthCatalog catalog,
            int index, HashSet<TraitKey> owned, HashSet<TraitKey> enhanced, List<TraitKey> path, HashSet<string> visited,
            PlayerSkillDefinition playerSkill)
        {
            if (index == progression.Steps.Count) return;
            string state = index + ":" + string.Join(",", owned.OrderBy(k => k)) + ":" + string.Join(",", enhanced.OrderBy(k => k));
            if (!visited.Add(state)) return;
            var step = progression.Steps[index];
            var candidates = TraitGrowthRules.EligibleTraits(profile, owned, enhanced, step.Kind, progression, catalog, playerSkill);
            const int required = 1;
            if (candidates.Count < required)
                throw new InvalidOperationException($"성장 후보 부족: Category={progression.Category}, Unit={profile.Definition.Key}, " +
                    $"Step={step.StepId}, Kind={step.Kind}, Path=[{string.Join(" -> ", path)}], Candidates={candidates.Count}, Required={required}.");
            foreach (var key in candidates)
            {
                var next = step.Kind == TraitGrowthKind.Acquire ? owned : enhanced;
                next.Add(key);
                path.Add(key);
                Visit(profile, progression, catalog, index + 1, owned, enhanced, path, visited, playerSkill);
                path.RemoveAt(path.Count - 1);
                next.Remove(key);
            }
        }
    }
}
