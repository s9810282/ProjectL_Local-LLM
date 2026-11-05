using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class CombatActionCostRetirementTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [Test]
        public void AllEligibleUnitsCanActWithoutSharedBudget()
        {
            var input = CombatCommandTestData.Input();
            var actors = new[] { input.Characters.Single(c => c.EntityId == input.ActorId),
                CombatCommandTestData.Character(20, EntityKey.Unit_Akimbo, new CellCoord(1, 2), Owner.Player, 20),
                CombatCommandTestData.Character(21, EntityKey.Unit_Akimbo, new CellCoord(1, 4), Owner.Player, 20),
                CombatCommandTestData.Character(22, EntityKey.Unit_Akimbo, new CellCoord(2, 4), Owner.Player, 20) };
            input = CombatCommandTestData.With(input, input.Characters.Concat(actors.Skip(1)).ToArray());
            input = new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim,
                input.State, input.Characters, input.Profiles, input.Main,
                input.Characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(
                    c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                input.Characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, _ => 0),
                input.Souls, input.Artifacts, battleInstanceId: 1, expectedBattleInstanceId: 1,
                expectedControllerTurnSequence: 1);
            foreach (var actor in actors)
            {
                var result = CombatCommandCalculator.Calculate(input.WithActor(actor.EntityId), Rules);
                Assert.That(result.Accepted, Is.True, result.Reason);
                Assert.That(MovementTestData.Actor(result).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
                input = CombatCommandComposition.Project(result);
            }
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 2).Health.CurrentHealth, Is.EqualTo(20));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void CategoryDistanceLimitIsPreserved(int limit)
        {
            var input = CombatCommandTestData.Input();
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.With(stats: c.Stats.With(moveRange: limit)) : c).ToArray());
            var cells = Enumerable.Range(0, limit + 2).Select(y => new CellCoord(1, 3 + y)).ToArray();
            var result = CombatCommandCalculator.Calculate(MovementTestData.Route(input, cells), Rules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(MovementFailureReason.MoveRangeExceeded.ToString()));
            Assert.That(result.Characters.Characters, Is.Empty);
        }

        [Test]
        public void ShortMoveConsumesMovementAction()
        {
            var moved = Move();
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(MovementTestData.Actor(moved).ActionUse, Is.EqualTo(UnitActionUse.Moved));
            var again = CombatCommandCalculator.Calculate(MovementTestData.Route(CombatCommandComposition.Project(moved),
                new CellCoord(2, 3), new CellCoord(2, 2)), Rules);
            Assert.That(again.Reason, Is.EqualTo(MovementFailureReason.ActionAlreadyUsed.ToString()));
        }

        [Test]
        public void ZeroVoluntaryRange_DisablesMovementWithoutChangingFacts()
        {
            var input = CombatCommandTestData.Input();
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.With(stats: c.Stats.With(moveRange: 0)) : c).ToArray());
            var result = CombatCommandCalculator.Calculate(MovementTestData.Route(input,
                new CellCoord(1, 3), new CellCoord(2, 3)), Rules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(MovementFailureReason.VoluntaryMoveDisabled.ToString()));
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState.SameValues(input.State), Is.True);
            Assert.That(CombatCommandCalculator.GetActionAvailability(input).CanMove, Is.False);
        }

        [Test]
        public void MoveThenAttackEachOnce()
        {
            var moved = Move();
            var attack = CombatCommandCalculator.Calculate(CombatCommandTestData.FreshCommand(
                CombatCommandComposition.Project(moved), CombatCommandKind.Attack,
                aim: AttackAim.ForCell(new CellCoord(4, 3))), Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(MovementTestData.Actor(attack).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        [Test]
        public void AttackFirstBlocksMoveAndSecondAttack()
        {
            var attack = CombatCommandCalculator.Calculate(CombatCommandTestData.Input(), Rules);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            var after = CombatCommandComposition.Project(attack);
            var move = CombatCommandCalculator.Calculate(MovementTestData.Route(after,
                new CellCoord(1, 3), new CellCoord(2, 3)), Rules);
            var second = CombatCommandCalculator.Calculate(after, Rules);
            Assert.That(move.Reason, Is.EqualTo(MovementFailureReason.ActionAlreadyUsed.ToString()));
            Assert.That(second.Reason, Is.EqualTo(AttackFailureReason.ActionAlreadyUsed.ToString()));
        }

        [Test]
        public void RejectedCommandPreservesActionUse()
        {
            var input = CombatCommandTestData.Input();
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.FreshCommand(input,
                CombatCommandKind.Attack, aim: AttackAim.ForCell(new CellCoord(0, 7))), Rules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState.SameValues(input.State), Is.True);
        }

        [Test]
        public void OwnTurnStartResetsActionUse()
        {
            var input = CombatCommandTestData.Input();
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.Owner == Owner.Player
                ? c.With(actionUse: UnitActionUse.Attacked) : c).ToArray());
            var next = CombatCommandTestData.NextPlayerTurn(input);
            Assert.That(next.Characters.Where(c => c.Owner == Owner.Player).All(c => c.ActionUse == UnitActionUse.Available), Is.True);
        }

        [TestCase(UnitActionUse.Available)]
        [TestCase(UnitActionUse.Moved)]
        [TestCase(UnitActionUse.Attacked)]
        public void EndPlayerTurnWithUnusedActions(UnitActionUse use)
        {
            var input = CombatCommandTestData.Input();
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.Owner == Owner.Player
                ? c.With(actionUse: use) : c).ToArray());
            var ended = CombatCommandCalculator.Calculate(CombatCommandTestData.FreshCommand(input, CombatCommandKind.EndPlayerTurn), Rules);
            Assert.That(ended.Accepted, Is.True, ended.Reason);
            Assert.That(ended.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingEnemy));
            var duplicate = CombatCommandCalculator.Calculate(CombatCommandTestData.FreshCommand(
                CombatCommandComposition.Project(ended), CombatCommandKind.EndPlayerTurn), Rules);
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(duplicate.FinalState.SameValues(ended.FinalState), Is.True);
        }

        [Test]
        public void EnemyActsWithoutBudgetAndTerminates()
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(2, 3), targetCell: new CellCoord(3, 3));
            input = CombatCommandTestData.With(input, input.Characters.Concat(new[] {
                CombatCommandTestData.Character(20, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy),
                CombatCommandTestData.Character(21, EntityKey.Unit_Demon, new CellCoord(3, 2), Owner.Enemy),
                CombatCommandTestData.Character(22, EntityKey.Unit_Demon, new CellCoord(3, 4), Owner.Enemy) }).ToArray());
            var actual = new System.Collections.Generic.List<EntityId>();
            for (int i = 0; i < 5; i++)
            {
                var result = CombatCommandCalculator.Advance(input, Rules);
                Assert.That(result.Accepted, Is.True, result.Reason);
                if (result.Attack.HasValue) actual.Add(result.Attack.Value.AttackerId);
                input = CombatCommandComposition.Project(result);
                if (result.Events.Any(e => e.Kind == CombatEventKind.EnemyPhaseEnded)) break;
            }
            Assert.That(actual, Is.EqualTo(new[] { new EntityId(1), new EntityId(20), new EntityId(21), new EntityId(22) }));
            Assert.That(input.State.Turn.ControllerPhase, Is.Not.EqualTo(CombatControllerPhase.Enemy));
        }

        private static CombatCommandTransition Move() => CombatCommandCalculator.Calculate(MovementTestData.Route(
            CombatCommandTestData.Input(), new CellCoord(1, 3), new CellCoord(2, 3)), Rules);
    }
}
