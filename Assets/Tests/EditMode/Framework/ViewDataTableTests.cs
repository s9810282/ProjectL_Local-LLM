using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.InputSettings;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.InputSettings.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;
using UnityEngine.InputSystem;
using static ProjectL.Tests.Framework.DataBaseTestAssets;

namespace ProjectL.Tests.Framework
{
    /// <summary>ViewGroup 테이블의 설정형(아이템 1개) 규칙, 목록형 키 규칙, 필수 값 검증을 확인합니다.</summary>
    public sealed class ViewDataTableTests
    {
        private readonly DataBaseTestAssets _assets = new();

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void StageFieldView_ReturnsTheOnlyItem()
        {
            var table = new StageFieldViewTable(_assets.Table<StageFieldViewDataTableSO>(FieldView()));

            Assert.That(table.Config.CellPrefab.name, Is.EqualTo("Cell"));
            Assert.That(table.Config.GhostScale, Is.EqualTo(0.5f));
        }

        [Test]
        public void ConfigTables_RequireExactlyOneItem()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new StageFieldViewTable(_assets.Table<StageFieldViewDataTableSO>()), "StageFieldView 0개");
            Assert.Throws<InvalidOperationException>(() =>
                new StageFieldViewTable(_assets.Table<StageFieldViewDataTableSO>(FieldView(), FieldView())), "StageFieldView 2개");
            Assert.Throws<InvalidOperationException>(() =>
                new StageMapViewTable(_assets.Table<StageMapViewDataTableSO>()), "StageMapView 0개");
            // StageMapView 2개 거부는 프리팹 에셋이 필요해서 DataBaseAssetTests에서 확인한다.
        }

        [Test]
        public void InputSettings_ReturnsTheOnlyItem()
        {
            var actions = _assets.Create<InputActionAsset>();
            var table = new InputSettingsTable(_assets.Table<InputSettingsDataTableSO>(InputSettingsItem(actions)));

            Assert.That(table.Config.Actions, Is.SameAs(actions));
        }

        [Test]
        public void InputSettings_RequiresExactlyOneItem()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new InputSettingsTable(_assets.Table<InputSettingsDataTableSO>()), "InputSettings 0개");
            Assert.Throws<InvalidOperationException>(() =>
                new InputSettingsTable(_assets.Table<InputSettingsDataTableSO>(
                    InputSettingsItem(_assets.Create<InputActionAsset>()),
                    InputSettingsItem(_assets.Create<InputActionAsset>()))), "InputSettings 2개");
        }

        [Test]
        public void InputSettings_RequiresActionAsset()
        {
            var missing = _assets.Create<InputSettingsDataSO>();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                new InputSettingsTable(_assets.Table<InputSettingsDataTableSO>(missing)), "actions 없음");
            // 개수 검사가 아니라 에셋 누락 때문에 실패했는지 구분합니다.
            Assert.That(exception.Message, Does.Contain("Input action asset"));
        }

        [Test]
        public void StageFieldView_RequiresCellPrefabWithRenderer()
        {
            var missing = _assets.Create<StageFieldViewDataSO>();
            Assert.Throws<InvalidOperationException>(() =>
                new StageFieldViewTable(_assets.Table<StageFieldViewDataTableSO>(missing)), "cellPrefab 없음");

            var withoutRenderer = _assets.Create<StageFieldViewDataSO>();
            Set(Get(withoutRenderer, "stageFieldViewData"), "cellPrefab", _assets.CreateGameObject("Empty"));
            Assert.Throws<InvalidOperationException>(() =>
                new StageFieldViewTable(_assets.Table<StageFieldViewDataTableSO>(withoutRenderer)), "Renderer 없음");
        }

        [Test]
        public void StageFieldView_GridOutlineDoesNotRequireCellPrefab()
        {
            var table = new StageFieldViewTable(_assets.Table<StageFieldViewDataTableSO>(OutlineFieldView()));

            Assert.That(table.Config.DisplayMode, Is.EqualTo(FieldDisplayMode.GridOutline));
            Assert.That(table.Config.CellPrefab, Is.Null);
        }

        [Test]
        public void StageFieldView_GridOutlineRequiresDecalAndValidSizes()
        {
            AssertOutlineRejected("gridDecalMaterial", null, "데칼 머티리얼 없음");
            AssertOutlineRejected("outlineCellSpacing", new Vector2(0f, 1f), "칸 간격 0");
            AssertOutlineRejected("gridLineWidth", 1f, "선 두께가 칸 간격 이상");
            AssertOutlineRejected("outlineThickness", 0f, "아웃라인 두께 0");
            AssertOutlineRejected("outlineDashLength", 0f, "점선 길이 0");
            AssertOutlineRejected("outlineDashFill", 0f, "점선 채움 0");
        }

        [Test]
        public void StageNodeVisual_LooksUpByTypeAndRole()
        {
            var table = new StageNodeVisualTable(_assets.Table<StageNodeVisualDataTableSO>(
                NodeVisual(StageType.Combat, StageMapRole.Regular, "Normal Stage A"),
                NodeVisual(StageType.Combat, StageMapRole.Boss, "")));

            Assert.That(table.GetNodeVisual(StageType.Combat, StageMapRole.Regular).DisplayName, Is.EqualTo("Normal Stage A"));
            // displayName이 비면 프리팹 이름을 쓴다.
            Assert.That(table.GetNodeVisual(StageType.Combat, StageMapRole.Boss).DisplayName, Is.EqualTo("Visual_Combat_Boss"));
            Assert.That(table.TryGetNodeVisual(StageType.Shop, StageMapRole.Regular, out _), Is.False);
            Assert.Throws<KeyNotFoundException>(() => table.GetNodeVisual(StageType.Shop, StageMapRole.Regular));
        }

        [Test]
        public void StageNodeVisual_RejectsDuplicateKeyAndMissingPrefab()
        {
            Assert.Throws<InvalidOperationException>(() => new StageNodeVisualTable(_assets.Table<StageNodeVisualDataTableSO>(
                NodeVisual(StageType.Combat, StageMapRole.Boss, "A"),
                NodeVisual(StageType.Combat, StageMapRole.Boss, "B"))), "키 중복");

            var missing = NodeVisual(StageType.Combat, StageMapRole.Regular, "A");
            Set(Get(missing, "stageNodeVisualData"), "visualPrefab", null);
            Assert.Throws<InvalidOperationException>(() =>
                new StageNodeVisualTable(_assets.Table<StageNodeVisualDataTableSO>(missing)), "프리팹 없음");
        }

        [Test]
        public void UnitVisual_LooksUpByEntityKey()
        {
            var table = new UnitVisualTable(_assets.Table<UnitVisualDataTableSO>(
                UnitVisual(EntityKey.Unit_Akimbo, "Akimbo"),
                UnitVisual(EntityKey.Structure_Headquarters, "")));

            Assert.That(table.GetUnitVisual(EntityKey.Unit_Akimbo).DisplayName, Is.EqualTo("Akimbo"));
            Assert.That(table.GetUnitVisual(EntityKey.Structure_Headquarters).DisplayName, Is.EqualTo("Prefab_Structure_Headquarters"));
            Assert.That(table.TryGetUnitVisual(EntityKey.Unit_Sniper, out _), Is.False);
            Assert.Throws<KeyNotFoundException>(() => table.GetUnitVisual(EntityKey.Unit_Sniper));
        }

        [Test]
        public void UnitVisual_RejectsDuplicateNoneAndMissingPrefab()
        {
            Assert.Throws<InvalidOperationException>(() => new UnitVisualTable(_assets.Table<UnitVisualDataTableSO>(
                UnitVisual(EntityKey.Unit_Akimbo, "A"),
                UnitVisual(EntityKey.Unit_Akimbo, "B"))), "키 중복");
            Assert.Throws<InvalidOperationException>(() =>
                new UnitVisualTable(_assets.Table<UnitVisualDataTableSO>(UnitVisual(EntityKey.None, "A"))), "키 None");

            var missing = UnitVisual(EntityKey.Unit_Demon, "Demon");
            Set(Get(missing, "unitVisualData"), "prefab", null);
            Assert.Throws<InvalidOperationException>(() =>
                new UnitVisualTable(_assets.Table<UnitVisualDataTableSO>(missing)), "프리팹 없음");
        }

        private InputSettingsDataSO InputSettingsItem(InputActionAsset actions)
        {
            var settings = _assets.Create<InputSettingsDataSO>();
            Set(Get(settings, "inputSettingsData"), "actions", actions);
            return settings;
        }

        private StageFieldViewDataSO FieldView()
        {
            GameObject cell = _assets.CreateGameObject("Cell");
            cell.AddComponent<MeshRenderer>();
            var fieldView = _assets.Create<StageFieldViewDataSO>();
            Set(Get(fieldView, "stageFieldViewData"), "cellPrefab", cell);
            return fieldView;
        }

        /// <summary>기본값(칸 간격 1, 선 두께 0.05, 아웃라인 두께 0.06)과 데칼 머티리얼만 갖춘 GridOutline 설정입니다.</summary>
        private StageFieldViewDataSO OutlineFieldView()
        {
            var fieldView = _assets.Create<StageFieldViewDataSO>();
            object data = Get(fieldView, "stageFieldViewData");
            Set(data, "fieldDisplayMode", FieldDisplayMode.GridOutline);
            Set(data, "gridDecalMaterial", _assets.CreateMaterial("GridDecal"));
            return fieldView;
        }

        private void AssertOutlineRejected(string field, object value, string because)
        {
            var fieldView = OutlineFieldView();
            Set(Get(fieldView, "stageFieldViewData"), field, value);
            Assert.Throws<InvalidOperationException>(() =>
                new StageFieldViewTable(_assets.Table<StageFieldViewDataTableSO>(fieldView)), because);
        }

        private StageNodeVisualDataSO NodeVisual(StageType stageType, StageMapRole mapRole, string displayName)
        {
            var nodeVisual = _assets.Create<StageNodeVisualDataSO>();
            object data = Get(nodeVisual, "stageNodeVisualData");
            Set(data, "stageType", stageType);
            Set(data, "mapRole", mapRole);
            Set(data, "visualPrefab", _assets.CreateGameObject($"Visual_{stageType}_{mapRole}"));
            Set(data, "displayName", displayName);
            return nodeVisual;
        }

        private UnitVisualDataSO UnitVisual(EntityKey key, string displayName)
        {
            var unitVisual = _assets.Create<UnitVisualDataSO>();
            object data = Get(unitVisual, "unitVisualData");
            Set(data, "key", key);
            Set(data, "prefab", _assets.CreateGameObject($"Prefab_{key}"));
            Set(data, "displayName", displayName);
            return unitVisual;
        }
    }
}
