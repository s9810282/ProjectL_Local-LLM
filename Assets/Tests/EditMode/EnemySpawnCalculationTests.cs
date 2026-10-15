using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class EnemySpawnCalculationTests
    {
        [TestCase(UnitRole.MainCharacter, UnitOwnerEligibility.Both)]
        [TestCase(UnitRole.Regular, UnitOwnerEligibility.PlayerOnly)]
        public void IneligibleCandidate_IsRejectedBeforeRandomSelection(UnitRole role, UnitOwnerEligibility owner)
        {
            Assert.Throws<InvalidOperationException>(() => Calculate(new[] {
                new EnemySpawnCandidate(Unit(role: role, owner: owner), 1) }));
        }

        [Test]
        public void MultiCellCandidate_DoesNotUseAnchorOnlyPlacement()
        {
            var error = Assert.Throws<InvalidOperationException>(() => Calculate(new[] {
                new EnemySpawnCandidate(Unit(width: 2), 1) }));
            Assert.That(error.Message, Does.Contain("다칸"));
        }

        [Test]
        public void OverflowingWeightSum_IsNotWrappedIntoAnotherDistribution()
        {
            Assert.Throws<OverflowException>(() => Calculate(new[] {
                new EnemySpawnCandidate(Unit(), int.MaxValue), new EnemySpawnCandidate(Unit(), 1) }));
        }

        [Test]
        public void DefaultCandidate_DoesNotBecomeAnImplicitUnit()
        {
            Assert.Throws<InvalidOperationException>(() => Calculate(new[] { default(EnemySpawnCandidate) }));
        }

        private static InitialEnemyPlacementSpec[] Calculate(EnemySpawnCandidate[] candidates) =>
            EnemySpawnCalculator.Calculate(new StageConfig(8, 6, 8, 2, 2), candidates, 2, 7,
                Array.Empty<StructureSpec>());

        private static UnitSpec Unit(int width = 1, UnitRole role = UnitRole.Regular,
            UnitOwnerEligibility owner = UnitOwnerEligibility.EnemyOnly) => new UnitSpec(
            EntityKey.Unit_Demon, role, width, 1,
            Enumerable.Range(0, width).Select(x => new CellCoord(x, 0)).ToArray(), 10,
            MoveMethod.LiftAndPlace, 2, 1, 1, CategoryKey.Base,
            5, 4, 1, 0, 1, 1, true, Array.Empty<TraitSpec>(), owner);
    }
}
