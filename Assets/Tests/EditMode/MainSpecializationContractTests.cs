using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class MainSpecializationContractTests
    {
        [TestCase(EntityKey.Unit_Cowboy, CategoryKey.Rider)]
        [TestCase(EntityKey.Unit_Cowboy, CategoryKey.Heavy)]
        [TestCase(EntityKey.Unit_Cowboy, CategoryKey.Cannon)]
        [TestCase(EntityKey.Unit_Shielder, CategoryKey.Rider)]
        [TestCase(EntityKey.Unit_Shielder, CategoryKey.Heavy)]
        [TestCase(EntityKey.Unit_Shielder, CategoryKey.Cannon)]
        public void MainAcceptsEachSpecialization(EntityKey key, CategoryKey category)
        {
            var expected = new OwnedCharacterRecord(new CharacterInstanceId(1), key, CharacterOwnershipRole.Main, 3, 7);
            var decision = MainSpecializationRules.Plan(expected, category);
            Assert.That(decision.Succeeded, Is.True);
            Assert.That(decision.FailureReason, Is.EqualTo(MainSpecializationFailureReason.None));
            Assert.That(decision.Change.Expected, Is.SameAs(expected));
            var final = decision.Change.Final;
            Assert.That(final.SpecializationCategory, Is.EqualTo(category));
            Assert.That((final.Id, final.CharacterKey, final.Role, final.Level, final.Experience),
                Is.EqualTo((expected.Id, key, CharacterOwnershipRole.Main, 3, 7)));
        }

        [TestCase(CategoryKey.None)]
        [TestCase(CategoryKey.Main)]
        [TestCase(CategoryKey.Boss)]
        [TestCase((CategoryKey)999)]
        public void SubAndInvalidClassesReject(CategoryKey category)
        {
            AssertFailure(MainSpecializationRules.Plan(Main(), category), MainSpecializationFailureReason.InvalidCategory);
            var sub = new OwnedCharacterRecord(new CharacterInstanceId(2), EntityKey.Unit_Cowboy, CharacterOwnershipRole.Sub);
            foreach (var selected in new[] { CategoryKey.Rider, CategoryKey.Heavy, CategoryKey.Cannon })
                AssertFailure(MainSpecializationRules.Plan(sub, selected), MainSpecializationFailureReason.InvalidTarget);
            Assert.Throws<ArgumentException>(() => Main(category: category));
            Assert.Throws<ArgumentException>(() => new OwnedCharacterRecord(sub.Id, sub.CharacterKey, sub.Role,
                specializationCategory: CategoryKey.Rider));
        }

        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void RepeatedChoiceDoesNotChangeRecord(CategoryKey category)
        {
            var expected = Main(category: category);
            foreach (var selected in new[] { CategoryKey.Rider, CategoryKey.Heavy, CategoryKey.Cannon })
                AssertFailure(MainSpecializationRules.Plan(expected, selected), MainSpecializationFailureReason.AlreadySpecialized);
            Assert.That(expected.SpecializationCategory, Is.EqualTo(category));
        }

        [Test]
        public void ChangePreservesIdentityGrowthXpAndHunter()
        {
            var expected = Main();
            var final = MainSpecializationRules.Plan(expected, CategoryKey.Cannon).Change.Final;
            Assert.That((final.Id, final.CharacterKey, final.Role, final.Level, final.Experience, final.HunterAttackBonus),
                Is.EqualTo((new CharacterInstanceId(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main, 8, 7, 9)));
            Assert.That(final.Growth.SameValues(expected.Growth), Is.True);
            Assert.That(expected.SpecializationCategory, Is.Null);
            foreach (var invalid in new[] { Main(category: CategoryKey.Cannon, id: 2),
                Main(category: CategoryKey.Cannon, key: EntityKey.Unit_Cowboy), Main(category: CategoryKey.Cannon, level: 9),
                Main(category: CategoryKey.Cannon, experience: 8), Main(category: CategoryKey.Cannon, hunter: 10),
                Main(category: CategoryKey.Cannon, growth: OwnedTraitGrowth.Empty), Main() })
                Assert.Throws<ArgumentException>(() => new MainSpecializationChange(expected, invalid));
            Assert.Throws<ArgumentException>(() => new MainSpecializationChange(final, Main(category: CategoryKey.Rider)));
            var sub = new OwnedCharacterRecord(expected.Id, expected.CharacterKey, CharacterOwnershipRole.Sub);
            Assert.Throws<ArgumentException>(() => new MainSpecializationChange(sub, final));
        }

        [Test]
        public void GenericChangesCannotGrantOrEraseSpecialization()
        {
            foreach (var pair in new[] { (Main(), Main(category: CategoryKey.Rider)),
                (Main(category: CategoryKey.Rider), Main()),
                (Main(category: CategoryKey.Rider), Main(category: CategoryKey.Cannon)) })
            {
                Assert.Throws<ArgumentException>(() => new OwnedCharacterChange(pair.Item1, pair.Item2));
                var withGrowth = Main(category: pair.Item2.SpecializationCategory,
                    growth: pair.Item1.Growth.Append(new TraitGrowthSelection(30, TraitKey.Knockback)));
                Assert.Throws<ArgumentException>(() => new TraitGrowthChange(pair.Item1, withGrowth));
            }
        }

        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void ExperienceGrowthAndHunterReconstructionPreservesChoice(CategoryKey category)
        {
            var expected = Main(category: category, growth: OwnedTraitGrowth.Empty);
            var gained = ExperienceRules.Gain(expected, 1200, new LevelProgressionSpec(150, 100, 3, 1, 1)).Final;
            Assert.That((gained.Level, gained.Experience, gained.SpecializationCategory), Is.EqualTo((9, 7, (CategoryKey?)category)));
            Assert.DoesNotThrow(() => new OwnedCharacterChange(expected, gained));
            var profile = TraitGrowthFixtures.Profile(CategoryKey.Main);
            var catalog = TraitGrowthFixtures.Catalog(category: CategoryKey.Main);
            var offer = TraitGrowthFixtures.Offer(gained, catalog.GetCategory(CategoryKey.Main).Steps[0],
                TraitKey.Knockback, TraitKey.SoloAction, TraitKey.HuntersMark);
            var grown = TraitGrowthRules.PlanChoice(gained, TraitGrowthFixtures.Request(offer, TraitKey.Knockback),
                offer, profile, catalog);
            Assert.That(grown.Succeeded, Is.True);
            Assert.That(grown.Change.Final.SpecializationCategory, Is.EqualTo(category));
            Assert.That(grown.Change.Final.Growth.Selections.Single().TraitKey, Is.EqualTo(TraitKey.Knockback));
            var awarded = new HunterTraitRule().Award(grown.Change.Final, 4);
            Assert.That((awarded.HunterAttackBonus, awarded.SpecializationCategory), Is.EqualTo((13, (CategoryKey?)category)));
            Assert.DoesNotThrow(() => new OwnedCharacterChange(grown.Change.Final, awarded));
        }

        [Test]
        public void SnapshotCopyAndEqualityIncludeSpecialization()
        {
            var source = new[] { Main(category: CategoryKey.Heavy) };
            var snapshot = RosterAcquireResult.Success(source);
            source[0] = Main();
            Assert.That(snapshot.Acquired[0].SpecializationCategory, Is.EqualTo(CategoryKey.Heavy));
            Assert.That(snapshot.Acquired[0].SameValues(Main(category: CategoryKey.Heavy)), Is.True);
            Assert.That(snapshot.Acquired[0].SameValues(Main()), Is.False);
            Assert.That(snapshot.Acquired[0].SameValues(Main(category: CategoryKey.Cannon)), Is.False);
        }

        [Test]
        public void DecisionRequiresSuccessChangeAndValidFailure()
        {
            Assert.Throws<ArgumentNullException>(() => MainSpecializationRules.Plan(null, CategoryKey.Rider));
            Assert.Throws<ArgumentNullException>(() => MainSpecializationDecision.Success(null));
            Assert.Throws<ArgumentNullException>(() => new MainSpecializationChange(null, Main()));
            Assert.Throws<ArgumentNullException>(() => new MainSpecializationChange(Main(), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => MainSpecializationDecision.Failure(MainSpecializationFailureReason.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => MainSpecializationDecision.Failure((MainSpecializationFailureReason)999));
            foreach (MainSpecializationFailureReason reason in Enum.GetValues(typeof(MainSpecializationFailureReason)))
                if (reason != MainSpecializationFailureReason.None)
                    AssertFailure(MainSpecializationDecision.Failure(reason), reason);
        }

        private static void AssertFailure(MainSpecializationDecision decision, MainSpecializationFailureReason reason)
        {
            Assert.That(decision.Succeeded, Is.False);
            Assert.That(decision.Change, Is.Null);
            Assert.That(decision.FailureReason, Is.EqualTo(reason));
        }

        [TestCase(CategoryKey.Rider, 6)]
        [TestCase(CategoryKey.Heavy, 1)]
        [TestCase(CategoryKey.Cannon, 7)]
        [TestCase(CategoryKey.Cannon, 0)]
        public void SelectedClassUsesConfiguredMovement(CategoryKey category, int distance)
        {
            var spec = Spec(category);
            var registry = new CategoryRegistry(UnitSpecTestFactory.CreateDefaultCategoryCatalog().Select(c =>
                c.Key == category ? new CategorySpec(c.Key, c.RuleSpecs.ToArray(), distance) : c).ToArray());
            var definition = new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault(), registry).GetRequired(spec.Key);
            Assert.That((spec.CategoryKey, spec.SpecializationCategory, spec.CombatCategoryKey),
                Is.EqualTo((CategoryKey.Main, (CategoryKey?)category, category)));
            Assert.That(definition.CategoryKey, Is.EqualTo(CategoryKey.Main));
            Assert.That(definition.Category, Is.SameAs(registry.GetRequired(CategoryKey.Main)));
            Assert.That(definition.CombatCategory, Is.SameAs(registry.GetRequired(category)));
            Assert.That(definition.CombatCategoryKey, Is.EqualTo(category));
            Assert.That(definition.MoveRange, Is.EqualTo(distance));
            Assert.That((definition.MaxHealth, definition.AttackPower, definition.PhysicalDefense, definition.AttackRange),
                Is.EqualTo((10, 5, 1, 2)));
            Assert.DoesNotThrow(() => PlayerMainDefinitionPolicy.Validate(definition.Key, definition.Role,
                definition.CategoryKey, definition.OwnerEligibility));
            var input = MovementTestData.WithMovement(MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(2, 1)),
                definition.MoveRange);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            Assert.That(MovementRouteValidator.StepBudget(actor, distance + 1L), Is.EqualTo(distance == 0
                ? MovementFailureReason.VoluntaryMoveDisabled : MovementFailureReason.MoveRangeExceeded));
            if (distance > 0)
                Assert.That(MovementRouteValidator.StepBudget(actor, distance), Is.EqualTo(MovementFailureReason.None));
        }

        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void EffectiveSpecCopiesPreserveChoice(CategoryKey category)
        {
            var spec = Spec(category);
            var owned = Main(category: category, key: spec.Key, growth: new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, TraitKey.SoloAction) }));
            var catalog = TraitGrowthFixtures.Catalog(category: CategoryKey.Main);
            var composed = OwnedUnitSpecResolver.Resolve(spec, owned, catalog);
            Assert.That(composed.SpecializationCategory, Is.EqualTo(category));
            Assert.That(composed.CategoryKey, Is.EqualTo(CategoryKey.Main));
            Assert.That(composed.Traits.ToArray().Single().TraitKey, Is.EqualTo(TraitKey.SoloAction));
        }

        [Test]
        public void LegacyConstructorsAndResolutionKeepIdentityCategory()
        {
            foreach (var category in new[] { CategoryKey.Main, CategoryKey.Rider, CategoryKey.Heavy, CategoryKey.Cannon, CategoryKey.Boss })
                Assert.That(MainSpecializationRules.ResolveCombatCategory(category, null), Is.EqualTo(category));
            var spec = TraitGrowthFixtures.Profile(CategoryKey.Main).Definition;
            var definition = new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault()).GetRequired(spec.Key);
            Assert.That(spec.SpecializationCategory, Is.Null);
            Assert.That(spec.CombatCategoryKey, Is.EqualTo(CategoryKey.Main));
            Assert.That(definition.Category, Is.SameAs(definition.CombatCategory));
            Assert.That(definition.MoveRange, Is.EqualTo(3));
            foreach (var category in new[] { CategoryKey.Rider, CategoryKey.Heavy, CategoryKey.Cannon })
                Assert.That(MainSpecializationRules.ResolveCombatCategory(CategoryKey.Main, category), Is.EqualTo(category));
        }

        [TestCase(CategoryKey.None)]
        [TestCase(CategoryKey.Main)]
        [TestCase(CategoryKey.Boss)]
        [TestCase((CategoryKey)999)]
        public void InvalidEffectiveCategoryRejects(CategoryKey category)
        {
            Assert.Throws<ArgumentException>(() => Spec(category));
            Assert.Throws<ArgumentException>(() => MainSpecializationRules.ResolveCombatCategory(CategoryKey.Main, category));
            Assert.Throws<ArgumentException>(() => MainSpecializationRules.ResolveCombatCategory(CategoryKey.Heavy, CategoryKey.Rider));
            Assert.Throws<ArgumentOutOfRangeException>(() => MainSpecializationRules.ResolveCombatCategory(CategoryKey.None, null));
        }

        [TestCase(UnitRole.Regular, CategoryKey.Main, UnitOwnerEligibility.PlayerOnly)]
        [TestCase(UnitRole.Regular, CategoryKey.Heavy, UnitOwnerEligibility.PlayerOnly)]
        [TestCase(UnitRole.Regular, CategoryKey.Main, UnitOwnerEligibility.EnemyOnly)]
        [TestCase(UnitRole.MainCharacter, CategoryKey.Main, UnitOwnerEligibility.EnemyOnly)]
        [TestCase(UnitRole.MainCharacter, CategoryKey.Main, UnitOwnerEligibility.Both)]
        [TestCase(UnitRole.MainCharacter, CategoryKey.Rider, UnitOwnerEligibility.PlayerOnly)]
        public void SpecializationCannotWeakenMainPolicy(UnitRole role, CategoryKey identity, UnitOwnerEligibility eligibility)
        {
            Assert.Throws<ArgumentException>(() => Spec(CategoryKey.Cannon, role, identity, eligibility));
        }

        [Test]
        public void CatalogStillRejectsInvalidMainIdentityAndRetiredMain()
        {
            var invalid = Spec(null, identity: CategoryKey.Rider);
            Assert.Throws<InvalidOperationException>(() => new UnitCatalog(new[] { invalid }, TraitRegistry.CreateDefault()));
            Assert.Throws<InvalidOperationException>(() => Spec(CategoryKey.Rider, key: EntityKey.Unit_MainCharacter));
        }

        private static UnitSpec Spec(CategoryKey? specialization, UnitRole role = UnitRole.MainCharacter,
            CategoryKey identity = CategoryKey.Main, UnitOwnerEligibility eligibility = UnitOwnerEligibility.PlayerOnly,
            EntityKey key = EntityKey.Unit_Cowboy) => new(key, role, 1, 1, new[] { CellCoord.Zero }, 10,
                MoveMethod.LiftAndPlace, 2, identity, 5, 1, 1, Array.Empty<TraitSpec>(),
                eligibility, specializationCategory: specialization);

        private static OwnedCharacterRecord Main(CategoryKey? category = null, int id = 1, EntityKey key = EntityKey.Unit_Akimbo,
            int level = 8, int experience = 7, int hunter = 9, OwnedTraitGrowth growth = null) =>
            new(new CharacterInstanceId(id), key, CharacterOwnershipRole.Main, level, experience,
                growth ?? new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, TraitKey.SoloAction) }), hunter, category);
    }
}
