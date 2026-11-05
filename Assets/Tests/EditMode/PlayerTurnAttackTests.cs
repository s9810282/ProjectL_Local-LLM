using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class PlayerTurnAttackTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [Test]
        public void TwoDifferentCannonsEachConsumeTheirOwnAction()
        {
            var input = CannonInput();
            var first = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(first.Accepted, Is.True, first.Reason);
            Assert.That(MovementTestData.Actor(first).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            var after = CombatCommandComposition.Project(first).WithActor(new EntityId(10));
            Assert.That(after.Characters.Single(c => c.EntityId == after.ActorId).ActionUse, Is.EqualTo(UnitActionUse.Available));
            var second = CombatCommandCalculator.Calculate(after.WithIntent(CombatCommandKind.Attack,
                AttackAim.ForCell(new CellCoord(4, 3)), default, null, null), Rules);
            Assert.That(second.Accepted, Is.True, second.Reason);
            Assert.That(MovementTestData.Actor(second).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).ActionUse, Is.EqualTo(UnitActionUse.Available));
        }

        [Test]
        public void CannonAttackHasNoBudgetAdmission()
        {
            var input = CannonInput();
            input = CombatCommandTestData.With(input, state: input.State.With());
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.ActionUseSettlement.FinalActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(result.FinalState.TraitTurn.PlayerAttackActionCount,
                Is.EqualTo(input.State.TraitTurn.PlayerAttackActionCount + 1));
            Assert.That(result.FinalState.TraitTurn.EnemyAttackActionCount, Is.EqualTo(input.State.TraitTurn.EnemyAttackActionCount));
            Assert.That(result.FinalState.With(traitTurn: input.State.TraitTurn).SameValues(input.State), Is.True);
            Assert.That(input.State.TraitTurn.PlayerAttackActionCount, Is.Zero);
            Assert.That(result.Attack.HasValue, Is.True);
            Assert.That(result.Events, Is.Not.Empty);
            Assert.That(CombatCommandCalculator.GetActionAvailability(input).CanAttack, Is.True);
        }

        [Test]
        public void AttackThenAttackOrMoveRejectsAndOtherUnitStaysAvailable()
        {
            var first = CombatCommandCalculator.Calculate(CombatCommandTestData.Input(), Rules);
            Assert.That(first.Accepted, Is.True, first.Reason);
            var after = CombatCommandComposition.Project(first);
            var attack = CombatCommandCalculator.Calculate(after, Rules);
            var move = CombatCommandCalculator.Calculate(MovementTestData.Route(after,
                new CellCoord(1, 3), new CellCoord(2, 3)), Rules);
            Assert.That(attack.Reason, Is.EqualTo(AttackFailureReason.ActionAlreadyUsed.ToString()));
            Assert.That(move.Reason, Is.EqualTo(MovementFailureReason.ActionAlreadyUsed.ToString()));
            Assert.That(attack.Accepted || move.Accepted, Is.False);
            Assert.That(attack.Events.Concat(move.Events), Is.Empty);
            Assert.That(after.Characters.Single(c => c.EntityId == new EntityId(10)).ActionUse, Is.EqualTo(UnitActionUse.Available));
            var availability = CombatCommandCalculator.GetActionAvailability(after);
            Assert.That(availability.CanMove || availability.CanAttack, Is.False);
            Assert.That(availability.MoveFailure, Is.EqualTo(MovementFailureReason.ActionAlreadyUsed));
            Assert.That(availability.AttackFailure, Is.EqualTo(AttackFailureReason.ActionAlreadyUsed));
        }

        [Test]
        public void LegalZeroDamageConsumesAction()
        {
            var input = CombatCommandTestData.Input();
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var stats = actor.Stats.With(attackPower: 0, effectiveAttackPower: 0);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == actor.EntityId
                ? c.With(stats: stats) : c).ToArray());
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().All(h => h.TargetHealthBefore == h.TargetHealthAfter), Is.True);
            Assert.That(MovementTestData.Actor(result).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        [Test]
        public void MissingMandatoryTargetDoesNotConsumeAction()
        {
            var input = CombatCommandTestData.Input(empty: true);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(AttackFailureReason.InvalidPrimaryTarget.ToString()));
            Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).ActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void UsedUnitStillHasInspectionGeometry()
        {
            var first = CombatCommandCalculator.Calculate(CombatCommandTestData.Input(), Rules);
            var after = CombatCommandComposition.Project(first);
            Assert.That(CombatCommandCalculator.GetActionAvailability(after).CanAttack, Is.False);
            Assert.That(CombatCommandCalculator.GetUnitAttackRange(after).Succeeded, Is.True);
        }

        [Test]
        public void EnemyZeroDamageAttackConsumesAction()
        {
            var source = CombatCommandTestData.Input();
            var enemy = source.Characters.Single(c => c.EntityId == new EntityId(2));
            enemy = enemy.With(stats: enemy.Stats.With(attackPower: 0, effectiveAttackPower: 0));
            var state = source.State.With(phase: CombatFlowPhase.Automatic,                turn: new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy,
                    controllerTurnSequence: 2));
            var input = new CombatCommandInput(0, CombatCommandKind.Attack, enemy.EntityId,
                AttackAim.ForCell(new CellCoord(1, 3)), state,
                source.Characters.Select(c => c.EntityId == enemy.EntityId ? enemy : c).ToArray(),
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                processingKind: CombatProcessingKind.Advance, battleInstanceId: source.BattleInstanceId,
                expectedBattleInstanceId: source.BattleInstanceId, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(result.Attack.Value.Hits.ToArray().All(h => h.DamageDealt == 0), Is.True);
        }

        private static CombatCommandInput CannonInput()
        {
            var input = CombatCommandTestData.Input(category: CategoryKey.Cannon);
            var characters = input.Characters.Select(c => c.EntityId == new EntityId(10)
                ? CombatCommandTestData.With(c, category: CategorySnapshot.CreateStateless(CategoryKey.Cannon),
                    position: new CellCoord(1, 4)) : c).ToArray();
            var profiles = input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_MainCharacter
                ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_MainCharacter, 1, CategoryKey.Cannon), p.Method) : p).ToArray();
            return CombatCommandTestData.With(input, characters: characters, profiles: profiles);
        }
    }
}
