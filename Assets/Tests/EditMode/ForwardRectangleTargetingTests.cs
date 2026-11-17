using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class ForwardRectangleTargetingTests
    {
        private static TargetingMethodSpec Rectangle(int width, TargetingChoiceMode choice = TargetingChoiceMode.Direction) =>
            new TargetingMethodSpec(TargetingShape.ForwardRectangle, choice, TargetingSplash.None, 0, width: width);

        [TestCase(AttackDirection.Up, 0, 1, 1, 0)]
        [TestCase(AttackDirection.Right, 1, 0, 0, -1)]
        [TestCase(AttackDirection.Down, 0, -1, -1, 0)]
        [TestCase(AttackDirection.Left, -1, 0, 0, 1)]
        public void DirectionSelectionRotatesTheSixCellMask(AttackDirection direction, int fx, int fy, int rx, int ry)
        {
            var selection = CombatCommandTestData.ResolvedCells(Rectangle(3), 2, CellCoord.Zero, AttackAim.ForDirection(direction));
            var expected = new[]
            {
                new CellCoord(fx - rx, fy - ry), new CellCoord(fx, fy), new CellCoord(fx + rx, fy + ry),
                new CellCoord(2 * fx - rx, 2 * fy - ry), new CellCoord(2 * fx, 2 * fy), new CellCoord(2 * fx + rx, 2 * fy + ry)
            };
            Assert.That(selection.DesignatedCells.ToArray(), Is.EquivalentTo(expected));
        }

        [TestCase(1, 8)]
        [TestCase(2, 20)]
        [TestCase(3, 32)]
        public void RangeIsTheUnionWithOverlapCountedOnce(int depth, int count)
        {
            var method = Rectangle(3, TargetingChoiceMode.All);
            var cells = new AppointRangeCalculator().CalculateRangeCells(method, depth, CellCoord.Zero);
            var selected = CombatCommandTestData.ResolvedCells(method, depth, CellCoord.Zero, AttackAim.ForAll());
            Assert.That(cells, Has.Length.EqualTo(count));
            Assert.That(cells.Distinct().Count(), Is.EqualTo(count));
            Assert.That(cells, Has.No.Member(CellCoord.Zero));
            Assert.That(selected.DesignatedCells.ToArray(), Is.EqualTo(cells));
        }

        [Test]
        public void EvenWidthHasStableLocalAnchorAndSingleUsesTheSameUnion()
        {
            var north = AttackAim.ForDirection(AttackDirection.Up);
            Assert.That(CombatCommandTestData.ResolvedCells(Rectangle(2), 2, CellCoord.Zero, north).DesignatedCells.ToArray(),
                Is.EquivalentTo(new[] { new CellCoord(-1, 1), new CellCoord(0, 1), new CellCoord(-1, 2), new CellCoord(0, 2) }));
            var method = Rectangle(2, TargetingChoiceMode.Single);
            var resolver = new AttackSelectionResolver();
            Assert.That(resolver.TryResolve(method, 2, CellCoord.Zero, AttackAim.ForCell(new CellCoord(-1, 2)), out _, out _), Is.True);
            Assert.That(resolver.TryResolve(method, 2, CellCoord.Zero, AttackAim.ForCell(new CellCoord(1, 2)), out _, out _), Is.False);
        }

        [Test]
        public void PreviewClipsAtBoardEdgeAndLegacySquareKeepsTenCells()
        {
            var aim = AttackAim.ForDirection(AttackDirection.Up);
            var input = CombatCommandTestData.Input(Rectangle(3), aim, empty: true);
            var actor = CombatCommandTestData.With(input.Characters[0], position: new CellCoord(0, 3), appointRange: 2);
            var target = PiercingShotTestData.Character(2, 0, 4);
            input = CombatCommandTestData.With(input, characters: new[] { actor, input.Characters[1], target });
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.DesignatedCells.ToArray(), Is.EquivalentTo(new[]
            {
                new CellCoord(0, 4), new CellCoord(1, 4), new CellCoord(0, 5), new CellCoord(1, 5)
            }));
            var legacy = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            Assert.That(CombatCommandTestData.ResolvedCells(legacy, 2, CellCoord.Zero, aim).DesignatedCells.Length, Is.EqualTo(10));
        }

        [Test]
        public void LimitedDirectionalRangeMatchesTheUnionOfItsLegalSelections()
        {
            var method = new TargetingMethodSpec(TargetingShape.ForwardRectangle, TargetingChoiceMode.Direction,
                TargetingSplash.None, 0, TargetingChoiceAngle.Front, width: 5);
            var resolver = new AttackSelectionResolver();
            Assert.That(resolver.TryResolve(method, 1, CellCoord.Zero, Owner.Player, AttackAim.ForDirection(AttackDirection.Up),
                out var selected, out _), Is.True);
            Assert.That(new AppointRangeCalculator().CalculateRangeCells(method, 1, CellCoord.Zero, Owner.Player),
                Is.EqualTo(selected.DesignatedCells.ToArray()));
        }

        [Test]
        public void ThreeByThreeIsNineCellsAndOverflowFailsBeforeAllocation()
        {
            Assert.That(AppointShapeCalculator.CreateDirectionMask(TargetingShape.ForwardRectangle, 3, AttackDirection.Up, 3),
                Has.Length.EqualTo(9));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AppointShapeCalculator.CreateBaseMask(TargetingShape.ForwardRectangle, int.MaxValue, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AppointShapeCalculator.CreateDirectionMask(TargetingShape.ForwardRectangle, int.MaxValue, AttackDirection.Up, 3));
            var limited = new TargetingMethodSpec(TargetingShape.ForwardRectangle, TargetingChoiceMode.Direction,
                TargetingSplash.None, 0, TargetingChoiceAngle.FrontAndSides, width: 1);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AppointRangeCalculator().CalculateRangeCells(limited, int.MaxValue / 2, CellCoord.Zero, Owner.Player));
        }

        [TestCase(2, false)]
        [TestCase(3, true)]
        public void WideRectangleRequiresDistanceCoverageAtItsLateralEdge(int coveredDistance, bool eligible)
        {
            var basis = TraitGrowthFixtures.Profile();
            var profile = new CombatAttackProfile(basis.Definition, Rectangle(7));
            var scale = new DistanceDamageScaleTraitSpec(Enumerable.Repeat(100, coveredDistance).ToArray());
            var definition = new TraitGrowthDefinition(scale, null, "", "", "", "");
            var catalog = TraitGrowthFixtures.Catalog(definitions: new[] { definition });

            var candidates = TraitGrowthRules.Candidates(TraitGrowthFixtures.Owned(profile), profile, catalog);

            Assert.That(candidates.Contains(scale.TraitKey), Is.EqualTo(eligible));
        }
    }
}
