using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    public sealed class PullObstacleTests
    {
        [TestCase(1, false)]
        [TestCase(1, true)]
        [TestCase(2, false)]
        [TestCase(2, true)]
        public void Obstacle_InnerAttackUnchanged_OuterAttackRejected_NoMovement(int distance, bool multiCell)
        {
            var input = PullTestData.Input(distance: distance);
            var barrel = ObstacleReactionTests.Barrel(51, input.Aim.Cell.Value, 3, 7,
                multiCell ? new[] { CellCoord.Zero, new CellCoord(0, 1) } : null);
            input = ObstacleReactionTests.Copy(input,
                characters: input.Characters.Where(c => c.EntityId != PullTestData.Target).ToArray(),
                structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.AttackAims.ToArray().Contains(input.Aim), Is.EqualTo(distance == 1));
            Assert.That(range.InRangeTargetIds.ToArray().Contains(barrel.Structure.EntityId), Is.EqualTo(distance == 1));
            var result = CombatCommandCalculator.Calculate(input, PullTestData.NoRules);
            Assert.That(result.Accepted, Is.EqualTo(distance == 1), result.Reason);
            var final = result.FinalState.Structures.Single();
            Assert.That(final.ObstacleDurability.RemainingHits, Is.EqualTo(distance == 1 ? 2 : 3));
            Assert.That(final.Anchor, Is.EqualTo(barrel.Structure.Anchor));
            Assert.That(final.OccupiedCells, Is.EqualTo(barrel.Structure.OccupiedCells));
            Assert.That(result.Events.Any(e => e.GetType().Name == "CombatPullEvent"), Is.False);
        }
    }
}
