using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;

namespace ProjectL.Tests.Core
{
    public sealed class ThornHeartReflectionDirectTests
    {
        [Test]
        public void FirstQualifyingZeroDamageHit_Reflects()
        {
            var input = ThornHeartTestData.Replace(ThornHeartTestData.Input(heart: false), 2,
                c => HeldEnvironmentEffects.AcquireBadge(c, 41));
            var (plan, world, ledger, delivered) = Prepare(input);
            Assert.That(Apply(plan, world, ledger, delivered, Scope()), Is.False);
            Assert.That(world.Characters[new EntityId(2)].Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(world.Characters[new EntityId(2)].HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(world.Characters[input.ActorId].Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(world.Events.OfType<CombatArtifactEvent>().Single().Reflection.StruckAllyId.Value, Is.EqualTo(2));
        }

        [Test]
        public void LethalAllyHit_StillReflectsAfterItsDeath()
        {
            var input = ThornHeartTestData.Replace(ThornHeartTestData.Input(heart: false), 2,
                c => CombatCommandTestData.With(c, hp: 1));
            var (plan, world, ledger, delivered) = Prepare(input);
            Apply(plan, world, ledger, delivered, Scope());
            Assert.That(world.Characters[new EntityId(2)].Health.IsDead, Is.True);
            Assert.That(world.Characters[input.ActorId].Health.CurrentHealth, Is.EqualTo(7));
            int died = world.Events.FindIndex(e => e is CombatCharacterEvent c && c.Kind == CombatEventKind.Died);
            int reflected = world.Events.FindIndex(e => e is CombatArtifactEvent);
            Assert.That(reflected, Is.EqualTo(died + 1));
        }

        [Test]
        public void MultipleTargets_OnlyFirstReflects_ThenRemainingHealthFollows()
        {
            var (plan, world, ledger, delivered) = Prepare(ThornHeartTestData.Input(heart: false, splash: true));
            Apply(plan, world, ledger, delivered, Scope());
            Assert.That(world.Events.OfType<CombatArtifactEvent>().Count(), Is.EqualTo(1));
            Assert.That(world.Characters[plan.Actor.EntityId].Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(delivered.Select(id => id.Value), Is.EquivalentTo(new[] { 2, 3 }));
            Assert.That(world.Events.OfType<CombatCharacterEvent>().Select(e => e.Final.EntityId.Value), Is.EqualTo(new[] { 2, 1, 3 }));
        }

        [Test]
        public void RepeatedScopeHit_OnlyOnce()
        {
            var (plan, world, ledger, delivered) = Prepare(ThornHeartTestData.Input(heart: false));
            var scope = Scope();
            Apply(plan, world, ledger, delivered, scope);
            Apply(plan, world, new CommandAttackLedger(), delivered, scope);
            Assert.That(world.Events.OfType<CombatArtifactEvent>().Count(), Is.EqualTo(1));
            Assert.That(world.Characters[plan.Actor.EntityId].Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(world.Characters[new EntityId(2)].Health.CurrentHealth, Is.EqualTo(60));
        }

        [Test]
        public void NonPiercingReflectionKill_LeavesLaterTargetsUntouched()
        {
            var input = ThornHeartTestData.Replace(ThornHeartTestData.Input(heart: false, splash: true, hp: 2), 3,
                c => HeldEnvironmentEffects.AcquireBadge(CombatCommandTestData.With(c,
                    traits: new TraitStatusSnapshot[] { new RoarTraitStatusSnapshot(1, 5) }), 41));
            var (plan, world, ledger, delivered) = Prepare(input);
            Assert.That(Apply(plan, world, ledger, delivered, Scope()), Is.True);
            var later = world.Characters[new EntityId(3)];
            Assert.That(later.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(later.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(RoarHitCounter.HitCount(later), Is.EqualTo(1));
            Assert.That(delivered.Select(id => id.Value), Is.EqualTo(new[] { 2 }));
            Assert.That(ledger.Roles.Keys.Select(id => id.Value), Is.EqualTo(new[] { 2 }));
            Assert.That(world.DeathAnchors[plan.Actor.EntityId], Is.EqualTo(new CellCoord(1, 3)));
        }

        [Test]
        public void PiercingFirstHitReflectionKill_CompletesAllEncounters()
        {
            var (plan, world, ledger, delivered) = Prepare(ThornHeartTestData.Input(heart: false, piercing: true, hp: 2));
            Assert.That(Apply(plan, world, ledger, delivered, Scope()), Is.True);
            Assert.That(world.Characters[plan.Actor.EntityId].Anchor, Is.Null);
            Assert.That(world.Characters[new EntityId(2)].Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(world.Characters[new EntityId(3)].Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(world.Events.OfType<CombatArtifactEvent>().Count(), Is.EqualTo(1));
            Assert.That(ledger.Roles.Keys, Is.EquivalentTo(plan.Delivery.PiercingShot.Encounters.ToArray().Select(e => e.TargetId)));
            ledger.CaptureHitHealth(world);
            var result = AttackResultFactory.Create(plan, world, ledger, new UnitActionUseSettlementSnapshot(new EntityId(1),UnitActionUse.Available,UnitActionUse.Attacked),
                Array.Empty<TraitActivationSnapshot>(), Array.Empty<CategoryActivationSnapshot>(), out _);
            Assert.That(result.PiercingShot, Is.SameAs(plan.Delivery.PiercingShot));
            Assert.That(result.Hits.Length, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NoActiveReflection_PreservesLegacyHeldBeforeHealthOrder(bool disabledScope)
        {
            var input = ThornHeartTestData.Replace(ThornHeartTestData.Input(heart: false, splash: true), 3,
                c => HeldEnvironmentEffects.AcquireBadge(c, 41));
            var (plan, world, ledger, delivered) = Prepare(input);
            Apply(plan, world, ledger, delivered, disabledScope ? Scope(false) : null);
            Assert.That(world.Events.Select(e => e.Kind), Is.EqualTo(new[]
                { CombatEventKind.HeldEnvironmentChanged, CombatEventKind.HealthChanged }));
            Assert.That(world.Characters[plan.Actor.EntityId].Health.CurrentHealth, Is.EqualTo(10));
        }

        private static ThornHeartAttackScope Scope(bool enabled = true) =>
            new ThornHeartAttackScope(ArtifactEffectCompiler.Compile(ThornHeartTestData.Heart()), enabled, true);

        private static bool Apply(CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger,
            ISet<EntityId> delivered, ThornHeartAttackScope scope)
        {
            return AttackDirectDamageCalculator.Apply(plan, world, ledger, delivered, scope);
        }

        private static (CommandAttackPlan, CommandAttackProjection, CommandAttackLedger, HashSet<EntityId>) Prepare(CombatCommandInput input)
        {
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            var environment = input.State.Environment;
            var walls = WallGeometryBuilder.Build(8, 8, environment, input.EnvironmentBindings, input.State.WallBlocking);
            var selection = CombatAttackQueries.Selection(input, actor, profile, environment: environment, walls: walls);
            Assert.That(selection.FailureReason, Is.EqualTo(AttackFailureReason.None));
            var engine = new CombatEffectEngine();
            var bindings = CombatCommandEffects.Capture(actor, profile);
            int damage = CombatAttackCalculator.CommonAttackDamage(input, actor, profile, CombatDamageKind.OrdinaryAttack);
            var delivery = CombatAttackDelivery.Resolve(input, actor, profile, selection.Selection, damage, walls, AttackElevation.From(environment, input.EnvironmentBindings));
            var plan = new CommandAttackPlan(input,Array.Empty<IMovementTileRule>(),profile,actor,actor,environment,walls,                selection.Selection,delivery,engine,bindings,                new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())),damage,Rotation4.R0,false);
            return (plan, new CommandAttackProjection(input, environment, selection.BlockedContacts), new CommandAttackLedger(), new HashSet<EntityId>());
        }
    }

    internal static class ThornHeartTestData
    {
        internal static ArtifactLoadoutSnapshot Heart()
        {
            var effect = ArtifactThornHeartRuleTests.HeartEffect();
            effect.value.amount = 3;
            return new ArtifactLoadoutSnapshot(0, new[] { new ArtifactPlacedEffectSnapshot(1002, 13, effect) });
        }

        internal static CombatCommandInput Input(bool heart = true, bool splash = false, bool piercing = false, int hp = 10,
            TraitSpec[] traits = null, TraitStatusSnapshot[] statuses = null)
        {
            var baseline = CombatCommandTestData.Input(empty: true);
            var method = new TargetingMethodSpec(piercing ? TargetingShape.EightDirectionLine : TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, splash ? TargetingSplash.Radial : TargetingSplash.None, splash ? 70 : 0);
            var specs = (traits ?? Array.Empty<TraitSpec>()).ToList();
            var snapshots = (statuses ?? Array.Empty<TraitStatusSnapshot>()).ToList();
            if (piercing)
            {
                specs.Add(new PiercingFalloffTraitSpec(30, int.MaxValue));
                snapshots.Add(new PiercingFalloffTraitStatusSnapshot(30));
            }
            var actor = CombatCommandTestData.With(CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo,
                new CellCoord(1, 3), Owner.Enemy, piercing ? 10 : 20), hp: hp, appointRange: 6, traits: snapshots.ToArray());
            var characters = new[] { actor, baseline.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Player),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(5, 3), Owner.Player) };
            var profiles = baseline.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, actor.Stats.AttackPower,
                CategoryKey.Main, specs.ToArray()), method);
            var state = new CombatCommandState(8,8,CombatFlowPhase.Automatic,Array.Empty<StructureCombatFacts>(),turn: new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy, controllerTurnSequence: 1));
            return new CombatCommandInput(0, CombatCommandKind.Attack, actor.EntityId, AttackAim.ForCell(new CellCoord(4, 3)),
                state, characters, profiles, baseline.Main,
                characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value,
                    c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, c => 0),
                SoulFieldSnapshot.Empty, heart ? Heart() : baseline.Artifacts, processingKind: CombatProcessingKind.Advance,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);
        }

        internal static CombatCommandInput Replace(CombatCommandInput input, int id, Func<CharacterCombatFacts, CharacterCombatFacts> replace) =>
            CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == id ? replace(c) : c).ToArray());

        internal static CharacterCombatFacts Final(CombatCommandTransition result, int id) =>
            CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId.Value == id);
    }
}
