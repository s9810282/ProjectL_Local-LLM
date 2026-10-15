using System;
using System.Collections;
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
    public sealed class WallGeometryTests
    {
        private static readonly Owner[] Public = { Owner.Player, Owner.Enemy };

        [TestCase(5, 3, 6, 6, 128, true, TestName = "W01")]
        [TestCase(3, 5, 6, 6, 128, true, TestName = "W02")]
        [TestCase(3, 4, 6, 6, 128, true, TestName = "W03")]
        [TestCase(4, 4, 2, 7, 128, true, TestName = "W04")]
        [TestCase(3, 6, 6, 6, 128, false, TestName = "W05")]
        [TestCase(3, 5, 4, 6, 128, false, TestName = "W06")]
        [TestCase(3, 5, 8, 9, 128, false, TestName = "W07")]
        [TestCase(3, 5, 8, 9, 0, true, TestName = "W08")]
        [TestCase(5, 4, 5, 6, 128, true, TestName = "W09")]
        [TestCase(3, 5, 8, 9, 127, true, TestName = "W07_Cut127")]
        [TestCase(3, 5, 8, 9, 129, false, TestName = "W07_Cut129")]
        [TestCase(3, 4, 8, 7, 128, true, TestName = "ArbitraryDelta5By3")]
        [TestCase(5, 1, 5, 4, 128, false, TestName = "WallBehindEndpoint")]
        [TestCase(3, 5, 3, 5, 128, false, TestName = "SameCellOutsideWall")]
        public void LiteralSegment_HasExpectedInteriorContact(int ax, int ay, int bx, int by, int cut, bool expected)
        {
            var run = LiteralRun(cut);
            var a = new CellCoord(ax, ay);
            var b = new CellCoord(bx, by);
            Assert.That(WallGeometryReference.IntersectsInterior(a, b, run), Is.EqualTo(expected), "reference");
            Assert.That(Blocks(a, b, run), Is.EqualTo(expected), "forward");
            Assert.That(Blocks(b, a, run), Is.EqualTo(expected), "reverse");
        }

        [TestCase(-1)]
        [TestCase(512)]
        [TestCase(int.MaxValue)]
        public void UnsupportedCut_Rejects(int cut) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new WallBlockingSpec(cut));

        [TestCase(0)]
        [TestCase(511)]
        public void SupportedCut_PreservesValue(int cut) =>
            Assert.That(new WallBlockingSpec(cut).CornerCutTicks, Is.EqualTo(cut));

        [Test]
        public void DefaultsAndTerrainValues_AreStable()
        {
            Assert.That(WallBlockingSpec.TicksPerTile, Is.EqualTo(1024));
            Assert.That(WallBlockingSpec.Default.CornerCutTicks, Is.EqualTo(128));
            Assert.That((int)StaticTerrainKind.HighGround, Is.EqualTo(1));
            Assert.That((int)StaticTerrainKind.Sandstorm, Is.EqualTo(2));
            Assert.That((int)StaticTerrainKind.Wall, Is.EqualTo(3));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConnectedRun_MergesCellsAndSortsEvidence(bool vertical)
        {
            var cells = new[] { new CellCoord(4, 5), new CellCoord(5, 5), new CellCoord(6, 5) };
            if (vertical) cells = cells.Select(c => new CellCoord(c.Y, c.X)).ToArray();
            var facts = new[] { Wall(30, cells[2]), Wall(20, cells[0]), Wall(10, cells[1]) };
            var geometry = Build(facts);
            Assert.That(geometry.Runs.Count, Is.EqualTo(1));
            var run = geometry.Runs[0];
            Assert.That(run.SourceIds, Is.EqualTo(new long[] { 10, 20, 30 }));
            Assert.That(run.ComponentKey, Is.EqualTo(10));
            Assert.That(run.Cells, Is.EqualTo(cells));
            Assert.That(run.XMin, Is.EqualTo(vertical ? 4608 : 3584));
            Assert.That(run.XMax, Is.EqualTo(vertical ? 5632 : 6656));
            Assert.That(run.YMin, Is.EqualTo(vertical ? 3584 : 4608));
            Assert.That(run.YMax, Is.EqualTo(vertical ? 6656 : 5632));
            Assert.That(run.CornerCutTicks, Is.EqualTo(128));
            foreach (var cell in cells) Assert.That(geometry.Occupies(cell), Is.True);
            Assert.That(geometry.Occupies(new CellCoord(3, 5)), Is.False);
            var reordered = Build(facts.Reverse().ToArray()).Runs.Single();
            Assert.That(reordered.Cells, Is.EqualTo(run.Cells));
            Assert.That(reordered.SourceIds, Is.EqualTo(run.SourceIds));
        }

        [Test]
        public void DisconnectedRuns_UseMinimumSourceIdOrder()
        {
            var geometry = Build(new[] { Wall(90, new CellCoord(1, 1)), Wall(7, new CellCoord(5, 5)) });
            Assert.That(geometry.Runs.Select(r => r.ComponentKey), Is.EqualTo(new long[] { 7, 90 }));
            Assert.That(geometry.Runs[0].XMin, Is.EqualTo(4608));
            Assert.That(geometry.Runs[0].XMax, Is.EqualTo(5632));
        }

        [Test]
        public void Geometry_CopiesInputsAndExposesReadOnlyEvidence()
        {
            var cells = new[] { new CellCoord(1, 1) };
            var ids = new long[] { 4 };
            var run = new WallRun(512, 1536, 512, 1536, 128, ids, cells);
            var runs = new[] { run };
            var geometry = new WallGeometry(runs);
            ids[0] = 99;
            cells[0] = new CellCoord(9, 9);
            runs[0] = null;
            Assert.That(geometry.Runs.Single(), Is.SameAs(run));
            Assert.That(run.SourceIds.Single(), Is.EqualTo(4));
            Assert.That(geometry.Occupies(new CellCoord(1, 1)), Is.True);
            Assert.That(geometry.Occupies(new CellCoord(9, 9)), Is.False);
            Assert.Throws<NotSupportedException>(() => ((IList<long>)run.SourceIds)[0] = 77);
            Assert.Throws<NotSupportedException>(() => ((IList<CellCoord>)run.Cells)[0] = cells[0]);
            Assert.Throws<NotSupportedException>(() => ((IList<WallRun>)geometry.Runs)[0] = null);
        }

        private static IEnumerable InvalidTopologies()
        {
            yield return new TestCaseData(new[] { new CellCoord(2, 2), new CellCoord(3, 2), new CellCoord(3, 3) }).SetName("BentRun");
            yield return new TestCaseData(new[] { new CellCoord(2, 2), new CellCoord(3, 2), new CellCoord(4, 2), new CellCoord(3, 3) }).SetName("BranchedRun");
            yield return new TestCaseData(new[] { new CellCoord(2, 2), new CellCoord(3, 2), new CellCoord(2, 3), new CellCoord(3, 3) }).SetName("ThickRun");
            yield return new TestCaseData(new[] { new CellCoord(2, 2), new CellCoord(3, 3) }).SetName("DiagonalContact");
            yield return new TestCaseData(new[] { new CellCoord(2, 2), new CellCoord(2, 2) }).SetName("DuplicateWallCell");
        }

        [TestCaseSource(nameof(InvalidTopologies))]
        public void InvalidTopology_RejectsWithSourceEvidence(CellCoord[] cells)
        {
            var facts = cells.Select((c, i) => Wall(100 + i, c)).ToArray();
            var error = Assert.Throws<ArgumentException>(() => Build(facts));
            Assert.That(error.Message, Does.Contain("100").And.Contain("101").And.Contain("(2,2)"));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(10, 0)]
        [TestCase(0, 10)]
        [TestCase(int.MaxValue, int.MinValue)]
        public void OutOfBoundsWall_Rejects(int x, int y) =>
            Assert.Throws<ArgumentException>(() => Build(new[] { Wall(1, new CellCoord(x, y)) }));

        [TestCase(false, true, true)]
        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        [TestCase(true, false, false)]
        public void NonPublicOrInactiveWall_Rejects(bool active, bool player, bool enemy)
        {
            var known = new List<Owner>();
            if (player) known.Add(Owner.Player);
            if (enemy) known.Add(Owner.Enemy);
            var wall = new StaticTerrainFact(1, new CellCoord(1, 1), StaticTerrainKind.Wall, active, known);
            Assert.Throws<ArgumentException>(() => Build(new[] { wall }));
        }

        [Test]
        public void MissingOrObjectBinding_RejectsWall()
        {
            var environment = new MovementEnvironmentFacts(new[] { Wall(1, new CellCoord(1, 1)) });
            Assert.Throws<ArgumentException>(() => WallGeometryBuilder.Build(10, 10, environment,
                EnvironmentSemanticBindings.Empty, WallBlockingSpec.Default));
            Assert.Throws<ArgumentException>(() => WallGeometryBuilder.Build(10, 10, environment,
                new EnvironmentSemanticBindings(Array.Empty<long>(), new long[] { 1 }, Array.Empty<EntityId>()), WallBlockingSpec.Default));
        }

        [Test]
        public void InvalidArguments_RejectAtBuilderBoundary()
        {
            var empty = MovementEnvironmentFacts.Empty;
            var bindings = EnvironmentSemanticBindings.Empty;
            var spec = WallBlockingSpec.Default;
            Assert.Throws<ArgumentOutOfRangeException>(() => WallGeometryBuilder.Build(0, 10, empty, bindings, spec));
            Assert.Throws<ArgumentOutOfRangeException>(() => WallGeometryBuilder.Build(10, -1, empty, bindings, spec));
            Assert.Throws<ArgumentNullException>(() => WallGeometryBuilder.Build(10, 10, null, bindings, spec));
            Assert.Throws<ArgumentNullException>(() => WallGeometryBuilder.Build(10, 10, empty, null, spec));
            Assert.Throws<ArgumentNullException>(() => WallGeometryBuilder.Build(10, 10, empty, bindings, null));
        }

        [Test]
        public void EmptyOrNonWallEnvironment_HasNoGeometry()
        {
            Assert.That(Build(Array.Empty<StaticTerrainFact>()).Runs, Is.Empty);
            var terrain = new StaticTerrainFact(1, new CellCoord(3, 3), StaticTerrainKind.HighGround, true, Public);
            Assert.That(Build(new[] { terrain }).Runs, Is.Empty);
            Assert.That(Build(new[] { terrain }).Occupies(terrain.Cell), Is.False);
        }

        [Test]
        public void InternalSeam_IsNotACutCorner()
        {
            var run = Build(new[] { Wall(1, new CellCoord(4, 5)), Wall(2, new CellCoord(5, 5)) }, 511).Runs;
            Assert.That(run.Count, Is.EqualTo(1));
            Assert.That(Blocks(new CellCoord(4, 6), new CellCoord(5, 5), run[0]), Is.True);
        }

        [Test]
        public void BoundaryAlignedSegment_IsClearButInteriorParallelSegmentBlocks()
        {
            // 셀 중심을 지나는 경계를 가진 합성 형상으로 경계 위의 구간을 직접 검증한다.
            var run = new WallRun(0, 4096, 0, 4096, 128, new long[] { 1 }, new[] { CellCoord.Zero });
            Assert.That(Blocks(new CellCoord(1, 0), new CellCoord(3, 0), run), Is.False);
            Assert.That(Blocks(new CellCoord(1, 1), new CellCoord(3, 1), run), Is.True);
            Assert.That(WallGeometryReference.IntersectsInterior(new CellCoord(1, 0), new CellCoord(3, 0), run), Is.False);
        }

        [Test]
        public void EntryParameter_IsExactAndUsesFiniteSegment()
        {
            var run = LiteralRun(128);
            Assert.That(WallSegmentClipper.IntersectsInterior(new CellCoord(5, 3), new CellCoord(5, 7), run, out var entry), Is.True);
            Assert.That(entry.CompareTo(new SegmentParameter(3, 8)), Is.Zero);
            Assert.That(WallSegmentClipper.IntersectsInterior(new CellCoord(5, 5), new CellCoord(5, 7), run, out entry), Is.True);
            Assert.That(entry.CompareTo(SegmentParameter.Zero), Is.Zero);
            Assert.That(Blocks(new CellCoord(5, 5), new CellCoord(5, 5), run), Is.True);
        }

        [Test]
        public void ParameterComparison_UsesExactProductsBeyondLongRange()
        {
            var a = new SegmentParameter(long.MaxValue - 1, long.MaxValue);
            var b = new SegmentParameter(long.MaxValue - 2, long.MaxValue - 1);
            Assert.That(a.CompareTo(b), Is.GreaterThan(0));
            Assert.That(new SegmentParameter(-1, 3).CompareTo(SegmentParameter.Zero), Is.LessThan(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SegmentParameter(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SegmentParameter(0, -1));
            Assert.Throws<InvalidOperationException>(() => default(SegmentParameter).CompareTo(a));
        }

        [TestCase(int.MinValue, 5, int.MaxValue, 5, true)]
        [TestCase(5, int.MinValue, 5, int.MaxValue, true)]
        [TestCase(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue, true)]
        [TestCase(int.MinValue, int.MaxValue, int.MaxValue, int.MinValue, false)]
        [TestCase(int.MinValue, 0, int.MaxValue, 0, false)]
        public void ExtremeEndpoints_AgreeWithExactReference(int ax, int ay, int bx, int by, bool expected)
        {
            var run = LiteralRun(511);
            var a = new CellCoord(ax, ay);
            var b = new CellCoord(bx, by);
            Assert.That(WallGeometryReference.IntersectsInterior(a, b, run), Is.EqualTo(expected));
            Assert.That(Blocks(a, b, run), Is.EqualTo(expected));
            Assert.That(Blocks(b, a, run), Is.EqualTo(expected));
        }

        [Test]
        public void LargeBoard_OnlyAllocatesFromWallFactsAndScalesBeforeAddingHalfCell()
        {
            var cell = new CellCoord(int.MaxValue - 1, int.MaxValue - 1);
            var geometry = Build(new[] { Wall(1, cell) }, 128, int.MaxValue, int.MaxValue);
            Assert.That(geometry.Runs.Count, Is.EqualTo(1));
            var run = geometry.Runs[0];
            Assert.That(run.XMax, Is.EqualTo((long)cell.X * 1024 + 512));
            Assert.That(run.YMin, Is.EqualTo((long)cell.Y * 1024 - 512));
            Assert.That(Blocks(new CellCoord(int.MinValue, int.MinValue), new CellCoord(int.MaxValue, int.MaxValue), run), Is.True);
        }

        [Test, Combinatorial]
        public void EveryNonWallPair_AgreesWithIndependentPolygonOracle(
            [Values(0, 127, 128, 129, 511)] int cut, [Values(0, 1, 2, 3)] int rotation, [Values(false, true)] bool translated)
        {
            var baseCells = new[] { new CellCoord(1, 2), new CellCoord(2, 2), new CellCoord(3, 2) };
            Func<CellCoord, CellCoord> transform = c => Transform(c, rotation, translated);
            var facts = baseCells.Select((c, i) => Wall(3 - i, transform(c))).ToArray();
            var geometry = Build(facts, cut, 20, 20);
            Assert.That(geometry.Runs.Count, Is.EqualTo(1));
            var run = geometry.Runs[0];
            var reordered = Build(facts.Reverse().ToArray(), cut, 20, 20).Runs.Single();
            Assert.That(run.CornerCutTicks, Is.EqualTo(cut));
            var referenceRun = new WallRun(512, 3584, 1536, 2560, cut, new long[] { 1, 2, 3 }, baseCells);
            var points = SmallBoard().Where(c => !baseCells.Contains(c)).ToArray();
            int comparisons = 0;
            foreach (var a in points)
            foreach (var b in points)
            {
                if (a == b) continue;
                var start = transform(a);
                var end = transform(b);
                bool expected = WallGeometryReference.IntersectsInterior(a, b, referenceRun);
                Assert.That(WallGeometryReference.IntersectsInterior(start, end, run), Is.EqualTo(expected),
                    "rotation and translation must preserve the original polygon result");
                bool actual = Blocks(start, end, run);
                Assert.That(actual, Is.EqualTo(expected), $"({start.X},{start.Y}) -> ({end.X},{end.Y})");
                Assert.That(Blocks(start, end, reordered), Is.EqualTo(actual), "source reorder");
                comparisons++;
            }
            Assert.That(comparisons, Is.EqualTo(462));
        }

        [Test]
        public void IncreasingCornerCut_CannotIntroduceANewBlock()
        {
            var facts = new[] { Wall(1, new CellCoord(1, 2)), Wall(2, new CellCoord(2, 2)), Wall(3, new CellCoord(3, 2)) };
            var cuts = new[] { 0, 64, 127, 128, 129, 256, 511 };
            var runs = cuts.Select(cut => Build(facts, cut).Runs).ToArray();
            foreach (var run in runs) Assert.That(run.Count, Is.EqualTo(1));
            var points = SmallBoard().Where(c => !facts.Any(f => f.Cell == c)).ToArray();
            foreach (var a in points)
            foreach (var b in points)
            {
                if (a == b) continue;
                bool previous = true;
                foreach (var run in runs)
                {
                    bool current = Blocks(a, b, run[0]);
                    Assert.That(previous || !current, Is.True, "shrinking a wall cannot add an intersection");
                    previous = current;
                }
            }
        }

        private static CellCoord Transform(CellCoord cell, int rotation, bool translated)
        {
            for (int i = 0; i < rotation; i++) cell = new CellCoord(4 - cell.Y, cell.X);
            return translated ? new CellCoord(cell.X + 11, cell.Y + 7) : cell;
        }

        private static IEnumerable<CellCoord> SmallBoard()
        {
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 5; x++) yield return new CellCoord(x, y);
        }

        private static WallRun LiteralRun(int cut) => new WallRun(3584, 6656, 4608, 5632, cut,
            new long[] { 1, 2, 3 }, new[] { new CellCoord(4, 5), new CellCoord(5, 5), new CellCoord(6, 5) });

        private static bool Blocks(CellCoord a, CellCoord b, WallRun run) =>
            WallSegmentClipper.IntersectsInterior(a, b, run, out _);

        private static StaticTerrainFact Wall(long id, CellCoord cell) =>
            new StaticTerrainFact(id, cell, StaticTerrainKind.Wall, true, Public);

        private static WallGeometry Build(StaticTerrainFact[] facts, int cut = 128, int width = 10, int height = 10) =>
            WallGeometryBuilder.Build(width, height, new MovementEnvironmentFacts(facts),
                new EnvironmentSemanticBindings(facts.Select(f => f.Id).ToArray(), Array.Empty<long>(), Array.Empty<EntityId>()),
                new WallBlockingSpec(cut));
    }
}
