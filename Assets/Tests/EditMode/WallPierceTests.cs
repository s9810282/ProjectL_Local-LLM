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
        public void NoWall_ExecutionPreservesThreeRayDamage(
            [Values(AttackDirection.Up, AttackDirection.Right, AttackDirection.Down, AttackDirection.Left)] AttackDirection direction,
            [Values(0, 1, 3)] int cap)
        {
            var result = CombatCommandCalculator.Calculate(Input(direction, cap), Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.HasValue, Is.True);
            var hits = result.Attack.Value.Hits.ToArray();
            Assert.That(hits.Select(h => h.TargetId.Value), Is.EqualTo(
                Enumerable.Range(0, 3).SelectMany(ray => Enumerable.Range(1, cap).Select(d => (long)(100 + ray * 10 + d)))));
            Assert.That(hits.Select(h => h.DamageDealt), Is.EqualTo(
                Enumerable.Range(0, 3).SelectMany(_ => new[] { 20, 14, 8 }.Take(cap))));
        }

        [Test, Combinatorial]
        public void OrderedRaysAndTrace_PreserveIndicesAndBudget(
            [Values(AttackDirection.Up, AttackDirection.Right, AttackDirection.Down, AttackDirection.Left)] AttackDirection direction,
            [Values(0, 1, 3)] int cap)
        {
            var expectedSteps = Steps(direction);
            var steps = AppointShapeCalculator.CreateOrderedRaySteps(TargetingShape.EightDirectionLine, direction);
            Assert.That(steps, Is.EqualTo(expectedSteps));
            var input = Input(direction, cap);
            var trace = Collect(input, steps);
            var expected = Enumerable.Range(0, 3).SelectMany(ray => Enumerable.Range(1, 3)
                .Select(d => (new EntityId(100 + ray * 10 + d), d - 1, ray))).ToArray();
            Assert.That(trace.Encounters, Is.EqualTo(expected));
            Assert.That(PierceTargetCollector.ApplyBudget(trace, cap), Is.EqualTo(expected.Where(h => h.Item2 < cap)));
            Assert.That(trace.Encounters.Count, Is.EqualTo(9));
            Assert.That(trace.BlockedContacts, Is.Empty);
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
        public void WallInEmptyCell_StopsOnlyOneRay_AndExecutionUsesTrace()
        {
            var input = WallMovementTests.WithWalls(Input(AttackDirection.Up, 1),
                WallStateLifecycleTests.Wall(71, new CellCoord(3, 4)));
            input = CombatCommandTestData.With(input, characters: input.Characters.Where(c =>
                c.EntityId != new EntityId(101)).ToArray());
            var evaluation = CombatAttackQueries.Selection(input, input.Characters[0], input.Profiles[0]);
            Assert.That(evaluation.FailureReason, Is.EqualTo(AttackFailureReason.None));
            var selection = evaluation.Selection;
            Assert.That(selection.PierceTrace, Is.Not.Null);
            Assert.That(selection.PierceTrace.Encounters.Select(h => h.Ray), Is.EqualTo(new[] { 1, 1, 1, 2, 2, 2 }));
            Assert.That(selection.PierceTrace.BlockedContacts.Select(c => c.Contact), Is.EqualTo(new[] { new CellCoord(3, 5), new CellCoord(3, 6) }));
            Assert.That(selection.PierceTrace.HasWallBlockedEligibleTargets, Is.True);
            Assert.That(selection.PierceTrace.BlockedContacts.All(c => c.WallSourceIds.SequenceEqual(new long[] { 71 })), Is.True);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId.Value), Is.EqualTo(new long[] { 111, 121 }));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.DamageDealt), Is.EqualTo(new[] { 20, 20 }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PhysicalAlliedStructure_StopsHitsAndHiddenDiagnostics(bool wallBeforeStructure)
        {
            var input = Input(AttackDirection.Right, 3);
            var blocker = Structure(50, Owner.Player, new CellCoord(5, 3));
            var enemies = new[] { Character(101, new CellCoord(6, 3)) };
            var walls = wallBeforeStructure ? Walls(new CellCoord(4, 3)) : EmptyWalls;
            var trace = PierceTargetCollector.Collect(Origin, new[] { Origin }, new[] { CellCoord.Right }, 8, 8, 4,
                enemies, Array.Empty<StructureCombatFacts>(), new[] { blocker }, default, walls, AttackOcclusionSpec.Default);
            Assert.That(trace.Encounters, Is.Empty);
            Assert.That(trace.BlockedContacts, Is.Empty);
            Assert.That(trace.HasWallBlockedEligibleTargets, Is.False);
        }

        [Test]
        public void HiddenEligibleStructure_ReportsFirstContactAndStopsBeforeFarVictim()
        {
            var blocker = Structure(50, Owner.Enemy, new CellCoord(5, 3), new CellCoord(6, 3));
            var trace = PierceTargetCollector.Collect(Origin, new[] { Origin }, new[] { CellCoord.Right }, 8, 8, 4,
                new[] { Character(101, new CellCoord(7, 3)) }, new[] { blocker }, new[] { blocker }, default,
                Walls(new CellCoord(4, 3)), AttackOcclusionSpec.Default);
            Assert.That(trace.Encounters, Is.Empty);
            Assert.That(trace.BlockedContacts.Select(c => c.Contact), Is.EqualTo(new[] { new CellCoord(5, 3) }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MultiCellStructure_StopsEachRayAndKeepsIndependentEncounters(bool ignoreWalls)
        {
            var blocker = Structure(50, Owner.Enemy, new CellCoord(5, 1), new CellCoord(5, 2), new CellCoord(5, 3), new CellCoord(5, 4), new CellCoord(5, 5));
            var trace = PierceTargetCollector.Collect(Origin, new[] { Origin }, Steps(AttackDirection.Right), 8, 8, 4,
                new[] { Character(101, new CellCoord(6, 3)) }, new[] { blocker }, new[] { blocker }, default,
                ignoreWalls ? Walls(new CellCoord(4, 3)) : EmptyWalls, ignoreWalls
                    ? new AttackOcclusionSpec(AttackWallDelivery.IgnoreWalls, AttackWallPropagation.FromImpact) : AttackOcclusionSpec.Default);
            Assert.That(trace.Encounters, Is.EqualTo(new[] { (new EntityId(50), 0, 0), (new EntityId(50), 0, 1), (new EntityId(50), 0, 2) }));
            Assert.That(trace.BlockedContacts, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NearExclusion_PreservesSkippedStructure_ButNeverSkipsWall(bool wall)
        {
            var blocker = Structure(50, Owner.Player, new CellCoord(4, 3));
            var trace = PierceTargetCollector.Collect(Origin, new[] { Origin }, new[] { CellCoord.Right }, 8, 8, 4,
                new[] { Character(101, new CellCoord(5, 3)) }, Array.Empty<StructureCombatFacts>(), wall ? Array.Empty<StructureCombatFacts>() : new[] { blocker },
                new AttackTargetingPolicy(false, false, false, true), wall ? Walls(new CellCoord(4, 3)) : EmptyWalls, AttackOcclusionSpec.Default);
            Assert.That(trace.Encounters.Count, Is.EqualTo(wall ? 0 : 1));
            Assert.That(trace.BlockedContacts.Count, Is.EqualTo(wall ? 1 : 0));
        }

        [Test]
        public void Collector_DoesNotOverflowBoardLimit()
        {
            var origin = new CellCoord(int.MaxValue - 2, 1);
            var trace = PierceTargetCollector.Collect(origin, new[] { origin }, new[] { CellCoord.Right }, int.MaxValue, int.MaxValue,
                int.MaxValue, new[] { Character(101, new CellCoord(int.MaxValue - 1, 1)) }, Array.Empty<StructureCombatFacts>(),
                Array.Empty<StructureCombatFacts>(), default, EmptyWalls, AttackOcclusionSpec.Default);
            Assert.That(trace.Encounters.Count, Is.EqualTo(1));
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
        public void EvidenceAndTrace_DefensivelyCopyAndExposeReadOnlyCollections()
        {
            var ids = new long[] { 20, 10 };
            var cells = new[] { new CellCoord(5, 3), new CellCoord(4, 3) };
            var block = new AttackBlockedContact(Origin, new CellCoord(6, 3), AttackOcclusionPhase.Direct, ids, cells);
            ids[0] = 99;
            cells[0] = CellCoord.Zero;
            Assert.That(block.WallSourceIds, Is.EqualTo(new long[] { 10, 20 }));
            Assert.That(block.WallCells, Is.EqualTo(new[] { new CellCoord(4, 3), new CellCoord(5, 3) }));
            Assert.Throws<NotSupportedException>(() => ((IList<long>)block.WallSourceIds)[0] = 0);
            var encounters = new[] { (new EntityId(100), 0, 0) };
            var blocks = new[] { block };
            var trace = new PierceTrace(encounters, blocks);
            encounters[0] = default;
            blocks[0] = null;
            Assert.That(trace.Encounters[0].Id, Is.EqualTo(new EntityId(100)));
            Assert.That(trace.BlockedContacts[0], Is.SameAs(block));
            Assert.Throws<NotSupportedException>(() => ((IList<AttackBlockedContact>)trace.BlockedContacts).Clear());
            Assert.Throws<ArgumentOutOfRangeException>(() => PierceTargetCollector.ApplyBudget(trace, -1));
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
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(direction), empty: true);
            var actor = CombatCommandTestData.With(input.Characters[0], position: Origin, appointRange: 3,
                traits: new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) });
            var steps = Steps(direction);
            var characters = new[] { actor, input.Characters[1] }.Concat(Enumerable.Range(0, 3).SelectMany(ray => Enumerable.Range(1, 3)
                .Select(d => Character(100 + ray * 10 + d, new CellCoord(Origin.X + steps[ray].X * d, Origin.Y + steps[ray].Y * d))))).ToArray();
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Base, new PiercingFalloffTraitSpec(30, cap)), method);
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

        private static StructureCombatFacts Structure(int id, Owner owner, params CellCoord[] cells) =>
            new StructureCombatFacts(new EntityId(id), new StructureSpec(EntityKey.Structure_Headquarters, 1, cells.Length,
                Enumerable.Range(0, cells.Length).Select(y => new CellCoord(0, y)).ToArray(), 100, false, 0),
                new HealthStateSnapshot(100, 100), cells[0], Rotation4.R0, cells, owner);

        private static WallGeometry Geometry(CombatCommandInput input) => WallGeometryBuilder.Build(input.State.BoardWidth,
            input.State.BoardHeight, input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);

        private static WallGeometry Walls(CellCoord cell) => Geometry(WallMovementTests.WithWalls(
            CombatCommandTestData.Input(empty: true), WallStateLifecycleTests.Wall(71, cell)));

        private static PierceTrace Collect(CombatCommandInput input, CellCoord[] steps) => PierceTargetCollector.Collect(Origin,
            new[] { Origin }, steps, 8, 8, 3, input.Characters.Where(c => c.Owner == Owner.Enemy).ToArray(),
            Array.Empty<StructureCombatFacts>(), Array.Empty<StructureCombatFacts>(), default, EmptyWalls, AttackOcclusionSpec.Default);
    }
}
