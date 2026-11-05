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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
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
        [TestCase(CategoryKey.Main)]
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

        [TestCase(CategoryKey.Main)]
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
            var snapshot = CategorySnapshot.CreateStateless(category);
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
                input.Souls, input.Artifacts,
                expectedBattleInstanceId: input.ExpectedBattleInstanceId, expectedControllerTurnSequence: input.ExpectedControllerTurnSequence,
                battleInstanceId: input.BattleInstanceId), System.Array.Empty<IMovementTileRule>()).Targets.ToArray().Single().TargetId, Is.EqualTo(new EntityId(2)));
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
            var state = input.State.With(structures: new[] { hq });
            input = CombatCommandTestData.With(input, chars, state);
            Assert.That(new EnemyAiPolicy(new HostilityPolicy(), System.Array.Empty<IMovementTileRule>()).TrySelectAttackAim(input,
                CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
            Assert.That(aim.Cell, Is.EqualTo(new CellCoord(5, 3)));
        }

        [Test]
        public void Facts_BadgeBreakIsValidAttackWhenOnlyTarget()
        {
            var input = BadgeInput(20, BadgeDemon(2, new CellCoord(2, 3)));
            Assert.That(new EnemyAiPolicy(new HostilityPolicy(), System.Array.Empty<IMovementTileRule>()).TrySelectAttackAim(input,
                CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
            Assert.That(aim.Cell, Is.EqualTo(new CellCoord(2, 3)));
        }

        [Test]
        public void Facts_ActualHpLossPrecedesBadgeBreak()
        {
            // 칸 동점이면 x가 작은 (2,3)이 이긴다. 실제 피해가 있는 (5,3)이 골라져야 한다.
            var input = BadgeInput(20, BadgeDemon(2, new CellCoord(2, 3)),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(5, 3), Owner.Enemy));
            Assert.That(new EnemyAiPolicy(new HostilityPolicy(), System.Array.Empty<IMovementTileRule>()).TrySelectAttackAim(input,
                CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
            Assert.That(aim.Cell, Is.EqualTo(new CellCoord(5, 3)));
        }

        [Test]
        public void Facts_ZeroDamageLegalTargetStillAttacks()
        {
            // 합법적인 대상이면 피해 0도 공격 후보이며 예산과 별개다.
            var input = BadgeInput(0, BadgeDemon(2, new CellCoord(2, 3)));
            Assert.That(new EnemyAiPolicy(new HostilityPolicy(), System.Array.Empty<IMovementTileRule>()).TrySelectAttackAim(input,
                CombatCommandCalculator.GetAttackRange(input), out _), Is.True);
        }

        private static CharacterCombatFacts BadgeDemon(int id, CellCoord cell) =>
            HeldEnvironmentEffects.AcquireBadge(CombatCommandTestData.Character(id, EntityKey.Unit_Demon, cell, Owner.Enemy), 41);

        private static CombatCommandInput BadgeInput(int attackPower, params CharacterCombatFacts[] targets)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, empty: true);
            var actor = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Player, attackPower);
            return CombatCommandTestData.With(input, new[] { actor, input.Characters[1] }.Concat(targets).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, attackPower), method) : p).ToArray());
        }

        [Test]
        public void RejectedAttackOnFartherTarget_FallsBackToPursuitOfNearestHostile()
        {
            // 적이 모르는 모래폭풍이 사거리를 3에서 2로 줄인다. 직선 공격은 거리 3의 (4,1)을 고르지만 실제로 거절되고,
            // 가장 가까운(맨해튼 2) 대각선 (2,2)는 직선 밖이라 추격 이동으로 넘어간다.
            var input = HiddenSandstormInput(new CellCoord(4, 1), new CellCoord(2, 2));
            var range = CombatCommandCalculator.GetAttackRange(input);
            var policy = new EnemyAiPolicy(new HostilityPolicy(), Array.Empty<IMovementTileRule>());
            Assert.That(policy.TrySelectAttackAim(input, range, out var aim), Is.True);
            Assert.That(aim.Cell, Is.EqualTo(new CellCoord(4, 1)));
            Assert.That(CombatCommandCalculator.Calculate(CombatAttackQueries.WithAim(input, aim),
                Array.Empty<IMovementTileRule>()).Accepted, Is.False);
            Assert.That(policy.TrySelectPursuitTarget(input, out var pursuit), Is.True);
            Assert.That(pursuit, Is.EqualTo(new EntityId(3)));

            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(MovementTestData.Actor(result).Anchor, Is.EqualTo(new CellCoord(2, 1)));
            Assert.That(MovementTestData.Actor(result).ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.Attacked), Is.Zero);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.Zero);
        }

        [Test]
        public void RejectedAttackOnNearestTarget_FindsNoPursuitAndEndsPhaseWithoutChangingUnit()
        {
            // 거절된 조준이 가장 가까운 대상이면 경로 탐색도 같은 공개 사실로 "이미 공격 위치"라 판단해 이동하지 않는다.
            var input = HiddenSandstormInput(new CellCoord(4, 1));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            Assert.That(EnemyMovementPathfinder.TryFind(input, new EntityId(2), out _), Is.False);

            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(result.PathMovement, Is.Null);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.Attacked), Is.Zero);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.EqualTo(1));
            var final = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == actor.EntityId);
            Assert.That(final.Anchor, Is.EqualTo(actor.Anchor));
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(actor.Health.CurrentHealth));
        }

        private static CombatCommandInput HiddenSandstormInput(CellCoord farTarget, CellCoord? nearTarget = null)
        {
            var method = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = EnemyMovementPathTests.Input(targetCell: farTarget);
            var characters = input.Characters.Select(c => c.EntityId == input.ActorId
                ? CombatCommandTestData.With(c, appointRange: 3) : c);
            if (nearTarget.HasValue)
                characters = characters.Append(CombatCommandTestData.Character(3, EntityKey.Unit_Akimbo, nearTarget.Value, Owner.Player));
            var actorKey = input.Characters.Single(c => c.EntityId == input.ActorId).CharacterKey;
            var all = characters.ToArray();
            input = CombatCommandTestData.With(input, characters: all,
                profiles: input.Profiles.Select(p => p.Definition.Key == actorKey
                    ? new CombatAttackProfile(p.Definition, method) : p).ToArray());
            var owned = all.Where(c => c.OwnedId.HasValue).ToArray();
            input = input.WithPersistentFacts(owned.Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey,
                c.OwnershipRole.Value)).ToArray(), input.ItemInventory, input.GoldBalance, input.FameBalance);
            input = input.WithWorld(input.State, input.Characters, input.Main, owned.ToDictionary(c => c.OwnedId.Value, _ => 0),
                input.Souls, input.EnvironmentBindings);
            return HighGroundMovementTests.TerrainInput(input, new StaticTerrainFact(11, new CellCoord(1, 1),
                StaticTerrainKind.Sandstorm, true, new[] { Owner.Player }));
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
