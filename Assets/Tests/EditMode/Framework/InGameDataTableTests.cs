using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.Factory.DBFactory.Group;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using static ProjectL.Tests.Framework.DataBaseTestAssets;

namespace ProjectL.Tests.Framework
{
    /// <summary>InGameGroup 신규 테이블(Field, Unit, Structure, Loadout, Roster)과 전투 스테이지 상세의 조회·검증을 확인합니다.</summary>
    public sealed class InGameDataTableTests
    {
        private readonly DataBaseTestAssets _assets = new();

        private UnitDataSO _akimbo;
        private UnitDataSO _demon;
        private UnitDataSO _mainCharacter;
        private TargetingMethodDataSO _targetingMethod;
        private StructureDataSO _headquarters;
        private FieldDataSO _field;
        private UnitRosterDataSO _roster;
        private EnemyLoadoutDataSO _enemyLoadout;
        private PlayerLoadoutDataSO _playerLoadout;
        private StageEconomyDataSO _economy;
        private CategoryConfigDataSO _categoryConfig;
        private CombatStageData _combat;
        private StageRootDataSO _regularStage;
        private StageRootDataSO _bossStage;
        private StageMapDataSO _map;

        [SetUp]
        public void SetUp()
        {
            _targetingMethod = _assets.Create<TargetingMethodDataSO>();
            _akimbo = Unit(EntityKey.Unit_Akimbo);
            Set(_akimbo, "moveRange", 5);
            Set(_akimbo, "traits", new List<UnitTraitAuthoring> { Focus(1, 3) });

            _demon = Unit(EntityKey.Unit_Demon);
            Set(_demon, "ownerEligibility", UnitOwnerEligibility.EnemyOnly);

            _mainCharacter = Unit(EntityKey.Unit_MainCharacter);
            Set(_mainCharacter, "role", UnitRole.MainCharacter);

            _economy = _assets.Create<StageEconomyDataSO>();
            Set(_economy, "initialGold", 200);
            Set(_economy, "manualDrawGoldCost", 40);

            _categoryConfig = _assets.Create<CategoryConfigDataSO>();

            _headquarters = _assets.Create<StructureDataSO>();
            Set(_headquarters, "maxHealth", 3);

            _field = Field("field_7x6");
            Set(_field, "columnCount", 7);
            Set(_field, "rowCount", 6);

            _roster = Roster(1, _akimbo, _demon);
            _enemyLoadout = EnemyLoadout(1, (_demon, 1));
            Set(_enemyLoadout, "spawnCount", 2);
            _playerLoadout = PlayerLoadout(1, (_akimbo, 2));
            Set(_playerLoadout, "startingHandSize", 5);
            Set(_playerLoadout, "startingRunCurrency", 4);

            _combat = _assets.Create<CombatStageData>();
            Set(_combat, "timelineSeed", 1);
            Set(_combat, "fieldDefinition", _field);
            Set(_combat, "unitRoster", _roster);
            Set(_combat, "enemyLoadout", _enemyLoadout);
            Set(_combat, "economy", _economy);
            Set(_combat, "headquarters", _headquarters);
            Set(_combat, "mainCharacterAnchor", new CellInput(3, 0));
            Set(_combat, "enemyHeadquartersAnchor", new CellInput(3, 5));

            _regularStage = Stage(1101, StageMapRole.Regular);
            _bossStage = Stage(2101, StageMapRole.Boss);

            _map = _assets.Create<StageMapDataSO>();
            Set(_map, "depthCount", 7);
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void Factory_BuildsAllTables_AndConvertsReferencesToKeys()
        {
            IInGameQuery query = CreateQuery();

            Assert.That(query.StageMapTableQuery.GetStageMap(1).DepthCount, Is.EqualTo(7));
            Assert.That(query.FieldTableQuery.GetField("field_7x6").ColumnCount, Is.EqualTo(7));

            UnitData akimbo = query.UnitTableQuery.GetUnit(EntityKey.Unit_Akimbo);
            Assert.That(akimbo.TargetingMethodKey, Is.EqualTo(1));
            var method = query.TargetingMethodTableQuery.GetTargetingMethod(akimbo.TargetingMethodKey);
            Assert.That((method.Shape, method.ChoiceMode, method.Splash, method.SplashPercent),
                Is.EqualTo((TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)));
            Assert.That(query.TargetingMethodTableQuery.TryGetTargetingMethod(1, out var same), Is.True);
            Assert.That(same, Is.SameAs(method));
            Assert.That(query.TargetingMethodTableQuery.GetAllTargetingMethods(), Has.Count.EqualTo(1));
            Assert.That(akimbo.MoveRange, Is.EqualTo(5));
            Assert.That((akimbo.Role, akimbo.Category), Is.EqualTo((UnitRole.Regular, CategoryKey.Base)));
            var focus = (FocusTraitData)akimbo.Traits[0];
            Assert.That((focus.DamagePerStack, focus.MaxStacks), Is.EqualTo((1, 3)));
            Assert.That(query.UnitTableQuery.GetUnit(EntityKey.Unit_Demon).OwnerEligibility,
                Is.EqualTo(UnitOwnerEligibility.EnemyOnly));

            Assert.That(query.StructureTableQuery.GetStructure(EntityKey.Structure_Headquarters).MaxHealth, Is.EqualTo(3));
            Assert.That(query.UnitRosterTableQuery.GetUnitRoster(1).UnitKeys,
                Is.EqualTo(new[] { EntityKey.Unit_Akimbo, EntityKey.Unit_Demon }));

            EnemyLoadoutData enemy = query.EnemyLoadoutTableQuery.GetEnemyLoadout(1);
            Assert.That((enemy.Entries[0].UnitKey, enemy.Entries[0].Weight, enemy.SpawnCount),
                Is.EqualTo((EntityKey.Unit_Demon, 1, 2)));
            PlayerLoadoutData player = query.PlayerLoadoutTableQuery.GetPlayerLoadout(1);
            Assert.That((player.Entries[0].UnitKey, player.Entries[0].Count, player.StartingHandSize, player.StartingRunCurrency),
                Is.EqualTo((EntityKey.Unit_Akimbo, 2, 5, 4)));
            Assert.That(player.MainCharacterKey, Is.EqualTo(EntityKey.Unit_MainCharacter));
            Assert.That(query.StageEconomyTableQuery.GetStageEconomy(1).InitialGold, Is.EqualTo(200));
            Assert.That(query.CategoryConfigTableQuery.Config.HeavyDamageReductionPercent, Is.EqualTo(50));

            var detail = (CombatStageDetail)query.StageTableQuery.GetStage(1101).StageData;
            Assert.That(detail.FieldId, Is.EqualTo("field_7x6"));
            Assert.That((detail.UnitRosterKey, detail.EnemyLoadoutKey, detail.EconomyKey), Is.EqualTo((1, 1, 1)));
            Assert.That(detail.NightLength, Is.EqualTo(500));
            Assert.That(detail.HeadquartersKey, Is.EqualTo(EntityKey.Structure_Headquarters));
            Assert.That(detail.MainCharacterAnchor, Is.EqualTo(new CellCoord(3, 0)));
            Assert.That(detail.EnemyHeadquartersAnchor, Is.EqualTo(new CellCoord(3, 5)));
            Assert.That(detail.Railway, Is.Null);
            Assert.That(query.StageTableQuery.GetStage(2101).MapRole, Is.EqualTo(StageMapRole.Boss));
        }

        [Test]
        public void Tables_TryGetReturnsFalse_AndGetThrowsForMissingKey()
        {
            IInGameQuery query = CreateQuery();

            Assert.That(query.FieldTableQuery.TryGetField("field_9x9", out _), Is.False);
            Assert.That(query.UnitTableQuery.TryGetUnit(EntityKey.Unit_Sniper, out _), Is.False);
            Assert.That(query.TargetingMethodTableQuery.TryGetTargetingMethod(2, out _), Is.False);
            Assert.That(query.StructureTableQuery.TryGetStructure(EntityKey.Unit_Akimbo, out _), Is.False);
            Assert.That(query.EnemyLoadoutTableQuery.TryGetEnemyLoadout(2, out _), Is.False);
            Assert.That(query.PlayerLoadoutTableQuery.TryGetPlayerLoadout(2, out _), Is.False);
            Assert.That(query.UnitRosterTableQuery.TryGetUnitRoster(2, out _), Is.False);

            Assert.Throws<KeyNotFoundException>(() => query.FieldTableQuery.GetField("field_9x9"));
            Assert.Throws<KeyNotFoundException>(() => query.UnitTableQuery.GetUnit(EntityKey.Unit_Sniper));
            Assert.Throws<KeyNotFoundException>(() => query.TargetingMethodTableQuery.GetTargetingMethod(2));
            Assert.Throws<KeyNotFoundException>(() => query.StructureTableQuery.GetStructure(EntityKey.Unit_Akimbo));
            Assert.Throws<KeyNotFoundException>(() => query.EnemyLoadoutTableQuery.GetEnemyLoadout(2));
            Assert.Throws<KeyNotFoundException>(() => query.PlayerLoadoutTableQuery.GetPlayerLoadout(2));
            Assert.Throws<KeyNotFoundException>(() => query.UnitRosterTableQuery.GetUnitRoster(2));
        }

        [Test]
        public void Tables_RejectDuplicateKeys()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new TargetingMethodTable(_assets.Table<TargetingMethodDataTableSO>(
                    _targetingMethod, _assets.Create<TargetingMethodDataSO>())));
            Assert.Throws<InvalidOperationException>(() =>
                new FieldTable(_assets.Table<FieldDataTableSO>(_field, Field("field_7x6"))));
            Assert.Throws<InvalidOperationException>(() =>
                new UnitTable(_assets.Table<UnitDataTableSO>(_akimbo, Unit(EntityKey.Unit_Akimbo))));
            Assert.Throws<InvalidOperationException>(() =>
                new StructureTable(_assets.Table<StructureDataTableSO>(_headquarters, _assets.Create<StructureDataSO>())));
            Assert.Throws<InvalidOperationException>(() =>
                new EnemyLoadoutTable(_assets.Table<EnemyLoadoutDataTableSO>(_enemyLoadout, EnemyLoadout(1, (_demon, 1)))));
            Assert.Throws<InvalidOperationException>(() =>
                new PlayerLoadoutTable(_assets.Table<PlayerLoadoutDataTableSO>(_playerLoadout, PlayerLoadout(1, (_akimbo, 1)))));
            Assert.Throws<InvalidOperationException>(() =>
                new UnitRosterTable(_assets.Table<UnitRosterDataTableSO>(_roster, Roster(1, _akimbo))));
        }

        [Test]
        public void Tables_RejectMissingRequiredValues()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new FieldTable(_assets.Table<FieldDataTableSO>(Field(""))), "FieldId 없음");
            Assert.Throws<InvalidOperationException>(() =>
                new UnitTable(_assets.Table<UnitDataTableSO>(Unit(EntityKey.None))), "유닛 키 없음");

            var structure = _assets.Create<StructureDataSO>();
            Set(structure, "key", EntityKey.None);
            Assert.Throws<InvalidOperationException>(() =>
                new StructureTable(_assets.Table<StructureDataTableSO>(structure)), "구조물 키 없음");

            Assert.Throws<InvalidOperationException>(() =>
                new EnemyLoadoutTable(_assets.Table<EnemyLoadoutDataTableSO>(EnemyLoadout(1, (null, 1)))), "적 구성 유닛 없음");
            Assert.Throws<InvalidOperationException>(() =>
                new PlayerLoadoutTable(_assets.Table<PlayerLoadoutDataTableSO>(PlayerLoadout(1, (null, 1)))), "플레이어 구성 유닛 없음");
            Assert.Throws<InvalidOperationException>(() =>
                new UnitRosterTable(_assets.Table<UnitRosterDataTableSO>(Roster(1, _akimbo, null))), "로스터 유닛 없음");
        }

        [TestCase("fieldDefinition")]
        [TestCase("unitRoster")]
        public void CombatStage_WithoutFieldOrRoster_IsRejected(string requiredField)
        {
            Set(_combat, requiredField, null);

            Assert.Throws<InvalidOperationException>(() => CreateQuery());
        }

        [Test]
        public void CombatStage_Railway_IsConvertedOnlyWhenEnabled()
        {
            var disabled = (CombatStageDetail)((StageRootData)_regularStage.GetData()).StageData;
            Assert.That(disabled.Railway, Is.Null);

            Set(_combat, "railwayEnabled", true);
            var enabled = (CombatStageDetail)((StageRootData)_regularStage.GetData()).StageData;
            Assert.That(enabled.Railway, Is.Not.Null);
            Assert.That(enabled.Railway.FirstActivationAfterUnitTurns, Is.EqualTo(3));
        }

        [Test]
        public void GetData_CreatesNewDataEachCall()
        {
            // P2: SO 값을 바꾸면 다음 GetData부터 반영되고, 이미 받은 Data는 바뀌지 않는다.
            var before = (UnitData)_akimbo.GetData();
            Set(_akimbo, "moveRange", 2);
            var after = (UnitData)_akimbo.GetData();

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That((before.MoveRange, after.MoveRange), Is.EqualTo((5, 2)));
        }

        [Test]
        public void Unit_DuplicateTrait_IsRejected()
        {
            Set(_akimbo, "traits", new List<UnitTraitAuthoring> { Focus(1, 3), Focus(2, 2) });

            Assert.Throws<InvalidOperationException>(() => new UnitTable(_assets.Table<UnitDataTableSO>(_akimbo)));
        }

        [Test]
        public void Unit_NullTrait_IsRejected()
        {
            Set(_akimbo, "traits", new List<UnitTraitAuthoring> { null });

            Assert.Throws<InvalidOperationException>(() => new UnitTable(_assets.Table<UnitDataTableSO>(_akimbo)));
        }

        [Test]
        public void Traits_DefaultValuesAreValid()
        {
            Assert.DoesNotThrow(() => new FocusTraitAuthoring().ToData().Validate());
            Assert.DoesNotThrow(() => new MoveAttackDiscountTraitAuthoring().ToData().Validate());
            Assert.DoesNotThrow(() => new PiercingFalloffTraitAuthoring().ToData().Validate());
            Assert.DoesNotThrow(() => new ThornFieldTraitAuthoring().ToData().Validate());
            Assert.DoesNotThrow(() => new KnockbackTraitAuthoring().ToData().Validate());
            Assert.DoesNotThrow(() => new DistanceDamageScaleTraitAuthoring().ToData().Validate());
            Assert.DoesNotThrow(() => new KillChainAttackTraitAuthoring().ToData().Validate());
            Assert.That(((DistanceDamageScaleTraitData)new DistanceDamageScaleTraitAuthoring().ToData()).PercentByDistance,
                Is.EqualTo(new[] { 150, 50 }), "기본값은 샷건 산탄(앞 1칸 150%, 뒤 칸 50%)이다.");
        }

        [TestCase(null)]
        [TestCase(new int[0])]
        [TestCase(new[] { 150, -1 })]
        public void DistanceDamageScale_InvalidPercents_AreRejected(int[] percents)
        {
            var authoring = new DistanceDamageScaleTraitAuthoring();
            Set(authoring, "percentByDistance", percents);
            Set(_akimbo, "traits", new List<UnitTraitAuthoring> { authoring });

            Assert.Throws<InvalidOperationException>(() => new UnitTable(_assets.Table<UnitDataTableSO>(_akimbo)));
        }

        [Test]
        public void DistanceDamageScale_DataKeepsCopyOfInput()
        {
            var source = new[] { 150, 50 };
            var data = new DistanceDamageScaleTraitData(source);
            source[0] = 999;

            Assert.That(data.PercentByDistance, Is.EqualTo(new[] { 150, 50 }));
        }

        [TestCase(typeof(FocusTraitAuthoring), "damagePerStack", 0)]
        [TestCase(typeof(FocusTraitAuthoring), "maxStacks", 0)]
        [TestCase(typeof(MoveAttackDiscountTraitAuthoring), "discountAmount", 0)]
        [TestCase(typeof(MoveAttackDiscountTraitAuthoring), "minimumAttackCost", 0)]
        [TestCase(typeof(PiercingFalloffTraitAuthoring), "reductionPercentPerTarget", 0)]
        [TestCase(typeof(PiercingFalloffTraitAuthoring), "reductionPercentPerTarget", 101)]
        [TestCase(typeof(ThornFieldTraitAuthoring), "thornDamage", 0)]
        [TestCase(typeof(ThornFieldTraitAuthoring), "lifetimeUnitTurns", 0)]
        [TestCase(typeof(KnockbackTraitAuthoring), "distanceCells", 0)]
        public void Trait_OutOfRange_IsRejected(Type authoringType, string field, int value)
        {
            var authoring = (UnitTraitAuthoring)Activator.CreateInstance(authoringType, true);
            Set(authoring, field, value);
            Set(_akimbo, "traits", new List<UnitTraitAuthoring> { authoring });

            Assert.Throws<InvalidOperationException>(() => new UnitTable(_assets.Table<UnitDataTableSO>(_akimbo)));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Factory_RejectsUnregisteredTargetingReference(int attackPower)
        {
            var unregistered = _assets.Create<TargetingMethodDataSO>();
            Set(unregistered, "methodKey", 99);
            Set(_akimbo, "targetingMethod", unregistered);
            Set(_akimbo, "attackPower", attackPower);

            var exception = Assert.Throws<InvalidOperationException>(() => CreateQuery());
            Assert.That(exception.Message, Does.Contain("Unit_Akimbo").And.Contain("99"));
        }

        [Test]
        public void Factory_RejectsAttackingUnitWithoutTargetingReference()
        {
            Set(_akimbo, "targetingMethod", null);

            var exception = Assert.Throws<InvalidOperationException>(() => CreateQuery());
            Assert.That(exception.Message, Does.Contain("Unit_Akimbo").And.Contain("targeting method"));
        }

        [Test]
        public void Factory_AllowsNonAttackingUnitWithoutTargetingReference()
        {
            Set(_akimbo, "attackPower", 0);
            Set(_akimbo, "targetingMethod", null);

            Assert.That(CreateQuery().UnitTableQuery.GetUnit(EntityKey.Unit_Akimbo).TargetingMethodKey, Is.Zero);
        }

        [Test]
        public void TargetingTable_RejectsInvalidSourceAndItems()
        {
            Assert.Throws<ArgumentNullException>(() => new TargetingMethodTable(null));
            Assert.Throws<InvalidOperationException>(() =>
                new TargetingMethodTable(_assets.Table<TargetingMethodDataTableSO>((DatabaseItem)null)));
            Assert.Throws<InvalidOperationException>(() =>
                new TargetingMethodTable(_assets.Table<TargetingMethodDataTableSO>(_akimbo)));
            Set(_targetingMethod, "shape", TargetingShape.None);
            Assert.Throws<InvalidOperationException>(() =>
                new TargetingMethodTable(_assets.Table<TargetingMethodDataTableSO>(_targetingMethod)));
        }

        [Test]
        public void TargetingGetData_ReturnsIndependentValues_AndUnitConvertsReferenceToKey()
        {
            var before = (TargetingMethodData)_targetingMethod.GetData();
            var unitBefore = (UnitData)_akimbo.GetData();
            Set(_targetingMethod, "methodKey", 2);
            Set(_targetingMethod, "shape", TargetingShape.Fan);
            Set(_targetingMethod, "choiceMode", TargetingChoiceMode.Direction);
            var after = (TargetingMethodData)_targetingMethod.GetData();

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That((before.MethodKey, before.Shape, before.ChoiceMode),
                Is.EqualTo((1, TargetingShape.RadialSquare, TargetingChoiceMode.Single)));
            Assert.That((after.MethodKey, after.Shape, after.ChoiceMode),
                Is.EqualTo((2, TargetingShape.Fan, TargetingChoiceMode.Direction)));
            Assert.That((unitBefore.TargetingMethodKey, ((UnitData)_akimbo.GetData()).TargetingMethodKey),
                Is.EqualTo((1, 2)));
        }

        private IInGameQuery CreateQuery()
        {
            InGameGroup group = _assets.Group<InGameGroup>(
                _assets.Table<StageMapDataTableSO>(_map),
                _assets.Table<StageRootDataTableSO>(_regularStage, _bossStage),
                _assets.Table<FieldDataTableSO>(_field),
                _assets.Table<UnitDataTableSO>(_akimbo, _demon, _mainCharacter),
                _assets.Table<TargetingMethodDataTableSO>(_targetingMethod),
                _assets.Table<StructureDataTableSO>(_headquarters),
                _assets.Table<EnemyLoadoutDataTableSO>(_enemyLoadout),
                _assets.Table<PlayerLoadoutDataTableSO>(_playerLoadout),
                _assets.Table<UnitRosterDataTableSO>(_roster),
                _assets.Table<ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.ArtifactDefinitionDataTableSO>(),
                _assets.Table<CategoryConfigDataTableSO>(_categoryConfig),
                _assets.Table<ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard.ArtifactBoardConfigDataTableSO>(
                    _assets.Create<ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard.Item.ArtifactBoardConfigDataSO>()),
                _assets.Table<StageEconomyDataTableSO>(_economy),
                _assets.Table<RecoveryOptionDataTableSO>());
            return (IInGameQuery)InGameDataBaseFactory.Create(group);
        }

        private UnitDataSO Unit(EntityKey key)
        {
            var unit = _assets.Create<UnitDataSO>();
            Set(unit, "key", key);
            Set(unit, "targetingMethod", _targetingMethod);
            return unit;
        }

        private FieldDataSO Field(string fieldId)
        {
            var field = _assets.Create<FieldDataSO>();
            Set(field, "fieldId", fieldId);
            return field;
        }

        private UnitRosterDataSO Roster(int rosterKey, params UnitDataSO[] units)
        {
            var roster = _assets.Create<UnitRosterDataSO>();
            Set(roster, "rosterKey", rosterKey);
            Set(roster, "unitDefinitions", units);
            return roster;
        }

        private EnemyLoadoutDataSO EnemyLoadout(int loadoutKey, params (UnitDataSO Unit, int Weight)[] entries)
        {
            var loadout = _assets.Create<EnemyLoadoutDataSO>();
            Set(loadout, "loadoutKey", loadoutKey);
            Set(loadout, "entries", Entries(typeof(EnemyLoadoutDataSO), "Entry",
                Array.ConvertAll(entries, e => new[] { ("unitDefinition", (object)e.Unit), ("weight", (object)e.Weight) })));
            return loadout;
        }

        private PlayerLoadoutDataSO PlayerLoadout(int loadoutKey, params (UnitDataSO Unit, int Count)[] entries)
        {
            var loadout = _assets.Create<PlayerLoadoutDataSO>();
            Set(loadout, "loadoutKey", loadoutKey);
            Set(loadout, "mainCharacter", _mainCharacter);
            Set(loadout, "entries", Entries(typeof(PlayerLoadoutDataSO), "Entry",
                Array.ConvertAll(entries, e => new[] { ("unitDefinition", (object)e.Unit), ("count", (object)e.Count) })));
            return loadout;
        }

        private StageRootDataSO Stage(int stageKey, StageMapRole mapRole)
        {
            var stage = _assets.Create<StageRootDataSO>();
            Set(stage, "stageKey", stageKey);
            Set(stage, "mapKey", 1);
            Set(stage, "mapRole", mapRole);
            Set(stage, "stageData", _combat);
            return stage;
        }

        private static FocusTraitAuthoring Focus(int damagePerStack, int maxStacks)
        {
            var focus = new FocusTraitAuthoring();
            Set(focus, "damagePerStack", damagePerStack);
            Set(focus, "maxStacks", maxStacks);
            return focus;
        }
    }
}
