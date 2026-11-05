using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.CombatField.Editor.Tests
{
    public sealed class AttackOutlineOpeningTests
    {
        [TestCase(GridOutlineSide.Left)]
        [TestCase(GridOutlineSide.Right)]
        [TestCase(GridOutlineSide.Down)]
        [TestCase(GridOutlineSide.Up)]
        public void Rectangle_OpensOnlyChosenSide_WithoutClosingAcrossCenter(GridOutlineSide side)
        {
            foreach (float radius in new[] { 0f, 0.2f })
            {
                foreach (bool eachCell in new[] { false, true })
                {
                    Vector2 inset = new(0.12f, 0.08f);
                    var opening = new GridOutlineOpening(side, side == GridOutlineSide.Right ? 3 : side == GridOutlineSide.Up ? 2 : 0);
                    GridOutlineCreateData data = Data(Rectangle(3, 2), opening, eachCell, inset, radius);
                    MeshGeometryData mesh = MeshHightLevelBuilder.CreateGridOutline(data);
                    Vector3[] probes =
                    {
                        new(-0.5f * data.CellSize.x + inset.x, 0f, 0f),
                        new(2.5f * data.CellSize.x - inset.x, 0f, 0f),
                        new(0f, 0f, -0.5f * data.CellSize.y + inset.y),
                        new(0f, 0f, 1.5f * data.CellSize.y - inset.y)
                    };
                    for (int i = 0; i < probes.Length; i++)
                        Assert.That(Covers(mesh, probes[i]), Is.EqualTo((int)side != i + 1), $"{side}, radius {radius}, EachCell {eachCell}, probe {i}");
                    Assert.That(Covers(mesh, new Vector3(0.2f, 0f, 0.2f)), Is.False, "열린 끝 사이를 가로지르는 선이 없어야 합니다.");
                    AssertMeshIsValid(mesh);
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EachCell_KeepsInternalEdges_WhenOnlyOuterSideOpens(bool rounded)
        {
            Vector2 inset = rounded ? new Vector2(0.1f, 0.1f) : Vector2.zero;
            var opening = new GridOutlineOpening(GridOutlineSide.Down, 0);
            GridOutlineCreateData data = Data(Rectangle(1, 2), opening, true, inset, rounded ? 0.2f : 0f);
            MeshGeometryData mesh = MeshHightLevelBuilder.CreateGridOutline(data);
            Assert.That(Covers(mesh, new Vector3(0f, 0f, -0.5f * data.CellSize.y + inset.y)), Is.False);
            Assert.That(Covers(mesh, new Vector3(0f, 0f, 0.5f * data.CellSize.y + inset.y)), Is.True, "다음 칸의 내부 변은 남습니다.");
            Assert.That(Covers(mesh, new Vector3(0f, 0f, 1.5f * data.CellSize.y - inset.y)), Is.True);
            AssertMeshIsValid(mesh);
        }

        [Test]
        public void HeightPart_UsesWholeRegionBoundary_InsteadOfOpeningItsOwnBottom()
        {
            var cells = new[] { new Vector2Int(0, 1), new Vector2Int(1, 1) };
            GridOutlineCreateData closed = Data(cells, default, false, new Vector2(0.1f, 0.1f), 0.2f);
            GridOutlineCreateData part = closed;
            part.YOffset = 2f;
            part.Opening = new GridOutlineOpening(GridOutlineSide.Down, 0);
            MeshGeometryData expected = MeshHightLevelBuilder.CreateGridOutline(closed);
            MeshGeometryData actual = MeshHightLevelBuilder.CreateGridOutline(part);
            CollectionAssert.AreEqual(expected.Indices, actual.Indices);
        }

        [Test]
        public void HoleAndOtherSides_RemainClosed()
        {
            var cells = new List<Vector2Int>(Rectangle(3, 3));
            cells.Remove(new Vector2Int(1, 1));
            GridOutlineCreateData data = Data(cells.ToArray(), new GridOutlineOpening(GridOutlineSide.Down, 0));
            MeshGeometryData mesh = MeshHightLevelBuilder.CreateGridOutline(data);
            Assert.That(Covers(mesh, new Vector3(data.CellSize.x, 0f, -0.5f * data.CellSize.y)), Is.False);
            Assert.That(Covers(mesh, new Vector3(data.CellSize.x, 0f, 0.5f * data.CellSize.y)), Is.True);
            Assert.That(Covers(mesh, new Vector3(data.CellSize.x, 0f, 1.5f * data.CellSize.y)), Is.True);
            Assert.That(Covers(mesh, new Vector3(data.CellSize.x, 0f, 2.5f * data.CellSize.y)), Is.True);
            AssertMeshIsValid(mesh);
        }

        [Test]
        public void DefaultAreaData_KeepsClosedMesh_AndForwardsExplicitOpening()
        {
            var cells = new[] { Vector2Int.zero };
            var closed = new AreaOutlineData("closed", cells, Vector3.zero, Vector2.one, 0f, 0.05f, Color.white);
            var opening = new GridOutlineOpening(GridOutlineSide.Down, 0);
            var open = new AreaOutlineData("open", cells, Vector3.zero, Vector2.one, 0f, 0.05f, Color.white, opening: opening);
            MeshGeometryData full = MeshHightLevelBuilder.CreateGridOutline(closed.ToMeshData());
            MeshGeometryData cut = MeshHightLevelBuilder.CreateGridOutline(open.ToMeshData());
            Assert.That(closed.Opening.IsEnabled, Is.False);
            Assert.That(open.ToMeshData().Opening, Is.EqualTo(opening));
            Assert.That(full.Indices.Count, Is.EqualTo(24));
            Assert.That(cut.Indices.Count, Is.EqualTo(18));
            Assert.That(Covers(cut, Vector3.zero), Is.False);
        }

        [TestCase(0, 2, GridOutlineSide.Left, 2)]
        [TestCase(5, 2, GridOutlineSide.Right, 4)]
        [TestCase(2, 0, GridOutlineSide.Down, 2)]
        [TestCase(2, 5, GridOutlineSide.Up, 4)]
        public void Field_SelectsActorSide_AndChangesWhenOnlyActorMoves(int x, int y, GridOutlineSide side, int line)
        {
            var state = new FieldHighlightState();
            state.Reset(8, 8);
            var cells = new[] { new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(2, 3), new Vector2Int(3, 3) };
            SetCells(state, FieldHighlightLayer.Affected, cells);
            state.SetLayerCells(FieldHighlightLayer.ActiveActor, new[] { new CellCoord(2, 2) });
            Assert.That(FieldAttackOutlineOpening.Of(state, FieldHighlightLayer.Affected, Vector2.one).Side, Is.EqualTo(GridOutlineSide.None));
            state.SetLayerCells(FieldHighlightLayer.ActiveActor, new[] { new CellCoord(x, y) });
            GridOutlineOpening actual = FieldAttackOutlineOpening.Of(state, FieldHighlightLayer.Affected, Vector2.one);
            Assert.That(actual.Side, Is.EqualTo(side));
            Assert.That(actual.GridLine, Is.EqualTo(line));
            Assert.That(actual.ClearArea, Is.EqualTo(new RectInt(x, y, 1, 1)));
            SetCells(state, FieldHighlightLayer.HazardRange, cells);
            Assert.That(FieldAttackOutlineOpening.Of(state, FieldHighlightLayer.HazardRange, Vector2.one).IsEnabled, Is.False);
        }

        [Test]
        public void Field_NoActorOrSurroundingRange_KeepsClosed_DiagonalUsesWorldDistance()
        {
            var state = new FieldHighlightState();
            state.Reset(8, 8);
            Vector2Int[] cells = Rectangle(3, 3);
            SetCells(state, FieldHighlightLayer.AttackRange, cells);
            Assert.That(FieldAttackOutlineOpening.Of(state, FieldHighlightLayer.AttackRange, Vector2.one).IsEnabled, Is.False);
            state.SetLayerCells(FieldHighlightLayer.ActiveActor, new[] { new CellCoord(1, 1) });
            GridOutlineOpening inside = FieldAttackOutlineOpening.Of(state, FieldHighlightLayer.AttackRange, Vector2.one);
            Assert.That(inside.Side, Is.EqualTo(GridOutlineSide.None));
            Assert.That(inside.ClearArea, Is.EqualTo(new RectInt(1, 1, 1, 1)));
            state.SetLayerCells(FieldHighlightLayer.ActiveActor, new[] { new CellCoord(5, 5) });
            Assert.That(FieldAttackOutlineOpening.Of(state, FieldHighlightLayer.AttackRange, new Vector2(0.5f, 2f)).Side,
                Is.EqualTo(GridOutlineSide.Up));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CenterActor_ClearsAdjacentInnerEdges_AndItsOwnOutline(bool eachCell)
        {
            var ring = new List<Vector2Int>(Rectangle(3, 3));
            ring.Remove(new Vector2Int(1, 1));
            var opening = new GridOutlineOpening(GridOutlineSide.None, 0, new RectInt(1, 1, 1, 1));
            GridOutlineCreateData data = Data(ring.ToArray(), opening, eachCell, new Vector2(0.06f, 0.06f), 0.1f);
            MeshGeometryData mesh = MeshHightLevelBuilder.CreateGridOutline(data);
            Assert.That(Covers(mesh, new Vector3(data.CellSize.x, 0f, 0.5f * data.CellSize.y - 0.06f)), Is.False);
            Assert.That(Covers(mesh, new Vector3(data.CellSize.x, 0f, 1.5f * data.CellSize.y + 0.06f)), Is.False);
            Assert.That(Covers(mesh, new Vector3(0.5f * data.CellSize.x - 0.06f, 0f, data.CellSize.y)), Is.False);
            Assert.That(Covers(mesh, new Vector3(1.5f * data.CellSize.x + 0.06f, 0f, data.CellSize.y)), Is.False);
            Assert.That(Covers(mesh, new Vector3(data.CellSize.x, 0f, -0.5f * data.CellSize.y + 0.06f)), Is.True);
            MeshGeometryData actor = MeshHightLevelBuilder.CreateGridOutline(Data(new[] { new Vector2Int(1, 1) }, opening, eachCell));
            Assert.That(actor.Indices.Count, Is.Zero);
            AssertMeshIsValid(mesh);
        }

        [Test]
        public void HoverRange_UsesItsOwnSource_AndReleasesItWithFieldState()
        {
            var state = new FieldHighlightState();
            state.Reset(8, 8);
            state.SetLayerCells(FieldHighlightLayer.ActiveActor, new[] { new CellCoord(0, 0) });
            state.SetLayerCells(FieldHighlightLayer.AllyHoverRange, new[] { new CellCoord(4, 3), new CellCoord(4, 5) });
            state.SetHoverRangeSourceCells(new[] { new CellCoord(4, 4) });
            state.SetHover(new CellCoord(4, 4), ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction.CombatHoverState.Neutral);
            GridOutlineOpening range = FieldAttackOutlineOpening.Of(state, FieldHighlightLayer.AllyHoverRange, Vector2.one);
            Assert.That(range.ClearArea, Is.EqualTo(new RectInt(4, 4, 1, 1)));
            Assert.That(FieldAttackOutlineOpening.Of(state, FieldHighlightLayer.ActiveActor, Vector2.one).IsEnabled, Is.False,
                "다른 유닛을 호버했다고 현재 선택한 유닛의 표지를 지우면 안 됩니다.");
            Assert.That(FieldAttackOutlineOpening.ForHover(state, FieldHighlightLayer.None, Vector2.one), Is.EqualTo(range));
            state.SetHoverRangeSourceCells(System.Array.Empty<CellCoord>());
            Assert.That(FieldAttackOutlineOpening.Of(state, FieldHighlightLayer.AllyHoverRange, Vector2.one).IsEnabled, Is.False);
            state.SetHoverRangeSourceCells(new[] { new CellCoord(4, 4) });
            state.Clear();
            Assert.That(state.HoverRangeSourceCells, Is.Empty);
        }

        private static void SetCells(FieldHighlightState state, FieldHighlightLayer layer, Vector2Int[] cells)
        {
            var coords = new CellCoord[cells.Length];
            for (int i = 0; i < cells.Length; i++)
                coords[i] = new CellCoord(cells[i].x, cells[i].y);
            state.SetLayerCells(layer, coords);
        }

        private static GridOutlineCreateData Data(Vector2Int[] cells, GridOutlineOpening opening, bool eachCell = false,
            Vector2 inset = default, float radius = 0f) =>
            new("test", cells, Vector3.zero, new Vector2(1.5f, 0.8f), 0f, 0.03f, Color.white,
                dashLength: 0.3f, cornerRadius: radius, loopInset: inset, eachCell: eachCell, opening: opening);

        private static Vector2Int[] Rectangle(int width, int height)
        {
            var cells = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    cells.Add(new Vector2Int(x, y));
            return cells.ToArray();
        }

        // 생성한 삼각형에 실제 점이 들어가는지 대조하여, 다른 변·구멍·중앙을 잘못 잇지 않았는지 봅니다.
        private static bool Covers(MeshGeometryData mesh, Vector3 point)
        {
            for (int i = 0; i < mesh.Indices.Count; i += 3)
            {
                Vector3 a = mesh.Vertices[mesh.Indices[i]];
                Vector3 b = mesh.Vertices[mesh.Indices[i + 1]];
                Vector3 c = mesh.Vertices[mesh.Indices[i + 2]];
                float area = Cross(b - a, c - a);
                if (Mathf.Abs(area) < 0.00000001f)
                    continue;
                float u = Cross(b - point, c - point) / area;
                float v = Cross(c - point, a - point) / area;
                float w = 1f - u - v;
                if (u >= -0.00001f && v >= -0.00001f && w >= -0.00001f)
                    return true;
            }
            return false;
        }

        private static float Cross(Vector3 a, Vector3 b) => a.x * b.z - a.z * b.x;

        private static void AssertMeshIsValid(MeshGeometryData mesh)
        {
            Assert.That(mesh.Indices.Count % 3, Is.Zero);
            Assert.That(mesh.UVs.Count, Is.EqualTo(mesh.Vertices.Count));
            foreach (int index in mesh.Indices)
                Assert.That(index, Is.InRange(0, mesh.Vertices.Count - 1));
            foreach (Vector3 vertex in mesh.Vertices)
                Assert.That(float.IsFinite(vertex.x) && float.IsFinite(vertex.y) && float.IsFinite(vertex.z), Is.True);
            foreach (Vector2 uv in mesh.UVs)
                Assert.That(float.IsFinite(uv.x) && float.IsFinite(uv.y), Is.True);
        }
    }
}
