using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;

using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
namespace ProjectL.Tests.Core
{
    public sealed class TraitActivationMovementTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        [TestCase(TraitKey.Knockback)]
        [TestCase(TraitKey.ImpactBolt)]
        [TestCase(TraitKey.Pull)]
        public void MovementCause_DistinguishesKnockbackImpactBoltAndPull(TraitKey key)
        {
            var input = key == TraitKey.Pull ? PullTestData.Input() : key == TraitKey.ImpactBolt ? ImpactBoltTests.Input()
                : CliffFallTests.Knockback();
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            if (key == TraitKey.Pull)
            {
                var movement = result.Events.OfType<CombatPullEvent>().Single().Movement;
                var steps = ((System.Collections.IEnumerable)Member(movement, "Steps")).Cast<object>().ToArray();
                Assert.That(steps.Length, Is.EqualTo(movement.Path.Count));
                foreach (var step in steps) AssertCause(Member(step, "Cause"), key);
                Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Moved && e.Final.EntityId == movement.TargetId), Is.False);
            }
            else
            {
                var moved = result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved).ToArray();
                Assert.That(moved, Is.Not.Empty);
                foreach (var step in moved) AssertCause(Member(step, "MovementCause"), key);
            }
        }

        [Test]
        public void OrdinaryMove_RetainsVoluntaryCompatibility()
        {
            var result = CombatCommandCalculator.Calculate(Route(), NoRules);
            foreach (var step in result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved))
                Assert.That(Member(step, "MovementCause"), Is.Null);
        }

        [Test]
        public void StructureKnockback_HasTheSameDecidingCause()
        {
            var seed = CliffFallTests.Knockback();
            var barrel = ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 100, 7);
            var input = ObstacleReactionTests.Copy(seed,
                characters: seed.Characters.Where(c => c.EntityId.Value != 2 && c.EntityId.Value != 3).ToArray(),
                structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var moved = result.Events.OfType<CombatStructureEvent>().Single(e => e.Expected.Anchor != e.Final.Anchor);
            AssertCause(Member(moved, "MovementCause"), TraitKey.Knockback);
        }

        private static object Member(object value, string name)
        {
            var property = value.GetType().GetProperty(name);
            Assert.That(property, Is.Not.Null, name);
            return property.GetValue(value);
        }

        [Test]
        public void HiddenArrival_DoesNotExposeUnseenCause()
        {
            var input = TraitTrailTestData.Environment(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0)),
                TornadoEnvironmentTests.Source(cell: CellCoord.Right).WithKnownTo(new[] { Owner.Enemy }));
            var before = input.State;
            var preview = CombatMovementCalculator.Preview(input, NoRules);
            Assert.That(preview.PublicEffects.OfType<CombatCharacterEvent>().All(e => e.MovementCause == null), Is.True);
            Assert.That(preview.PublicEffects.OfType<CombatTornadoEvent>(), Is.Empty);
            Assert.That(input.State.SameValues(before), Is.True);
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            var forced = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Relocated);
            Assert.That(forced.MovementCause.Agency, Is.EqualTo(CombatMovementAgency.Forced));
            Assert.That(forced.MovementCause.TraitKey, Is.Null);
            Assert.That(forced.TornadoSourceId, Is.EqualTo(31));
        }

        private static void AssertCause(object cause, TraitKey key)
        {
            Assert.That(cause, Is.Not.Null);
            Assert.That(Member(cause, "Agency").ToString(), Is.EqualTo("Forced"));
            Assert.That(Member(cause, "TraitKey"), Is.EqualTo(key));
        }

        [Test]
        public void PassedCellTrail_FollowsTheStepLeavingIt()
        {
            var input = Route();
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var events = result.Events.ToList();
            var creations = result.Events.OfType<CombatMovementSourceEvent>().Where(e => e.Expected == null).ToArray();
            // 흔적은 지나간 (1,0)에만 두 종류. 출발·도착 칸에는 없다.
            Assert.That(creations.Length, Is.EqualTo(2));
            var moved = result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved).ToArray();
            foreach (var creation in creations)
            {
                var trail = (TraitTrailFact)creation.Final;
                Assert.That(trail.Active, Is.True);
                Assert.That(trail.Cell, Is.EqualTo(CellCoord.Right));
                int index = events.IndexOf(creation);
                Assert.That(TraitActivationOutcomeTests.HasActivation(events[index - 1], trail.Key, "TrailCreated"), Is.True);
                Assert.That(events.IndexOf(moved.Single(e => e.Expected.Anchor == trail.Cell)), Is.LessThan(index - 1));
            }
            Assert.That(creations.Select(e => e.Final.Id), Is.EqualTo(Enumerable.Range(0, 2).Select(i => input.State.EnvironmentProgress.NextSourceId + i)));
        }
        [Test]
        public void InterruptedRoute_HasNoUnenteredTrailEvents()
        {
            var input = TraitTrailTestData.Environment(Route(), new StopTile(80, CellCoord.Right, new[] { Owner.Player }));
            var result = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new StopRule() });
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(1));
            var sources = result.Events.OfType<CombatMovementSourceEvent>().Where(e => e.Expected == null).ToArray();
            // (1,0)에서 멈췄으므로 지나간 칸이 없고, 들어가지 않은 (2,0)에도 흔적이 없다.
            Assert.That(sources, Is.Empty);
        }
        [TestCase(Owner.Player)]
        [TestCase(Owner.Enemy)]
        public void DustOpposingEnd_EmitsOnlyDueSourceChanges(Owner side)
        {
            var input = TraitTrailTestData.Environment(Route(),
                TraitTrailTestData.Trail(10, CellCoord.Zero, TraitKey.DustScreen, range: 1, lifetime: 2, owner: side),
                TraitTrailTestData.Trail(11, CellCoord.Zero, TraitKey.DustScreen, range: 2, lifetime: 1, source: 6, owner: side));
            if (side == Owner.Player) input = EnemyControllerPhaseTests.Input(input, CombatControllerPhase.Enemy);
            var result = side == Owner.Player ? CombatTurnCalculator.EndEnemyPhase(input, NoRules) : CombatTurnCalculator.EndPlayerTurn(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var sources = result.Events.OfType<CombatMovementSourceEvent>().Where(e => e.Final is TraitTrailFact).ToArray();
            Assert.That(sources.Length, Is.EqualTo(2));
            Assert.That(sources[0].Expected.Id, Is.EqualTo(10));
            Assert.That(((TraitTrailFact)sources[0].Final).RemainingOpposingTurnEnds, Is.EqualTo(1));
            Assert.That(sources[1].Expected.Id, Is.EqualTo(11));
            Assert.That(sources[1].Final.Active, Is.False);
            Assert.That(TraitActivationOutcomeTests.Activations(result.Events, TraitKey.DustScreen).Select(a => a.TraitActivationKind.ToString()),
                Is.EquivalentTo(new[] { "TrailChanged", "TrailRemoved" }));
            Assert.That(TraitTrailCalculator.RangeReductionAt(CellCoord.Zero, result.FinalState.Environment), Is.EqualTo(1));
        }
        [Test]
        public void DustOwnEnd_PreservesSourcesWithoutLifecycleActivation()
        {
            var input = TraitTrailTestData.Environment(Route(), TraitTrailTestData.Trail(10, CellCoord.Zero, TraitKey.DustScreen, range: 1));
            var result = CombatTurnCalculator.EndPlayerTurn(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatMovementSourceEvent>(), Is.Empty);
            Assert.That(TraitActivationOutcomeTests.Activations(result.Events, TraitKey.DustScreen), Is.Empty);
        }
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Leap_ExceptionalTraversalHasOrderedFact(int obstacle)
        {
            var input = Route();
            input = obstacle == 2 ? TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, CellCoord.Right, Owner.Enemy)).ToArray())
                : TraitTrailTestData.Environment(input, new StaticTerrainFact(70, CellCoord.Right,
                    obstacle == 0 ? StaticTerrainKind.Wall : StaticTerrainKind.Cliff, true, new[] { Owner.Player, Owner.Enemy }));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new LeapTraitSpec(), new GroundCompactionTraitSpec(1, 1));
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var value = TraitActivationOutcomeTests.Activations(result.Events, TraitKey.Leap).Single();
            Assert.That(value.TargetCells, Is.EqualTo(new[] { CellCoord.Right }));
            int activation = result.Events.ToList().FindIndex(e => TraitActivationOutcomeTests.HasActivation(e, TraitKey.Leap, "MovementChanged"));
            int step = result.Events.ToList().FindIndex(e => e is CombatCharacterEvent c && c.Kind == CombatEventKind.Moved && c.Final.Anchor == CellCoord.Right);
            Assert.That(activation, Is.LessThan(step));
            if (obstacle != 2) Assert.That(result.Events.OfType<CombatMovementSourceEvent>().All(e => e.Final.Cell != CellCoord.Right), Is.True);
        }
        [Test]
        public void Leap_OrdinaryRouteDoesNotActivate()
        {
            var result = CombatCommandCalculator.Calculate(TraitCombatFixtures.Equip(Route(), EntityKey.Unit_Akimbo, new LeapTraitSpec()), NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitActivationOutcomeTests.Activations(result.Events, TraitKey.Leap), Is.Empty);
        }
        [Test]
        public void CompletedPath_StateActivationIsNotRepeatedPerStep()
        {
            var result = CombatCommandCalculator.Calculate(TraitMovementAttackTests.Route(), NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            foreach (var key in new[] { TraitKey.Acceleration, TraitKey.StationaryFire })
                Assert.That(TraitActivationOutcomeTests.Activations(result.Events, key).Count(), Is.EqualTo(1));
            var states = result.Events.OfType<CombatTraitStateEvent>().Where(e => e.Final.EntityId.Value == 1).ToArray();
            Assert.That(states.Length, Is.EqualTo(1));
            Assert.That(TraitMovementAttackTests.Held(states[0].Expected), Is.Zero);
            Assert.That(TraitMovementAttackTests.Held(states[0].Final), Is.EqualTo(6));
            Assert.That(TraitMovementAttackTests.Moved(states[0].Final), Is.True);
        }
        [Test]
        public void Attack_ConsumesCapturedAccelerationOnce()
        {
            var input = TraitCombatFixtures.Equip(TraitMovementAttackTests.SingleTarget(), EntityKey.Unit_Akimbo, new AccelerationTraitSpec(2));
            input = TraitMovementAttackTests.WithState(input, 1, 6, false);
            var result = TraitCombatFixtures.Attack(input);
            var value = TraitActivationOutcomeTests.Activations(result.Events, TraitKey.Acceleration).Single(a => a.TraitActivationKind == TraitActivationKind.StatusConsumed);
            Assert.That(value.Magnitude, Is.EqualTo(6));
            var state = result.Events.OfType<CombatTraitStateEvent>().Last(e => e.Final.EntityId.Value == 1);
            Assert.That(TraitMovementAttackTests.Held(state.Expected), Is.EqualTo(6));
            Assert.That(TraitMovementAttackTests.Held(state.Final), Is.Zero);
        }
        [Test]
        public void PreviewAndRejectedRoute_DoNotCommitSourcesOrOccurrences()
        {
            var input = Route();
            CombatMovementCalculator.Preview(input, NoRules);
            Assert.That(input.State.Environment.Tiles, Is.Empty);
            var rejected = CombatCommandCalculator.Calculate(MovementTestData.Route(input, CellCoord.Zero, CellCoord.Right, CellCoord.Zero), NoRules);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Events, Is.Empty);
            Assert.That(rejected.FinalState.SameValues(input.State), Is.True);
        }
        [Test]
        public void MovementSourceEvent_CreationAndSuccessorContracts()
        {
            var trail = (TraitTrailFact)TraitTrailTestData.Trail(10, CellCoord.Zero, TraitKey.GroundCompaction, movement: 1);
            Assert.That(new CombatMovementSourceEvent(null, trail).Expected, Is.Null);
            Assert.Throws<ArgumentNullException>(() => new CombatMovementSourceEvent(trail, null));
            Assert.Throws<ArgumentException>(() => new CombatMovementSourceEvent(trail, TraitTrailTestData.Trail(11, CellCoord.Zero, TraitKey.GroundCompaction, movement: 1)));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void GroundAllowance_RecordsOnlyTheUsedStrongestExtension(bool extends)
        {
            var cells = extends ? new[] { CellCoord.Zero, CellCoord.Right, new CellCoord(2, 0), new CellCoord(3, 0), new CellCoord(4, 0) }
                : new[] { CellCoord.Zero, CellCoord.Right };
            var input = TraitTrailTestData.Environment(MovementTestData.SafeRoute(cells),
                TraitTrailTestData.Trail(10, CellCoord.Zero, TraitKey.GroundCompaction, movement: 1, source: 5),
                TraitTrailTestData.Trail(11, CellCoord.Zero, TraitKey.GroundCompaction, movement: 2, source: 6));
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var values = TraitActivationOutcomeTests.Activations(result.Events, TraitKey.GroundCompaction)
                .Where(a => a.TraitActivationKind == TraitActivationKind.RangeChanged).ToArray();
            Assert.That(values.Length, Is.EqualTo(extends ? 1 : 0));
            if (extends)
            {
                Assert.That(values[0].SourceUnitId.Value, Is.EqualTo(6));
                Assert.That(values[0].Magnitude, Is.EqualTo(2));
            }
        }
        [TestCase(false)]
        [TestCase(true)]
        public void Leap_SkippedHiddenMineDoesNotRevealMetadata(bool hidden)
        {
            var input = TraitCombatFixtures.Equip(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0)),
                EntityKey.Unit_Akimbo, new LeapTraitSpec());
            input = TraitTrailTestData.Environment(input, MineEnvironmentTests.Mine(cell: CellCoord.Right, hidden: hidden));
            var preview = CombatMovementCalculator.Preview(input, MineEnvironmentTests.Rules);
            Assert.That(preview.CanMove, Is.True);
            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitActivationOutcomeTests.Activations(result.Events, TraitKey.Leap).Count(), Is.EqualTo(hidden ? 0 : 1));
            Assert.That(result.Events.OfType<CombatObjectActivationEvent>(), Is.Empty);
            Assert.That(result.FinalState.Environment.Tiles.Single().Active, Is.True);
        }
        private static CombatCommandInput Route() => TraitCombatFixtures.Equip(MovementTestData.SafeRoute(            CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0)), EntityKey.Unit_Akimbo,
            new GroundCompactionTraitSpec(1, 1), new DustScreenTraitSpec(1, 20, 1));
        private sealed class StopTile : MovementTileFact
        {
            internal StopTile(long id, CellCoord cell, IReadOnlyList<Owner> known) : base(id, cell, "eplus-stop", 0, true, known) { }
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => new StopTile(Id, Cell, knownTo);
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other) && other is StopTile;
        }
        private sealed class StopRule : IMovementTileRule
        {
            public string Key => "eplus-stop";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) => input.Phase == MovementEffectPhase.Hazard
                ? new MovementStepEffects(movementMethods: new[] { new MovementMethodChange(input.MoverId, input.Stats.MoveMethod, MoveMethod.None) }) : new MovementStepEffects();
        }
    }
}