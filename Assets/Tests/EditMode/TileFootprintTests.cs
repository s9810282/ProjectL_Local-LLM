using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Tests.Core
{
    public sealed class TileFootprintTests
    {
        [Test]
        public void OneByOne_ReturnsAnchorCell()
        {
            CellCoord[] localCells = new CellCoord[1] { new CellCoord(0, 0) };

            TileFootprint tileFootprint = new TileFootprint(1, 1, localCells);

            CellCoord anchor = new CellCoord(3, 2);

            CellCoord result = tileFootprint.GetOccupiedCell(0, anchor, Rotation4.R0);

            Assert.AreEqual(new CellCoord(3, 2), result);
        }

        [Test]
        public void HorizontalTwoCells_R90_BecomesVerticalTwoCells()
        {
            CellCoord[] localCells = new CellCoord[] { new CellCoord(0, 0), new CellCoord(1, 0) };
            TileFootprint tileFootprint = new TileFootprint(2, 1, localCells);

            CellCoord anchor = new CellCoord(3, 2);

            CellCoord result0 = tileFootprint.GetOccupiedCell(0, anchor, Rotation4.R90);
            CellCoord result1 = tileFootprint.GetOccupiedCell(1, anchor, Rotation4.R90);

            Assert.AreEqual(new CellCoord(3, 3), result0);
            Assert.AreEqual(new CellCoord(3, 2), result1);
        }
    }
}
