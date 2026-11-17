using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class DeadHandTerrainTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ForcedArrivalInHaboobRejectsTheNextOutsideSingleAttack(bool sweep)
        {
            var landing = sweep ? new CellCoord(5, 2) : new CellCoord(5, 3);
            var basis = sweep
                ? DeadHandTestData.Input(active: true)
                : CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            var victim = new EntityId(2);
            var followUpActor = new EntityId(10);
            var characters = basis.Characters.Where(c => c.Owner == Owner.Player || c.EntityId == victim)
                .Select(c => c.EntityId == followUpActor
                    ? CombatCommandTestData.With(c, position: new CellCoord(5, 5)) : c).ToArray();
            var input = HaboobTargetingTests.WithArea(CombatCommandTestData.With(basis, characters), landing);
            var rules = Array.Empty<IMovementTileRule>();

            var first = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(first.Accepted, Is.True, first.Reason);
            Assert.That(first.Attack.Value.Hits.ToArray().Select(hit => hit.TargetId), Does.Contain(victim));
            var after = CombatCommandComposition.Project(first);
            var moved = after.Characters.Single(c => c.EntityId == victim);
            Assert.That(moved.Anchor, Is.EqualTo(landing));
            Assert.That(moved.Health.IsDead, Is.False);

            var outsideAttack = CombatCommandTestData.FreshCommand(after, CombatCommandKind.Attack,
                followUpActor, AttackAim.ForCell(landing));
            var preview = CombatCommandCalculator.Preview(outsideAttack, rules);
            Assert.That(preview.CanAttack, Is.False);
            Assert.That(preview.FailureReason, Is.EqualTo(AttackFailureReason.HaboobDesignationBlocked));
            var rejected = CombatCommandCalculator.Calculate(outsideAttack, rules);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Reason, Is.EqualTo(nameof(AttackFailureReason.HaboobDesignationBlocked)));
            Assert.That(rejected.Events, Is.Empty);
        }

        [Test]
        public void SeparateForcedArrivalNormalizesDestinationOnceAfterObjects()
        {
            var input = TraitTrailTestData.Environment(DeadHandTestData.Input(),
                MineEnvironmentTests.Mine(60, damage: 7, cell: new CellCoord(5, 2)),
                new DestinationTileModifierFact(61, new CellCoord(5, 2), true, new[] { Owner.Player, Owner.Enemy }, 0, -10));
            var work = DeadHandTestData.Prepare(input, MineEnvironmentTests.Rules);
            foreach (var _ in DeadHandAttackCalculator.ExecuteSteps(work.Evaluation, work.Plan, work.World, work.Ledger,
                work.Arrival, value => { })) { }
            Assert.That(work.Arrival.ArrivalCells[new EntityId(2)], Is.EqualTo(new CellCoord(5, 2)));
            Assert.That(work.World.Characters[new EntityId(2)].Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(work.World.Characters[new EntityId(2)].Health.MaxHealth, Is.EqualTo(90));
            Assert.That(work.World.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.HealthNormalized), Is.EqualTo(1));
            Assert.That(work.World.Events.OfType<CombatObjectActivationEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void TornadoRelocationStopsPathAndPreservesActualFinalPosition()
        {
            var input = TraitTrailTestData.Environment(DeadHandTestData.Input(),
                TornadoEnvironmentTests.Source(cell: new CellCoord(4, 2)));
            var result = DeadHandTestData.Sweep(input, TornadoEnvironmentTests.Rules);
            var target = result.Sweep.Targets.Single();
            Assert.That(target.Stop, Is.EqualTo(AttackSweepStop.Interrupted));
            Assert.That(target.EnteredCells, Is.EqualTo(new[] { new CellCoord(4, 2) }));
            Assert.That(target.FinalAnchor, Is.EqualTo(result.World.Characters[new EntityId(2)].Anchor));
            Assert.That(target.FinalAnchor, Is.Not.EqualTo(target.EnteredCells.Last()));
            Assert.That(result.World.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Relocated), Is.EqualTo(1));
        }

        [Test]
        public void WallBlocksMovementWithoutCancellingEnemyBeyondIt()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(2, 2), new CellCoord(4, 2) });
            input = StaticTerrainIntegrationTests.Terrain(input,
                new StaticTerrainFact(71, new CellCoord(3, 2), StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy }));
            var result = DeadHandTestData.Sweep(input);
            Assert.That(result.World.Characters[new EntityId(2)].Anchor, Is.EqualTo(new CellCoord(2, 2)));
            Assert.That(result.World.Characters[new EntityId(3)].Anchor, Is.EqualTo(new CellCoord(5, 2)));
            Assert.That(result.Sweep.Targets.Single(t => t.TargetId.Value == 2).Stop, Is.EqualTo(AttackSweepStop.Blocked));
        }

        [Test]
        public void DestinationMineSettlesExactlyOnceBeforeNextTarget()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(2, 2), new CellCoord(4, 2) });
            input = CliffFallTests.WithEnvironment(input, MineEnvironmentTests.Mine(60, damage: 7, cell: new CellCoord(5, 2)));
            var result = DeadHandTestData.Sweep(input, MineEnvironmentTests.Rules);
            Assert.That(result.World.Characters[new EntityId(3)].Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(result.World.Characters[new EntityId(2)].Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.World.Events.OfType<CombatObjectActivationEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.World.Characters[new EntityId(2)].Anchor, Is.EqualTo(new CellCoord(4, 2)));
        }

        [Test]
        public void DeathVacatesCellForLaterTargetWithoutReplayingEarlierMove()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(2, 2), new CellCoord(4, 2), new CellCoord(7, 7) });
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 3
                ? CombatCommandTestData.With(c, hp: 1) : c).ToArray());
            input = CliffFallTests.WithEnvironment(input, MineEnvironmentTests.Mine(60, damage: 7, cell: new CellCoord(5, 2)));
            var result = DeadHandTestData.Sweep(input, MineEnvironmentTests.Rules);
            Assert.That(result.World.Characters[new EntityId(3)].Health.IsDead, Is.True);
            Assert.That(result.World.Characters[new EntityId(2)].Anchor, Is.EqualTo(new CellCoord(5, 2)));
            Assert.That(result.Sweep.Targets.Single(t => t.TargetId.Value == 3).EnteredCells.Count, Is.EqualTo(1));
            Assert.That(result.Sweep.Targets.Single(t => t.TargetId.Value == 3).Stop, Is.EqualTo(AttackSweepStop.Interrupted));
        }

        [Test]
        public void MainDeathStopsRemainingTargets()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(2, 2), new CellCoord(4, 2) });
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 10
                ? CombatCommandTestData.With(c, hp: 1, position: new CellCoord(5, 3)) : c).ToArray());
            input = CliffFallTests.WithEnvironment(input, MineEnvironmentTests.Mine(60, damage: 7, cell: new CellCoord(5, 2)));
            var result = DeadHandTestData.Sweep(input, MineEnvironmentTests.Rules);
            Assert.That(result.Sweep.Interrupted, Is.True);
            Assert.That(result.Sweep.Targets.Last().Stop, Is.EqualTo(AttackSweepStop.NotProcessed));
            Assert.That(result.World.Characters[new EntityId(2)].Anchor, Is.EqualTo(new CellCoord(2, 2)));
        }
    }
}
