using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class TraitActivationOutcomeTests
    {
        [TestCase(0)]
        [TestCase(2)]
        public void Hunter_ZeroReward_ChangesCapWithoutAttackGranted(int award)
        {
            var input = TraitCombatFixtures.Equip(HunterTraitTests.Input(), EntityKey.Unit_Akimbo, new HunterTraitSpec(award, 3));
            input = CombatCommandTestData.NextPlayerTurn(input);
            var target = input.Characters.Single(c => c.EntityId == HunterTraitTests.Target(HunterTraitTests.Actor(input)));
            var result = TraitCombatFixtures.Attack(CombatAttackQueries.WithAim(input, AttackAim.ForCell(target.Anchor.Value)));
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = MovementTestData.After(result);
            Assert.That(HunterTraitTests.Rewards(HunterTraitTests.Actor(final)), Is.EqualTo(1));
            Assert.That(HunterTraitTests.Bonus(final), Is.EqualTo(award));
            Assert.That(HunterTraitTests.Target(HunterTraitTests.Actor(final)), Is.Not.EqualTo(target.EntityId));
            var grants = Activations(result.Events, TraitKey.Hunter).Where(a => a.TraitActivationKind == TraitActivationKind.AttackGranted).ToArray();
            Assert.That(grants.Length, Is.EqualTo(award == 0 ? 0 : 1));
            if (award > 0) Assert.That(grants.Single().Magnitude, Is.EqualTo(award));
            Assert.That(result.Attack.Value.TraitActivations.ToArray().Any(a => a.TraitKey == TraitKey.Hunter && a.TraitActivationKind == TraitActivationKind.AttackGranted), Is.EqualTo(award > 0));
            Assert.That(result.Events.OfType<CombatTraitStateEvent>().Any(e => e.Final.EntityId == input.ActorId), Is.True);
        }

        [TestCase(10, new[] { 10, 20, 20 })]
        [TestCase(3, new[] { 3, 6, 6 })]
        public void FactorEvidence_MatchesOrderedExactPrefixes(int attack, int[] deltas)
        {
            var input = CannonThresholdTraitTests.ThreeFactors(attack);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var factors = result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Where(t => t.TraitActivationKind == TraitActivationKind.DamageModified).ToArray();
            Assert.That(factors.Select(t => t.TraitKey), Is.EqualTo(new[] {
                TraitKey.StationaryFire, TraitKey.TrackingShot, TraitKey.FocusedBombardment }));
            Assert.That(factors.Select(t => t.Magnitude), Is.EqualTo(deltas));
            foreach (var factor in factors)
            {
                Assert.That(factor.ResultOrigin.PhysicalCell, Is.EqualTo(input.Characters.Single(c => c.EntityId == input.ActorId).Anchor));
                Assert.That(factor.ResultOrigin.CreditedCharacterId, Is.EqualTo(input.ActorId));
                Assert.That(factor.ResultOrigin.AttackGroup, Is.EqualTo(1));
            }
        }

        [Test]
        public void CallOfBlood_ActivationPrecedesActualRecovery()
        {
            var input = KillInput(50);
            var facts = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var origin = new CombatDamageOrigin(CellCoord.Zero, new EntityId(1), 7);
            CombatDamageApplication.For(input, facts, events).Apply(new EntityId(2), CombatDamageKind.OrdinaryAttack, origin, 1);
            int death = events.FindIndex(e => e.Kind == CombatEventKind.Died);
            int activation = events.FindIndex(e => HasActivation(e, TraitKey.CallOfBlood, "HealthRecovered"));
            int recovery = events.FindIndex(e => e is CombatCharacterEvent c && c.Final.EntityId.Value == 1 && c.Final.Health.CurrentHealth > c.Expected.Health.CurrentHealth);
            Assert.That(activation, Is.GreaterThan(death));
            Assert.That(activation, Is.LessThan(recovery));
            var value = Activations(events, TraitKey.CallOfBlood).Single();
            Assert.That(value.Magnitude, Is.EqualTo(10));
            AssertOrigin(value.ResultOrigin, origin);
        }

        [Test]
        public void MultiRecipientGrant_IsOneOccurrence()
        {
            var input = GrantInput();
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var values = Activations(result.Events, TraitKey.Vanguard).Where(a => a.TraitActivationKind == TraitActivationKind.AttackGranted).ToArray();
            Assert.That(values, Has.Length.EqualTo(1));
            Assert.That(values[0].TargetIds, Is.EqualTo(new[] { new EntityId(5), new EntityId(10) }));
            Assert.That(values[0].Magnitude, Is.EqualTo(3));
            int at = result.Events.ToList().FindIndex(e => HasActivation(e, TraitKey.Vanguard, "AttackGranted"));
            foreach (int id in new[] { 5, 10 })
            {
                var state = result.Events.Skip(at + 1).FirstOrDefault(e => e.Kind.ToString() == "TraitStateChanged" && State(e, "Final").EntityId.Value == id);
                Assert.That(state, Is.Not.Null);
                Assert.That(State(state, "Expected").Stats.TraitAttackBonus, Is.Zero);
                Assert.That(State(state, "Final").Stats.TraitAttackBonus, Is.EqualTo(3));
            }
        }

        [Test]
        public void NoEffect_DoesNotActivate()
        {
            var input = KillInput(100);
            var facts = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            CombatDamageApplication.For(input, facts, events).Apply(new EntityId(2), CombatDamageKind.ExtraAttack,
                new CombatDamageOrigin(CellCoord.Zero, new EntityId(1), 1), 1);
            Assert.That(Activations(events, TraitKey.CallOfBlood), Is.Empty);
            Assert.That(facts[new EntityId(1)].Health.CurrentHealth, Is.EqualTo(100));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DefensiveOwner_UsesIncomingResultOrigin(bool nullCredit)
        {
            var input = TraitShieldTests.Cohesion();
            var facts = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var origin = new CombatDamageOrigin(new CellCoord(4, 3), nullCredit ? (EntityId?)null : new EntityId(2), nullCredit ? 0 : 19);
            CombatDamageApplication.For(input, facts, events).Apply(new EntityId(10), CombatDamageKind.OrdinaryAttack, origin, 14);
            var values = Activations(events, TraitKey.Cohesion).Where(a => a.TraitActivationKind.ToString() == "ShieldAbsorbed").ToArray();
            Assert.That(values, Has.Length.EqualTo(2));
            Assert.That(values.Select(a => a.SourceUnitId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(values.Sum(a => a.Magnitude), Is.EqualTo(14));
            foreach (var value in values)
            {
                Assert.That(value.TargetId, Is.EqualTo(new EntityId(10)));
                Assert.That(value.SourceUnitId, Is.Not.EqualTo(new EntityId(2)));
                AssertOrigin(value.ResultOrigin, origin);
            }
            Assert.That(facts[new EntityId(10)].Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(facts[new EntityId(10)].TraitEffects.ActiveShieldTotal, Is.EqualTo(6));
        }

        [Test]
        public void PureEvaluationAndSacrificePlanning_DoNotPublish()
        {
            var input = SacrificeTraitTests.Input(splash: true);
            var (plan, world, ledger) = CombatItemDirectDamageTests.Prepare(input);
            var before = world.Characters.Values.ToArray();
            Assert.That(SacrificeDamagePlanner.Plan(plan, world), Is.Not.Empty);
            Assert.That(world.Events, Is.Empty);
            foreach (var character in before) Assert.That(world.Characters[character.EntityId].SameValues(character), Is.True);
            var state = input.State;
            CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(input.State.SameValues(state), Is.True);
            Assert.That(world.Events, Is.Empty);
        }

        [Test]
        public void PostHitArmor_HasOrderedEventTimeState()
        {
            var input = TraitCombatFixtures.Equip(TraitMovementAttackTests.SingleTarget(), EntityKey.Unit_Akimbo, new ArmorBreakerTraitSpec(50));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var events = result.Events.ToList();
            int hp = events.FindIndex(e => e is CombatCharacterEvent c && c.Final.EntityId.Value == 2 && c.Kind == CombatEventKind.HealthChanged);
            int activation = events.FindIndex(e => HasActivation(e, TraitKey.ArmorBreaker, "ArmorBroken"));
            int state = events.FindIndex(e => e.Kind.ToString() == "TraitStateChanged" && State(e, "Final").EntityId.Value == 2);
            Assert.That(activation, Is.GreaterThan(hp));
            Assert.That(state, Is.GreaterThan(activation));
            var before = State(events[state], "Expected");
            var after = State(events[state], "Final");
            Assert.That(before.Debuffs.ArmorBreak?.Stacks.Count ?? 0, Is.Zero);
            Assert.That(after.Debuffs.ArmorBreak.Stacks.Count, Is.EqualTo(1));
            Assert.That(after.Health.CurrentHealth, Is.EqualTo(before.Health.CurrentHealth));
            Assert.That(after.Health.MaxHealth, Is.EqualTo(before.Health.MaxHealth));
            Assert.That(after.Stats.SameValues(before.Stats), Is.True);
            Assert.That(after.Debuffs.Disarm, Is.SameAs(before.Debuffs.Disarm));
        }

        [Test]
        public void AttackSummary_DoesNotDuplicateOrderedOccurrences()
        {
            var result = TraitCombatFixtures.Attack(GrantInput());
            var occurrences = result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).ToArray();
            Assert.That(result.Attack.Value.TraitActivations.ToArray(), Is.EqualTo(occurrences));
            Assert.That(occurrences.Count(a => a.TraitKey == TraitKey.Vanguard), Is.EqualTo(1));
            Assert.That(result.FinalState.TraitTurn.PlayerAttackActionCount, Is.EqualTo(1));
        }

        [Test]
        public void SourceRemovalAndReset_HaveDistinctOutcomes()
        {
            var input = TraitShieldTests.Cohesion();
            var facts = input.Characters.ToDictionary(c => c.EntityId);
            CombatDamageApplication.For(input, facts, new List<CombatEvent>()).Apply(new EntityId(10), CombatDamageKind.EnvironmentDamage,
                new CombatDamageOrigin(CellCoord.Zero, new EntityId(2), 1), 14);
            input = CombatCommandTestData.With(input, characters: facts.Values.ToArray());
            var removal = new List<CombatEvent>();
            CombatDamageApplication.For(input, facts, removal).Apply(new EntityId(1), CombatDamageKind.EnvironmentDamage,
                new CombatDamageOrigin(CellCoord.Zero, new EntityId(2), 2), 100);
            var removed = Activations(removal, TraitKey.Cohesion).Where(a => a.TraitActivationKind.ToString() == "ShieldRemoved" && a.TargetIds.Contains(new EntityId(10))).ToArray();
            Assert.That(removed.Select(a => a.SourceUnitId), Is.EqualTo(new[] { new EntityId(1) }));
            Assert.That(facts[new EntityId(10)].TraitEffects.ActiveShieldTotal, Is.EqualTo(6));
            Assert.That(facts[new EntityId(10)].TraitEffects.Shields.Single().Origin.OwnerInstanceId, Is.EqualTo(5));
            var reset = CombatTurnCalculator.EndPlayerTurn(input, Array.Empty<IMovementTileRule>());
            Assert.That(reset.Accepted, Is.True, reset.Reason);
            var resetEvents = Activations(reset.Events, TraitKey.Cohesion).Where(a => a.TraitActivationKind.ToString() == "ShieldReset" && a.TargetIds.Contains(new EntityId(10))).ToArray();
            Assert.That(resetEvents.Select(a => a.SourceUnitId), Is.EqualTo(new[] { new EntityId(1), new EntityId(5) }));
            Assert.That(TraitCombatFixtures.Final(reset, 10).TraitEffects.ActiveShieldTotal, Is.EqualTo(20));
            Assert.That(Activations(reset.Events, TraitKey.Cohesion).Any(a => a.TraitActivationKind.ToString() == "ShieldRemoved"), Is.False);
        }

        [TestCase(TraitKey.Disarm)]
        [TestCase(TraitKey.AnkleBreaker)]
        public void StatusChange_PreservesUnrelatedFields(TraitKey key)
        {
            TraitSpec restriction = key == TraitKey.Disarm ? (TraitSpec)new DisarmTraitSpec(1) : new AnkleBreakerTraitSpec(1);
            var input = TraitCombatFixtures.Equip(TraitMovementAttackTests.SingleTarget(), EntityKey.Unit_Akimbo,
                restriction, new ArmorBreakerTraitSpec(50));
            var applied = TraitCombatFixtures.Attack(input);
            Assert.That(applied.Accepted, Is.True, applied.Reason);
            Assert.That(Activations(applied.Events, key).Single().TraitActivationKind.ToString(), Is.EqualTo("StatusApplied"));
            var after = MovementTestData.After(applied);
            var before = after.Characters.Single(c => c.EntityId.Value == 2);
            var ended = CombatTurnCalculator.EndEnemyPhase(EnemyControllerPhaseTests.Input(after, CombatControllerPhase.Enemy), Array.Empty<IMovementTileRule>());
            Assert.That(ended.Accepted, Is.True, ended.Reason);
            Assert.That(Activations(ended.Events, key).Single().TraitActivationKind.ToString(), Is.EqualTo("StatusExpired"));
            var final = TraitCombatFixtures.Final(ended, 2);
            Assert.That(final.Debuffs.ArmorBreak.SameValues(before.Debuffs.ArmorBreak), Is.True);
            Assert.That(final.Debuffs.Disarm, Is.Null);
            Assert.That(final.Debuffs.AnkleBreaker, Is.Null);
            Assert.That(ended.Events.Any(e => e.Kind.ToString() == "TraitStateChanged" && State(e, "Final").EntityId.Value == 2), Is.True);
        }

        [Test]
        public void HunterState_HasSelectionRewardAndResetResults()
        {
            var start = EnemyControllerPhaseTests.Input(HunterTraitTests.Input(), CombatControllerPhase.StartingPlayer);
            var begun = CombatTurnCalculator.BeginControllerTurn(start, Array.Empty<IMovementTileRule>());
            Assert.That(begun.Accepted, Is.True, begun.Reason);
            var selection = begun.Events.ToList().FindIndex(e => HasActivation(e, TraitKey.Hunter, "HunterTargetSelected"));
            var selectedState = begun.Events.ToList().FindIndex(e => e.Kind.ToString() == "TraitStateChanged" && State(e, "Final").EntityId.Value == 1);
            Assert.That(selectedState, Is.GreaterThan(selection));
            var after = MovementTestData.After(begun);
            var target = after.Characters.Single(c => c.EntityId == HunterTraitTests.Target(HunterTraitTests.Actor(after)));
            var result = TraitCombatFixtures.Attack(CombatCommandTestData.FreshCommand(after, CombatCommandKind.Attack,
                new EntityId(1), ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.AttackAim.ForCell(target.Anchor.Value)));
            var reward = Activations(result.Events, TraitKey.Hunter).Single(a => a.TraitActivationKind == TraitActivationKind.AttackGranted);
            Assert.That(reward.TargetIds, Is.EqualTo(new[] { new EntityId(1) }));
            Assert.That(reward.Magnitude, Is.EqualTo(2));
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Count(a => a == reward), Is.EqualTo(1));
            var final = MovementTestData.After(result);
            Assert.That(HunterTraitTests.Bonus(final), Is.EqualTo(2));
            var events = new List<CombatEvent>();
            var cleared = CombatTemporaryStateCleanup.Clear(HunterTraitTests.Actor(final), events,
                ItemEventReason.BattleEnded);
            Assert.That(Activations(events, TraitKey.Hunter).Single().TraitActivationKind.ToString(), Is.EqualTo("StatusReset"));
            Assert.That(HunterTraitTests.Rewards(cleared), Is.Zero);
            Assert.That(HunterTraitTests.Bonus(final), Is.EqualTo(2));
        }

        [Test]
        public void UnchangedPresenceRefresh_DoesNotReplayAura()
        {
            var input = TraitShieldTests.Cohesion();
            var facts = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            CombatDamageApplication.For(input, facts, events).Apply(new EntityId(10), CombatDamageKind.EnvironmentDamage,
                new CombatDamageOrigin(CellCoord.Zero, null), 0);
            Assert.That(events, Is.Empty);
            Assert.That(facts[new EntityId(10)].TraitEffects.ActiveShieldTotal, Is.EqualTo(20));
        }

        [Test]
        public void StageCleanup_EmitsRetainedShieldSourcesOnce()
        {
            var input = TraitShieldTests.Cohesion();
            var before = input.Characters.Single(c => c.EntityId.Value == 10);
            var events = new List<CombatEvent>();
            var after = CombatTemporaryStateCleanup.Clear(before, events,
                ItemEventReason.BattleEnded);
            var removals = Activations(events, TraitKey.Cohesion).Where(a => a.TraitActivationKind.ToString() == "ShieldRemoved").ToArray();
            Assert.That(removals.Select(a => a.SourceUnitId), Is.EqualTo(new[] { new EntityId(1), new EntityId(5) }));
            Assert.That(after.TraitEffects.Shields, Is.Empty);
            Assert.That(events.Count(e => e.Kind.ToString() == "TraitStateChanged"), Is.EqualTo(1));
        }

        [TestCase(TraitKey.StationaryFire, false)]
        [TestCase(TraitKey.StationaryFire, true)]
        [TestCase(TraitKey.TrackingShot, false)]
        [TestCase(TraitKey.TrackingShot, true)]
        [TestCase(TraitKey.FocusedBombardment, false)]
        [TestCase(TraitKey.FocusedBombardment, true)]
        public void AppliedModifier_RecordsOnlyMeaningfulChange(TraitKey key, bool active)
        {
            TraitSpec spec = key switch
            {
                TraitKey.StationaryFire => new StationaryFireTraitSpec(50),
                TraitKey.TrackingShot => new TrackingShotTraitSpec(50, 50),
                _ => new FocusedBombardmentTraitSpec(active ? 1 : 2, 50)
            };
            var input = TraitCombatFixtures.Equip(TraitMovementAttackTests.SingleTarget(), EntityKey.Unit_Akimbo, spec);
            var cell = new CellCoord(2, 3);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: 20, traitStatuses: key == TraitKey.StationaryFire && !active
                    ? new TraitStatusSnapshot[] { new StationaryFireTraitStatusSnapshot(true) } : c.Stats.TraitStatuses.ToArray()))
                : c.EntityId.Value == 2 ? c.With(health: new HealthStateSnapshot(key == TraitKey.TrackingShot && !active ? 49 : 100, 100),
                    stats: c.Stats.With(attackPower: 1, physicalDefense: 0))
                    .WithPlacement(CharacterRuntimeArea.Field, null, cell, Rotation4.R0, new[] { cell }) : c).ToArray(),
                profiles: input.Profiles.Select(profile => profile.Definition.Key == EntityKey.Unit_Akimbo
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main, spec), profile.Method) : profile).ToArray());
            input = CombatAttackQueries.WithAim(input, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.AttackAim.ForCell(cell));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var values = Activations(result.Events, key).Where(a => a.TraitActivationKind.ToString() == "DamageModified" || a.TraitActivationKind.ToString() == "DefenseBypassed").ToArray();
            Assert.That(values.Length, Is.EqualTo(active ? 1 : 0));
            if (!active) return;
            Assert.That(values[0].Magnitude, Is.GreaterThan(0));
            Assert.That(values[0].TargetId, Is.EqualTo(new EntityId(2)));
            Assert.That(values[0].TargetCells, Does.Contain(cell));
            AssertOrigin(values[0].ResultOrigin, new CombatDamageOrigin(new CellCoord(1, 3), new EntityId(1), 1));
            var events = result.Events.ToList();
            Assert.That(events.FindIndex(e => e is CombatEffectsEvent t && t.Traits.Contains(values[0])),
                Is.LessThan(events.FindIndex(e => e is CombatCharacterEvent h && h.Kind == CombatEventKind.HealthChanged && h.Final.EntityId.Value == 2)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DistanceModifier_UsesAppliedDistanceEvidence(bool active)
        {
            var input = CombatTraitTestFixtures.FanAttackInput(nearHealth: 100);
            if (!active)
                input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                    ? c.With(stats: c.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new DistanceDamageScaleTraitStatusSnapshot(new[] { 100, 100 }) })) : c).ToArray(),
                    profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                        ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main,
                            new DistanceDamageScaleTraitSpec(new[] { 100, 100 })), p.Method) : p).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var values = Activations(result.Events, TraitKey.DistanceDamageScale).ToArray();
            Assert.That(values.Length, Is.EqualTo(active ? 2 : 0));
            if (active) Assert.That(values.Sum(a => a.Magnitude), Is.EqualTo(20));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PierceFalloff_OnlyChangedEncountersActivate(bool active)
        {
            var result = TraitCombatFixtures.Attack(PiercingShotTestData.Input(cap: active ? 3 : 1, damage: 20));
            Assert.That(result.Accepted, Is.True, result.Reason);
            var values = Activations(result.Events, TraitKey.PiercingFalloff).ToArray();
            Assert.That(values.Length, Is.EqualTo(active ? 2 : 0));
            if (active)
            {
                Assert.That(values.Sum(a => a.Magnitude), Is.EqualTo(18));
                Assert.That(values.All(a => a.ResultOrigin.AttackGroup == 1), Is.True);
                Assert.That(values.All(a => a.TargetId != new EntityId(9)), Is.True);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InstantDefeat_UsesAppliedCauseWithoutNumericModifier(bool executes)
        {
            var input = TraitCombatFixtures.Equip(TraitMovementAttackTests.SingleTarget(), EntityKey.Unit_Akimbo, new FinishingShotTraitSpec(20));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: 100)) : c.EntityId.Value == 2
                ? c.With(health: new HealthStateSnapshot(executes ? 19 : 20, 100), stats: c.Stats.With(physicalDefense: 100)) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 100, CategoryKey.Main, new FinishingShotTraitSpec(20)), p.Method) : p).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var values = Activations(result.Events, TraitKey.FinishingShot).ToArray();
            Assert.That(values.Length, Is.EqualTo(executes ? 1 : 0));
            if (executes)
            {
                Assert.That(values[0].TraitActivationKind.ToString(), Is.EqualTo("InstantDefeat"));
                Assert.That(values[0].Magnitude, Is.EqualTo(19));
                Assert.That(result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Died).DamageCause, Is.EqualTo(CombatDamageCause.InstantDefeat));
            }
        }

        [Test]
        public void RepeatedStrike_RemainsDistinctFromActionGrant()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method);
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.Owner == Owner.Enemy
                ? c.WithHealth(new HealthStateSnapshot(10, 100)) : c).Append(
                    CombatCommandTestData.Character(9, EntityKey.Unit_Demon, new CellCoord(7, 0), Owner.Enemy)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new VanguardTraitSpec(3, 1),
                new KillChainAttackTraitSpec(), new StationaryFireTraitSpec(100));
            var result = TraitCombatFixtures.Attack(input);
            var strikes = Activations(result.Events, TraitKey.StationaryFire).ToArray();
            Assert.That(strikes.Length, Is.GreaterThan(1));
            Assert.That(strikes.Select(a => a.ResultOrigin.AttackGroup).Distinct().Count(), Is.EqualTo(strikes.Length));
            Assert.That(Activations(result.Events, TraitKey.Vanguard).Count(a => a.TraitActivationKind == TraitActivationKind.AttackGranted), Is.EqualTo(1));
            // 처치 연쇄 추가 공격도 공격 행동으로 세지만 선봉장은 진영 첫 공격에만 발동한다(2026-11-02 결정).
            Assert.That(result.FinalState.TraitTurn.PlayerAttackActionCount,
                Is.EqualTo(1 + Activations(result.Events, TraitKey.KillChainAttack).Count()));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void IncomingModifier_RetainsActualSourceAndAppliedPercentage(bool active)
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.None, 0)), EntityKey.Unit_Demon, new EntrenchmentTraitSpec(1, 50));
            if (!active) input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.EntityId.Value != 3 && c.EntityId.Value != 4).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var values = Activations(result.Events, TraitKey.Entrenchment).ToArray();
            Assert.That(values.Length, Is.EqualTo(active ? 2 : 0));
            if (active)
            {
                Assert.That(values.Select(a => a.SourceUnitId), Is.EqualTo(new[] { new EntityId(3), new EntityId(4) }));
                Assert.That(values.Sum(a => a.Magnitude), Is.EqualTo(15));
                Assert.That(values.All(a => a.ResultOrigin.CreditedCharacterId == new EntityId(1)), Is.True);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DedicatedResult_HasOnePrecedingActivation(bool moves)
        {
            var result = TraitCombatFixtures.Attack(PullTestData.Input(distance: moves ? 2 : 1));
            Assert.That(result.Accepted, Is.True, result.Reason);
            var values = Activations(result.Events, TraitKey.Pull).ToArray();
            Assert.That(values.Length, Is.EqualTo(moves ? 1 : 0));
            if (!moves) return;
            var events = result.Events.ToList();
            int effect = events.FindIndex(e => HasActivation(e, TraitKey.Pull, "PullApplied"));
            int dedicated = events.FindIndex(e => e is CombatPullEvent);
            Assert.That(effect, Is.LessThan(dedicated));
            Assert.That(values[0].TargetId, Is.EqualTo(new EntityId(2)));
            Assert.That(values[0].Magnitude, Is.EqualTo(1));
            Assert.That(values[0].ResultOrigin.CreditedCharacterId, Is.EqualTo(new EntityId(1)));
        }

        [Test]
        public void ExposureApplyAndLaterDamage_KeepOriginalOwnerAndActualCreditor()
        {
            var input = MarkInput();
            var applied = TraitCombatFixtures.Attack(input);
            Assert.That(applied.Accepted, Is.True, applied.Reason);
            var first = Activations(applied.Events, TraitKey.HuntersMark).Single();
            Assert.That(first.TraitActivationKind, Is.EqualTo(TraitActivationKind.StatusApplied));
            Assert.That(first.SourceUnitId, Is.EqualTo(new EntityId(1)));
            Assert.That(first.TargetId, Is.EqualTo(new EntityId(2)));
            Assert.That(applied.Events.ToList().FindIndex(e => e is CombatEffectsEvent x && x.Traits.Contains(first)),
                Is.LessThan(applied.Events.ToList().FindIndex(e => e is CombatTraitStateEvent state && state.Final.EntityId == first.TargetId)));
            var after = MovementTestData.After(applied);
            var later = TraitCombatFixtures.Attack(CombatCommandTestData.FreshCommand(after, CombatCommandKind.Attack,
                new EntityId(5), AttackAim.ForCell(new CellCoord(4, 3))));
            Assert.That(later.Accepted, Is.True, later.Reason);
            Assert.That(Activations(later.Events, TraitKey.HuntersMark), Is.Empty);
            var retained = HuntersMarkAttackTests.Exposures(TraitCombatFixtures.Final(later, 2)).Single();
            Assert.That(retained.Origin.Value.OwnerInstanceId, Is.EqualTo(1));
            Assert.That(retained.IncreasePercent, Is.EqualTo(10));
            Assert.That(retained.RemainingAffectedSideTurnEnds, Is.EqualTo(1));
            var damage = later.Events.OfType<CombatCharacterEvent>().Single(e => e.Final.EntityId.Value == 2 && e.DamageOrigins.Any() && e.Final.Health.CurrentHealth < e.Expected.Health.CurrentHealth);
            Assert.That(damage.Expected.Health.CurrentHealth - damage.Final.Health.CurrentHealth, Is.EqualTo(22));
            Assert.That(damage.DamageOrigins.Single().CreditedCharacterId, Is.EqualTo(new EntityId(5)));
            Assert.That(later.Events.OfType<CombatHuntersMarkEvent>(), Is.Empty);
        }

        [Test]
        public void SameExposureSource_UnchangedRefreshDoesNotActivate()
        {
            var first = TraitCombatFixtures.Attack(MarkInput());
            var after = CombatCommandTestData.NextPlayerTurn(MovementTestData.After(first));
            var second = TraitCombatFixtures.Attack(CombatCommandTestData.FreshCommand(after, CombatCommandKind.Attack,
                new EntityId(1), AttackAim.ForCell(new CellCoord(4, 3))));
            Assert.That(second.Accepted, Is.True, second.Reason);
            Assert.That(Activations(second.Events, TraitKey.HuntersMark), Is.Empty);
            var retained = HuntersMarkAttackTests.Exposures(TraitCombatFixtures.Final(second, 2)).Single();
            Assert.That(retained.Origin.Value.OwnerInstanceId, Is.EqualTo(1));
            Assert.That(retained.RemainingAffectedSideTurnEnds, Is.EqualTo(1));
        }

        [Test]
        public void Intimidation_ApplyAndActualAttackConsumeRetainCaster()
        {
            var input = IntimidationAttackTests.Caster();
            input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.EntityId.Value != 3 && c.EntityId.Value != 4)
                .Select(c => c.EntityId.Value == 2 ? c.With(stats: c.Stats.With(attackPower: 20)) : c).ToArray());
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Demon
                ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Demon, 20), p.Method) : p).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var applied = Activations(result.Events, TraitKey.Intimidation).Single();
            Assert.That(applied.SourceUnitId, Is.EqualTo(new EntityId(1)));
            Assert.That(applied.Magnitude, Is.EqualTo(20));
            Assert.That(applied.TargetId, Is.EqualTo(new EntityId(2)));
            Assert.That(applied.TraitActivationKind, Is.EqualTo(TraitActivationKind.StatusApplied));
            var next = CombatCommandCalculator.Advance(EnemyControllerPhaseTests.Input(MovementTestData.After(result), CombatControllerPhase.Enemy), Array.Empty<IMovementTileRule>());
            Assert.That(next.Accepted, Is.True, next.Reason);
            var consumed = Activations(next.Events, TraitKey.Intimidation).Single(a => a.TraitActivationKind == TraitActivationKind.StatusConsumed);
            Assert.That(consumed.SourceUnitId, Is.EqualTo(new EntityId(1)));
            Assert.That(consumed.TargetId, Is.EqualTo(new EntityId(2)));
            Assert.That(consumed.ResultOrigin.CreditedCharacterId, Is.EqualTo(new EntityId(2)));
            Assert.That(TraitCombatFixtures.Final(next, 2).Debuffs.Intimidation, Is.Null);
        }

        [TestCase(TraitKey.Knockback)]
        [TestCase(TraitKey.ImpactBolt)]
        [TestCase(TraitKey.SteadfastWill)]
        public void Displacement_UsesAppliedOrPreventedDecision(TraitKey key)
        {
            var input = key == TraitKey.ImpactBolt ? ImpactBoltTests.Input() : DisplacementInput(key == TraitKey.SteadfastWill);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var values = Activations(result.Events, key).ToArray();
            Assert.That(values.Length, Is.GreaterThan(0));
            Assert.That(values.All(a => a.ResultOrigin?.CreditedCharacterId == new EntityId(1)), Is.True);
            if (key == TraitKey.SteadfastWill)
            {
                Assert.That(values.Single().TraitActivationKind, Is.EqualTo(TraitActivationKind.KnockbackPrevented));
                Assert.That(values.Single().SourceUnitId, Is.EqualTo(new EntityId(2)));
                Assert.That(TraitCombatFixtures.Final(result, 2).Anchor, Is.EqualTo(new CellCoord(4, 3)));
            }
            else Assert.That(values.Any(a => a.TraitActivationKind == TraitActivationKind.KnockbackApplied && a.Magnitude > 0), Is.True);
        }

        [Test]
        public void Steadfast_OrdinaryDamageDoesNotActivate()
        {
            var input = TraitCombatFixtures.Equip(TraitMovementAttackTests.SingleTarget(), EntityKey.Unit_Demon, new SteadfastWillTraitSpec());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Activations(result.Events, TraitKey.SteadfastWill), Is.Empty);
        }

        [TestCase(TraitKey.KillChainAttack)]
        public void FollowUp_RetainsItsRealOriginAndResolvedAim(TraitKey key)
        {
            var input = CombatTraitTestFixtures.KillChainInput();
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var values = Activations(result.Events, key).Where(a => a.TraitActivationKind == TraitActivationKind.FollowUpAttack ||
                a.TraitActivationKind == TraitActivationKind.KillChainAttack).ToArray();
            Assert.That(values.Length, Is.GreaterThan(0));
            foreach (var value in values)
            {
                Assert.That(value.ResultOrigin, Is.Not.Null);
                Assert.That(value.ResultOrigin.CreditedCharacterId, Is.EqualTo(value.SourceUnitId));
                Assert.That(value.ResultOrigin.AttackGroup, Is.GreaterThan(1));
                Assert.That(value.AttackDirection.HasValue, Is.True);
                Assert.That(value.TargetCell.HasValue, Is.True);
            }
        }


        [TestCase(true)]
        [TestCase(false)]
        public void Roar_BarrelOnly_UsesDirectStructureOrigin(bool explosion)
        {
            var input = BossRoarTests.Input(5, barrel: true, withTargets: false);
            if (!explosion)
            {
                var s = input.State.Structures.Single();
                var definition = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 1, false, 0);
                var ordinary = new StructureCombatFacts(s.EntityId, definition, s.Health, s.Anchor, s.Rotation, s.OccupiedCells, Owner.Player);
                input = ObstacleReactionTests.Copy(input, structures: new[] { ordinary }, profiles: Array.Empty<ObstacleContentProfile>());
            }
            var boss = input.Characters.Single(c => c.Owner == Owner.Enemy);
            var result = BossRoarCalculator.TryRoar(input, boss.EntityId, Array.Empty<IMovementTileRule>());
            var value = Activations(result.Events, TraitKey.Roar).Single();
            var direct = explosion
                ? result.Events.OfType<CombatObstacleDurabilityEvent>().Single().Origin
                : result.Events.OfType<CombatStructureEvent>().Single().DamageOrigins.Single();
            AssertOrigin(value.ResultOrigin, direct);
            Assert.That(direct.PhysicalCell, Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(direct.CreditedCharacterId, Is.EqualTo(boss.EntityId));
            Assert.That(direct.AttackGroup, Is.EqualTo(1));
            if (explosion)
            {
                var secondary = result.Events.OfType<CombatObstacleReactionEvent>().Single().Origin;
                Assert.That(secondary.PhysicalCell, Is.EqualTo(new CellCoord(3, 2)));
                Assert.That(secondary.CreditedCharacterId, Is.EqualTo(boss.EntityId));
                Assert.That(secondary.AttackGroup, Is.EqualTo(2));
            }
            Assert.That(result.FinalState.Structures.Single().Health.IsDead, Is.True);
            var reset = result.Events.OfType<CombatTraitStateEvent>().First(e => e.Final.EntityId == boss.EntityId);
            Assert.That(reset.Final.Stats.TraitStatuses.ToArray().OfType<RoarTraitStatusSnapshot>().Single().HitCount, Is.Zero);
            Assert.That(BossRoarTests.Final(result, boss.EntityId).Stats.TraitStatuses.ToArray().OfType<RoarTraitStatusSnapshot>().Single().HitCount,
                Is.EqualTo(explosion ? 1 : 0));
            Assert.That(BossRoarTests.Final(result, boss.EntityId).Health.CurrentHealth, Is.EqualTo(explosion ? 93 : 100));
            Assert.That(result.Events.ToList().FindIndex(e => HasActivation(e, TraitKey.Roar, "RoarTriggered")),
                Is.LessThan(result.Events.ToList().FindIndex(e => e is CombatRoarEvent)));
        }

        [TestCase(3, false)]
        [TestCase(4, true)]
        public void Roar_AppliedCounterDecisionHasOnePrecedingActivation(int hits, bool triggers)
        {
            var result = TraitCombatFixtures.Attack(BossRoarTests.Input(hits));
            Assert.That(result.Accepted, Is.True, result.Reason);
            var values = Activations(result.Events, TraitKey.Roar).ToArray();
            Assert.That(values.Length, Is.EqualTo(triggers ? 1 : 0));
            if (!triggers) return;
            var events = result.Events.ToList();
            int at = events.FindIndex(e => HasActivation(e, TraitKey.Roar, "RoarTriggered"));
            int dedicated = events.FindIndex(e => e is CombatRoarEvent);
            Assert.That(at, Is.LessThan(dedicated));
            Assert.That(values[0].TargetCells, Is.EqualTo(((CombatRoarEvent)events[dedicated]).AreaCells));
            AssertOrigin(values[0].ResultOrigin, result.Events.OfType<CombatCharacterEvent>()
                .SelectMany(e => e.DamageOrigins).First(o => o.CreditedCharacterId == values[0].SourceUnitId));
        }

        [Test]
        public void EmptyRoar_CounterResetIsStillMeaningful()
        {
            var input = BossRoarTests.Input(5, withTargets: false);
            var result = BossRoarCalculator.TryRoar(input, input.Characters.Single(c => c.Owner == Owner.Enemy).EntityId, Array.Empty<IMovementTileRule>());
            var value = Activations(result.Events, TraitKey.Roar).Single();
            Assert.That(value.TraitActivationKind, Is.EqualTo(TraitActivationKind.RoarTriggered));
            Assert.That(value.TargetIds, Is.Empty);
            Assert.That(value.ResultOrigin, Is.Null);
            Assert.That(result.Events.OfType<CombatRoarEvent>().Single().Attack, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Taunt_OnlyAnAppliedForcedDecisionActivates(bool active)
        {
            var input = TraitCombatFixtures.Equip(ThornHeartTestData.Input(heart: false, hp: 100), EntityKey.Unit_Demon, new TauntTraitSpec(2));
            var cell = active ? new CellCoord(2, 3) : new CellCoord(4, 3);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithPlacement(CharacterRuntimeArea.Field, null, cell, Rotation4.R0, new[] { cell }) : c).ToArray());
            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var values = Activations(result.Events, TraitKey.Taunt)
                .Where(a => a.TraitActivationKind == TraitActivationKind.TauntSelected).ToArray();
            Assert.That(values.Length, Is.EqualTo(active ? 1 : 0));
            if (!active) return;
            Assert.That(values[0].SourceUnitId, Is.EqualTo(new EntityId(2)));
            Assert.That(values[0].TargetId, Is.EqualTo(new EntityId(1)));
            Assert.That(values[0].TraitActivationKind, Is.EqualTo(TraitActivationKind.TauntSelected));
            Assert.That(values[0].ResultOrigin?.CreditedCharacterId, Is.EqualTo(new EntityId(1)));
        }

        private static CombatCommandInput MarkInput()
        {
            var input = CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(5, EntityKey.Unit_Cowboy, new CellCoord(1, 4), Owner.Player, 20)).ToArray());
            return CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new HuntersMarkTraitStatusSnapshot(10, 1) })) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main, new HuntersMarkTraitSpec(10, 1)), p.Method) : p)
                    .Append(new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Cowboy, 20), input.Profiles[0].Method)).ToArray());
        }

        [Test]
        public void Sacrifice_ActivationPrecedesConsumptionAndDamage_WithActualIncomingOrigin()
        {
            var input = SacrificeTraitTests.Input();
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var occurrence = Interceptions(result).Single();
            var value = occurrence.Value;
            int at = occurrence.Index;
            var hp = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Final.EntityId == value.SourceUnitId && e.DamageOrigins.Count > 0);
            Assert.That(at, Is.LessThan(result.Events.ToList().IndexOf(hp)));
            var state = result.Events.OfType<CombatTraitStateEvent>().First(e => e.Final.EntityId == value.SourceUnitId);
            Assert.That(at, Is.LessThan(result.Events.ToList().IndexOf(state)));
            Assert.That(state.Expected.Stats.TraitStatuses.ToArray().OfType<SacrificeTraitStatusSnapshot>().Single().ActivationsThisEnemyTurn,
                Is.LessThan(state.Final.Stats.TraitStatuses.ToArray().OfType<SacrificeTraitStatusSnapshot>().Single().ActivationsThisEnemyTurn));
            Assert.That(value.Magnitude, Is.EqualTo(hp.Expected.Health.CurrentHealth - hp.Final.Health.CurrentHealth));
            AssertOrigin(value.ResultOrigin, hp.DamageOrigins.Single());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DustIncoming_UsesRetainedTrailOwnerAndActualDamageOrigin(bool active)
        {
            var input = TraitMovementAttackTests.SingleTarget();
            var target = input.Characters.Single(c => c.EntityId.Value == 2);
            input = TraitTrailTestData.Environment(input, TraitTrailTestData.Trail(60, target.Anchor.Value,
                TraitKey.DustScreen, percent: active ? 50 : 0, source: 5));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var values = Activations(result.Events, TraitKey.DustScreen).ToArray();
            Assert.That(values.Length, Is.EqualTo(active ? 1 : 0));
            if (active)
            {
                Assert.That(values[0].SourceUnitId.Value, Is.EqualTo(5));
                Assert.That(values[0].ResultOrigin.CreditedCharacterId.Value.Value, Is.EqualTo(1));
                Assert.That(values[0].Magnitude, Is.EqualTo(10));
            }
        }

        [Test]
        public void Steadfast_AlreadyExplicitlyImmuneDoesNotClaimTheDecision()
        {
            var input = DisplacementInput(true);
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Demon
                ? new CombatAttackProfile(ImpactBoltTests.CopySpec(p.Definition, behavior: new UnitBehaviorSpec(true, true, true)), p.Method) : p).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Activations(result.Events, TraitKey.SteadfastWill), Is.Empty);
            Assert.That(result.Attack.Value.Knockbacks.Single().Stop.ToString(), Is.EqualTo("Immune"));
        }
        [Test]
        public void Morale_OneAttributedKillGroupsRecipientsBeforeTheirStates()
        {
            var input = TraitCombatFixtures.Equip(KillInput(50), EntityKey.Unit_Akimbo,
                new CallOfBloodTraitSpec(10), new MoraleBoostTraitSpec(20, 1));
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 2 ? c.With(stats: c.Stats.With(attackPower: 13)) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            var value = Activations(result.Events, TraitKey.MoraleBoost).Single(a => a.TraitActivationKind == TraitActivationKind.AttackGranted);
            Assert.That(value.TargetIds.Select(id => id.Value), Is.EquivalentTo(new[] { 1, 10 }));
            Assert.That(value.ResultOrigin.CreditedCharacterId.Value.Value, Is.EqualTo(1));
            int at = result.Events.ToList().FindIndex(e => HasActivation(e, TraitKey.MoraleBoost, "AttackGranted"));
            Assert.That(result.Events.Take(at).OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Died), Is.True);
            foreach (var id in value.TargetIds) Assert.That(result.Events.Skip(at + 1).OfType<CombatTraitStateEvent>().Any(e => e.Final.EntityId == id), Is.True);
        }
        [Test]
        public void SoloPresence_EmitsOneSourceGrantAndNoUnchangedReplay()
        {
            var input = TraitCombatFixtures.Equip(TraitMovementAttackTests.SingleTarget(), EntityKey.Unit_Akimbo, new SoloActionTraitSpec(10, 1, 0, 1));
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var events = new System.Collections.Generic.List<CombatEvent>();
            TraitPresenceReconciler.ApplyAndRecord(input, characters, events);
            var first = Activations(events, TraitKey.SoloAction).Single();
            Assert.That(first.Magnitude, Is.EqualTo(10));
            Assert.That(first.SourceUnitId.Value, Is.EqualTo(1));
            int count = events.Count;
            TraitPresenceReconciler.ApplyAndRecord(input, characters, events);
            Assert.That(events.Count, Is.EqualTo(count));
        }

        [Test]
        public void ZeroArmorContribution_DoesNotActivateOnApplyOrCleanup()
        {
            var input = TraitCombatFixtures.Equip(TraitMovementAttackTests.SingleTarget(), EntityKey.Unit_Akimbo, new ArmorBreakerTraitSpec(0));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Activations(result.Events, TraitKey.ArmorBreaker), Is.Empty);
            var events = new System.Collections.Generic.List<CombatEvent>();
            CombatTemporaryStateCleanup.Clear(TraitCombatFixtures.Final(result, 2), events, ItemEventReason.BattleEnded);
            Assert.That(Activations(events, TraitKey.ArmorBreaker), Is.Empty);
        }
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void Sacrifice_DefenseStatesDoNotMoveActivationAfterConsumption(bool armor, bool shield)
        {
            var result = TraitCombatFixtures.Attack(RedirectInput(false, armor, shield));
            Assert.That(result.Accepted, Is.True, result.Reason);
            AssertRedirectOrder(result, new EntityId(5));
        }
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void KillChain_IdentityPrecedesRedirectAndConsumption(bool armor, bool shield)
        {
            var basis = RedirectInput(true, armor, shield);
            CombatCommandTransition selected = null;
            for (uint seed = 1; seed <= 32 && selected == null; seed++)
            {
                var progress = basis.State.EnvironmentProgress;
                var input = CombatCommandTestData.With(basis, state: basis.State.With(environmentProgress:
                    new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, seed)));
                var candidate = TraitCombatFixtures.Attack(input);
                Assert.That(candidate.Accepted, Is.True, candidate.Reason);
                if (Activations(candidate.Events, TraitKey.KillChainAttack).Any(a => a.TargetId == new EntityId(3))) selected = candidate;
            }
            Assert.That(selected, Is.Not.Null, "A fixed finite seed range must include the protected child recipient.");
            var child = Activations(selected.Events, TraitKey.KillChainAttack).Single(a => a.TargetId == new EntityId(3));
            var redirect = child.ChildAttack.Reactions.Single(a => a.TargetId == new EntityId(3));
            Assert.That(redirect.ResultOrigin.AttackGroup, Is.EqualTo(child.ResultOrigin.AttackGroup));
            int identity = selected.Events.ToList().FindIndex(e => e is CombatEffectsEvent f && f.Traits.Contains(child));
            int activation = Interceptions(selected).Single(a => ReferenceEquals(a.Value, redirect)).Index;
            Assert.That(identity, Is.EqualTo(activation));
            Assert.That(selected.Attack.Value.TraitActivations.ToArray().Contains(redirect), Is.False);
            AssertRedirectOrder(selected, new EntityId(5));
        }
        [TestCase(TraitKey.Vanguard, 1)]
        [TestCase(TraitKey.Vanguard, 2)]
        [TestCase(TraitKey.MoraleBoost, 1)]
        [TestCase(TraitKey.MoraleBoost, 2)]
        public void ZeroGrant_DoesNotPublishCountdownExpiryOrRemoval(TraitKey key, int duration)
        {
            var input = key == TraitKey.Vanguard ? TraitCombatFixtures.Equip(GrantInput(), EntityKey.Unit_Akimbo, new VanguardTraitSpec(0, duration))
                : TraitCombatFixtures.Equip(KillInput(50), EntityKey.Unit_Akimbo, new MoraleBoostTraitSpec(1, duration));
            if (key == TraitKey.MoraleBoost) input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 2
                ? c.With(stats: c.Stats.With(attackPower: 1)) : c).ToArray());
            var attack = TraitCombatFixtures.Attack(input);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(Activations(attack.Events, key), Is.Empty);
            var ended = CombatTurnCalculator.EndPlayerTurn(CombatCommandComposition.Project(attack), Array.Empty<IMovementTileRule>());
            Assert.That(ended.Accepted, Is.True, ended.Reason);
            Assert.That(Activations(ended.Events, key), Is.Empty);
            var events = new System.Collections.Generic.List<CombatEvent>();
            foreach (var character in CombatCommandComposition.Project(attack).Characters)
                CombatTemporaryStateCleanup.Clear(character, events, ItemEventReason.BattleEnded);
            Assert.That(Activations(events, key), Is.Empty);
        }
        [Test]
        public void Sacrifice_NewEnemyTurnWithSameCountUsesItsConsumptionFact()
        {
            var input = RedirectInput(false, true, true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 5
                ? c.With(stats: c.Stats.With(traitStatuses: c.Stats.TraitStatuses.ToArray().Select(t => t.Key == TraitKey.Sacrifice
                    ? (TraitStatusSnapshot)new SacrificeTraitStatusSnapshot(input.State.Turn.ControllerTurnSequence - 1, 1) : t).ToArray())) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            AssertRedirectOrder(result, new EntityId(5));
        }
        private static CombatCommandInput RedirectInput(bool chain, bool armor, bool shield)
        {
            var input = SacrificeTraitTests.Input();
            var key = input.Characters.Single(c => c.EntityId == input.ActorId).CharacterKey;
            var traits = new System.Collections.Generic.List<TraitSpec>();
            if (chain) traits.Add(new KillChainAttackTraitSpec());
            if (armor) traits.Add(new ArmorBreakerTraitSpec(50));
            input = TraitCombatFixtures.Equip(input, key, traits.ToArray());
            if (chain)
            {
                input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Shielder, new SacrificeTraitSpec(1, 1));
                input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.EntityId.Value != 3).Select(c => c.EntityId.Value == 5
                    ? c.WithPlacement(c.Area, null, new CellCoord(6, 5), c.Rotation, new[] { new CellCoord(6, 5) }) : c)
                    .Append(CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(6, 4), Owner.Player)
                        .WithHealth(new HealthStateSnapshot(5, 100))).ToArray());
            }
            if (shield) input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new CohesionTraitSpec(0, 10, 1, 1));
            return TraitShieldTests.Reconcile(input);
        }
        private static System.Collections.Generic.IEnumerable<(TraitActivationSnapshot Value, int Index)> Interceptions(CombatCommandTransition result)
        {
            for (int i = 0; i < result.Events.Count; i++)
            {
                var e = result.Events[i];
                var values = e is CombatAttackEvent attack ? attack.Attack.TraitActivations.ToArray()
                    : e is CombatRoarEvent roar && roar.Attack.HasValue ? roar.Attack.Value.TraitActivations.ToArray()
                    : e is CombatEffectsEvent effects ? effects.Traits.Where(t => t.ChildAttack != null).SelectMany(t => t.ChildAttack.Reactions).ToArray()
                    : Array.Empty<TraitActivationSnapshot>();
                foreach (var value in values.Where(t => t.Interception != null)) yield return (value, i);
            }
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Any(t => t.Interception != null), Is.False);
        }

        private static void AssertRedirectOrder(CombatCommandTransition result, EntityId source)
        {
            var occurrence = Interceptions(result).Single(a => a.Value.SourceUnitId == source);
            var value = occurrence.Value;
            var events = result.Events.ToList();
            int activation = occurrence.Index;
            int consumed = events.FindIndex(e => e is CombatTraitStateEvent state && state.Final.EntityId == source &&
                !state.Expected.Stats.TraitStatuses.ToArray().OfType<SacrificeTraitStatusSnapshot>().Single().SameValues(
                    state.Final.Stats.TraitStatuses.ToArray().OfType<SacrificeTraitStatusSnapshot>().Single()));
            int hp = events.FindIndex(e => e is CombatCharacterEvent state && state.Final.EntityId == source && state.DamageOrigins.Any());
            Assert.That(consumed, Is.GreaterThanOrEqualTo(0));
            Assert.That(hp, Is.GreaterThanOrEqualTo(0));
            Assert.That(activation, Is.LessThan(consumed));
            Assert.That(activation, Is.LessThan(hp));
        }

        private static CombatCommandInput DisplacementInput(bool steadfast)
        {
            var input = CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0), empty: true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy)).ToArray());
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(1) })) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main, new KnockbackTraitSpec(1)), p.Method) : p).ToArray());
            return steadfast ? TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new SteadfastWillTraitSpec()) : input;
        }

        internal static IEnumerable<TraitActivationSnapshot> Activations(IEnumerable<CombatEvent> events, TraitKey key) =>
            events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Where(a => a.TraitKey == key);
        internal static bool HasActivation(CombatEvent e, TraitKey key, string kind) => e is CombatEffectsEvent effects &&
            effects.Traits.Any(a => a.TraitKey == key && a.TraitActivationKind.ToString() == kind);
        private static CharacterCombatFacts State(CombatEvent value, string property) =>
            (CharacterCombatFacts)value.GetType().GetProperty(property).GetValue(value);
        private static void AssertOrigin(CombatDamageOrigin actual, CombatDamageOrigin expected)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.PhysicalCell, Is.EqualTo(expected.PhysicalCell));
            Assert.That(actual.CreditedCharacterId, Is.EqualTo(expected.CreditedCharacterId));
            Assert.That(actual.AttackGroup, Is.EqualTo(expected.AttackGroup));
        }
        private static CombatCommandInput GrantInput()
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, new VanguardTraitSpec(3, 1));
            return TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(5, EntityKey.Unit_Demon, new CellCoord(0, 0), Owner.Player)).ToArray());
        }
        private static CombatCommandInput KillInput(int health)
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, new CallOfBloodTraitSpec(10));
            return TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 1
                ? c.WithHealth(new HealthStateSnapshot(health, 100)) : c.EntityId.Value == 2
                ? c.WithHealth(new HealthStateSnapshot(1, 100)) : c).ToArray());
        }
    }
}
