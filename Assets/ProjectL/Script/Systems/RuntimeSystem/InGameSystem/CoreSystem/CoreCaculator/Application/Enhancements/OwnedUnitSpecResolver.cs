using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements
{
    /// <summary>보유 개체의 선택을 불변 정의에 합성한다. 원본 카탈로그와 게임 상태는 변경하지 않는다.</summary>
    public static class OwnedUnitSpecResolver
    {
        internal static TraitSpec RequireAvailableDefault(TraitGrowthCatalog catalog, CategoryKey category)
        {
            var trait = catalog.GetDefaultTrait(category);
            // 병과 기본 특성은 플레이어 유닛에 붙으므로 적 전용 특성을 거절한다.
            if (trait != null && TraitOwnerPolicy.IsEnemyOnly(trait.TraitKey))
                throw new InvalidOperationException($"병과 기본 특성에 적 전용 특성을 넣을 수 없습니다: {category}, {trait.TraitKey}.");
            if (trait != null && !Combat.DeferredCombatFeatures.IsTraitAvailable(trait.TraitKey))
                throw new InvalidOperationException($"병과 기본 특성을 사용할 수 없습니다: {category}, {trait.TraitKey}, {Combat.DeferredCombatFeatures.GetTraitUnavailableReason(trait.TraitKey)}.");
            return trait;
        }

        public static UnitSpec ResolveBase(UnitSpec basis, TraitGrowthCatalog catalog)
        {
            if (basis == null) throw new ArgumentNullException(nameof(basis));
            return Compose(basis, basis.SpecializationCategory, null, catalog);
        }

        /// <summary><see cref="ResolveBase(UnitSpec, TraitGrowthCatalog)"/>와 같고, 표시 순서·출처를 <paramref name="display"/>에 채운다.</summary>
        internal static UnitSpec ResolveBase(UnitSpec basis, TraitGrowthCatalog catalog, List<TraitDisplayEntry> display)
        {
            if (basis == null) throw new ArgumentNullException(nameof(basis));
            if (display == null) throw new ArgumentNullException(nameof(display));
            return Compose(basis, basis.SpecializationCategory, null, catalog, display: display);
        }

        internal static UnitSpec ResolveSpecialization(UnitSpec basis, CategoryKey selected, TraitGrowthCatalog catalog)
            => Compose(basis, selected, null, catalog);

        public static UnitSpec Resolve(UnitSpec basis, OwnedCharacterRecord owned, TraitGrowthCatalog catalog) =>
            Resolve(basis, owned, catalog, null);

        /// <param name="display">주면 표시 순서·출처를 채운다(특성 표시 출처 설계 TO2·TO3).</param>
        internal static UnitSpec Resolve(UnitSpec basis, OwnedCharacterRecord owned, TraitGrowthCatalog catalog,
            Dictionary<TraitKey, int> ranks, List<TraitDisplayEntry> display = null)
        {
            if (basis == null)
                throw new ArgumentNullException(nameof(basis));
            if (owned == null)
                throw new ArgumentNullException(nameof(owned));
            if (basis.Key != owned.CharacterKey)
                throw new InvalidOperationException("보유 개체와 기본 정의 키가 다릅니다.");
            return Compose(basis, owned.SpecializationCategory, owned, catalog, ranks, display);
        }

        private static UnitSpec Compose(UnitSpec basis, CategoryKey? specialization, OwnedCharacterRecord owned, TraitGrowthCatalog catalog,
            Dictionary<TraitKey, int> ranks = null, List<TraitDisplayEntry> display = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var category = MainSpecializationRules.ResolveCombatCategory(basis.CategoryKey, specialization);
            var benefit = RequireAvailableDefault(catalog, category);
            var growth = owned?.Growth ?? OwnedTraitGrowth.Empty;
            var selections = growth.Selections;
            var traits = basis.Traits.ToArray().ToList();
            bool categoryOnly = benefit != null && !traits.Any(t => t.TraitKey == benefit.TraitKey);
            if (categoryOnly) traits.Add(benefit);

            var progression = selections.Count == 0 ? null : EffectiveTraitProgression.Resolve(basis, owned, catalog);
            var steps = progression == null ? null : TraitGrowthHistory.ResolveSteps(owned, progression);
            var enhanced = new HashSet<TraitKey>();
            var acquired = new HashSet<TraitKey>();
            for (int i = 0; i < selections.Count; i++)
            {
                var selection = selections[i];
                var step = steps[i];
                TraitGrowthDefinition definition;
                try
                {
                    definition = catalog.GetTrait(selection.TraitKey);
                }
                catch (KeyNotFoundException exception) { throw new InvalidOperationException("선택한 특성 정의가 없습니다.", exception); }
                int index = traits.FindIndex(t => t.TraitKey == selection.TraitKey);
                if (step.Kind == TraitGrowthKind.Acquire)
                {
                    acquired.Add(selection.TraitKey);
                    if (index < 0) traits.Add(definition.Normal);
                    else if (categoryOnly && selection.TraitKey == benefit.TraitKey && !enhanced.Contains(selection.TraitKey))
                        categoryOnly = false; // 전직 전에 얻은 같은 특성은 이력 한 번만 인정한다.
                    else throw new InvalidOperationException("획득 특성이 이미 있습니다.");
                }
                else
                {
                    if (index < 0 || definition.Enhanced == null || !enhanced.Add(selection.TraitKey))
                        throw new InvalidOperationException($"강화 대상·정의가 없거나 이미 강화했습니다: {basis.Key}, {category}, {selection.TraitKey}.");
                    if (!definition.IsTrainable) traits[index] = definition.Enhanced;
                }
            }
            if (growth.Purchases.Keys.Any(key => !traits.Any(t => t.TraitKey == key)))
                throw new InvalidOperationException("보유하지 않은 특성의 구매 이력이 있습니다.");
            bool changed = categoryOnly || selections.Count > 0 || specialization != basis.SpecializationCategory;
            for (int i = 0; i < traits.Count; i++)
            {
                var trait = traits[i];
                var key = trait.TraitKey;
                var definition = catalog.Definitions.FirstOrDefault(d => d.Key == key);
                growth.Purchases.TryGetValue(key, out int paid);
                int rank = 1;
                if (definition?.IsTrainable == true)
                {
                    rank += paid + (enhanced.Contains(key) ? 1 : 0);
                    traits[i] = definition.ResolveRank(trait, rank);
                    changed = true;
                }
                else if (paid > 0)
                    throw new InvalidOperationException($"훈련할 수 없는 특성의 구매 이력이 있습니다: {key}.");
                ranks?.Add(key, rank);
            }
            display?.AddRange(DisplayOf(traits, benefit, acquired));
            if (!changed) return basis;
            return new UnitSpec(basis.Key, basis.Role, basis.FootprintWidth, basis.FootprintHeight,
                basis.FootprintLocalCells.ToArray(), basis.MaxHealth, basis.MoveMethod, basis.AttackRange, basis.CategoryKey,
                basis.AttackPower, basis.PhysicalDefense, basis.Priority, traits, basis.OwnerEligibility, basis.Occlusion, basis.Behavior,
                basis.KillReward, specialization);
        }

        public static IReadOnlyList<CombatAttackProfile> BuildProfiles(IReadOnlyList<CombatAttackProfile> baseProfiles,
            IReadOnlyList<OwnedCharacterRecord> owned, TraitGrowthCatalog catalog)
        {
            if (baseProfiles == null)
                throw new ArgumentNullException(nameof(baseProfiles));
            if (owned == null)
                throw new ArgumentNullException(nameof(owned));
            if (baseProfiles.Any(p => p == null || p.OwnedId.HasValue) ||
                baseProfiles.Select(p => p.Definition.Key).Distinct().Count() != baseProfiles.Count)
                throw new InvalidOperationException("중복 없는 기본 공격 프로필만 필요합니다.");
            if (owned.Any(o => o == null) || owned.Select(o => o.Id).Distinct().Count() != owned.Count)
                throw new InvalidOperationException("보유 식별자가 비어 있거나 중복됩니다.");
            var result = new List<CombatAttackProfile>(baseProfiles.Count + owned.Count);
            foreach (var profile in baseProfiles)
            {
                var effective = ResolveBase(profile.Definition, catalog);
                result.Add(ReferenceEquals(effective, profile.Definition) ? profile : new CombatAttackProfile(effective, profile.Method));
            }
            foreach (var record in owned)
            {
                var basis = baseProfiles.SingleOrDefault(p => p.Definition.Key == record.CharacterKey);
                if (basis == null)
                    throw new InvalidOperationException("보유 개체의 기본 공격 프로필이 없습니다.");
                result.Add(new CombatAttackProfile(Resolve(basis.Definition, record, catalog), basis.Method, record.Id));
            }
            return result.AsReadOnly();
        }

        /// <summary>
        /// 전투에 공개할 특성 설정 표다(설명 수치 공개 설계 SP3). <see cref="BuildProfiles"/>와 같은 계산이다:
        /// 유닛 키마다 <see cref="ResolveBase"/>, 보유 개체마다 <see cref="Resolve(UnitSpec, OwnedCharacterRecord, TraitGrowthCatalog)"/>.
        /// </summary>
        public static BattleTraitSettings BuildTraitSettings(IReadOnlyList<UnitSpec> units,
            IReadOnlyList<OwnedCharacterRecord> owned, TraitGrowthCatalog catalog)
        {
            if (units == null)
                throw new ArgumentNullException(nameof(units));
            if (owned == null)
                throw new ArgumentNullException(nameof(owned));
            var bases = new Dictionary<EntityKey, TraitSettingsSet>();
            foreach (var unit in units)
            {
                if (unit == null || bases.ContainsKey(unit.Key))
                    throw new InvalidOperationException("중복 없는 유닛 명세만 필요합니다.");
                var display = new List<TraitDisplayEntry>();
                var resolved = ResolveBase(unit, catalog, display);
                bases.Add(unit.Key, TraitSettingsSet.From(resolved, display));
            }
            var instances = new Dictionary<CharacterInstanceId, (EntityKey Key, TraitSettingsSet Settings)>();
            foreach (var record in owned)
            {
                if (record == null || instances.ContainsKey(record.Id))
                    throw new InvalidOperationException("보유 식별자가 비어 있거나 중복됩니다.");
                var basis = units.SingleOrDefault(u => u.Key == record.CharacterKey)
                    ?? throw new InvalidOperationException($"보유 개체의 유닛 명세가 없습니다: {record.CharacterKey}.");
                var display = new List<TraitDisplayEntry>();
                var resolved = Resolve(basis, record, catalog, null, display);
                instances.Add(record.Id, (record.CharacterKey, TraitSettingsSet.From(resolved, display)));
            }
            return new BattleTraitSettings(bases, instances);
        }

        /// <summary>
        /// 합성한 특성 목록의 표시 순서·출처다(특성 표시 출처 설계 TO2·TO3). 출처는 병과 기본 특성 키 → 획득 키 → 그 밖 순으로 정하고,
        /// 출처 순서로 안정 정렬한다. 합성 목록은 유닛 정의 순서 뒤에 획득 순서로 쌓이므로 묶음 안 순서는 그대로 남는다.
        /// </summary>
        private static IEnumerable<TraitDisplayEntry> DisplayOf(List<TraitSpec> traits, TraitSpec benefit, HashSet<TraitKey> acquired) =>
            traits.Select(trait => new TraitDisplayEntry(trait.TraitKey,
                    benefit != null && trait.TraitKey == benefit.TraitKey ? TraitOrigin.Category
                    : acquired.Contains(trait.TraitKey) ? TraitOrigin.Growth
                    : TraitOrigin.Unit))
                .OrderBy(entry => entry.Origin);
    }
}
