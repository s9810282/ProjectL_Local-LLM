using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.PlayerSkill;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.PlayerSkill.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.Factory.DBFactory.Group;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.ServiceKeys.DBFactory;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PlayerSkillAuthoringTests
    {
        private readonly List<ScriptableObject> _objects = new();

        [TearDown]
        public void DestroyFixtures()
        {
            foreach (var item in _objects)
                UnityEngine.Object.DestroyImmediate(item);
            _objects.Clear();
        }

        [Test]
        public void TestOwnedTableResolvesBothMainDefinitionsAndFreshPureRows()
        {
            var akimbo = Powder();
            var sniper = Row(PlayerSkillKey.MagicBullet, EntityKey.Unit_Sniper, PlayerSkillUpgradeKey.Distortion, PlayerSkillUpgradeKey.Judgment);
            var table = Table(akimbo, sniper);
            Assert.That(table.GetPlayerSkill(PlayerSkillKey.PowderBlossom).Definition.StageUseLimit, Is.EqualTo(1));
            Assert.That(table.TryGetPlayerSkillForMain(EntityKey.Unit_Sniper, out var magic), Is.True);
            Assert.That(magic.Definition.Key, Is.EqualTo(PlayerSkillKey.MagicBullet));
            Assert.That(magic.Definition.StageUseLimit, Is.EqualTo(1));
            Assert.That(table.TryGetPlayerSkillForMain(EntityKey.Unit_ShotGun, out _), Is.False);
            var first = (PlayerActiveSkillData)akimbo.GetData();
            var second = (PlayerActiveSkillData)akimbo.GetData();
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Definition, Is.Not.SameAs(second.Definition));
            Assert.That(first.Definition.MainCharacterKey, Is.EqualTo(EntityKey.Unit_Akimbo));
            Assert.That(first, Is.Not.InstanceOf<UnityEngine.Object>());
        }

        [TestCase(true, "PowderBlossom")]
        [TestCase(false, "Unit_Akimbo")]
        public void DuplicateDefinitionsIdentifyTheConflictingKey(bool sameSkill, string key)
        {
            var other = sameSkill
                ? Row(PlayerSkillKey.PowderBlossom, EntityKey.Unit_Sniper, PlayerSkillUpgradeKey.Bloom, PlayerSkillUpgradeKey.Condemnation)
                : Row(PlayerSkillKey.MagicBullet, EntityKey.Unit_Akimbo, PlayerSkillUpgradeKey.Distortion, PlayerSkillUpgradeKey.Judgment);
            Assert.That(Assert.Throws<InvalidOperationException>(() => Table(Powder(), other)).Message, Does.Contain(key));
        }

        [TestCase(0, PlayerSkillUpgradeKey.Bloom, UnitRole.MainCharacter)]
        [TestCase(1, PlayerSkillUpgradeKey.Judgment, UnitRole.MainCharacter)]
        [TestCase(1, PlayerSkillUpgradeKey.Condemnation, UnitRole.MainCharacter)]
        [TestCase(1, PlayerSkillUpgradeKey.Bloom, UnitRole.Regular)]
        public void InvalidRowsReportSourceIdentity(int limit, PlayerSkillUpgradeKey first, UnitRole role)
        {
            var row = Row(PlayerSkillKey.PowderBlossom, EntityKey.Unit_Akimbo, first, PlayerSkillUpgradeKey.Condemnation, role, limit);
            string message = Assert.Throws<InvalidOperationException>(() => Table(row)).Message;
            Assert.That(message, Does.Contain("PowderBlossom"));
            Assert.That(message, Does.Contain("Unit_Akimbo"));
        }

        [Test]
        public void TestOwnedGroupExportsSkillQueryAndAllowsMissingTable()
        {
            var powder = Powder();
            var magic = Row(PlayerSkillKey.MagicBullet, EntityKey.Unit_Sniper, PlayerSkillUpgradeKey.Distortion, PlayerSkillUpgradeKey.Judgment);
            var akimbo = OwnerOf(powder);
            var sniper = OwnerOf(magic);
            CoreAuthoringFixture.Set(sniper, "traits", new List<UnitTraitAuthoring> { new PiercingFalloffTraitAuthoring() });
            var units = new[] { akimbo, sniper };
            var query = (IHasPlayerActiveSkillTable)CreateGroupQuery(units, SkillTable(powder, magic));
            Assert.That(query.PlayerActiveSkills.GetPlayerSkill(PlayerSkillKey.PowderBlossom).Definition.StageUseLimit, Is.EqualTo(1));
            Assert.That(query.PlayerActiveSkills.TryGetPlayerSkillForMain(EntityKey.Unit_Sniper, out var row), Is.True);
            Assert.That(row.Definition.StageUseLimit, Is.EqualTo(1));
            Assert.That(query.PlayerActiveSkills.TryGetPlayerSkillForMain(EntityKey.Unit_Cowboy, out _), Is.False);
            Assert.That(((IHasPlayerActiveSkillTable)CreateGroupQuery(units)).PlayerActiveSkills, Is.Null);
        }

        [Test]
        public void FactoryRejectsDuplicateRowsAndNonCanonicalOwnersWithSourceIdentity()
        {
            var powder = Powder();
            var akimbo = OwnerOf(powder);
            var duplicate = Row(PlayerSkillKey.MagicBullet, EntityKey.Unit_Akimbo, PlayerSkillUpgradeKey.Distortion, PlayerSkillUpgradeKey.Judgment);
            var error = Assert.Throws<InvalidOperationException>(() => CreateGroupQuery(new[] { akimbo }, SkillTable(powder, duplicate)));
            Assert.That(error.Message, Does.Contain("MagicBullet").And.Contain("Unit_Akimbo"));
            var differentAkimbo = OwnerOf(Powder());
            CoreAuthoringFixture.Set(differentAkimbo, "role", UnitRole.Regular);
            CoreAuthoringFixture.Set(differentAkimbo, "category", CategoryKey.Rider);
            error = Assert.Throws<InvalidOperationException>(() => CreateGroupQuery(new[] { differentAkimbo }, SkillTable(powder)));
            Assert.That(error.Message, Does.Contain("PowderBlossom").And.Contain("Unit_Akimbo"));
        }

        [Test]
        public void RegisteredMagicBulletRequiresAnAuthoredPiercingDefinition()
        {
            var magic = Row(PlayerSkillKey.MagicBullet, EntityKey.Unit_Sniper, PlayerSkillUpgradeKey.Distortion, PlayerSkillUpgradeKey.Judgment);
            var error = Assert.Throws<InvalidOperationException>(() => CreateGroupQuery(new[] { OwnerOf(magic) }, SkillTable(magic)));
            Assert.That(error.Message, Does.Contain("MagicBullet").And.Contain("Unit_Sniper"));
        }

        [TestCase(EntityKey.Unit_Akimbo, PlayerSkillKey.PowderBlossom)]
        [TestCase(EntityKey.Unit_Sniper, PlayerSkillKey.MagicBullet)]
        public void ProductionCatalogBindsSelectedMainAndConsumesOneAuthoredUse(EntityKey key, PlayerSkillKey skill)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset");
            var database = (IDataBaseQueryContext)DataBaseFactory.Create(catalog);
            var units = database.InGameQuery.UnitTableQuery;
            Assert.That(units.GetUnit(key).Role, Is.EqualTo(UnitRole.MainCharacter));
            var table = ((IHasPlayerActiveSkillTable)database.InGameQuery).PlayerActiveSkills;
            Assert.That(table.TryGetPlayerSkillForMain(key, out var data), Is.True);
            Assert.That(data.Definition.Key, Is.EqualTo(skill));
            int authoredLimit = data.Definition.StageUseLimit;

            using var game = new PlayerSkillRuntimeFixture(database, useAuthoredChapter: true);
            if (key == EntityKey.Unit_Sniper)
            {
                var powder = game.Core.Characters.ReadPlayerSkillUpgradeAvailability();
                var upgrade = game.Core.Characters.TryApplyPlayerSkillUpgrade(powder.RunId, powder.RosterRevision,
                    powder.MainId, PlayerSkillUpgradeKey.Bloom);
                Assert.That(upgrade.Succeeded, Is.True);
                var acquired = game.Core.Characters.Acquire(key);
                Assert.That(acquired.Succeeded, Is.True);
                var candidate = acquired.Acquired.Single();
                Assert.That(candidate.Role, Is.EqualTo(CharacterOwnershipRole.Main));
                Assert.That(game.Buffers.RunDeckReadSink.TryGetCurrent(out var deck), Is.True);
                var ids = new[] { candidate.Id }.Concat(deck.OrderedIds.Skip(1)).ToArray();
                var request = new DeckEditRequest(deck.RunId, game.Buffers.CharacterReadSink.RosterRevision, deck.DeckRevision, ids);
                Assert.That(game.Buffers.RunDeckWriteSink.TryPublish(request, ids), Is.EqualTo(DeckFailureReason.None));
                Assert.That(game.Core.Characters.ReadPlayerSkillUpgradeAvailability().CurrentBranch, Is.EqualTo(PlayerSkillUpgradeKey.None));
                Assert.That(game.Buffers.CharacterReadSink.Owned.Single(c => c.Id == powder.MainId).PlayerSkillUpgrade,
                    Is.EqualTo(PlayerSkillUpgradeKey.Bloom));
            }
            Assert.That(game.Core.Characters.ReadPlayerSkillUpgradeAvailability().Definition.Key, Is.EqualTo(skill));
            int firstStage = database.InGameQuery.StageMapTableQuery.GetStageMap(1).FirstStageKey;
            var nodes = game.Buffers.StageProgressReadSink.GetStages(1);
            int firstNode = nodes.Single(n => n.IsStart).NextNodeIds.Single();
            Assert.That(nodes.Single(n => n.NodeId == firstNode).StageKey, Is.EqualTo(firstStage));
            game.Enter(firstNode);
            Assert.That(game.Skill.Definition.Key, Is.EqualTo(skill));
            Assert.That(game.Skill.Definition.MainCharacterKey, Is.EqualTo(key));
            Assert.That(game.Skill.RemainingUses, Is.EqualTo(authoredLimit));
            game.DeployMain(game.FindMainPlacement());
            var points = skill == PlayerSkillKey.MagicBullet ? SelectTwoPoints(game) : Array.Empty<CellCoord>();
            var forecast = game.Forecast(points);
            Assert.That(forecast.CanUse, Is.True, forecast.FailureReason.ToString());
            var before = game.Buffers.CombatStateReadSink.LatestResultSequence;
            var result = game.Submit(CombatViewCommandData.ForPlayerSkill(game.Skill.ActorId, points, forecast.Source));
            Assert.That(result.Sequence, Is.EqualTo(before + 1));
            Assert.That(result.PlayerSkill.SkillKey, Is.EqualTo(skill));
            Assert.That(game.Skill.RemainingUses, Is.EqualTo(authoredLimit - 1));
        }

        private static CellCoord[] SelectTwoPoints(PlayerSkillRuntimeFixture game)
        {
            var first = game.Selection(Array.Empty<CellCoord>()).NextCandidateCells.First(cell =>
                game.Selection(new[] { cell }).NextCandidateCells.Count > 0);
            return new[] { first, game.Selection(new[] { first }).NextCandidateCells[0] };
        }

        private static UnitDataSO OwnerOf(PlayerActiveSkillDataSO row)
        {
            using var fields = new SerializedObject(row);
            return (UnitDataSO)fields.FindProperty("mainCharacter").objectReferenceValue;
        }

        private IInGameQuery CreateGroupQuery(UnitDataSO[] units, PlayerActiveSkillDataTableSO skillTable = null)
        {
            using var inputs = new CoreAuthoringFixture();
            var method = inputs.CreateTargetingMethod();
            foreach (var unit in units) CoreAuthoringFixture.Set(unit, "targetingMethod", method);
            var category = Create<CategoryConfigDataSO>();
            using (var fields = new SerializedObject(category))
            {
                foreach (string field in new[] { "mainMovement", "riderMovement", "heavyMovement", "cannonMovement" })
                    fields.FindProperty(field).FindPropertyRelative("movementAllowance").intValue = 2;
                fields.ApplyModifiedPropertiesWithoutUndo();
            }
            var board = Create<ArtifactBoardConfigDataSO>();
            CoreAuthoringFixture.Set(board, "initialWidth", 4);
            CoreAuthoringFixture.Set(board, "initialHeight", 4);
            var tables = new List<DatabaseTable>
            {
                Create<StageMapDataTableSO>(), Create<StageRootDataTableSO>(), Create<FieldDataTableSO>(),
                TableWithRows<TargetingMethodDataTableSO>(method), TableWithRows<UnitDataTableSO>(units),
                Create<StructureDataTableSO>(), Create<EnemyLoadoutDataTableSO>(), Create<PlayerLoadoutDataTableSO>(),
                Create<UnitRosterDataTableSO>(), Create<ArtifactDefinitionDataTableSO>(),
                TableWithRows<CategoryConfigDataTableSO>(category), TableWithRows<ArtifactBoardConfigDataTableSO>(board),
                Create<StageEconomyDataTableSO>(), Create<RecoveryOptionDataTableSO>(), Create<ConsumableItemDataTableSO>()
            };
            if (skillTable != null) tables.Add(skillTable);
            var group = Create<InGameGroup>();
            using (var fields = new SerializedObject(group))
            {
                var property = fields.FindProperty("databaseTables");
                property.arraySize = tables.Count;
                for (int i = 0; i < tables.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = tables[i];
                fields.ApplyModifiedPropertiesWithoutUndo();
            }
            var builder = new SystemBuilderHelper();
            InGameDataBaseFactory.Create(group).ExportServices(builder);
            return builder.BuildSystemReader().ServiceStoreReader.GetService(DataBaseGroupKey.InGame);
        }

        private PlayerActiveSkillDataSO Powder() =>
            Row(PlayerSkillKey.PowderBlossom, EntityKey.Unit_Akimbo, PlayerSkillUpgradeKey.Bloom, PlayerSkillUpgradeKey.Condemnation);

        private PlayerActiveSkillDataSO Row(PlayerSkillKey skill, EntityKey key, PlayerSkillUpgradeKey first, PlayerSkillUpgradeKey second,
            UnitRole role = UnitRole.MainCharacter, int limit = 1)
        {
            var unit = Create<UnitDataSO>();
            using (var fields = new SerializedObject(unit))
            {
                fields.FindProperty("key").intValue = (int)key;
                fields.FindProperty("role").intValue = (int)role;
                fields.ApplyModifiedPropertiesWithoutUndo();
            }
            var row = Create<PlayerActiveSkillDataSO>();
            row.name = skill.ToString();
            using var authored = new SerializedObject(row);
            authored.FindProperty("skillKey").intValue = (int)skill;
            authored.FindProperty("mainCharacter").objectReferenceValue = unit;
            authored.FindProperty("stageUseLimit").intValue = limit;
            authored.FindProperty("firstUpgrade").intValue = (int)first;
            authored.FindProperty("secondUpgrade").intValue = (int)second;
            authored.ApplyModifiedPropertiesWithoutUndo();
            return row;
        }

        private PlayerActiveSkillTable Table(params PlayerActiveSkillDataSO[] rows) => new(SkillTable(rows));

        private PlayerActiveSkillDataTableSO SkillTable(params PlayerActiveSkillDataSO[] rows) =>
            TableWithRows<PlayerActiveSkillDataTableSO>(rows);

        private T TableWithRows<T>(params DatabaseItem[] rows) where T : DatabaseTable
        {
            var table = Create<T>();
            using var authored = new SerializedObject(table);
            var items = authored.FindProperty("databaseItems");
            items.arraySize = rows.Length;
            for (int index = 0; index < rows.Length; index++)
                items.GetArrayElementAtIndex(index).objectReferenceValue = rows[index];
            authored.ApplyModifiedPropertiesWithoutUndo();
            return table;
        }

        private T Create<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            _objects.Add(instance);
            return instance;
        }
    }
}
