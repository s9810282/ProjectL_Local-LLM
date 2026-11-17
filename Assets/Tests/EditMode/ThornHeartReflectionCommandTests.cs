using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;

namespace ProjectL.Tests.Core
{
    public sealed class ThornHeartReflectionCommandTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [TestCase(false)]
        [TestCase(true)]
        public void EnemyOrdinaryHit_ReflectsOnce(bool zeroDamage)
        {
            var input = ThornHeartTestData.Input(splash: true);
            if (zeroDamage) input = ThornHeartTestData.Replace(input, 2, c => HeldEnvironmentEffects.AcquireBadge(c, 41));
            var result = Run(input);
            Assert.That(ThornHeartTestData.Final(result, 1).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(Reflections(result).Count(), Is.EqualTo(1));
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(zeroDamage ? 0 : 20));
            Assert.That(input.Characters[0].Health.CurrentHealth, Is.EqualTo(10));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Cowboy_LethalVictimAndAttacker_NoKillChain(bool piercing)
        {
            var input = ThornHeartTestData.Input(hp: 2, piercing: piercing, traits: new TraitSpec[] { new KillChainAttackTraitSpec() },
                statuses: new TraitStatusSnapshot[] { new KillChainAttackTraitStatusSnapshot() });
            input = ThornHeartTestData.Replace(input, 2, c => CombatCommandTestData.With(c, hp: 1));
            var result = Run(input);
            Assert.That(ThornHeartTestData.Final(result, 2).Health.IsDead, Is.True);
            Assert.That(ThornHeartTestData.Final(result, 1).Health.IsDead, Is.True);
            Assert.That(ThornHeartTestData.Final(result, 3).Health.CurrentHealth, Is.EqualTo(piercing ? 93 : 100));
            Assert.That(ThornHeartTestData.Final(result, 10).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(ChainEvents(result), Is.Empty);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Died)
                .Select(e => e.Final.EntityId.Value), Is.EqualTo(new[] { 2, 1 }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReflectionDeath_StopsOffensiveStages_ButPiercingCompletesDirectDelivery(bool piercing)
        {
            var traits = new List<TraitSpec> { new KnockbackTraitSpec(1), new IntimidationTraitSpec(20) };
            var statuses = new List<TraitStatusSnapshot> { new KnockbackTraitStatusSnapshot(1), new IntimidationTraitStatusSnapshot(20) };
            var input = ThornHeartTestData.Input(hp: 2, splash: !piercing, piercing: piercing,
                traits: traits.ToArray(), statuses: statuses.ToArray());
            var result = Run(input);
            var actor = ThornHeartTestData.Final(result, 1);
            Assert.That(actor.Area, Is.EqualTo(CharacterRuntimeArea.Excluded));
            Assert.That(actor.Anchor, Is.Null);
            Assert.That(actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(result.Events.Where(e => e.Kind == CombatEventKind.Moved), Is.Empty);
            Assert.That(ThornHeartTestData.Final(result, 2).Anchor, Is.EqualTo(new CellCoord(4, 3)));
            Assert.That(ThornHeartTestData.Final(result, 3).Anchor, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(ThornHeartTestData.Final(result, 2).Debuffs.Intimidation, Is.Not.Null);
            Assert.That(ThornHeartTestData.Final(result, 3).Debuffs.Intimidation != null, Is.EqualTo(piercing));
            Assert.That(ThornHeartTestData.Final(result, 3).Health.CurrentHealth, Is.EqualTo(piercing ? 93 : 100));
            Assert.That(result.Attack.Value.Hits.Length, Is.EqualTo(piercing ? 2 : 1));
            Assert.That(Reflections(result).Count(), Is.EqualTo(1));
        }

        [Test]
        public void PiercingDeath_PreservesPathEndpointStopReasonAndEveryEncounter()
        {
            var input = ThornHeartTestData.Input(hp: 2, piercing: true);
            var baseline = Run(CombatCommandTestData.With(input, artifacts: ThornHeartTestData.Input(heart: false).Artifacts));
            var result = Run(input);
            var expected = baseline.Attack.Value.PiercingShot;
            var actual = result.Attack.Value.PiercingShot;
            Assert.That(actual.TraveledCells.ToArray(), Is.EqualTo(expected.TraveledCells.ToArray()));
            Assert.That(actual.Endpoint, Is.EqualTo(expected.Endpoint));
            Assert.That(actual.StopReason, Is.EqualTo(expected.StopReason));
            Assert.That(actual.Encounters.ToArray().Select(e => e.TargetId), Is.EqualTo(expected.Encounters.ToArray().Select(e => e.TargetId)));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.DamageDealt), Is.EqualTo(new[] { 10, 7 }));
        }

        [Test]
        public void SurvivingAttacker_ContinuesKnockback()
        {
            var input = ThornHeartTestData.Input(traits: new TraitSpec[] { new KnockbackTraitSpec(1) },
                statuses: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(1) });
            input = ThornHeartTestData.Replace(input, 3, c => CombatCommandTestData.With(c, position: new CellCoord(7, 0)));
            var result = Run(input);
            Assert.That(ThornHeartTestData.Final(result, 1).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(ThornHeartTestData.Final(result, 2).Anchor, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(Reflections(result).Count(), Is.EqualTo(1));
        }

        [TestCase(99, 99)]
        [TestCase(2, 0)]
        public void RejectedOrEmptyAttack_HasNoReflectionOrCost(int x, int y)
        {
            var input = CombatAttackQueries.WithAim(ThornHeartTestData.Input(), AttackAim.ForCell(new CellCoord(x, y)));
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(Reflections(result), Is.Empty);
            Assert.That(result.Characters.Characters, Is.Empty);
        }

        [Test]
        public void BlockedAttack_HasNoReflectionOrCost()
        {
            var input = WallMovementTests.WithWalls(ThornHeartTestData.Input(), WallStateLifecycleTests.Wall(71, new CellCoord(2, 3)));
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("BlockedByWall"));
            Assert.That(Reflections(result), Is.Empty);
            Assert.That(result.Characters.Characters, Is.Empty);
        }

        [Test]
        public void EmptyAcceptedPiercingDelivery_HasNoReflection()
        {
            var input = ThornHeartTestData.Input(piercing: true);
            var actor = input.Characters[0];
            var profile = input.Profiles[0];
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 10, CategoryKey.Main,
                new PiercingFalloffTraitSpec(30, 0)), profile.Method);
            var result = Run(CombatCommandTestData.With(input, profiles: profiles));
            Assert.That(result.Attack.Value.Hits.Length, Is.Zero);
            Assert.That(Reflections(result), Is.Empty);
            Assert.That(ThornHeartTestData.Final(result, 1).Health.CurrentHealth, Is.EqualTo(10));
        }

        [Test]
        public void ReactionWithDifferentActorId_DoesNotConsumeActionOrReflect()
        {
            var input = CombatCommandTestData.With(BossRoarTests.Input(0), artifacts: ThornHeartTestData.Heart());
            var boss = input.Characters.Single(c => c.EntityId == BossRoarTests.Boss);
            Assert.That(input.ActorId, Is.Not.EqualTo(boss.EntityId));
            var result = CombatAttackCalculator.Reaction(input, NoRules, boss, AttackAim.ForAll());
            Assert.That(result, Is.Not.Null);
            Assert.That(Reflections(result), Is.Empty);
            Assert.That(BossRoarTests.Final(result, boss.EntityId).Health.CurrentHealth, Is.EqualTo(100));
            var attack = result.Events.OfType<CombatRoarEvent>().Single().Attack.Value;
            Assert.That(attack.Hits.Length, Is.GreaterThan(0));
        }

        [Test]
        public void ReactionForcedArrivalHazard_DoNotReflect()
        {
            var input = ThornHeartTestData.Input(traits: new TraitSpec[] { new KnockbackTraitSpec(1) },
                statuses: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(1) });
            input = ThornHeartTestData.Replace(input, 3, c => CombatCommandTestData.With(c, position: new CellCoord(7, 0)));
            var state = input.State;
            input = CombatCommandTestData.With(input, state: EnvironmentDamageTests.WithDamageObjects(new CombatCommandState(8,8,state.Phase,                state.Structures,turn: state.Turn), (new CellCoord(5, 3), 5)));
            var result = CombatAttackCalculator.Reaction(input, EnvironmentDamageTests.DamageRules, input.Characters[0], input.Aim);
            Assert.That(result, Is.Not.Null);
            Assert.That(ThornHeartTestData.Final(result, 2).Health.CurrentHealth, Is.EqualTo(75));
            Assert.That(Reflections(result), Is.Empty);
            Assert.That(ThornHeartTestData.Final(result, 1).Health.CurrentHealth, Is.EqualTo(10));
        }

        [Test]
        public void EnvironmentDamage_UsesExistingBoundaryWithoutReflection()
        {
            var input = ThornHeartTestData.Input();
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var projection = MovementProjection.ForArrival(input, characters, events);
            projection.ChangeHealth(new MovementHealthRequest(new EntityId(2), 5), false,
                new CombatDamageOrigin(new CellCoord(4, 3), input.ActorId, projection.NextAttackGroup()));
            Assert.That(characters[new EntityId(2)].Health.CurrentHealth, Is.EqualTo(95));
            Assert.That(characters[input.ActorId].Health.CurrentHealth, Is.EqualTo(10));
            Assert.That(events.OfType<CombatArtifactEvent>(), Is.Empty);
        }

        [Test]
        public void KillChainOnlyHit_DoesNotReflect()
        {
            var input = ThornHeartTestData.Input(traits: new TraitSpec[] { new KillChainAttackTraitSpec() },
                statuses: new TraitStatusSnapshot[] { new KillChainAttackTraitStatusSnapshot() });
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            characters[new EntityId(2)] = CombatCommandCalculator.Hurt(characters[new EntityId(2)], 100);
            var events = new List<CombatEvent>();
            var arrival = MovementProjection.ForArrival(input, characters, events);
            KillChainAttackCalculator.Resolve(input, input.ActorId, new CombatEffectEngine(),
                new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())),
                CombatCommandEffects.Capture(input.Characters[0], input.Profiles[0]), 20, arrival, characters,
                new CommandAttackGroups(), new Dictionary<EntityId, CellCoord>(), new CombatHitCause(), new HashSet<EntityId>());
            Assert.That(events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Count(t => t.TraitKey == TraitKey.KillChainAttack), Is.EqualTo(1));
            Assert.That(events.OfType<CombatCharacterEvent>().Count(), Is.EqualTo(1));
            Assert.That(events.OfType<CombatArtifactEvent>(), Is.Empty);
            Assert.That(characters[input.ActorId].Health.CurrentHealth, Is.EqualTo(10));
        }

        [Test]
        public void SimultaneousDeaths_PreserveDeathsWithoutDeferredSoulCreation()
        {
            var input = ThornHeartTestData.Replace(ThornHeartTestData.Input(hp: 2), 2, c => CombatCommandTestData.With(c, hp: 1));
            var loadout = new ArtifactLoadoutSnapshot(0, input.Artifacts.Effects.ToArray().Concat(new[]
                { new ArtifactPlacedEffectSnapshot(11, 4, ArtifactSoulSchemaTests.SoulEffect()) }).ToArray());
            var result = Run(CombatCommandTestData.With(input, artifacts: loadout));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Died), Is.EqualTo(2));
            Assert.That(result.Field.Final.Entries, Is.Empty);
            Assert.That(result.Events.OfType<CombatSoulEvent>(), Is.Empty);
        }

        [Test]
        public void MainAndDesignatedBossDie_RetainsDefeatPrecedence()
        {
            var input = ThornHeartTestData.Input(hp: 2);
            input = ThornHeartTestData.Replace(input, 2, c => CombatCommandTestData.With(c, position: new CellCoord(0, 0)));
            input = ThornHeartTestData.Replace(input, 10, c => CombatCommandTestData.With(c, hp: 1, position: new CellCoord(4, 3)));
            var s = input.State;
            input = CombatCommandTestData.With(input,
                main: new MainCharacterPersistentState(1, 100, 1),
                state: new CombatCommandState(8,8,s.Phase,s.Structures,objective: BattleObjective.DesignatedBoss(input.ActorId),turn: s.Turn));
            var result = Run(input);
            Assert.That(ThornHeartTestData.Final(result, 1).Health.IsDead, Is.True);
            Assert.That(ThornHeartTestData.Final(result, 10).Health.IsDead, Is.True);
            Assert.That(result.Characters.Main.Final.CurrentHealth, Is.Zero);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Defeat));
            Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Died), Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeliveredObstacleImpact_SettlesAfterReflectionDeath(bool piercing)
        {
            var input = ThornHeartTestData.Input(hp: 2, piercing: piercing, splash: !piercing);
            var barrel = ObstacleReactionTests.Barrel(piercing ? 40 : 2, new CellCoord(piercing ? 6 : 4, 3), 1, 7);
            if (!piercing)
            {
                // 일반 전달은 대상 ID 순서이므로 장애물 2가 아군 3보다 먼저 맞도록 배치합니다.
                input = CombatCommandTestData.With(input, characters: input.Characters.Where(c => c.EntityId.Value != 2).ToArray());
                input = ThornHeartTestData.Replace(input, 3, c => CombatCommandTestData.With(c, position: new CellCoord(4, 4)));
            }
            input = ObstacleReactionTests.Copy(input, structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
            var result = Run(input);
            Assert.That(ThornHeartTestData.Final(result, 1).Health.IsDead, Is.True);
            Assert.That(result.FinalState.Structures.Single().Health.IsDead, Is.True);
            Assert.That(result.Attack.Value.Hits.ToArray().Any(h => h.TargetId == barrel.Structure.EntityId), Is.True);
            Assert.That(Reflections(result).Count(), Is.EqualTo(1));
            var events = result.Events.ToList();
            Assert.That(events.FindIndex(e => e is CombatObstacleReactionEvent),
                Is.GreaterThan(events.FindIndex(e => e is CombatArtifactEvent a && a.Reflection != null)));
        }

        [TestCase(10, 1)]
        [TestCase(2, 5)]
        public void ReflectionDoesNotCountRoar_AndDeadAttackerCannotRoar(int hp, int hitCount)
        {
            var input = ThornHeartTestData.Input(hp: hp, traits: new TraitSpec[] { new RoarTraitSpec(5) },
                statuses: new TraitStatusSnapshot[] { new RoarTraitStatusSnapshot(hitCount, 5) });
            var result = Run(input);
            Assert.That(RoarHitCounter.HitCount(ThornHeartTestData.Final(result, 1)), Is.EqualTo(hitCount));
            Assert.That(result.Events.OfType<CombatRoarEvent>(), Is.Empty);
        }

        private static IEnumerable<CombatArtifactEvent> Reflections(CombatCommandTransition result) =>
            result.Events.OfType<CombatArtifactEvent>().Where(e => e.Reflection != null);

        private static IEnumerable<TraitActivationSnapshot> ChainEvents(CombatCommandTransition result) =>
            result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Where(t => t.TraitKey == TraitKey.KillChainAttack);

        private static CombatCommandTransition Run(CombatCommandInput input)
        {
            CombatCommandTransition result = null;
            Assert.DoesNotThrow(() => result = CombatCommandCalculator.Calculate(input, NoRules));
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }
    }
}
