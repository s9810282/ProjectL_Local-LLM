using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class TargetingAngleLifecycleTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [Test]
        public void EnemyAi_UsesMinusYDespiteR0()
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(2, 2), targetCell: new CellCoord(2, 1));
            input = TargetingAngleIntegrationTests.WithMethod(input, EntityKey.Unit_Demon, TargetingAngleIntegrationTests.Method());
            var rear = CombatCommandTestData.Character(3, EntityKey.Unit_Akimbo, new CellCoord(2, 3), Owner.Player);
            input = CombatCommandTestData.With(input, input.Characters.Append(rear).ToArray());
            Assert.That(input.Characters[0].Rotation, Is.EqualTo(Rotation4.R0));
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.AttackAims.ToArray(), Is.EqualTo(new[] { AttackAim.ForCell(new CellCoord(2, 1)) }));
            var policy = new EnemyAiPolicy(new HostilityPolicy(), Rules);
            Assert.That(policy.TrySelectAttackAim(input, range, out var aim), Is.True);
            Assert.That(aim, Is.EqualTo(AttackAim.ForCell(new CellCoord(2, 1))));
            Assert.That(CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, aim), Rules).CanAttack, Is.True);
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
        }

        [Test]
        public void EnemyPathProjection_ChangesOriginNotForward()
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(2, 4), targetCell: new CellCoord(2, 0));
            input = TargetingAngleIntegrationTests.WithMethod(input, EntityKey.Unit_Demon, TargetingAngleIntegrationTests.Method());
            Assert.That(EnemyMovementPathfinder.TryFind(input, new EntityId(2), out var path), Is.True);
            Assert.That(path.Endpoint, Is.EqualTo(new CellCoord(2, 1)));
            var moved = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input, CombatCommandKind.Move, path: path), Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            var arrival = CombatCommandComposition.Intent(CombatCommandComposition.Project(moved), CombatCommandKind.Attack);
            var range = CombatCommandCalculator.GetUnitAttackRange(arrival);
            Assert.That(range.RangeCells.ToArray(), Has.Member(new CellCoord(2, 0)));
            Assert.That(range.RangeCells.ToArray(), Has.No.Member(new CellCoord(2, 2)));
            // 이동한 유닛도 공격 범위를 조회할 수 있다(설계 2026-10-27 이동 후 공격). 자금 부족은 범위 실패가 아니다.
            Assert.That(CombatCommandCalculator.GetAttackRange(arrival).FailureReason, Is.EqualTo(AttackFailureReason.None));
        }

        [TestCase(Rotation4.R0)]
        [TestCase(Rotation4.R90)]
        [TestCase(Rotation4.R180)]
        [TestCase(Rotation4.R270)]
        public void RotationAndMovement_DoNotChangeForward(Rotation4 rotation)
        {
            var input = CombatCommandTestData.Input(empty: true);
            input = TargetingAngleIntegrationTests.WithMethod(input, EntityKey.Unit_Akimbo, TargetingAngleIntegrationTests.Method());
            var actor = Rotate(input.Characters[0], rotation);
            input = CombatCommandTestData.With(input, new[] { actor, input.Characters[1] });
            AssertForward(input, actor, 1);
            var moved = CombatCommandCalculator.Calculate(MovementTestData.Route(input, new CellCoord(1, 3), new CellCoord(2, 3)), Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            var projected = CombatCommandComposition.Project(moved);
            var after = projected.Characters.Single(c => c.EntityId == actor.EntityId);
            Assert.That(after.Anchor, Is.EqualTo(new CellCoord(2, 3)));
            Assert.That(after.Rotation, Is.EqualTo(rotation));
            AssertForward(projected, after, 1);
        }

        [Test]
        public void ForcedMovement_PreservesEnemyForward()
        {
            var input = CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            var enemy = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy);
            input = CombatAttackQueries.WithAim(CombatCommandTestData.With(input,
                new[] { input.Characters[0], input.Characters[1], enemy }), AttackAim.ForCell(enemy.Anchor.Value));
            input = TargetingAngleIntegrationTests.WithMethod(input, EntityKey.Unit_Demon, TargetingAngleIntegrationTests.Method());
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var projected = CombatCommandComposition.Project(result);
            var after = projected.Characters.Single(c => c.EntityId == enemy.EntityId);
            Assert.That(after.Anchor, Is.EqualTo(new CellCoord(3, 3)));
            AssertForward(Activate(projected, after), after, -1);
        }

        [Test]
        public void DeferredCreatedEnemy_UsesItsOwnFactionForInformationalRange()
        {
            var input = CombatSummonTests.Input(new CellCoord(3, 3));
            input = TargetingAngleIntegrationTests.WithMethod(input, EntityKey.Unit_Gravekeeper, TargetingAngleIntegrationTests.Method());
            var actor = Rotate(CombatSummonTests.Enemy(11, new CellCoord(4, 3), EntityKey.Unit_Gravekeeper), Rotation4.R90)
                .With(actionUse: UnitActionUse.Deferred);
            actor = CombatCommandTestData.With(actor, category: CategorySnapshot.CreateStateless(CategoryKey.Rider));
            input = CombatCommandTestData.With(input, characters: input.Characters.Append(actor).ToArray());
            AssertForward(input, actor, -1);
            Assert.That(actor.ActionUse, Is.EqualTo(UnitActionUse.Deferred));
        }

        // 정보 조회 대상만 선택하며 제어자·행동 사용 상태는 바꾸지 않는다.
        private static CombatCommandInput Activate(CombatCommandInput input, CharacterCombatFacts actor) =>
            input.WithActor(actor.EntityId);

        private static CharacterCombatFacts Rotate(CharacterCombatFacts c, Rotation4 rotation) =>
            new CharacterCombatFacts(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole,
                c.Area, c.Health, c.Stats, c.ReserveSlotIndex, c.Anchor, rotation, c.OccupiedCells, c.ActionUse, c.TemporaryAttackBonus,
                c.TemporaryMaxHealthBonus,
                c.PositionModifier, c.HeldEnvironment, c.Debuffs);

        private static void AssertForward(CombatCommandInput input, CharacterCombatFacts actor, int y)
        {
            var range = CombatCommandCalculator.GetUnitAttackRange(input.WithActor(actor.EntityId));
            Assert.That(range.Succeeded, Is.True, range.FailureReason.ToString());
            var origin = actor.Anchor.Value;
            Assert.That(range.RangeCells.ToArray(), Has.Member(new CellCoord(origin.X, origin.Y + y)));
            Assert.That(range.RangeCells.ToArray(), Has.No.Member(new CellCoord(origin.X, origin.Y - y)));
            Assert.That(range.RangeCells.ToArray(), Has.No.Member(new CellCoord(origin.X + 1, origin.Y)));
        }
    }
}

