using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class WallPierceTests
    {
        private static readonly CellCoord Origin = new CellCoord(3, 3);
        private static readonly WallGeometry EmptyWalls = new WallGeometry(Array.Empty<WallRun>());

        [Test, Combinatorial]
        public void NoWall_ExecutionUsesOneRayAndPreservesFalloff(
            [Values(AttackDirection.Up, AttackDirection.Right, AttackDirection.Down, AttackDirection.Left)] AttackDirection direction,
            [Values(0, 1, 3)] int cap)
        {
            var result = CombatCommandCalculator.Calculate(Input(direction, cap), Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.HasValue, Is.True);
            var hits = result.Attack.Value.Hits.ToArray();
            Assert.That(hits.Select(h => h.TargetId.Value), Is.EqualTo(
                Enumerable.Range(1, cap).Select(d => 100 + d)));
            Assert.That(hits.Select(h => h.DamageDealt), Is.EqualTo(
                new[] { 20, 14, 8 }.Take(cap)));
        }

        /// <summary>관통과 무관한 방향 지정의 세 갈래 기하는 그대로 유지한다.</summary>
        [Test]
        public void OrderedRaysAndDirectionMask_PreserveSharedGeometry(
            [Values(AttackDirection.Up, AttackDirection.Right, AttackDirection.Down, AttackDirection.Left)] AttackDirection direction)
        {
            var expectedSteps = Steps(direction);
            var steps = AppointShapeCalculator.CreateOrderedRaySteps(TargetingShape.EightDirectionLine, direction);
            Assert.That(steps, Is.EqualTo(expectedSteps));
            var mask = AppointShapeCalculator.CreateDirectionMask(TargetingShape.EightDirectionLine, 3, direction);
            Assert.That(mask, Is.EquivalentTo(expectedSteps.SelectMany(s => Enumerable.Range(1, 3)
                .Select(d => new CellCoord(s.X * d, s.Y * d)))));
        }

        [TestCase(TargetingShape.CardinalLine, AttackDirection.Left, -1, 0)]
        [TestCase(TargetingShape.DiagonalLine, AttackDirection.UpRight, 1, 1)]
        public void OrderedRays_SingleLine(TargetingShape shape, AttackDirection direction, int x, int y) =>
            Assert.That(AppointShapeCalculator.CreateOrderedRaySteps(shape, direction), Is.EqualTo(new[] { new CellCoord(x, y) }));

        [Test]
        public void OrderedRays_RejectNonRayShapeAndUnsupportedDirection()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AppointShapeCalculator.CreateOrderedRaySteps(TargetingShape.EightDirectionLine, AttackDirection.UpRight));
            Assert.Throws<NotSupportedException>(() => AppointShapeCalculator.CreateOrderedRaySteps(TargetingShape.RadialSquare, AttackDirection.Up));
        }

        [Test]
        public void WallBeforeSelectedCell_RejectsSingleShot()
        {
            var input = WallMovementTests.WithWalls(Input(AttackDirection.Up, 1),
                WallStateLifecycleTests.Wall(71, new CellCoord(3, 4)));
            input = CombatCommandTestData.With(input, characters: input.Characters.Where(c =>
                c.EntityId != new EntityId(101)).ToArray());
            input = CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(3, 5)));
            var evaluation = CombatAttackQueries.Selection(input, input.Characters[0], input.Profiles[0]);
            Assert.That(evaluation.FailureReason, Is.EqualTo(AttackFailureReason.BlockedByWall));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Attack, Is.Null);
            Assert.That(result.BlockedContacts.All(c => c.WallSourceIds.SequenceEqual(new long[] { 71 })), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Resolver_ChoosesNearestEntryBeforeSourceId(bool reverse)
        {
            var input = WallMovementTests.WithWalls(CombatCommandTestData.Input(empty: true),
                WallStateLifecycleTests.Wall(90, new CellCoord(2, 3)), WallStateLifecycleTests.Wall(10, new CellCoord(5, 3)));
            var walls = Geometry(input);
            var a = new CellCoord(0, 3);
            var b = new CellCoord(7, 3);
            var block = AttackOcclusionResolver.FindBlock(reverse ? b : a, reverse ? a : b, walls, AttackOcclusionPhase.Direct, false);
            Assert.That(block, Is.Not.Null);
            Assert.That(block.WallSourceIds, Is.EqualTo(new long[] { reverse ? 10 : 90 }));
            Assert.That(AttackOcclusionResolver.FindBlock(a, b, walls, AttackOcclusionPhase.Direct, true), Is.Null);
        }

        [Test]
        public void Resolver_EqualEntryUsesComponentKey_AndTangentIsClear()
        {
            var run = Walls(new CellCoord(4, 3)).Runs[0];
            var duplicate = new WallRun(run.XMin, run.XMax, run.YMin, run.YMax, run.CornerCutTicks, new long[] { 1 }, run.Cells);
            var block = AttackOcclusionResolver.FindBlock(Origin, new CellCoord(6, 3), new WallGeometry(new[] { run, duplicate }), AttackOcclusionPhase.Propagation, false);
            Assert.That(block, Is.Not.Null);
            Assert.That(block.WallSourceIds, Is.EqualTo(new long[] { 1 }));
            Assert.That(block.Phase, Is.EqualTo(AttackOcclusionPhase.Propagation));
            Assert.That(AttackOcclusionResolver.FindBlock(new CellCoord(3, 3), new CellCoord(4, 4), new WallGeometry(new[] { run }), AttackOcclusionPhase.Direct, false), Is.Null);
        }

        [Test]
        public void Evidence_DefensivelyCopiesAndExposesReadOnlyCollections()
        {
            var ids = new long[] { 20, 10 };
            var cells = new[] { new CellCoord(5, 3), new CellCoord(4, 3) };
            var block = new AttackBlockedContact(Origin, new CellCoord(6, 3), AttackOcclusionPhase.Direct, ids, cells);
            ids[0] = 99;
            cells[0] = CellCoord.Zero;
            Assert.That(block.WallSourceIds, Is.EqualTo(new long[] { 10, 20 }));
            Assert.That(block.WallCells, Is.EqualTo(new[] { new CellCoord(4, 3), new CellCoord(5, 3) }));
            Assert.Throws<NotSupportedException>(() => ((IList<long>)block.WallSourceIds)[0] = 0);
        }

        [Test]
        public void Evidence_RejectsInvalidSourceFacts()
        {
            var cells = new[] { CellCoord.Zero };
            Assert.Throws<ArgumentException>(() => new AttackBlockedContact(Origin, Origin, AttackOcclusionPhase.Direct, Array.Empty<long>(), Array.Empty<CellCoord>()));
            Assert.Throws<ArgumentException>(() => new AttackBlockedContact(Origin, Origin, AttackOcclusionPhase.Direct, new long[] { 0 }, cells));
            Assert.Throws<ArgumentException>(() => new AttackBlockedContact(Origin, Origin, AttackOcclusionPhase.Direct, new long[] { 1, 1 }, new[] { CellCoord.Zero, CellCoord.Right }));
            Assert.Throws<ArgumentException>(() => new AttackBlockedContact(Origin, Origin, AttackOcclusionPhase.Direct, new long[] { 1, 2 }, new[] { CellCoord.Zero, CellCoord.Zero }));
            Assert.Throws<ArgumentException>(() => new AttackBlockedContact(Origin, Origin, AttackOcclusionPhase.Direct, new long[] { 1, 2 }, cells));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackBlockedContact(Origin, Origin, (AttackOcclusionPhase)0, new long[] { 1 }, cells));
            Assert.Throws<ArgumentNullException>(() => new AttackBlockedContact(Origin, Origin, AttackOcclusionPhase.Direct, null, cells));
        }

        private static CombatCommandInput Input(AttackDirection direction, int cap)
        {
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForCell(new CellCoord(Origin.X + Steps(direction)[0].X, Origin.Y + Steps(direction)[0].Y)), empty: true);
            var actor = CombatCommandTestData.With(input.Characters[0], position: Origin, appointRange: 3,
                traits: new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) });
            var steps = Steps(direction);
            var characters = new[] { actor, input.Characters[1] }.Concat(Enumerable.Range(0, 3).SelectMany(ray => Enumerable.Range(1, 3)
                .Select(d => Character(100 + ray * 10 + d, new CellCoord(Origin.X + steps[ray].X * d, Origin.Y + steps[ray].Y * d))))).ToArray();
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Main, new PiercingFalloffTraitSpec(30, cap)), method);
            return CombatCommandTestData.With(input, characters: characters, profiles: profiles);
        }

        private static CellCoord[] Steps(AttackDirection direction) => direction switch
        {
            AttackDirection.Up => new[] { new CellCoord(0, 1), new CellCoord(-1, 1), new CellCoord(1, 1) },
            AttackDirection.Right => new[] { new CellCoord(1, 0), new CellCoord(1, 1), new CellCoord(1, -1) },
            AttackDirection.Down => new[] { new CellCoord(0, -1), new CellCoord(1, -1), new CellCoord(-1, -1) },
            AttackDirection.Left => new[] { new CellCoord(-1, 0), new CellCoord(-1, -1), new CellCoord(-1, 1) },
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };

        private static CharacterCombatFacts Character(int id, CellCoord cell) =>
            CombatCommandTestData.Character(id, EntityKey.Unit_Demon, cell, Owner.Enemy);

        private static WallGeometry Geometry(CombatCommandInput input) => WallGeometryBuilder.Build(input.State.BoardWidth,
            input.State.BoardHeight, input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);

        private static WallGeometry Walls(CellCoord cell) => Geometry(WallMovementTests.WithWalls(
            CombatCommandTestData.Input(empty: true), WallStateLifecycleTests.Wall(71, cell)));
    }
}
