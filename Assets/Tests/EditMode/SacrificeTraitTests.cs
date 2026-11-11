using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Tests.Core
{
    public sealed class SacrificeTraitTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [TestCase(true, 80)]
        [TestCase(false, 90)]
        public void RecipientDefense_UsesSacrificerStatsAndPreviewParity(bool closer, int expectedHealth)
        {
            var input = Input(defense: 50);
            if (!closer)
            {
                input = Move(input, 1, new CellCoord(3, 3));
                input = Move(input, 5, new CellCoord(3, 5));
            }
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 5
                ? c.With(stats: c.Stats.With(physicalDefense: closer ? 0 : 50)) : c).ToArray());
            var preview = CombatCommandCalculator.Preview(input, Rules);
            Assert.That(preview.CanAttack, Is.True);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(expectedHealth));
            Assert.That(Used(TraitCombatFixtures.Final(result, 5)), Is.EqualTo(1));
        }

        [TestCase(TraitKey.Disarm)]
        [TestCase(TraitKey.AnkleBreaker)]
        public void Restriction_GoesToSacrificer_NotOriginalTarget(TraitKey key)
        {
            // 희생이 대신 맞으면 무장 해제·발목 부수기도 대신 맞은 유닛이 받는다(2026-11-02 결정).
            TraitSpec restriction = key == TraitKey.Disarm ? (TraitSpec)new DisarmTraitSpec(1) : new AnkleBreakerTraitSpec(1);
            var input = TraitCombatFixtures.Equip(Input(), EntityKey.Unit_Akimbo, restriction);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Used(TraitCombatFixtures.Final(result, 5)), Is.EqualTo(1));
            var guard = TraitCombatFixtures.Final(result, 5).Debuffs;
            var original = TraitCombatFixtures.Final(result, 2).Debuffs;
            Assert.That(key == TraitKey.Disarm ? guard.Disarm : (object)guard.AnkleBreaker, Is.Not.Null);
            Assert.That(original.Disarm, Is.Null);
            Assert.That(original.AnkleBreaker, Is.Null);
        }

        [Test]
        public void SavingEarlierAuraSource_RemovesUnnecessaryLaterReservation()
        {
            var input = Move(Input(splash: true), 10, new CellCoord(5, 4));
            input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.EntityId.Value != 3).ToArray());
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 10
                ? c.WithHealth(new HealthStateSnapshot(10, 100)) : c).ToArray(), main: input.Main.WithCurrentHealth(10));
            input = AddSource(input, 6, EntityKey.Unit_Cowboy, new CellCoord(6, 4));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Cowboy, new SacrificeTraitSpec(2, 1));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new EntrenchmentTraitSpec(1, 50));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 10).Health.CurrentHealth, Is.EqualTo(3));
            Assert.That(Used(TraitCombatFixtures.Final(result, 5)), Is.EqualTo(1));
            Assert.That(Used(TraitCombatFixtures.Final(result, 6)), Is.Zero);
            Assert.That(TraitCombatFixtures.Final(result, 6).Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void LethalReflection_ReservesProtectionOnlyForDeliveredHits()
        {
            var input = Move(Input(splash: true), 10, new CellCoord(5, 4));
            input = CombatCommandTestData.With(input, artifacts: ThornHeartTestData.Input(heart: true, splash: true).Artifacts,
                characters: input.Characters.Select(c => c.EntityId.Value == 1 ? c.WithHealth(new HealthStateSnapshot(1, 100))
                    : c.EntityId.Value == 10 ? c.WithHealth(new HealthStateSnapshot(5, 100)) : c).ToArray(), main: input.Main.WithCurrentHealth(5));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 1).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 10).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(Used(TraitCombatFixtures.Final(result, 5)), Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatArtifactEvent>().Count(e => e.Reflection != null), Is.EqualTo(1));
        }

        [Test]
        public void PiercingAfterLethalReflection_StillProtectsDeliveredLaterHit()
        {
            var input = ThornHeartTestData.Input(heart: true, piercing: true, hp: 1);
            input = Move(input, 10, new CellCoord(6, 3));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithHealth(new HealthStateSnapshot(1, 100)) : c.EntityId.Value == 10
                    ? c.WithHealth(new HealthStateSnapshot(1, 100)) : c).ToArray(), main: input.Main.WithCurrentHealth(1));
            input = AddSource(input, 5, EntityKey.Unit_Shielder, new CellCoord(5, 4));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Shielder, new SacrificeTraitSpec(2, 1));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 1).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 10).Health.CurrentHealth, Is.EqualTo(1));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.LessThan(100));
            Assert.That(Used(TraitCombatFixtures.Final(result, 5)), Is.EqualTo(1));
        }

        [Test]
        public void ReflectedNumericDamage_DoesNotTriggerSacrifice()
        {
            var input = Input();
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            CombatDamageApplication.For(input, characters, new List<CombatEvent>()).Apply(new EntityId(2),
                CombatDamageKind.ReflectedDamage, new CombatDamageOrigin(new CellCoord(1, 3), new EntityId(1), 1), 20);
            Assert.That(characters[new EntityId(2)].Health.IsDead, Is.True);
            Assert.That(characters[new EntityId(5)].Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Used(characters[new EntityId(5)]), Is.Zero);
        }

        [Test]
        public void OriginalShield_PreventsUnnecessarySacrifice()
        {
            var input = Input();
            input = AddSource(input, 7, EntityKey.Unit_ShotGun, new CellCoord(5, 4));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_ShotGun, new CohesionTraitSpec(0, 30, 1, 1));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Used(TraitCombatFixtures.Final(result, 5)), Is.Zero);
            Assert.That(TraitCombatFixtures.Final(result, 2).TraitEffects.ActiveShieldTotal, Is.EqualTo(10));
        }

        [Test]
        public void Redirect_RecalculatesSacrificerMatchupAndDefense()
        {
            var input = Input(defense: 20);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: 100,category: CategorySnapshot.CreateStateless(CategoryKey.Rider))) : c).ToArray());
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 100, CategoryKey.Rider), p.Method) : p).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(44));
            Assert.That(Used(TraitCombatFixtures.Final(result, 5)), Is.EqualTo(1));
        }

        [Test]
        public void EnemyTurnOnly_OncePerTurn()
        {
            var input = AddEnemy(Input(), 6, new CellCoord(1, 4));
            var first = TraitCombatFixtures.Attack(input);
            Assert.That(TraitCombatFixtures.Final(first, 2).Health.CurrentHealth, Is.EqualTo(5));
            var after = CombatCommandComposition.Project(first);
            var second = TraitCombatFixtures.Attack(after.WithActor(new EntityId(6)));
            Assert.That(TraitCombatFixtures.Final(second, 2).Health.IsDead, Is.True);
            Assert.That(Used(TraitCombatFixtures.Final(second, 5)), Is.EqualTo(1));
            after = CombatCommandComposition.Project(second);
            after = CombatCommandTestData.With(after, characters: after.Characters.Select(c => c.EntityId.Value == 3
                ? c.WithHealth(new HealthStateSnapshot(5, 100)) : c).ToArray());
            after = CombatCommandTestData.NextEnemyTurn(after.WithActor(new EntityId(1)))
                .WithIntent(CombatCommandKind.Attack, AttackAim.ForCell(new CellCoord(5, 3)), default, null, null);
            var next = TraitCombatFixtures.Attack(after);
            Assert.That(next.Accepted, Is.True, next.Reason);
            Assert.That(TraitCombatFixtures.Final(next, 3).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(next, 5).Health.CurrentHealth, Is.EqualTo(60));
        }

        [Test]
        public void PlayerTurnReaction_DoesNotSpendSacrifice()
        {
            var input = Input();
            input = CombatCommandTestData.With(input, state: input.State.With(phase: CombatFlowPhase.PlayerInput,
                turn: new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Player, controllerTurnSequence: 1)));
            var result = CombatCommandTestData.Reaction(input, Rules);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.IsDead, Is.True);
            Assert.That(Used(TraitCombatFixtures.Final(result, 5)), Is.Zero);
        }

        [Test]
        public void Redirect_DoesNotRelay()
        {
            var input = Input();
            input = Move(input, 1, new CellCoord(0, 3));
            input = Move(input, 5, new CellCoord(3, 3));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(appointRange: 8)) : c.EntityId.Value == 5 ? c.WithHealth(new HealthStateSnapshot(5, 100)) : c).ToArray());
            input = AddSource(input, 6, EntityKey.Unit_Shielder, new CellCoord(1, 3));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Shielder, new SacrificeTraitSpec(2, 1));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 6).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Used(TraitCombatFixtures.Final(result, 6)), Is.Zero);
        }

        [Test]
        public void Priority_DistanceBeforeRemainingHp()
        {
            var input = Move(Input(), 5, new CellCoord(3, 3));
            input = AddSource(input, 6, EntityKey.Unit_Shielder, new CellCoord(2, 4));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Shielder, new SacrificeTraitSpec(2, 1));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 5
                ? c.WithHealth(new HealthStateSnapshot(25, 100)) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 6).Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void Priority_AoeEqualDistanceReservesMainBeforeEarlierSubHit()
        {
            var input = Move(Input(splash: true), 10, new CellCoord(5, 4));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 10
                ? c.WithHealth(new HealthStateSnapshot(5, 100)) : c).ToArray(), main: input.Main.WithCurrentHealth(5));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 10).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(72));
            Assert.That(Used(TraitCombatFixtures.Final(result, 5)), Is.EqualTo(1));
        }

        [Test]
        public void Priority_EqualDistanceUsesHigherSurvivingHp()
        {
            var input = Move(Input(), 5, new CellCoord(3, 3));
            input = AddSource(input, 6, EntityKey.Unit_Shielder, new CellCoord(4, 2));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Shielder, new SacrificeTraitSpec(2, 1));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 5
                ? c.WithHealth(new HealthStateSnapshot(30, 100)) : c.EntityId.Value == 6 ? c.WithHealth(new HealthStateSnapshot(80, 100)) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(30));
            Assert.That(TraitCombatFixtures.Final(result, 6).Health.CurrentHealth, Is.EqualTo(60));
        }

        [Test]
        public void AoeSourceThatDiesEarlier_CannotProtectLaterHit()
        {
            var input = Move(Input(splash: true), 10, new CellCoord(5, 4));
            input = AddSource(input, 6, EntityKey.Unit_Shielder, new CellCoord(7, 4));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Shielder, new SacrificeTraitSpec(2, 1));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2 ? c.WithHealth(new HealthStateSnapshot(100, 100)) : c.EntityId.Value == 10
                ? c.WithHealth(new HealthStateSnapshot(5, 100)) : c.EntityId.Value == 5 || c.EntityId.Value == 6
                    ? c.WithHealth(new HealthStateSnapshot(10, 100)) : c).ToArray(), main: input.Main.WithCurrentHealth(5));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 10).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.IsDead, Is.True);
            Assert.That(Used(TraitCombatFixtures.Final(result, 5)), Is.Zero);
            Assert.That(TraitCombatFixtures.Final(result, 6).Health.IsDead, Is.True);
            Assert.That(Used(TraitCombatFixtures.Final(result, 6)), Is.EqualTo(1));
        }

        [Test]
        public void PureRule_DoesNotBanEnemyOwnerAssignment()
        {
            var input = Input();
            var actor = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Player, 20);
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy)
                .WithHealth(new HealthStateSnapshot(5, 100));
            var source = CombatCommandTestData.Character(5, EntityKey.Unit_Shielder, new CellCoord(3, 4), Owner.Enemy)
                .With(stats: input.Characters.Single(c => c.EntityId.Value == 5).Stats);
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 1 ? actor : c.EntityId.Value == 2 ? target : c.EntityId.Value == 5 ? source : c).ToArray());
            var result = CombatCommandTestData.Reaction(input, Rules);
            Assert.That(result, Is.Not.Null);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(80));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Planning_IsImmutableAndInstantDefeatDoesNotTriggerOrSpend(bool instantDefeat)
        {
            var input = Input();
            var prepared = CombatItemDirectDamageTests.Prepare(input);
            var plan = prepared.Plan;
            if (instantDefeat)
            {
                var h = plan.Delivery.Hits[0];
                var cause = typeof(CombatCommandCalculator).Assembly.GetType(typeof(CombatCommandCalculator).Namespace + ".SacrificeDamagePlanner");
                Assert.That(cause, Is.Not.Null);
                var hit = new DeliveredAttackHit(h.TargetId, h.Role, h.BaseDamage, h.AppliedPercent, h.ImpactOrigin, CombatDamageCause.InstantDefeat);
                plan = new CommandAttackPlan(plan.Input,plan.Rules,plan.Profile,plan.OriginalActor,plan.Actor,plan.Environment,plan.Walls,                    plan.Selection,new ResolvedAttackDelivery(new[] { hit }, plan.Delivery.AffectedCells),plan.Engine,plan.Bindings,                    plan.Pipeline,plan.CommonDamage,plan.Direction,plan.PublicPreview,plan.DamageKind);
            }
            var before = prepared.World.Characters.Values.ToArray();
            var type = typeof(CombatCommandCalculator).Assembly.GetType(typeof(CombatCommandCalculator).Namespace + ".SacrificeDamagePlanner");
            Assert.That(type, Is.Not.Null);
            var result = (IEnumerable)type.GetMethod("Plan", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { plan, prepared.World, null });
            Assert.That(result.Cast<object>().Count(), Is.EqualTo(instantDefeat ? 0 : 1));
            foreach (var c in before) Assert.That(prepared.World.Characters[c.EntityId].SameValues(c), Is.True);
            Assert.That(prepared.World.Events, Is.Empty);
            Assert.That(Used(prepared.World.Characters[new EntityId(5)]), Is.Zero);
        }

        [Test]
        public void LaterDirectHit_UsesEarlierKillGrantInPlanningAndApplication()
        {
            var input = Input(splash: true);
            input = Move(Move(input, 3, new CellCoord(4, 4)), 5, new CellCoord(4, 6));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new MoraleBoostTraitSpec(20, 1));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.With(health: new HealthStateSnapshot(20, 100), stats: c.Stats.With(attackPower: 10))
                : c.EntityId.Value == 3 ? c.With(health: new HealthStateSnapshot(11, 100), stats: c.Stats.With(physicalDefense: 50)) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                    ? new CombatAttackProfile(p.Definition, new TargetingMethodSpec(TargetingShape.RadialSquare,
                        TargetingChoiceMode.Single, TargetingSplash.Radial, 100)) : p).ToArray());
            var (plan, world, ledger) = CombatItemDirectDamageTests.Prepare(input);
            var redirects = SacrificeDamagePlanner.Plan(plan, world);
            Assert.That(redirects.Values, Is.EqualTo(new[] { new EntityId(5) }));
            Assert.That(world.Characters[new EntityId(2)].Health.CurrentHealth, Is.EqualTo(20));
            Assert.That(world.Events, Is.Empty);
            Assert.That(Used(world.Characters[new EntityId(5)]), Is.Zero);
            AttackDirectDamageCalculator.Apply(plan, world, ledger, new HashSet<EntityId>());
            Assert.That(world.Characters[new EntityId(2)].Health.IsDead, Is.True);
            Assert.That(world.Characters[new EntityId(3)].Health.CurrentHealth, Is.EqualTo(11));
            Assert.That(world.Characters[new EntityId(5)].Health.CurrentHealth, Is.EqualTo(78));
            Assert.That(Used(world.Characters[new EntityId(5)]), Is.EqualTo(1));
        }

        [Test]
        public void RedirectedHit_UsesGuardianHealthAndPreservesOriginalAim()
        {
            var result = TraitCombatFixtures.Attack(Input(defense: 50));
            Assert.That(result.Accepted, Is.True, result.Reason);
            var attack = result.Attack.Value;
            Assert.That(attack.PrimaryTargetId, Is.EqualTo(new EntityId(2)));
            Assert.That(attack.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(5) }));
            var hit = attack.Hits.ToArray().Single();
            Assert.That(hit.TargetHealthBefore, Is.EqualTo(100));
            Assert.That(hit.TargetHealthAfter, Is.EqualTo(90));
            Assert.That(hit.DamageDealt, Is.EqualTo(10));
            var contacts = hit.GetType().GetProperty("Contacts");
            Assert.That(contacts, Is.Not.Null);
            var contact = ((IEnumerable)contacts.GetValue(hit)).Cast<object>().Single();
            Assert.That(contact.GetType().GetProperty("OriginalTargetId").GetValue(contact), Is.EqualTo(new EntityId(2)));
        }

        [Test]
        public void Interception_IsAvailableInOwningAttackAtStart_WithoutSecondEffectsOccurrence()
        {
            var result = TraitCombatFixtures.Attack(Input());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var reaction = result.Attack.Value.TraitActivations.ToArray().Single(t => t.TraitActivationKind == TraitActivationKind.DamageRedirected);
            var property = reaction.GetType().GetProperty("Interception");
            Assert.That(property, Is.Not.Null);
            var interception = property.GetValue(reaction);
            Assert.That(interception, Is.Not.Null);
            Assert.That(interception.GetType().GetProperty("GuardianId").GetValue(interception), Is.EqualTo(new EntityId(5)));
            Assert.That(interception.GetType().GetProperty("ProtectedId").GetValue(interception), Is.EqualTo(new EntityId(2)));
            Assert.That(interception.GetType().GetProperty("ActualRecipientId").GetValue(interception), Is.EqualTo(new EntityId(5)));
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Any(t => t.TraitActivationKind == TraitActivationKind.DamageRedirected), Is.False);
        }

        [Test]
        public void AoeGuardianAlsoDirectTarget_KeepsEveryOriginalRelationship()
        {
            var input = TraitCombatFixtures.Equip(Input(splash: true), EntityKey.Unit_Shielder, new SacrificeTraitSpec(2, 2));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 3
                ? c.WithHealth(new HealthStateSnapshot(5, 100)) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var hit = result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == new EntityId(5));
            Assert.That(hit.Contacts.Select(c => c.OriginalTargetId), Is.EquivalentTo(new[] { new EntityId(2), new EntityId(3), new EntityId(5) }));
            Assert.That(hit.Role, Is.EqualTo(AttackTargetRole.None));
            var reactions = result.Attack.Value.TraitActivations.ToArray().Where(t => t.Interception != null).ToArray();
            Assert.That(reactions.Length, Is.EqualTo(2));
            Assert.That(reactions.Select(t => t.Interception.DeliveryIndex).Distinct().Count(), Is.EqualTo(2));
            Assert.That(hit.TargetHealthAfter, Is.EqualTo(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth));
            Assert.That(hit.DamageDealt, Is.EqualTo(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Final.EntityId == new EntityId(5))
                .Sum(e => e.Expected.Health.CurrentHealth - e.Final.Health.CurrentHealth)));
        }

        [Test]
        public void RepeatedHits_AggregateGuardianHealthWithSeparateInterceptions()
        {
            var input = TraitCombatFixtures.Equip(Input(), EntityKey.Unit_Shielder, new SacrificeTraitSpec(2, 2));
            var (plan, world, ledger) = CombatItemDirectDamageTests.Prepare(input, duplicate: true);
            AttackDirectDamageCalculator.Apply(plan, world, ledger, new HashSet<EntityId>());
            ledger.CaptureHitHealth(world);
            var outcome = new AttackOutcomeSnapshot(AttackResultFactory.CreateHits(ledger), ledger.Reactions);
            Assert.That(outcome.Reactions.Count, Is.EqualTo(2));
            Assert.That(outcome.Hits.Count, Is.EqualTo(1));
            Assert.That(outcome.Hits[0].TargetId, Is.EqualTo(new EntityId(5)));
            Assert.That(outcome.Hits[0].Contacts.Select(c => c.DeliveryIndex), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(outcome.Hits[0].TargetHealthBefore, Is.EqualTo(100));
            Assert.That(outcome.Hits[0].TargetHealthAfter, Is.EqualTo(60));
        }

        [Test]
        public void RoarReaction_IsAttachedToRoarStart()
        {
            var result = CombatCommandTestData.Reaction(Input(), Rules);
            var roar = result.Events.OfType<CombatRoarEvent>().Single();
            var reaction = roar.Attack.Value.TraitActivations.ToArray().Single(t => t.Interception != null);
            Assert.That(reaction.Interception.ActualRecipientId, Is.EqualTo(new EntityId(5)));
            Assert.That(roar.Attack.Value.Hits.ToArray().Single().TargetId, Is.EqualTo(new EntityId(5)));
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Any(t => t.TraitActivationKind == TraitActivationKind.DamageRedirected), Is.False);
            Assert.That(result.Events.ToList().IndexOf(roar), Is.LessThan(result.Events.ToList().FindIndex(e => e is CombatTraitStateEvent)));
        }

        [Test]
        public void FinishingTarget_IsProtected_AndNoInstantDefeatFires()
        {
            // 희생이 대신 맞으면 원래 대상은 맞지 않아 마무리 사격 즉사가 터지지 않는다. 수호자는 숫자 피해만 받는다(2026-11-10 사용자 결정).
            var input = TraitCombatFixtures.Equip(Input(), EntityKey.Unit_Akimbo, new FinishingShotTraitSpec(20));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: 100)) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.TraitActivations.ToArray().Count(t => t.Interception != null), Is.EqualTo(1));
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(Used(TraitCombatFixtures.Final(result, 5)), Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Any(t => t.TraitActivationKind == TraitActivationKind.InstantDefeat), Is.False);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.DamageCause == CombatDamageCause.InstantDefeat), Is.False);
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(result.Events));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RedirectedKnockback_GoesToSacrificer_NotOriginalTarget(bool steadfast)
        {
            // 희생이 대신 맞으면 넉백도 수호자가 받는다(2026-11-02 결정). 원래 대상은 밀리지 않아 (6,3) 지뢰에 닿지 않는다.
            // 실제 Heavy는 기본 특성 굳건한 의지가 있어 수호자도 안 밀린다(steadfast).
            var input = Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(2) })) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main, new KnockbackTraitSpec(2)), p.Method) : p).ToArray());
            input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.EntityId.Value != 3).ToArray());
            input = CliffFallTests.WithEnvironment(input, new MineSourceFact(60, new CellCoord(6, 3), 1, 7, true, new[] { Owner.Player, Owner.Enemy }));
            if (steadfast) input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Shielder, new SacrificeTraitSpec(2, 1), new SteadfastWillTraitSpec());
            var guardianStart = input.Characters.Single(c => c.EntityId.Value == 5).Anchor;
            var originalStart = input.Characters.Single(c => c.EntityId.Value == 2).Anchor;
            var preview = CombatCommandCalculator.Preview(input, MineEnvironmentTests.Rules, out var projection);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Knockbacks.ToArray().Select(k => k.TargetId), Is.EqualTo(new[] { new EntityId(5) }));
            Assert.That(preview.Targets.ToArray().Single(t => t.TargetId == new EntityId(5)).TargetHealthAfter, Is.EqualTo(80));
            var original = TraitCombatFixtures.Final(projection, 2);
            Assert.That(original.Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(original.Anchor, Is.EqualTo(originalStart));
            if (steadfast) Assert.That(TraitCombatFixtures.Final(projection, 5).Anchor, Is.EqualTo(guardianStart));
        }

        [Test]
        public void Intimidation_GoesToSacrificer_NotOriginalTarget()
        {
            // 위압도 실제로 맞은 수호자가 받는다(2026-11-02 결정). 표식은 WeaknessExposureDamageTests.Sacrifice_…GrantsActualRecipient가 본다.
            var input = Input();
            var actorKey = input.Characters.Single(c => c.EntityId == input.ActorId).CharacterKey;
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == input.ActorId
                    ? c.With(stats: c.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new IntimidationTraitStatusSnapshot(20) })) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == actorKey
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(actorKey, 20, CategoryKey.Main, new IntimidationTraitSpec(20)), p.Method) : p).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Used(TraitCombatFixtures.Final(result, 5)), Is.EqualTo(1));
            Assert.That(TraitCombatFixtures.Final(result, 5).Debuffs.Intimidation, Is.Not.Null);
            Assert.That(TraitCombatFixtures.Final(result, 2).Debuffs.Intimidation, Is.Null);
        }

        internal static CombatCommandInput Input(bool splash = false, int defense = 0)
        {
            var input = ThornHeartTestData.Input(heart: false, splash: splash, hp: 100);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithHealth(new HealthStateSnapshot(5, 100)) : c).ToArray());
            input = AddSource(input, 5, EntityKey.Unit_Shielder, new CellCoord(3, 4), defense);
            return TraitCombatFixtures.Equip(input, EntityKey.Unit_Shielder, new SacrificeTraitSpec(2, 1));
        }

        private static CombatCommandInput AddSource(CombatCommandInput input, int id, EntityKey key, CellCoord cell, int defense = 0)
        {
            var source = CombatCommandTestData.Character(id, key, cell, Owner.Player);
            source = source.With(stats: source.Stats.With(category: CategorySnapshot.CreateStateless(CategoryKey.Heavy), physicalDefense: defense));
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] { source }).ToArray());
            if (!input.Profiles.Any(p => p.Definition.Key == key)) input = CombatCommandTestData.With(input, profiles: input.Profiles.Concat(new[]
            {
                new CombatAttackProfile(CombatCommandTestData.Spec(key, 1, CategoryKey.Heavy),
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))
            }).ToArray());
            return input;
        }

        private static CombatCommandInput AddEnemy(CombatCommandInput input, int id, CellCoord cell) =>
            TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] { CombatCommandTestData.Character(id, EntityKey.Unit_Akimbo, cell, Owner.Enemy, 20) }).ToArray());
        private static CombatCommandInput Move(CombatCommandInput input, int id, CellCoord cell) => CombatCommandTestData.With(input,
            characters: input.Characters.Select(c => c.EntityId.Value == id ? c.WithPlacement(CharacterRuntimeArea.Field, null, cell, Rotation4.R0, new[] { cell }) : c).ToArray());
        private static int Used(CharacterCombatFacts source) => (int)source.Stats.TraitStatuses.ToArray().Single(t => t.Key == TraitKey.Sacrifice)
            .GetType().GetProperty("ActivationsThisEnemyTurn").GetValue(source.Stats.TraitStatuses.ToArray().Single(t => t.Key == TraitKey.Sacrifice));
    }
}
