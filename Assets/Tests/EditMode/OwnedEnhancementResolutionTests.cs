using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class OwnedEnhancementResolutionTests
    {
        private static UnitSpec Basis() => CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 10, CategoryKey.Main, new FocusTraitSpec(1, 3));
        private static OwnedCharacterRecord Owned(int id, bool selected = true, int step = 10) => new(new CharacterInstanceId(id),
            EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub, 2, 5, selected ? new OwnedTraitGrowth(new[] { new TraitGrowthSelection(step, TraitKey.Focus) }) : null);
        private static TraitGrowthCatalog Catalog() => TraitGrowthFixtures.Catalog(new[] { new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Enhance) },
            CategoryKey.Main, new[] { new TraitGrowthDefinition(new FocusTraitSpec(1, 3), new FocusTraitSpec(7, 3), "n", "d", "e", "ed") });

        [TestCase(true)]
        [TestCase(false)]
        public void History_ResolvesAcquisitionBeforeEnhancingAcquiredOrBaseTrait(bool enhanceAcquired)
        {
            var basis = Basis();
            var addition = new KnockbackTraitSpec(1);
            var definitions = new[] { new TraitGrowthDefinition(addition, new KnockbackTraitSpec(3), "n", "d", "e", "ed"),
                new TraitGrowthDefinition(new FocusTraitSpec(1, 3), new FocusTraitSpec(7, 3), "n", "d", "e", "ed") };
            var catalog = TraitGrowthFixtures.Catalog(new[] { new TraitGrowthStepSpec(20, 2, TraitGrowthKind.Acquire), new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Enhance) }, CategoryKey.Main, definitions);
            var growth = new OwnedTraitGrowth(new[] { new TraitGrowthSelection(20, TraitKey.Knockback), new TraitGrowthSelection(10, enhanceAcquired ? TraitKey.Knockback : TraitKey.Focus) });
            var owned = new OwnedCharacterRecord(new CharacterInstanceId(2), basis.Key, CharacterOwnershipRole.Sub, 2, 5, growth);
            var traits = OwnedUnitSpecResolver.Resolve(basis, owned, catalog).Traits.ToArray();
            Assert.That(traits.Length, Is.EqualTo(2));
            Assert.That(((FocusTraitSpec)traits.Single(t => t.TraitKey == TraitKey.Focus)).DamagePerStack, Is.EqualTo(enhanceAcquired ? 1 : 7));
            Assert.That(((KnockbackTraitSpec)traits.Single(t => t.TraitKey == TraitKey.Knockback)).DistanceCells, Is.EqualTo(enhanceAcquired ? 3 : 1));
            Assert.That(((FocusTraitSpec)basis.Traits.Span[0]).DamagePerStack, Is.EqualTo(1));
            Assert.That(addition.DistanceCells, Is.EqualTo(1));
        }
        [Test]
        public void OwnedThornDescription_UsesEachResolvedInstance()
        {
            var normal = new ThornFieldTraitSpec(5, 5);
            var enhanced = new ThornFieldTraitSpec(7, 5, 30, UnitTurnTickPhase.Started);
            var basis = CombatCommandTestData.Spec(EntityKey.Unit_CactusCannon, 10, CategoryKey.Cannon, normal);
            var catalog = TraitGrowthFixtures.Catalog(new[] { new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Enhance) }, CategoryKey.Cannon,
                new[] { new TraitGrowthDefinition(normal, enhanced, "n", "d", "e", "ed") });
            var selection = new TraitGrowthSelection(10, TraitKey.ThornField);
            var owned = new[] { new OwnedCharacterRecord(new CharacterInstanceId(1), basis.Key, CharacterOwnershipRole.Sub, 2),
                new OwnedCharacterRecord(new CharacterInstanceId(2), basis.Key, CharacterOwnershipRole.Sub, 2, growth: new OwnedTraitGrowth(new[] { selection })) };
            var result = OwnedCharacterStatsCalculator.Calculate(UnitSpecTestFactory.Profiles(basis), UnitSpecTestFactory.CreateDefaultCategoryCatalog(), owned, null, owned.ToDictionary(o => o.Id, _ => 0), default, catalog);
            var plain = result[0].ThornFieldDescription;
            var selected = result[1].ThornFieldDescription;
            Assert.That((plain.ThornDamage, plain.LifetimeUnitTurns, plain.DefenseReductionPercent, plain.TickPhase), Is.EqualTo((5, 5, 0, UnitTurnTickPhase.Ended)));
            Assert.That((selected.ThornDamage, selected.LifetimeUnitTurns, selected.DefenseReductionPercent, selected.TickPhase), Is.EqualTo((7, 5, 30, UnitTurnTickPhase.Started)));
            Assert.That(selected, Is.Not.SameAs(plain));
            Assert.That(owned[0].Growth.IsEmpty, Is.True);
            Assert.That(owned[1].Growth.Selections[0], Is.SameAs(selection));
            Assert.That(basis.Traits.Span[0], Is.SameAs(normal));
            Assert.That(catalog.GetTrait(TraitKey.ThornField).Enhanced, Is.SameAs(enhanced));
        }
        [Test]
        public void Resolution_RetainsSharedBasisAndRejectsMissingDefinitionsOrWrongPrefix()
        {
            var basis = Basis();
            var resolved = OwnedUnitSpecResolver.Resolve(basis, Owned(2), Catalog());
            Assert.That(resolved.CategoryKey, Is.EqualTo(CategoryKey.Main));
            Assert.That(((FocusTraitSpec)resolved.Traits.Span[0]).DamagePerStack, Is.EqualTo(7));
            Assert.That(((FocusTraitSpec)basis.Traits.Span[0]).DamagePerStack, Is.EqualTo(1));
            Assert.That(resolved.Behavior, Is.SameAs(basis.Behavior));
            Assert.That(resolved.KillReward, Is.SameAs(basis.KillReward));
            Assert.That(OwnedUnitSpecResolver.Resolve(basis, Owned(3, false), TraitGrowthCatalog.Disabled), Is.SameAs(basis));
            Assert.Throws<InvalidOperationException>(() => OwnedUnitSpecResolver.Resolve(basis, Owned(2), TraitGrowthCatalog.Disabled));
            Assert.Throws<InvalidOperationException>(() => OwnedUnitSpecResolver.Resolve(basis, Owned(2, step: 20), Catalog()));
        }
        [Test]
        public void Profiles_SeparateSameTypeInstancesAndRequireSelectedIdentity()
        {
            var input = CombatCommandTestData.Input();
            var owned = input.Owned.Select(o => o.CharacterKey == EntityKey.Unit_Akimbo ? Owned(o.Id.Value) : o).ToArray();
            var profiles = input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo ? new CombatAttackProfile(Basis(), p.Method) : p).ToArray();
            var bound = OwnedUnitSpecResolver.BuildProfiles(profiles, owned, Catalog());
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            CombatCommandInput Command(IReadOnlyList<CombatAttackProfile> ps) => new(input.Number, input.Kind, input.ActorId, input.Aim, input.State, input.Characters, ps,
                input.Main, owned, input.SoulCounts, input.Souls, input.Artifacts, expectedBattleInstanceId: input.ExpectedBattleInstanceId,
                expectedControllerTurnSequence: input.ExpectedControllerTurnSequence, battleInstanceId: input.BattleInstanceId);
            Assert.That(((FocusTraitSpec)CombatCommandCalculator.RequiredProfile(Command(bound), actor).Definition.Traits.Span[0]).DamagePerStack, Is.EqualTo(7));
            Assert.That(((FocusTraitSpec)bound.Single(p => p.Definition.Key == EntityKey.Unit_Akimbo && !p.OwnedId.HasValue).Definition.Traits.Span[0]).DamagePerStack, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => CombatCommandCalculator.RequiredProfile(Command(profiles), actor));
        }
        [Test]
        public void MovementContext_HandlesRebuiltDefinitionsAndRejectsChangedOwnedSelection()
        {
            var input = CombatCommandTestData.Input();
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var owned = input.Owned.Select(o => o.Id == actor.OwnedId ? Owned(o.Id.Value) : o).ToArray();
            var profiles = input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo ? new CombatAttackProfile(Basis(), p.Method) : p).ToArray();
            var characters = input.Characters.Select(c => c.EntityId == actor.EntityId ? CombatCommandTestData.With(c,
                traits: new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits.TraitStatusSnapshot[] {
                    new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits.FocusTraitStatusSnapshot(0, false, false) }) : c).ToArray();
            var catalog = Catalog();
            var path = new MovementPath(new[] { actor.Anchor.Value, new CellCoord(2, 3) });
            CombatCommandInput Move(bool selected) => new(1, CombatCommandKind.Move, input.ActorId, default, input.State, characters,
                OwnedUnitSpecResolver.BuildProfiles(profiles, selected ? owned : input.Owned, catalog), input.Main, selected ? owned : input.Owned,
                input.SoulCounts, input.Souls, input.Artifacts, path: path, battleInstanceId: 1, expectedBattleInstanceId: 1,
                expectedControllerTurnSequence: input.State.Turn.ControllerTurnSequence);
            var rules = Array.Empty<IMovementTileRule>();
            var first = MovementContextCalculator.Capture(Move(true), Owner.Player, rules);
            Assert.That(MovementContextCalculator.Matches(first, Move(true), Owner.Player, rules), Is.True);
            Assert.That(MovementContextCalculator.Matches(first, Move(false), Owner.Player, rules), Is.False);
        }
        [Test]
        public void PreparationAndOwnedStats_UseSamePerInstanceDefinition()
        {
            var basis = Basis();
            var mainSpec = new UnitSpec(EntityKey.Unit_Gravekeeper, UnitRole.MainCharacter, 1, 1, new[] { CellCoord.Zero }, 20,
                MoveMethod.LiftAndPlace, 1, CategoryKey.Main, 5, 0, 10, Array.Empty<TraitSpec>(), UnitOwnerEligibility.PlayerOnly);
            var owned = new[] { new OwnedCharacterRecord(new CharacterInstanceId(1), mainSpec.Key, CharacterOwnershipRole.Main), Owned(2), Owned(3, false) };
            var main = new MainCharacterPersistentState(20, 20, 5);
            var souls = owned.ToDictionary(o => o.Id, _ => 0);
            var categories = UnitSpecTestFactory.CreateDefaultCategoryCatalog();
            var init = new BattleInitialization(new StageConfig(8, 8, 8, 2, 2, 42, Array.Empty<InitialEnemyPlacementSpec>()), CellCoord.Zero,
                Rotation4.R0, new[] { mainSpec, basis }, Array.Empty<StructureSpec>(), categories).WithTraitGrowth(Catalog()).WithAttackProfiles(Array.Empty<CombatAttackProfile>());
            Assert.That(init.TraitGrowth, Is.Not.SameAs(TraitGrowthCatalog.Disabled));
            var prepared = BattlePreparationCalculator.Prepare(init, owned, main, souls, new InitialDrawPlan(new[] { owned[1].Id, owned[2].Id }), Array.Empty<CharacterCombatFacts>());
            var stats = OwnedCharacterStatsCalculator.Calculate(UnitSpecTestFactory.Profiles(init.Units), categories, owned, main, souls, default, init.TraitGrowth);
            foreach (var c in prepared.Characters)
            {
                var q = stats.Single(s => s.OwnedId == c.OwnedId).Stats;
                Assert.That((c.Stats.AttackPower, c.Health.MaxHealth), Is.EqualTo((q.AttackPower, q.MaxHealth)));
            }
            Assert.That(stats.Single(s => s.OwnedId == owned[1].Id).Stats.TraitKeys.ToArray(), Is.EqualTo(new[] { TraitKey.Focus }));
        }
    }
}
