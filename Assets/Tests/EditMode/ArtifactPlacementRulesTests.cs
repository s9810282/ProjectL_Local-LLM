using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;

namespace ProjectL.Tests.Core
{
    public sealed class ArtifactPlacementRulesTests
    {
        [TestCase(Rotation4.R0, 2, 1, 0, 0, 1, 0)]
        [TestCase(Rotation4.R90, 1, 2, 0, 1, 0, 0)]
        [TestCase(Rotation4.R180, 2, 1, 1, 0, 0, 0)]
        [TestCase(Rotation4.R270, 1, 2, 0, 0, 0, 1)]
        public void Rotation_UsesExistingTileFootprintConvention(
            Rotation4 rotation, int width, int height, int firstX, int firstY, int secondX, int secondY)
        {
            ArtifactShapeSnapshot shape = ArtifactPlacementRules.GetShape(
                Definition(CellCoord.Zero, CellCoord.Right), rotation);

            Assert.That(shape.Width, Is.EqualTo(width));
            Assert.That(shape.Height, Is.EqualTo(height));
            Assert.That(shape.Cells, Is.EqualTo(new[]
            {
                new CellCoord(firstX, firstY), new CellCoord(secondX, secondY)
            }));
        }

        [TestCase(Rotation4.R0, 3, 2, 0, 0, 2, 0, 0, 1)]
        [TestCase(Rotation4.R90, 2, 3, 0, 2, 0, 0, 1, 2)]
        [TestCase(Rotation4.R180, 3, 2, 2, 1, 0, 1, 2, 0)]
        [TestCase(Rotation4.R270, 2, 3, 1, 0, 1, 2, 0, 0)]
        public void AsymmetricShape_PreservesRotationDirectionAndSparseCells(
            Rotation4 rotation, int width, int height,
            int firstX, int firstY, int secondX, int secondY, int thirdX, int thirdY)
        {
            ArtifactShapeSnapshot shape = ArtifactPlacementRules.GetShape(
                Definition(CellCoord.Zero, new CellCoord(2, 0), CellCoord.Up), rotation);

            Assert.That(shape.Width, Is.EqualTo(width));
            Assert.That(shape.Height, Is.EqualTo(height));
            Assert.That(shape.Cells, Is.EqualTo(new[]
            {
                new CellCoord(firstX, firstY), new CellCoord(secondX, secondY),
                new CellCoord(thirdX, thirdY)
            }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BoundsTakePrecedenceOverCollision(bool reverseCells)
        {
            var cells = new[] { CellCoord.Zero, CellCoord.Right };
            if (reverseCells) Array.Reverse(cells);
            var shape = new ArtifactShapeSnapshot(2, 1, cells);
            var state = new ArtifactState(2, 2, null, new[] { Placed(1, new CellCoord(1, 0)) });

            Assert.That(ArtifactPlacementRules.CheckPlacement(state, shape, new CellCoord(1, 0), null),
                Is.EqualTo(ArtifactActionFailureReason.OutOfBounds));
        }

        [TestCase(Rotation4.R0)]
        [TestCase(Rotation4.R90)]
        [TestCase(Rotation4.R180)]
        [TestCase(Rotation4.R270)]
        public void MovePreview_IgnoresOnlyOwnCells(Rotation4 rotation)
        {
            var state = new ArtifactState(4, 4, null, new[]
            {
                Placed(0, CellCoord.Zero, CellCoord.Right, CellCoord.Up),
                Placed(1, new CellCoord(2, 0), new CellCoord(2, 1))
            });
            ArtifactShapeSnapshot shape = ArtifactPlacementRules.GetShape(
                Definition(CellCoord.Zero, CellCoord.Right), rotation);

            Assert.That(ArtifactPlacementRules.CheckPlacement(state, shape, CellCoord.Zero, 0),
                Is.EqualTo(ArtifactActionFailureReason.None));
            Assert.That(ArtifactPlacementRules.CheckPlacement(state, shape, CellCoord.Zero, null),
                Is.EqualTo(ArtifactActionFailureReason.CellOccupied));
            Assert.That(ArtifactPlacementRules.CheckPlacement(state, shape, new CellCoord(2, 0), 0),
                Is.EqualTo(ArtifactActionFailureReason.CellOccupied));
            Assert.That(ArtifactPlacementRules.CheckPlacement(state, shape, CellCoord.Zero, 99),
                Is.EqualTo(ArtifactActionFailureReason.CellOccupied));
        }

        [Test]
        public void EmptyInventory_DoesNotInvalidateSpatialPreview()
        {
            var state = new ArtifactState(3, 2, null, null);
            ArtifactShapeSnapshot shape = ArtifactPlacementRules.GetShape(
                Definition(CellCoord.Zero, CellCoord.Right), Rotation4.R0);

            Assert.That(ArtifactPlacementRules.CheckPlacement(state, shape, new CellCoord(1, 1), null),
                Is.EqualTo(ArtifactActionFailureReason.None));
        }

        [Test]
        public void PoolCells_DoNotOccupyBoard()
        {
            var poolItem = new ArtifactItemState(0, 0, false, CellCoord.Zero, Rotation4.R0,
                new[] { CellCoord.Zero });
            var state = new ArtifactState(1, 1, new[] { poolItem }, null);
            ArtifactShapeSnapshot shape = ArtifactPlacementRules.GetShape(Definition(CellCoord.Zero), Rotation4.R0);

            Assert.That(ArtifactPlacementRules.CheckPlacement(state, shape, CellCoord.Zero, null),
                Is.EqualTo(ArtifactActionFailureReason.None));
        }

        [Test]
        public void Collision_UsesAbsolutePlacedCellsWithoutReapplyingAnchorOrRotation()
        {
            var item = new ArtifactItemState(0, 0, true, new CellCoord(3, 3), Rotation4.R90,
                new[] { CellCoord.Right });
            var state = new ArtifactState(4, 4, null, new[] { item });
            ArtifactShapeSnapshot shape = ArtifactPlacementRules.GetShape(Definition(CellCoord.Zero), Rotation4.R0);

            Assert.That(ArtifactPlacementRules.CheckPlacement(state, shape, CellCoord.Right, null),
                Is.EqualTo(ArtifactActionFailureReason.CellOccupied));
            Assert.That(ArtifactPlacementRules.CheckPlacement(state, shape, new CellCoord(3, 3), null),
                Is.EqualTo(ArtifactActionFailureReason.None));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(2, 0)]
        [TestCase(0, 2)]
        public void BoardEdges_RejectCellsOutsideEitherAxis(int x, int y)
        {
            var state = new ArtifactState(2, 2, null, null);
            ArtifactShapeSnapshot shape = ArtifactPlacementRules.GetShape(Definition(CellCoord.Zero), Rotation4.R0);

            Assert.That(ArtifactPlacementRules.CheckPlacement(state, shape, new CellCoord(x, y), null),
                Is.EqualTo(ArtifactActionFailureReason.OutOfBounds));
        }

        [TestCase(int.MaxValue, 0, 1, 0)]
        [TestCase(0, int.MaxValue, 0, 1)]
        [TestCase(int.MinValue, 0, int.MinValue, 0)]
        [TestCase(0, int.MinValue, 0, int.MinValue)]
        public void ExtremeAnchor_DoesNotWrapIntoBoard(int x, int y, int offsetX, int offsetY)
        {
            var state = new ArtifactState(2, 2, null, null);
            var shape = new ArtifactShapeSnapshot(1, 1, new[] { new CellCoord(offsetX, offsetY) });

            Assert.That(ArtifactPlacementRules.CheckPlacement(state, shape, new CellCoord(x, y), null),
                Is.EqualTo(ArtifactActionFailureReason.OutOfBounds));
        }

        [Test]
        public void ShapeAndCheck_DoNotMutateInputs()
        {
            var definitionCells = new[] { CellCoord.Zero, CellCoord.Right };
            ArtifactDefinition definition = Definition(definitionCells);
            var poolCells = new[] { CellCoord.Zero, CellCoord.Right };
            var placedCells = new[] { CellCoord.Zero, CellCoord.Up };
            var pool = new[] { new ArtifactItemState(0, 2, false, CellCoord.Zero, Rotation4.R0, poolCells) };
            var placed = new[] { new ArtifactItemState(0, 0, true, CellCoord.Zero, Rotation4.R90, placedCells) };
            var state = new ArtifactState(3, 3, pool, placed);

            foreach (Rotation4 rotation in Enum.GetValues(typeof(Rotation4)))
            {
                ArtifactShapeSnapshot shape = ArtifactPlacementRules.GetShape(definition, rotation);
                var before = new CellCoord[shape.Cells.Count];
                for (int i = 0; i < before.Length; i++) before[i] = shape.Cells[i];
                ArtifactPlacementRules.CheckPlacement(state, shape, CellCoord.Zero, 0);
                ArtifactPlacementRules.CheckPlacement(state, shape, CellCoord.Zero, null);
                ArtifactPlacementRules.CheckPlacement(state, shape, new CellCoord(3, 3), null);
                Assert.That(shape.Cells, Is.EqualTo(before));
                Assert.Throws<NotSupportedException>(() =>
                    ((System.Collections.Generic.IList<CellCoord>)shape.Cells)[0] = new CellCoord(9, 9));
            }

            Assert.That(definitionCells, Is.EqualTo(new[] { CellCoord.Zero, CellCoord.Right }));
            Assert.That(definition.Cells, Is.EqualTo(definitionCells));
            Assert.That(poolCells, Is.EqualTo(new[] { CellCoord.Zero, CellCoord.Right }));
            Assert.That(placedCells, Is.EqualTo(new[] { CellCoord.Zero, CellCoord.Up }));
            Assert.That(state.BoardWidth, Is.EqualTo(3));
            Assert.That(state.BoardHeight, Is.EqualTo(3));
            Assert.That(state.PoolItems.ToArray(), Is.EqualTo(pool));
            Assert.That(state.PlacedItems.ToArray(), Is.EqualTo(placed));
            Assert.That(pool[0].InstanceId, Is.EqualTo(2));
            Assert.That(pool[0].IsPlaced, Is.False);
            Assert.That(placed[0].InstanceId, Is.EqualTo(0));
            Assert.That(placed[0].Anchor, Is.EqualTo(CellCoord.Zero));
            Assert.That(placed[0].Rotation, Is.EqualTo(Rotation4.R90));
        }

        [Test]
        public void InvalidRotation_ThrowsWithoutChangingDefinition()
        {
            ArtifactDefinition definition = Definition(CellCoord.Zero, CellCoord.Right);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ArtifactPlacementRules.GetShape(definition, (Rotation4)99));
            Assert.That(definition.Cells, Is.EqualTo(new[] { CellCoord.Zero, CellCoord.Right }));
        }

        private static ArtifactDefinition Definition(params CellCoord[] cells)
            => new ArtifactDefinition(0, 0, cells, Array.Empty<ArtifactEffectData>());

        private static ArtifactItemState Placed(int instanceId, params CellCoord[] cells)
            => new ArtifactItemState(0, instanceId, true, CellCoord.Zero, Rotation4.R0, cells);
    }
}
