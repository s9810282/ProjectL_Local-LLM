using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class NexusReinforcementAuthoringTests
    {
        private const string SpawnIntervalPath = "nexusReinforcements.spawnIntervalTurns";

        private static readonly OwnedCharacterRecord Main =
            new(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main);

        [Test]
        public void Authoring_PreservesInitialPlacementAndCapturesStageLevel()
        {
            using var assets = new StageAssets();
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            var before = Read(database, assets.Read());
            assets.ConfigurePool(true, assets.PoolUnit, 3);
            var authored = Read(database, assets.Read());
            var prepared = Prepare(authored);
            var snapshot = new CombatStateData(prepared);
            var next = new CombatStateData(snapshot, snapshot.CommandState);

            assets.ConfigurePool(true, assets.PoolUnit, 8, enemyLevel: 6);
            var edited = Read(database, assets.Read());
            Assert.That(authored.NexusReinforcements.EnemyLevel, Is.EqualTo(3));
            Assert.That(authored.NexusReinforcements.Pool.Single().Count, Is.EqualTo(3));
            Assert.That(edited.NexusReinforcements.Pool.Single().Count, Is.EqualTo(8));
            Assert.That(edited.NexusReinforcements.EnemyLevel, Is.EqualTo(6));
            Assert.That(next.NexusReinforcements, Is.SameAs(authored.NexusReinforcements));
            Assert.That(authored.Stage.TimelineSeed, Is.EqualTo(before.Stage.TimelineSeed));
            Assert.That(authored.Stage.InitialEnemyPlacements.ToArray().Select(p => (p.UnitKey, p.Anchor, p.Rotation, p.Level)),
                Is.EqualTo(before.Stage.InitialEnemyPlacements.ToArray().Select(p => (p.UnitKey, p.Anchor, p.Rotation, p.Level))));
            Assert.That(prepared.Characters.Single(c => c.Owner == Owner.Enemy).Experience.EnemyLevel, Is.EqualTo(3));
        }

        [Test]
        public void Authoring_CapturesIntervalInPreparedBattle()
        {
            using var assets = new StageAssets();
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            assets.ConfigurePool(true, assets.PoolUnit, 3);
            assets.SetInterval(2);
            var first = Prepare(Read(database, assets.Read()));

            assets.SetInterval(3);
            var second = Prepare(Read(database, assets.Read()));

            Assert.That(first.Combat.NexusReinforcements.SpawnIntervalTurns, Is.EqualTo(2));
            Assert.That(second.Combat.NexusReinforcements.SpawnIntervalTurns, Is.EqualTo(3));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void DisabledAuthoring_IgnoresInvalidInterval(int interval)
        {
            using var assets = new StageAssets();
            assets.ConfigurePool(false, null, 0);
            assets.SetInterval(interval);

            Assert.That(assets.Read().NexusReinforcements, Is.Null);
        }

        [Test]
        public void LegacySerializedStage_UsesIntervalOne()
        {
            var spec = ReadReimportedStage(null);
            Assert.That(spec.SpawnIntervalTurns, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void EnabledSerializedStage_RejectsInvalidInterval(int interval)
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() => ReadReimportedStage(interval));
            Assert.That(error.ParamName, Is.EqualTo("spawnIntervalTurns"));
        }

        [Test]
        public void PoolChanges_PreserveWeightedInitialLoadoutSeedAndResults()
        {
            using var assets = new StageAssets();
            var loadout = assets.UseInitialLoadout();
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config)
            {
                EnemyLoadouts = new EnemyLoadoutTable(new LoadoutProvider(loadout))
            };
            var before = Read(database, assets.Read());
            assets.ConfigurePool(true, assets.PoolUnit, 3);
            var after = Read(database, assets.Read());

            Assert.That(((EnemyLoadoutData)loadout.GetData()).RandomSeed, Is.EqualTo(29));
            Assert.That(after.Stage.InitialEnemyPlacements.Length, Is.EqualTo(2));
            Assert.That(after.Stage.InitialEnemyPlacements.ToArray().Select(p => (p.UnitKey, p.Anchor, p.Rotation, p.Level)),
                Is.EqualTo(before.Stage.InitialEnemyPlacements.ToArray().Select(p => (p.UnitKey, p.Anchor, p.Rotation, p.Level))));
            Assert.That(Prepare(after).Combat.NexusReinforcements.Pool.Single().Count, Is.EqualTo(3));
        }

        [Test]
        public void DisabledAuthoring_PreservesLegacyDefinition()
        {
            using var assets = new StageAssets();
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            Assert.That(assets.Read().NexusReinforcements, Is.Null);
            assets.ConfigurePool(false, null, 0);
            var initialization = Read(database, assets.Read());

            Assert.That(initialization.NexusReinforcements, Is.Null);
            Assert.That(Prepare(initialization).Combat.NexusReinforcements, Is.Null);
            Assert.That(initialization.Stage.InitialEnemyPlacements.Length, Is.EqualTo(1));
        }

        [Test]
        public void PoolOnlyUnit_HasResolvedAttackProfile()
        {
            using var assets = new StageAssets();
            assets.ConfigurePool(true, assets.PoolUnit, 2);
            var initialization = Read(new CoreIntegrationTestData(ConfiguredMovementBattle.Config), assets.Read());
            var candidate = initialization.NexusReinforcements.Pool.Single();
            Assert.That(candidate.UnitKey, Is.EqualTo(EntityKey.Unit_Demon));
            Assert.That(initialization.AttackProfiles.Single(p => p.Definition.Key == candidate.UnitKey).OwnedId, Is.Null);
            Assert.That(Prepare(initialization).Characters.Select(c => c.CharacterKey), Has.No.Member(candidate.UnitKey));
        }

        [Test]
        public void EnabledAuthoring_RejectsMissingReference()
        {
            using var assets = new StageAssets();
            assets.ConfigurePool(true, null, 1);
            Assert.Throws<InvalidOperationException>(() => assets.Read());
        }

        [Test]
        public void PoolUnitOutsideStageRoster_IsRejectedEvenWhenOwned()
        {
            using var assets = new StageAssets();
            assets.ConfigurePool(true, assets.InitialUnit, 1);
            var owned = new[] { Main, new OwnedCharacterRecord(new CharacterInstanceId(2), EntityKey.Unit_Cowboy, CharacterOwnershipRole.Sub) };
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            var error = Assert.Throws<InvalidOperationException>(() => Read(database, assets.Read(), owned));
            Assert.That(error.Message, Does.Contain("UnitRoster"));
        }

        [TestCase("boss objective", "본부 파괴")]
        [TestCase("missing nexus", "적 본부")]
        [TestCase("multiple nexus", "적 본부")]
        [TestCase("player only", "소유 권한")]
        [TestCase("boss unit", "보스")]
        [TestCase("multi-cell", "다칸 유닛")]
        public void DbDefinition_RejectsInvalidPreparation(string condition, string reason)
        {
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            var placements = new[] { new EntityPlacement(EntityKey.Unit_Cowboy, new CellCoord(0, 4), Rotation4.R0,
                isVictoryTarget: condition == "boss objective") };
            var extraStructures = condition == "multiple nexus" ?
                new[] { new EntityPlacement(EntityKey.Structure_Headquarters, new CellCoord(1, 5), Rotation4.R0) } :
                Array.Empty<EntityPlacement>();
            var spec = new NexusReinforcementSpec(new[] { new NexusReinforcementEntry(EntityKey.Unit_Demon, 1) }, 3);
            if (condition == "player only" || condition == "boss unit" || condition == "multi-cell")
            {
                var unit = new UnitData(EntityKey.Unit_Demon, UnitRole.Regular,
                    condition == "player only" ? UnitOwnerEligibility.PlayerOnly : UnitOwnerEligibility.EnemyOnly,
                    condition == "multi-cell" ? 2 : 1, 1, 10, 0, 1, condition == "boss unit" ? CategoryKey.Boss : CategoryKey.Main,
                    5, 1, Array.Empty<UnitTraitData>(), targetingMethodKey: 1,
                    behavior: condition == "boss unit" ? new UnitBehaviorSpec(false, true, false) : null,
                    killReward: UnitKillRewardSpec.Excluded);
                database.SetUnit(unit);
            }
            var detail = new CombatStageDetail(17, "field", 1, 0, 1, placements, Array.Empty<EntityKey>(),
                extraStructures, condition == "missing nexus" ? EntityKey.None : EntityKey.Structure_Headquarters,
                CellCoord.Zero, Rotation4.R0, new CellCoord(0, 5), Rotation4.R0, null,
                objectiveKind: condition == "boss objective" ? BattleObjectiveKind.DesignatedBossDefeated : BattleObjectiveKind.EnemyHeadquartersDestroyed,
                enemyLevel: 3, nexusReinforcements: spec);

            var error = Assert.Throws<InvalidOperationException>(() => Prepare(Read(database, detail)));
            if (condition == "multi-cell")
            {
                Assert.That(error.Message, Does.Contain("Core 공격 프로필"));
                Assert.That(error.InnerException, Is.TypeOf<ArgumentException>());
                Assert.That(error.InnerException.Message, Does.Contain(reason));
            }
            else
                Assert.That(error.Message, Does.Contain(reason));
        }

        private static NexusReinforcementSpec ReadReimportedStage(int? interval)
        {
            string path = $"Assets/NexusReinforcementInterval_{Guid.NewGuid():N}.asset";
            var stage = ScriptableObject.CreateInstance<CombatStageData>();
            try
            {
                using (var serialized = new SerializedObject(stage))
                {
                    serialized.FindProperty("nexusReinforcements.enabled").boolValue = true;
                    if (interval.HasValue)
                        serialized.FindProperty(SpawnIntervalPath).intValue = interval.Value;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                AssetDatabase.CreateAsset(stage, path);
                AssetDatabase.SaveAssetIfDirty(stage);
                string yaml = File.ReadAllText(path);
                Assert.That(yaml, Does.Match(@"(?m)^  nexusReinforcements:\r?\n    enabled: 1\r?$"));
                Resources.UnloadAsset(stage);

                if (interval.HasValue)
                {
                    Assert.That(yaml, Does.Match($@"(?m)^    spawnIntervalTurns: {interval.Value}\r?$"));
                }
                else
                {
                    // 테스트가 저장한 SO에서 새 필드만 빼고 다시 읽어 기존 직렬화 형식을 재현합니다.
                    yaml = Regex.Replace(yaml, @"(?m)^    spawnIntervalTurns: -?\d+\r?\n", "");
                    Assert.That(yaml, Does.Not.Contain("spawnIntervalTurns:"));
                    File.WriteAllText(path, yaml);
                }

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var loaded = AssetDatabase.LoadAssetAtPath<CombatStageData>(path);
                Assert.That(loaded, Is.Not.Null);
                return ((CombatStageDetail)loaded.GetData()).NexusReinforcements;
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                if (stage != null)
                    Object.DestroyImmediate(stage);
            }
        }

        private static BattleInitialization Read(CoreIntegrationTestData database, CombatStageDetail detail,
            IReadOnlyList<OwnedCharacterRecord> owned = null)
        {
            database.SetStage(new StageRootData(1101, 1, StageMapRole.Regular, detail));
            var definition = BattleDefinitionAdapter.Read(database, 1, 1101, owned ?? new[] { Main });
            return definition.WithAttackProfiles(BattleDefinitionAdapter.ReadAttackProfiles(database, definition.Units));
        }

        private static BattlePreparation Prepare(BattleInitialization definition) =>
            BattlePreparationCalculator.Prepare(definition, new[] { Main }, new MainCharacterPersistentState(10, 10, 1),
                new Dictionary<CharacterInstanceId, int> { [Main.Id] = 0 },
                new InitialDrawPlan(Array.Empty<CharacterInstanceId>()), Array.Empty<CharacterCombatFacts>());

        private sealed class LoadoutProvider : IDataProvider<IDatabaseItem>
        {
            private readonly EnemyLoadoutDataSO _loadout;

            internal LoadoutProvider(EnemyLoadoutDataSO loadout) => _loadout = loadout;

            public IEnumerable<IDatabaseItem> GetAllData() => new[] { _loadout };
        }

        private sealed class StageAssets : IDisposable
        {
            private readonly List<ScriptableObject> _created = new();
            private readonly CombatStageData _stage;
            internal UnitDataSO PoolUnit { get; }
            internal UnitDataSO InitialUnit { get; }

            internal StageAssets()
            {
                var field = Create<FieldDataSO>();
                using (var serialized = new SerializedObject(field))
                {
                    serialized.FindProperty("fieldId").stringValue = "field";
                    serialized.FindProperty("columnCount").intValue = 8;
                    serialized.FindProperty("rowCount").intValue = 6;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                PoolUnit = CreateUnit(EntityKey.Unit_Demon);
                InitialUnit = CreateUnit(EntityKey.Unit_Cowboy);
                _stage = Create<CombatStageData>();
                using var stage = new SerializedObject(_stage);
                stage.FindProperty("fieldDefinition").objectReferenceValue = field;
                stage.FindProperty("unitRoster").objectReferenceValue = Create<UnitRosterDataSO>();
                stage.FindProperty("economy").objectReferenceValue = Create<StageEconomyDataSO>();
                stage.FindProperty("headquarters").objectReferenceValue = Create<StructureDataSO>();
                stage.FindProperty("enemyHeadquartersAnchor.y").intValue = 5;
                stage.FindProperty("timelineSeed").intValue = 17;
                stage.FindProperty("enemyLevel").intValue = 3;
                var placements = stage.FindProperty("initialEnemyPlacements");
                placements.arraySize = 1;
                var placement = placements.GetArrayElementAtIndex(0);
                placement.FindPropertyRelative("unitDefinition").objectReferenceValue = InitialUnit;
                placement.FindPropertyRelative("anchor.y").intValue = 4;
                stage.ApplyModifiedPropertiesWithoutUndo();
            }

            internal CombatStageDetail Read() => (CombatStageDetail)_stage.GetData();

            internal void SetInterval(int interval)
            {
                using var serialized = new SerializedObject(_stage);
                serialized.FindProperty(SpawnIntervalPath).intValue = interval;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            internal EnemyLoadoutDataSO UseInitialLoadout()
            {
                var loadout = Create<EnemyLoadoutDataSO>();
                using (var serialized = new SerializedObject(loadout))
                {
                    serialized.FindProperty("randomSeed").intValue = 29;
                    serialized.FindProperty("spawnCount").intValue = 2;
                    var entries = serialized.FindProperty("entries");
                    entries.arraySize = 1;
                    var entry = entries.GetArrayElementAtIndex(0);
                    entry.FindPropertyRelative("unitDefinition").objectReferenceValue = PoolUnit;
                    entry.FindPropertyRelative("weight").intValue = 1;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                using var stage = new SerializedObject(_stage);
                stage.FindProperty("initialEnemyPlacements").ClearArray();
                stage.FindProperty("enemyLoadout").objectReferenceValue = loadout;
                stage.ApplyModifiedPropertiesWithoutUndo();
                return loadout;
            }

            internal void ConfigurePool(bool enabled, UnitDataSO unit, int count, int enemyLevel = 3)
            {
                using var serialized = new SerializedObject(_stage);
                serialized.FindProperty("enemyLevel").intValue = enemyLevel;
                var authoring = serialized.FindProperty("nexusReinforcements");
                Assert.That(authoring, Is.Not.Null);
                authoring.FindPropertyRelative("enabled").boolValue = enabled;
                var pool = authoring.FindPropertyRelative("pool");
                pool.arraySize = 1;
                var entry = pool.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("unitDefinition").objectReferenceValue = unit;
                entry.FindPropertyRelative("count").intValue = count;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            private UnitDataSO CreateUnit(EntityKey key)
            {
                var unit = Create<UnitDataSO>();
                using var serialized = new SerializedObject(unit);
                serialized.FindProperty("key").intValue = (int)key;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return unit;
            }

            private T Create<T>() where T : ScriptableObject
            {
                var asset = ScriptableObject.CreateInstance<T>();
                _created.Add(asset);
                return asset;
            }

            public void Dispose()
            {
                foreach (var asset in _created)
                    Object.DestroyImmediate(asset);
            }
        }
    }
}
