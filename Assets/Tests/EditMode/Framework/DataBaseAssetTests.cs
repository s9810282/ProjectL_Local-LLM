using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Data.View;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.Factory.DBFactory.Group;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// 프로젝트에 실제로 있는 DB 에셋(<c>Assets/ProjectL/DataBaseSO</c>)을 불러와 조립되는지,
    /// 그룹 사이의 키가 서로 맞는지 확인합니다. 에셋 값을 바꾼 뒤 누락을 잡는 용도입니다.
    /// </summary>
    public sealed class DataBaseAssetTests
    {
        private const string Root = "Assets/ProjectL/DataBaseSO/";
        private const string CatalogPath = Root + "DatabaseCatalogSO.asset";
        private const string InGameGroupPath = Root + "InGameGroup/InGameGroup.asset";
        private const string ViewGroupPath = Root + "ViewGroup/ViewGroup.asset";
        private const string StageMapViewPath = Root + "ViewGroup/StageMapView/Item/StageMapView_Default.asset";

        private readonly DataBaseTestAssets _assets = new();

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void PiercingTrait_SerializedAuthoringPreservesExplicitCap(int cap)
        {
            var authoring = new PiercingFalloffTraitAuthoring();
            DataBaseTestAssets.Set(authoring, "reductionPercentPerTarget", 30);
            DataBaseTestAssets.Set(authoring, "maxTargetsPerRay", cap);
            var unit = _assets.Create<UnitDataSO>();
            DataBaseTestAssets.Set(unit, "key", EntityKey.Unit_Ballista);
            DataBaseTestAssets.Set(unit, "category", CategoryKey.Cannon);
            DataBaseTestAssets.Set(unit, "attackPower", 0);
            DataBaseTestAssets.Set(unit, "traits", new List<UnitTraitAuthoring> { authoring });
            var restored = _assets.Create<UnitDataSO>();
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(unit), restored);
            var data = (UnitData)restored.GetData();
            var trait = (PiercingFalloffTraitData)data.Traits.Single();

            Assert.DoesNotThrow(trait.Validate);
            Assert.That(trait.MaxTargetsPerRay, Is.EqualTo(cap));
            var spec = BattleDefinitionAdapter.ReadUnit(data);
            Assert.That(((PiercingFalloffTraitSpec)spec.Traits.Span[0]).MaxTargetsPerRay, Is.EqualTo(cap));
        }

        [Test]
        public void PiercingTrait_RejectsNegativeCap()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new PiercingFalloffTraitData(30, -1).Validate());
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void Catalog_CreatesAllDataBaseGroups()
        {
            var catalog = Load<DatabaseCatalogSO>(CatalogPath);

            var context = (IDataBaseQueryContext)DataBaseFactory.Create(catalog);

            Assert.That(context.RenderQuery, Is.Not.Null);
            Assert.That(context.InGameQuery, Is.Not.Null);
            Assert.That(context.ViewQuery, Is.Not.Null);
        }

        [Test]
        public void EveryUnitAndStructure_HasUnitVisual()
        {
            IInGameQuery inGame = CreateInGameQuery();
            IViewQuery view = CreateViewQuery();

            var missing = new List<EntityKey>();
            foreach (var unit in inGame.UnitTableQuery.GetAllUnits())
            {
                if (!view.UnitVisualTableQuery.TryGetUnitVisual(unit.Key, out _))
                    missing.Add(unit.Key);
            }

            foreach (var structure in inGame.StructureTableQuery.GetAllStructures())
            {
                if (!view.UnitVisualTableQuery.TryGetUnitVisual(structure.Key, out _))
                    missing.Add(structure.Key);
            }

            Assert.That(missing, Is.Empty, "UnitVisual이 없는 EntityKey");
        }

        [Test]
        public void EveryUnit_TargetingReferenceExistsWhenRequired()
        {
            IInGameQuery inGame = CreateInGameQuery();
            foreach (var unit in inGame.UnitTableQuery.GetAllUnits())
            {
                if (unit.AttackPower > 0)
                    Assert.That(unit.TargetingMethodKey, Is.GreaterThan(0), unit.Key.ToString());
                if (unit.TargetingMethodKey != 0)
                    Assert.That(inGame.TargetingMethodTableQuery.TryGetTargetingMethod(unit.TargetingMethodKey, out _),
                        Is.True, $"Unit {unit.Key}, method {unit.TargetingMethodKey}");
            }
        }

        /// <summary>
        /// 승인된 유닛별 공격 형태(owner inventory §1.1)다. 지정 방식과 사거리를 함께 확인한다.
        /// 메인 캐릭터의 "RadialSquare·사거리 1" 고정 규칙도 실행 검사 대신 여기서 확인한다.
        /// </summary>
        [TestCase(EntityKey.Unit_Akimbo, 1, TargetingShape.RadialSquare, TargetingChoiceMode.Single, 1)]
        [TestCase(EntityKey.Unit_Demon, 1, TargetingShape.RadialSquare, TargetingChoiceMode.Single, 1)]
        [TestCase(EntityKey.Unit_Sniper, 1, TargetingShape.RadialSquare, TargetingChoiceMode.Single, 4)]
        [TestCase(EntityKey.Unit_MainCharacter, 1, TargetingShape.RadialSquare, TargetingChoiceMode.Single, 1)]
        [TestCase(EntityKey.Unit_ShotGun, 2, TargetingShape.Fan, TargetingChoiceMode.Direction, 2)]
        public void InitialUnit_TargetingMethodMatchesApprovedProfile(EntityKey unitKey, int methodKey,
            TargetingShape shape, TargetingChoiceMode choiceMode, int attackRange)
        {
            IInGameQuery inGame = CreateInGameQuery();
            Assert.That(inGame.UnitTableQuery.GetUnit(unitKey).TargetingMethodKey, Is.EqualTo(methodKey));
            Assert.That(inGame.UnitTableQuery.GetUnit(unitKey).AttackRange, Is.EqualTo(attackRange));
            var method = inGame.TargetingMethodTableQuery.GetTargetingMethod(methodKey);
            Assert.That((method.Shape, method.ChoiceMode, method.Splash, method.SplashPercent),
                Is.EqualTo((shape, choiceMode, TargetingSplash.None, 0)));
        }

        [Test]
        public void EveryStage_HasNodeVisual()
        {
            IInGameQuery inGame = CreateInGameQuery();
            IViewQuery view = CreateViewQuery();

            var missing = new List<string>();
            foreach (StageRootData stage in AllStages(inGame))
            {
                if (!view.StageNodeVisualTableQuery.TryGetNodeVisual(stage.StageType, stage.MapRole, out _))
                    missing.Add($"{stage.StageKey} ({stage.StageType}, {stage.MapRole})");
            }

            Assert.That(missing, Is.Empty, "노드 외형이 없는 스테이지");
        }

        [Test]
        public void CombatStages_ReferenceExistingInGameData()
        {
            IInGameQuery inGame = CreateInGameQuery();

            var errors = new List<string>();
            foreach (StageRootData stage in AllStages(inGame))
            {
                if (stage.StageData is not CombatStageDetail combat)
                    continue;

                string label = $"Stage {stage.StageKey}";
                if (!inGame.FieldTableQuery.TryGetField(combat.FieldId, out _))
                    errors.Add($"{label}: Field {combat.FieldId}");
                if (!inGame.UnitRosterTableQuery.TryGetUnitRoster(combat.UnitRosterKey, out var roster))
                    errors.Add($"{label}: UnitRoster {combat.UnitRosterKey}");
                else
                    RequireUnits(inGame, roster.UnitKeys, $"{label} Roster", errors);

                if (combat.EnemyLoadoutKey != 0)
                {
                    if (!inGame.EnemyLoadoutTableQuery.TryGetEnemyLoadout(combat.EnemyLoadoutKey, out var loadout))
                        errors.Add($"{label}: EnemyLoadout {combat.EnemyLoadoutKey}");
                    else
                        foreach (var entry in loadout.Entries)
                            RequireUnits(inGame, new[] { entry.UnitKey }, $"{label} EnemyLoadout", errors);
                }

                if (combat.HeadquartersKey != EntityKey.None &&
                    !inGame.StructureTableQuery.TryGetStructure(combat.HeadquartersKey, out _))
                    errors.Add($"{label}: Headquarters {combat.HeadquartersKey}");

                foreach (EntityKey key in combat.StructureCatalog)
                {
                    if (!inGame.StructureTableQuery.TryGetStructure(key, out _))
                        errors.Add($"{label}: StructureCatalog {key}");
                }

                foreach (var placement in combat.InitialEnemyPlacements)
                    RequireUnits(inGame, new[] { placement.Key }, $"{label} InitialEnemyPlacements", errors);
                foreach (var placement in combat.InitialEnemyStructurePlacements)
                {
                    if (!inGame.StructureTableQuery.TryGetStructure(placement.Key, out _))
                        errors.Add($"{label}: InitialEnemyStructurePlacements {placement.Key}");
                }
            }

            foreach (var loadout in inGame.PlayerLoadoutTableQuery.GetAllPlayerLoadouts())
            {
                foreach (var entry in loadout.Entries)
                    RequireUnits(inGame, new[] { entry.UnitKey }, $"PlayerLoadout {loadout.LoadoutKey}", errors);
            }

            Assert.That(errors, Is.Empty, "InGameGroup 안에서 찾을 수 없는 키");
        }

        [Test]
        public void EveryCombatStage_BuildsOperationalBattleDefinitionWithAttackProfiles()
        {
            // 운영 전투 준비(T1/G7)가 실제 에셋만으로 정의와 공격 프로필을 만들 수 있는지 확인한다.
            var context = (IDataBaseQueryContext)DataBaseFactory.Create(Load<DatabaseCatalogSO>(CatalogPath));
            var errors = new List<string>();
            int combatStages = 0;
            foreach (StageRootData stage in AllStages(context.InGameQuery))
            {
                if (stage.StageData is not CombatStageDetail) continue;
                combatStages++;
                try
                {
                    var definition = BattleDefinitionAdapter.Read(context, stage.MapKey, stage.StageKey, Array.Empty<OwnedCharacterRecord>());
                    if (BattleDefinitionAdapter.ReadAttackProfiles(context, definition.Units).Count == 0)
                        errors.Add($"Stage {stage.StageKey}: 공격 프로필 없음");
                }
                catch (Exception exception)
                {
                    errors.Add($"Stage {stage.StageKey}: {exception.Message}");
                }
            }

            // 로스터에 없는 보유 카드(시작 로드아웃 등)도 전투에 들어오므로 모든 유닛의 프로필 변환을 함께 확인한다.
            var units = context.InGameQuery.UnitTableQuery.GetAllUnits().Select(BattleDefinitionAdapter.ReadUnit).ToArray();
            Assert.DoesNotThrow(() => BattleDefinitionAdapter.ReadAttackProfiles(context, units));
            Assert.That(combatStages, Is.GreaterThan(0));
            Assert.That(errors, Is.Empty, "운영 전투 정의를 만들 수 없는 스테이지");
        }

        [Test]
        public void StartingLoadout_PlansNewRunRosterWithValidRunCurrency()
        {
            // 새 Run 시작(StagePlan.DefaultLoadoutKey)이 실제 에셋만으로 로스터를 만들 수 있는지 확인한다.
            IInGameQuery inGame = CreateInGameQuery();
            var decision = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem.PlayerLoadoutRosterPlanner.Plan(
                inGame.PlayerLoadoutTableQuery, inGame.UnitTableQuery, 1, rosterIsEmpty: true, out var loadout);
            Assert.That(decision.Succeeded, Is.True, decision.FailureReason.ToString());
            Assert.That(loadout.StartingRunCurrency, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void StageMapView_RejectsSecondItem()
        {
            // 설정형 테이블 2개 거부: 노드 프리팹 검증(씬 인스턴스 거부)을 통과하려면 실제 프리팹 에셋이 필요하다.
            var mapView = Load<StageMapViewDataSO>(StageMapViewPath);

            Assert.DoesNotThrow(() => new StageMapViewTable(_assets.Table<StageMapViewDataTableSO>(mapView)));
            Assert.Throws<InvalidOperationException>(() =>
                new StageMapViewTable(_assets.Table<StageMapViewDataTableSO>(mapView, mapView)));
        }

        private static IEnumerable<StageRootData> AllStages(IInGameQuery inGame)
        {
            foreach (var map in inGame.StageMapTableQuery.GetAllStageMaps())
            {
                foreach (StageRootData stage in inGame.StageTableQuery.GetStages(map.MapKey))
                    yield return stage;
            }
        }

        private static void RequireUnits(IInGameQuery inGame, IEnumerable<EntityKey> keys, string label, List<string> errors)
        {
            foreach (EntityKey key in keys)
            {
                if (!inGame.UnitTableQuery.TryGetUnit(key, out _))
                    errors.Add($"{label}: Unit {key}");
            }
        }

        private static IInGameQuery CreateInGameQuery()
        {
            return (IInGameQuery)InGameDataBaseFactory.Create(Load<InGameGroup>(InGameGroupPath));
        }

        private static IViewQuery CreateViewQuery()
        {
            return (IViewQuery)ViewDataBaseFactory.Create(Load<ViewGroup>(ViewGroupPath));
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"에셋을 찾을 수 없습니다: {path}");
            return asset;
        }
    }
}
