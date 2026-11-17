using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Obstacle.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PillarPlatformAuthoringTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void RepeatedSoConversionPreservesShapeAndRoute(int length)
        {
            using var fixture = new CoreAuthoringFixture();
            var stage = Stage(fixture);
            AuthorPlatform(stage, length);
            var original = (CombatStageDetail)stage.GetData();
            var roundTrip = (CombatStageDetail)fixture.Clone(stage).GetData();
            var left = Platforms(original).Single();
            var right = Platforms(roundTrip).Single();
            Assert.That(left, Is.Not.SameAs(right));
            Assert.That(right.Length, Is.EqualTo(length));
            Assert.That(right.ShapeAxis, Is.EqualTo(PlatformAxis.Horizontal));
            Assert.That(right.TravelAxis, Is.EqualTo(PlatformAxis.Vertical));
            Assert.That(right.FootprintAt(new CellCoord(1, 1)), Has.Count.EqualTo(length));
        }

        [TestCase("route")]
        [TestCase("sign")]
        [TestCase("length")]
        [TestCase("source")]
        [TestCase("initial")]
        [TestCase("bounds")]
        [TestCase("support")]
        [TestCase("overlap")]
        public void InvalidAuthoredPlatformGeometryIsRejected(string edit)
        {
            using var fixture = new CoreAuthoringFixture();
            var stage = Stage(fixture);
            AuthorPlatform(stage, 2, sharedRoute: edit == "overlap", change: (index, row) =>
            {
                if (index == 1)
                {
                    row.FindPropertyRelative("initialAnchor.y").intValue = 1;
                    return;
                }
                switch (edit)
                {
                    case "route": row.FindPropertyRelative("routeEnd.x").intValue = 2; break;
                    case "sign": row.FindPropertyRelative("initialSign").intValue = 0; break;
                    case "length": row.FindPropertyRelative("length").intValue = 4; break;
                    case "source": row.FindPropertyRelative("sourceId").longValue = 11; break;
                    case "initial": row.FindPropertyRelative("initialAnchor.y").intValue = 5; break;
                    case "bounds":
                        row.FindPropertyRelative("routeEnd.x").intValue = 7;
                        row.FindPropertyRelative("routeEnd.y").intValue = 1;
                        break;
                    case "support": row.FindPropertyRelative("initialAnchor.y").intValue = 2; break;
                }
            });
            if (edit is "support" or "overlap" or "bounds")
                Assert.Throws<InvalidOperationException>(() => stage.GetData());
            else
                Assert.Catch<ArgumentException>(() => stage.GetData());
        }

        [Test]
        public void SharedRouteAndPillarReachTheFirstTurnThroughTheDatabaseSource()
        {
            using var fixture = new CoreAuthoringFixture();
            var stage = Stage(fixture);
            var pillar = fixture.Create<ObstacleDefinitionDataSO>();
            CoreAuthoringFixture.Set(pillar, "kind", ObstacleContentKind.Pillar);
            CoreAuthoringFixture.Set(pillar, "requiredHits", 4);
            var authored = (ObstacleDefinitionData)pillar.GetData();
            Assert.That(authored.CreatePlacement(new CellCoord(6, 3), Rotation4.R0).FallLength, Is.EqualTo(3));
            CoreAuthoringFixture.Set(pillar, "fallLength", 5);
            using (var serialized = new SerializedObject(stage))
            {
                var obstacles = serialized.FindProperty("environment.obstaclePlacements");
                obstacles.arraySize = 1;
                var row = obstacles.GetArrayElementAtIndex(0);
                row.FindPropertyRelative("definition").objectReferenceValue = pillar;
                row.FindPropertyRelative("anchor.x").intValue = 6;
                row.FindPropertyRelative("anchor.y").intValue = 3;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AuthorPlatform(stage, 2, sharedRoute: true);
            var detail = (CombatStageDetail)stage.GetData();
            Assert.That(Platforms(detail).Count(), Is.EqualTo(2));
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            database.SetStage(new StageRootData(1101, 1, StageMapRole.Regular, detail));
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, database: database);
            var state = battle.Buffers.CombatStateReadSink.Current;
            Assert.That(state.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Player));
            Assert.That(state.CommandState.LastPlatformBoundaryCount, Is.Zero);
            Assert.That(state.ReadPlatforms().Select(platform => platform.Cell),
                Is.EqualTo(new[] { new CellCoord(1, 1), new CellCoord(1, 3) }));
            Assert.That(state.ReadPlatforms().Select(platform => platform.TravelSign), Is.EqualTo(new[] { 1, -1 }));
            var content = state.ObstacleContents.Single();
            Assert.That(content.Kind, Is.EqualTo(ObstacleContentKind.Pillar));
            Assert.That(content.FallLength, Is.EqualTo(5));
            Assert.That(state.Structures.Single(s => s.EntityId == content.EntityId).ObstacleDurability.RemainingHits,
                Is.EqualTo(4));
        }

        [Test]
        public void PillarRejectsNonpositiveFallLength()
        {
            using var fixture = new CoreAuthoringFixture();
            var pillar = fixture.Create<ObstacleDefinitionDataSO>();
            CoreAuthoringFixture.Set(pillar, "kind", ObstacleContentKind.Pillar);
            CoreAuthoringFixture.Set(pillar, "requiredHits", 1);
            CoreAuthoringFixture.Set(pillar, "fallLength", 0);
            Assert.Throws<InvalidOperationException>(() => pillar.GetData());
        }

        [Test]
        public void EditorCreateDefaultDoesNotBecomeALegacyFallback()
        {
            using var fixture = new CoreAuthoringFixture();
            var created = ObstacleDefinitionDataSO.CreateForAuthoring();
            try
            {
                Assert.That(((ObstacleDefinitionData)created.GetData()).RequiredHits, Is.EqualTo(1));
                Assert.Throws<InvalidOperationException>(() => fixture.Create<ObstacleDefinitionDataSO>().GetData());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(created);
            }
        }

        [Test]
        public void EmptyLayoutCopyClearsPlatformsWithTheirSupportingCliffs()
        {
            using var fixture = new CoreAuthoringFixture();
            var stage = Stage(fixture);
            AuthorPlatform(stage, 2);
            var clone = fixture.Clone(stage);
            var serializer = Type.GetType("ProjectL.Tool.Editor.CombatStageSoAssets, ProjectL.Tool.Editor", throwOnError: true);
            var clear = serializer.GetMethod("ClearPlacementsOutsideLayout", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(clear, Is.Not.Null);
            clear.Invoke(null, new object[] { clone });

            var empty = (CombatStageDetail)clone.GetData();
            Assert.That(empty.MovingPlatforms, Is.Empty);
            Assert.That(empty.StaticTerrainPlacements, Is.Empty);
            Assert.That(((CombatStageDetail)stage.GetData()).MovingPlatforms, Has.Count.EqualTo(1));
        }

        private static CombatStageData Stage(CoreAuthoringFixture fixture)
        {
            var stage = fixture.Create<CombatStageData>();
            var field = fixture.Create<FieldDataSO>();
            CoreAuthoringFixture.Set(field, "fieldId", "field");
            CoreAuthoringFixture.Set(field, "columnCount", 8);
            CoreAuthoringFixture.Set(field, "rowCount", 6);
            CoreAuthoringFixture.Set(stage, "fieldDefinition", field);
            CoreAuthoringFixture.Set(stage, "unitRoster", fixture.Create<UnitRosterDataSO>());
            CoreAuthoringFixture.Set(stage, "economy", fixture.Create<StageEconomyDataSO>());
            CoreAuthoringFixture.Set(stage, "headquarters", fixture.Create<StructureDataSO>());
            CoreAuthoringFixture.Set(stage, "enemyHeadquartersAnchor", new ProjectL.Systems.DataBaseSystem.Data.InGame.Common.CellInput(3, 5));
            using var serialized = new SerializedObject(stage);
            var cells = serialized.FindProperty("staticTerrainPlacements");
            cells.arraySize = 6;
            for (int i = 0; i < cells.arraySize; i++)
            {
                var cell = cells.GetArrayElementAtIndex(i);
                cell.FindPropertyRelative("sourceId").longValue = 11 + i;
                cell.FindPropertyRelative("kind").intValue = (int)StaticTerrainKind.Cliff;
                cell.FindPropertyRelative("cell.x").intValue = 1 + i % 3;
                cell.FindPropertyRelative("cell.y").intValue = i < 3 ? 1 : 3;
                var owners = cell.FindPropertyRelative("knownTo");
                owners.arraySize = 2;
                owners.GetArrayElementAtIndex(0).intValue = (int)Owner.Player;
                owners.GetArrayElementAtIndex(1).intValue = (int)Owner.Enemy;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return stage;
        }

        private static void AuthorPlatform(CombatStageData stage, int length, bool sharedRoute = false,
            Action<int, SerializedProperty> change = null)
        {
            using var serialized = new SerializedObject(stage);
            var platforms = serialized.FindProperty("environment.movingPlatforms");
            Assert.That(platforms, Is.Not.Null);
            platforms.arraySize = sharedRoute ? 2 : 1;
            for (int i = 0; i < platforms.arraySize; i++)
            {
                var row = platforms.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("sourceId").longValue = 100 + i;
                row.FindPropertyRelative("length").intValue = length;
                row.FindPropertyRelative("shapeAxis").intValue = (int)PlatformAxis.Horizontal;
                row.FindPropertyRelative("routeStart.x").intValue = 1;
                row.FindPropertyRelative("routeStart.y").intValue = 1;
                row.FindPropertyRelative("routeEnd.x").intValue = 1;
                row.FindPropertyRelative("routeEnd.y").intValue = 3;
                row.FindPropertyRelative("initialAnchor.x").intValue = 1;
                row.FindPropertyRelative("initialAnchor.y").intValue = i == 0 ? 1 : 3;
                row.FindPropertyRelative("initialSign").intValue = i == 0 ? 1 : -1;
                change?.Invoke(i, row);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static IReadOnlyList<MovingPlatformSpec> Platforms(CombatStageDetail detail) => detail.MovingPlatforms;
    }
}
