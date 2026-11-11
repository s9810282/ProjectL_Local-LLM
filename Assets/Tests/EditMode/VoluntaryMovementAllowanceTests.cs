using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class VoluntaryMovementAllowanceTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [Test]
        public void ZeroBase_DisablesEveryEntryPoint()
        {
            var input = WithTrail(MovementTestData.SafeRoute(CellCoord.Zero, CellCoord.Right), 0);
            var availability = CombatCommandCalculator.GetActionAvailability(input);

            Assert.That(availability.MovementAllowance, Is.Null);
            Assert.That(availability.MoveFailure, Is.EqualTo(MovementFailureReason.VoluntaryMoveDisabled));
            Assert.That(CombatMovementCalculator.Range(input).Succeeded, Is.False);
            Assert.That(CombatCommandCalculator.Calculate(input, NoRules).Reason,
                Is.EqualTo(MovementFailureReason.VoluntaryMoveDisabled.ToString()));
            Assert.That(EnemyMovementPathfinder.TryFind(EnemyInput(0), new EntityId(2), out _), Is.False);
        }

        [Test]
        public void GroundCompaction_QueryExecutionAndAiAgree()
        {
            var cells = Enumerable.Range(0, 7).Select(x => new CellCoord(x, 0)).ToArray();
            var input = WithTrail(MovementTestData.SafeRoute(cells.Take(6).ToArray()), 3);

            Assert.That(CombatCommandCalculator.GetActionAvailability(input).MovementAllowance, Is.EqualTo(5));
            var range = CombatMovementCalculator.Range(input);
            Assert.That(range.Succeeded, Is.True);
            Assert.That(range.Destinations.ToArray().Select(d => d.TargetAnchor),
                Has.Member(cells[5]).And.No.Member(cells[6]));

            var accepted = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(accepted.Accepted, Is.True, accepted.Reason);
            Assert.That(accepted.PathMovement.DistanceSpent, Is.EqualTo(5));
            var rejected = CombatCommandCalculator.Calculate(MovementTestData.Route(input, cells), NoRules);
            Assert.That(rejected.Reason, Is.EqualTo(MovementFailureReason.MoveRangeExceeded.ToString()));

            Assert.That(EnemyMovementPathfinder.TryFind(EnemyInput(3), new EntityId(2), out var path), Is.True);
            Assert.That(path.StepCount, Is.EqualTo(5));
        }

        private static CombatCommandInput EnemyInput(int moveRange) =>
            WithTrail(EnemyMovementPathTests.Input(actorCell: CellCoord.Zero, targetCell: new CellCoord(4, 3)), moveRange);

        private static CombatCommandInput WithTrail(CombatCommandInput input, int moveRange) =>
            TraitTrailTestData.Environment(MovementTestData.WithMovement(input, moveRange),
                TraitTrailTestData.Trail(10, CellCoord.Zero, TraitKey.GroundCompaction, movement: 2));
    }
}
