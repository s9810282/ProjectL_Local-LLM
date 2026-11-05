using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;

namespace ProjectL.Tests.Core
{
    public sealed class MainSpecializationCombatTests
    {
        [TestCase(CategoryKey.Rider, 6)]
        [TestCase(CategoryKey.Heavy, 1)]
        [TestCase(CategoryKey.Cannon, 7)]
        public void EmptyGrowthStillAppliesClassWithoutChangingNativeSpec(CategoryKey category, int distance)
        {
            var basis = Spec();
            var owned = Owned(category);
            var resolved = OwnedUnitSpecResolver.Resolve(basis, owned, TraitGrowthCatalog.Disabled);
            Assert.That(resolved.CategoryKey, Is.EqualTo(CategoryKey.Main));
            Assert.That(resolved.SpecializationCategory, Is.EqualTo(category));
            Assert.That(resolved.CombatCategoryKey, Is.EqualTo(category));
            Assert.That(new UnitCatalog(new[] { resolved }, TraitRegistry.CreateDefault(), new CategoryRegistry(Categories()))
                .GetRequired(resolved.Key).MoveRange, Is.EqualTo(distance));
            Assert.That(resolved.Traits.ToArray(), Is.EqualTo(basis.Traits.ToArray()));
            Assert.That((resolved.AttackRange, resolved.AttackPower, resolved.MaxHealth), Is.EqualTo((4, 5, 10)));
            Assert.That(basis.SpecializationCategory, Is.Null);
            Assert.That(OwnedUnitSpecResolver.Resolve(basis, Owned(null), TraitGrowthCatalog.Disabled), Is.SameAs(basis));
        }

        [TestCase(CategoryKey.Rider, 6)]
        [TestCase(CategoryKey.Heavy, 1)]
        [TestCase(CategoryKey.Cannon, 7)]
        public void OwnedReadKeepsMainPersistentStatsAndUsesConfiguredClass(CategoryKey category, int distance)
        {
            var owned = Owned(category);
            var row = OwnedCharacterStatsCalculator.Calculate(UnitSpecTestFactory.Profiles(Spec()), Categories(), new[] { owned },
                new MainCharacterPersistentState(7, 13, 11, 5), new Dictionary<CharacterInstanceId, int> { [owned.Id] = 0 }).Single();
            Assert.That((row.Stats.CurrentHealth, row.Stats.MaxHealth, row.Stats.AttackPower, row.Stats.PhysicalDefense),
                Is.EqualTo((7, 13, 20, 5)));
            Assert.That(row.Stats.MoveRange, Is.EqualTo(distance));
            Assert.That(row.Stats.CategoryKey, Is.EqualTo(CategoryKey.Main));
            Assert.That(Read(row.Stats, "SpecializationCategory"), Is.EqualTo(category));
            Assert.That(Read(row.Stats, "CombatCategoryKey"), Is.EqualTo(category));
            Assert.That(row.Role, Is.EqualTo(CharacterOwnershipRole.Main));
            Assert.That(row.Stats.TraitKeys.ToArray(), Is.EqualTo(new[] { TraitKey.Knockback }));
        }

        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void BattleCategoryAndSnapshotCopiesRetainMainIdentity(CategoryKey category)
        {
            var facts = Battle(category);
            Assert.That(facts.Stats.Category.Key, Is.EqualTo(category));
            Assert.That(Read(facts.Stats, "IdentityCategoryKey"), Is.EqualTo(CategoryKey.Main));
            Assert.That(Read(facts.Stats, "SpecializationCategory"), Is.EqualTo(category));
            Assert.That(facts.Stats.SameValues(facts.Stats.With()), Is.True);
            Assert.That(Read(facts.Stats.With(attackPower: 12), "SpecializationCategory"), Is.EqualTo(category));
            var ordinary = new UnitStatsSnapshot(facts.Stats.MoveMethod, facts.Stats.MoveRange, facts.Stats.AttackRange,
                facts.Stats.AttackPower, facts.Stats.PhysicalDefense, facts.Stats.Priority, facts.Stats.TraitStatuses.ToArray(),
                facts.Stats.Category, traitAttackBonus: facts.Stats.TraitAttackBonus);
            Assert.That(facts.Stats.SameValues(ordinary), Is.False, "같은 전투 병과라도 Main 전직 여부는 다르다.");
        }

        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void CaptureUsesEffectiveCategoryAndExistingCannonRule(CategoryKey category)
        {
            var facts = Battle(category);
            var profile = new CombatAttackProfile(Spec(category),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0), facts.OwnedId);
            Assert.That(facts.Stats.Category.Key, Is.EqualTo(category));
            var bindings = CombatCommandEffects.Capture(facts, profile);
            Assert.That(bindings.Count(b => b.Rule is SafeBombardmentCategoryRule), Is.EqualTo(category == CategoryKey.Cannon ? 1 : 0));
            Assert.Throws<InvalidOperationException>(() => CombatCommandEffects.Capture(facts,
                new CombatAttackProfile(Spec(), profile.Method, facts.OwnedId)));
        }

        [Test]
        public void MovementContextRejectsChangedSpecializationProfileAndIdentity()
        {
            var facts = Battle(CategoryKey.Rider);
            var basis = Spec(CategoryKey.Rider);
            var changed = new UnitSpec(basis.Key, basis.Role, 1, 1, basis.FootprintLocalCells.ToArray(), basis.MaxHealth,
                basis.MoveMethod, basis.AttackRange, basis.CategoryKey, basis.AttackPower, basis.PhysicalDefense, basis.Priority,
                basis.Traits.ToArray(), basis.OwnerEligibility, basis.Occlusion, basis.Behavior, basis.KillReward, CategoryKey.Heavy);
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var admission = CombatCommandTestData.Input().State;
            var path = new MovementPath(new[] { CellCoord.Zero, new CellCoord(1, 0) });
            MovementPreviewContext Context(CharacterCombatFacts actor, UnitSpec definition) => new(1, Owner.Player, actor,
                CombatCommandKind.Move, path, admission: admission,
                ownedProfiles: new[] { new CombatAttackProfile(definition, method, facts.OwnedId) });
            var original = Context(facts, basis);
            Assert.That(MovementContextCalculator.SameValues(original, Context(facts, basis)), Is.True);
            Assert.That(MovementContextCalculator.SameValues(original, Context(facts, changed)), Is.False);
            var s = facts.Stats;
            var lost = facts.With(stats: new UnitStatsSnapshot(s.MoveMethod, s.MoveRange, s.AttackRange, s.AttackPower,
                s.PhysicalDefense, s.Priority, s.TraitStatuses.ToArray(), s.Category, traitAttackBonus: s.TraitAttackBonus));
            Assert.That(MovementContextCalculator.SameValues(original, Context(lost, basis)), Is.False);
        }

        [TestCase(CategoryKey.Rider, 7, 15)]
        [TestCase(CategoryKey.Heavy, 10, 10)]
        [TestCase(CategoryKey.Cannon, 11, 9)]
        public void ActualDamagePipelineUsesSelectedAttackerAndDefenderCategory(CategoryKey category, int outgoing, int incoming)
        {
            var original = CombatCommandTestData.Input();
            var basis = Battle(category);
            var main = new CharacterCombatFacts(basis.EntityId, basis.OwnedId, basis.CharacterKey, Owner.Player,
                CharacterOwnershipRole.Main, CharacterRuntimeArea.Field, new HealthStateSnapshot(100, 100),
                basis.Stats.With(attackPower: 10, physicalDefense: 0, traitAttackBonus: 0), null, CellCoord.Zero, Rotation4.R0,
                new[] { CellCoord.Zero }, UnitActionUse.Available);
            var enemy = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(1, 0), Owner.Enemy, 10);
            enemy = enemy.With(stats: enemy.Stats.With(category: CategorySnapshot.CreateStateless(CategoryKey.Heavy)));
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var enemySpec = new UnitSpec(EntityKey.Unit_Demon, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 100,
                MoveMethod.LiftAndPlace, 4, CategoryKey.Heavy, 10, 0, 1, Array.Empty<TraitSpec>(), UnitOwnerEligibility.EnemyOnly);
            var input = new CombatCommandInput(1, CombatCommandKind.Attack, main.EntityId, AttackAim.ForCell(enemy.Anchor.Value),
                original.State, new[] { main, enemy }, new[] { new CombatAttackProfile(Spec(category), method, main.OwnedId),
                    new CombatAttackProfile(enemySpec, method) }, new MainCharacterPersistentState(100, 100, 10),
                new[] { new OwnedCharacterRecord(main.OwnedId.Value, main.CharacterKey, CharacterOwnershipRole.Main, specializationCategory: category) },
                new Dictionary<CharacterInstanceId, int> { [main.OwnedId.Value] = 0 }, SoulFieldSnapshot.Empty, original.Artifacts);
            var pipeline = new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault()));
            var engine = new CombatEffectEngine();
            Assert.That(CombatDamageCalculator.Attack(input, engine, pipeline, main, enemy, 10, AttackTargetRole.Primary, 100,
                main.Anchor, null).FinalDamage, Is.EqualTo(outgoing));
            Assert.That(CombatDamageCalculator.Attack(input, engine, pipeline, enemy, main, 10, AttackTargetRole.Primary, 100,
                enemy.Anchor, null).FinalDamage, Is.EqualTo(incoming));
        }

        private static object Read(object value, string name)
        {
            var property = value.GetType().GetProperty(name);
            Assert.That(property, Is.Not.Null, name);
            return property.GetValue(value);
        }

        private static CharacterCombatFacts Battle(CategoryKey category)
        {
            var registry = TraitRegistry.CreateDefault();
            var definition = new UnitCatalog(new[] { Spec(category) }, registry, new CategoryRegistry(Categories())).GetRequired(EntityKey.Unit_Akimbo);
            return BattleCharacterCalculator.CreateOwned(new EntityId(1), definition, Owned(category),
                new MainCharacterPersistentState(7, 13, 11, 5), 0, 0, null, null, Array.Empty<CellCoord>(), registry);
        }

        private static OwnedCharacterRecord Owned(CategoryKey? category) => new(new CharacterInstanceId(1), EntityKey.Unit_Akimbo,
            CharacterOwnershipRole.Main, 8, 7, hunterAttackBonus: 9, specializationCategory: category);

        private static UnitSpec Spec(CategoryKey? category = null) => new(EntityKey.Unit_Akimbo, UnitRole.MainCharacter,
            1, 1, new[] { CellCoord.Zero }, 10, MoveMethod.LiftAndPlace, 4, CategoryKey.Main, 5, 1, 1,
            new TraitSpec[] { new KnockbackTraitSpec(1) }, UnitOwnerEligibility.PlayerOnly, specializationCategory: category);

        private static CategorySpec[] Categories() => new[]
        {
            new CategorySpec(CategoryKey.Main, Array.Empty<CategoryRuleSpec>(), 3),
            new CategorySpec(CategoryKey.Rider, Array.Empty<CategoryRuleSpec>(), 6, new CategoryLevelGrowth(21, 34)),
            new CategorySpec(CategoryKey.Heavy, Array.Empty<CategoryRuleSpec>(), 1),
            new CategorySpec(CategoryKey.Cannon, new CategoryRuleSpec[] { new SafeBombardmentCategoryRuleSpec() }, 7)
        };
    }
}
