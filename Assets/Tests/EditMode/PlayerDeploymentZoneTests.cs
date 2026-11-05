using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    public sealed class PlayerDeploymentZoneTests
    {
        [Test]
        public void Rows_ContainsOnlyCellsBelowDepth()
        {
            var zone = PlayerDeploymentZone.Rows(2);
            Assert.That(zone.Contains(new CellCoord(0, 0)), Is.True);
            Assert.That(zone.Contains(new CellCoord(5, 1)), Is.True);
            Assert.That(zone.Contains(new CellCoord(0, 2)), Is.False);
            Assert.That(zone.Contains(new CellCoord(0, -1)), Is.False);
            Assert.That(zone.RowDepth, Is.EqualTo(2));
            Assert.That(zone.ListedCells, Is.Empty);
        }

        [Test]
        public void Of_SortsCellsRowFirst_AndContainsOnlyListedCells()
        {
            var zone = PlayerDeploymentZone.Of(new[] { new CellCoord(2, 1), new CellCoord(0, 1), new CellCoord(3, 0) });
            Assert.That(zone.ListedCells, Is.EqualTo(new[] { new CellCoord(3, 0), new CellCoord(0, 1), new CellCoord(2, 1) }));
            Assert.That(zone.Contains(new CellCoord(0, 1)), Is.True);
            Assert.That(zone.Contains(new CellCoord(1, 1)), Is.False);
            Assert.That(zone.RowDepth, Is.Null);
        }

        [Test]
        public void InvalidInput_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PlayerDeploymentZone.Rows(-1));
            Assert.Throws<ArgumentNullException>(() => PlayerDeploymentZone.Of(null));
            var error = Assert.Throws<ArgumentException>(() =>
                PlayerDeploymentZone.Of(new[] { new CellCoord(1, 2), new CellCoord(1, 2) }));
            Assert.That(error.Message, Does.Contain("(1, 2)"));
        }

        [Test]
        public void IsEmpty_CoversZeroRowsAndNoCells()
        {
            Assert.That(PlayerDeploymentZone.Rows(0).IsEmpty, Is.True);
            Assert.That(PlayerDeploymentZone.Empty.IsEmpty, Is.True);
            Assert.That(PlayerDeploymentZone.Of(Array.Empty<CellCoord>()).IsEmpty, Is.True);
            Assert.That(PlayerDeploymentZone.Rows(1).IsEmpty, Is.False);
            Assert.That(PlayerDeploymentZone.Of(new[] { CellCoord.Zero }).IsEmpty, Is.False);
        }

        [Test]
        public void FitsBoard_ChecksRowDepthOrListedCells()
        {
            Assert.That(PlayerDeploymentZone.Rows(3).FitsBoard(3, 3), Is.True);
            Assert.That(PlayerDeploymentZone.Rows(4).FitsBoard(3, 3), Is.False);
            var zone = PlayerDeploymentZone.Of(new[] { CellCoord.Zero, new CellCoord(3, 0), new CellCoord(1, -1) });
            Assert.That(zone.CellsOutside(3, 3).ToArray(), Is.EqualTo(new[] { new CellCoord(1, -1), new CellCoord(3, 0) }));
            Assert.That(zone.FitsBoard(3, 3), Is.False);
            Assert.That(PlayerDeploymentZone.Of(new[] { new CellCoord(2, 2) }).FitsBoard(3, 3), Is.True);
            Assert.That(PlayerDeploymentZone.Rows(9).CellsOutside(3, 3), Is.Empty);
        }
    }
}
