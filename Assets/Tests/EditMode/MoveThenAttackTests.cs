using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 한 진영 턴에 이동 → 공격을 둘 다 할 수 있다는 규칙을 확인한다(설계 2026-10-27 이동 후 공격).
    /// 공격 → 이동·이동 → 이동·공격 → 공격 거절은 PlayerTurnAttackTests·PlayerTurnMovementTests가 확인한다.
    /// </summary>
    public sealed class MoveThenAttackTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [Test]
        public void MoveThenAttack_IsAcceptedAndConsumesActionInOrder()
        {
            var input = CombatCommandTestData.Input();
            var moved = CombatCommandCalculator.Calculate(MovementTestData.Route(input,
                new CellCoord(1, 3), new CellCoord(2, 3)), Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);

            var attack = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(
                MovementTestData.After(moved), CombatCommandKind.Attack, input.Aim), Rules);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            var settlement = attack.Attack.Value.ActionUseSettlement;
            Assert.That((settlement.BeforeActionUse, settlement.FinalActionUse),
                Is.EqualTo((UnitActionUse.Moved, UnitActionUse.Attacked)));
            Assert.That(MovementTestData.Actor(attack).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        [Test]
        public void AfterMoveThenAttack_NoFurtherActionIsAccepted()
        {
            var input = CombatCommandTestData.Input();
            var moved = CombatCommandCalculator.Calculate(MovementTestData.Route(input,
                new CellCoord(1, 3), new CellCoord(2, 3)), Rules);
            var attack = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(
                MovementTestData.After(moved), CombatCommandKind.Attack, input.Aim), Rules);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            var after = MovementTestData.After(attack);

            var again = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(
                after, CombatCommandKind.Attack, input.Aim), Rules);
            var move = CombatCommandCalculator.Calculate(MovementTestData.Route(after,
                new CellCoord(2, 3), new CellCoord(2, 2)), Rules);
            Assert.That(again.Reason, Is.EqualTo(AttackFailureReason.ActionAlreadyUsed.ToString()));
            Assert.That(move.Reason, Is.EqualTo(MovementFailureReason.ActionAlreadyUsed.ToString()));
        }

        [Test]
        public void AfterMove_OnlyAttackRemainsAvailable()
        {
            var moved = CombatCommandCalculator.Calculate(MovementTestData.Route(CombatCommandTestData.Input(),
                new CellCoord(1, 3), new CellCoord(2, 3)), Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            var availability = CombatCommandCalculator.GetActionAvailability(MovementTestData.After(moved));
            Assert.That(availability.ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(availability.CanMove, Is.False);
            Assert.That(availability.MoveFailure, Is.EqualTo(MovementFailureReason.ActionAlreadyUsed));
            Assert.That(availability.CanAttack, Is.True);
            Assert.That(availability.AttackFailure, Is.EqualTo(AttackFailureReason.None));
        }

        [Test]
        public void AfterMove_AttackNeedsNoFunding()
        {
            var input = CombatCommandTestData.Input();
            input = CombatCommandTestData.With(input, state: input.State.With());
            var moved = CombatCommandCalculator.Calculate(MovementTestData.Route(input,
                new CellCoord(1, 3), new CellCoord(2, 3)), Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);

            var attack = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(
                MovementTestData.After(moved), CombatCommandKind.Attack, input.Aim), Rules);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(attack.ActionUseSettlement.BeforeActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(attack.ActionUseSettlement.FinalActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        [TestCase(UnitActionUse.Deferred, false, false)]
        [TestCase(UnitActionUse.Available, true, true)]
        [TestCase(UnitActionUse.Moved, false, true)]
        [TestCase(UnitActionUse.Attacked, false, false)]
        public void ActionUseRules_MoveOnlyFromAvailable_AttackFromAvailableOrMoved(
            UnitActionUse use, bool canMove, bool canAttack)
        {
            Assert.That(UnitActionUseRules.CanStartMove(use), Is.EqualTo(canMove));
            Assert.That(UnitActionUseRules.CanStartAttack(use), Is.EqualTo(canAttack));
        }
    }
}
