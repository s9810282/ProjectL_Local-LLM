using NUnit.Framework;
using System.Linq;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Map;

namespace ProjectL.Tests.Core
{
    public sealed class StageEnemyAiTurnTests
    {
        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        public void NonCannon_FriendlyOnlyDirectionIsNotSelectable(CategoryKey category)
        {
            var input = DirectionInput(category);
            input = CombatCommandTestData.With(input, characters: input.Characters.Where(c => c.EntityId != new EntityId(2)).ToArray());
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.Succeeded, Is.True);
            Assert.That(range.AttackAims.ToArray(), Has.No.Member(AttackAim.ForDirection(AttackDirection.Right)));
            var preview = CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input,
                AttackAim.ForDirection(AttackDirection.Right)), Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanAttack, Is.False);
            Assert.That(preview.FailureReason, Is.EqualTo(AttackFailureReason.NoEligibleTargets));
            Assert.That(new EnemyAiPolicy(new HostilityPolicy(), Array.Empty<IMovementTileRule>())
                .TrySelectAttackAim(input, range, out _), Is.False);
        }

        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        public void NonCannon_MixedDirectionFiltersAlliesFromAiPreviewAndExecution(CategoryKey category)
        {
            var input = DirectionInput(category);
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(new EnemyAiPolicy(new HostilityPolicy(), Array.Empty<IMovementTileRule>())
                .TrySelectAttackAim(input, range, out var aim), Is.True);
            Assert.That(aim, Is.EqualTo(AttackAim.ForDirection(AttackDirection.Right)));
            var attack = CombatAttackQueries.WithAim(input, aim);
            var preview = CombatCommandCalculator.Preview(attack, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Targets.ToArray().Select(t => t.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
            var result = CombatCommandCalculator.Calculate(attack, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(result.Characters.Characters.Any(c => c.Final.EntityId == new EntityId(3)), Is.False);
        }

        [Test]
        public void Cannon_AlliedCollateralMakesAiPreferCleanDirection()
        {
            var input = DirectionInput(CategoryKey.Cannon);
            var dirtyAim = AttackAim.ForDirection(AttackDirection.Right);
            var policy = new EnemyAiPolicy(new HostilityPolicy(), Array.Empty<IMovementTileRule>());
            Assert.That(policy.TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out var onlyAim), Is.True);
            Assert.That(onlyAim, Is.EqualTo(dirtyAim));
            var dirty = CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, dirtyAim), Array.Empty<IMovementTileRule>());
            Assert.That(dirty.Targets.ToArray().Select(t => t.TargetId), Is.EquivalentTo(new[] { new EntityId(2), new EntityId(3) }));
            Assert.That(dirty.Targets.ToArray().Single(t => t.TargetId == new EntityId(3)).TargetHealthAfter, Is.LessThan(100));

            // 오른쪽은 적 둘을 맞히지만 아군도 맞힌다. 위쪽의 적 한 명만 맞히는 공격이 우선이다.
            input = CombatCommandTestData.With(input, characters: input.Characters.Concat(new[] {
                CombatCommandTestData.Character(4, EntityKey.Unit_Akimbo, new CellCoord(3, 2), Owner.Player),
                CombatCommandTestData.Character(5, EntityKey.Unit_Akimbo, new CellCoord(0, 4), Owner.Player) }).ToArray());
            Assert.That(policy.TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
            Assert.That(aim, Is.EqualTo(AttackAim.ForDirection(AttackDirection.Up)));
            var clean = CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, aim), Array.Empty<IMovementTileRule>());
            Assert.That(clean.Targets.ToArray().Select(t => t.TargetId), Is.EqualTo(new[] { new EntityId(5) }));
        }

        private static CombatCommandInput DirectionInput(CategoryKey category)
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(0, 2), targetCell: new CellCoord(4, 2));
            var snapshot = category == CategoryKey.Rider ? CategorySnapshot.CreateRider(false)
                : category == CategoryKey.Heavy ? CategorySnapshot.CreateHeavy(Rotation4.R90, false, 50)
                : CategorySnapshot.CreateStateless(category);
            var actor = CombatCommandTestData.With(input.Characters[0], category: snapshot, appointRange: 4);
            var ally = CombatCommandTestData.Character(3, EntityKey.Unit_Akimbo, new CellCoord(2, 2), Owner.Enemy);
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            return CombatCommandTestData.With(input, characters: new[] { actor, input.Characters[1], input.Characters[2], ally },
                profiles: input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 10, category), method) : p).ToArray());
        }

        [Test]
        public void Facts_OverlappingDirectionGroupsCompeteAndUpWinsEqualScore()
        {
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method);
            input = CombatCommandTestData.With(input, new[] { input.Characters[0], input.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 5), Owner.Enemy) });
            var policy = new EnemyAiPolicy(new HostilityPolicy(), System.Array.Empty<IMovementTileRule>());
            Assert.That(policy.TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
            Assert.That(aim.Kind, Is.EqualTo(AttackAimKind.Direction));
            Assert.That(aim.Direction, Is.EqualTo(AttackDirection.Up));
        }

        [Test]
        public void Facts_IndirectSplashScoresVictimOutsideDirectRange()
        {
            var input = CannonInput(CombatCommandTestData.Input(empty: true));
            input = CombatCommandTestData.With(input, input.Characters.Concat(new[] {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(6, 3), Owner.Enemy) }).ToArray());
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.InRangeTargetIds.Length, Is.Zero);
            Assert.That(new EnemyAiPolicy(new HostilityPolicy(), System.Array.Empty<IMovementTileRule>()).TrySelectAttackAim(input, range, out var aim), Is.True);
            Assert.That(CombatCommandCalculator.Preview(new CombatCommandInput(input.Number, input.Kind, input.ActorId,
                aim, input.State, input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts,
                input.Souls, input.Artifacts), System.Array.Empty<IMovementTileRule>()).Targets.ToArray().Single().TargetId, Is.EqualTo(new EntityId(2)));
        }

        [Test]
        public void Facts_AlliedUnitZeroDamageHitStillLosesToCleanSingle()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 0);
            var input = CannonInput(CombatCommandTestData.Input(method, empty: true));
            input = CombatCommandTestData.With(input, input.Characters.Concat(new[] {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(5, 5), Owner.Enemy),
                CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Player) }).ToArray());
            Assert.That(new EnemyAiPolicy(new HostilityPolicy(), System.Array.Empty<IMovementTileRule>()).TrySelectAttackAim(input,
                CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
            Assert.That(aim.Cell, Is.EqualTo(new CellCoord(5, 5)));
        }

        [Test]
        public void Facts_HostileUnitPriorityAndActualHpLossPrecedeCellTie()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, empty: true);
            var chars = input.Characters.Concat(new[] {
                CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy), hp: 1),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(5, 3), Owner.Enemy) }).ToArray();
            var hq = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 1, new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(100, 100),
                new CellCoord(1, 2), Rotation4.R0, new[] { new CellCoord(1, 2) });
            var state = new CombatCommandState(8, 8, input.State.Phase, input.ActorId, false,
                new[] { hq }, input.State.ThornFields, input.State.Timeline);
            input = CombatCommandTestData.With(input, chars, state);
            Assert.That(new EnemyAiPolicy(new HostilityPolicy(), System.Array.Empty<IMovementTileRule>()).TrySelectAttackAim(input,
                CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
            Assert.That(aim.Cell, Is.EqualTo(new CellCoord(5, 3)));
        }

        [Test]
        public void Facts_PursuitUsesNearestHostileUnitAndProducesExplicitRoute()
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(0, 1), targetCell: new CellCoord(3, 1));
            var policy = new EnemyAiPolicy(new HostilityPolicy(), System.Array.Empty<IMovementTileRule>());
            Assert.That(policy.TrySelectPursuitTarget(input, out var target), Is.True);
            Assert.That(target, Is.EqualTo(new EntityId(2)));
            Assert.That(EnemyMovementPathfinder.TryFind(input, target, out var path), Is.True);
            Assert.That(path.Cells, Is.EqualTo(new[] { new CellCoord(0, 1), new CellCoord(1, 1), new CellCoord(2, 1) }));
        }

        private static CombatCommandInput CannonInput(CombatCommandInput input)
        {
            var actor = CombatCommandTestData.With(input.Characters[0], category: CategorySnapshot.CreateStateless(CategoryKey.Cannon));
            return CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, actor.Stats.AttackPower, CategoryKey.Cannon), p.Method) : p).ToArray());
        }
    }
}
