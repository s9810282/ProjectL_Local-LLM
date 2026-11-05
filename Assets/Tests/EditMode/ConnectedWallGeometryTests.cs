using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class ConnectedWallGeometryTests
    {
        internal static readonly CellCoord[] LevelWalls =
        {
            new CellCoord(1, 2), new CellCoord(2, 2), new CellCoord(6, 2), new CellCoord(7, 2),
            new CellCoord(2, 3), new CellCoord(7, 3), new CellCoord(2, 5), new CellCoord(7, 5),
            new CellCoord(1, 6), new CellCoord(2, 6), new CellCoord(6, 6), new CellCoord(7, 6)
        };

        [Test]
        public void UserLevel_PreservesTwelveCellsAndFourComponents()
        {
            var walls = LevelWalls.Select((cell, i) => Wall(201 + i, cell)).ToArray();
            var geometry = Build(walls);
            Assert.That(geometry.Runs.Select(run => run.SourceIds), Is.EqualTo(new[]
            {
                new long[] { 201, 202, 205 }, new long[] { 203, 204, 206 },
                new long[] { 207, 209, 210 }, new long[] { 208, 211, 212 }
            }));
            Assert.That(geometry.Runs.Sum(run => run.Parts.Count), Is.EqualTo(12));
            Assert.That(LevelWalls.All(geometry.Occupies), Is.True);
            Assert.That(geometry.Occupies(new CellCoord(1, 3)), Is.False);
            Assert.That(geometry.Occupies(new CellCoord(6, 3)), Is.False);
            Assert.Throws<NotSupportedException>(() => ((IList<WallConvexPart>)geometry.Runs[0].Parts)[0] = default);
        }

        [Test, Combinatorial]
        public void ConnectedShapes_AgreeWithIndependentOutline(
            [Values(0, 1, 2, 3, 4)] int shape, [Values(0, 128, 511)] int cut, [Values(0, 1, 2, 3)] int rotation)
        {
            var cells = Shape(shape).Select(cell => Transform(cell, rotation)).ToArray();
            var facts = cells.Select((cell, i) => Wall(i + 1, cell)).ToArray();
            var run = Build(facts, cut).Runs.Single();
            var reordered = Build(facts.Reverse().ToArray(), cut).Runs.Single();
            var reference = new ConnectedWallOutlineReference(cells, cut);
            var points = Enumerable.Range(0, 25).Select(i => Transform(new CellCoord(i % 5, i / 5), rotation)).ToArray();
            foreach (var a in points)
            foreach (var b in points)
            {
                bool expected = reference.Intersects(a, b, out var expectedEntry);
                bool actual = WallSegmentClipper.IntersectsInterior(a, b, run, out var entry);
                Assert.That(actual, Is.EqualTo(expected), $"shape {shape}, cut {cut}, {a} -> {b}");
                Assert.That(WallSegmentClipper.IntersectsInterior(a, b, reordered, out _), Is.EqualTo(actual));
                if (actual)
                    Assert.That(new ConnectedWallOutlineReference.Rational(entry.Numerator, entry.Denominator).CompareTo(expectedEntry), Is.Zero);
            }
        }

        [Test]
        public void InternalPartitionLine_IsInteriorWhileExteriorLineIsClear()
        {
            // 내부 변 위에 정수 셀 중심 선분을 놓기 위해 합성 형상의 경계를 정한다.
            var parts = new[]
            {
                new WallConvexPart(0, 1024, 0, 4096, 128,
                    WallBoundary.Left | WallBoundary.Bottom | WallBoundary.Top, WallCorner.BottomLeft | WallCorner.TopLeft),
                new WallConvexPart(1024, 2048, 0, 4096, 128,
                    WallBoundary.Right | WallBoundary.Bottom | WallBoundary.Top, WallCorner.BottomRight | WallCorner.TopRight)
            };
            var run = new WallRun(0, 2048, 0, 4096, 128, new long[] { 1, 2 },
                new[] { CellCoord.Zero, CellCoord.Right }, parts);
            Assert.That(WallSegmentClipper.IntersectsInterior(new CellCoord(1, 1), new CellCoord(1, 3), run, out _), Is.True);
            Assert.That(WallSegmentClipper.IntersectsInterior(new CellCoord(1, 2), new CellCoord(1, 2), run, out _), Is.True);
            Assert.That(WallSegmentClipper.IntersectsInterior(new CellCoord(0, 1), new CellCoord(0, 3), run, out _), Is.False);
        }

        [Test]
        public void HollowComponent_UsesFirstEntryAcrossTwoDisjointIntersections()
        {
            var run = Build(Shape(4).Select((cell, i) => Wall(i + 1, cell)).ToArray()).Runs.Single();
            Assert.That(WallSegmentClipper.IntersectsInterior(new CellCoord(0, 2), new CellCoord(4, 2), run, out var entry), Is.True);
            Assert.That(entry.CompareTo(new SegmentParameter(1, 8)), Is.Zero);
            Assert.That(WallSegmentClipper.IntersectsInterior(new CellCoord(2, 2), new CellCoord(2, 2), run, out _), Is.False);
        }

        [TestCase(int.MinValue, 2, int.MaxValue, 2)]
        [TestCase(1, int.MinValue, 1, int.MaxValue)]
        [TestCase(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue)]
        public void BentWall_ExtremeEndpointsAgreeWithOutline(int ax, int ay, int bx, int by)
        {
            var cells = Shape(0);
            var run = Build(cells.Select((cell, i) => Wall(i + 1, cell)).ToArray(), 511).Runs.Single();
            var a = new CellCoord(ax, ay);
            var b = new CellCoord(bx, by);
            var reference = new ConnectedWallOutlineReference(cells, 511);
            Assert.That(WallSegmentClipper.IntersectsInterior(a, b, run, out _), Is.EqualTo(reference.Intersects(a, b, out _)));
            Assert.That(WallSegmentClipper.IntersectsInterior(b, a, run, out _), Is.EqualTo(reference.Intersects(b, a, out _)));
        }

        internal static StaticTerrainFact Wall(long id, CellCoord cell) =>
            new StaticTerrainFact(id, cell, StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy });

        internal static WallGeometry Build(StaticTerrainFact[] facts, int cut = 128) =>
            WallGeometryBuilder.Build(32, 32, new MovementEnvironmentFacts(facts),
                new EnvironmentSemanticBindings(facts.Select(fact => fact.Id).ToArray(), Array.Empty<long>(), Array.Empty<EntityId>()),
                new WallBlockingSpec(cut));

        private static CellCoord Transform(CellCoord cell, int rotation)
        {
            for (int i = 0; i < rotation; i++) cell = new CellCoord(4 - cell.Y, cell.X);
            return new CellCoord(cell.X + 7, cell.Y + 11);
        }

        private static CellCoord[] Shape(int shape)
        {
            if (shape == 0) return new[] { new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(2, 2) };
            if (shape == 1) return new[] { new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1), new CellCoord(2, 2) };
            if (shape == 2) return new[] { new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(1, 2), new CellCoord(2, 2) };
            var ring = Enumerable.Range(0, 9).Select(i => new CellCoord(i % 3 + 1, i / 3 + 1)).Where(cell => cell != new CellCoord(2, 2));
            return shape == 3 ? ring.Where(cell => cell != new CellCoord(2, 3)).ToArray() : ring.ToArray();
        }
    }
}
