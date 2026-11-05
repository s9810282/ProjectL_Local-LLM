using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class TraitStateContractTests
    {
        [Test]
        public void CopiesAndEquality_PreserveTraitFacts()
        {
            object effects = Effects(3, out Array source);
            var original = WithTraits(Facts(), effects);
            source.SetValue(null, 0);
            var copy = original.WithHealth(new HealthStateSnapshot(9, 10))
                .WithPlacement(CharacterRuntimeArea.Field, null, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero });
            var grants = (IList)Get(Get(copy, "TraitEffects"), "AttackGrants");
            Assert.That(grants.Count, Is.EqualTo(1));
            Assert.That(Get(grants[0], "Amount"), Is.EqualTo(3));
            Assert.Throws<NotSupportedException>(() => grants[0] = null);
            Assert.That(original.SameValues(WithTraits(Facts(), Effects(3, out _))), Is.True);
            Assert.That(original.SameValues(WithTraits(Facts(), Effects(4, out _))), Is.False);
            Assert.That(original.SameIdentity(Facts()), Is.True);
            Assert.That(original.Stats.With().SameValues(original.Stats), Is.True);
        }

        [Test]
        public void SharedDefinition_HasIndependentReceivedState()
        {
            var first = WithTraits(Facts(), Effects(3, out _));
            var second = Facts(2);
            Assert.That(((IList)Get(Get(first, "TraitEffects"), "AttackGrants")).Count, Is.EqualTo(1));
            Assert.That(((IList)Get(Get(second, "TraitEffects"), "AttackGrants")).Count, Is.Zero);
            Assert.That(second.Stats.SameValues(first.Stats), Is.True);
        }

        [Test]
        public void TraitProjection_DoesNotRewriteBaseAttackOrArtifactBonus()
        {
            var character = WithTraits(Facts(), Effects(3, out _));
            var projected = Project(character, 2);
            Assert.That(projected.Stats.AttackPower, Is.EqualTo(10));
            Assert.That(projected.Stats.ArtifactAttackBonus, Is.Zero);
            Assert.That(projected.Stats.EffectiveAttackPower, Is.EqualTo(15));
            Assert.That(Get(projected.Stats, "TraitAttackBonus"), Is.EqualTo(5));
            Assert.That(Project(projected, 2).Stats.EffectiveAttackPower, Is.EqualTo(15));
            var calculator = new SoulEffectCalculator(new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()));
            Assert.That(calculator.EvaluateAttackPower(projected, 0, new[] { projected }), Is.EqualTo(15));
            Assert.That(projected.With(stats: projected.Stats.With(attackPower: 11)).Stats.EffectiveAttackPower, Is.EqualTo(16));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OwnedRebuilds_PreserveHunterBonus(bool main)
        {
            var profile = TraitGrowthFixtures.Profile(main ? CategoryKey.Main : CategoryKey.Heavy);
            var basis = TraitGrowthFixtures.Owned(profile);
            var owned = Owned(basis, 2);
            Assert.That(owned.SameValues(basis), Is.False);
            var progression = new LevelProgressionSpec(150, 100, 3, 1, 1);
            var grown = ExperienceRules.Gain(owned, 950, progression).Final;
            Assert.That(Get(grown, "HunterAttackBonus"), Is.EqualTo(2));
            var catalog = TraitGrowthFixtures.Catalog(category: profile.Definition.CategoryKey);
            var step = catalog.GetCategory(profile.Definition.CategoryKey).Steps[0];
            var offer = TraitGrowthFixtures.Offer(grown, step, TraitKey.Knockback, TraitKey.SoloAction, TraitKey.TrackingShot);
            var selected = TraitGrowthRules.PlanChoice(grown, TraitGrowthFixtures.Request(offer, TraitKey.Knockback), offer, profile, catalog).Change.Final;
            Assert.That(Get(selected, "HunterAttackBonus"), Is.EqualTo(2));
            Assert.That(selected.Level, Is.EqualTo(grown.Level));
            Assert.That(selected.Experience, Is.EqualTo(grown.Experience));
            var updated = Owned(selected, 4);
            Assert.DoesNotThrow(() => new OwnedCharacterChange(selected, updated));
            Assert.Throws<ArgumentException>(() => new OwnedCharacterChange(updated, selected));
            var state = main ? new MainCharacterPersistentState(10, 10, 10) : null;
            var souls = new Dictionary<CharacterInstanceId, int> { [owned.Id] = 0 };
            int Read(OwnedCharacterRecord record) => OwnedCharacterStatsCalculator.Calculate(new[] { profile },
                UnitSpecTestFactory.CreateGrowthCategoryCatalog(), new[] { record }, state, souls)[0].Stats.AttackPower;
            Assert.That(Read(owned), Is.EqualTo(Read(basis) + 2));
            var registry = TraitRegistry.CreateDefault();
            var definition = new UnitCatalog(new[] { profile.Definition }, registry,
                new CategoryRegistry(UnitSpecTestFactory.CreateGrowthCategoryCatalog())).GetRequired(owned.CharacterKey);
            var prepared = BattleCharacterCalculator.CreateOwned(new EntityId(1), definition, owned, state, 0,
                null, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }, registry);
            Assert.That(prepared.Stats.EffectiveAttackPower, Is.EqualTo(Read(owned)));
            Assert.That(prepared.Stats.TraitAttackBonus, Is.EqualTo(2));
            if (main) Assert.That(prepared.MatchesMainState(state), Is.True);
            Assert.That(Get(new OwnedCharacterRecord(owned.Id, owned.CharacterKey, owned.Role), "HunterAttackBonus"), Is.Zero);
        }

        [Test]
        public void TraitTurn_CopyAndEqualityPreserveBothSides()
        {
            var state = new CombatCommandState(4,4,CombatFlowPhase.PlayerInput,false,                Array.Empty<StructureCombatFacts>(),Array.Empty<ThornFieldCellSnapshot>());
            var factsType = ContractType("TraitTurnFacts");
            object turns = Activator.CreateInstance(factsType, 2, 3);
            var changed = (CombatCommandState)With(state, "traitTurn", turns);
            Assert.That(changed.SameValues(state), Is.False);
            Assert.That(Get(Get(changed.With(), "TraitTurn"), "PlayerAttackActionCount"), Is.EqualTo(2));
            Assert.That(Get(Get(changed.With(), "TraitTurn"), "EnemyAttackActionCount"), Is.EqualTo(3));
            Assert.That(changed.With().SameValues(changed), Is.True);
        }

        [Test]
        public void Cleanup_RemovesReceivedGrantsAndPreservesOwnedContribution()
        {
            var character = Project(WithTraits(Facts(), Effects(3, out _)), 2);
            var cleared = CombatTemporaryStateCleanup.Clear(character);
            Assert.That(cleared.TraitEffects.AttackGrants, Is.Empty);
            Assert.That(cleared.Stats.AttackPower, Is.EqualTo(10));
            Assert.That(cleared.Stats.TraitAttackBonus, Is.EqualTo(2));
            Assert.That(cleared.Stats.EffectiveAttackPower, Is.EqualTo(12));
        }

        [Test]
        public void TraitTurn_StartResetsOnlyTheStartingSide()
        {
            var facts = new TraitTurnFacts(2, 3);
            Assert.That(facts.BeginTurn(Owner.Player).SameValues(new TraitTurnFacts(0, 3)), Is.True);
            Assert.That(facts.BeginTurn(Owner.Enemy).SameValues(new TraitTurnFacts(2, 0)), Is.True);
        }

        [Test]
        public void GrantSourceAndLifetime_AreNotOnlyComparedByTotal()
        {
            var origin = new EffectOriginKey(EffectOwnerKind.Trait, 1, 220, "fixture");
            var otherOrigin = new EffectOriginKey(EffectOwnerKind.Trait, 2, 220, "fixture");
            var grant = new TraitAttackGrant(origin, 3, 1);
            Assert.That(grant.SameValues(new TraitAttackGrant(otherOrigin, 3, 1)), Is.False);
            Assert.That(grant.SameValues(new TraitAttackGrant(origin, 3, 2)), Is.False);
            Assert.Throws<ArgumentException>(() => new TraitAttackGrant(default, 3, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TraitAttackGrant(origin, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TraitAttackGrant(origin, 3, 0));
        }

        private static CharacterCombatFacts Facts(int id = 1) => new(new EntityId(id), new CharacterInstanceId(id),
            EntityKey.Unit_Shielder, Owner.Player, CharacterOwnershipRole.Sub, CharacterRuntimeArea.Field,
            new HealthStateSnapshot(10, 10), new UnitStatsSnapshot(MoveMethod.LiftAndPlace,2,2,10,1,3,                Array.Empty<TraitStatusSnapshot>(),CategorySnapshot.CreateStateless(CategoryKey.Heavy)),
            null, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero });

        private static object Effects(int amount, out Array source)
        {
            Type grant = ContractType("TraitAttackGrant");
            source = Array.CreateInstance(grant, 1);
            var origin = new EffectOriginKey(EffectOwnerKind.Trait, 1, 220, "fixture");
            source.SetValue(Activator.CreateInstance(grant, origin, amount, 1), 0);
            return Activator.CreateInstance(ContractType("CharacterTraitEffects"), new object[] { source });
        }

        private static Type ContractType(string name)
        {
            Type type = typeof(CharacterCombatFacts).Assembly.GetType(typeof(CharacterCombatFacts).Namespace + "." + name);
            Assert.That(type, Is.Not.Null, "The immutable contract is required: " + name);
            return type;
        }

        private static CharacterCombatFacts WithTraits(CharacterCombatFacts facts, object effects) =>
            (CharacterCombatFacts)With(facts, "traitEffects", effects);

        private static object With(object value, string parameter, object argument)
        {
            var method = value.GetType().GetMethod("With");
            var parameters = method.GetParameters();
            Assert.That(parameters.Any(p => p.Name == parameter), Is.True, parameter);
            return method.Invoke(value, parameters.Select(p => p.Name == parameter ? argument : p.DefaultValue).ToArray());
        }

        private static object Get(object value, string property)
        {
            var info = value.GetType().GetProperty(property);
            Assert.That(info, Is.Not.Null, property);
            return info.GetValue(value);
        }

        private static CharacterCombatFacts Project(CharacterCombatFacts character, int hunter)
        {
            var method = typeof(CombatEffectiveStatsCalculator).GetMethod("ProjectTraitAttack");
            Assert.That(method, Is.Not.Null);
            return (CharacterCombatFacts)method.Invoke(null, new object[] { character, hunter });
        }

        private static OwnedCharacterRecord Owned(OwnedCharacterRecord original, int hunter)
        {
            var constructor = typeof(OwnedCharacterRecord).GetConstructors().Single();
            Assert.That(constructor.GetParameters().Any(p => p.Name == "hunterAttackBonus"), Is.True);
            return (OwnedCharacterRecord)constructor.Invoke(new object[] { original.Id, original.CharacterKey, original.Role,
                original.Level, original.Experience, original.Growth, hunter, original.SpecializationCategory });
        }
    }
}
