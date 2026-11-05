using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class TargetingAngleSelectionTests
    {
        [Test]
        public void Single_RejectsOutsideAngleBeforeSplash()
        {
            var resolver = new AttackSelectionResolver();
            Assert.That(resolver.TryResolve(Single(), 1, CellCoord.Zero, Owner.Player,
                AttackAim.ForCell(CellCoord.Right), out var cells, out var failure), Is.False);
            Assert.That(cells, Is.Null);
            Assert.That(failure, Is.EqualTo(AttackFailureReason.InvalidPrimaryTarget));
        }

        [TestCase(Owner.Player, 1)]
        [TestCase(Owner.Enemy, -1)]
        public void Single_PreservesSplashOutsideAngleAndRange(Owner owner, int sign)
        {
            var resolver = new AttackSelectionResolver();
            var primary = new CellCoord(1, sign);
            Assert.That(resolver.TryResolve(Single(), 1, CellCoord.Zero, owner,
                AttackAim.ForCell(primary), out var cells, out var failure), Is.True);
            Assert.That(failure, Is.EqualTo(AttackFailureReason.None));
            Assert.That(cells.PrimaryCell, Is.EqualTo(primary));
            Assert.That(cells.DesignatedCells.ToArray(), Is.EqualTo(new[] { primary }));
            Assert.That(cells.AdditionalCells.ToArray(), Is.EqualTo(new[]
            {
                new CellCoord(0, sign - 1), new CellCoord(1, sign - 1), new CellCoord(2, sign - 1),
                new CellCoord(0, sign), new CellCoord(2, sign),
                new CellCoord(0, sign + 1), new CellCoord(1, sign + 1), new CellCoord(2, sign + 1),
            }));
        }

        [Test]
        public void Direction_RejectsForbiddenIntentDespiteOverlappingRay()
        {
            var resolver = new AttackSelectionResolver();
            var method = Method(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingChoiceAngle.Front);
            Assert.That(resolver.TryResolve(method, 1, CellCoord.Zero, Owner.Player,
                AttackAim.ForDirection(AttackDirection.Left), out _, out var failure), Is.False);
            Assert.That(failure, Is.EqualTo(AttackFailureReason.InvalidAttackDirection));
            Assert.That(resolver.TryResolve(method, 1, CellCoord.Zero, Owner.Player,
                AttackAim.ForDirection(AttackDirection.Up), out var cells, out _), Is.True);
            Assert.That(cells.DesignatedCells.ToArray(), Is.EqualTo(new[]
            {
                new CellCoord(-1, 1), CellCoord.Up, new CellCoord(1, 1)
            }));
            Assert.That(resolver.TryResolve(method, 1, CellCoord.Zero, Owner.Player,
                AttackAim.ForDirection(AttackDirection.UpRight), out _, out failure), Is.False);
            Assert.That(failure, Is.EqualTo(AttackFailureReason.InvalidAttackDirection));
        }

        [TestCase(Owner.Player, 1)]
        [TestCase(Owner.Enemy, -1)]
        public void Direction_ClipsAllowedSideGroup(Owner owner, int sign)
        {
            var method = Method(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingChoiceAngle.FrontAndSides);
            Assert.That(new AttackSelectionResolver().TryResolve(method, 1, CellCoord.Zero, owner,
                AttackAim.ForDirection(AttackDirection.Right), out var cells, out _), Is.True);
            CollectionAssert.AreEquivalent(new[] { CellCoord.Right, new CellCoord(1, sign) }, cells.DesignatedCells.ToArray());
            Assert.That(cells.PrimaryCell, Is.Null);
            Assert.That(cells.AdditionalCells.ToArray(), Is.Empty);
        }

        [TestCase(Owner.Player, AttackDirection.UpLeft, -1, 1)]
        [TestCase(Owner.Enemy, AttackDirection.DownRight, 1, -1)]
        public void DiagonalLine_RetainsSupportedBoundaryIntent(Owner owner, AttackDirection direction, int x, int y)
        {
            var method = Method(TargetingShape.DiagonalLine, TargetingChoiceMode.Direction, TargetingChoiceAngle.Front);
            Assert.That(new AttackSelectionResolver().TryResolve(method, 1, CellCoord.Zero, owner,
                AttackAim.ForDirection(direction), out var cells, out _), Is.True);
            Assert.That(cells.DesignatedCells.ToArray(), Is.EqualTo(new[] { new CellCoord(x, y) }));
        }

        [TestCase(TargetingShape.Fan)]
        [TestCase(TargetingShape.CardinalLine)]
        public void All_RestrictsMaskWithoutAddingShapeCells(TargetingShape shape)
        {
            var method = Method(shape, TargetingChoiceMode.All, TargetingChoiceAngle.FrontAndRear);
            Assert.That(new AttackSelectionResolver().TryResolve(method, 1, CellCoord.Zero, Owner.Player,
                AttackAim.ForAll(), out var cells, out var failure), Is.True);
            Assert.That(failure, Is.EqualTo(AttackFailureReason.None));
            Assert.That(cells.DesignatedCells.ToArray(), Is.EqualTo(new[] { CellCoord.Down, CellCoord.Up }));
            Assert.That(cells.PrimaryCell, Is.Null);
            Assert.That(cells.AdditionalCells.ToArray(), Is.Empty);
        }

        [Test]
        public void All_RadialFrontAndRearRetainsBothDiagonalBoundaries()
        {
            var method = Method(TargetingShape.RadialSquare, TargetingChoiceMode.All, TargetingChoiceAngle.FrontAndRear);
            Assert.That(new AttackSelectionResolver().TryResolve(method, 1, CellCoord.Zero, Owner.Player,
                AttackAim.ForAll(), out var cells, out _), Is.True);
            Assert.That(cells.DesignatedCells.ToArray(), Is.EqualTo(new[]
            {
                new CellCoord(-1, -1), CellCoord.Down, new CellCoord(1, -1),
                new CellCoord(-1, 1), CellCoord.Up, new CellCoord(1, 1)
            }));
        }

        [TestCase(TargetingChoiceAngle.Front)]
        [TestCase(TargetingChoiceAngle.FrontAndSides)]
        [TestCase(TargetingChoiceAngle.FrontAndRear)]
        public void LegacySelection_PreservesAngleZeroAndRejectsRestrictedUse(TargetingChoiceAngle angle)
        {
            var resolver = new AttackSelectionResolver();
            Assert.Throws<ArgumentException>(() => resolver.TryResolve(
                Method(TargetingShape.RadialSquare, TargetingChoiceMode.All, angle),
                1, CellCoord.Zero, AttackAim.ForAll(), out _, out _));
            var unrestricted = Method(TargetingShape.RadialSquare, TargetingChoiceMode.All, TargetingChoiceAngle.Unrestricted);
            Assert.That(resolver.TryResolve(unrestricted, 1, CellCoord.Zero, AttackAim.ForAll(), out var legacy, out _), Is.True);
            Assert.That(resolver.TryResolve(unrestricted, 1, CellCoord.Zero, Owner.Enemy, AttackAim.ForAll(), out var explicitOwner, out _), Is.True);
            Assert.That(explicitOwner.DesignatedCells.ToArray(), Is.EqualTo(legacy.DesignatedCells.ToArray()));
        }

        [TestCase(TargetingChoiceMode.Single)]
        [TestCase(TargetingChoiceMode.Direction)]
        [TestCase(TargetingChoiceMode.All)]
        public void InvalidIntent_StillReturnsInvalidAttackAim(TargetingChoiceMode choice)
        {
            Assert.That(new AttackSelectionResolver().TryResolve(
                Method(TargetingShape.RadialSquare, choice, TargetingChoiceAngle.Front),
                1, CellCoord.Zero, Owner.Player, default, out var cells, out var failure), Is.False);
            Assert.That(cells, Is.Null);
            Assert.That(failure, Is.EqualTo(AttackFailureReason.InvalidAttackAim));
        }

        [Test]
        public void Selection_PreservesArgumentShapeAndOverflowFailures()
        {
            var resolver = new AttackSelectionResolver();
            var method = Method(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingChoiceAngle.Front);
            Assert.Throws<ArgumentNullException>(() => resolver.TryResolve(null, 1, CellCoord.Zero, Owner.Player, default, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => resolver.TryResolve(method, -1, CellCoord.Zero, Owner.Player, default, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => resolver.TryResolve(method, 0, CellCoord.Zero, Owner.Neutral, default, out _, out _));
            Assert.That(resolver.TryResolve(method, 1, CellCoord.Zero, Owner.Player,
                AttackAim.ForCell(new CellCoord(1, 1)), out _, out var failure), Is.False);
            Assert.That(failure, Is.EqualTo(AttackFailureReason.InvalidPrimaryTarget));
            Assert.That(resolver.TryResolve(method, 1, CellCoord.Zero, Owner.Player,
                AttackAim.ForCell(CellCoord.Zero), out _, out failure), Is.False);
            Assert.That(failure, Is.EqualTo(AttackFailureReason.InvalidPrimaryTarget));
            Assert.Throws<OverflowException>(() => resolver.TryResolve(Single(), 1,
                new CellCoord(0, int.MaxValue - 1), Owner.Player, AttackAim.ForCell(new CellCoord(0, int.MaxValue)),
                out _, out _));
        }

        [TestCase(TargetingChoiceMode.Direction)]
        [TestCase(TargetingChoiceMode.All)]
        public void ZeroRange_ReturnsEmptyDesignation(TargetingChoiceMode choice)
        {
            var aim = choice == TargetingChoiceMode.All ? AttackAim.ForAll() : AttackAim.ForDirection(AttackDirection.Up);
            Assert.That(new AttackSelectionResolver().TryResolve(
                Method(TargetingShape.Fan, choice, TargetingChoiceAngle.Front),
                0, CellCoord.Zero, Owner.Player, aim, out var cells, out _), Is.True);
            Assert.That(cells.DesignatedCells.ToArray(), Is.Empty);
        }

        private static TargetingMethodSpec Single()
        {
            return new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single,
                TargetingSplash.Radial, 70, TargetingChoiceAngle.Front);
        }

        private static TargetingMethodSpec Method(TargetingShape shape, TargetingChoiceMode choice, TargetingChoiceAngle angle)
        {
            return new TargetingMethodSpec(shape, choice, TargetingSplash.None, 0, angle);
        }
    }
}
