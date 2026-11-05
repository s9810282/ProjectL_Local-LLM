using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class TraitGrowthCatalogTests
    {
        [Test]
        public void CategoryDefault_PublicEligibilityFiltersRawBasis()
        {
            var basis = Profile(CategoryKey.Rider);
            var catalog = Catalog(new[] { Acquire() }, new[] { Definition(new LeapTraitSpec()) }, CategoryKey.Rider);
            var mapped = new TraitGrowthCatalog(catalog.Categories, catalog.Definitions, new Dictionary<CategoryKey, TraitKey>
                { [CategoryKey.Rider] = TraitKey.Leap });
            Assert.That(TraitGrowthRules.EligibleTraits(basis, Array.Empty<TraitKey>(), Array.Empty<TraitKey>(), TraitGrowthKind.Acquire,
                mapped.GetCategory(CategoryKey.Rider), mapped), Is.Empty);
        }

        [Test]
        public void CategoryDefault_CoreValidationRejectsDeferredAssignmentWithoutAnyUnits()
        {
            var definitions = new[] { Definition(new ThornFieldTraitSpec(1, 1)) };
            var rows = TraitGrowthCatalog.Disabled.Categories;
            Assert.DoesNotThrow(() => TraitGrowthCatalogValidator.Validate(new TraitGrowthCatalog(rows, definitions), Array.Empty<CombatAttackProfile>()));
            var mapped = new TraitGrowthCatalog(rows, definitions, new Dictionary<CategoryKey, TraitKey> { [CategoryKey.Boss] = TraitKey.ThornField });
            Assert.Throws<InvalidOperationException>(() => TraitGrowthCatalogValidator.Validate(mapped, Array.Empty<CombatAttackProfile>()));
        }

        private static readonly TraitSpec[] Options =
        {
            new KnockbackTraitSpec(1), new SoloActionTraitSpec(1, 1, 0, 1),
            new VanguardTraitSpec(2, 1), new TrackingShotTraitSpec(50, 10),
            new GroundCompactionTraitSpec(1, 1), new HuntersMarkTraitSpec(20, 1)
        };

        private static TraitGrowthStepSpec Acquire(int id = 1, int level = 2) => new(id, level, TraitGrowthKind.Acquire);
        private static TraitGrowthStepSpec Enhance(int id = 2, int level = 3) => new(id, level, TraitGrowthKind.Enhance);
        private static TraitGrowthDefinition Definition(TraitSpec normal, TraitSpec enhanced = null) =>
            new(normal, enhanced, "normal", "description", "enhanced", "enhanced description");

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void AcquireValidation_AcceptsOneOrMoreEligibleCandidates(int count)
        {
            var definitions = Options.Take(count).Select(t => Definition(t)).ToArray();
            Assert.DoesNotThrow(() => TraitGrowthCatalogValidator.Validate(Catalog(new[] { Acquire() }, definitions), new[] { Profile() }));
        }

        [Test]
        public void AcquireValidation_ReportsReachableZeroAtSecondStepWithPriorPath()
        {
            var catalog = Catalog(new[] { Acquire(), Acquire(2, 3) }, new[] { Definition(Options[0]) });
            var failure = Assert.Throws<InvalidOperationException>(() => TraitGrowthCatalogValidator.Validate(catalog, new[] { Profile() }));
            Assert.That(failure.Message, Does.Contain("Heavy").And.Contain("Unit_Shielder").And.Contain("Step=2")
                .And.Contain("Path=[Knockback]").And.Contain("Candidates=0").And.Contain("Required=1"));
        }

        private static CombatAttackProfile Profile(CategoryKey category = CategoryKey.Heavy,
            TraitSpec[] native = null, TargetingMethodSpec method = null,
            UnitOwnerEligibility eligibility = UnitOwnerEligibility.PlayerOnly, UnitRole role = UnitRole.Regular) =>
            new(new UnitSpec(category == CategoryKey.Main ? EntityKey.Unit_Akimbo : EntityKey.Unit_Shielder,
                category == CategoryKey.Main ? UnitRole.MainCharacter : role, 1, 1, new[] { new CellCoord(0, 0) },
                10, MoveMethod.LiftAndPlace, 2, category, 1, 0, 1, native ?? Array.Empty<TraitSpec>(), eligibility),
                method ?? new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0));

        private static TraitGrowthCatalog Catalog(TraitGrowthStepSpec[] steps, TraitGrowthDefinition[] definitions,
            CategoryKey category = CategoryKey.Heavy, TraitKey[] pool = null) =>
            new(TraitGrowthCatalog.Disabled.Categories.Select(row => row.Category == category
                ? new CategoryTraitProgressionSpec(category, steps, pool ?? definitions.Select(d => d.Key).ToArray())
                : row).ToArray(), definitions);

        [Test]
        public void StepOrder_RejectsDuplicateIdsDescendingLevelsAndLevelOne()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Acquire(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Acquire(level: 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TraitGrowthStepSpec(1, 2, TraitGrowthKind.None));
            Assert.Throws<ArgumentException>(() => Catalog(new[] { Acquire(), Acquire() }, Array.Empty<TraitGrowthDefinition>()));
            Assert.Throws<ArgumentException>(() => Catalog(new[] { Acquire(level: 3), Enhance(level: 2) }, Array.Empty<TraitGrowthDefinition>()));
            var rows = new[] { Enhance(20, 2), Acquire(10, 2) };
            Assert.That(Catalog(rows, Array.Empty<TraitGrowthDefinition>()).GetCategory(CategoryKey.Heavy).Steps.Select(s => s.StepId),
                Is.EqualTo(new[] { 20, 10 }));
        }

        [Test]
        public void AcquireCount_ComesFromRowsAndIncludesNativeOverlap()
        {
            var definitions = Options.Take(5).Select(t => Definition(t)).ToArray();
            var steps = new[] { Acquire(), Acquire(2, 3) };
            Assert.Throws<InvalidOperationException>(() => TraitGrowthCatalogValidator.Validate(
                Catalog(steps, definitions.Take(1).ToArray()), new[] { Profile() }));
            Assert.DoesNotThrow(() => TraitGrowthCatalogValidator.Validate(
                Catalog(steps, definitions.Take(2).ToArray()), new[] { Profile() }));
            Assert.Throws<InvalidOperationException>(() => TraitGrowthCatalogValidator.Validate(
                Catalog(steps, definitions.Take(2).ToArray()), new[] { Profile(native: new[] { Options[0] }) }));
            Assert.DoesNotThrow(() => TraitGrowthCatalogValidator.Validate(
                Catalog(steps, definitions), new[] { Profile(native: new[] { Options[0] }) }));
        }

        [Test]
        public void Enhance_AllReachablePathsMustRemainSatisfiable()
        {
            var definitions = Options.Take(3).Select(t => Definition(t))
                .Concat(new[] { Definition(Options[3], new TrackingShotTraitSpec(50, 20)) }).ToArray();
            var catalog = Catalog(new[] { Acquire(), Enhance() }, definitions);
            var error = Assert.Throws<InvalidOperationException>(() => TraitGrowthCatalogValidator.Validate(catalog, new[] { Profile() }));
            Assert.That(error.Message, Does.Contain("Heavy").And.Contain("Unit_Shielder").And.Contain("2").And.Contain("Path=[Knockback]"));
            Assert.DoesNotThrow(() => TraitGrowthCatalogValidator.Validate(catalog,
                new[] { Profile(native: new TraitSpec[] { new TrackingShotTraitSpec(50, 20) }) }));
        }

        [Test]
        public void MainConfiguration_EmptyAndConfiguredAreBothValid()
        {
            var main = Profile(CategoryKey.Main);
            Assert.DoesNotThrow(() => TraitGrowthCatalogValidator.Validate(TraitGrowthCatalog.Disabled, new[] { main }));
            var catalog = Catalog(new[] { Acquire() }, Options.Take(3).Select(t => Definition(t)).ToArray(), CategoryKey.Main);
            Assert.DoesNotThrow(() => TraitGrowthCatalogValidator.Validate(catalog, new[] { main }));
            Assert.That(Eligible(main, catalog).Count, Is.EqualTo(3));
            Assert.That(Eligible(Profile(), catalog), Is.Empty);
        }

        [Test]
        public void Contracts_DefensivelyCopyListsAndRejectMissingDuplicateReferences()
        {
            var steps = new[] { Acquire() };
            var pool = Options.Take(3).Select(t => t.TraitKey).ToArray();
            var category = new CategoryTraitProgressionSpec(CategoryKey.Heavy, steps, pool);
            steps[0] = Enhance();
            pool[0] = TraitKey.Pull;
            Assert.That(category.Steps[0].Kind, Is.EqualTo(TraitGrowthKind.Acquire));
            Assert.That(category.Pool[0], Is.EqualTo(TraitKey.Knockback));
            Assert.Throws<NotSupportedException>(() => ((IList<TraitKey>)category.Pool)[0] = TraitKey.Pull);
            Assert.Throws<ArgumentException>(() => new CategoryTraitProgressionSpec(CategoryKey.Boss, Array.Empty<TraitGrowthStepSpec>(), Array.Empty<TraitKey>()));
            Assert.Throws<ArgumentException>(() => new CategoryTraitProgressionSpec(CategoryKey.Heavy, Array.Empty<TraitGrowthStepSpec>(),
                new[] { TraitKey.Knockback, TraitKey.Knockback }));
            Assert.Throws<ArgumentException>(() => Catalog(new[] { Acquire() }, Array.Empty<TraitGrowthDefinition>(), pool: new[] { TraitKey.Pull }));
            Assert.Throws<ArgumentException>(() => new TraitGrowthCatalog(new[] { category, category }, Array.Empty<TraitGrowthDefinition>()));
            Assert.Throws<ArgumentException>(() => Catalog(Array.Empty<TraitGrowthStepSpec>(), new[] { Definition(Options[0]), Definition(Options[0]) }));
            Assert.Throws<ArgumentException>(() => new TraitGrowthCatalog(Array.Empty<CategoryTraitProgressionSpec>(), Array.Empty<TraitGrowthDefinition>()));
            var definitions = new[] { Definition(Options[0]) };
            var categories = TraitGrowthCatalog.Disabled.Categories.ToArray();
            var catalog = new TraitGrowthCatalog(categories, definitions);
            categories[0] = category;
            definitions[0] = Definition(Options[1]);
            Assert.That(catalog.Definitions[0].Key, Is.EqualTo(TraitKey.Knockback));
            Assert.That(catalog.Categories[0].Category, Is.EqualTo(CategoryKey.Main));
            Assert.Throws<KeyNotFoundException>(() => catalog.GetTrait(TraitKey.Pull));
        }

        [Test]
        public void Definition_RequiresNormalAndMatchingOptionalEnhancedIdentity()
        {
            Assert.Throws<ArgumentNullException>(() => Definition(null));
            Assert.Throws<ArgumentException>(() => Definition(Options[0], Options[1]));
            Assert.That(Definition(Options[0]).Enhanced, Is.Null);
            Assert.That(Definition(Options[0], new KnockbackTraitSpec(3)).Key, Is.EqualTo(TraitKey.Knockback));
        }

        [Test]
        public void Eligibility_ExcludesDuplicatesUnavailableAndAlreadyEnhancedKeys()
        {
            var definitions = new[]
            {
                Definition(new SoloActionTraitSpec(1, 1, 0, 1), new SoloActionTraitSpec(3, 1, 0, 1)),
                Definition(new KnockbackTraitSpec(1), new KnockbackTraitSpec(3)),
                Definition(new ThornFieldTraitSpec(1, 1))
            };
            var catalog = Catalog(Array.Empty<TraitGrowthStepSpec>(), definitions);
            var profile = Profile(native: new TraitSpec[] { new KnockbackTraitSpec(1) });
            Assert.That(Eligible(profile, catalog, owned: new[] { TraitKey.Knockback }), Is.EqualTo(new[] { TraitKey.SoloAction }));
            Assert.That(Eligible(profile, catalog, TraitGrowthKind.Enhance, new[] { TraitKey.Knockback, TraitKey.SoloAction },
                new[] { TraitKey.Knockback }), Is.EqualTo(new[] { TraitKey.SoloAction }));
            Assert.That(Eligible(Profile(), catalog, TraitGrowthKind.Enhance, new[] { TraitKey.ThornField }), Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() => Eligible(profile, catalog, TraitGrowthKind.None));
            var unavailable = Catalog(new[] { Acquire() }, definitions);
            Assert.DoesNotThrow(() => TraitGrowthCatalogValidator.Validate(unavailable, new[] { profile }));
        }

        [Test]
        public void Eligibility_UsesActualTargetingAndOwnedTraitCombination()
        {
            var catalog = Catalog(Array.Empty<TraitGrowthStepSpec>(), new[]
            {
                Definition(new PullTraitSpec(1)), Definition(new ImpactBoltTraitSpec(1)),
                Definition(new PiercingFalloffTraitSpec(50, 3)), Definition(new KnockbackTraitSpec(1))
            });
            var single = Profile();
            Assert.That(Eligible(single, catalog), Has.Member(TraitKey.Pull).And.Member(TraitKey.ImpactBolt).And.Member(TraitKey.PiercingFalloff));
            Assert.That(Eligible(single, catalog, owned: new[] { TraitKey.PiercingFalloff }),
                Has.No.Member(TraitKey.Pull).And.No.Member(TraitKey.ImpactBolt));
            var directional = Profile(method: new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Direction,
                TargetingSplash.None, 0));
            Assert.That(Eligible(directional, catalog), Is.EqualTo(new[] { TraitKey.Knockback }));
            Assert.That(Eligible(single, catalog, owned: new[] { TraitKey.Knockback }), Has.No.Member(TraitKey.ImpactBolt));
        }

        [Test]
        public void Validator_ChecksEveryPlayerUnitButExcludesEnemyOnlyDefinitions()
        {
            var catalog = Catalog(new[] { Acquire() }, Options.Take(3).Select(t => Definition(t)).ToArray());
            Assert.DoesNotThrow(() => TraitGrowthCatalogValidator.Validate(catalog,
                new[] { Profile(eligibility: UnitOwnerEligibility.EnemyOnly, native: new[] { Options[0] }) }));
            Assert.Throws<InvalidOperationException>(() => TraitGrowthCatalogValidator.Validate(catalog,
                new[] { Profile(), Profile(native: Options.Take(3).ToArray()) }));
            Assert.Throws<InvalidOperationException>(() => TraitGrowthCatalogValidator.Validate(TraitGrowthCatalog.Disabled,
                new[] { Profile(CategoryKey.Main, eligibility: UnitOwnerEligibility.Both) }));
        }

        [Test]
        public void ManySteps_HaveNoFixedTraitCountAndEnhancementCannotRepeat()
        {
            var definitions = Options.Select(t => Definition(t, t)).ToArray();
            var steps = new[] { Acquire(), Acquire(2), Acquire(3), Acquire(4), Enhance(5), Enhance(6) };
            Assert.DoesNotThrow(() => TraitGrowthCatalogValidator.Validate(Catalog(steps, definitions), new[] { Profile() }));
            var insufficient = Catalog(new[] { Enhance(1), Enhance(2) }, new[] { Definition(Options[0], Options[0]) });
            Assert.Throws<InvalidOperationException>(() => TraitGrowthCatalogValidator.Validate(insufficient,
                new[] { Profile(native: new[] { Options[0] }) }));
        }

        private static IReadOnlyList<TraitKey> Eligible(CombatAttackProfile profile, TraitGrowthCatalog catalog,
            TraitGrowthKind kind = TraitGrowthKind.Acquire, TraitKey[] owned = null, TraitKey[] enhanced = null) =>
            TraitGrowthRules.EligibleTraits(profile, owned ?? Array.Empty<TraitKey>(), enhanced ?? Array.Empty<TraitKey>(), kind,
                catalog.GetCategory(profile.Definition.CategoryKey), catalog);

        [TestCase(false, TargetingSplash.None, 2)]
        [TestCase(true, TargetingSplash.None, 2)]
        [TestCase(false, TargetingSplash.Cross, 3)]
        [TestCase(true, TargetingSplash.Radial, 3)]
        public void Eligibility_DistanceDamageCoverageIncludesNormalEnhancedAndSplash(bool enhance, TargetingSplash splash, int required)
        {
            var method = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, splash,
                splash == TargetingSplash.None ? 0 : 100);
            var normal = new DistanceDamageScaleTraitSpec(Enumerable.Repeat(100, required).ToArray());
            var shortForm = new DistanceDamageScaleTraitSpec(Enumerable.Repeat(100, required - 1).ToArray());
            var profile = Profile(native: enhance ? new TraitSpec[] { normal } : new TraitSpec[]
                { new KnockbackTraitSpec(3), new TrackingShotTraitSpec(50, 20) }, method: method);
            var step = enhance ? Enhance() : Acquire();
            var definitions = new[]
            {
                Definition(new KnockbackTraitSpec(3)), Definition(new TrackingShotTraitSpec(50, 20)),
                enhance ? Definition(normal, shortForm) : Definition(shortForm)
            };
            var catalog = Catalog(new[] { step }, definitions);
            Assert.That(Eligible(profile, catalog, step.Kind), Has.No.Member(TraitKey.DistanceDamageScale));
            Assert.Throws<InvalidOperationException>(() => TraitGrowthCatalogValidator.Validate(catalog, new[] { profile }));
            definitions[2] = enhance ? Definition(normal, normal) : Definition(normal);
            catalog = Catalog(new[] { step }, definitions);
            Assert.That(Eligible(profile, catalog, step.Kind), Has.Member(TraitKey.DistanceDamageScale));
            Assert.DoesNotThrow(() => TraitGrowthCatalogValidator.Validate(catalog, new[] { profile }));
        }

        [TestCase(TraitKey.PiercingFalloff)]
        [TestCase(TraitKey.KillChainAttack)]
        public void Eligibility_DistanceScalingNeedsDeliveryBoundsInEitherSelectionOrder(TraitKey delivery)
        {
            TraitSpec unbounded = delivery switch
            {
                TraitKey.PiercingFalloff => new PiercingFalloffTraitSpec(50, 3),
                TraitKey.KillChainAttack => new KillChainAttackTraitSpec(),
                _ => throw new ArgumentOutOfRangeException(nameof(delivery))
            };
            var scale = new DistanceDamageScaleTraitSpec(new[] { 100, 100 });
            var catalog = Catalog(Array.Empty<TraitGrowthStepSpec>(), new[] { Definition(unbounded), Definition(scale) });
            Assert.That(Eligible(Profile(native: new[] { unbounded }), catalog), Has.No.Member(TraitKey.DistanceDamageScale));
            Assert.That(Eligible(Profile(native: new TraitSpec[] { scale }), catalog), Has.No.Member(delivery));
        }

    }
}
