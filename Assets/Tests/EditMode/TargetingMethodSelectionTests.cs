using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class TargetingMethodSelectionTests
    {
        [Test]
        public void AllLiteralAims_ResolveWithoutEntityOrMapState()
        {
            var resolver = new AttackSelectionResolver();
            foreach (var example in TargetingMethodFixtures.Examples)
            {
                Assert.That(resolver.TryResolve(example.Method, example.Range, CellCoord.Zero, example.Aim(CellCoord.Zero),
                    out var cells, out var failure), Is.True, example.ToString());
                Assert.That(failure, Is.EqualTo(AttackFailureReason.None));
                Assert.That(cells.DesignatedCells.ToArray(), Is.EqualTo(example.Cells("T", CellCoord.Zero)));
            }
        }

        [TestCase(TargetingChoiceMode.Single)]
        [TestCase(TargetingChoiceMode.Direction)]
        [TestCase(TargetingChoiceMode.All)]
        public void MissingOrWrongIntent_ReturnsInvalidAim(TargetingChoiceMode choice)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, choice, TargetingSplash.None, 0);
            AssertFailure(method, default, AttackFailureReason.InvalidAttackAim);
            if (choice != TargetingChoiceMode.All) AssertFailure(method, AttackAim.ForAll(), AttackFailureReason.InvalidAttackAim);
            if (choice != TargetingChoiceMode.Single) AssertFailure(method, AttackAim.ForCell(CellCoord.Up), AttackFailureReason.InvalidAttackAim);
            if (choice != TargetingChoiceMode.Direction) AssertFailure(method, AttackAim.ForDirection(AttackDirection.Up), AttackFailureReason.InvalidAttackAim);
        }

        [Test]
        public void Single_RequiresPureCellInsideMask()
        {
            var method = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            AssertFailure(method, AttackAim.ForCell(CellCoord.Zero), AttackFailureReason.InvalidPrimaryTarget);
            AssertFailure(method, AttackAim.ForCell(new CellCoord(1,1)), AttackFailureReason.InvalidPrimaryTarget);
            AssertFailure(method, AttackAim.ForCell(new CellCoord(0,4)), AttackFailureReason.InvalidPrimaryTarget);
            AssertFailure(method, WithField(AttackAim.ForCell(CellCoord.Up), "Direction", AttackDirection.Up),
                AttackFailureReason.InvalidAttackAim);
        }

        [TestCase(TargetingChoiceMode.Single, 1)]
        [TestCase(TargetingChoiceMode.Single, 99)]
        [TestCase(TargetingChoiceMode.Direction, 1)]
        [TestCase(TargetingChoiceMode.Direction, 99)]
        [TestCase(TargetingChoiceMode.All, 1)]
        [TestCase(TargetingChoiceMode.All, 99)]
        public void RetiredOrUnknownAimKind_IsRejected(TargetingChoiceMode choice, int kind)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, choice, TargetingSplash.None, 0);
            AssertFailure(method, WithField(default, "Kind", (AttackAimKind)kind), AttackFailureReason.InvalidAttackAim);
        }

        [TestCase(TargetingShape.CardinalLine, AttackDirection.UpLeft)]
        [TestCase(TargetingShape.DiagonalLine, AttackDirection.Up)]
        [TestCase(TargetingShape.EightDirectionLine, AttackDirection.UpRight)]
        [TestCase(TargetingShape.Fan, AttackDirection.DownLeft)]
        [TestCase(TargetingShape.RadialSquare, AttackDirection.DownRight)]
        public void UnsupportedShapeDirection_ReturnsInvalidDirection(TargetingShape shape, AttackDirection direction)
        {
            var method = new TargetingMethodSpec(shape, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            AssertFailure(method, AttackAim.ForDirection(direction), AttackFailureReason.InvalidAttackDirection);
        }

        [Test]
        public void OrdinaryTargets_DeduplicateFootprintsAndPrimaryWins()
        {
            var geometry = new AttackGeometryCalculator(new AppointRangeCalculator());
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Cross, 50);
            var selection = geometry.CalculateCells(method, 2, CellCoord.Zero, AttackAim.ForCell(new CellCoord(-1,2)));
            var primary = Facts(9, new CellCoord(-1,2), new CellCoord(-1,3));
            var additional = Facts(2, new CellCoord(-2,2), new CellCoord(-1,3));
            var miss = Facts(3, new CellCoord(9,9));
            var resolver = new AttackTargetResolver();
            var result = resolver.CollectCandidates(selection, new[] { primary, miss, additional, primary },
                Array.Empty<StructureCombatFacts>());
            Assert.That(result.Select(target => target.Key.Value), Is.EqualTo(new[] { 2, 9 }));
            Assert.That(result.Select(target => target.Value), Is.EqualTo(new[] { AttackTargetRole.Additional, AttackTargetRole.Primary }));
            // 기준점 대신 모든 점유 셀을 확인한다. 선택 범위 밖의 추가 셀도 대상이 될 수 있다.
            Assert.That(resolver.ResolveRole(selection, new[] { new CellCoord(-1,3) }), Is.EqualTo(AttackTargetRole.Additional));
        }

        [TestCase(TargetingChoiceMode.Direction)]
        [TestCase(TargetingChoiceMode.All)]
        public void DirectionAndAll_OnlyReturnDesignatedRoles(TargetingChoiceMode choice)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, choice, TargetingSplash.None, 0);
            var aim = choice == TargetingChoiceMode.Direction ? AttackAim.ForDirection(AttackDirection.Left) : AttackAim.ForAll();
            var selection = new AttackGeometryCalculator(new AppointRangeCalculator()).CalculateCells(method, 2, CellCoord.Zero, aim);
            var result = new AttackTargetResolver().CollectCandidates(selection,
                new[] { Facts(5, new CellCoord(9,9), new CellCoord(-2,-2), new CellCoord(-1,1)) },
                Array.Empty<StructureCombatFacts>());
            Assert.That(selection.PrimaryCell, Is.Null);
            Assert.That(result.Length, Is.EqualTo(1));
            Assert.That(result[0].Value, Is.EqualTo(AttackTargetRole.Designated));
        }

        [Test]
        public void Projection_UsesExplicitCallerApprovedFactsWithoutInventingEligibility()
        {
            var selection = new AttackCellSelection(null, new[] { CellCoord.Up }, Array.Empty<CellCoord>());
            var structure = new StructureCombatFacts(new EntityId(1),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 10, false, 0),
                new HealthStateSnapshot(10,10), CellCoord.Up, Rotation4.R0, new[] { CellCoord.Up });
            var resolver = new AttackTargetResolver();
            Assert.That(resolver.CollectCandidates(selection, Array.Empty<CharacterCombatFacts>(), new[] { structure })[0].Value,
                Is.EqualTo(AttackTargetRole.Designated));
            Assert.That(resolver.CollectCandidates(selection, Array.Empty<CharacterCombatFacts>(), Array.Empty<StructureCombatFacts>()), Is.Empty);
            Assert.That(resolver.ResolveRole(selection, Array.Empty<CellCoord>()), Is.EqualTo(AttackTargetRole.None));
            Assert.Throws<ArgumentNullException>(() => resolver.ResolveRole(null, new[] { CellCoord.Up }));
            Assert.Throws<ArgumentNullException>(() => resolver.ResolveRole(selection, null));
            Assert.Throws<ArgumentException>(() => resolver.CollectCandidates(selection,
                new CharacterCombatFacts[] { null }, Array.Empty<StructureCombatFacts>()));
        }

        private static CharacterCombatFacts Facts(int id, params CellCoord[] cells)
        {
            return new CharacterCombatFacts(new EntityId(id), null, EntityKey.Unit_Demon, Owner.Enemy,
                null, CharacterRuntimeArea.Field, new HealthStateSnapshot(10,10), default, null,
                cells[0], Rotation4.R0, cells, cells[0], 0, 0);
        }

        private static AttackAim WithField(AttackAim aim, string name, object value)
        {
            // 생성자를 우회한 외부 입력도 Core에서 거부하는지 확인한다.
            object boxed = aim;
            typeof(AttackAim).GetField($"<{name}>k__BackingField",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(boxed, value);
            return (AttackAim)boxed;
        }

        private static void AssertFailure(TargetingMethodSpec method, AttackAim aim, AttackFailureReason expected)
        {
            Assert.That(new AttackSelectionResolver().TryResolve(method, 3, CellCoord.Zero, aim,
                out var selection, out var failure), Is.False);
            Assert.That(selection, Is.Null);
            Assert.That(failure, Is.EqualTo(expected));
            Assert.Throws<ArgumentException>(() => new AttackGeometryCalculator(new AppointRangeCalculator())
                .CalculateCells(method, 3, CellCoord.Zero, aim));
        }
    }
}
