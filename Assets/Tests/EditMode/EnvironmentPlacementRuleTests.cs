using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using static ProjectL.Tests.Core.CellEnvironmentTestData;

namespace ProjectL.Tests.Core
{
    public sealed class EnvironmentPlacementRuleTests
    {
        [TestCase(false, false, false, false, EnvironmentPlacementViolation.None)]
        [TestCase(true, false, false, false, EnvironmentPlacementViolation.WallOverlap)]
        [TestCase(false, true, false, false, EnvironmentPlacementViolation.WallOverlap)]
        [TestCase(false, false, true, false, EnvironmentPlacementViolation.WallOverlap)]
        [TestCase(false, false, false, true, EnvironmentPlacementViolation.WallOverlap)]
        public void WallOccupancy_UsesSharedCoexistenceRule(bool character, bool structure, bool objects, bool otherTerrain,
            EnvironmentPlacementViolation expected)
        {
            var snapshot = new CellEnvironmentSnapshot(CellCoord.Zero, true, otherTerrain ? new long[] { 1, 2 } : new long[] { 1 },
                objects ? new long[] { 3 } : Array.Empty<long>(), Array.Empty<EntityId>(),
                structure ? new[] { new EntityId(4) } : Array.Empty<EntityId>(),
                character ? new[] { new EntityId(5) } : Array.Empty<EntityId>(), isWallOccupied: true);
            Assert.That(CellEnvironmentQuery.CheckPlacement(snapshot), Is.EqualTo(expected));
        }

        [TestCase(false, false, 0, EnvironmentPlacementViolation.None)]
        [TestCase(true, false, 0, EnvironmentPlacementViolation.None)]
        [TestCase(false, true, 0, EnvironmentPlacementViolation.None)]
        [TestCase(true, true, 0, EnvironmentPlacementViolation.None)]
        [TestCase(false, false, 1, EnvironmentPlacementViolation.None)]
        [TestCase(true, false, 1, EnvironmentPlacementViolation.TerrainWithObstacle)]
        [TestCase(false, true, 1, EnvironmentPlacementViolation.ObstacleWithObject)]
        [TestCase(true, true, 1, EnvironmentPlacementViolation.TerrainWithObstacle | EnvironmentPlacementViolation.ObstacleWithObject)]
        [TestCase(false, false, 2, EnvironmentPlacementViolation.OverlappingObstacles)]
        [TestCase(true, false, 2, EnvironmentPlacementViolation.TerrainWithObstacle | EnvironmentPlacementViolation.OverlappingObstacles)]
        [TestCase(false, true, 2, EnvironmentPlacementViolation.ObstacleWithObject | EnvironmentPlacementViolation.OverlappingObstacles)]
        [TestCase(true, true, 2, EnvironmentPlacementViolation.TerrainWithObstacle | EnvironmentPlacementViolation.ObstacleWithObject | EnvironmentPlacementViolation.OverlappingObstacles)]
        public void KnownCoexistence_ReportsAllApplicableViolations(bool terrain, bool objects,
            int obstacleCount, EnvironmentPlacementViolation expected)
        {
            // 금지된 조합도 규칙 평가 전의 후보로 구성하며 실제 전투에 게시하지 않는다.
            var obstacles = Enumerable.Range(21, obstacleCount).Select(id => new EntityId(id)).ToArray();
            var candidate = new CellEnvironmentSnapshot(Cell, true,
                terrain ? new long[] { 11 } : Array.Empty<long>(),
                objects ? new long[] { 12 } : Array.Empty<long>(),
                obstacles, obstacles, Array.Empty<EntityId>());

            Assert.That(CellEnvironmentQuery.CheckPlacement(candidate), Is.EqualTo(expected));
            Assert.That(candidate.TerrainSourceIds, Is.EquivalentTo(terrain ? new long[] { 11 } : Array.Empty<long>()));
            Assert.That(candidate.ObjectSourceIds, Is.EquivalentTo(objects ? new long[] { 12 } : Array.Empty<long>()));
            Assert.That(candidate.ObstacleIds, Has.Count.EqualTo(obstacleCount));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OrdinaryFloorAndCharacterDoNotBecomeTerrainOrObstacle(bool ordinaryStructure)
        {
            var candidate = new CellEnvironmentSnapshot(Cell, true, Array.Empty<long>(), Array.Empty<long>(),
                Array.Empty<EntityId>(), ordinaryStructure ? new[] { new EntityId(21) } : Array.Empty<EntityId>(),
                new[] { new EntityId(11) });

            Assert.That(CellEnvironmentQuery.CheckPlacement(candidate), Is.EqualTo(EnvironmentPlacementViolation.None));
            Assert.That(candidate.CharacterIds, Is.EquivalentTo(new[] { new EntityId(11) }));
        }

        [Test]
        public void OutsideCell_ReportsOutsideBoard()
        {
            var outside = new CellEnvironmentSnapshot(new CellCoord(4, 1), false,
                Array.Empty<long>(), Array.Empty<long>(), Array.Empty<EntityId>(),
                Array.Empty<EntityId>(), Array.Empty<EntityId>());
            Assert.That(CellEnvironmentQuery.CheckPlacement(outside), Is.EqualTo(EnvironmentPlacementViolation.OutsideBoard));
        }

        [Test]
        public void KnownCoexistence_IsIndependentOfCandidateCollectionOrder()
        {
            var sources = new MovementTileFact[] { Terrain(), Generic() };
            var structures = new[] { Structure(21), Structure(22) };
            var characters = new[] { Character(31), Character(32) };
            var bindings = new EnvironmentSemanticBindings(new long[] { 11 }, new long[] { 12 },
                new[] { new EntityId(21), new EntityId(22) });
            var reversedBindings = new EnvironmentSemanticBindings(new long[] { 11 }, new long[] { 12 },
                new[] { new EntityId(22), new EntityId(21) });
            var forward = CellEnvironmentQuery.Read(State(Environment(sources), structures), characters, bindings, Cell);
            var reverse = CellEnvironmentQuery.Read(State(Environment(sources.Reverse().ToArray()),
                structures.Reverse().ToArray()), characters.Reverse().ToArray(), reversedBindings, Cell);
            var expected = EnvironmentPlacementViolation.TerrainWithObstacle |
                EnvironmentPlacementViolation.ObstacleWithObject | EnvironmentPlacementViolation.OverlappingObstacles;

            Assert.That(CellEnvironmentQuery.CheckPlacement(forward), Is.EqualTo(expected));
            Assert.That(CellEnvironmentQuery.CheckPlacement(reverse), Is.EqualTo(expected));
            Assert.That(reverse.TerrainSourceIds, Is.EquivalentTo(new long[] { 11 }));
            Assert.That(reverse.ObjectSourceIds, Is.EquivalentTo(new long[] { 12 }));
            Assert.That(reverse.ObstacleIds, Is.EquivalentTo(new[] { new EntityId(21), new EntityId(22) }));
        }

        [Test]
        public void KnownCoexistence_RejectsMissingCandidate()
        {
            Assert.Throws<ArgumentNullException>(() => CellEnvironmentQuery.CheckPlacement(null));
        }
    }
}
