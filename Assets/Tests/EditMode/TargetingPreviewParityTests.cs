using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class TargetingPreviewParityTests
    {
        [Test]
        public void DirectionWithZeroDamageVictim_RemainsAdmissibleButAiDoesNotScoreIt()
        {
            var method = new TargetingMethodSpec(TargetingShape.Fan, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right));
            var chars = input.Characters.ToArray();
            var c = chars[0]; var s = c.Stats;
            var stats = new UnitStatsSnapshot(s.MoveMethod, s.MoveRange, s.MoveCost, s.AppointRange,
                0, s.PhysicalDefense, s.MaxActionCost, s.AttackCost, s.RemainingMoveRange, s.CurrentActionCost,
                s.Priority, s.Delay, s.TraitStatuses.ToArray(), s.Category);
            chars[0] = new CharacterCombatFacts(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole,
                c.Area, c.Health, stats, c.ReserveSlotIndex, c.Anchor, c.Rotation, c.OccupiedCells,
                c.MovementCheckpointAnchor, c.CommittedMoveDistance, c.PendingMoveActionCost);
            input = CombatCommandTestData.With(input, chars);
            Assert.That(CombatCommandCalculator.GetAttackRange(input).AttackAims.ToArray().Any(a => a.Direction == AttackDirection.Right), Is.True);
            Assert.That(CombatCommandCalculator.Preview(input, System.Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
            Assert.That(CombatCommandCalculator.Preview(input, System.Array.Empty<IMovementTileRule>()).Targets.ToArray().All(t => t.ExpectedDamage == 0), Is.True);
            Assert.That(new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi.EnemyAiPolicy(
                new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat.HostilityPolicy(), System.Array.Empty<IMovementTileRule>())
                .TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out _), Is.False);
        }

        [TestCase(AttackDirection.Up)]
        [TestCase(AttackDirection.Right)]
        [TestCase(AttackDirection.Down)]
        [TestCase(AttackDirection.Left)]
        public void BallistaPreview_AllCardinalGroupsRetainHalfUpFalloff(AttackDirection direction)
        {
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(direction), empty: true);
            var baseActor = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(3, 3), Owner.Player, 21);
            var actor = CombatCommandTestData.With(baseActor, category: CategorySnapshot.CreateStateless(CategoryKey.Cannon),
                traits: new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) });
            var step = direction == AttackDirection.Up ? CellCoord.Up : direction == AttackDirection.Right ? CellCoord.Right :
                direction == AttackDirection.Down ? CellCoord.Down : CellCoord.Left;
            var chars = new[] { actor, input.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3 + step.X * 2, 3 + step.Y * 2), Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(3 + step.X * 3, 3 + step.Y * 3), Owner.Enemy) };
            var profiles = input.Profiles.ToArray(); profiles[0] = new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 21, CategoryKey.Cannon, new PiercingFalloffTraitSpec(30, 99)), method);
            input = CombatCommandTestData.With(input, chars, profiles: profiles);
            var preview = CombatCommandCalculator.Preview(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(preview.Targets.ToArray().Where(t => t.TargetId != input.Characters[1].EntityId)
                .Select(t => t.ExpectedDamage), Is.EqualTo(new[] { 21, 15 }));
            Assert.That(preview.Targets.ToArray().Select(t => t.TargetHealthAfter), Is.EqualTo(
                CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()).Attack.Value.Hits.ToArray().Select(t => t.TargetHealthAfter)));
            Assert.That(preview.PrimaryTargetId.IsEmpty, Is.True);
        }

        [TestCase(TargetingChoiceMode.Single)]
        [TestCase(TargetingChoiceMode.Direction)]
        [TestCase(TargetingChoiceMode.All)]
        public void PreviewAndExecution_ShareTargetsRolesCellsAndHealth_WithoutMutatingInput(TargetingChoiceMode choice)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, choice,
                choice == TargetingChoiceMode.Single ? TargetingSplash.Radial : TargetingSplash.None,
                choice == TargetingChoiceMode.Single ? 70 : 0);
            var aim = choice == TargetingChoiceMode.Single ? AttackAim.ForCell(new CellCoord(4, 3))
                : choice == TargetingChoiceMode.Direction ? AttackAim.ForDirection(AttackDirection.Right) : AttackAim.ForAll();
            var input = CombatCommandTestData.Input(method, aim);
            var preview = CombatCommandCalculator.Preview(input, System.Array.Empty<IMovementTileRule>());
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Targets.ToArray().Select(t => (t.TargetId, t.Role, t.TargetHealthAfter)),
                Is.EqualTo(result.Attack.Value.Hits.ToArray().Select(h => (h.TargetId, h.Role, h.TargetHealthAfter))));
            Assert.That(preview.DesignatedCells.ToArray(), Is.EqualTo(result.Attack.Value.DesignatedCells.ToArray()));
            Assert.That(preview.AdditionalCells.ToArray(), Is.EqualTo(result.Attack.Value.AdditionalCells.ToArray()));
            Assert.That(preview.AttackCost, Is.EqualTo(result.Attack.Value.ActionCostSpent));
            Assert.That(input.Characters[0].Stats.CurrentActionCost, Is.EqualTo(5));
            Assert.That(input.Characters.All(c => c.Health.CurrentHealth == 100), Is.True);
        }

        [Test]
        public void EmptySinglePreview_CannonHasSelectedCenterAndNoInventedPrimary()
        {
            var preview = CombatCommandCalculator.Preview(CombatCommandTestData.Input(empty: true, category: CategoryKey.Cannon), System.Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.PrimaryCell, Is.EqualTo(new CellCoord(4, 3)));
            Assert.That(preview.PrimaryTargetId.IsEmpty, Is.True);
            Assert.That(preview.Targets.Length, Is.Zero);
            Assert.That(preview.AttackCost, Is.EqualTo(2));
        }

        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        public void EmptySinglePreview_NonCannonRejectsWithoutInventingPrimaryOrSpendingCost(CategoryKey category)
        {
            var input = CombatCommandTestData.Input(empty: true, category: category);
            var range = CombatCommandCalculator.GetAttackRange(input);
            var preview = CombatCommandCalculator.Preview(input, System.Array.Empty<IMovementTileRule>());

            Assert.That(range.RangeCells.ToArray(), Has.Member(new CellCoord(4, 3)));
            Assert.That(range.AttackAims.ToArray(), Is.Empty);
            Assert.That(preview.CanAttack, Is.False);
            Assert.That(preview.FailureReason, Is.EqualTo(AttackFailureReason.InvalidPrimaryTarget));
            Assert.That(preview.PrimaryTargetId.IsEmpty, Is.True);
            Assert.That(preview.Targets.Length, Is.Zero);
            Assert.That(preview.AttackCost, Is.Zero);
            Assert.That(input.Characters[0].Stats.CurrentActionCost, Is.EqualTo(5));
            Assert.That(input.Characters.All(c => c.Health.CurrentHealth == 100), Is.True);
        }

        [Test]
        public void OverkillPreview_PreservesPotentialDamageBeforeHpClamp()
        {
            var input = CombatCommandTestData.Input();
            var chars = input.Characters.ToArray(); chars[2] = CombatCommandTestData.With(chars[2], hp: 1);
            var preview = CombatCommandCalculator.Preview(CombatCommandTestData.With(input, chars), System.Array.Empty<IMovementTileRule>());
            var target = preview.Targets.ToArray().Single(t => t.TargetId == new EntityId(2));
            Assert.That(target.ExpectedDamage, Is.EqualTo(20));
            Assert.That(target.TargetHealthBefore, Is.EqualTo(1));
            Assert.That(target.TargetHealthAfter, Is.Zero);
            Assert.That(target.DamageBeforeMatchup, Is.EqualTo(20));
        }

        [Test]
        public void Range_WhenUnaffordableRetainsCellsTargetsAndGeometricCandidates()
        {
            var input = CombatCommandTestData.Input();
            var before = CombatCommandCalculator.GetAttackRange(input);
            var chars = input.Characters.ToArray(); chars[0] = CombatCommandTestData.With(chars[0], ap: 0);
            var changed = CombatCommandTestData.With(input, chars);
            var after = CombatCommandCalculator.GetAttackRange(changed);
            Assert.That(after.Succeeded, Is.True);
            Assert.That(after.CanAffordAttack, Is.False);
            Assert.That(after.RangeCells.ToArray(), Is.EqualTo(before.RangeCells.ToArray()));
            Assert.That(after.InRangeTargetIds.ToArray(), Is.EqualTo(before.InRangeTargetIds.ToArray()));
            Assert.That(after.AttackAims.ToArray(), Is.EqualTo(before.AttackAims.ToArray()));
            Assert.That(CombatCommandCalculator.Preview(changed, System.Array.Empty<IMovementTileRule>()).FailureReason, Is.EqualTo(AttackFailureReason.ActionCostInsufficient));
        }

        [Test]
        public void IndirectSplashVictim_IsAbsentFromDirectRangeButPresentInPreview()
        {
            var input = CombatCommandTestData.Input(empty: true, aim: AttackAim.ForCell(new CellCoord(5, 3)));
            var chars = input.Characters.Concat(new[] {
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(5, 3), Owner.Enemy),
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(6, 3), Owner.Enemy) }).ToArray();
            input = CombatCommandTestData.With(input, chars);
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.RangeCells.ToArray(), Has.No.Member(new CellCoord(6, 3)));
            Assert.That(range.InRangeTargetIds.ToArray(), Is.EqualTo(new[] { new EntityId(3) }));
            Assert.That(range.AttackAims.ToArray().Any(a => a.Cell == new CellCoord(5, 3)), Is.True);
            var preview = CombatCommandCalculator.Preview(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.PrimaryTargetId, Is.EqualTo(new EntityId(3)));
            Assert.That(preview.Targets.ToArray().Select(t => t.TargetId), Is.EquivalentTo(new[] { new EntityId(3), new EntityId(2) }));
            var primary = preview.Targets.ToArray().Single(t => t.TargetId == new EntityId(3));
            Assert.That(primary.Role, Is.EqualTo(AttackTargetRole.Primary));
            Assert.That(primary.ExpectedDamage, Is.EqualTo(20));
            var indirect = preview.Targets.ToArray().Single(t => t.TargetId == new EntityId(2));
            Assert.That(indirect.Role, Is.EqualTo(AttackTargetRole.Additional));
            Assert.That(indirect.ExpectedDamage, Is.EqualTo(14));
        }

        [Test]
        public void DirectionCandidates_KeepOverlappingGroupsAndRejectDiagonalForEightDirectionShape()
        {
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Up));
            var chars = new[] { input.Characters[0], input.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 5), Owner.Enemy) };
            input = CombatCommandTestData.With(input, chars);
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.AttackAims.ToArray().Select(a => a.Direction), Is.EqualTo(new[] { AttackDirection.Up, AttackDirection.Right }));
            var diagonal = new CombatCommandInput(input.Number, input.Kind, input.ActorId,
                AttackAim.ForDirection(AttackDirection.UpRight), input.State, input.Characters, input.Profiles,
                input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts);
            Assert.That(CombatCommandCalculator.Preview(diagonal, System.Array.Empty<IMovementTileRule>()).CanAttack, Is.False);
        }

        [Test]
        public void AttackPreview_DoesNotReplaySettledMovementHazards()
        {
            var input = CombatCommandTestData.Input();
            var actor = CombatCommandTestData.With(input.Characters[0], hp: 1);
            var origin = new CellCoord(1, 2);
            actor = CombatMovementCalculator.CopyMovement(actor, actor.Stats, origin, 0, 0, 0, 0, origin);
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray());
            var moved = CombatCommandCalculator.Calculate(MovementTestData.Route(input, origin, new CellCoord(1, 3)),
                Array.Empty<IMovementTileRule>());
            input = CombatCommandComposition.Intent(CombatCommandComposition.Project(moved), CombatCommandKind.Attack, input.Aim);
            input = CombatCommandTestData.With(input, state: CombatCommandEffectTests.WithFields(input,
                new ThornFieldCellSnapshot(new CellCoord(1, 3), 5, 5)));
            var preview = CombatCommandCalculator.Preview(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(input.State.ThornFields.Count, Is.EqualTo(1));
            Assert.That(input.Characters[0].Health.CurrentHealth, Is.EqualTo(1));
            Assert.That(input.Characters[0].Stats.CurrentActionCost, Is.EqualTo(4));
        }

        [TestCase(TargetingShape.RadialSquare)]
        [TestCase(TargetingShape.Fan)]
        public void ConfirmedSingleRangeOne_AdmitsDefinedNeighbor_AndRejectsTwoCells(TargetingShape shape)
        {
            var method = new TargetingMethodSpec(shape, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = shape == TargetingShape.Fan ? CombatCommandEffectTests.ActorEffect(
                new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1)) : CombatCommandTestData.Input(empty: true);
            var actor = input.Characters[0]; var s = actor.Stats;
            var stats = new UnitStatsSnapshot(s.MoveMethod, s.MoveRange, s.MoveCost, 1,
                s.AttackPower, s.PhysicalDefense, s.MaxActionCost, s.AttackCost, s.RemainingMoveRange,
                s.CurrentActionCost, s.Priority, s.Delay, s.TraitStatuses.ToArray(), s.Category);
            actor = new CharacterCombatFacts(actor.EntityId, actor.OwnedId, actor.CharacterKey, actor.Owner,
                actor.OwnershipRole, actor.Area, actor.Health, stats, null, actor.Anchor, actor.Rotation,
                actor.OccupiedCells, actor.Anchor.Value, 0, 0);
            var profiles = input.Profiles.ToArray(); profiles[0] = new CombatAttackProfile(profiles[0].Definition, method);
            var cell = shape == TargetingShape.Fan ? new CellCoord(2, 3) : new CellCoord(2, 4);
            input = CombatCommandTestData.With(input, new[] { actor, input.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, cell, Owner.Enemy) }, profiles: profiles);
            input = CombatAttackQueries.WithAim(input, AttackAim.ForCell(cell));
            var range = CombatCommandCalculator.GetAttackRange(input);
            var preview = CombatCommandCalculator.Preview(input, System.Array.Empty<IMovementTileRule>());
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(range.RangeCells.Length, Is.EqualTo(shape == TargetingShape.Fan ? 4 : 8));
            Assert.That(preview.Targets.Span[0].ExpectedDamage, Is.EqualTo(20));
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(20));
            Assert.That(result.Attack.Value.Hits.Length, Is.EqualTo(1));
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId.Value == 2).Final.Anchor,
                Is.EqualTo(shape == TargetingShape.Fan ? new CellCoord(3, 3) : new CellCoord(2, 4)));
            Assert.That(CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input,
                AttackAim.ForCell(new CellCoord(3, 3))), System.Array.Empty<IMovementTileRule>()).CanAttack, Is.False);
            Assert.That(actor.Stats.AttackRange, Is.EqualTo(1));
        }

        [TestCase(70, 14)]
        [TestCase(35, 7)]
        public void CannonRangeFour_UsesConfiguredSplash_AndAllowsEmptyFarAim(int percent, int damage)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, percent);
            var input = CombatCommandTestData.Input(method, AttackAim.ForCell(new CellCoord(5, 3)), empty: true);
            var actor = CombatCommandTestData.With(input.Characters[0], category: CategorySnapshot.CreateStateless(CategoryKey.Cannon));
            var profiles = input.Profiles.ToArray(); profiles[0] = new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Cannon), method);
            input = CombatCommandTestData.With(input, new[] { actor, input.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(6, 3), Owner.Enemy) }, profiles: profiles);
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.InRangeTargetIds.ToArray(), Is.Empty);
            Assert.That(range.RangeCells.ToArray(), Has.Member(new CellCoord(5, 3)));
            Assert.That(range.RangeCells.ToArray(), Has.No.Member(new CellCoord(2, 3)));
            Assert.That(CombatCommandCalculator.Preview(input, System.Array.Empty<IMovementTileRule>()).Targets.Span[0].ExpectedDamage, Is.EqualTo(damage));
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Attack.Value.PrimaryTargetId.IsEmpty, Is.True);
            Assert.That(result.Attack.Value.Hits.Span[0].Role, Is.EqualTo(AttackTargetRole.Additional));
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(damage));
            Assert.That(result.Attack.Value.ActionCostSpent, Is.EqualTo(2));
        }

        [Test]
        public void AiFullMask_UsesAllAimAndSameLiteralDamageAsPlayerAndPreview()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.All, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForAll(), empty: true);
            input = CombatCommandTestData.With(input, input.Characters.Concat(new[] {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(2, 4), Owner.Enemy) }).ToArray());
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(new EnemyAiPolicy(new HostilityPolicy(), System.Array.Empty<IMovementTileRule>()).TrySelectAttackAim(input, range, out var aim), Is.True);
            Assert.That(aim.Kind, Is.EqualTo(AttackAimKind.All));
            Assert.That(range.AttackAims.Length, Is.EqualTo(1));
            Assert.That(CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, aim), System.Array.Empty<IMovementTileRule>()).Targets.ToArray()
                .Select(t => t.ExpectedDamage), Is.EqualTo(new[] { 20, 20 }));
            Assert.That(CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()).Attack.Value.Hits.ToArray()
                .Select(t => t.DamageDealt), Is.EqualTo(new[] { 20, 20 }));
        }

        [Test]
        public void AiUnaffordableAttack_HasGeometricCandidatesButSelectsNoAim()
        {
            var input = CombatCommandTestData.Input();
            var chars = input.Characters.ToArray(); chars[0] = CombatCommandTestData.With(chars[0], ap: 1);
            input = CombatCommandTestData.With(input, chars);
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.CanAffordAttack, Is.False);
            Assert.That(range.AttackAims.Length, Is.GreaterThan(0));
            Assert.That(new EnemyAiPolicy(new HostilityPolicy(), System.Array.Empty<IMovementTileRule>()).TrySelectAttackAim(input, range, out _), Is.False);
            Assert.That(CombatCommandCalculator.Preview(input, System.Array.Empty<IMovementTileRule>()).CanAttack, Is.False);
            Assert.That(CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()).Accepted, Is.False);
        }
    }
}
