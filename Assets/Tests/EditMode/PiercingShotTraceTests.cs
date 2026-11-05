using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class PiercingShotTraceTests
    {
        private static readonly CellCoord Origin = new CellCoord(1, 3);
        private static readonly CellCoord Aim = new CellCoord(5, 3);
        private static readonly WallGeometry EmptyWalls = new WallGeometry(Array.Empty<WallRun>());

        private static CharacterCombatFacts Character(int id, int x, int y = 3, Owner owner = Owner.Enemy) =>
            CombatCommandTestData.Character(id, EntityKey.Unit_Demon, new CellCoord(x, y), owner);

        private static StructureCombatFacts Structure(int x, bool dead = false) => new StructureCombatFacts(
            new EntityId(50), new StructureSpec(EntityKey.Structure_Headquarters, 1, 1,
                new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(dead ? 0 : 100, 100),
            new CellCoord(x, 3), Rotation4.R0, new[] { new CellCoord(x, 3) }, Owner.Player);

        private static WallGeometry Walls(int x)
        {
            var input = WallMovementTests.WithWalls(CombatCommandTestData.Input(empty: true),
                WallStateLifecycleTests.Wall(71, new CellCoord(x, 3)));
            return WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight,
                input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
        }

        private static PiercingShotTrace Trace(CharacterCombatFacts[] characters = null,
            StructureCombatFacts[] eligible = null, StructureCombatFacts[] physical = null,
            WallGeometry walls = null, bool cannon = false, bool ignoreWalls = false,
            CellCoord? aim = null) => PierceTargetCollector.CollectSingle(Origin, aim ?? Aim, new[] { Origin },
                8, 8, characters ?? Array.Empty<CharacterCombatFacts>(),
                eligible ?? Array.Empty<StructureCombatFacts>(), physical ?? Array.Empty<StructureCombatFacts>(),
                new AttackTargetingPolicy(false, false, false, cannon), walls ?? EmptyWalls, new AttackElevation(Array.Empty<CellCoord>()),
                ignoreWalls ? new AttackOcclusionSpec(AttackWallDelivery.IgnoreWalls, AttackWallPropagation.FromImpact)
                    : AttackOcclusionSpec.Default);

        [TestCase(false)]
        [TestCase(true)]
        public void CollectSingle_BeforeAndAfterAim_UsesDistanceNotEntityId(bool reverse)
        {
            var targets = new[] { Character(9, 3), Character(2, 5), Character(7, 6) };
            var trace = Trace(reverse ? targets.Reverse().ToArray() : targets);
            Assert.That(trace.Encounters, Is.EqualTo(new[]
            {
                (new EntityId(9), 0, new CellCoord(3, 3)),
                (new EntityId(2), 1, new CellCoord(5, 3)),
                (new EntityId(7), 2, new CellCoord(6, 3))
            }));
            Assert.That(trace.TraveledCells, Is.EqualTo(new[] { new CellCoord(2, 3), new CellCoord(3, 3),
                new CellCoord(4, 3), new CellCoord(5, 3), new CellCoord(6, 3), new CellCoord(7, 3) }));
            Assert.That(trace.OriginCell, Is.EqualTo(Origin));
            Assert.That(trace.AimCell, Is.EqualTo(Aim));
            Assert.That(trace.Step, Is.EqualTo(CellCoord.Right));
            Assert.That(trace.Endpoint, Is.EqualTo(new CellCoord(7, 3)));
            Assert.That(trace.StopReason, Is.EqualTo(PiercingShotStopReason.BoardEdge));
            var fartherAim = Trace(targets, aim: new CellCoord(6, 3));
            Assert.That(fartherAim.TraveledCells, Is.EqualTo(trace.TraveledCells));
            Assert.That(fartherAim.Encounters, Is.EqualTo(trace.Encounters));
        }

        [TestCase(1, 0)] [TestCase(-1, 0)] [TestCase(0, 1)] [TestCase(0, -1)]
        [TestCase(1, 1)] [TestCase(1, -1)] [TestCase(-1, 1)] [TestCase(-1, -1)]
        public void EightDirections_KeepTravelOrderUntilBoardEdge(int x, int y)
        {
            // 7×7 보드 가운데에서 쏘면 여덟 방향 모두 세 칸 뒤가 보드 끝이다.
            var origin = new CellCoord(3, 3);
            var near = new CellCoord(3 + x, 3 + y);
            var aim = new CellCoord(3 + 2*x, 3 + 2*y);
            var far = new CellCoord(3 + 3*x, 3 + 3*y);
            var trace = PierceTargetCollector.CollectSingle(origin, aim, new[] { origin }, 7, 7,
                new[] { Character(2, far.X, far.Y), Character(9, near.X, near.Y) },
                Array.Empty<StructureCombatFacts>(), Array.Empty<StructureCombatFacts>(), default,
                EmptyWalls, new AttackElevation(Array.Empty<CellCoord>()), AttackOcclusionSpec.Default);
            Assert.That(trace.TraveledCells, Is.EqualTo(new[] { near, aim, far }));
            Assert.That(trace.Encounters.Select(h => h.Id.Value), Is.EqualTo(new[] { 9, 2 }));
            Assert.That(trace.Encounters.Select(h => h.Encounter), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(trace.Endpoint, Is.EqualTo(far));
            Assert.That(trace.Step, Is.EqualTo(new CellCoord(x, y)));
            Assert.That(trace.StopReason, Is.EqualTo(PiercingShotStopReason.BoardEdge));
        }

        [Test]
        public void DeadActorAndUnselectedCharacters_DoNotConsumeEncounterIndices()
        {
            var actor = Character(1, 1, owner: Owner.Player);
            var dead = CombatCommandTestData.With(Character(8, 2), hp: 0);
            var ineligible = Character(4, 4, owner: Owner.Player);
            var world = new[] { actor, dead, ineligible, Character(9, 3), Character(2, 5) };
            var trace = Trace(world.Where(c => c.EntityId != ineligible.EntityId).ToArray());
            Assert.That(trace.Encounters.Select(h => (h.Id.Value, h.Encounter)), Is.EqualTo(new[] { (9, 0), (2, 1) }));
            Assert.That(trace.TraveledCells, Does.Contain(new CellCoord(4, 3)));
            Assert.That(Trace(new[] { ineligible }).Encounters.Single().Id, Is.EqualTo(ineligible.EntityId));
        }

        [TestCase(3, false)] [TestCase(5, false)] [TestCase(6, false)]
        [TestCase(3, true)] [TestCase(5, true)] [TestCase(6, true)]
        public void LiveStructure_BeforeAtOrAfterAim_StopsEvenWhenIneligible(int x, bool eligible)
        {
            var structure = Structure(x);
            var trace = Trace(new[] { Character(2, 7) }, eligible ? new[] { structure } : null, new[] { structure });
            Assert.That(trace.Endpoint, Is.EqualTo(new CellCoord(x, 3)));
            Assert.That(trace.StopReason, Is.EqualTo(PiercingShotStopReason.Structure));
            Assert.That(trace.TraveledCells.Count, Is.EqualTo(x - 1));
            Assert.That(trace.Encounters.Select(h => h.Id.Value), Is.EqualTo(eligible ? new[] { 50 } : Array.Empty<int>()));
            Assert.That(trace.BlockedContacts, Is.Empty);
        }

        [Test]
        public void DeadStructure_DoesNotHitOrStop()
        {
            var structure = Structure(3, dead: true);
            var trace = Trace(new[] { Character(2, 5) }, new[] { structure }, new[] { structure });
            Assert.That(trace.Encounters.Select(h => h.Id.Value), Is.EqualTo(new[] { 2 }));
            Assert.That(trace.Endpoint, Is.EqualTo(new CellCoord(7, 3)));
        }

        [TestCase(false)] [TestCase(true)]
        public void WallInEmptyOrOccupiedCell_StopsTravelAndSeparatesDiagnostics(bool occupied)
        {
            var trace = Trace(occupied ? new[] { Character(9, 3), Character(2, 5) } : new[] { Character(2, 5) }, walls: Walls(3));
            Assert.That(trace.TraveledCells, Is.EqualTo(new[] { new CellCoord(2, 3) }));
            Assert.That(trace.Encounters, Is.Empty);
            Assert.That(trace.Endpoint, Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(trace.StopReason, Is.EqualTo(PiercingShotStopReason.Wall));
            Assert.That(trace.BlockedContacts.Select(b => b.Contact), Is.EqualTo(new[] { Aim }));
            Assert.That(trace.BlockedContacts[0].WallSourceIds, Is.EqualTo(new long[] { 71 }));
        }

        [TestCase(false)] [TestCase(true)]
        public void PhysicalStructure_StopsDiagnosticsBehindIt(bool wallBefore)
        {
            var structure = Structure(4);
            var trace = Trace(new[] { Character(2, 5) }, physical: new[] { structure }, walls: Walls(wallBefore ? 3 : 6));
            Assert.That(trace.BlockedContacts, Is.Empty);
            Assert.That(trace.Encounters, Is.Empty);
            Assert.That(trace.Endpoint, Is.EqualTo(new CellCoord(wallBefore ? 3 : 4, 3)));
            Assert.That(trace.StopReason, Is.EqualTo(wallBefore ? PiercingShotStopReason.Wall : PiercingShotStopReason.Structure));
        }

        [Test]
        public void EligibleStructureBehindWall_ReportsContactButNoLaterVictim()
        {
            var structure = Structure(4);
            var trace = Trace(new[] { Character(2, 5) }, new[] { structure }, new[] { structure }, walls: Walls(3));
            Assert.That(trace.BlockedContacts.Select(b => b.Contact), Is.EqualTo(new[] { new CellCoord(4, 3) }));
            Assert.That(trace.Encounters, Is.Empty);
        }

        /// <summary>경로에 걸친 다칸 구조물은 첫 칸에서 한 번만 조우하고 경로를 끝낸다. 벽 무시 전달도 같다.</summary>
        [TestCase(false)] [TestCase(true)]
        public void MultiCellStructure_IsEncounteredOnceAndStops(bool ignoreWalls)
        {
            var cells = new[] { new CellCoord(4, 3), new CellCoord(5, 3) };
            var structure = new StructureCombatFacts(new EntityId(50), new StructureSpec(EntityKey.Structure_Headquarters, 1, 2,
                new[] { new CellCoord(0, 0), new CellCoord(0, 1) }, 100, false, 0), new HealthStateSnapshot(100, 100),
                cells[0], Rotation4.R0, cells, Owner.Enemy);
            var trace = Trace(new[] { Character(2, 6) }, new[] { structure }, new[] { structure },
                walls: ignoreWalls ? Walls(3) : null, ignoreWalls: ignoreWalls);
            Assert.That(trace.Encounters.Select(h => (h.Id.Value, h.Encounter)), Is.EqualTo(new[] { (50, 0) }));
            Assert.That(trace.Endpoint, Is.EqualTo(new CellCoord(4, 3)));
            Assert.That(trace.StopReason, Is.EqualTo(PiercingShotStopReason.Structure));
            Assert.That(trace.BlockedContacts, Is.Empty);
        }

        [Test]
        public void CannonAdjacentExclusion_RecordsTravelButSkipsNearbyHitsAndStructure()
        {
            var near = Structure(2);
            var trace = Trace(new[] { Character(2, 5) }, new[] { near }, new[] { near }, cannon: true);
            Assert.That(trace.TraveledCells[0], Is.EqualTo(new CellCoord(2, 3)));
            Assert.That(trace.Encounters.Select(h => (h.Id.Value, h.Encounter)), Is.EqualTo(new[] { (2, 0) }));
            Assert.That(trace.StopReason, Is.EqualTo(PiercingShotStopReason.BoardEdge));
            var characterTrace = Trace(new[] { Character(9, 2), Character(2, 5) }, cannon: true);
            Assert.That(characterTrace.Encounters.Select(h => h.Id.Value), Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void CannonAdjacentExclusion_DoesNotSkipWall()
        {
            var trace = Trace(new[] { Character(2, 5) }, walls: Walls(2), cannon: true);
            Assert.That(trace.TraveledCells, Is.Empty);
            Assert.That(trace.Encounters, Is.Empty);
            Assert.That(trace.Endpoint, Is.EqualTo(new CellCoord(2, 3)));
            Assert.That(trace.StopReason, Is.EqualTo(PiercingShotStopReason.Wall));
            Assert.That(trace.BlockedContacts.Single().Contact, Is.EqualTo(Aim));
        }

        [Test]
        public void IgnoreWalls_TraversesOccupiedCellWithoutDamagingItsOccupant()
        {
            var trace = Trace(new[] { Character(9, 3), Character(2, 5) }, walls: Walls(3), ignoreWalls: true);
            Assert.That(trace.TraveledCells.Count, Is.EqualTo(6));
            Assert.That(trace.TraveledCells[1], Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(trace.Encounters.Select(h => (h.Id.Value, h.Encounter)), Is.EqualTo(new[] { (2, 0) }));
            Assert.That(trace.BlockedContacts, Is.Empty);
            Assert.That(trace.StopReason, Is.EqualTo(PiercingShotStopReason.BoardEdge));
        }

        [TestCase(4)] [TestCase(7)]
        public void ObstructionMidwayOrAtBoardEnd_ReportsObstruction(int x)
        {
            var structure = Structure(x);
            Assert.That(Trace(physical: new[] { structure }).StopReason, Is.EqualTo(PiercingShotStopReason.Structure));
            var wall = Trace(walls: Walls(x));
            Assert.That(wall.StopReason, Is.EqualTo(PiercingShotStopReason.Wall));
            Assert.That(wall.Endpoint, Is.EqualTo(new CellCoord(x, 3)));
            Assert.That(wall.TraveledCells.Count, Is.EqualTo(x - 2));
        }

        [TestCase(1, 3)] [TestCase(5, 4)] [TestCase(2, 5)]
        public void UnsupportedVector_IsRejectedBeforeSignNormalization(int x, int y) =>
            Assert.Throws<ArgumentException>(() => Trace(aim: new CellCoord(x, y)));

        [Test]
        public void LargeCoordinates_DoNotWrapOrContinueBeyondBoard()
        {
            var origin = new CellCoord(int.MaxValue - 2, 1);
            var aim = new CellCoord(int.MaxValue - 1, 1);
            var trace = PierceTargetCollector.CollectSingle(origin, aim, new[] { origin }, int.MaxValue, 3,
                new[] { Character(9, aim.X, aim.Y) }, Array.Empty<StructureCombatFacts>(),
                Array.Empty<StructureCombatFacts>(), default, EmptyWalls, new AttackElevation(Array.Empty<CellCoord>()), AttackOcclusionSpec.Default);
            Assert.That(trace.TraveledCells, Is.EqualTo(new[] { aim }));
            Assert.That(trace.StopReason, Is.EqualTo(PiercingShotStopReason.BoardEdge));
            Assert.That(trace.Endpoint, Is.EqualTo(aim));
        }

        [Test]
        public void TraceCollections_AreReadOnlyAndRetainSpatialOrder()
        {
            var trace = Trace(new[] { Character(9, 3), Character(2, 5) });
            Assert.Throws<NotSupportedException>(() => ((IList<CellCoord>)trace.TraveledCells).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<(EntityId, int, CellCoord)>)trace.Encounters).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<AttackBlockedContact>)trace.BlockedContacts).Clear());
            Assert.That(trace.Encounters.Select(h => h.Id.Value), Is.EqualTo(new[] { 9, 2 }));
        }
    }
}
