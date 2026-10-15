using System;
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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    public sealed class WallAttackAiTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [Test]
        public void EnemyAcrossWall_PursuesGenuinelyClearPosition_ThenAttacks()
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(1, 1), targetCell: new CellCoord(3, 1));
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == input.ActorId
                ? CombatCommandTestData.With(c, appointRange: 4) : c.OwnershipRole == CharacterOwnershipRole.Main
                    ? CombatCommandTestData.With(c, position: new CellCoord(4, 1)) : c).ToArray());
            input = WallMovementTests.WithWalls(input, WallStateLifecycleTests.Wall(71, new CellCoord(2, 1)));
            var aim = AttackAim.ForCell(new CellCoord(3, 1));
            var blocked = CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, aim), Rules);
            Assert.That(blocked.FailureReason, Is.EqualTo(AttackFailureReason.BlockedByWall));
            Assert.That(EnemyMovementPathfinder.TryFind(input, new EntityId(2), out var path), Is.True);
            Assert.That(path.Endpoint, Is.EqualTo(new CellCoord(2, 0)));
            Assert.That(path.Cells, Has.No.Member(new CellCoord(2, 1)));
            var move = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input, CombatCommandKind.Move, path: path), Rules);
            Assert.That(move.Accepted, Is.True, move.Reason);
            var attack = CombatCommandComposition.Intent(CombatCommandComposition.Project(move), CombatCommandKind.Attack, aim);
            var preview = CombatCommandCalculator.Preview(attack, Rules);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.BlockedContacts, Is.Empty);
            Assert.That(preview.Targets.ToArray().Single().TargetId, Is.EqualTo(new EntityId(2)));
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(result.Attack.HasValue, Is.True);
            Assert.That(result.Attack.Value.Hits.ToArray().Any(h => h.TargetId == new EntityId(2) && h.DamageDealt > 0), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PlayerAndEnemy_UseSameWallAdmissionAndEvidence(bool player)
        {
            var input = player ? CombatCommandTestData.Input(empty: true) : EnemyMovementPathTests.Input();
            var actor = CombatCommandTestData.With(input.Characters[0], position: new CellCoord(1, 1), appointRange: 4);
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Akimbo, new CellCoord(3, 1), player ? Owner.Enemy : Owner.Player);
            input = CombatCommandTestData.With(input, new[] { actor, target, input.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main) });
            input = WallMovementTests.WithWalls(input, WallStateLifecycleTests.Wall(71, new CellCoord(2, 1)));
            input = CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(3, 1)));
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.AttackAims.ToArray(), Has.No.Member(input.Aim));
            var preview = CombatCommandCalculator.Preview(input, Rules);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(preview.FailureReason, Is.EqualTo(AttackFailureReason.BlockedByWall));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Events, Is.Empty);
            Assert.That(preview.BlockedContacts.Single().WallSourceIds, Is.EqualTo(new long[] { 71 }));
            Assert.That(result.BlockedContacts.Single().Origin, Is.EqualTo(actor.Anchor));
        }

        [TestCase(false, 1, true)]
        [TestCase(false, 2, true)]
        [TestCase(true, 1, true)]
        [TestCase(true, 2, false)]
        public void FinitePierceGoal_AgreesWithPreviewAndAuthoredWallPolicy(bool ignoreWalls, int cap, bool needsMove)
        {
            var input = EnemyMovementPathTests.Input(cost: 0, actorCell: new CellCoord(0, 2), targetCell: new CellCoord(4, 2));
            var actor = CombatCommandTestData.With(input.Characters[0], appointRange: 4,
                category: CategorySnapshot.CreateStateless(CategoryKey.Cannon),
                traits: new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) });
            var spec = CombatCommandTestData.Spec(actor.CharacterKey, 10, CategoryKey.Cannon, new PiercingFalloffTraitSpec(30, cap));
            var policy = new AttackOcclusionSpec(ignoreWalls ? AttackWallDelivery.IgnoreWalls : AttackWallDelivery.RequireClear,
                AttackWallPropagation.FromImpact);
            var definition = new UnitSpec(spec.Key, spec.Role, 1, 1, new[] { CellCoord.Zero }, spec.MaxHealth,
                spec.MoveMethod, spec.MoveRange, spec.MoveCost, 4, spec.CategoryKey, spec.AttackPower, spec.MaxActionCost,
                spec.AttackCost, spec.PhysicalDefense, spec.Priority, spec.Delay, true, spec.Traits.ToArray(), spec.OwnerEligibility, policy);
            var ally = CombatCommandTestData.Character(3, EntityKey.Unit_Akimbo, new CellCoord(2, 2), Owner.Enemy);
            input = CombatCommandTestData.With(input, new[] { actor, input.Characters[1], input.Characters[2], ally },
                profiles: input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey ? new CombatAttackProfile(definition,
                    new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0)) : p).ToArray());
            input = WallMovementTests.WithWalls(input, WallStateLifecycleTests.Wall(71, new CellCoord(3, 2)));
            var aim = AttackAim.ForDirection(AttackDirection.Right);
            var preview = CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, aim), Rules);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Targets.ToArray().Any(t => t.TargetId == new EntityId(2)), Is.EqualTo(!needsMove));
            Assert.That(EnemyMovementPathfinder.TryFind(input, new EntityId(2), out var path), Is.EqualTo(needsMove));
            if (!needsMove) return;
            Assert.That(path.Cells, Has.No.Member(new CellCoord(3, 2)));
            Assert.That(MovementRouteValidator.Validate(input, path, out _), Is.EqualTo(MovementFailureReason.None));
            var moved = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input, CombatCommandKind.Move, path: path), Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            var after = CombatCommandComposition.Project(moved);
            var range = CombatCommandCalculator.GetAttackRange(CombatCommandComposition.Intent(after, CombatCommandKind.Attack));
            Assert.That(range.AttackAims.ToArray().Any(a => CombatCommandCalculator.Preview(
                CombatCommandComposition.Intent(after, CombatCommandKind.Attack, a), Rules).Targets.ToArray().Any(t => t.TargetId == new EntityId(2))), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void IgnoringAttackWalls_DoesNotAllowPursuitThroughOccupiedWallCells(bool ignore)
        {
            var input = EnemyMovementPathTests.Input(cost: 0, actorCell: new CellCoord(0, 1), targetCell: new CellCoord(4, 1));
            var profiles = input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Demon ? new CombatAttackProfile(
                CopyPolicy(p.Definition, new AttackOcclusionSpec(ignore ? AttackWallDelivery.IgnoreWalls : AttackWallDelivery.RequireClear,
                    AttackWallPropagation.FromImpact)), p.Method) : p).ToArray();
            input = CombatCommandTestData.With(input, profiles: profiles);
            input = WallMovementTests.WithWalls(input, WallStateLifecycleTests.Wall(71, new CellCoord(2, 1)));
            Assert.That(EnemyMovementPathfinder.TryFind(input, new EntityId(2), out var path), Is.True);
            Assert.That(path.Cells, Has.No.Member(new CellCoord(2, 1)));
            Assert.That(MovementRouteValidator.Validate(input, path, out _), Is.EqualTo(MovementFailureReason.None));
        }

        private static UnitSpec CopyPolicy(UnitSpec s, AttackOcclusionSpec policy) => new UnitSpec(s.Key, s.Role,
            s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(), s.MaxHealth, s.MoveMethod, s.MoveRange,
            s.MoveCost, 4, s.CategoryKey, s.AttackPower, s.MaxActionCost, s.AttackCost, s.PhysicalDefense, s.Priority, s.Delay,
            true, s.Traits.ToArray(), s.OwnerEligibility, policy);
    }
}
