using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 특성 표시 순서·출처(특성 표시 출처 설계 TO2·TO3, 결정 Q-TO1~Q-TO4 A)를 확인한다.
    /// 순서는 유닛 기본(유닛 정의 순서) → 병과 고유 → 레벨업 획득(획득 순서)이며, 키 순서와 무관하다.
    /// </summary>
    public sealed class TraitDisplayOriginTests
    {
        [Test]
        public void Rider_KeepsNativeAuthoredOrder_ThenCategoryDefault()
        {
            // 유닛 정의 순서는 키 순서(HuntersMark 190 < TrackingShot 410)와 반대다.
            var basis = TraitGrowthFixtures.Profile(CategoryKey.Rider,
                new TrackingShotTraitSpec(50, 10), new HuntersMarkTraitSpec(20, 1)).Definition;

            Assert.That(Display(basis, null, Catalog(CategoryKey.Rider)), Is.EqualTo(new[]
            {
                (TraitKey.TrackingShot, TraitOrigin.Unit),
                (TraitKey.HuntersMark, TraitOrigin.Unit),
                (TraitKey.Leap, TraitOrigin.Category),
            }));
        }

        [Test]
        public void Heavy_NativeCopyOfCategoryDefault_IsCategory_AndComesAfterUnitTraits()
        {
            // Q-TO2 A: 유닛이 원래 가진 같은 키도 병과 고유다. 유닛 정의에서 앞에 있어도 병과 고유 자리로 간다.
            var basis = TraitGrowthFixtures.Profile(CategoryKey.Heavy,
                new SteadfastWillTraitSpec(), new KnockbackTraitSpec(1)).Definition;

            Assert.That(Display(basis, null, Catalog(CategoryKey.Heavy)), Is.EqualTo(new[]
            {
                (TraitKey.Knockback, TraitOrigin.Unit),
                (TraitKey.SteadfastWill, TraitOrigin.Category),
            }));
        }

        [Test]
        public void Growth_LowerKeyAcquiredLaterGoesLast_AndEnhanceKeepsPosition()
        {
            var profile = TraitGrowthFixtures.Profile(CategoryKey.Rider,
                new TrackingShotTraitSpec(50, 10), new HuntersMarkTraitSpec(20, 1));
            // 단계 10 획득(Knockback 50, 가장 작은 키) → 단계 20 원래 특성 HuntersMark 강화.
            var owned = TraitGrowthFixtures.Owned(profile, growth: new OwnedTraitGrowth(new[]
            {
                new TraitGrowthSelection(10, TraitKey.Knockback),
                new TraitGrowthSelection(20, TraitKey.HuntersMark),
            }));

            Assert.That(Display(profile.Definition, owned, Catalog(CategoryKey.Rider)), Is.EqualTo(new[]
            {
                (TraitKey.TrackingShot, TraitOrigin.Unit),
                (TraitKey.HuntersMark, TraitOrigin.Unit),
                (TraitKey.Leap, TraitOrigin.Category),
                (TraitKey.Knockback, TraitOrigin.Growth),
            }));
        }

        [Test]
        public void Growth_KeepsAcquisitionOrder()
        {
            var profile = TraitGrowthFixtures.Profile(CategoryKey.Heavy);
            var catalog = Catalog(CategoryKey.Heavy,
                new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Acquire), new TraitGrowthStepSpec(20, 2, TraitGrowthKind.Acquire));
            var owned = TraitGrowthFixtures.Owned(profile, growth: new OwnedTraitGrowth(new[]
            {
                new TraitGrowthSelection(10, TraitKey.Vanguard),
                new TraitGrowthSelection(20, TraitKey.Knockback),
            }));

            Assert.That(Display(profile.Definition, owned, catalog), Is.EqualTo(new[]
            {
                (TraitKey.SteadfastWill, TraitOrigin.Category),
                (TraitKey.Vanguard, TraitOrigin.Growth),
                (TraitKey.Knockback, TraitOrigin.Growth),
            }));
        }

        [Test]
        public void MainSpecialization_EarlierAcquireMatchingNewDefault_IsCategory_OtherwiseGrowth()
        {
            // Q-TO3 A: 전직 전에 얻은 특성이 전직 병과의 기본 특성이면 병과 고유다. 기본 특성이 아니면 레벨업 획득이다.
            var original = MainSpecializationGrowthTests.Catalog(CategoryKey.Rider);
            var mapped = new TraitGrowthCatalog(original.Categories, original.Definitions,
                new Dictionary<CategoryKey, TraitKey> { [CategoryKey.Rider] = TraitKey.SoloAction });
            var raw = TraitGrowthFixtures.Profile(CategoryKey.Main, new KnockbackTraitSpec(1)).Definition;
            var earned = new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main, 3, 7,
                new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, TraitKey.SoloAction) }), 9, CategoryKey.Rider);

            Assert.That(Display(raw, earned, mapped), Is.EqualTo(new[]
            {
                (TraitKey.Knockback, TraitOrigin.Unit),
                (TraitKey.SoloAction, TraitOrigin.Category),
            }));
            Assert.That(Display(raw, earned, original), Is.EqualTo(new[]
            {
                (TraitKey.Knockback, TraitOrigin.Unit),
                (TraitKey.SoloAction, TraitOrigin.Growth),
            }));
        }

        [Test]
        public void CategoryWithoutDefault_AllUnitInAuthoredOrder()
        {
            var basis = TraitGrowthFixtures.Profile(CategoryKey.Cannon,
                new TrackingShotTraitSpec(50, 10), new KnockbackTraitSpec(1)).Definition;

            Assert.That(Display(basis, null, Catalog(CategoryKey.Cannon)), Is.EqualTo(new[]
            {
                (TraitKey.TrackingShot, TraitOrigin.Unit),
                (TraitKey.Knockback, TraitOrigin.Unit),
            }));
        }

        [Test]
        public void BattleTraitSettings_PublishesSameDisplayAsResolver_ForUnitsAndOwned()
        {
            var catalog = Catalog(CategoryKey.Rider);
            var profile = TraitGrowthFixtures.Profile(CategoryKey.Rider,
                new TrackingShotTraitSpec(50, 10), new HuntersMarkTraitSpec(20, 1));
            var owned = TraitGrowthFixtures.Owned(profile, growth: new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, TraitKey.Knockback) }));

            var settings = OwnedUnitSpecResolver.BuildTraitSettings(new[] { profile.Definition }, new[] { owned }, catalog);
            var unitDisplay = settings.ForUnit(profile.Definition.Key).Display.Select(e => (e.Key, e.Origin)).ToArray();

            Assert.That(unitDisplay, Is.EqualTo(Display(profile.Definition, null, catalog)));
            Assert.That(settings.ForUnit(profile.Definition.Key).Traits.Select(t => t.TraitKey),
                Is.EquivalentTo(unitDisplay.Select(e => e.Item1)), "표시 목록의 키 집합은 설정과 같다.");
        }

        private static (TraitKey, TraitOrigin)[] Display(UnitSpec basis, OwnedCharacterRecord owned, TraitGrowthCatalog catalog)
        {
            var display = new List<TraitDisplayEntry>();
            var resolved = owned == null
                ? OwnedUnitSpecResolver.ResolveBase(basis, catalog, display)
                : OwnedUnitSpecResolver.Resolve(basis, owned, catalog, null, display);
            Assert.That(display.Select(e => e.Key), Is.EquivalentTo(resolved.Traits.ToArray().Select(t => t.TraitKey)),
                "표시 목록의 키 집합은 합성한 특성과 같다.");
            return display.Select(e => (e.Key, e.Origin)).ToArray();
        }

        /// <summary>고정물 특성에 뛰어넘기·굳건한 의지를 더하고, Rider·Heavy 기본 특성을 운영처럼 연결한 카탈로그다.</summary>
        private static TraitGrowthCatalog Catalog(CategoryKey category, params TraitGrowthStepSpec[] steps)
        {
            var definitions = TraitGrowthFixtures.Definitions.Concat(new[]
            {
                new TraitGrowthDefinition(new LeapTraitSpec(), null, "leap", "", "", ""),
                new TraitGrowthDefinition(new SteadfastWillTraitSpec(), null, "will", "", "", ""),
            }).ToArray();
            var basis = TraitGrowthFixtures.Catalog(steps.Length == 0 ? null : steps, category, definitions);
            return new TraitGrowthCatalog(basis.Categories, basis.Definitions, new Dictionary<CategoryKey, TraitKey>
                { [CategoryKey.Rider] = TraitKey.Leap, [CategoryKey.Heavy] = TraitKey.SteadfastWill });
        }
    }
}
