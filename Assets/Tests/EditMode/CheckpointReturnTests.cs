using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    public sealed class CheckpointReturnTests
    {
        [Test]
        public void RetiredReturnCommandValue_IsNotReused()
        {
            Assert.That(Enum.IsDefined(typeof(CombatCommandKind), 9), Is.False);
            Assert.That((int)CombatCommandKind.ReturnUnit, Is.EqualTo(3));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RetiredReturn_IsRejectedBeforeAndAfterMovement(bool moveFirst)
        {
            var input = Route();
            if (moveFirst)
            {
                var moved = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
                Assert.That(moved.Accepted, Is.True, moved.Reason);
                input = MovementTestData.After(moved);
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => input.WithIntent((CombatCommandKind)9, default,
                new CellCoord(1, 1), null, null));
            Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).ActionUse,
                Is.EqualTo(moveFirst ? UnitActionUse.Moved : UnitActionUse.Available));
        }

        [Test]
        public void MovementIsFinal_WithoutAnAttackOrTurnEnd()
        {
            var input = Route();
            var moved = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(MovementTestData.Actor(moved).Anchor, Is.EqualTo(new CellCoord(3, 1)));
            Assert.That(MovementTestData.Actor(moved).ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(moved.FinalState.Turn.SameValues(input.State.Turn), Is.True);
            Assert.That(input.Characters.First(c => c.EntityId == input.ActorId).Anchor, Is.EqualTo(new CellCoord(1, 1)));
        }

        [TestCase(1, 1)]
        [TestCase(4, 1)]
        public void SecondDragCannotMoveAgain(int x, int y)
        {
            var moved = CombatCommandCalculator.Calculate(Route(), Array.Empty<IMovementTileRule>());
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            var after = MovementTestData.After(moved);
            var next = MovementTestData.Route(after, new CellCoord(3, 1), new CellCoord(x, y));
            var result = CombatCommandCalculator.Calculate(next, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("ActionAlreadyUsed"));
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.Events, Is.Empty);
            Assert.That(MovementTestData.Actor(result).Anchor, Is.EqualTo(new CellCoord(3, 1)));
        }

        [Test]
        public void DiscardingAPreview_PreservesActionAndAllowsAlternativeRoute()
        {
            var input = Route();
            var preview = CombatMovementCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanMove, Is.True);
            Assert.That(input.Characters.First(c => c.EntityId == input.ActorId).ActionUse, Is.EqualTo(UnitActionUse.Available));
            var alternative = MovementTestData.Route(input, new CellCoord(1, 1), new CellCoord(1, 2));
            var result = CombatCommandCalculator.Calculate(alternative, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Anchor, Is.EqualTo(new CellCoord(1, 2)));
        }

        private static CombatCommandInput Route() => MovementTestData.SafeRoute(            new CellCoord(1, 1),new CellCoord(2, 1),new CellCoord(3, 1));
    }
}
