using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class DeadHandMovementTests
    {
        [Test]
        public void BoardEdgeKeepsNominalOffBoardEndpointAndDoesNotInventArrival()
        {
            var input = DeadHandTestData.Input();
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 1
                ? CombatCommandTestData.With(c, position: new CellCoord(6, 1)) : c.EntityId.Value == 2
                ? CombatCommandTestData.With(c, position: new CellCoord(7, 2)) : c).ToArray());
            var work = DeadHandTestData.Prepare(input);
            AttackSweepSnapshot result = null;
            foreach (var _ in DeadHandAttackCalculator.ExecuteSteps(work.Evaluation, work.Plan, work.World, work.Ledger,
                work.Arrival, value => result = value)) { }
            Assert.That(result.Targets.Single().IntendedEndpoint, Is.EqualTo(new CellCoord(8, 2)));
            Assert.That(result.Targets.Single().Stop, Is.EqualTo(AttackSweepStop.Blocked));
            Assert.That(result.Targets.Single().EnteredCells, Is.Empty);
            Assert.That(work.Arrival.ArrivalCells.ContainsKey(new EntityId(2)), Is.False);
        }

        [Test]
        public void AllUsesExistingFacingAndDifferentRowsDoNotBlockEachOther()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(4, 4), new CellCoord(5, 4) },
                method: new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.All, TargetingSplash.None, 0),
                aim: AttackAim.ForAll());
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 1
                ? c.WithPlacement(CharacterRuntimeArea.Field, null, new CellCoord(3, 3), Rotation4.R90, new[] { new CellCoord(3, 3) }) : c).ToArray());
            var result = DeadHandTestData.Sweep(input);
            Assert.That(result.Sweep.Facing, Is.EqualTo(Rotation4.R90));
            Assert.That(result.World.Characters[new EntityId(2)].Anchor, Is.EqualTo(new CellCoord(4, 0)));
            Assert.That(result.World.Characters[new EntityId(3)].Anchor, Is.EqualTo(new CellCoord(5, 0)));
        }

        [TestCase(AttackDirection.Up, 1, 0)]
        [TestCase(AttackDirection.Right, 0, -1)]
        [TestCase(AttackDirection.Down, -1, 0)]
        [TestCase(AttackDirection.Left, 0, 1)]
        public void MovementRotatesWithAttackFacing(AttackDirection direction, int dx, int dy)
        {
            var input = DeadHandTestData.Input();
            var actorCell = new CellCoord(3, 3);
            var targetCell = new CellCoord(3 - dy, 3 + dx);
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 1
                ? CombatCommandTestData.With(c, position: actorCell) : c.EntityId.Value == 2
                ? CombatCommandTestData.With(c, position: targetCell) : c).ToArray());
            input = input.WithIntent(input.Kind, AttackAim.ForDirection(direction), input.TargetCell, input.Path, input.MovementContext);
            var result = DeadHandTestData.Sweep(input);
            Assert.That(result.World.Characters[new EntityId(2)].Anchor,
                Is.EqualTo(new CellCoord(targetCell.X + 2 * dx, targetCell.Y + 2 * dy)));
        }

        [Test]
        public void SingleSelectionKeepsOtherInRangeEnemyAsBlocker()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(3, 2), new CellCoord(4, 2) },
                method: new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0),
                aim: AttackAim.ForCell(new CellCoord(3, 2)));
            var result = DeadHandTestData.Sweep(input);
            Assert.That(result.Sweep.CapturedTargetIds, Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(result.Sweep.Targets.Single().EnteredCells, Is.Empty);
            Assert.That(result.Sweep.Targets.Single().IntendedEndpoint, Is.EqualTo(new CellCoord(6, 2)));
            Assert.That(result.World.Characters[new EntityId(3)].Anchor, Is.EqualTo(new CellCoord(4, 2)));
        }

        [TestCase(TargetingSplash.None, 0)]
        [TestCase(TargetingSplash.Radial, 70)]
        public void SingleDestinationIgnoresHaboobAtTheRowEnd(TargetingSplash splash, int splashPercent)
        {
            // 하붑의 지정 금지는 주 칸 지정만 막는다. 쓸기 도착 경계는 기본 모양의 끝이라 줄지 않는다(검토 PD-4).
            var input = DeadHandTestData.Input(method: new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, splash, splashPercent), aim: AttackAim.ForCell(new CellCoord(3, 2)));
            var plain = DeadHandTestData.Sweep(input);
            var haboob = DeadHandTestData.Sweep(HaboobTargetingTests.WithArea(input, new CellCoord(5, 2)));
            Assert.That(plain.Sweep.Targets.Single().IntendedEndpoint, Is.EqualTo(new CellCoord(6, 2)));
            Assert.That(haboob.Sweep.Targets.Single().IntendedEndpoint, Is.EqualTo(new CellCoord(6, 2)));
        }

        [TestCase(5, 4)]
        [TestCase(4, 3)]
        public void OutsideTargetAndAllyBlockAtLastFreeCell(int blockerX, int expectedX)
        {
            var input = DeadHandTestData.Input();
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(7, EntityKey.Unit_Demon, new CellCoord(blockerX, 2), Owner.Player)).ToArray());
            var result = DeadHandTestData.Sweep(input);
            Assert.That(result.Sweep.Targets.Single().Stop, Is.EqualTo(AttackSweepStop.Blocked));
            Assert.That(result.World.Characters[new EntityId(2)].Anchor, Is.EqualTo(new CellCoord(expectedX, 2)));
        }

        [Test]
        public void ImmuneTargetDoesNotSynthesizeMovementOrArrival()
        {
            var input = TraitCombatFixtures.Equip(DeadHandTestData.Input(), EntityKey.Unit_Demon, new SteadfastWillTraitSpec());
            var result = DeadHandTestData.Sweep(input);
            Assert.That(result.Sweep.Targets.Single().Stop, Is.EqualTo(AttackSweepStop.Immune));
            Assert.That(result.World.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Moved), Is.False);
            Assert.That(result.World.Characters[new EntityId(2)].Anchor, Is.EqualTo(new CellCoord(3, 2)));
        }

        [Test]
        public void DisposeAfterTransientMovementKeepsInputAndDoesNotCompleteHeader()
        {
            var input = DeadHandTestData.Input();
            var work = DeadHandTestData.Prepare(input);
            bool complete = false;
            using (var steps = DeadHandAttackCalculator.ExecuteSteps(work.Evaluation, work.Plan, work.World, work.Ledger,
                work.Arrival, _ => complete = true).GetEnumerator())
            {
                while (steps.MoveNext() && work.World.Characters[new EntityId(2)].Anchor == new CellCoord(3, 2)) { }
            }
            Assert.That(complete, Is.False);
            Assert.That(work.World.Events.OfType<CombatSweepEvent>(), Is.Empty);
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 2).Anchor, Is.EqualTo(new CellCoord(3, 2)));
            var other = input.WithActor(input.ActorId);
            Assert.Throws<InvalidOperationException>(() => work.Evaluation.RequireSource(other));
        }

        [Test]
        public void GeometryChecksCoordinateOverflowAndMaskHoles()
        {
            Assert.That(AttackSweepGeometry.Destination(new[] { new CellCoord(1, 2), new CellCoord(4, 2) },
                new CellCoord(1, 2), CellCoord.Right), Is.EqualTo(new CellCoord(5, 2)));
            Assert.Throws<OverflowException>(() => AttackSweepGeometry.Destination(new[] { new CellCoord(int.MaxValue, 2) },
                new CellCoord(int.MaxValue, 2), CellCoord.Right));
        }

        [TestCase(AttackSweepSide.Right, 5)]
        [TestCase(AttackSweepSide.Left, 1)]
        public void SingleEnemyReachesCompleteMaskBoundaryPlusOne(AttackSweepSide side, int x)
        {
            var input = DeadHandTestData.Input(side: side);
            var result = DeadHandTestData.Sweep(input);
            Assert.That(result.World.Characters[new EntityId(2)].Anchor, Is.EqualTo(new CellCoord(x, 2)));
            Assert.That(result.Sweep.Targets.Single().Stop, Is.EqualTo(AttackSweepStop.Completed));
            Assert.That(result.Sweep.AreaCells.Count, Is.EqualTo(6));
            Assert.That(input.Characters.Single(c => c.EntityId == new EntityId(2)).Anchor, Is.EqualTo(new CellCoord(3, 2)));
            Assert.That(result.World.Events.First(), Is.TypeOf<CombatSweepEvent>());
            Assert.That(((CombatSweepEvent)result.World.Events.First()).Sweep, Is.SameAs(result.Sweep));
        }

        [TestCase(AttackSweepSide.Right, 3, 4, 5)]
        [TestCase(AttackSweepSide.Left, 1, 2, 3)]
        public void NearestEdgeFirstPacksThreeEnemies(AttackSweepSide side, int first, int second, int third)
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(2, 2), new CellCoord(3, 2), new CellCoord(4, 2) }, side);
            var result = DeadHandTestData.Sweep(input);
            Assert.That(new[] { 2, 3, 4 }.Select(id => result.World.Characters[new EntityId(id)].Anchor.Value.X),
                Is.EqualTo(new[] { first, second, third }));
            Assert.That(result.Sweep.Targets.Select(t => t.TargetId.Value),
                Is.EqualTo(side == AttackSweepSide.Right ? new long[] { 4, 3, 2 } : new long[] { 2, 3, 4 }));
        }
    }
}
