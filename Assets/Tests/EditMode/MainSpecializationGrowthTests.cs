using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class MainSpecializationGrowthTests
    {
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void MainAndChosenPoolUnionIsDistinctAndOtherClassIsExcluded(CategoryKey category)
        {
            var profile = Profile();
            var owned = Owned(category);
            var keys = TraitGrowthRules.Candidates(owned, profile, Catalog(category));
            Assert.That(keys, Is.EquivalentTo(new[] { TraitKey.TrackingShot, TraitKey.GroundCompaction, TraitKey.HuntersMark, TraitKey.Vanguard }));
            Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Count));
            Assert.That(keys, Has.No.Member(TraitKey.Pull));
        }

        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void ClassChoicePreservesOldMainHistoryAndEnhanceTargetsAcquiredClassTrait(CategoryKey category)
        {
            var profile = Profile();
            var catalog = Catalog(category);
            var before = Owned(category);
            var offer = TraitGrowthFixtures.Offer(before, catalog.GetCategory(CategoryKey.Main).Steps[1],
                TraitKey.HuntersMark, TraitKey.Vanguard, TraitKey.GroundCompaction);
            var choice = TraitGrowthRules.PlanChoice(before, TraitGrowthFixtures.Request(offer, TraitKey.HuntersMark), offer, profile, catalog);
            Assert.That(choice.Succeeded, Is.True);
            var final = choice.Change.Final;
            Assert.That(final.SpecializationCategory, Is.EqualTo(category));
            Assert.That((final.Level, final.Experience, final.HunterAttackBonus), Is.EqualTo((3, 7, 9)));
            Assert.That(final.Growth.Selections.Select(s => s.StepId), Is.EqualTo(new[] { 10, 20 }));
            Assert.That(final.Growth.Selections[0].TraitKey, Is.EqualTo(TraitKey.SoloAction));
            var effective = OwnedUnitSpecResolver.Resolve(profile.Definition, final, catalog);
            Assert.That(effective.Traits.ToArray().Select(t => t.TraitKey),
                Is.EquivalentTo(new[] { TraitKey.Knockback, TraitKey.SoloAction, TraitKey.HuntersMark }));
            Assert.That(TraitGrowthRules.Candidates(final, profile, catalog), Does.Contain(TraitKey.HuntersMark));
            var enhance = TraitGrowthFixtures.Offer(final, catalog.GetCategory(CategoryKey.Main).Steps[2], TraitKey.HuntersMark);
            var enhanced = TraitGrowthRules.PlanChoice(final, TraitGrowthFixtures.Request(enhance, TraitKey.HuntersMark), enhance, profile, catalog);
            Assert.That(enhanced.Succeeded, Is.True);
            Assert.That(OwnedUnitSpecResolver.Resolve(profile.Definition, enhanced.Change.Final, catalog).Traits.ToArray()
                .OfType<HuntersMarkTraitSpec>().Single().BonusDamagePercent, Is.EqualTo(40));
            Assert.That(TraitGrowthRules.Candidates(enhanced.Change.Final, profile, catalog), Is.Empty);
        }

        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void EveryReachableAcquireUsesCombinedPoolWithoutImportingClassSteps(CategoryKey category)
        {
            var catalog = Catalog(category);
            Assert.DoesNotThrow(() => TraitGrowthCatalogValidator.Validate(catalog, new[] { Profile(category) }));
            var insufficient = Catalog(category, insufficient: true);
            Assert.Throws<InvalidOperationException>(() => TraitGrowthCatalogValidator.Validate(insufficient, new[] { Profile(category) }));
            Assert.That(catalog.GetCategory(category).Steps.Single().StepId, Is.EqualTo(999));
        }

        [Test]
        public void EmptyMainSequenceDoesNotGrantTraitsOrClassSteps()
        {
            var profile = Profile();
            var before = Owned(CategoryKey.Rider, empty: true);
            var catalog = Catalog(CategoryKey.Rider, emptyMain: true);
            Assert.That(TraitGrowthRules.Candidates(before, profile, catalog), Is.Empty);
            Assert.That(OwnedUnitSpecResolver.Resolve(profile.Definition, before, catalog).Traits.ToArray()
                .Select(t => t.TraitKey), Is.EqualTo(new[] { TraitKey.Knockback }));
            Assert.That(before.Growth.IsEmpty, Is.True);
        }

        [Test]
        public void RuntimeCompositionKeepsCurrentHistoryWhileEntryValidatesFutureBranches()
        {
            var profile = Profile();
            var owned = Owned(CategoryKey.Rider);
            var catalog = Catalog(CategoryKey.Rider, insufficient: true);
            Assert.DoesNotThrow(() => OwnedUnitSpecResolver.BuildProfiles(new[] { profile }, new[] { owned }, catalog));
            var stage = new StageConfig(8, 6, 8, 2, 2, 42, Array.Empty<InitialEnemyPlacementSpec>(),
                Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), Array.Empty<InitialEnemyStructurePlacementSpec>());
            var initialization = new BattleInitialization(stage, CellCoord.Zero, Rotation4.R0,
                new[] { profile.Definition }, Array.Empty<StructureSpec>(), new[]
                {
                    new CategorySpec(CategoryKey.Main, Array.Empty<CategoryRuleSpec>(), 3),
                    new CategorySpec(CategoryKey.Rider, Array.Empty<CategoryRuleSpec>(), 6),
                    new CategorySpec(CategoryKey.Heavy, Array.Empty<CategoryRuleSpec>(), 1),
                    new CategorySpec(CategoryKey.Cannon, new CategoryRuleSpec[] { new SafeBombardmentCategoryRuleSpec() }, 7)
                }, new[] { profile }, traitGrowth: catalog);
            var failure = Assert.Throws<InvalidOperationException>(() => BattlePreparationCalculator.Prepare(initialization,
                new[] { owned }, new MainCharacterPersistentState(10, 10, 5),
                new System.Collections.Generic.Dictionary<CharacterInstanceId, int> { [owned.Id] = 0 },
                new InitialDrawPlan(Array.Empty<CharacterInstanceId>()), Array.Empty<CharacterCombatFacts>()));
            Assert.That(failure.Message, Does.Contain("성장 후보 부족"));
        }

        [TestCase(CategoryKey.Rider, TraitKey.Leap)]
        [TestCase(CategoryKey.Heavy, TraitKey.SteadfastWill)]
        public void CategoryDefault_GrantsOneCopyAndFiltersOnlyItsOwner(CategoryKey category, TraitKey key)
        {
            var forms = new TraitSpec[] { new LeapTraitSpec(), new SteadfastWillTraitSpec() };
            var definitions = forms.Select(t => new TraitGrowthDefinition(t, null, "trait", "normal", "", "")).ToArray();
            var rows = TraitGrowthCatalog.Disabled.Categories.Select(c => new CategoryTraitProgressionSpec(c.Category,
                new[] { new TraitGrowthStepSpec(1, 2, TraitGrowthKind.Acquire) }, forms.Select(t => t.TraitKey).ToArray())).ToArray();
            var catalog = new TraitGrowthCatalog(rows, definitions, new Dictionary<CategoryKey, TraitKey>
                { [CategoryKey.Rider] = TraitKey.Leap, [CategoryKey.Heavy] = TraitKey.SteadfastWill });
            var profile = TraitGrowthFixtures.Profile(category);
            var record = TraitGrowthFixtures.Owned(profile);
            var effective = OwnedUnitSpecResolver.Resolve(profile.Definition, record, catalog);
            Assert.That(effective.Traits.ToArray().Select(t => t.TraitKey), Is.EqualTo(new[] { key }));
            Assert.That(TraitGrowthRules.Candidates(record, profile, catalog), Is.EquivalentTo(forms.Where(t => t.TraitKey != key).Select(t => t.TraitKey)));
            var native = TraitGrowthFixtures.Profile(category, catalog.GetTrait(key).Normal);
            Assert.That(OwnedUnitSpecResolver.Resolve(native.Definition, record, catalog).Traits.Length, Is.EqualTo(1));
        }

        [Test]
        public void CategoryDefault_OverlapsEarlierAcquireAndSurvivesClearingThroughHistory()
        {
            var original = Catalog(CategoryKey.Rider);
            var mapped = new TraitGrowthCatalog(original.Categories, original.Definitions, new Dictionary<CategoryKey, TraitKey>
                { [CategoryKey.Rider] = TraitKey.SoloAction });
            var raw = Profile().Definition;
            var earned = Owned(CategoryKey.Rider);
            var fresh = Owned(CategoryKey.Rider, empty: true);
            Assert.That(OwnedUnitSpecResolver.Resolve(raw, fresh, mapped).Traits.Length, Is.EqualTo(2));
            foreach (var catalog in new[] { mapped, original })
            {
                var traits = OwnedUnitSpecResolver.Resolve(raw, earned, catalog).Traits.ToArray();
                Assert.That(traits.Count(t => t.TraitKey == TraitKey.SoloAction), Is.EqualTo(1));
                Assert.That(earned.Growth.Selections.Single().StepId, Is.EqualTo(10));
            }
            Assert.That(OwnedUnitSpecResolver.Resolve(raw, fresh, original).Traits.Length, Is.EqualTo(1));
            var duplicate = new OwnedCharacterRecord(earned.Id, earned.CharacterKey, earned.Role, earned.Level, earned.Experience,
                earned.Growth.Append(new TraitGrowthSelection(20, TraitKey.SoloAction)), specializationCategory: CategoryKey.Rider);
            Assert.Throws<InvalidOperationException>(() => OwnedUnitSpecResolver.Resolve(raw, duplicate, mapped));
        }

        [Test]
        public void CategoryDefault_EnhanceRequiresSurvivingNormalSourceOnReload()
        {
            var basis = TraitGrowthFixtures.Profile();
            var original = TraitGrowthFixtures.Catalog(new[] { new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Enhance) });
            var catalog = new TraitGrowthCatalog(original.Categories, original.Definitions, new Dictionary<CategoryKey, TraitKey>
                { [CategoryKey.Heavy] = TraitKey.Knockback });
            var owned = TraitGrowthFixtures.Owned(basis, growth: OwnedTraitGrowth.Empty.Append(new TraitGrowthSelection(10, TraitKey.Knockback)));
            var upgraded = OwnedUnitSpecResolver.Resolve(basis.Definition, owned, catalog);
            Assert.That(((KnockbackTraitSpec)upgraded.Traits.ToArray().Single()).DistanceCells, Is.EqualTo(3));
            var failure = Assert.Throws<InvalidOperationException>(() => OwnedUnitSpecResolver.Resolve(basis.Definition, owned, original));
            Assert.That(failure.Message, Does.Contain(owned.CharacterKey.ToString()).And.Contain("Heavy").And.Contain("Knockback"));
            var native = TraitGrowthFixtures.Profile(CategoryKey.Heavy, new KnockbackTraitSpec(4));
            Assert.That(((KnockbackTraitSpec)OwnedUnitSpecResolver.Resolve(native.Definition, TraitGrowthFixtures.Owned(native), catalog)
                .Traits.ToArray().Single()).DistanceCells, Is.EqualTo(4));
            Assert.That(((KnockbackTraitSpec)OwnedUnitSpecResolver.Resolve(native.Definition, owned, original).Traits.ToArray().Single()).DistanceCells,
                Is.EqualTo(3));
        }

        internal static TraitGrowthCatalog Catalog(CategoryKey chosen, bool insufficient = false, bool emptyMain = false)
        {
            var definitions = TraitGrowthFixtures.Definitions.Concat(new[]
            {
                new TraitGrowthDefinition(new PullTraitSpec(1), null, "pull", "pull", "", "")
            }).ToArray();
            var steps = emptyMain ? Array.Empty<TraitGrowthStepSpec>() : new[]
            {
                new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Acquire),
                new TraitGrowthStepSpec(20, 3, TraitGrowthKind.Acquire),
                new TraitGrowthStepSpec(30, 3, TraitGrowthKind.Enhance),
                new TraitGrowthStepSpec(40, 4, TraitGrowthKind.Acquire)
            };
            var mainPool = insufficient ? new[] { TraitKey.Knockback, TraitKey.SoloAction }
                : new[] { TraitKey.Knockback, TraitKey.SoloAction, TraitKey.TrackingShot, TraitKey.GroundCompaction };
            var chosenPool = insufficient ? new[] { TraitKey.SoloAction, TraitKey.HuntersMark }
                : new[] { TraitKey.SoloAction, TraitKey.HuntersMark, TraitKey.Vanguard };
            return new TraitGrowthCatalog(new[] { CategoryKey.Main, CategoryKey.Rider, CategoryKey.Heavy, CategoryKey.Cannon }
                .Select(c => new CategoryTraitProgressionSpec(c, c == CategoryKey.Main ? steps : new[] { new TraitGrowthStepSpec(999, 2, TraitGrowthKind.Enhance) },
                    c == CategoryKey.Main ? mainPool : c == chosen ? chosenPool : new[] { TraitKey.Pull })).ToArray(), definitions);
        }

        private static OwnedCharacterRecord Owned(CategoryKey category, bool empty = false) => new(new CharacterInstanceId(1), EntityKey.Unit_Akimbo,
            CharacterOwnershipRole.Main, 3, 7, empty ? OwnedTraitGrowth.Empty : new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, TraitKey.SoloAction) }), 9, category);

        private static CombatAttackProfile Profile(CategoryKey? category = null)
        {
            var basis = TraitGrowthFixtures.Profile(CategoryKey.Main, new KnockbackTraitSpec(1));
            var s = basis.Definition;
            return new CombatAttackProfile(new UnitSpec(s.Key, s.Role, 1, 1, new[] { CellCoord.Zero }, s.MaxHealth, s.MoveMethod,
                s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority, s.Traits.ToArray(), s.OwnerEligibility,
                specializationCategory: category), basis.Method);
        }
    }
}
