using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 적 AI도 이동 → 공격을 한다(설계 2026-10-27 이동 후 공격 Q2 B).
    /// Advance 한 번에 행동 하나만 나오므로, 이동한 적은 다음 Advance에서 같은 순회가 다시 골라 공격만 시도한다.
    /// </summary>
    public sealed class EnemyMoveThenAttackTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();
        private static readonly EntityId Enemy = new EntityId(1);
        private static readonly EntityId Target = new EntityId(2);

        [Test]
        public void PursuitEndingInRange_IsFollowedByAttackOfTheSameEnemy()
        {
            var input = EnemyMovementPathTests.Input(actorCell: CellCoord.Zero, targetCell: new CellCoord(3, 0));
            var moved = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(moved.PathMovement, Is.Not.Null);
            Assert.That(moved.Attack.HasValue, Is.False);
            Assert.That(Final(moved, Enemy).ActionUse, Is.EqualTo(UnitActionUse.Moved));

            var attacked = CombatCommandCalculator.Advance(CombatCommandComposition.Project(moved), Rules);
            Assert.That(attacked.Accepted, Is.True, attacked.Reason);
            Assert.That(attacked.PathMovement, Is.Null);
            Assert.That(attacked.Attack.HasValue, Is.True);
            Assert.That(attacked.Attack.Value.AttackerId, Is.EqualTo(Enemy));
            Assert.That(attacked.Attack.Value.Hits.ToArray().Any(h => h.TargetId == Target), Is.True);
            var settlement = attacked.Attack.Value.ActionUseSettlement;
            Assert.That((settlement.BeforeActionUse, settlement.FinalActionUse),
                Is.EqualTo((UnitActionUse.Moved, UnitActionUse.Attacked)));
            Assert.That(attacked.Events.Any(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.False);
        }

        [Test]
        public void PursuitEndingOutOfRange_SkipsTheMovedEnemyWithoutMovingAgain()
        {
            // 알려진 지뢰가 지름길을 막아 허용 거리 안에서는 사거리에 닿지 못한다(EnemyMovementPathTests.SafeGoalBeyondAllowance).
            var input = EnemyMovementPathTests.Input(category: CategoryKey.Main, environment: EnemyMovementPathTests.Maze());
            var moved = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(moved.PathMovement, Is.Not.Null);

            var next = CombatCommandCalculator.Advance(CombatCommandComposition.Project(moved), Rules);
            Assert.That(next.Accepted, Is.True, next.Reason);
            Assert.That(next.PathMovement, Is.Null);
            Assert.That(next.Attack.HasValue, Is.False);
            Assert.That(next.Events.Any(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.True);
        }

        [Test]
        public void MovedEnemyAttacksWithoutBudgetAdmission()
        {
            var input = EnemyMovementPathTests.Input(actorCell: CellCoord.Zero, targetCell: new CellCoord(3, 0));
            input = CombatCommandTestData.With(input, state: input.State.With());
            var moved = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(moved.PathMovement, Is.Not.Null);

            var next = CombatCommandCalculator.Advance(CombatCommandComposition.Project(moved), Rules);
            Assert.That(next.Accepted, Is.True, next.Reason);
            Assert.That(next.Attack.HasValue, Is.True);
            Assert.That(next.PathMovement, Is.Null);
            Assert.That(next.Events.Any(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.False);
            Assert.That(Final(next, Enemy).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        [Test]
        public void LowerIdEnemyFinishesMoveThenAttack_BeforeTheNextEnemyActs()
        {
            var input = EnemyMovementPathTests.Input(actorCell: CellCoord.Zero, targetCell: new CellCoord(3, 0));
            var second = CombatCommandTestData.With(CombatCommandTestData.Character(4, EntityKey.Unit_Demon,
                new CellCoord(4, 1), Owner.Enemy, 10), appointRange: 1);
            input = CombatCommandTestData.With(input, input.Characters.Append(second).ToArray());

            var first = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(first.Accepted, Is.True, first.Reason);
            Assert.That(first.PathMovement, Is.Not.Null);
            Assert.That(first.ActionUseSettlement.ActorId, Is.EqualTo(Enemy));

            var followUp = CombatCommandCalculator.Advance(CombatCommandComposition.Project(first), Rules);
            Assert.That(followUp.Accepted, Is.True, followUp.Reason);
            Assert.That(followUp.Attack.HasValue, Is.True);
            Assert.That(followUp.Attack.Value.AttackerId, Is.EqualTo(Enemy));

            var third = CombatCommandCalculator.Advance(CombatCommandComposition.Project(followUp), Rules);
            Assert.That(third.Accepted, Is.True, third.Reason);
            Assert.That(third.Attack.HasValue, Is.True);
            Assert.That(third.Attack.Value.AttackerId, Is.EqualTo(second.EntityId));
        }

        private static CharacterCombatFacts Final(CombatCommandTransition result, EntityId id) =>
            CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == id);
    }
}
