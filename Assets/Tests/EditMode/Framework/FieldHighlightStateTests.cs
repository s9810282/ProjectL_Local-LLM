using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;

namespace ProjectL.Tests.Framework
{
    /// <summary>필드 강조 상태가 바뀐 칸만 알려 주는지, 정적 구역·호버·층 규칙이 기존 셀 표시와 같은지 확인합니다(격자 아웃라인 설계 F2·F3).</summary>
    public sealed class FieldHighlightStateTests
    {
        private FieldHighlightState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new FieldHighlightState();
            _state.Reset(3, 4);
        }

        [Test]
        public void FieldViewAndRenderers_ExposeOnlyTheReadContract()
        {
            PropertyInfo exposed = typeof(FieldView).GetProperty("HighlightState", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(exposed.PropertyType, Is.EqualTo(typeof(IFieldHighlightStateReader)), "바꾸는 쪽은 FieldView뿐이다(D4).");
            Assert.That(typeof(IFieldHighlightRenderer).GetMethod(nameof(IFieldHighlightRenderer.Build)).GetParameters()[1].ParameterType,
                Is.EqualTo(typeof(IFieldHighlightStateReader)));
            Assert.That(typeof(IFieldHighlightStateReader).GetMethods().Any(m => m.Name.StartsWith("Set") || m.Name == "Clear" || m.Name == "Reset"),
                Is.False);
        }

        [Test]
        public void SetLayerCells_ReportsOnlyAddedOrRemovedCells()
        {
            Assert.That(_state.SetLayerCells(FieldHighlightLayer.Range, new[] { new CellCoord(0, 0), new CellCoord(1, 0) }).ToArray(),
                Is.EqualTo(new[] { 0, 1 }));

            int[] changed = _state.SetLayerCells(FieldHighlightLayer.Range,
                new[] { new CellCoord(1, 0), new CellCoord(2, 0), new CellCoord(9, 9), new CellCoord(2, 0) }).ToArray();
            Assert.That(changed, Is.EqualTo(new[] { 0, 2 }), "유지된 칸·격자 밖·중복은 다시 그리지 않는다.");
            Assert.That(_state.LayersAt(1), Is.EqualTo(FieldHighlightLayer.Range));

            _state.SetLayerCells(FieldHighlightLayer.Affected, new[] { new CellCoord(1, 0) });
            Assert.That(_state.ClearLayer(FieldHighlightLayer.Range).ToArray(), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(_state.LayersAt(1), Is.EqualTo(FieldHighlightLayer.Affected), "다른 층은 남는다.");
            Assert.That(_state.ClearLayer(FieldHighlightLayer.Range), Is.Empty);
        }

        [Test]
        public void SetHover_ReportsPreviousAndNewCell_AndClearsOutsideGrid()
        {
            Assert.That(_state.SetHover(new CellCoord(1, 1), CombatHoverState.Valid).ToArray(), Is.EqualTo(new[] { 4 }));
            Assert.That(_state.SetHover(new CellCoord(1, 1), CombatHoverState.Valid), Is.Empty);
            Assert.That(_state.SetHover(new CellCoord(1, 1), CombatHoverState.Invalid).ToArray(), Is.EqualTo(new[] { 4 }));
            Assert.That(_state.SetHover(new CellCoord(2, 1), CombatHoverState.Neutral).ToArray(), Is.EqualTo(new[] { 4, 5 }));

            Assert.That(_state.SetHover(new CellCoord(-1, 0), CombatHoverState.Valid).ToArray(), Is.EqualTo(new[] { 5 }));
            Assert.That(_state.HoverIndex, Is.EqualTo(-1));
            Assert.That(_state.HoverState, Is.EqualTo(CombatHoverState.Neutral));
            Assert.That(_state.ClearHover(), Is.Empty);
        }

        [Test]
        public void SetStaticZones_RailWinsOverZones_AndReportsOnlyChangedCells()
        {
            int[] first = _state.SetStaticZones(1, 1, new[] { new CellCoord(0, 0), new CellCoord(1, 2) }).ToArray();

            Assert.That(_state.ZoneAt(0), Is.EqualTo(FieldStaticZone.Rail), "레일이 아군 구역보다 우선한다.");
            Assert.That(_state.ZoneAt(1), Is.EqualTo(FieldStaticZone.PlayerZone));
            Assert.That(_state.ZoneAt(7), Is.EqualTo(FieldStaticZone.Rail));
            Assert.That(_state.ZoneAt(4), Is.EqualTo(FieldStaticZone.None));
            Assert.That(_state.ZoneAt(10), Is.EqualTo(FieldStaticZone.EnemyZone));
            Assert.That(first, Is.EqualTo(new[] { 0, 1, 2, 7, 9, 10, 11 }));

            Assert.That(_state.SetStaticZones(1, 1, new[] { new CellCoord(0, 0), new CellCoord(1, 2) }), Is.Empty);
            Assert.That(_state.SetStaticZones(1, 1, new[] { new CellCoord(1, 2) }).ToArray(), Is.EqualTo(new[] { 0 }));
            Assert.That(_state.ZoneAt(0), Is.EqualTo(FieldStaticZone.PlayerZone));
        }

        [Test]
        public void IsActing_OnlyForActionLayersOrPlacement()
        {
            _state.SetLayerCells(FieldHighlightLayer.ActiveActor, new[] { new CellCoord(0, 0) });
            _state.SetHover(new CellCoord(1, 1), CombatHoverState.Neutral);
            Assert.That(_state.IsActing, Is.False, "현재 행위자·호버만으로는 행동 중이 아니다.");

            foreach (var layer in new[] { FieldHighlightLayer.Range, FieldHighlightLayer.Affected,
                         FieldHighlightLayer.MovementDraft, FieldHighlightLayer.BlockedWall })
            {
                _state.SetLayerCells(layer, new[] { new CellCoord(2, 2) });
                Assert.That(_state.IsActing, Is.True, layer.ToString());
                _state.ClearLayer(layer);
                Assert.That(_state.IsActing, Is.False, layer.ToString());
            }

            _state.SetPlacementActive(true);
            Assert.That(_state.IsActing, Is.True, "배치 드래그 중");
            _state.SetPlacementActive(false);

            Assert.That(_state.SetPathPlanning(true), Is.True);
            Assert.That(_state.IsActing, Is.True, "Q 경로 지정 중");
            Assert.That(_state.SetPathPlanning(true), Is.False);
            _state.Reset(3, 4);
            Assert.That(_state.IsPathPlanning, Is.False);
        }

        [Test]
        public void ResetAndClear_DropEverything()
        {
            _state.SetLayerCells(FieldHighlightLayer.Range, new[] { new CellCoord(0, 0) });
            _state.SetStaticZones(1, 0, new CellCoord[0]);
            _state.SetHover(new CellCoord(0, 0), CombatHoverState.Valid);
            Assert.That(_state.SetPlacementActive(true), Is.True);
            Assert.That(_state.SetPlacementActive(true), Is.False);

            _state.Reset(2, 2);
            Assert.That(_state.CellCount, Is.EqualTo(4));
            Assert.That(_state.LayersAt(0), Is.EqualTo(FieldHighlightLayer.None));
            Assert.That(_state.ZoneAt(0), Is.EqualTo(FieldStaticZone.None));
            Assert.That(_state.HoverIndex, Is.EqualTo(-1));
            Assert.That(_state.IsPlacementActive, Is.False);

            _state.Clear();
            Assert.That(_state.CellCount, Is.EqualTo(0));
            Assert.That(_state.TryGetIndex(new CellCoord(0, 0), out _), Is.False);
            Assert.That(_state.ClearLayer(FieldHighlightLayer.Range), Is.Empty);
        }
    }
}
