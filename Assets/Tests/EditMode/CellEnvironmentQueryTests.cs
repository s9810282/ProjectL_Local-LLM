using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using static ProjectL.Tests.Core.CellEnvironmentTestData;

namespace ProjectL.Tests.Core
{
    public sealed class CellEnvironmentQueryTests
    {
        private static EnvironmentSemanticBindings Bindings() =>
            new(new long[] { 11 }, new long[] { 12 }, new[] { new EntityId(21) });

        [Test]
        public void Query_ClassifiesStaticTerrainWithoutInventingPresenceBonuses()
        {
            var terrain = new StaticTerrainFact(11, Cell, StaticTerrainKind.HighGround, true,
                new[] { Owner.Player });
            var result = CellEnvironmentQuery.Read(State(Environment(terrain)),
                new[] { Character() }, Bindings(), Cell);
            Assert.That(result.TerrainSourceIds, Is.EqualTo(new[] { 11L }));
            Assert.That(result.CharacterIds, Is.EqualTo(new[] { new EntityId(11) }));
            Assert.That(result.ObjectSourceIds, Is.Empty);
            Assert.That(CellEnvironmentQuery.CheckPlacement(result), Is.EqualTo(EnvironmentPlacementViolation.None));
        }

        [Test]
        public void Bindings_CompareAsSetsWithinSeparateNamespaces()
        {
            var original = new EnvironmentSemanticBindings(new[] { 11L, 12L }, new[] { 21L, 22L },
                new[] { new EntityId(11), new EntityId(12) });
            Assert.That(original.SameValues(new EnvironmentSemanticBindings(new[] { 12L, 11L },
                new[] { 22L, 21L }, new[] { new EntityId(12), new EntityId(11) })), Is.True);
            Assert.That(original.SameValues(new EnvironmentSemanticBindings(new[] { 21L, 22L },
                new[] { 11L, 12L }, new[] { new EntityId(11), new EntityId(12) })), Is.False);
            Assert.That(original.SameValues(new EnvironmentSemanticBindings(new[] { 11L, 12L },
                new[] { 21L, 22L }, new[] { new EntityId(11) })), Is.False);
            Assert.That(original.SameValues(null), Is.False);
        }

        [Test]
        public void Query_ComposesCurrentFactsWithoutOwningThem()
        {
            // 공존 제약을 판단하기 전의 후보 사실을 조회하며 실제 전투로 게시하지 않는다.
            var environment = Environment(Terrain(), Generic(), Generic(13));
            var structure = Structure();
            var actor = Character();
            var state = State(environment, structure);
            var result = CellEnvironmentQuery.Read(state, new[] { actor }, Bindings(), Cell);

            Assert.That(result.Cell, Is.EqualTo(Cell));
            Assert.That(result.IsInside, Is.True);
            Assert.That(result.TerrainSourceIds, Is.EquivalentTo(new long[] { 11 }));
            Assert.That(result.ObjectSourceIds, Is.EquivalentTo(new long[] { 12 }));
            Assert.That(result.CharacterIds, Is.EquivalentTo(new[] { new EntityId(11) }));
            Assert.That(result.ObstacleIds, Is.EquivalentTo(new[] { new EntityId(21) }));
            Assert.That(result.BlockingStructureIds, Is.EquivalentTo(new[] { new EntityId(21) }));
            Assert.That(state.Environment, Is.SameAs(environment));
            Assert.That(state.Structures[0], Is.SameAs(structure));
            Assert.That(actor.Health.CurrentHealth, Is.EqualTo(10));
            Assert.That(environment.Tiles[1].Active, Is.True);
        }

        [Test]
        public void Query_OmitsInactiveDeadAndHandParticipants()
        {
            var bindings = Bindings();
            var active = CellEnvironmentQuery.Read(State(Environment(Terrain(), Generic()), Structure()),
                new[] { Character() }, bindings, Cell);
            var inactive = CellEnvironmentQuery.Read(
                State(Environment(Terrain(active: false), Generic(active: false)), Structure(hp: 0)),
                new[] { Character(hp: 0), Character(31, area: CharacterRuntimeArea.Hand),
                    Character(32, area: CharacterRuntimeArea.EnemyWaiting),
                    Character(33, area: CharacterRuntimeArea.Excluded) }, bindings, Cell);
            var removed = CellEnvironmentQuery.Read(State(MovementEnvironmentFacts.Empty),
                Array.Empty<CharacterCombatFacts>(), bindings, Cell);

            Assert.That(active.ObjectSourceIds, Is.EquivalentTo(new long[] { 12 }));
            AssertEmpty(inactive);
            AssertEmpty(removed);
            Assert.That(active.ObjectSourceIds, Is.EquivalentTo(new long[] { 12 }));
        }

        [Test]
        public void Query_UsesCurrentCellsAndFullOccupiedFootprints()
        {
            var origin = new CellCoord(0, 1);
            var actor = Character(11, 10, CharacterRuntimeArea.Field, origin, Cell);
            var structure = Structure(21, 10, origin, Cell);
            var bindings = Bindings();
            var result = CellEnvironmentQuery.Read(State(Environment(Terrain(), Generic()), structure),
                new[] { actor }, bindings, Cell);
            Assert.That(result.CharacterIds, Is.EquivalentTo(new[] { new EntityId(11) }));
            Assert.That(result.ObstacleIds, Is.EquivalentTo(new[] { new EntityId(21) }));

            var moved = CellEnvironmentQuery.Read(State(Environment(Terrain(cell: origin), Generic(cell: origin)),
                    Structure(21, 10, origin)), new[] { Character(11, 10, CharacterRuntimeArea.Field, origin) },
                bindings, Cell);
            AssertEmpty(moved);
        }

        [Test]
        public void Bindings_RejectMalformedIdentityAndMissingInitialReferences()
        {
            Assert.Throws<ArgumentNullException>(() => new EnvironmentSemanticBindings(null,
                Array.Empty<long>(), Array.Empty<EntityId>()));
            Assert.Throws<ArgumentNullException>(() => new EnvironmentSemanticBindings(Array.Empty<long>(),
                null, Array.Empty<EntityId>()));
            Assert.Throws<ArgumentNullException>(() => new EnvironmentSemanticBindings(Array.Empty<long>(),
                Array.Empty<long>(), null));
            Assert.Throws<ArgumentException>(() => new EnvironmentSemanticBindings(new long[] { 0 },
                Array.Empty<long>(), Array.Empty<EntityId>()));
            Assert.Throws<ArgumentException>(() => new EnvironmentSemanticBindings(Array.Empty<long>(),
                new long[] { -1 }, Array.Empty<EntityId>()));
            Assert.Throws<ArgumentException>(() => new EnvironmentSemanticBindings(new long[] { 11, 11 },
                Array.Empty<long>(), Array.Empty<EntityId>()));
            Assert.Throws<ArgumentException>(() => new EnvironmentSemanticBindings(Array.Empty<long>(),
                new long[] { 12, 12 }, Array.Empty<EntityId>()));
            Assert.Throws<ArgumentException>(() => new EnvironmentSemanticBindings(new long[] { 11 },
                new long[] { 11 }, Array.Empty<EntityId>()));
            Assert.Throws<ArgumentException>(() => new EnvironmentSemanticBindings(Array.Empty<long>(),
                Array.Empty<long>(), new[] { EntityId.Empty }));
            Assert.Throws<ArgumentException>(() => new EnvironmentSemanticBindings(Array.Empty<long>(),
                Array.Empty<long>(), new[] { new EntityId(21), new EntityId(21) }));

            var bindings = Bindings();
            Assert.Throws<ArgumentException>(() => bindings.ValidateInitialReferences(
                Environment(Terrain()), new[] { Structure() }));
            Assert.Throws<ArgumentException>(() => bindings.ValidateInitialReferences(
                Environment(Generic()), new[] { Structure() }));
            Assert.Throws<ArgumentException>(() => bindings.ValidateInitialReferences(
                Environment(Terrain(), Generic()), Array.Empty<StructureCombatFacts>()));
            Assert.Throws<ArgumentNullException>(() => bindings.ValidateInitialReferences(null,
                Array.Empty<StructureCombatFacts>()));
            Assert.Throws<ArgumentNullException>(() => bindings.ValidateInitialReferences(
                MovementEnvironmentFacts.Empty, null));
            Assert.Throws<ArgumentException>(() => bindings.ValidateInitialReferences(
                Environment(Terrain(), Generic()), new StructureCombatFacts[] { null }));
        }

        [Test]
        public void Bindings_ValidateReferencesWithoutInferringGameplayRolesOrRequiringActivity()
        {
            var bindings = new EnvironmentSemanticBindings(new long[] { 11 }, new long[] { 12 },
                new[] { new EntityId(11) });
            Assert.DoesNotThrow(() => bindings.ValidateInitialReferences(
                Environment(Generic(11, active: false), Terrain(12, active: false)),
                new[] { Structure(11, 0) }));
            Assert.DoesNotThrow(() => EnvironmentSemanticBindings.Empty.ValidateInitialReferences(
                Environment(Terrain(), Generic()), Array.Empty<StructureCombatFacts>()));
        }

        [Test]
        public void BindingsAndSnapshots_DefensivelyCopyInputCollections()
        {
            var terrain = new long[] { 11 };
            var objects = new long[] { 12 };
            var obstacles = new[] { new EntityId(21) };
            var blockers = new[] { new EntityId(21) };
            var characters = new[] { new EntityId(11) };
            var bindings = new EnvironmentSemanticBindings(terrain, objects, obstacles);
            var snapshot = new CellEnvironmentSnapshot(Cell, true, terrain, objects, obstacles, blockers, characters);
            terrain[0] = 99;
            objects[0] = 98;
            obstacles[0] = new EntityId(97);
            blockers[0] = new EntityId(96);
            characters[0] = new EntityId(95);

            Assert.That(bindings.TerrainSourceIds, Is.EquivalentTo(new long[] { 11 }));
            Assert.That(bindings.ObjectSourceIds, Is.EquivalentTo(new long[] { 12 }));
            Assert.That(bindings.ObstacleEntityIds, Is.EquivalentTo(new[] { new EntityId(21) }));
            Assert.That(snapshot.TerrainSourceIds, Is.EquivalentTo(new long[] { 11 }));
            Assert.That(snapshot.ObjectSourceIds, Is.EquivalentTo(new long[] { 12 }));
            Assert.That(snapshot.ObstacleIds, Is.EquivalentTo(new[] { new EntityId(21) }));
            Assert.That(snapshot.BlockingStructureIds, Is.EquivalentTo(new[] { new EntityId(21) }));
            Assert.That(snapshot.CharacterIds, Is.EquivalentTo(new[] { new EntityId(11) }));
            Assert.Throws<NotSupportedException>(() => ((IList<long>)bindings.TerrainSourceIds)[0] = 99);
            Assert.Throws<NotSupportedException>(() => ((IList<EntityId>)snapshot.ObstacleIds).Clear());
        }

        [Test]
        public void Snapshot_RejectsMalformedIdentitiesSubsetAndOutsideParticipants()
        {
            Assert.Throws<ArgumentNullException>(() => new CellEnvironmentSnapshot(Cell, true,
                null, Array.Empty<long>(), Array.Empty<EntityId>(), Array.Empty<EntityId>(), Array.Empty<EntityId>()));
            Assert.Throws<ArgumentNullException>(() => new CellEnvironmentSnapshot(Cell, true,
                Array.Empty<long>(), null, Array.Empty<EntityId>(), Array.Empty<EntityId>(), Array.Empty<EntityId>()));
            Assert.Throws<ArgumentNullException>(() => new CellEnvironmentSnapshot(Cell, true,
                Array.Empty<long>(), Array.Empty<long>(), null, Array.Empty<EntityId>(), Array.Empty<EntityId>()));
            Assert.Throws<ArgumentNullException>(() => new CellEnvironmentSnapshot(Cell, true,
                Array.Empty<long>(), Array.Empty<long>(), Array.Empty<EntityId>(), null, Array.Empty<EntityId>()));
            Assert.Throws<ArgumentNullException>(() => new CellEnvironmentSnapshot(Cell, true,
                Array.Empty<long>(), Array.Empty<long>(), Array.Empty<EntityId>(), Array.Empty<EntityId>(), null));
            Assert.Throws<ArgumentException>(() => Snapshot(terrain: new long[] { 0 }));
            Assert.Throws<ArgumentException>(() => Snapshot(objects: new long[] { -1 }));
            Assert.Throws<ArgumentException>(() => Snapshot(terrain: new long[] { 11, 11 }));
            Assert.Throws<ArgumentException>(() => Snapshot(objects: new long[] { 12, 12 }));
            Assert.Throws<ArgumentException>(() => Snapshot(obstacles: new[] { EntityId.Empty }));
            Assert.Throws<ArgumentException>(() => Snapshot(blockers: new[] { EntityId.Empty }));
            Assert.Throws<ArgumentException>(() => Snapshot(characters: new[] { EntityId.Empty }));
            Assert.Throws<ArgumentException>(() => Snapshot(obstacles: new[] { new EntityId(21), new EntityId(21) },
                blockers: new[] { new EntityId(21) }));
            Assert.Throws<ArgumentException>(() => Snapshot(blockers: new[] { new EntityId(21), new EntityId(21) }));
            Assert.Throws<ArgumentException>(() => Snapshot(characters: new[] { new EntityId(11), new EntityId(11) }));
            Assert.Throws<ArgumentException>(() => Snapshot(obstacles: new[] { new EntityId(21) }));
            Assert.Throws<ArgumentException>(() => Snapshot(inside: false, terrain: new long[] { 11 }));
            Assert.Throws<ArgumentException>(() => Snapshot(inside: false, objects: new long[] { 12 }));
            Assert.Throws<ArgumentException>(() => Snapshot(inside: false, blockers: new[] { new EntityId(21) }));
            Assert.Throws<ArgumentException>(() => Snapshot(inside: false, characters: new[] { new EntityId(11) }));
        }

        [Test]
        public void Query_DifferentBattlesDoNotShareCellState()
        {
            var firstState = State(Environment(Terrain()));
            var secondState = State(Environment(Terrain(41)));
            var firstBindings = new EnvironmentSemanticBindings(new long[] { 11 }, Array.Empty<long>(), Array.Empty<EntityId>());
            var secondBindings = new EnvironmentSemanticBindings(new long[] { 41 }, Array.Empty<long>(), Array.Empty<EntityId>());
            var first = CellEnvironmentQuery.Read(firstState, Array.Empty<CharacterCombatFacts>(), firstBindings, Cell);
            var second = CellEnvironmentQuery.Read(secondState, Array.Empty<CharacterCombatFacts>(), secondBindings, Cell);
            Assert.That(first.TerrainSourceIds, Is.EquivalentTo(new long[] { 11 }));
            Assert.That(second.TerrainSourceIds, Is.EquivalentTo(new long[] { 41 }));
            Assert.That(CellEnvironmentQuery.Read(firstState, Array.Empty<CharacterCombatFacts>(), firstBindings, Cell)
                .TerrainSourceIds, Is.EquivalentTo(new long[] { 11 }));
        }

        [TestCase(-1, 1)]
        [TestCase(1, -1)]
        [TestCase(4, 1)]
        [TestCase(1, 5)]
        public void Query_OutsideBoardIsEmpty(int x, int y)
        {
            var cell = new CellCoord(x, y);
            var result = CellEnvironmentQuery.Read(State(Environment(Terrain(cell: cell)), Structure(21, 10, cell)),
                new[] { Character(11, 10, CharacterRuntimeArea.Field, cell) }, Bindings(), cell);
            Assert.That(result.Cell, Is.EqualTo(cell));
            Assert.That(result.IsInside, Is.False);
            AssertEmpty(result);
        }

        [Test]
        public void Query_LegacySourcesAndHeadquartersAreNotReclassified()
        {
            var result = CellEnvironmentQuery.Read(State(Environment(Terrain(), Generic(isMine: true)), Structure()),
                Array.Empty<CharacterCombatFacts>(), EnvironmentSemanticBindings.Empty, Cell);
            Assert.That(result.TerrainSourceIds, Is.Empty);
            Assert.That(result.ObjectSourceIds, Is.Empty);
            Assert.That(result.ObstacleIds, Is.Empty);
            Assert.That(result.BlockingStructureIds, Is.EquivalentTo(new[] { new EntityId(21) }));
        }

        [TestCase(1, 1)]
        [TestCase(4, 1)]
        public void Query_StateFacadeMatchesNarrowSpatialComposition(int x, int y)
        {
            var state = State(Environment(Terrain(), Generic()), Structure());
            var characters = new[] { Character() };
            var bindings = Bindings();
            var cell = new CellCoord(x, y);
            var facade = CellEnvironmentQuery.Read(state, characters, bindings, cell);
            var narrow = CellEnvironmentQuery.ReadSpatial(state.BoardWidth, state.BoardHeight,
                state.Environment, state.Structures, characters, bindings, cell);
            Assert.That(narrow.Cell, Is.EqualTo(facade.Cell));
            Assert.That(narrow.IsInside, Is.EqualTo(facade.IsInside));
            Assert.That(narrow.TerrainSourceIds, Is.EquivalentTo(facade.TerrainSourceIds));
            Assert.That(narrow.ObjectSourceIds, Is.EquivalentTo(facade.ObjectSourceIds));
            Assert.That(narrow.ObstacleIds, Is.EquivalentTo(facade.ObstacleIds));
            Assert.That(narrow.BlockingStructureIds, Is.EquivalentTo(facade.BlockingStructureIds));
            Assert.That(narrow.CharacterIds, Is.EquivalentTo(facade.CharacterIds));
            if (narrow.IsInside)
            {
                Assert.That(narrow.TerrainSourceIds, Is.EquivalentTo(new long[] { 11 }));
                Assert.That(narrow.CharacterIds, Is.EquivalentTo(new[] { new EntityId(11) }));
            }
            else AssertEmpty(narrow);
        }

        [Test]
        public void Query_RejectsEntityIdentitySharedByCharacterAndStructure()
        {
            var state = State(Environment(Terrain()), Structure(11));
            var characters = new[] { Character(11) };
            var bindings = new EnvironmentSemanticBindings(new long[] { 11 },
                Array.Empty<long>(), Array.Empty<EntityId>());
            foreach (var cell in new[] { Cell, new CellCoord(4, 5) })
            {
                Assert.Throws<ArgumentException>(() => CellEnvironmentQuery.Read(state, characters, bindings, cell));
                Assert.Throws<ArgumentException>(() => CellEnvironmentQuery.ReadSpatial(state.BoardWidth,
                    state.BoardHeight, state.Environment, state.Structures, characters, bindings, cell));
            }
        }

        [Test]
        public void Query_RejectsMalformedSharedInputsBeforeReturningOutsideSnapshot()
        {
            var state = State(MovementEnvironmentFacts.Empty);
            var empty = Array.Empty<CharacterCombatFacts>();
            var bindings = EnvironmentSemanticBindings.Empty;
            var outside = new CellCoord(4, 5);
            Assert.Throws<ArgumentNullException>(() => CellEnvironmentQuery.Read(null, empty, bindings, Cell));
            Assert.Throws<ArgumentNullException>(() => CellEnvironmentQuery.Read(state, null, bindings, outside));
            Assert.Throws<ArgumentNullException>(() => CellEnvironmentQuery.Read(state, empty, null, outside));
            Assert.Throws<ArgumentException>(() => CellEnvironmentQuery.Read(state, new[] { Character(), Character() }, bindings, outside));
            Assert.Throws<ArgumentException>(() => CellEnvironmentQuery.Read(state, new CharacterCombatFacts[] { null }, bindings, outside));
            Assert.Throws<ArgumentException>(() => CellEnvironmentQuery.Read(State(MovementEnvironmentFacts.Empty, Structure(), Structure()), empty, bindings, outside));
            Assert.Throws<ArgumentOutOfRangeException>(() => CellEnvironmentQuery.ReadSpatial(0, 5, state.Environment, state.Structures, empty, bindings, Cell));
            Assert.Throws<ArgumentOutOfRangeException>(() => CellEnvironmentQuery.ReadSpatial(4, 0, state.Environment, state.Structures, empty, bindings, Cell));
            Assert.Throws<ArgumentNullException>(() => CellEnvironmentQuery.ReadSpatial(4, 5, null, state.Structures, empty, bindings, outside));
            Assert.Throws<ArgumentNullException>(() => CellEnvironmentQuery.ReadSpatial(4, 5, state.Environment, null, empty, bindings, outside));
            Assert.Throws<ArgumentException>(() => CellEnvironmentQuery.ReadSpatial(4, 5, state.Environment,
                new StructureCombatFacts[] { null }, empty, bindings, outside));
            Assert.Throws<ArgumentNullException>(() => CellEnvironmentQuery.ReadSpatial(4, 5, state.Environment, state.Structures, null, bindings, outside));
            Assert.Throws<ArgumentNullException>(() => CellEnvironmentQuery.ReadSpatial(4, 5, state.Environment, state.Structures, empty, null, outside));
            Assert.Throws<ArgumentException>(() => CellEnvironmentQuery.ReadSpatial(4, 5, state.Environment, state.Structures,
                new[] { Character(), Character() }, bindings, outside));
            Assert.Throws<ArgumentException>(() => CellEnvironmentQuery.ReadSpatial(4, 5, state.Environment,
                new[] { Structure(), Structure() }, empty, bindings, outside));
        }

        private static void AssertEmpty(CellEnvironmentSnapshot snapshot)
        {
            Assert.That(snapshot.TerrainSourceIds, Is.Empty);
            Assert.That(snapshot.ObjectSourceIds, Is.Empty);
            Assert.That(snapshot.ObstacleIds, Is.Empty);
            Assert.That(snapshot.BlockingStructureIds, Is.Empty);
            Assert.That(snapshot.CharacterIds, Is.Empty);
        }

        private static CellEnvironmentSnapshot Snapshot(bool inside = true,
            long[] terrain = null, long[] objects = null, EntityId[] obstacles = null,
            EntityId[] blockers = null, EntityId[] characters = null) =>
            new(Cell, inside, terrain ?? Array.Empty<long>(), objects ?? Array.Empty<long>(),
                obstacles ?? Array.Empty<EntityId>(), blockers ?? Array.Empty<EntityId>(),
                characters ?? Array.Empty<EntityId>());
    }
}
