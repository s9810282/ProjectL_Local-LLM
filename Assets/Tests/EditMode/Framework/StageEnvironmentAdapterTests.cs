using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Framework
{
    public sealed class StageEnvironmentAdapterTests
    {
        [Test]
        public void TornadoTypedPlacement_ClassifiesTerrainAndPreservesDueAndVisibility()
        {
            var source = new TornadoSourceFact(99, new CellCoord(2, 0), new[] { Owner.Player, Owner.Enemy });
            var result = StageEnvironmentAdapter.Read(4, 5, new[] { Placement() }, Array.Empty<StaticTerrainPlacementData>(),
                new InitialObjectPlacementSpec[] { new SheriffBadgePlacementSpec(100, source.Cell, new[] { Owner.Player }) },
                new[] { source });
            Assert.That(result.Environment.Tiles.OfType<TornadoSourceFact>().Single().SameValues(source), Is.True);
            Assert.That(result.Bindings.TerrainSourceIds, Is.EquivalentTo(new[] { 11L, 99L }));
            Assert.That(result.Bindings.ObjectSourceIds, Is.EqualTo(new[] { 100L }));
        }

        [Test]
        public void TornadoTypedPlacement_RejectsOtherTerrainOnSameCell()
        {
            Assert.Throws<NotSupportedException>(() => StageEnvironmentAdapter.Read(4, 5, new[] { Placement() },
                Array.Empty<StaticTerrainPlacementData>(), tornadoPlacements:
                new[] { new TornadoSourceFact(99, Placement().Cell, new[] { Owner.Player }) }));
        }

        [Test]
        public void MinePlacement_PreservesExplicitDamageVisibilityAndAuthoredOrder()
        {
            var stage = Stage(objects: new InitialObjectPlacementSpec[] {
                new SheriffBadgePlacementSpec(99, CellCoord.Zero, new[] { Owner.Player }),
                new MinePlacementSpec(1, CellCoord.Zero, 7, new[] { Owner.Enemy }) });
            var result = BattleDefinitionAdapter.Read(new LayoutDatabase(stage, Stage()), 1, 1101, Array.Empty<OwnedCharacterRecord>());
            var mine = (MineSourceFact)result.Environment.Tiles.Single(t => t.Id == 1);
            Assert.That(mine.Damage, Is.EqualTo(7));
            Assert.That(mine.EntryOrdinal, Is.EqualTo(2));
            Assert.That(mine.IsMine, Is.True);
            Assert.That(mine.KnownTo, Is.EqualTo(new[] { Owner.Enemy }));
            Assert.That(result.EnvironmentBindings.ObjectSourceIds, Is.EqualTo(new[] { 99L, 1L }));
        }

        private static TerrainPresencePlacementData Placement(long id = 11, CellCoord? cell = null) =>
            new(id, cell ?? new CellCoord(1, 0), new TerrainPresenceDefinitionData(2, 3), new[] { Owner.Player });

        private static CombatStageDetail Stage(IReadOnlyList<TerrainPresencePlacementData> placements = null,
            IReadOnlyList<StaticTerrainPlacementData> staticTerrain = null, IReadOnlyList<InitialObstaclePlacementSpec> obstacles = null,
            IReadOnlyList<InitialObjectPlacementSpec> objects = null) =>
            new(42, "shared-field", 1, 0, 1, 321, Array.Empty<EntityPlacement>(),
                Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.None,
                new CellCoord(1, 0), Rotation4.R0, new CellCoord(3, 4), Rotation4.R180, null, placements, staticTerrain, obstacles, objects);

        [Test]
        public void ObjectOrder_UsesAuthoredListAndPassesThroughBattleDefinitionAdapter()
        {
            var items = new InitialObjectPlacementSpec[] {
                new SheriffBadgePlacementSpec(99, CellCoord.Zero, new[] { Owner.Player }),
                new SheriffBadgePlacementSpec(1, CellCoord.Zero, new[] { Owner.Enemy }) };
            var stage = Stage(staticTerrain: new[] { new StaticTerrainPlacementData(71, CellCoord.Zero,
                StaticTerrainKind.HighGround, new[] { Owner.Player }) }, objects: items);
            items[0] = null;
            var result = BattleDefinitionAdapter.Read(new LayoutDatabase(stage, Stage()), 1, 1101, Array.Empty<OwnedCharacterRecord>());
            var objects = ObjectArrivalCalculator.OrderedAt(result.Environment, result.EnvironmentBindings, CellCoord.Zero);
            Assert.That(objects.Select(o => o.Id), Is.EqualTo(new[] { 99L, 1L }));
            Assert.That(objects.Select(o => o.EntryOrdinal), Is.EqualTo(new[] { 1L, 2L }));
            Assert.That(objects[1].KnownTo, Is.EqualTo(new[] { Owner.Enemy }));
            var state = new CombatCommandState(4, 5, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                Array.Empty<StructureCombatFacts>(), Array.Empty<ThornFieldCellSnapshot>(), Array.Empty<CombatTimelineEntrySnapshot>(),
                environment: result.Environment);
            Assert.That(state.EnvironmentProgress.NextObjectEntryOrdinal, Is.EqualTo(3));
        }

        [Test]
        public void ObstaclePlacement_PassesThroughActualBattleDefinitionAdapter()
        {
            var placement = new InitialObstaclePlacementSpec(new StructureSpec(EntityKey.Structure_PowderBarrel,
                1, 1, new[] { CellCoord.Zero }, 10, false, 0), ObstacleContentKind.PowderBarrel, new CellCoord(2, 2), Rotation4.R90, 7);
            var source = new[] { placement };
            var stage = Stage(obstacles: source);
            source[0] = null;
            var result = BattleDefinitionAdapter.Read(new LayoutDatabase(stage, Stage()), 1, 1101, Array.Empty<OwnedCharacterRecord>());
            Assert.That(result.ObstaclePlacements.Single(), Is.SameAs(placement));
            Assert.That(result.ObstaclePlacements.Single().ExplosionDamage, Is.EqualTo(7));
            Assert.That(result.EnvironmentBindings.ObstacleEntityIds, Is.Empty);
        }

        [Test]
        public void StaticTerrain_StageAdapterPreservesKindIdentityAndVisibility()
        {
            var terrain = new[] {
                new StaticTerrainPlacementData(21, new CellCoord(0, 0), StaticTerrainKind.HighGround, new[] { Owner.Player }),
                new StaticTerrainPlacementData(22, new CellCoord(2, 0), StaticTerrainKind.Sandstorm, new[] { Owner.Enemy }) };
            var database = new LayoutDatabase(Stage(new[] { Placement() }, terrain), Stage());
            var result = BattleDefinitionAdapter.Read(database, 1, 1101, Array.Empty<OwnedCharacterRecord>());
            Assert.That(result.Environment.Tiles.Count, Is.EqualTo(3));
            var high = (StaticTerrainFact)result.Environment.Tiles.Single(t => t.Id == 21);
            Assert.That(high.Kind, Is.EqualTo(StaticTerrainKind.HighGround));
            Assert.That(high.Cell, Is.EqualTo(CellCoord.Zero));
            Assert.That(high.KnownTo, Is.EqualTo(new[] { Owner.Player }));
            Assert.That(result.EnvironmentBindings.TerrainSourceIds, Is.EquivalentTo(new long[] { 11, 21, 22 }));
        }

        [Test]
        public void StaticTerrain_RejectsDuplicateIdentityBoundsAndUnsupportedTerrainComposition()
        {
            var high = new StaticTerrainPlacementData(21, CellCoord.Zero, StaticTerrainKind.HighGround, Array.Empty<Owner>());
            var sand = new StaticTerrainPlacementData(22, CellCoord.Zero, StaticTerrainKind.Sandstorm, Array.Empty<Owner>());
            Assert.Throws<NotSupportedException>(() => StageEnvironmentAdapter.Read(4, 5,
                Array.Empty<TerrainPresencePlacementData>(), new[] { high, sand }));
            Assert.Throws<ArgumentException>(() => Stage(new[] { Placement(21) }, new[] { high }));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageEnvironmentAdapter.Read(4, 5,
                Array.Empty<TerrainPresencePlacementData>(), new[] {
                    new StaticTerrainPlacementData(21, new CellCoord(4, 0), StaticTerrainKind.HighGround, Array.Empty<Owner>()) }));
        }

        [Test]
        public void Layout_ConvertsExplicitIdentityValuesAndVisibility()
        {
            var result = StageEnvironmentAdapter.Read(4, 5, new[] { Placement() });
            var tile = (DestinationTileModifierFact)result.Environment.Tiles.Single();
            Assert.That(tile.Id, Is.EqualTo(11));
            Assert.That(tile.Cell, Is.EqualTo(new CellCoord(1, 0)));
            Assert.That(tile.AttackBonus, Is.EqualTo(2));
            Assert.That(tile.MaxHealthBonus, Is.EqualTo(3));
            Assert.That(tile.KnownTo, Is.EquivalentTo(new[] { Owner.Player }));
            Assert.That(tile.Active, Is.True);
            Assert.That(tile.Priority, Is.Zero);
            Assert.That(tile.RuleKey, Is.EqualTo(DestinationTileModifierFact.Key));
            Assert.That(result.Bindings.TerrainSourceIds, Is.EquivalentTo(new long[] { 11 }));
            Assert.That(result.Bindings.ObjectSourceIds, Is.Empty);
            Assert.That(result.Bindings.ObstacleEntityIds, Is.Empty);
        }

        [TestCase(-1, 0)]
        [TestCase(4, 0)]
        [TestCase(1, -1)]
        [TestCase(1, 5)]
        public void Layout_RejectsOutOfFieldBounds(int x, int y)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StageEnvironmentAdapter.Read(4, 5,
                new[] { Placement(cell: new CellCoord(x, y)) }));
        }

        [Test]
        public void Layout_RejectsBoundsDuplicatesAndUnsupportedComposition()
        {
            Assert.Throws<ArgumentException>(() => StageEnvironmentAdapter.Read(4, 5,
                new[] { Placement(), Placement(cell: new CellCoord(2, 0)) }));
            Assert.Throws<NotSupportedException>(() => StageEnvironmentAdapter.Read(4, 5,
                new[] { Placement(), Placement(12) }));
            Assert.Throws<ArgumentException>(() => StageEnvironmentAdapter.Read(4, 5,
                new TerrainPresencePlacementData[] { null }));
            Assert.Throws<ArgumentNullException>(() => StageEnvironmentAdapter.Read(4, 5, null));
        }

        [Test]
        public void Layout_DefinitionReuseDoesNotCouplePlacementState()
        {
            var definition = new TerrainPresenceDefinitionData(-2, -3);
            var placements = new[] {
                new TerrainPresencePlacementData(41, new CellCoord(3, 4), definition, Array.Empty<Owner>()),
                new TerrainPresencePlacementData(11, new CellCoord(0, 0), definition, new[] { Owner.Player, Owner.Enemy }) };
            var result = StageEnvironmentAdapter.Read(4, 5, placements);
            Assert.That(result.Environment.Tiles.Single(t => t.Id == 41).Cell, Is.EqualTo(new CellCoord(3, 4)));
            Assert.That(result.Environment.Tiles.Single(t => t.Id == 41).KnownTo, Is.Empty);
            var other = (DestinationTileModifierFact)result.Environment.Tiles.Single(t => t.Id == 11);
            Assert.That(other.Cell, Is.EqualTo(new CellCoord(0, 0)));
            Assert.That(other.KnownTo, Is.EquivalentTo(new[] { Owner.Player, Owner.Enemy }));
            Assert.That(other.AttackBonus, Is.EqualTo(-2));
            Assert.That(other.MaxHealthBonus, Is.EqualTo(-3));
            Assert.That(result.Bindings.TerrainSourceIds, Is.EquivalentTo(new long[] { 41, 11 }));
        }

        [Test]
        public void StageAndPlacement_CopyCollectionsAndRejectMalformedAuthoredFacts()
        {
            var visibility = new[] { Owner.Player };
            var definition = new TerrainPresenceDefinitionData(int.MinValue, int.MaxValue);
            var placement = new TerrainPresencePlacementData(11, new CellCoord(1, 0), definition, visibility);
            var placements = new[] { placement };
            var stage = Stage(placements);
            visibility[0] = Owner.Enemy;
            placements[0] = Placement(99);
            Assert.That(stage.TerrainPresencePlacements.Single(), Is.SameAs(placement));
            Assert.That(placement.KnownTo, Is.EquivalentTo(new[] { Owner.Player }));
            Assert.That(placement.Definition.AttackBonus, Is.EqualTo(int.MinValue));
            Assert.That(placement.Definition.MaxHealthBonus, Is.EqualTo(int.MaxValue));
            Assert.Throws<NotSupportedException>(() => ((IList<Owner>)placement.KnownTo).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<TerrainPresencePlacementData>)stage.TerrainPresencePlacements).Clear());
            Assert.Throws<ArgumentOutOfRangeException>(() => new TerrainPresencePlacementData(0,
                CellCoord.Zero, definition, Array.Empty<Owner>()));
            Assert.Throws<ArgumentNullException>(() => new TerrainPresencePlacementData(11,
                CellCoord.Zero, null, Array.Empty<Owner>()));
            Assert.Throws<ArgumentNullException>(() => new TerrainPresencePlacementData(11,
                CellCoord.Zero, definition, null));
            Assert.Throws<ArgumentException>(() => new TerrainPresencePlacementData(11,
                CellCoord.Zero, definition, new[] { Owner.Player, Owner.Player }));
            Assert.Throws<ArgumentException>(() => new TerrainPresencePlacementData(11,
                CellCoord.Zero, definition, new[] { (Owner)999 }));
            Assert.Throws<ArgumentException>(() => Stage(new TerrainPresencePlacementData[] { null }));
            Assert.Throws<ArgumentException>(() => Stage(new[] { Placement(), Placement() }));
        }

        [Test]
        public void Adapter_UsesRequestedStagesLayoutOnSharedField()
        {
            var database = new LayoutDatabase(Stage(new[] { Placement(11) }),
                Stage(new[] { Placement(42, new CellCoord(2, 4)) }));
            var owned = new[] { new OwnedCharacterRecord(new CharacterInstanceId(1),
                EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main) };
            var first = BattleDefinitionAdapter.Read(database, 1, 1101, owned);
            var second = BattleDefinitionAdapter.Read(database, 1, 1102, owned);
            Assert.That(database.StageReads, Is.EqualTo(new[] { 1101, 1102 }));
            Assert.That(database.FieldReads, Is.EqualTo(new[] { "shared-field", "shared-field" }));
            Assert.That(first.Environment.Tiles.Single().Id, Is.EqualTo(11));
            Assert.That(first.Environment.Tiles.Single().Cell, Is.EqualTo(new CellCoord(1, 0)));
            Assert.That(first.EnvironmentBindings.TerrainSourceIds, Is.EquivalentTo(new long[] { 11 }));
            Assert.That(second.Environment.Tiles.Single().Id, Is.EqualTo(42));
            Assert.That(second.Environment.Tiles.Single().Cell, Is.EqualTo(new CellCoord(2, 4)));
            Assert.That(second.EnvironmentBindings.TerrainSourceIds, Is.EquivalentTo(new long[] { 42 }));
            Assert.That(second.Stage.MapWidth, Is.EqualTo(4));
            Assert.That(second.Stage.MapHeight, Is.EqualTo(5));
            Assert.That(second.Stage.NightLength, Is.EqualTo(321));
            Assert.That(second.Stage.TimelineSeed, Is.EqualTo(42));
            Assert.That(second.MainAnchor, Is.EqualTo(new CellCoord(1, 0)));
            Assert.That(second.Units.Select(u => u.Key), Is.EquivalentTo(new[] { EntityKey.Unit_MainCharacter, EntityKey.Unit_Akimbo }));
            Assert.That(second.Economy.InitialGold, Is.EqualTo(27));
            Assert.That(second.AttackProfiles, Is.Empty);
        }

        [Test]
        public void LegacyStage_ProducesEmptyEnvironment()
        {
            var database = new LayoutDatabase(Stage(), Stage());
            var result = BattleDefinitionAdapter.Read(database, 1, 1101, Array.Empty<OwnedCharacterRecord>());
            Assert.That(database.Stages[1101].TerrainPresencePlacements, Is.Empty);
            Assert.That(result.Environment.Tiles, Is.Empty);
            Assert.That(result.EnvironmentBindings.TerrainSourceIds, Is.Empty);
            Assert.That(result.EnvironmentBindings.ObjectSourceIds, Is.Empty);
            Assert.That(result.EnvironmentBindings.ObstacleEntityIds, Is.Empty);
            var legacy = new BattleInitialization(result.Stage, result.MainAnchor, result.MainRotation,
                result.Units, result.Structures, result.Categories);
            Assert.That(legacy.Environment, Is.SameAs(MovementEnvironmentFacts.Empty));
            Assert.That(legacy.EnvironmentBindings, Is.SameAs(EnvironmentSemanticBindings.Empty));
            var bound = new BattleInitialization(result.Stage, result.MainAnchor, result.MainRotation,
                result.Units, result.Structures, result.Categories, environment: Environment(Modifier(11)),
                environmentBindings: new EnvironmentSemanticBindings(new long[] { 11 }, Array.Empty<long>(), Array.Empty<EntityId>()));
            Assert.That(bound.Environment.Tiles.Single().Id, Is.EqualTo(11));
            Assert.That(bound.EnvironmentBindings.TerrainSourceIds, Is.EquivalentTo(new long[] { 11 }));
        }

        [Test]
        public void InitialEnvironment_ValidatesCompositionWithoutOccupancyOrClassification()
        {
            var overlapping = Environment(Modifier(11), Modifier(12));
            Assert.Throws<NotSupportedException>(() => CellEnvironmentQuery.ValidateInitialEnvironment(4, 5, overlapping));
            var oneActive = Environment(Modifier(11), Modifier(12, active: false));
            Assert.DoesNotThrow(() => CellEnvironmentQuery.ValidateInitialEnvironment(4, 5, oneActive));
            Assert.That(oneActive.Tiles[1].Active, Is.False);
            Assert.That(oneActive.Tiles[0].KnownTo, Is.Empty);
            Assert.DoesNotThrow(() => CellEnvironmentQuery.ValidateInitialEnvironment(4, 5, MovementEnvironmentFacts.Empty));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void InitialEnvironment_ValidatesEverySourceBounds(bool active)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CellEnvironmentQuery.ValidateInitialEnvironment(4, 5,
                Environment(Modifier(11, new CellCoord(4, 0), active))));
            Assert.Throws<ArgumentOutOfRangeException>(() => CellEnvironmentQuery.ValidateInitialEnvironment(4, 5,
                Environment(new GenericSource(new CellCoord(1, 5), active))));
        }

        [Test]
        public void InitialEnvironment_RejectsInvalidDimensionsAndNull()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CellEnvironmentQuery.ValidateInitialEnvironment(0, 5, MovementEnvironmentFacts.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => CellEnvironmentQuery.ValidateInitialEnvironment(4, 0, MovementEnvironmentFacts.Empty));
            Assert.Throws<ArgumentNullException>(() => CellEnvironmentQuery.ValidateInitialEnvironment(4, 5, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageEnvironmentAdapter.Read(0, 5, Array.Empty<TerrainPresencePlacementData>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageEnvironmentAdapter.Read(4, 0, Array.Empty<TerrainPresencePlacementData>()));
        }

        private static MovementEnvironmentFacts Environment(params MovementTileFact[] sources) => new(sources);
        private static DestinationTileModifierFact Modifier(long id, CellCoord? cell = null, bool active = true) =>
            new(id, cell ?? new CellCoord(2, 2), active, Array.Empty<Owner>(), 2, 3);

        private sealed class GenericSource : MovementTileFact
        {
            internal GenericSource(CellCoord cell, bool active) : base(99, cell, "fixture-generic", 0, active, Array.Empty<Owner>()) { }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => throw new NotSupportedException();
        }

        // 운영 SO나 조회 테이블을 바꾸지 않고 실제 DTO 변환 경로를 검증한다.
        private sealed class LayoutDatabase : IDataBaseQueryContext, IInGameQuery, IStageTableQuery,
            IFieldTableQuery, IUnitRosterTableQuery, IStageEconomyTableQuery, ICategoryConfigTableQuery
        {
            private readonly CharacterRosterDatabase _roster = new();
            private readonly FieldData _field = new("shared-field", 4, 5, 8, 1, 1);
            private readonly UnitRosterData _units = new(1, new[] { EntityKey.Unit_Akimbo });
            private readonly StageEconomyData _economy = new(1, 27, 6);
            internal readonly Dictionary<int, CombatStageDetail> Stages;
            internal readonly List<int> StageReads = new();
            internal readonly List<string> FieldReads = new();
            internal LayoutDatabase(CombatStageDetail first, CombatStageDetail second) => Stages = new() { [1101] = first, [1102] = second };
            public StageRootData GetStage(int key) { StageReads.Add(key); return new(key, 1, StageMapRole.Regular, Stages[key]); }
            public IReadOnlyList<StageRootData> GetStages(int key) => Stages.Keys.Select(GetStage).ToArray();
            public FieldData GetField(string key) { FieldReads.Add(key); return key == _field.FieldId ? _field : throw new KeyNotFoundException(); }
            public bool TryGetField(string key, out FieldData value) { value = key == _field.FieldId ? _field : null; return value != null; }
            public IReadOnlyCollection<FieldData> GetAllFields() => new[] { _field };
            public UnitRosterData GetUnitRoster(int key) => key == 1 ? _units : throw new KeyNotFoundException();
            public bool TryGetUnitRoster(int key, out UnitRosterData value) { value = key == 1 ? _units : null; return value != null; }
            public IReadOnlyCollection<UnitRosterData> GetAllUnitRosters() => new[] { _units };
            public StageEconomyData GetStageEconomy(int key) => key == 1 ? _economy : throw new KeyNotFoundException();
            public bool TryGetStageEconomy(int key, out StageEconomyData value) { value = key == 1 ? _economy : null; return value != null; }
            public IReadOnlyCollection<StageEconomyData> GetAllStageEconomies() => new[] { _economy };
            public CategoryConfigData Config => new(37, Rotation4.R90);
            public IInGameQuery InGameQuery => this;
            public IStageTableQuery StageTableQuery => this;
            public IFieldTableQuery FieldTableQuery => this;
            public IUnitRosterTableQuery UnitRosterTableQuery => this;
            public IStageEconomyTableQuery StageEconomyTableQuery => this;
            public ICategoryConfigTableQuery CategoryConfigTableQuery => this;
            public IUnitTableQuery UnitTableQuery => _roster;
            public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => _roster;
            public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => _roster.ArtifactDefinitionTableQuery;
            public IStructureTableQuery StructureTableQuery => throw new NotSupportedException();
            public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => throw new NotSupportedException();
            public IStageMapTableQuery StageMapTableQuery => throw new NotSupportedException();
            public ITargetingMethodTableQuery TargetingMethodTableQuery => throw new NotSupportedException();
            public IRecoveryOptionTableQuery RecoveryOptionTableQuery => throw new NotSupportedException();
            public IRenderQuery RenderQuery => throw new NotSupportedException();
            public IViewQuery ViewQuery => throw new NotSupportedException();
        }
    }
}
