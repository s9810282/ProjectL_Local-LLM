using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using UnityEngine;
using static ProjectL.Tests.Framework.DataBaseTestAssets;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// GridOutline 표시기가 평소에는 현재 행위자 테두리만, 행동 중에는 격자 데칼·층·호버·배치 구역 테두리를 그리는지 확인합니다
    /// (격자 아웃라인 설계 U3·U4·F5·F6).
    /// </summary>
    public sealed class FieldOutlineRendererTests
    {
        private static readonly Color Range = Color.cyan;
        private static readonly Color HoverValid = Color.green;
        private static readonly Color PlayerZone = Color.blue;
        private static readonly Color ActiveActor = Color.yellow;

        private readonly DataBaseTestAssets _assets = new();
        private CombatViewSystemTests.FakeAreaOutlines _outlines;
        private FieldHighlightState _state;
        private FieldOutlineRenderer _renderer;
        private Transform _root;

        [SetUp]
        public void SetUp()
        {
            var fieldView = _assets.Create<StageFieldViewDataSO>();
            var config = (StageFieldViewData)Get(fieldView, "stageFieldViewData");
            Set(config, "fieldDisplayMode", FieldDisplayMode.GridOutline);
            Set(config, "gridDecalMaterial", _assets.CreateMaterial("GridDecal"));

            _root = _assets.CreateGameObject("CellsRoot").transform;
            _outlines = new CombatViewSystemTests.FakeAreaOutlines();
            var palette = new FieldHighlightPalette(PlayerZone, Color.red, Color.gray, Color.white, HoverValid, Color.magenta,
                new Color(1f, 0.5f, 0f), Color.black, Range, ActiveActor);
            _renderer = new FieldOutlineRenderer(_root, config, _outlines, palette);

            _state = new FieldHighlightState();
            _state.Reset(3, 3);
            _renderer.Build(new FieldGridMapper(_root, 3, 3, 1f, 1f), _state);
        }

        [TearDown]
        public void TearDown()
        {
            _renderer.Dispose();
            _assets.DestroyAll();
        }

        [Test]
        public void Build_CreatesOneHandlePerSlot_AndStartsEmptyWithoutGrid()
        {
            Assert.That(_outlines.Handles, Has.Count.EqualTo(7));
            Assert.That(_outlines.Handles.All(h => h.Last.HasValue && h.Last.Value.Cells.Length == 0), Is.True);
            Assert.That(IsGridShown(), Is.False);
        }

        [Test]
        public void Idle_ShowsOnlyActiveActor()
        {
            _renderer.Repaint(_state.SetLayerCells(FieldHighlightLayer.ActiveActor, new[] { new CellCoord(0, 0) }));
            _renderer.Repaint(_state.SetHover(new CellCoord(1, 1), CombatHoverState.Valid));

            Assert.That(Cells("ActiveActor"), Is.EqualTo(new[] { new Vector2Int(0, 0) }));
            Assert.That(Slot("ActiveActor").Color, Is.EqualTo(ActiveActor));
            Assert.That(Cells("Hover"), Is.Empty, "행동 중이 아니면 호버 테두리를 그리지 않는다.");
            Assert.That(IsGridShown(), Is.False);
        }

        [Test]
        public void Acting_ShowsGridLayersAndHover_ThenHidesThemAgain()
        {
            _renderer.Repaint(_state.SetHover(new CellCoord(1, 1), CombatHoverState.Valid));
            _renderer.Repaint(_state.SetLayerCells(FieldHighlightLayer.Range, new[] { new CellCoord(0, 1), new CellCoord(1, 1) }));

            Assert.That(IsGridShown(), Is.True);
            Assert.That(Cells("Range"), Is.EquivalentTo(new[] { new Vector2Int(0, 1), new Vector2Int(1, 1) }));
            Assert.That(Slot("Range").Color, Is.EqualTo(Range));
            Assert.That(Slot("Range").IsDashed, Is.True);
            Assert.That(Slot("Range").DashLength, Is.EqualTo(0.25f), "View DB 기본 점선 길이");
            Assert.That(Slot("Range").DashFill, Is.EqualTo(0.65f));
            Assert.That(Cells("Hover"), Is.EqualTo(new[] { new Vector2Int(1, 1) }));
            Assert.That(Slot("Hover").Color, Is.EqualTo(HoverValid));
            Assert.That(Slot("Hover").YOffset, Is.GreaterThan(Slot("Range").YOffset), "높은 층이 위에 그려진다.");

            _renderer.Repaint(_state.ClearLayer(FieldHighlightLayer.Range));
            Assert.That(IsGridShown(), Is.False);
            Assert.That(Cells("Range"), Is.Empty);
            Assert.That(Cells("Hover"), Is.Empty);
        }

        [Test]
        public void PlacementDrag_ShowsPlayerZoneAndGrid()
        {
            _renderer.Repaint(_state.SetStaticZones(1, 1, new[] { new CellCoord(2, 0) }));
            Assert.That(Cells("PlayerZone"), Is.Empty);

            _state.SetPlacementActive(true);
            _renderer.Refresh();
            Assert.That(IsGridShown(), Is.True);
            Assert.That(Cells("PlayerZone"), Is.EquivalentTo(new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }),
                "레일 칸은 배치 구역 테두리에 넣지 않는다.");
            Assert.That(Slot("PlayerZone").Color, Is.EqualTo(PlayerZone));

            _state.SetPlacementActive(false);
            _renderer.Refresh();
            Assert.That(IsGridShown(), Is.False);
            Assert.That(Cells("PlayerZone"), Is.Empty);
        }

        [Test]
        public void PathPlanning_ShowsGridAndHover_WithoutPlayerZone()
        {
            _renderer.Repaint(_state.SetStaticZones(1, 1, new CellCoord[0]));
            _renderer.Repaint(_state.SetHover(new CellCoord(1, 1), CombatHoverState.Neutral));

            _state.SetPathPlanning(true);
            _renderer.Refresh();
            Assert.That(IsGridShown(), Is.True);
            Assert.That(Cells("Hover"), Is.EqualTo(new[] { new Vector2Int(1, 1) }));
            Assert.That(Cells("PlayerZone"), Is.Empty, "배치 구역은 배치 드래그 때만 그린다.");

            _state.SetPathPlanning(false);
            _renderer.Refresh();
            Assert.That(IsGridShown(), Is.False);
        }

        [Test]
        public void SameCells_DoNotRebuildMesh_AndReleaseReturnsHandles()
        {
            _renderer.Repaint(_state.SetLayerCells(FieldHighlightLayer.ActiveActor, new[] { new CellCoord(0, 0) }));
            int updates = Handle("ActiveActor").Updates;
            _renderer.Repaint(_state.SetHover(new CellCoord(2, 2), CombatHoverState.Neutral));
            Assert.That(Handle("ActiveActor").Updates, Is.EqualTo(updates));

            _renderer.Release();
            Assert.That(_outlines.Handles.All(h => h.IsDisposed), Is.True);
        }

        [Test]
        public void Release_WhenOneHandleFails_ReturnsOthersResetsStateAndThrowsOnce()
        {
            _renderer.Repaint(_state.SetLayerCells(FieldHighlightLayer.Range, new[] { new CellCoord(0, 0) }));
            Assert.That(IsGridShown(), Is.True);
            _outlines.Handles[1].ThrowOnDispose = true;

            var error = Assert.Throws<AggregateException>(() => _renderer.Release());
            Assert.That(error.InnerExceptions, Has.Count.EqualTo(1));
            Assert.That(_outlines.Handles.Where((h, i) => i != 1).All(h => h.IsDisposed), Is.True,
                "실패한 핸들 뒤의 핸들도 돌려준다.");
            Assert.That(_outlines.Handles.All(h => h.DisposeCalls == 1), Is.True);
            Assert.That(IsGridShown(), Is.False, "핸들 실패와 관계없이 데칼을 숨긴다.");

            // 상태를 비웠으므로 다시 해제해도 같은 핸들을 두 번 돌려주지 않고, 다시 구성할 수 있다.
            Assert.DoesNotThrow(() => _renderer.Release());
            Assert.That(_outlines.Handles.All(h => h.DisposeCalls == 1), Is.True);
            _renderer.Refresh();
            _renderer.Build(new FieldGridMapper(_root, 3, 3, 1f, 1f), _state);
            Assert.That(_outlines.Handles, Has.Count.EqualTo(14));
        }

        private bool IsGridShown()
        {
            Transform decal = _root.Find("FieldGridDecal");
            return decal != null && decal.gameObject.activeSelf;
        }

        private CombatViewSystemTests.FakeAreaOutlineHandle Handle(string slot)
        {
            return _outlines.Handles.Single(h => h.Last.HasValue && h.Last.Value.Name == $"FieldOutline_{slot}");
        }

        private AreaOutlineData Slot(string slot) => Handle(slot).Last.Value;

        private Vector2Int[] Cells(string slot) => Slot(slot).Cells;
    }
}
