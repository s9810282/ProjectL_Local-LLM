using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup;
using UnityEngine;
using static ProjectL.Tests.Framework.DataBaseTestAssets;

namespace ProjectL.Tests.Framework
{
    /// <summary>3D 전용 팝업 View(보드·모델·배경·맵 숨김)를 확인합니다. UI 목록·문구는 ArtifactPopupUITests가 확인합니다.</summary>
    public sealed class ArtifactPopupViewTests
    {
        private readonly List<GameObject> _objects = new();
        private ArtifactPopupView _view;
        private GameObject _background, _map, _model;
        private Transform _board;
        [SetUp]
        public void SetUp()
        {
            var root = Create("Root");
            _board = Create("World", root.transform).transform;
            _background = Create("Background", root.transform);
            _map = Create("Map");
            var prefabs = Create("Prefabs");
            var cell = Create("Cell", prefabs.transform);
            _model = Create("Model", prefabs.transform);
            _view = root.AddComponent<ArtifactPopupView>();
            Set(_view, "worldRoot", _board);
            Set(_view, "backgroundRoot", _background); Set(_view, "stageMapRoot", _map);
            Set(_view, "cellPrefab", cell);
            _view.Configure(new ArtifactPopupConfigData());
        }
        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _objects) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            _objects.Clear();
        }
        [Test]
        public void Render_UsesAbsoluteCellsAndModelRotation()
        {
            _view.Render(Board());
            Assert.That(_view.BorrowedCount, Is.EqualTo(2), "Floor 하나와 배치 모델 하나만 빌립니다. 보유 목록은 UI가 그립니다.");
            Assert.That(_board.childCount, Is.EqualTo(2));
            Assert.That(_board.Find("Cell(Clone)").localPosition, Is.EqualTo(Vector3.zero));
            var placed = _board.Find("Model(Clone)");
            Assert.That(placed, Is.Not.Null);
            Assert.That(Vector3.Distance(placed.localPosition, new Vector3(0, .15f, -.6f)), Is.LessThan(.0001f));
            Assert.That(Quaternion.Angle(placed.localRotation, Quaternion.Euler(0, 90, 0)), Is.LessThan(.001f));
        }
        [Test]
        public void RenderAgain_ReusesModels()
        {
            _view.Render(Board());
            int count = _view.GetComponentsInChildren<Transform>(true).Length;
            _view.Render(Board());
            Assert.That(_view.BorrowedCount, Is.EqualTo(2));
            Assert.That(_view.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(count));
        }
        [Test]
        public void DifferentModelPrefabs_UseSeparatePools()
        {
            var second = Create("SecondModel");
            _view.Render(Board());
            _view.Render(new ArtifactPopupBoard(4, 4,
                new[] { new ArtifactPopupItem(4, 8, new[] { CellCoord.Zero }, null, second) },
                Array.Empty<ArtifactPopupItem>()));
            Assert.That(_board.Find("SecondModel(Clone)"), Is.Not.Null);
            Assert.That(_board.Find("Model(Clone)"), Is.Null);
            _view.Render(Board());
            Assert.That(_board.Find("Model(Clone)"), Is.Not.Null);
        }
        [Test]
        public void Hide_ReturnsAllAndRestoresMap_AndIsRepeatable()
        {
            _view.Show(); _view.Render(Board());
            Assert.That(_map.activeSelf, Is.False);
            _view.Hide(); _view.Hide();
            Assert.That(_view.BorrowedCount, Is.Zero);
            Assert.That(_background.activeSelf, Is.False);
            Assert.That(_board.gameObject.activeSelf, Is.False);
            Assert.That(_board.childCount, Is.Zero);
            Assert.That(_map.activeSelf, Is.True);
        }
        [Test]
        public void HiddenMapBeforeOpen_RemainsHiddenAfterClose()
        {
            _map.SetActive(false); _view.Show(); _view.Hide();
            Assert.That(_map.activeSelf, Is.False);
        }
        [Test]
        public void FailureWhileRendering_ReturnsAlreadyBorrowedModels()
        {
            var broken = new ArtifactPopupBoard(4, 4,
                new[] { new ArtifactPopupItem(3, 7, Array.Empty<CellCoord>(), null, _model) }, Array.Empty<ArtifactPopupItem>());
            Assert.Throws<InvalidOperationException>(() => _view.Render(broken));
            Assert.That(_view.BorrowedCount, Is.Zero);
        }
        [Test]
        public void InvalidSpacing_ThrowsBeforeRendering()
        {
            var config = new ArtifactPopupConfigData(); Set(config, "cellSpacing", 0f);
            Assert.Throws<InvalidOperationException>(() => _view.Configure(config));
        }
        [Test]
        public void Validate_ViewInsideHiddenWorldRoot_Throws()
        {
            var outer = Create("OuterWorld");
            Set(_view, "worldRoot", outer.transform);
            _view.transform.SetParent(outer.transform, false);
            Assert.Throws<InvalidOperationException>(_view.Validate);
            // 정리 전에 테스트용 부모 연결을 해제합니다.
            _view.transform.SetParent(null);
        }
        private ArtifactPopupBoard Board() => new(4, 4,
            new[] { new ArtifactPopupItem(3, 7, new[] { new CellCoord(1, 1), new CellCoord(2, 1) }, null, _model, Rotation4.R90) },
            new[] { new ArtifactPopupItem(5, 1, new[] { CellCoord.Zero }, null) });
        private GameObject Create(string name, Transform parent = null)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            if (parent != null) obj.transform.SetParent(parent, false); else _objects.Add(obj);
            return obj;
        }
    }
}
