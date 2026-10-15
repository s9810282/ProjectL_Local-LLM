using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class BattleDefinitionAdapterTests
    {
        private readonly List<FrameworkRuntime> _runtimes = new();

        [TestCase(7)]
        [TestCase(8)]
        public void WeightedEnemyPlacement_ExcludesWallsBeforeCountingCapacity(int count)
        {
            var database = new DefinitionDatabase { EnemyLoadoutKey = 1, HasAuthoredEnemies = false, SpawnCount = count,
                WallCells = Enumerable.Range(0, 8).Select(x => new CellCoord(x, 4)).ToArray() };
            if (count == 8)
                Assert.Throws<InvalidOperationException>(() => BattleDefinitionAdapter.Read(database, 1, 1101, Array.Empty<OwnedCharacterRecord>()));
            else
            {
                var definition = BattleDefinitionAdapter.Read(database, 1, 1101, Array.Empty<OwnedCharacterRecord>());
                Assert.That(definition.Stage.InitialEnemyPlacements.ToArray().All(p => p.Anchor.Y == 5), Is.True);
            }
        }

        [Test]
        public void WallAuthoring_SerializesCellsAndNondefaultCut()
        {
            var asset = UnityEngine.ScriptableObject.CreateInstance<CombatStageData>();
            try
            {
                UnityEngine.JsonUtility.FromJsonOverwrite("{\"initialWallCells\":[{\"x\":3,\"y\":2}],\"cornerCutTicks\":257}", asset);
                var detail = (CombatStageDetail)asset.GetData();
                Assert.That(detail.WallCells, Is.EqualTo(new[] { new CellCoord(3, 2) }));
                Assert.That(detail.WallBlocking.CornerCutTicks, Is.EqualTo(257));
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        [Test]
        public void Walls_AllocateStableIdsAfterExistingSourcesAndPreserveSettings()
        {
            var database = new DefinitionDatabase {
                WallCells = new[] { new CellCoord(4, 3), new CellCoord(3, 3) },
                WallBlocking = new WallBlockingSpec(257),
                StaticTerrain = new[] { new StaticTerrainPlacementData(91, CellCoord.Zero,
                    StaticTerrainKind.HighGround, new[] { Owner.Player }) } };
            var definition = BattleDefinitionAdapter.Read(database, 1, 1101, Array.Empty<OwnedCharacterRecord>());
            var walls = definition.Environment.Tiles.OfType<StaticTerrainFact>().Where(t => t.Kind == StaticTerrainKind.Wall).ToArray();
            Assert.That(walls.Select(w => w.Id), Is.EqualTo(new long[] { 92, 93 }));
            Assert.That(walls.Select(w => w.Cell), Is.EqualTo(new[] { new CellCoord(3, 3), new CellCoord(4, 3) }));
            Assert.That(walls.All(w => w.Active && w.KnownTo.Contains(Owner.Player) && w.KnownTo.Contains(Owner.Enemy)), Is.True);
            Assert.That(definition.EnvironmentBindings.TerrainSourceIds, Is.EquivalentTo(new long[] { 91, 92, 93 }));
            Assert.That(definition.WallBlocking.CornerCutTicks, Is.EqualTo(257));
            database.WallCells = database.WallCells.Reverse().ToArray();
            Assert.That(BattleDefinitionAdapter.Read(database, 1, 1101, Array.Empty<OwnedCharacterRecord>())
                .Environment.SameValues(definition.Environment), Is.True);
        }

        [Test]
        public void DuplicateWallCells_AreRejectedByAdapter()
        {
            var database = new DefinitionDatabase { WallCells = new[] { CellCoord.Right, CellCoord.Right } };
            Assert.Throws<ArgumentException>(() => BattleDefinitionAdapter.Read(database, 1, 1101, Array.Empty<OwnedCharacterRecord>()));
        }

        [Test]
        public void WallDetail_DefensivelyCopiesCells()
        {
            var cells = new[] { new CellCoord(3, 3) };
            var database = new DefinitionDatabase { WallCells = cells };
            var detail = (CombatStageDetail)database.GetStage(1101).StageData;
            cells[0] = new CellCoord(7, 7);
            Assert.That(detail.WallCells.Single(), Is.EqualTo(new CellCoord(3, 3)));
            Assert.Throws<NotSupportedException>(() => ((IList<CellCoord>)detail.WallCells)[0] = cells[0]);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExplicitSource_UsesRequestedTargetWithoutCurrentSelection(bool weighted)
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var services = builder.BuildSystemReader().ServiceStoreReader;
            var buffers = services.GetService(BufferSystemKey.Buffer);
            var source = new DatabaseBattleInitializationSource();
            source.PostBindService(services);
            var database = new DefinitionDatabase { EnemyLoadoutKey = weighted ? 1 : 0, HasAuthoredEnemies = !weighted };
            var definition = source.Read(database, 1, new StageKey(1101));
            Assert.That(database.LastStageKey, Is.EqualTo(1101));
            // 운영 공급원은 유닛이 참조하는 DB 지정 방식으로 유닛마다 공격 프로필을 하나씩 붙인다.
            Assert.That(definition.AttackProfiles.Select(p => p.Definition.Key), Is.EqualTo(definition.Units.Select(u => u.Key)));
            Assert.That(definition.AttackProfiles.All(p => p.Method.Shape == TargetingShape.RadialSquare &&
                p.Method.ChoiceMode == TargetingChoiceMode.Single && p.Method.Splash == TargetingSplash.None), Is.True);
            Assert.That(definition.Economy.InitialGold, Is.EqualTo(27));
            Assert.That(buffers.CurrentProgressReadSink.HasMap, Is.False);
            Assert.That(buffers.CurrentProgressReadSink.HasSelection, Is.False);
            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(buffers.CharacterReadSink.Owned, Is.Empty);
        }

        [Test]
        public void ReadAttackProfiles_NonAttackingUnit_IsRejectedBeforeBattleInstall()
        {
            // DB는 공격력 0·지정 방식 키 0을 허용하지만, Core는 배치·피격에서도 유닛 정의를 공격 프로필로 조회한다.
            // 프로필 없는 유닛이 전투에 들어가 배치 순간에 실패하지 않도록 전투 설치 전에 거절한다.
            var database = new CharacterRosterDatabase(nonAttacker: EntityKey.Unit_Sniper);
            var units = new[] {
                BattleDefinitionAdapter.ReadUnit(database.GetUnit(EntityKey.Unit_Akimbo)),
                BattleDefinitionAdapter.ReadUnit(database.GetUnit(EntityKey.Unit_Sniper)) };

            var error = Assert.Throws<InvalidOperationException>(() => BattleDefinitionAdapter.ReadAttackProfiles(database, units));

            Assert.That(error.Message, Does.Contain(nameof(EntityKey.Unit_Sniper)));
            Assert.That(BattleDefinitionAdapter.ReadAttackProfiles(database, units.Take(1).ToArray()).Count, Is.EqualTo(1));
        }

        [Test]
        public void ExplicitSource_IgnoresDifferentCurrentStage()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var services = builder.BuildSystemReader().ServiceStoreReader;
            var buffers = services.GetService(BufferSystemKey.Buffer);
            buffers.CurrentProgressWriteSink.SelectMap(2);
            buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(3), new StageKey(999), "Other");
            var source = new DatabaseBattleInitializationSource();
            source.PostBindService(services);
            var database = new DefinitionDatabase();
            Assert.That(source.Read(database, 1, new StageKey(1101)).AttackProfiles, Is.Not.Empty);
            Assert.That(database.LastStageKey, Is.EqualTo(1101));
            Assert.That(buffers.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(2));
            Assert.That(buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(999));
        }

        [TestCase(0, 1101)]
        [TestCase(2, 1101)]
        [TestCase(1, 0)]
        [TestCase(1, 999)]
        public void ExplicitSource_InvalidIdentityOrDataFailsBeforePublication(int chapter, int stage)
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var services = builder.BuildSystemReader().ServiceStoreReader;
            var buffers = services.GetService(BufferSystemKey.Buffer);
            var source = new DatabaseBattleInitializationSource();
            source.PostBindService(services);
            var error = Assert.Catch(() => source.Read(new DefinitionDatabase(), chapter,
                stage == 0 ? StageKey.Empty : new StageKey(stage)));
            Assert.That(error, Is.TypeOf(chapter == 0 ? typeof(ArgumentOutOfRangeException) :
                stage == 0 ? typeof(ArgumentException) : stage == 999 ? typeof(KeyNotFoundException) : typeof(InvalidOperationException)));
            Assert.That(buffers.CurrentProgressReadSink.HasSelection, Is.False);
            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(buffers.CharacterReadSink.Characters, Is.Empty);
        }

        [Test]
        public void CompatibilityPreparation_MissingSelectionPreservesState()
        {
            var setup = CreateRegisteredPreparation();
            var core = setup.Runtime.GetService(InGameSystemGroupServiceKey.Core);
            var buffers = setup.Runtime.GetService(BufferSystemKey.Buffer);
            Assert.That(core.Characters.InitializeRoster(1).Succeeded, Is.True);
            buffers.CurrentProgressWriteSink.SelectMap(1);
            setup.Source.Initialization = FixtureDefinition(setup.Database, buffers);
            buffers.CurrencyWriteSink.SetAmount(91);
            var owned = buffers.CharacterReadSink.Owned.ToArray();
            buffers.CharacterReadSink.TryGetMainState(out var main);
            Assert.Throws<InvalidOperationException>(() => core.Combat.PrepareBattle());
            Assert.That(setup.Source.ReadCount, Is.Zero);
            Assert.That(buffers.CurrentProgressReadSink.HasSelection, Is.False);
            Assert.That(buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(buffers.CharacterReadSink.Owned, Is.EqualTo(owned));
            buffers.CharacterReadSink.TryGetMainState(out var after);
            Assert.That(after, Is.SameAs(main));
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(91));
            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.False);
        }

        [Test]
        public void DefaultFactory_EntryReadsTargetAndInstallsBattleFromDatabase()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            InGameCoreSystemFactory.Create().ExportServices(builder);
            var runtime = new FrameworkRuntime(builder.BuildSystemReader());
            _runtimes.Add(runtime);
            var database = new DefinitionDatabase();
            runtime.InitData(database);
            runtime.PostBindService();
            var core = runtime.GetService(InGameSystemGroupServiceKey.Core);
            var buffers = runtime.GetService(BufferSystemKey.Buffer);
            core.Characters.InitializeRoster(1);
            buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 1101, 1, 0, Array.Empty<int>()) });
            Assert.That(core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
            buffers.CurrencyWriteSink.SetAmount(91);
            var owned = buffers.CharacterReadSink.Owned.ToArray();

            // 기본 Factory의 운영 공급원(T1/G7)만으로 대상 정의를 읽고 전투를 설치한다.
            Assert.That(core.Combat.EnterBattle(1, new StageNodeId(2)).Accepted, Is.True);

            Assert.That(database.LastStageKey, Is.EqualTo(1101));
            Assert.That(buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(1101));
            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(buffers.CharacterReadSink.Owned, Is.EqualTo(owned));
            // 스테이지 골드는 스테이지 경제(27)에서 시작하고 Run 재화(91)는 그대로다.
            Assert.That(buffers.CombatStateReadSink.Current.StageGold, Is.EqualTo(27));
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(91));
        }

        [Test]
        public void CompatibilityPreparation_UsesCommittedIdentity()
        {
            var setup = CreateRegisteredPreparation();
            var core = setup.Runtime.GetService(InGameSystemGroupServiceKey.Core);
            var buffers = setup.Runtime.GetService(BufferSystemKey.Buffer);
            core.Characters.InitializeRoster(1);
            buffers.CurrentProgressWriteSink.SelectMap(1);
            buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(1101), "Committed");
            setup.Source.Initialization = FixtureDefinition(setup.Database, buffers);
            core.Combat.PrepareBattle();
            Assert.That(setup.Source.LastChapter, Is.EqualTo(1));
            Assert.That(setup.Source.LastStage.Value, Is.EqualTo(1101));
            Assert.That(setup.Source.ReadCount, Is.EqualTo(1));
            Assert.That(buffers.CurrentProgressReadSink.CurrentStage.StageName, Is.EqualTo("Committed"));
            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.True);
        }

        [Test]
        public void DefinitionReload_RequiresCommittedIdentityAndCachesOnlySuccessfulRead()
        {
            var setup = CreateRegisteredPreparation();
            var core = setup.Runtime.GetService(InGameSystemGroupServiceKey.Core);
            var buffers = setup.Runtime.GetService(BufferSystemKey.Buffer);
            core.Characters.InitializeRoster(1);
            buffers.CurrentProgressWriteSink.SelectMap(1);
            buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(1101), "Committed");
            var definition = FixtureDefinition(setup.Database, buffers);
            setup.Source.Initialization = definition;
            core.Combat.PrepareBattle();
            var battle = buffers.CombatStateReadSink.Current;
            var builder = new SystemBuilderHelper();
            builder.AddService<IBufferQuery>(buffers);
            var reloaded = new CombatProcessingService(setup.Source, new CharacterRosterService(), System.Array.AsReadOnly(new IMovementTileRule[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.ThornMovementTileRule() }));
            reloaded.InitData(setup.Database);
            reloaded.PostBindService(builder.BuildSystemReader().ServiceStoreReader);
            buffers.CurrentProgressWriteSink.Clear();
            int reads = setup.Source.ReadCount;
            Assert.Throws<InvalidOperationException>(() => reloaded.GetTimeline());
            Assert.That(setup.Source.ReadCount, Is.EqualTo(reads));
            buffers.CurrentProgressWriteSink.SelectMap(1);
            buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(1101), "Committed");
            setup.Source.Initialization = null;
            Assert.Throws<InvalidOperationException>(() => reloaded.GetTimeline());
            setup.Source.Initialization = definition;
            Assert.DoesNotThrow(() => reloaded.GetTimeline());
            int successfulReads = setup.Source.ReadCount;
            Assert.DoesNotThrow(() => reloaded.GetTimeline());
            Assert.That(setup.Source.ReadCount, Is.EqualTo(successfulReads));
            Assert.That(setup.Source.LastChapter, Is.EqualTo(1));
            Assert.That(setup.Source.LastStage.Value, Is.EqualTo(1101));
            Assert.That(buffers.CombatStateReadSink.Current, Is.SameAs(battle));
            Assert.That(buffers.CombatStateReadSink.LatestResultSequence, Is.Zero);
        }

        [TearDown]
        public void ShutdownRuntimes()
        {
            foreach (var runtime in _runtimes) runtime.Shutdown();
            _runtimes.Clear();
        }

        [Test]
        public void Definitions_UseStageReferencesAndOwnedKeysWithoutLegacySession()
        {
            var db = new DefinitionDatabase();
            var owned = new[] { new OwnedCharacterRecord(new CharacterInstanceId(1),
                EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main) };
            var result = BattleDefinitionAdapter.Read(db, 1, 1101, owned);
            Assert.That(db.LastStageKey, Is.EqualTo(1101));
            Assert.That(result.Stage.MapWidth, Is.EqualTo(8));
            Assert.That(result.Stage.NightLength, Is.EqualTo(321));
            Assert.That(result.MainAnchor, Is.EqualTo(new CellCoord(1, 0)));
            Assert.That(result.Units.Select(u => u.Key), Is.EquivalentTo(new[] {
                EntityKey.Unit_MainCharacter, EntityKey.Unit_Akimbo, EntityKey.Unit_Demon }));
            Assert.That(result.Stage.InitialEnemyPlacements.Span[0].Anchor, Is.EqualTo(new CellCoord(2, 5)));
            Assert.That(result.Stage.InitialEnemyStructurePlacements.Span[0].StructureKey,
                Is.EqualTo(EntityKey.Structure_Headquarters));
            Assert.That(result.Economy.InitialGold, Is.EqualTo(27));
            Assert.That(result.Economy.ManualDrawGoldCost, Is.EqualTo(6));
            var armor = (DirectionalArmorCategoryRuleSpec)result.Categories.Single(c => c.Key == CategoryKey.Heavy).RuleSpecs.Span[0];
            Assert.That(armor.DamageReductionPercent, Is.EqualTo(37));
            Assert.That(armor.InitialFacing, Is.EqualTo(Rotation4.R90));
            // 새 지정 방식은 DB에 없는 동안 기존 enum이나 캐릭터 키로 만들어 내지 않는다.
            Assert.That(result.AttackProfiles, Is.Empty);
        }

        [TestCase(AttackWallDelivery.RequireClear, AttackWallPropagation.FromImpact)]
        [TestCase(AttackWallDelivery.IgnoreWalls, AttackWallPropagation.FromImpact)]
        [TestCase(AttackWallDelivery.RequireClear, AttackWallPropagation.IgnoreWalls)]
        [TestCase(AttackWallDelivery.IgnoreWalls, AttackWallPropagation.IgnoreWalls)]
        public void UnitWallPolicy_AuthoringAndAdapterPreserveBothStages(AttackWallDelivery delivery, AttackWallPropagation propagation)
        {
            var asset = UnityEngine.ScriptableObject.CreateInstance<UnitDataSO>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(asset);
                var deliveryField = serialized.FindProperty("wallDelivery");
                var propagationField = serialized.FindProperty("wallPropagation");
                Assert.That(deliveryField, Is.Not.Null);
                Assert.That(propagationField, Is.Not.Null);
                Assert.That(deliveryField.intValue, Is.EqualTo((int)AttackWallDelivery.RequireClear));
                Assert.That(propagationField.intValue, Is.EqualTo((int)AttackWallPropagation.FromImpact));
                serialized.FindProperty("key").intValue = (int)EntityKey.Unit_Akimbo;
                serialized.FindProperty("attackPower").intValue = 0;
                deliveryField.intValue = (int)delivery;
                propagationField.intValue = (int)propagation;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var data = (UnitData)asset.GetData();
                data.Validate();
                var spec = BattleDefinitionAdapter.ReadUnit(data);
                Assert.That(data.Occlusion.Delivery, Is.EqualTo(delivery));
                Assert.That(spec.Occlusion.Delivery, Is.EqualTo(delivery));
                Assert.That(spec.Occlusion.Propagation, Is.EqualTo(propagation));
                deliveryField.intValue = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<ArgumentOutOfRangeException>(() => asset.GetData());
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        [Test]
        public void UnitConversion_PreservesIndependentRangeAndPiercingTraitParameters()
        {
            var data = new UnitData(EntityKey.Unit_Ballista, UnitRole.Regular, UnitOwnerEligibility.Both,
                1, 1, 17, 3, 2, 2, 9, 
                CategoryKey.Cannon, 13, 7, 2, 4, 5,
                new UnitTraitData[] { new PiercingFalloffTraitData(30, 11) }, targetingMethodKey: 3);
            var result = BattleDefinitionAdapter.ReadUnit(data);
            Assert.That(result.AttackRange, Is.EqualTo(9));
            Assert.That(((PiercingFalloffTraitSpec)result.Traits.Span[0]).MaxTargetsPerRay, Is.EqualTo(11));
            Assert.That(((PiercingFalloffTraitSpec)result.Traits.Span[0]).ReductionPercentPerTarget, Is.EqualTo(30));
            Assert.That(result.PhysicalDefense, Is.EqualTo(3));
            Assert.That(result.Delay, Is.EqualTo(5));
        }

        [Test]
        public void UnitConversion_MapsDistanceDamageScaleAndKillChainAttackTraits()
        {
            var data = new UnitData(EntityKey.Unit_ShotGun, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                1, 1, 10, 0, 3, 1, 2, 
                CategoryKey.Base, 1, 3, 1, 10, 10,
                new UnitTraitData[] { new DistanceDamageScaleTraitData(new[] { 150, 50 }), new KillChainAttackTraitData() },
                targetingMethodKey: 3);

            var result = BattleDefinitionAdapter.ReadUnit(data);

            Assert.That(result.Traits.Length, Is.EqualTo(2));
            Assert.That(((DistanceDamageScaleTraitSpec)result.Traits.Span[0]).PercentByDistance, Is.EqualTo(new[] { 150, 50 }));
            Assert.That(result.Traits.Span[1], Is.TypeOf<KillChainAttackTraitSpec>());
        }

        [TestCase(0, 1101)]
        [TestCase(2, 1101)]
        [TestCase(1, 0)]
        public void MissingOrMismatchedSelection_DoesNotAssembleAnotherChapter(int map, int stage)
        {
            Assert.Throws<InvalidOperationException>(() => BattleDefinitionAdapter.Read(
                new DefinitionDatabase(), map, stage, Array.Empty<OwnedCharacterRecord>()));
        }

        [Test]
        public void WeightedSpawn_PreservesRecoveredDrawThenShuffleSequence()
        {
            var db = new DefinitionDatabase { EnemyLoadoutKey = 1, HasAuthoredEnemies = false,
                SpawnCount = 4, Entries = new[] { new EnemyLoadoutEntry(EntityKey.Unit_Demon, 1),
                    new EnemyLoadoutEntry(EntityKey.Unit_Akimbo, 3) } };
            var result = BattleDefinitionAdapter.Read(db, 1, 1101, Array.Empty<OwnedCharacterRecord>());
            var enemies = result.Stage.InitialEnemyPlacements.ToArray();
            // 기존 EnemySpawnPlanner의 seed 7 결과다. 추첨과 셔플의 난수 소비 순서도 보존한다.
            Assert.That(enemies.Select(p => p.UnitKey), Is.EqualTo(new[] { EntityKey.Unit_Akimbo,
                EntityKey.Unit_Akimbo, EntityKey.Unit_Akimbo, EntityKey.Unit_Demon }));
            Assert.That(enemies.Select(p => p.Anchor), Is.EqualTo(new[] { new CellCoord(6, 4),
                new CellCoord(7, 5), new CellCoord(2, 4), new CellCoord(1, 4) }));
            Assert.That(enemies.All(p => p.Rotation == Rotation4.R0), Is.True);
            var repeated = BattleDefinitionAdapter.Read(db, 1, 1101, Array.Empty<OwnedCharacterRecord>());
            Assert.That(repeated.Stage.InitialEnemyPlacements.ToArray().Select(p => p.Anchor),
                Is.EqualTo(enemies.Select(p => p.Anchor)));
        }

        [TestCase(0)]
        [TestCase(2)]
        public void AuthoredEnemiesAndLoadout_AreMutuallyExclusiveEvenWithZeroSpawns(int count)
        {
            var db = new DefinitionDatabase { EnemyLoadoutKey = 1, SpawnCount = count };
            var error = Assert.Throws<InvalidOperationException>(() => BattleDefinitionAdapter.Read(
                db, 1, 1101, Array.Empty<OwnedCharacterRecord>()));
            Assert.That(error.Message, Does.Contain("동시에"));
        }

        [TestCase(14, false)]
        [TestCase(15, true)]
        public void WeightedSpawn_ReservesRotatedStructureFootprintAndRejectsInsufficientSpace(int count, bool fails)
        {
            var db = new DefinitionDatabase { EnemyLoadoutKey = 1, HasAuthoredEnemies = false,
                SpawnCount = count, RotatedLargeHeadquarters = true };
            if (fails)
            {
                var error = Assert.Throws<InvalidOperationException>(() => BattleDefinitionAdapter.Read(
                    db, 1, 1101, Array.Empty<OwnedCharacterRecord>()));
                Assert.That(error.Message, Does.Contain("부족"));
                return;
            }
            var enemies = BattleDefinitionAdapter.Read(db, 1, 1101,
                Array.Empty<OwnedCharacterRecord>()).Stage.InitialEnemyPlacements.ToArray();
            Assert.That(enemies, Has.Length.EqualTo(count));
            Assert.That(enemies.Select(p => p.Anchor).Distinct().Count(), Is.EqualTo(count));
            Assert.That(enemies.All(p => p.Anchor.Y >= 4 && p.Anchor.Y < 6 &&
                p.Anchor.X >= 0 && p.Anchor.X < 8 && p.Anchor != new CellCoord(3, 4) &&
                p.Anchor != new CellCoord(3, 5)), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WeightedSpawn_RejectsEmptyOrOutOfRosterCandidates(bool outOfRoster)
        {
            var db = new DefinitionDatabase { EnemyLoadoutKey = 1, HasAuthoredEnemies = false,
                Entries = outOfRoster ? new[] { new EnemyLoadoutEntry(EntityKey.Unit_Sniper, 1) }
                    : Array.Empty<EnemyLoadoutEntry>() };
            Assert.Throws<InvalidOperationException>(() => BattleDefinitionAdapter.Read(
                db, 1, 1101, Array.Empty<OwnedCharacterRecord>()));
        }

        [Test]
        public void ZeroSpawnLoadout_DoesNotRequireCandidates()
        {
            var db = new DefinitionDatabase { EnemyLoadoutKey = 1, HasAuthoredEnemies = false,
                SpawnCount = 0, Entries = Array.Empty<EnemyLoadoutEntry>() };
            Assert.That(BattleDefinitionAdapter.Read(db, 1, 1101, Array.Empty<OwnedCharacterRecord>())
                .Stage.InitialEnemyPlacements.Length, Is.Zero);
        }

        [Test]
        public void Structures_PreserveHeadquartersBeforeAuthoredPlacementOrder()
        {
            var result = BattleDefinitionAdapter.Read(new DefinitionDatabase { AddStructure = true },
                1, 1101, Array.Empty<OwnedCharacterRecord>());
            Assert.That(result.Stage.InitialEnemyStructurePlacements.ToArray().Select(p => p.Anchor),
                Is.EqualTo(new[] { new CellCoord(3, 5), new CellCoord(6, 5) }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DefaultFactory_UsesBufferSelectionAndPreparesBattleFromDatabase(bool weighted)
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var group = InGameCoreSystemFactory.Create();
            group.ExportServices(builder);
            var services = builder.BuildSystemReader().ServiceStoreReader;
            var db = new DefinitionDatabase { EnemyLoadoutKey = weighted ? 1 : 0, HasAuthoredEnemies = !weighted };
            ((ISystemInitData)group).InitData(db);
            ((ISystemPostBindService)group).PostBindService(services);
            var core = (IInGameCoreSystemGroupService)group;
            var buffers = services.GetService(BufferSystemKey.Buffer);
            Assert.That(core.Characters.InitializeRoster(1).Succeeded, Is.True);
            buffers.CurrentProgressWriteSink.SelectMap(1);
            buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(1101), "Combat");
            buffers.MapProgressWriteSink.RegisterMap(1);
            buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(2, 1101, 1, 0, Array.Empty<int>()) });
            buffers.CurrencyWriteSink.SetAmount(91);
            var owned = buffers.CharacterReadSink.Owned.ToArray();

            core.Combat.PrepareBattle();

            Assert.That(db.LastStageKey, Is.EqualTo(1101));
            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(buffers.CharacterReadSink.Characters, Is.Not.Empty);
            Assert.That(buffers.CharacterReadSink.Owned, Is.EqualTo(owned));
            Assert.That(buffers.CombatStateReadSink.Current.StageGold, Is.EqualTo(27));
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(91));
            Assert.That(buffers.CombatStateReadSink.LatestResultSequence, Is.Zero);
        }

        [Test]
        public void RegisteredPreparation_UsesInitializedRosterAndPreservesPersistentBuffers()
        {
            var setup = CreateRegisteredPreparation();
            var core = setup.Runtime.GetService(InGameSystemGroupServiceKey.Core);
            var buffers = setup.Runtime.GetService(BufferSystemKey.Buffer);
            Assert.That(buffers.CharacterReadSink.Owned, Is.Empty);
            Assert.That(core.Characters.InitializeRoster(1).Succeeded, Is.True);
            var owned = buffers.CharacterReadSink.Owned.ToArray();
            var mainId = owned.Single(x => x.Role == CharacterOwnershipRole.Main).Id;
            buffers.CharacterReadSink.TryGetMainState(out var original);
            var wounded = CharacterBufferTestData.MainState(health: 7);
            buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(mainId, 0, 2) }, new MainCharacterPersistentChange(original, wounded)));
            buffers.CurrencyWriteSink.SetAmount(91);
            setup.Source.Initialization = FixtureDefinition(setup.Database, buffers);

            buffers.CurrentProgressWriteSink.SelectMap(1);
            buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(1101), "Compatibility fixture");
            for (int preparation = 0; preparation < 2; preparation++)
            {
                core.Combat.PrepareBattle();
                var main = buffers.CharacterReadSink.Characters.Single(x => x.OwnershipRole == CharacterOwnershipRole.Main);
                Assert.That(main.Health.CurrentHealth, Is.EqualTo(7));
                Assert.That(main.Health.MaxHealth, Is.EqualTo(12));
                Assert.That(main.Stats.EffectiveAttackPower, Is.EqualTo(7));
                Assert.That(buffers.CharacterReadSink.GetSoulCount(mainId), Is.EqualTo(2));
                Assert.That(buffers.CharacterReadSink.Owned, Is.EqualTo(owned));
                Assert.That(buffers.CharacterReadSink.Characters.Where(x => x.OwnedId.HasValue)
                    .Select(x => x.OwnedId.Value), Is.EquivalentTo(owned.Select(x => x.Id)));
                Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(preparation == 0 ? 91 : 85));
                Assert.That(buffers.CombatStateReadSink.Current.DrawCursor, Is.EqualTo(3));
                buffers.CharacterReadSink.TryGetMainState(out var persistent);
                Assert.That(persistent.CurrentHealth, Is.EqualTo(7));
                Assert.That(persistent.PersistentMaxHealth, Is.EqualTo(10));
                Assert.That(core.Characters.InitializeRoster(1).Succeeded, Is.False);

                var battle = buffers.CombatStateReadSink.Current;
                var characters = buffers.CharacterReadSink.Characters.ToArray();
                Assert.Throws<InvalidOperationException>(() => core.Combat.PrepareBattle());
                Assert.That(buffers.CombatStateReadSink.Current, Is.SameAs(battle));
                Assert.That(buffers.CharacterReadSink.Characters, Is.EqualTo(characters));

                // 다음 준비의 입력만 구성한다. 서비스 종료·정산 순서는 C4에서 별도로 검증한다.
                if (preparation == 0)
                {
                    buffers.BattleWriteSink.EndBattle();
                    buffers.CurrencyWriteSink.SetAmount(85);
                }
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void StageEntry_PreparationFailureLeavesSelectionUnconfirmed(bool missingDefinition)
        {
            var setup = CreateRegisteredPreparation();
            var core = setup.Runtime.GetService(InGameSystemGroupServiceKey.Core);
            var buffers = setup.Runtime.GetService(BufferSystemKey.Buffer);
            Assert.That(core.Characters.InitializeRoster(1).Succeeded, Is.True);
            buffers.CurrencyWriteSink.SetAmount(91);
            Assert.That(buffers.MapProgressReadSink.GetMap(1).IsClear, Is.False);
            buffers.StageProgressWriteSink.Clear();
            buffers.StageProgressWriteSink.RegisterStages(1, new[]
            {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 1101, 1, 0, Array.Empty<int>())
            });
            Assert.That(core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
            var valid = FixtureDefinition(setup.Database, buffers);
            setup.Source.Initialization = missingDefinition ? null : new BattleInitialization(valid.Stage,
                new CellCoord(99, 99), valid.MainRotation, valid.Units, valid.Structures,
                valid.Categories, valid.AttackProfiles, valid.Economy);
            var owned = buffers.CharacterReadSink.Owned.ToArray();
            buffers.CharacterReadSink.TryGetMainState(out var main);
            var selection = buffers.CurrentProgressReadSink.CurrentStage;
            var soulField = buffers.CombatStateReadSink.SoulField;

            // 등록된 Core 진입 계약을 검증한다. 실제 Job과 Flow 이전은 별도 완료 조건이다.
            void TryEnter()
            {
                Assert.That(core.Combat.EnterBattle(1, new StageNodeId(2)).Accepted, Is.True);
            }

            Assert.Throws<InvalidOperationException>(TryEnter);
            Assert.That(buffers.CurrentProgressReadSink.HasSelection, Is.False);
            Assert.That(buffers.CurrentProgressReadSink.CurrentStage, Is.EqualTo(selection));
            Assert.That(buffers.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(1));
            Assert.That(buffers.StageProgressReadSink.IsCleared(1, 2), Is.False);
            Assert.That(buffers.MapProgressReadSink.GetMap(1).IsClear, Is.False);
            Assert.That(buffers.CharacterReadSink.Owned, Is.EqualTo(owned));
            buffers.CharacterReadSink.TryGetMainState(out var after);
            Assert.That(after, Is.SameAs(main));
            Assert.That(buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(91));
            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(buffers.CombatStateReadSink.SoulField, Is.SameAs(soulField));
            Assert.That(buffers.CombatStateReadSink.Completion, Is.Null);
            Assert.That(buffers.CombatStateReadSink.LatestResultSequence, Is.Zero);
            Assert.That(buffers.CombatStateReadSink.HandledCommandNumber, Is.Zero);

            setup.Source.Initialization = valid;
            TryEnter();
            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(1101));
            Assert.That(buffers.CurrentProgressReadSink.CurrentStage.NodeId, Is.EqualTo(2));
            Assert.That(buffers.CharacterReadSink.Owned, Is.EqualTo(owned));
            Assert.That(buffers.CharacterReadSink.Characters.Select(x => x.EntityId.Value),
                Is.EquivalentTo(new[] { 1, 2, 4, 5, 6 }));
            Assert.That(buffers.CombatStateReadSink.Current.NextEntityId, Is.EqualTo(7));
        }

        private (FrameworkRuntime Runtime, DefinitionDatabase Database, FixtureInitializationSource Source)
            CreateRegisteredPreparation()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var source = new FixtureInitializationSource();
            InGameCoreSystemFactory.Create(source).ExportServices(builder);
            var database = new DefinitionDatabase();
            var runtime = new FrameworkRuntime(builder.BuildSystemReader());
            _runtimes.Add(runtime);
            runtime.InitData(database);
            runtime.PostBindService();
            var buffers = runtime.GetService(BufferSystemKey.Buffer);
            buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 1101, 1, 0, Array.Empty<int>()) });
            return (runtime, database, source);
        }

        private static BattleInitialization FixtureDefinition(DefinitionDatabase database, IBufferQuery buffers)
        {
            var definition = BattleDefinitionAdapter.Read(database, 1, 1101, buffers.CharacterReadSink.Owned);
            // 실제 어댑터 변환 뒤 테스트용 프로필만 명시한다. 운영 DB의 Method_Key 연결을 대체하지 않는다.
            var profiles = definition.Units.Select(unit => new CombatAttackProfile(unit,
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray();
            return definition.WithAttackProfiles(profiles);
        }

        private sealed class FixtureInitializationSource : IBattleInitializationSource
        {
            internal BattleInitialization Initialization;
            internal int ReadCount;
            internal int LastChapter;
            internal StageKey LastStage;
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                ReadCount++;
                LastChapter = chapterKey;
                LastStage = stageKey;
                return Initialization;
            }
        }

        // 정식 DB 데이터 형식을 사용하지만 저작 에셋 완료를 의미하지 않는 조회 대역이다.
        private sealed class DefinitionDatabase : IDataBaseQueryContext, IInGameQuery,
            IStageTableQuery, IFieldTableQuery, IUnitRosterTableQuery, IStructureTableQuery,
            IStageEconomyTableQuery, ICategoryConfigTableQuery, IEnemyLoadoutTableQuery, IStageMapTableQuery
        {
            private readonly CharacterRosterDatabase _roster = new();
            private readonly StageMapData _map = new(1, 2, 1, 0, StageConnectionAlgorithm.Nearest, 1, 1, 42, false);
            private readonly FieldData _field = new("field", 8, 6, 8, 2, 2);
            private readonly UnitRosterData _units = new(1, new[] { EntityKey.Unit_Akimbo, EntityKey.Unit_Demon });
            private StructureData Headquarters => new(EntityKey.Structure_Headquarters,
                RotatedLargeHeadquarters ? 2 : 1, 1, 20, false, 0);
            private readonly StageEconomyData _economy = new(1, 27, 6);
            internal int LastStageKey;
            internal int EnemyLoadoutKey;
            internal bool HasAuthoredEnemies = true;
            internal bool RotatedLargeHeadquarters;
            internal bool AddStructure;
            internal CellCoord[] WallCells = Array.Empty<CellCoord>();
            internal WallBlockingSpec WallBlocking = WallBlockingSpec.Default;
            internal StaticTerrainPlacementData[] StaticTerrain = Array.Empty<StaticTerrainPlacementData>();
            internal int SpawnCount = 2;
            internal EnemyLoadoutEntry[] Entries = { new(EntityKey.Unit_Demon, 1) };
            public StageRootData GetStage(int key)
            {
                LastStageKey = key;
                if (key != 1101) throw new KeyNotFoundException();
                return new StageRootData(key, 1, StageMapRole.Regular, new CombatStageDetail(42,
                    "field", 1, EnemyLoadoutKey, 1, 321,
                    HasAuthoredEnemies ? new[] { new EntityPlacement(EntityKey.Unit_Demon, new CellCoord(2, 5), Rotation4.R180) }
                        : Array.Empty<EntityPlacement>(),
                    Array.Empty<EntityKey>(), AddStructure ? new[] { new EntityPlacement(EntityKey.Structure_Headquarters,
                        new CellCoord(6, 5), Rotation4.R0) } : Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters,
                    new CellCoord(1, 0), Rotation4.R0, RotatedLargeHeadquarters ? new CellCoord(3, 4) : new CellCoord(3, 5),
                    RotatedLargeHeadquarters ? Rotation4.R90 : Rotation4.R180, null,
                    staticTerrainPlacements: StaticTerrain, wallCells: WallCells, wallBlocking: WallBlocking));
            }
            public IReadOnlyList<StageRootData> GetStages(int key) => new[] { GetStage(1101) };
            public FieldData GetField(string key) => key == "field" ? _field : throw new KeyNotFoundException();
            public bool TryGetField(string key, out FieldData value) { value = key == "field" ? _field : null; return value != null; }
            public IReadOnlyCollection<FieldData> GetAllFields() => new[] { _field };
            public UnitRosterData GetUnitRoster(int key) => key == 1 ? _units : throw new KeyNotFoundException();
            public bool TryGetUnitRoster(int key, out UnitRosterData value) { value = key == 1 ? _units : null; return value != null; }
            public IReadOnlyCollection<UnitRosterData> GetAllUnitRosters() => new[] { _units };
            public StructureData GetStructure(EntityKey key) => key == Headquarters.Key ? Headquarters : throw new KeyNotFoundException();
            public bool TryGetStructure(EntityKey key, out StructureData value) { value = key == Headquarters.Key ? Headquarters : null; return value != null; }
            public IReadOnlyCollection<StructureData> GetAllStructures() => new[] { Headquarters };
            public StageEconomyData GetStageEconomy(int key) => key == 1 ? _economy : throw new KeyNotFoundException();
            public bool TryGetStageEconomy(int key, out StageEconomyData value) { value = key == 1 ? _economy : null; return value != null; }
            public IReadOnlyCollection<StageEconomyData> GetAllStageEconomies() => new[] { _economy };
            public CategoryConfigData Config => new(37, Rotation4.R90);
            public EnemyLoadoutData GetEnemyLoadout(int key) => new(key, Entries, SpawnCount, 7);
            public bool TryGetEnemyLoadout(int key, out EnemyLoadoutData value) { value = GetEnemyLoadout(key); return true; }
            public IReadOnlyCollection<EnemyLoadoutData> GetAllEnemyLoadouts() => new[] { GetEnemyLoadout(1) };
            public StageMapData GetStageMap(int key) => key == 1 ? _map : throw new KeyNotFoundException();
            public IReadOnlyCollection<StageMapData> GetAllStageMaps() => new[] { _map };
            public IInGameQuery InGameQuery => this;
            public IStageTableQuery StageTableQuery => this;
            public IFieldTableQuery FieldTableQuery => this;
            public IUnitRosterTableQuery UnitRosterTableQuery => this;
            public IStructureTableQuery StructureTableQuery => this;
            public IStageEconomyTableQuery StageEconomyTableQuery => this;
            public ICategoryConfigTableQuery CategoryConfigTableQuery => this;
            public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => this;
            public IUnitTableQuery UnitTableQuery => _roster;
            public ITargetingMethodTableQuery TargetingMethodTableQuery => _roster.TargetingMethodTableQuery;
            public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => _roster;
            public IStageMapTableQuery StageMapTableQuery => this;
            public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => _roster.ArtifactDefinitionTableQuery;
            public IRecoveryOptionTableQuery RecoveryOptionTableQuery => throw new NotSupportedException();
            public IRenderQuery RenderQuery => throw new NotSupportedException();
            public IViewQuery ViewQuery => throw new NotSupportedException();
        }
    }
}
