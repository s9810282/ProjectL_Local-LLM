using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;

namespace ProjectL.Tests.Core
{
    public sealed class ObstacleHitDurabilityTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [Test]
        public void OneHitConsumesOneRegardlessOfDamage()
        {
            var input = ObstacleReactionTests.Barrels(
                ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 3, 7));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var structure = result.FinalState.Structures.Single();
            Assert.That(structure.ObstacleDurability, Is.EqualTo(new ObstacleDurabilitySnapshot(2, 3)));
            Assert.That(result.FinalState.Structures.Single().Health.IsDead, Is.False);
            Assert.That(structure.Health.CurrentHealth, Is.EqualTo(1));
            Assert.That(structure.Health.MaxHealth, Is.EqualTo(1));
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.Zero);
            Assert.That(result.Events.OfType<CombatObstacleReactionEvent>().Single().Reaction,
                Is.EqualTo(ObstacleReactionKind.Hit));
        }

        [Test]
        public void AkimboConsumesTwoDeliveredHits()
        {
            var input = ObstacleReactionTests.Barrels(
                ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 3, 7));
            var world = ApplyContacts(input, percent: 100, landed: true, count: 2);
            Assert.That(world.Events.OfType<CombatObstacleDurabilityEvent>()
                .Select(e => (e.Expected.ObstacleDurability.RemainingHits, e.Final.ObstacleDurability.RemainingHits)),
                Is.EqualTo(new[] { (3, 2), (2, 1) }));
            Assert.That(world.Structures[new EntityId(51)].ObstacleDurability.RequiredHits, Is.EqualTo(3));
        }

        [TestCase(true, 100, 2)]
        [TestCase(false, 0, 3)]
        public void DeliveredZeroDamageAndReportingPlaceholderHaveDifferentDurability(bool landed, int percent, int remaining)
        {
            var input = ObstacleReactionTests.Barrels(
                ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 3, 7));
            var world = ApplyContacts(input, percent, landed, count: 1);
            Assert.That(world.Structures[new EntityId(51)].ObstacleDurability.RemainingHits, Is.EqualTo(remaining));
        }

        [TestCase(-1, 3)]
        [TestCase(0, 0)]
        [TestCase(4, 3)]
        public void InvalidCountsRejectBeforeMutation(int remaining, int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObstacleDurabilitySnapshot(remaining, capacity));
        }

        [Test]
        public void CopyAndCompositionPreserveDurability()
        {
            var input = ObstacleReactionTests.Barrels(ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 3, 7));
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            var projected = CombatCommandComposition.Project(result);
            Assert.That(projected.State.Structures.Single().ObstacleDurability, Is.EqualTo(new ObstacleDurabilitySnapshot(2, 3)));
            Assert.That(input.State.SameValues(input.State.With(structures: projected.State.Structures)), Is.False);
            Assert.That(input.State.Structures.Single().ObstacleDurability.RemainingHits, Is.EqualTo(3));
        }

        [Test]
        public void ExplicitDetonationChainsBarrelsAndHitsOtherObstacleOnce()
        {
            var first = ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 4, 0);
            var second = ObstacleReactionTests.Barrel(52, new CellCoord(5, 4), 9, 7);
            var supply = Supply(53, new CellCoord(3, 2), 3, 2);
            var input = ObstacleReactionTests.Barrels(first, second, supply);
            var origin = new CombatDamageOrigin(new CellCoord(1, 3), input.ActorId);
            var result = ObstacleCommandCalculator.DetonatePowderBarrel(input, first.Structure.EntityId, origin, NoRules);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Events.OfType<CombatObstacleDurabilityEvent>().Select(e => (e.ObstacleId.Value, e.Cause)),
                Is.EqualTo(new[] { (51, ObstacleDestructionCause.ExplicitDetonation),
                    (52, ObstacleDestructionCause.BarrelChain), (53, ObstacleDestructionCause.BarrelBlast) }));
            Assert.That(result.FinalState.Structures.Single(s => s.EntityId == supply.Structure.EntityId).ObstacleDurability.RemainingHits, Is.EqualTo(2));
            Assert.That(result.Events.OfType<CombatObstacleReactionEvent>().Count(e => e.Reaction == ObstacleReactionKind.Destroyed), Is.EqualTo(2));
            Assert.Throws<ArgumentException>(() =>
                ObstacleCommandCalculator.DetonatePowderBarrel(input, supply.Structure.EntityId, origin, NoRules));
            var repeated = ObstacleCommandCalculator.DetonatePowderBarrel(CombatCommandComposition.Project(result),
                first.Structure.EntityId, origin, NoRules);
            Assert.That(repeated.Accepted, Is.False);
            Assert.That(repeated.Events, Is.Empty);
        }

        [Test]
        public void OrdinaryStructureStillUsesHp()
        {
            var barrel = ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 3, 7);
            var input = ObstacleReactionTests.Barrels(barrel);
            var cell = new CellCoord(3, 2);
            var definition = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1,
                new[] { CellCoord.Zero }, 20, false, 0);
            var headquarters = new StructureCombatFacts(new EntityId(52), definition,
                new HealthStateSnapshot(20, 20), cell, Rotation4.R0, new[] { cell });
            input = ObstacleReactionTests.Copy(input, structures: new[] { barrel.Structure, headquarters });

            var result = ObstacleCommandCalculator.DetonatePowderBarrel(input, barrel.Structure.EntityId,
                new CombatDamageOrigin(new CellCoord(1, 3), input.ActorId), NoRules);

            var hit = result.Events.OfType<CombatStructureEvent>().Single();
            Assert.That(hit.Expected.Health.CurrentHealth, Is.EqualTo(20));
            Assert.That(hit.Final.Health.CurrentHealth, Is.EqualTo(13));
            Assert.That(hit.Final.ObstacleDurability, Is.Null);
        }

        [Test]
        public void EnvironmentalContactConsumesOneAndHealingDoesNotRestoreHits()
        {
            var barrel = ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 3, 7);
            var input = ObstacleReactionTests.Barrels(barrel);
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>());
            projection.ChangeHealth(new MovementHealthRequest(barrel.Structure.EntityId, 0),
                false, new CombatDamageOrigin(new CellCoord(3, 3), null));
            projection.ChangeHealth(new MovementHealthRequest(barrel.Structure.EntityId, 100), true);
            Assert.That(projection.Structure(barrel.Structure.EntityId).ObstacleDurability.RemainingHits, Is.EqualTo(2));
            Assert.That(projection.Events.OfType<CombatObstacleDurabilityEvent>().Single().Cause,
                Is.EqualTo(ObstacleDestructionCause.EnvironmentalContact));
        }

        [Test]
        public void ArrivalBetweenHitScopesSharesCurrentTerrainWithoutRepeatingBlastStatistics()
        {
            var first = ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 1, 10);
            var arrivalBarrel = ObstacleReactionTests.Barrel(52, new CellCoord(2, 5), 1, 10);
            var last = ObstacleReactionTests.Barrel(53, new CellCoord(4, 5), 1, 10);
            var input = ObstacleReactionTests.Barrels(first, arrivalBarrel, last);
            var victim = new EntityId(3);
            var destination = new CellCoord(3, 4);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == victim
                ? CombatCommandTestData.With(c, position: destination) : c).ToArray());
            var ground = new StaticTerrainFact(71, destination, StaticTerrainKind.HighGround, true, new[] { Owner.Player });
            input = CliffFallTests.WithEnvironment(input, ground);
            var world = new CommandAttackProjection(input, input.State.Environment, null);

            var plan = Plan(input, first.Structure.EntityId, 100, true, 1, victim);
            var firstLedger = new CommandAttackLedger();
            AttackDirectDamageCalculator.Apply(plan, world, firstLedger, new HashSet<EntityId>());
            var arrival = world.CreateArrivalProjection(plan);
            var inactiveGround = new StaticTerrainFact(ground.Id, ground.Cell, ground.Kind, false, ground.KnownTo);
            arrival.ChangeSource(ground, new MovementStepEffects(expectedSource: ground, finalSource: inactiveGround));
            arrival.ChangeHealth(new MovementHealthRequest(arrivalBarrel.Structure.EntityId, 0), false,
                new CombatDamageOrigin(destination, input.ActorId));

            var lastLedger = new CommandAttackLedger();
            AttackDirectDamageCalculator.Apply(Plan(arrival.CurrentInput(), last.Structure.EntityId, 100, true, 1, victim),
                world, lastLedger, new HashSet<EntityId>());

            Assert.That(firstLedger.Expected(victim), Is.EqualTo(7));
            Assert.That(lastLedger.Expected(victim), Is.EqualTo(10));
            Assert.That(world.Characters[victim].Health.CurrentHealth, Is.EqualTo(73));
            Assert.That(world.Events.OfType<CombatObstacleReactionEvent>().Where(e => e.Reaction == ObstacleReactionKind.Destroyed)
                .Select(e => e.ObstacleId), Is.EqualTo(new[] { first.Structure.EntityId, arrivalBarrel.Structure.EntityId, last.Structure.EntityId }));
            Assert.That(input.State.Environment.Tiles.OfType<StaticTerrainFact>().Single().Active, Is.True);
        }

        private static CommandAttackProjection ApplyContacts(CombatCommandInput input, int percent, bool landed, int count)
        {
            var world = new CommandAttackProjection(input, input.State.Environment, null);
            AttackDirectDamageCalculator.Apply(Plan(input, input.State.Structures.Single().EntityId, percent, landed, count),
                world, new CommandAttackLedger(), new HashSet<EntityId>());
            return world;
        }

        private static CommandAttackPlan Plan(CombatCommandInput input, EntityId obstacle, int percent, bool landed,
            int count, EntityId? numericTarget = null)
        {
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var profile = input.Profiles.Single(p => p.Definition.Key == actor.CharacterKey);
            var environment = input.State.Environment;
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight, environment,
                input.EnvironmentBindings, input.State.WallBlocking);
            var target = input.State.Structures.Single(s => s.EntityId == obstacle);
            var role = percent == 0 ? AttackTargetRole.Additional : AttackTargetRole.Primary;
            var cells = new AttackCellSelection(target.Anchor, new[] { target.Anchor }, Array.Empty<CellCoord>());
            var targets = new List<KeyValuePair<EntityId, AttackTargetRole>> { new KeyValuePair<EntityId, AttackTargetRole>(target.EntityId, role) };
            var recipients = input.Characters.Where(c => c.EntityId == numericTarget).ToArray();
            var hits = Enumerable.Repeat(new DeliveredAttackHit(target.EntityId, role, 0, percent, actor.Anchor, landed: landed), count).ToList();
            foreach (var recipient in recipients)
            {
                targets.Add(new KeyValuePair<EntityId, AttackTargetRole>(recipient.EntityId, AttackTargetRole.Effect));
                hits.Add(new DeliveredAttackHit(recipient.EntityId, AttackTargetRole.Effect, 0, 100, actor.Anchor));
            }
            var selection = new ResolvedAttackSelection(cells, cells, targets, recipients, new[] { target },
                new AttackTargetingPolicy(true, false, false, false));
            var delivery = new ResolvedAttackDelivery(hits, new[] { target.Anchor });
            var engine = new CombatEffectEngine();
            return new CommandAttackPlan(input, NoRules, profile, actor, actor, environment, walls, selection, delivery,
                engine, CombatCommandEffects.Capture(actor, profile),
                new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())), 0, Rotation4.R0, false,
                damagePercent: 0);
        }

        private static (StructureCombatFacts Structure, ObstacleContentProfile Profile) Supply(int id, CellCoord cell,
            int hits, int height)
        {
            var offsets = Enumerable.Range(0, height).Select(y => new CellCoord(0, y)).ToArray();
            var definition = new StructureSpec(EntityKey.Structure_Supplies, 1, height, offsets, 1, false, 0);
            var entity = new EntityId(id);
            return (new StructureCombatFacts(entity, definition, new ObstacleDurabilitySnapshot(hits, hits), cell,
                Rotation4.R0, offsets.Select(c => cell + c).ToArray()),
                new ObstacleContentProfile(entity, ObstacleContentKind.Supplies, 0, 1, 2));
        }
    }
}
