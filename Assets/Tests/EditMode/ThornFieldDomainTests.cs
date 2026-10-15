using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Tests.Core
{
    public sealed class ThornFieldDomainTests
    {

        [TestCaseSource(nameof(MovementCases))]
        public void ResolvePathCells_UsesDiagonalFirstOrder_AndDeduplicatesFootprints(
            CellCoord checkpointAnchor,
            CellCoord committedAnchor,
            TileFootprint footprint,
            Rotation4 rotation,
            CellCoord[] expected)
        {
            CellCoord[] actual = MovementPathCalculator.ResolvePathCells(
                checkpointAnchor,
                committedAnchor,
                footprint,
                rotation);

            Assert.That(actual, Is.EqualTo(expected));
        }

        private static IEnumerable<TestCaseData> MovementCases()
        {
            var single = new TileFootprint(
                1,
                1,
                new[] { CellCoord.Zero });

            yield return new TestCaseData(
                    CellCoord.Zero,
                    CellCoord.Zero,
                    single,
                    Rotation4.R0,
                    Array.Empty<CellCoord>())
                .SetName("Equal anchors return no traversed cells");

            yield return new TestCaseData(
                    CellCoord.Zero,
                    new CellCoord(0, 3),
                    single,
                    Rotation4.R0,
                    new[]
                    {
                        new CellCoord(0, 1),
                        new CellCoord(0, 2),
                        new CellCoord(0, 3),
                    })
                .SetName("Straight movement includes each later anchor");

            yield return new TestCaseData(
                    CellCoord.Zero,
                    new CellCoord(2, 1),
                    single,
                    Rotation4.R0,
                    new[]
                    {
                        new CellCoord(1, 1),
                        new CellCoord(2, 1),
                    })
                .SetName("Diagonal movement advances both axes first");

            yield return new TestCaseData(
                    CellCoord.Zero,
                    new CellCoord(1, 2),
                    single,
                    Rotation4.R0,
                    new[]
                    {
                        new CellCoord(1, 1),
                        new CellCoord(1, 2),
                    })
                .SetName("Swapped deltas retain diagonal-first order");

            yield return new TestCaseData(
                    new CellCoord(3, 3),
                    new CellCoord(0, 1),
                    single,
                    Rotation4.R0,
                    new[]
                    {
                        new CellCoord(2, 2),
                        new CellCoord(1, 1),
                        new CellCoord(0, 1),
                    })
                .SetName("Reversed coordinates advance toward the destination");

            var horizontalTwoCell = new TileFootprint(
                2,
                1,
                new[] { CellCoord.Zero, CellCoord.Right });
            yield return new TestCaseData(
                    CellCoord.Zero,
                    new CellCoord(0, 2),
                    horizontalTwoCell,
                    Rotation4.R90,
                    new[]
                    {
                        new CellCoord(0, 2),
                        new CellCoord(0, 3),
                    })
                .SetName("Rotated footprint excludes start and repeated overlap cells");
        }
    }
}
