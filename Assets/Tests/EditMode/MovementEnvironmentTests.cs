using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Tests.Core
{
    public sealed class MovementEnvironmentTests
    {
        private static readonly IReadOnlyList<IMovementTileRule> Rules = Array.AsReadOnly(
            new IMovementTileRule[] { new ThornMovementTileRule(), new FixtureRule() });

        [Test]
        public void Soul_CollectedOnlyAtActualDestination_BeforePositionModifier()
        {
            var input = MovementTestData.SafeRoute(20, 20, 1,
                new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1));
            var actor = CombatCommandTestData.With(input.Characters[0], hp: 95);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(),
                souls: Souls(new CellCoord(2, 1), new CellCoord(3, 1)));
            input = WithTiles(input, new DestinationTileModifierFact(7, new CellCoord(3, 1), true, new[] { Owner.Player }, 0, 5));
            var preview = CombatMovementCalculator.Preview(input, Rules);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            var final = MovementTestData.Actor(result);
            Assert.That(result.ConsumedSoulIds, Is.EqualTo(new long[] { 2 }));
            Assert.That(result.Field.Final.Entries.Single().Id, Is.EqualTo(1));
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(97));
            Assert.That(final.Health.MaxHealth, Is.EqualTo(106));
            Assert.That(final.Stats.SoulAttackBonus, Is.EqualTo(1));
            Assert.That(final.PendingMoveDistance, Is.Zero);
            Assert.That(final.CommittedMoveDistance, Is.EqualTo(2));
            Assert.That(result.PathMovement.ReturnBarrierOccurred, Is.True);
            Assert.That(preview.PathMovement.ReturnBarrierOccurred, Is.True);
            Assert.That(input.Souls.Entries.Count, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InterruptedArrival_CollectsOnce(bool forced)
        {
            var input = FourSteps(MovementTestData.SafeRoute(20, 20, 1, new CellCoord(1, 1), new CellCoord(2, 1)));
            var stop = forced ? new CellCoord(2, 2) : new CellCoord(2, 1);
            input = WithTiles(CombatCommandTestData.With(input, souls: Souls(stop)),
                new FixtureTile(7, new CellCoord(2, 1), force: forced, block: !forced));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.ConsumedSoulIds, Is.EqualTo(new long[] { 1 }));
            Assert.That(result.Events.OfType<CombatSoulEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(result.PathMovement.FinalStop, Is.EqualTo(stop));
            Assert.That(result.PathMovement.StopReason, Is.EqualTo(forced ? MovementStopReason.ForcedRelocation : MovementStopReason.MovementBlocked));
            Assert.That(MovementTestData.Actor(result).Stats.SoulMaxHealthBonus, Is.EqualTo(1));
            var ended = CombatCommandCalculator.Calculate(CombatCommonCommandTests.Command(CombatCommandKind.EndUnitTurn,
                MovementTestData.After(result)), Rules);
            Assert.That(ended.Accepted, Is.True, ended.Reason);
            Assert.That(MovementTestData.Actor(ended).AttackRefundEligibleMoveCost, Is.Zero);
            Assert.That(ended.ConsumedSoulIds, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AttackAndTurnEnd_DoNotRepeatMoveEffects_AndSealActualDistance(bool endTurn)
        {
            var input = MovementTestData.SafeRoute(20, 20, 1,
                new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(2, 2), new CellCoord(1, 2));
            var moved = CombatCommandCalculator.Calculate(input, Rules);
            var after = MovementTestData.After(moved);
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 2), Owner.Enemy);
            var s = after.State;
            after = CombatCommandTestData.With(after, characters: after.Characters.Append(target).ToArray(), souls: Souls(new CellCoord(1, 2)),
                state: new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                    s.Structures, new[] { new ThornFieldCellSnapshot(new CellCoord(1, 1), 5, 100) }, s.Timeline, s.Turn, s.Environment));
            var command = endTurn ? CombatCommonCommandTests.Command(CombatCommandKind.EndUnitTurn, after) :
                CombatCommandComposition.Intent(after, CombatCommandKind.Attack, AttackAim.ForCell(new CellCoord(2, 2)));
            var result = CombatCommandCalculator.Calculate(command, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = MovementTestData.Actor(result);
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.ConsumedSoulIds, Is.Empty);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.SoulsCollected), Is.Zero);
            Assert.That(result.FinalState.ThornFields.Single().ThornDamage, Is.EqualTo(100));
            Assert.That(final.CommittedMoveDistance, Is.EqualTo(3));
            Assert.That(final.PendingMoveDistance, Is.Zero);
            Assert.That(final.PendingMoveActionCost, Is.Zero);
            Assert.That(final.AttackRefundEligibleMoveCost, Is.Zero);
        }

        [Test]
        public void Rider_BarrierSafeIntervalReturnAndAttack_UseSeparateRefundLedger()
        {
            var input = FourSteps(MovementTestData.SafeRoute(20, 20, 1, new CellCoord(1, 1), new CellCoord(2, 1)));
            var actor = CombatCommandTestData.With(input.Characters[0], category: CategorySnapshot.CreateRider(false));
            var s = actor.Stats;
            var spec = UnitSpecTestFactory.Create(actor.CharacterKey, 1, 1, new[] { CellCoord.Zero }, 100,
                s.MoveMethod, s.MoveRange, s.MoveCost, s.AppointRange, s.AttackPower, s.MaxActionCost, s.AttackCost, s.Priority, s.Delay,
                Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits.TraitSpec>(), categoryKey: CategoryKey.Rider);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey ? new CombatAttackProfile(spec, p.Method) : p).ToArray());
            var barrier = CombatCommandCalculator.Calculate(WithTiles(input, new FixtureTile(7, new CellCoord(2, 1), consume: true)), Rules);
            Assert.That(MovementTestData.Actor(barrier).PendingMoveActionCost, Is.Zero);
            Assert.That(MovementTestData.Actor(barrier).AttackRefundEligibleMoveCost, Is.EqualTo(4));
            var safe = CombatCommandCalculator.Calculate(MovementTestData.Route(MovementTestData.After(barrier),
                new CellCoord(5, 1), new CellCoord(5, 2), new CellCoord(5, 3)), Rules);
            Assert.That(MovementTestData.Actor(safe).AttackRefundEligibleMoveCost, Is.EqualTo(6));
            var failed = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(MovementTestData.After(safe),
                CombatCommandKind.Attack, AttackAim.ForCell(new CellCoord(99, 99))), Rules);
            Assert.That(failed.Accepted, Is.False);
            Assert.That(failed.Characters.Characters, Is.Empty);
            Assert.That(MovementTestData.Actor(failed).PendingMoveActionCost, Is.EqualTo(2));
            Assert.That(MovementTestData.Actor(failed).AttackRefundEligibleMoveCost, Is.EqualTo(6));
            var returned = CombatCommandCalculator.Calculate(MovementTestData.Return(CombatCommandTestData.With(
                MovementTestData.After(safe), souls: Souls(new CellCoord(5, 1)))), Rules);
            Assert.That(MovementTestData.Actor(returned).AttackRefundEligibleMoveCost, Is.EqualTo(4));
            Assert.That(returned.ConsumedSoulIds, Is.Empty);
            Assert.That(returned.Field.Final.Entries.Count, Is.EqualTo(1));
            var afterReturn = MovementTestData.After(returned);
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(6, 1), Owner.Enemy);
            afterReturn = CombatCommandTestData.With(afterReturn, characters: afterReturn.Characters.Append(target).ToArray());
            var attacked = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(afterReturn,
                CombatCommandKind.Attack, AttackAim.ForCell(target.Anchor.Value)), Rules);
            Assert.That(attacked.Accepted, Is.True, attacked.Reason);
            Assert.That(attacked.Attack.Value.ActionCostRefunded, Is.EqualTo(4));
            Assert.That(MovementTestData.Actor(attacked).AttackRefundEligibleMoveCost, Is.Zero);
            Assert.That(MovementTestData.Actor(attacked).Stats.Category.RefundUsedThisTurn, Is.True);
            Assert.That(attacked.ConsumedSoulIds, Is.Empty);
        }

        private static SoulFieldSnapshot Souls(params CellCoord[] cells) => new(cells.Length + 1,
            cells.Select((cell, i) => new SoulFieldFacts(i + 1,
                new EffectOriginKey(EffectOwnerKind.Artifact, 4, 11, "soul"), cell, 5, 2)).ToArray());
        [Test]
        public void ThornEntry_ConsumesAndDamagesInTheMoveCommand()
        {
            var input = Route();
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth, s.BoardHeight,
                s.Phase, s.ActiveActorId, s.IsNight, s.Structures,
                new[] { new ThornFieldCellSnapshot(new CellCoord(2, 1), 3, 2) }, s.Timeline, s.Turn, s.Environment));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Health.CurrentHealth, Is.EqualTo(8));
            Assert.That(result.FinalState.ThornFields, Is.Empty);
            Assert.That(result.PathMovement.ReturnBarrierOccurred, Is.True);
            Assert.That(MovementTestData.Actor(result).PendingMoveDistance, Is.Zero);
        }

        [Test]
        public void MineSealsPriorSafeDragsAtFinalStop()
        {
            var input = WithTiles(Route(), new FixtureTile(7, new CellCoord(3, 1), damage: 2, consume: true));
            var first = CombatCommandCalculator.Calculate(input, Rules);
            var next = MovementTestData.Route(MovementTestData.After(first), new CellCoord(2, 1), new CellCoord(3, 1));
            var result = CombatCommandCalculator.Calculate(next, Rules);
            Assert.That(result.PathMovement.ReturnBarrierOccurred, Is.True);
            var final = MovementTestData.Actor(result);
            Assert.That(final.CommittedMoveDistance, Is.EqualTo(2));
            Assert.That(final.PendingMoveDistance, Is.Zero);
            Assert.That(final.PendingMoveActionCost, Is.Zero);
            Assert.That(final.AttackRefundEligibleMoveCost, Is.EqualTo(2));
            Assert.That(final.MovementCheckpointAnchor, Is.EqualTo(new CellCoord(3, 1)));
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(8));
        }

        [Test]
        public void DamageThenHeal_RemainsBarrier_AndPreservesEarlierChanges()
        {
            var input = Route();
            var main = input.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            var a = new FixtureTile(7, new CellCoord(2, 1), damage: 2, consume: true, target: main.EntityId);
            var b = new FixtureTile(8, a.Cell, heal: 2, consume: true, target: main.EntityId);
            var unrelated = CombatCommandTestData.Character(50, EntityKey.Unit_Demon, new CellCoord(6, 6), Owner.Enemy);
            input = CombatCommandTestData.With(input, characters: input.Characters.Append(unrelated).ToArray());
            input = WithTiles(input, b, a, new DestinationTileModifierFact(9, a.Cell, true, new[] { Owner.Player }, 1, 5));
            bool observed = false;
            var rules = Array.AsReadOnly(new IMovementTileRule[] { new FixtureRule((current, tile) =>
            {
                if (tile.Id != b.Id) return;
                Assert.That(current.SourcesAtCell.Single(t => t.Id == a.Id).Active, Is.False);
                Assert.That(current.Targets.Single(t => t.EntityId == main.EntityId).Health.CurrentHealth, Is.EqualTo(98));
                observed = true;
            }) });
            var result = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(observed, Is.True);
            Assert.That(result.PathMovement.ReturnBarrierOccurred, Is.True);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == main.EntityId).Final.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == main.EntityId).Expected, Is.SameAs(main));
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == a.Id).Active, Is.False);
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == b.Id).Active, Is.False);
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == 9).Active, Is.True);
            Assert.That(result.Characters.Characters.Any(c => c.Final.EntityId == unrelated.EntityId), Is.False);
            Assert.That(input.State.Environment.Tiles.All(t => t.Active), Is.True);
            var healthEvents = result.Events.OfType<CombatCharacterEvent>()
                .Where(e => e.Kind == CombatEventKind.HealthChanged && e.Final.EntityId == main.EntityId).ToArray();
            Assert.That(healthEvents.Length, Is.EqualTo(2));
            Assert.That(healthEvents[0].DamageOrigins.Single().PhysicalCell, Is.EqualTo(a.Cell));
            Assert.That(healthEvents[0].DamageOrigins.Single().CreditedCharacterId, Is.Null);
            Assert.That(healthEvents[1].DamageOrigins, Is.Empty);
            var projection = new MovementProjection(input);
            var damaged = CharacterSettlementCalculator.Copy(main, new HealthStateSnapshot(98, 100), main.Stats);
            projection.MergeCharacter(new CharacterStateChange(main, damaged));
            Assert.Throws<InvalidOperationException>(() => projection.MergeCharacter(new CharacterStateChange(main, main)));
            Assert.That(projection.Character(main.EntityId), Is.SameAs(damaged));
            projection.ChangeSource(a, new MovementStepEffects(expectedSource: a, finalSource: a.Consumed()));
            Assert.Throws<InvalidOperationException>(() => projection.ChangeSource(a,
                new MovementStepEffects(expectedSource: a, finalSource: a)));
            Assert.That(projection.Source(a.Id).Active, Is.False);
        }

        [Test]
        public void OtherEntityDamage_BlocksReturn()
        {
            var input = WithTiles(Route(), new FixtureTile(7, new CellCoord(2, 1), damage: 2, target: new EntityId(10)));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(MovementTestData.Actor(result).Health.CurrentHealth, Is.EqualTo(10));
            Assert.That(result.Characters.Main.Final.CurrentHealth, Is.EqualTo(98));
            Assert.That(result.PathMovement.ReturnBarrierOccurred, Is.True);
            Assert.That(CheckpointReturnCalculator.Preview(MovementTestData.Return(MovementTestData.After(result))).CanReturn, Is.False);
        }

        [Test]
        public void ZeroDamageConsumedMine_BlocksReturn()
        {
            var result = CombatCommandCalculator.Calculate(WithTiles(Route(),
                new FixtureTile(7, new CellCoord(2, 1), consume: true)), Rules);
            Assert.That(MovementTestData.Actor(result).Health.CurrentHealth, Is.EqualTo(10));
            Assert.That(result.PathMovement.ReturnBarrierOccurred, Is.True);
            Assert.That(result.Events.OfType<CombatMovementSourceEvent>().Count(), Is.EqualTo(1));
            Assert.That(MovementTestData.Actor(result).PendingMoveActionCost, Is.Zero);
        }

        [Test]
        public void NoOpHeal_DoesNotBlockReturn()
        {
            var result = CombatCommandCalculator.Calculate(WithTiles(Route(),
                new FixtureTile(7, new CellCoord(2, 1), heal: 5)), Rules);
            Assert.That(result.PathMovement.ReturnBarrierOccurred, Is.False);
            Assert.That(CheckpointReturnCalculator.Preview(MovementTestData.Return(MovementTestData.After(result))).CanReturn, Is.True);
        }

        [Test]
        public void LethalHazard_PreventsHealingAndSuffix()
        {
            var input = FourSteps(Route(2));
            input = WithTiles(input, new FixtureTile(8, new CellCoord(3, 1), damage: 2),
                new FixtureTile(7, new CellCoord(3, 1), heal: 10, priority: -100),
                new FixtureTile(9, new CellCoord(4, 1), consume: true));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(2));
            Assert.That(result.PathMovement.ActionCostSpent, Is.EqualTo(2));
            Assert.That(result.PathMovement.StopReason, Is.EqualTo(MovementStopReason.Dead));
            Assert.That(result.PathMovement.FinalStop, Is.EqualTo(new CellCoord(3, 1)));
            Assert.That(result.PathMovement.ReturnBarrierOccurred, Is.True);
            Assert.That(MovementTestData.Actor(result).Area, Is.EqualTo(CharacterRuntimeArea.Excluded));
            Assert.That(MovementTestData.Actor(result).Anchor, Is.Null);
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == 9).Active, Is.True);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.HealthChanged), Is.Empty);
        }

        [Test]
        public void SurvivingInterruption_HasOneFinalArrival()
        {
            var input = WithTiles(FourSteps(Route(8)),
                new FixtureTile(7, new CellCoord(2, 1), block: true),
                new DestinationTileModifierFact(8, new CellCoord(2, 1), true, new[] { Owner.Player }, 3, 5),
                new DestinationTileModifierFact(9, new CellCoord(5, 1), true, new[] { Owner.Player }, 10, 20));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.PathMovement.StopReason, Is.EqualTo(MovementStopReason.MovementBlocked));
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(MovementTestData.Actor(result).Health, Is.EqualTo(new HealthStateSnapshot(8, 15)));
            Assert.That(MovementTestData.Actor(result).PositionModifier.SourceIds, Is.EqualTo(new long[] { 8 }));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.HealthNormalized), Is.EqualTo(1));
            Assert.That(MovementTestData.Actor(result).CommittedMoveDistance, Is.EqualTo(1));
        }

        [Test]
        public void ForcedRelocation_DoesNotResumeOriginalRoute()
        {
            var input = WithTiles(FourSteps(Route(8)),
                new FixtureTile(7, new CellCoord(2, 1), force: true),
                new FixtureTile(8, new CellCoord(3, 1), consume: true),
                new DestinationTileModifierFact(9, new CellCoord(2, 2), true, new[] { Owner.Player }, 3, 5));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.PathMovement.StopReason, Is.EqualTo(MovementStopReason.ForcedRelocation));
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(result.PathMovement.ActionCostSpent, Is.EqualTo(1));
            Assert.That(result.PathMovement.FinalStop, Is.EqualTo(new CellCoord(2, 2)));
            Assert.That(MovementTestData.Actor(result).PositionModifier.SourceIds, Is.EqualTo(new long[] { 9 }));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.HealthNormalized), Is.EqualTo(1));
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == 8).Active, Is.True);
        }

        [Test]
        public void ProjectedOccupancy_BlocksNextStepBeforeCharging()
        {
            var input = FourSteps(Route());
            var other = CombatCommandTestData.Character(50, EntityKey.Unit_Demon, new CellCoord(3, 0), Owner.Enemy);
            input = CombatCommandTestData.With(input, characters: input.Characters.Append(other).ToArray());
            input = WithTiles(input, new FixtureTile(7, new CellCoord(2, 1), force: true, target: other.EntityId),
                new FixtureTile(8, new CellCoord(3, 1), consume: true));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.PathMovement.StopReason, Is.EqualTo(MovementStopReason.NextStepInvalid));
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(result.PathMovement.ActionCostSpent, Is.EqualTo(1));
            Assert.That(result.PathMovement.FinalStop, Is.EqualTo(new CellCoord(2, 1)));
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == other.EntityId).Final.Anchor,
                Is.EqualTo(new CellCoord(3, 1)));
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == 8).Active, Is.True);
        }

        [Test]
        public void TerminalEffect_PreventsLaterSteps()
        {
            var input = FourSteps(Route());
            input = WithTiles(input, new FixtureTile(7, new CellCoord(2, 1), damage: 100, target: new EntityId(10)),
                new FixtureTile(8, new CellCoord(2, 1), heal: 10));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Defeat));
            Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(result.PathMovement.StopReason, Is.EqualTo(MovementStopReason.BattleEnded));
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(result.Characters.Main.Final.CurrentHealth, Is.Zero);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.StageEnded), Is.EqualTo(1));
        }

        [TestCase(false, StageOutcome.Victory)]
        [TestCase(true, StageOutcome.Defeat)]
        public void HeadquartersAndMainDeath_KeepExistingTerminalPrecedence(bool killMain, StageOutcome expected)
        {
            var input = WithTiles(FourSteps(Route()), new FixtureTile(7, new CellCoord(2, 1), damage: 100,
                target: new EntityId(20), secondTarget: killMain ? new EntityId(10) : null));
            var s = input.State;
            var hq = new StructureCombatFacts(new EntityId(20),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 5, false, 0),
                new HealthStateSnapshot(5, 5), new CellCoord(6, 5), Rotation4.R0, new[] { new CellCoord(6, 5) });
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase,
                s.ActiveActorId, s.IsNight, new[] { hq }, s.ThornFields, s.Timeline, s.Turn, s.Environment));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(expected));
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(result.FinalState.Structures.Single().Health.IsDead, Is.True);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.StageEnded), Is.EqualTo(1));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.HealthNormalized), Is.Zero);
            var damage = result.Events.OfType<CombatStructureEvent>().Single();
            Assert.That(damage.DamageOrigins.Single().PhysicalCell, Is.EqualTo(new CellCoord(2, 1)));
            Assert.That(damage.DamageOrigins.Single().CreditedCharacterId, Is.Null);
        }

        [Test]
        public void DeathSoulSpawn_IsResolvedOnceBeforeFinalPublication()
        {
            var input = Route();
            var enemy = CombatCommandTestData.Character(50, EntityKey.Unit_Demon, new CellCoord(6, 6), Owner.Enemy);
            input = CombatCommandTestData.With(input, characters: input.Characters.Append(enemy).ToArray(),
                artifacts: new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactLoadoutSnapshot(1,
                    new[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactPlacedEffectSnapshot(
                        11, 4, ArtifactSoulSchemaTests.SoulEffect()) }));
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase,
                s.ActiveActorId, true, s.Structures, s.ThornFields, s.Timeline, s.Turn, s.Environment));
            input = WithTiles(input, new FixtureTile(7, new CellCoord(2, 1), damage: 100, target: enemy.EntityId));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Field.Final.Entries.Count, Is.EqualTo(1));
            Assert.That(result.Field.Final.Entries.Single().Cell, Is.EqualTo(enemy.Anchor.Value));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.SoulFieldSettled), Is.EqualTo(1));
            Assert.That(result.Field.Expected, Is.SameAs(input.Souls));
        }

        [Test]
        public void Preview_UsesSameRulesWithVisibleFacts_AndExecutionReadsHiddenSources()
        {
            var input = WithTiles(FourSteps(Route(2)),
                new FixtureTile(7, new CellCoord(2, 1), damage: 2, known: new[] { Owner.Enemy }));
            var preview = CombatMovementCalculator.Preview(input, Rules);
            Assert.That(preview.PathMovement.DistanceSpent, Is.EqualTo(4));
            Assert.That(preview.PathMovement.ReturnBarrierOccurred, Is.False);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(result.PathMovement.StopReason, Is.EqualTo(MovementStopReason.Dead));
            Assert.That(input.State.Environment.Tiles.Single().Active, Is.True);
            var visible = WithTiles(FourSteps(Route(2)), new FixtureTile(7, new CellCoord(2, 1), damage: 2));
            Assert.That(CombatMovementCalculator.Preview(visible, Rules).PathMovement.EnteredCells,
                Is.EqualTo(CombatCommandCalculator.Calculate(visible, Rules).PathMovement.EnteredCells));
            Assert.Throws<ArgumentNullException>(() => CombatCommandCalculator.Calculate(visible, null));
            Assert.Throws<NotSupportedException>(() => CombatCommandCalculator.Calculate(visible, Array.Empty<IMovementTileRule>()));
        }

        [Test]
        public void ForcedOtherEntity_RenewsCheckpointBeforeFollowingKnockback()
        {
            var input = CombatCommandEffectTests.ActorEffect(
                new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy);
            input = CombatCommandTestData.With(input, characters: new[] { input.Characters[0], input.Characters[1], target });
            input = WithTiles(MovementTestData.Route(input, new CellCoord(1, 3), new CellCoord(1, 4)),
                new FixtureTile(7, new CellCoord(1, 4), target: target.EntityId, force: true));
            var moved = CombatCommandCalculator.Calculate(input, Rules);
            var current = CombatCommandComposition.Project(moved);
            var pushed = current.Characters.Single(c => c.EntityId == target.EntityId);
            Assert.That(pushed.Anchor, Is.EqualTo(new CellCoord(3, 4)));
            Assert.That(pushed.MovementCheckpointAnchor, Is.EqualTo(new CellCoord(3, 4)));
            Assert.That(pushed.PendingMoveDistance, Is.Zero);
            Assert.That(pushed.PendingMoveActionCost, Is.Zero);
            Assert.That(pushed.Stats.CurrentActionCost, Is.EqualTo(target.Stats.CurrentActionCost));
            var attack = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(current,
                CombatCommandKind.Attack, AttackAim.ForCell(pushed.Anchor.Value)), Rules);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(attack.Characters.Characters.Single(c => c.Final.EntityId == target.EntityId).Final.Anchor,
                Is.EqualTo(new CellCoord(4, 4)));
        }

        private static CombatCommandInput FourSteps(CombatCommandInput input) => MovementTestData.Route(input,
            new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1), new CellCoord(4, 1), new CellCoord(5, 1));

        private static CombatCommandInput Route(int hp = 10)
        {
            var input = MovementTestData.SafeRoute(20, 20, 1, new CellCoord(1, 1), new CellCoord(2, 1));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            actor = CharacterSettlementCalculator.Copy(actor, new HealthStateSnapshot(hp, 10), actor.Stats);
            return CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray());
        }

        private static CombatCommandInput WithTiles(CombatCommandInput input, params MovementTileFact[] tiles)
        {
            var s = input.State;
            return CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth, s.BoardHeight,
                s.Phase, s.ActiveActorId, s.IsNight, s.Structures, s.ThornFields, s.Timeline, s.Turn,
                new MovementEnvironmentFacts(tiles)));
        }

        private sealed class FixtureTile : MovementTileFact
        {
            internal int Damage { get; }
            internal int Heal { get; }
            internal bool Consume { get; }
            internal EntityId? Target { get; }
            internal bool Force { get; }
            internal bool Block { get; }
            internal EntityId? SecondTarget { get; }
            internal FixtureTile(long id, CellCoord cell, int damage = 0, int heal = 0,
                bool consume = false, bool active = true, int priority = 0, IReadOnlyList<Owner> known = null,
                EntityId? target = null, bool force = false, bool block = false, EntityId? secondTarget = null)
                : base(id, cell, "fixture", priority, active, known ?? new[] { Owner.Player, Owner.Enemy })
            { Damage = damage; Heal = heal; Consume = consume; Target = target; Force = force; Block = block; SecondTarget = secondTarget; }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other) &&
                other is FixtureTile f && f.Damage == Damage && f.Heal == Heal && f.Consume == Consume &&
                f.Target == Target && f.Force == Force && f.Block == Block && f.SecondTarget == SecondTarget;
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) =>
                new FixtureTile(Id, Cell, Damage, Heal, Consume, Active, Priority, knownTo, Target, Force, Block, SecondTarget);
            internal FixtureTile Consumed() => new(Id, Cell, Damage, Heal, Consume, false, Priority, KnownTo, Target, Force, Block, SecondTarget);
        }

        private sealed class FixtureRule : IMovementTileRule
        {
            private readonly Action<MovementStepInput, FixtureTile> _observe;
            internal FixtureRule(Action<MovementStepInput, FixtureTile> observe = null) { _observe = observe; }
            public string Key => "fixture";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current)
            {
                var f = (FixtureTile)tile;
                return new[] { f.Target ?? mover, mover }.Concat(f.SecondTarget.HasValue ? new[] { f.SecondTarget.Value } :
                    Array.Empty<EntityId>()).Distinct().ToArray();
            }
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
            {
                var f = (FixtureTile)tile;
                _observe?.Invoke(input, f);
                bool hazard = input.Phase == MovementEffectPhase.Hazard;
                bool consume = f.Consume && (hazard ? f.Damage > 0 || f.Heal == 0 : f.Heal > 0);
                var damage = hazard && f.Damage > 0 ? new List<MovementHealthRequest> {
                    new(f.Target ?? input.MoverId, f.Damage) } : new List<MovementHealthRequest>();
                if (hazard && f.SecondTarget.HasValue) damage.Add(new MovementHealthRequest(f.SecondTarget.Value, f.Damage));
                return new MovementStepEffects(
                    damage: damage,
                    healing: !hazard && f.Heal > 0 ? new[] { new MovementHealthRequest(f.Target ?? input.MoverId, f.Heal) } : null,
                    forcedMovement: hazard && f.Force ? new[] { new MovementForcedMoveRequest(f.Target ?? input.MoverId, Rotation4.R0, 1) } : null,
                    expectedSource: consume ? f : null, finalSource: consume ? f.Consumed() : null,
                    movementMethods: hazard && f.Block ? new[] { new MovementMethodChange(input.MoverId, MoveMethod.LiftAndPlace, MoveMethod.None) } : null);
            }
        }
    }
}
