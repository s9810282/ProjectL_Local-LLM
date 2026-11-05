using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class ThornHeartReflectionScopeTests
    {
        [TestCase(3, 10, 7, 3)]
        [TestCase(3, 2, 0, 2)]
        [TestCase(8, 10, 2, 8)]
        public void QualifyingHit_UsesConfiguredAmountAndCapsApplied(int amount, int hp, int finalHp, int applied)
        {
            var attacker = Attacker(hp);
            var ally = Ally();
            var result = Reflect(Scope(amount), attacker, ally);
            Assert.That(result.TargetChange.Expected, Is.SameAs(attacker));
            Assert.That(result.TargetChange.Final.Health.CurrentHealth, Is.EqualTo(finalHp));
            Assert.That(result.Activation.Activations[0].RequestedAmount, Is.EqualTo(amount));
            Assert.That(result.Activation.Activations[0].AppliedAmount, Is.EqualTo(applied));
            Assert.That(attacker.Health.CurrentHealth, Is.EqualTo(hp));
            Assert.That(ally.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.TargetChange.Final.Area,
                Is.EqualTo(finalHp == 0 ? CharacterRuntimeArea.Excluded : CharacterRuntimeArea.Field));
        }

        [Test]
        public void HitWithoutHpLoss_StillReflects()
        {
            var scope = Scope();
            var ally = Ally();
            Assert.That(scope.CanReflect(Attacker(), ally), Is.True);
            Assert.That(Reflect(scope, Attacker(), ally).TargetChange.Final.Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(ally.Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void LethalAllyHit_UsesCapturedPreDamageFacts()
        {
            var allyBefore = Ally();
            var deadAlly = CombatCommandCalculator.Hurt(allyBefore, 100);
            Assert.That(deadAlly.Health.IsDead, Is.True);
            var result = Reflect(Scope(), Attacker(), allyBefore);
            Assert.That(result.Activation.Reflection.StruckAllyId, Is.EqualTo(allyBefore.EntityId));
            Assert.That(result.DamageOrigin.PhysicalCell, Is.EqualTo(new CellCoord(3, 3)));
        }

        [Test]
        public void Scope_ReflectsOnlyOnceAcrossDifferentTargets()
        {
            var scope = Scope();
            Assert.That(Reflect(scope, Attacker(), Ally()), Is.Not.Null);
            Assert.That(Reflect(scope, Attacker(), Ally(8), 3), Is.Null);
            Assert.That(scope.CanReflect(Attacker(), Ally()), Is.False);
        }

        [Test]
        public void CanReflect_DoesNotConsumeOpportunity()
        {
            var scope = Scope();
            Assert.That(scope.CanReflect(Attacker(), Ally()), Is.True);
            Assert.That(scope.CanReflect(Attacker(), Ally()), Is.True);
            Assert.That(Reflect(scope, Attacker(), Ally()), Is.Not.Null);
        }

        [Test]
        public void NewAttackScope_HasItsOwnOpportunity()
        {
            Assert.That(Reflect(Scope(), Attacker(), Ally()), Is.Not.Null);
            Assert.That(Reflect(Scope(), Attacker(), Ally()), Is.Not.Null);
        }

        [Test]
        public void ConfiguredZero_ConsumesOpportunityWithoutPublishingChange()
        {
            var scope = Scope(0);
            Assert.That(scope.CanReflect(Attacker(), Ally()), Is.True);
            Assert.That(Reflect(scope, Attacker(), Ally()), Is.Null);
            Assert.That(scope.CanReflect(Attacker(), Ally()), Is.False);
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void DayOrNonOrdinaryExecution_DoesNotReflect(bool ordinary, bool night)
        {
            var scope = Scope(3, ordinary, night);
            Assert.That(scope.CanReflect(Attacker(), Ally()), Is.False);
            Assert.That(Reflect(scope, Attacker(), Ally()), Is.Null);
        }

        [Test]
        public void MissingHeart_DoesNotReflect()
        {
            Assert.That(Reflect(Scope(copies: 0), Attacker(), Ally()), Is.Null);
        }

        [Test]
        public void WrongFaction_DoesNotConsumeOpportunity()
        {
            var scope = Scope();
            Assert.That(Reflect(scope, Ally(9), Attacker()), Is.Null);
            Assert.That(Reflect(scope, Attacker(), CombatCommandTestData.Character(9, EntityKey.Unit_Demon,
                new CellCoord(2, 2), Owner.Enemy)), Is.Null);
            Assert.That(Reflect(scope, Attacker(), Ally()), Is.Not.Null);
        }

        [TestCase("dead-attacker")]
        [TestCase("absent-attacker")]
        [TestCase("dead-ally")]
        [TestCase("absent-ally")]
        public void NonFieldOrDeadFacts_DoNotQualify(string invalidCase)
        {
            var attacker = Attacker();
            var ally = Ally();
            if (invalidCase == "dead-attacker") attacker = CombatCommandCalculator.Hurt(attacker, 10);
            if (invalidCase == "absent-attacker") attacker = OutsideField(attacker);
            if (invalidCase == "dead-ally") ally = CombatCommandCalculator.Hurt(ally, 100);
            if (invalidCase == "absent-ally") ally = OutsideField(ally);
            Assert.That(Reflect(Scope(), attacker, ally), Is.Null);
        }

        [Test]
        public void DuplicateHeartRules_AreRejectedBeforeAnyHit()
        {
            Assert.Throws<ArgumentException>(() => Scope(copies: 2));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(1)]
        public void InvalidReflectionGroup_DoesNotConsumeOpportunity(long group)
        {
            var scope = Scope();
            Assert.Catch<ArgumentException>(() => Reflect(scope, Attacker(), Ally(), group));
            Assert.That(Reflect(scope, Attacker(), Ally()), Is.Not.Null);
        }

        [Test]
        public void Reflection_PreservesDefensesHeldStatePositionAndFootprint()
        {
            var original = Attacker();
            var stats = new UnitStatsSnapshot(MoveMethod.LiftAndPlace,4,4,1,999,100,                Array.Empty<TraitStatusSnapshot>(),CategorySnapshot.CreateStateless(CategoryKey.Heavy));
            var held = new CharacterHeldEnvironmentState(41, true, 7);
            var modifier = new PositionTileModifier(original.Anchor, new long[] { 51 }, 5, 20);
            var attacker = new CharacterCombatFacts(original.EntityId, original.OwnedId, original.CharacterKey,
                original.Owner, original.OwnershipRole, original.Area, original.Health, stats, null,
                original.Anchor, original.Rotation, new[] { new CellCoord(4, 3), new CellCoord(5, 3) }, original.ActionUse, positionModifier: modifier, heldEnvironment: held);
            var result = Reflect(Scope(), attacker, Ally());
            var final = result.TargetChange.Final;
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(final.Stats.PhysicalDefense, Is.EqualTo(999));
            Assert.That(final.Stats.Category, Is.EqualTo(stats.Category));
            Assert.That(final.HeldEnvironment, Is.SameAs(held));
            Assert.That(final.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(final.PositionModifier, Is.SameAs(modifier));
            CollectionAssert.AreEqual(new[] { new CellCoord(4, 3), new CellCoord(5, 3) }, final.OccupiedCells);
        }

        [Test]
        public void Reflection_PublishesArtifactCauseWithoutCharacterKillCredit()
        {
            var result = Reflect(Scope(), Attacker(), Ally(), 4);
            var activation = result.Activation.Activations[0];
            Assert.That(activation.Origin, Is.EqualTo(new EffectOriginKey(EffectOwnerKind.Artifact,
                13, 1002, "heart")));
            Assert.That(activation.TargetId, Is.EqualTo(new EntityId(2)));
            Assert.That(result.Activation.Reflection.StruckAllyId, Is.EqualTo(new EntityId(7)));
            Assert.That(result.Activation.Reflection.TriggeringAttackGroup, Is.EqualTo(1L));
            Assert.That(result.Activation.Reflection.ReflectionAttackGroup, Is.EqualTo(4L));
            Assert.That(result.DamageOrigin.AttackGroup, Is.EqualTo(4L));
            Assert.That(result.DamageOrigin.CreditedCharacterId, Is.Null);
        }

        [Test]
        public void CompiledLoadoutAccessor_ReusesImmutableCompilation()
        {
            var calculator = new SoulEffectCalculator(Snapshot(3, 1));
            var first = calculator.CompiledLoadout;
            Assert.That(calculator.CompiledLoadout, Is.SameAs(first));
            Assert.That(first.Rules.Length, Is.EqualTo(1));
        }

        private static CharacterCombatFacts Attacker(int hp = 10)
        {
            return CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Demon,
                new CellCoord(4, 3), Owner.Enemy), hp: hp);
        }

        private static CharacterCombatFacts Ally(int id = 7)
        {
            return CombatCommandTestData.Character(id, EntityKey.Unit_Akimbo, new CellCoord(3, 3), Owner.Player);
        }

        private static CharacterCombatFacts OutsideField(CharacterCombatFacts c)
        {
            return new CharacterCombatFacts(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole,
                CharacterRuntimeArea.Excluded, c.Health, c.Stats, null, null, null, Array.Empty<CellCoord>(), c.ActionUse);
        }

        private static ArtifactLoadoutSnapshot Snapshot(int amount, int copies)
        {
            var effect = ArtifactThornHeartRuleTests.HeartEffect();
            effect.value.amount = amount;
            var effects = new ArtifactPlacedEffectSnapshot[copies];
            for (int i = 0; i < copies; i++)
                effects[i] = new ArtifactPlacedEffectSnapshot(1002, 13 + i, effect);
            return new ArtifactLoadoutSnapshot(0, effects);
        }

        private static ThornHeartAttackScope Scope(int amount = 3, bool ordinary = true, bool night = true, int copies = 1)
        {
            return new ThornHeartAttackScope(ArtifactEffectCompiler.Compile(Snapshot(amount, copies)), ordinary, night);
        }

        private static ThornHeartReflection Reflect(ThornHeartAttackScope scope,
            CharacterCombatFacts attacker, CharacterCombatFacts ally, long group = 2)
        {
            var characters = new System.Collections.Generic.Dictionary<EntityId, CharacterCombatFacts> { [attacker.EntityId] = attacker };
            var damage = new CombatDamageApplication(Array.Empty<ItemDefinition>(), 0, characters,
                new System.Collections.Generic.List<CombatEvent>());
            bool reflected = scope.TryReflect(attacker, ally, group, damage, out var result);
            Assert.That(reflected, Is.EqualTo(result != null));
            return result;
        }
    }
}
