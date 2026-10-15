using System;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap;
using UnityEditor;
using UnityEngine;
using static ProjectL.Tests.Framework.DataBaseTestAssets;

namespace ProjectL.Tests.Framework
{
    /// <summary>실제 표시 객체의 완료 표시와 기존 외형 Table 연동을 확인합니다.</summary>
    public sealed class StageMapViewDisplayTests
    {
        private readonly DataBaseTestAssets _assets = new();
        private readonly StageMapViewDisplay _display = new();
        private GameObject _parent;

        [TearDown]
        public void TearDown()
        {
            // EditMode에서는 생성한 계층을 먼저 즉시 제거하고 표시 객체의 참조를 해제합니다.
            if (_parent != null) UnityEngine.Object.DestroyImmediate(_parent);
            _display.Clear();
            _assets.DestroyAll();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Show_UsesCoreClearFactWithoutChangingPrefab(bool isClear)
        {
            StageMapViewData data = MapData();
            Transform original = data.NodePrefab.transform.Find(StageMapViewData.ClearedIndicatorName);
            Assert.That(original, Is.Not.Null, "저작된 노드에 완료 표시가 필요합니다.");
            bool originalActive = original.gameObject.activeSelf;
            var node = new StageProgressData(2, 100, 1, 2, Array.Empty<int>(), isClear,
                StageType.Combat, StageMapRole.Regular);

            Show(new[] { node }, data, _ => null);

            Assert.That(_display.TryGetNode(2, out Transform root), Is.True);
            Assert.That(root.Find(StageMapViewData.ClearedIndicatorName).gameObject.activeSelf, Is.EqualTo(isClear));
            Assert.That(root.localPosition, Is.EqualTo(new Vector3(8f, 0f, 4f)));
            Assert.That(original.gameObject.activeSelf, Is.EqualTo(originalActive));
            Assert.That(node.IsClear, Is.EqualTo(isClear));
        }

        [Test]
        public void Show_UsesExistingTableAndKeepsDefaultNodeForMissingVisual()
        {
            var item = _assets.Create<StageNodeVisualDataSO>();
            object visual = Get(item, "stageNodeVisualData");
            Set(visual, "stageType", StageType.Combat);
            Set(visual, "mapRole", StageMapRole.Regular);
            Set(visual, "visualPrefab", _assets.CreateGameObject("CombatVisual"));
            var table = new StageNodeVisualTable(_assets.Table<StageNodeVisualDataTableSO>(item));
            var nodes = new[]
            {
                new StageProgressData(1, 0, 0, 0, Array.Empty<int>()),
                new StageProgressData(2, 100, 1, 0, Array.Empty<int>(), false, StageType.Combat, StageMapRole.Regular),
                new StageProgressData(3, 101, 1, 1, Array.Empty<int>(), false, StageType.Shop, StageMapRole.Regular)
            };

            Show(nodes, MapData(), node => table.TryGetNodeVisual(node.StageType, node.MapRole, out var data)
                ? data.VisualPrefab : null);

            Assert.That(_display.TryGetNode(2, out Transform combat), Is.True);
            Assert.That(combat.Find(StageMapViewData.VisualRootName).childCount, Is.EqualTo(1));
            Assert.That(combat.Find(StageMapViewData.VisualRootName).GetChild(0).name, Is.EqualTo("CombatVisual(Clone)"));
            Assert.That(_display.TryGetNode(3, out Transform shop), Is.True);
            Assert.That(shop.Find(StageMapViewData.VisualRootName).childCount, Is.Zero);
            Assert.That(_display.TryGetNode(1, out Transform start), Is.True);
            Assert.That(start.Find(StageMapViewData.ClearedIndicatorName).gameObject.activeSelf, Is.False);
        }

        [Test]
        public void TryGetNodeId_FindsNodeFromHitTargetAndVisualChild()
        {
            var visualPrefab = _assets.CreateGameObject("CombatVisual");
            new GameObject("Roof").transform.SetParent(visualPrefab.transform, false);
            var nodes = new[]
            {
                new StageProgressData(1, 0, 0, 0, Array.Empty<int>()),
                new StageProgressData(2, 100, 1, 0, Array.Empty<int>(), false, StageType.Combat, StageMapRole.Regular)
            };

            Show(nodes, MapData(), _ => visualPrefab);

            Assert.That(_display.TryGetNode(2, out Transform root), Is.True);
            Transform hitTarget = root.Find(StageMapViewData.HitTargetName);
            Assert.That(hitTarget, Is.Not.Null);
            Assert.That(_display.TryGetNodeId(hitTarget, out int fromHit), Is.True);
            Assert.That(fromHit, Is.EqualTo(2));

            Transform roof = root.Find(StageMapViewData.VisualRootName).GetChild(0).Find("Roof");
            Assert.That(_display.TryGetNodeId(roof, out int fromVisual), Is.True);
            Assert.That(fromVisual, Is.EqualTo(2));

            Assert.That(_display.TryGetNodeId(_parent.transform, out _), Is.False);
            Assert.That(_display.TryGetNodeId(null, out _), Is.False);
        }

        private StageMapViewData MapData()
        {
            var data = new StageMapViewData();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ProjectL/Prefabs/StageSelect/Infrastructure/StageMapNode.prefab");
            Assert.That(prefab, Is.Not.Null);
            Set(data, "nodePrefab", prefab);
            var connection = _assets.CreateGameObject("Connection");
            connection.AddComponent<LineRenderer>();
            Set(data, "connectionPrefab", connection);
            return data;
        }

        private void Show(StageProgressData[] nodes, StageMapViewData data,
            Func<StageProgressData, GameObject> findVisual)
        {
            _parent = new GameObject("MapRoot");
            _display.Show(nodes, data, findVisual, _parent.transform);
        }
    }
}
