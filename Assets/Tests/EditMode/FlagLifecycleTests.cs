using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class FlagLifecycleTests
    {
        internal static readonly IMovementTileRule[] Rules = {
            new SheriffBadgeMovementTileRule(), new MineMovementTileRule(), new FlagMovementTileRule() };
        internal static FlagSourceFact Flag(long id = 10, long ordinal = 1, int bonus = 7, CellCoord? cell = null) =>
            new(id, cell ?? MineEnvironmentTests.Center, ordinal, bonus, true, new[] { Owner.Player, Owner.Enemy });
        private static UnitStatsSnapshot WithPriority(UnitStatsSnapshot s, int priority) => new(
            s.MoveMethod, s.MoveRange, s.MoveCost, s.AppointRange, s.AttackPower, s.PhysicalDefense,
            s.MaxActionCost, s.AttackCost, s.RemainingMoveRange, s.CurrentActionCost, priority, s.Delay,
            s.TraitStatuses.ToArray(), s.Category, s.ArtifactAttackBonus, s.EffectiveAttackPower, s.SoulAttackBonus,
            s.SoulMaxHealthBonus, s.PositionAttackBonus);

        [Test]
        public void CurrentTurnThenNextTurn_UsesNewPriorityImmediately_AndForecastMatches()
        {
            var seed = MineEnvironmentTests.Input(Flag(bonus: 30));
            var actor = seed.Characters.Single(c => c.EntityId == seed.ActorId);
            actor = CharacterSettlementCalculator.Copy(actor, actor.Health, WithPriority(actor.Stats, 10));
            var characters = seed.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray();
            var turn = new CombatTurnFacts(StagePhase.Combat, currentTime: 10, participants: new[] {
                new CombatTurnParticipant(actor.EntityId, 10, 10, 10, false, 1),
                new CombatTurnParticipant(new EntityId(10), 100, 10, 15, false, 1) });
            var s = seed.State;
            var state = CombatTurnCalculator.State(s, turn, s.Phase, actor.EntityId);
            var input = new CombatCommandInput(seed.Number, seed.Kind, seed.ActorId, seed.Aim, state, characters,
                seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts, seed.TargetCell,
                battle: new CombatBattleFacts(10, 1000, 37, 2, 2, Array.Empty<CharacterInstanceId>(), 0, 100, 0, 0),
                path: seed.Path, environmentBindings: seed.EnvironmentBindings);
            var acquired = CombatCommandComposition.Project(CombatCommandCalculator.Calculate(input, Rules));
            Assert.That(acquired.State.ActiveActorId, Is.EqualTo(actor.EntityId));
            Assert.That(acquired.State.Turn.CurrentTime, Is.EqualTo(10));
            Assert.That(acquired.State.Turn.Participants.First().NextActionTime, Is.EqualTo(10));
            var forecast = CombatTurnCalculator.Forecast(acquired, 2);
            Assert.That(forecast.Select(t => t.UnitId), Is.EqualTo(new EntityId?[] { actor.EntityId, actor.EntityId }));
            Assert.That(forecast[1].ScheduledTime, Is.EqualTo(13));
            var ended = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(acquired, CombatCommandKind.EndUnitTurn), Rules);
            Assert.That(ended.Accepted, Is.True, ended.Reason);
            Assert.That(ended.FinalState.Turn.Participants.Single(p => p.EntityId == actor.EntityId).NextActionTime, Is.EqualTo(13));
            Assert.That(CombatTurnCalculator.Forecast(CombatCommandComposition.Project(ended), 1).Single().UnitId, Is.EqualTo(actor.EntityId));
        }

        [Test]
        public void FlagThenLethalMine_DropsOneFlagAfterDeath_AndLeavesLaterPickup()
        {
            var input = MineEnvironmentTests.Input(Flag(), MineEnvironmentTests.Mine(11, 2, 100), Flag(12, 3));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == 12).Active, Is.True);
            var drop = result.FinalState.Environment.Tiles.OfType<FlagSourceFact>().Single(t => t.Id > 12);
            Assert.That(drop.Cell, Is.EqualTo(MineEnvironmentTests.Center));
            Assert.That(drop.EntryOrdinal, Is.EqualTo(4));
            Assert.That(result.Events.OfType<CombatObjectCreatedEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatObjectActivationEvent>().Select(e => e.Source.Id), Is.EqualTo(new[] { 10L, 11L }));
            Assert.That(CombatCommandComposition.Project(result).EnvironmentBindings.ObjectSourceIds, Has.Member(drop.Id));
            Assert.That(result.PathMovement.ReturnBarrierOccurred, Is.True);
        }

        [Test]
        public void DuplicateFlag_RemainsOnField_AndDoesNotPreventLaterMine()
        {
            var input = MineEnvironmentTests.Input(Flag(), Flag(12, 2, 99), MineEnvironmentTests.Mine(13, 3, 1));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            var actor = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == input.ActorId);
            Assert.That(actor.HeldEnvironment.Flag.PriorityBonus, Is.EqualTo(7));
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == 12).Active, Is.True);
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == 13).Active, Is.False);
            Assert.That(actor.Health.CurrentHealth, Is.EqualTo(99));
        }

        [Test]
        public void DropCandidates_UseStraightLineDistance_AvoidUnitsAndStructures()
        {
            var i = MineEnvironmentTests.Input(Flag());
            var occupied = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(2, 2), Owner.Enemy);
            var nearest = FlagDropCalculator.NearestCells(i.State, new[] { occupied }, i.State.Structures, new CellCoord(2, 2), i.EnvironmentBindings);
            Assert.That(nearest, Is.EquivalentTo(new[] { new CellCoord(1, 2), new CellCoord(3, 2), new CellCoord(2, 1), new CellCoord(2, 3) }));
            Assert.That(nearest, Has.No.Member(new CellCoord(3, 3)));
        }

        [Test]
        public void ExplicitCreationUnderUnit_AcquiresImmediately_AndProgressIsLocal()
        {
            var i = MineEnvironmentTests.Input(Flag());
            var actor = i.Characters.Single(c => c.EntityId == i.ActorId);
            var characters = i.Characters.ToDictionary(c => c.EntityId);
            var environment = i.State.Environment;
            var bindings = i.EnvironmentBindings;
            var progress = i.State.EnvironmentProgress;
            var events = new System.Collections.Generic.List<CombatEvent>();
            FlagDropCalculator.CreateAt(HeldFlag.From(Flag()), actor.Anchor.Value, characters, ref environment, ref bindings, ref progress, events);
            Assert.That(characters[actor.EntityId].HeldEnvironment.Flag, Is.Not.Null);
            Assert.That(environment.Tiles.Last().Active, Is.False);
            Assert.That(bindings.ObjectSourceIds, Has.Member(environment.Tiles.Last().Id));
            Assert.That(events.Select(e => e.Kind), Is.EqualTo(new[] { CombatEventKind.ObjectCreated,
                CombatEventKind.ObjectActivated, CombatEventKind.HeldEnvironmentChanged, CombatEventKind.MovementSourceChanged }));
            Assert.That(i.State.Environment.Tiles.Count, Is.EqualTo(1));
            Assert.That(i.Characters.Single(c => c.EntityId == i.ActorId).HeldEnvironment.Flag, Is.Null);
        }

        [Test]
        public void RandomTieSequence_IsDeterministic_AndSingleCandidateDoesNotNeedRandom()
        {
            uint state = 1;
            Assert.That(EnvironmentRandom.NextIndex(ref state, 4), Is.EqualTo(0));
            Assert.That(state, Is.EqualTo(1015568748u));
            Assert.That(EnvironmentRandom.NextIndex(ref state, 4), Is.EqualTo(3));
            Assert.That(EnvironmentRandom.NextIndex(ref state, 4), Is.EqualTo(2));
            var i = MineEnvironmentTests.Input(Flag());
            Assert.That(FlagDropCalculator.NearestCells(i.State, Array.Empty<CharacterCombatFacts>(),
                i.State.Structures, new CellCoord(-1, -1), i.EnvironmentBindings), Is.EqualTo(new[] { CellCoord.Zero }));
        }

        [Test]
        public void PreviewAndRepeatedCalculation_DoNotAdvanceSourceIdsOrRandomInInput()
        {
            var i = MineEnvironmentTests.Input(Flag(), MineEnvironmentTests.Mine(11, 2, 100));
            var original = i.State.EnvironmentProgress;
            var preview = CombatMovementCalculator.Preview(i, Rules);
            var first = CombatCommandCalculator.Calculate(i, Rules);
            var second = CombatCommandCalculator.Calculate(i, Rules);
            Assert.That(first.FinalState.SameValues(second.FinalState), Is.True);
            Assert.That(i.State.EnvironmentProgress, Is.SameAs(original));
            Assert.That(i.State.EnvironmentProgress.NextSourceId, Is.EqualTo(12));
            Assert.That(first.FinalState.EnvironmentProgress.NextSourceId, Is.EqualTo(13));
            var projected = CombatCommandComposition.Project(first);
            var noop = CombatCommandComposition.Intent(projected, CombatCommandKind.ReturnToCheckpoint);
            var rejected = CombatCommandCalculator.Calculate(noop, Rules);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.FinalState.EnvironmentProgress.SameValues(projected.State.EnvironmentProgress), Is.True);
            var merged = CombatCommandComposition.Merge(i, new[] { first });
            Assert.That(merged.EnvironmentBindings.Final.SameValues(first.EnvironmentBindings.Final), Is.True);
        }

        [Test]
        public void NoFreeDropCell_RemovesHeldFlagWithoutAllocatingOrAdvancingRandom()
        {
            var seed = MineEnvironmentTests.Input();
            var main = seed.Characters.Single(c => c.EntityId == new EntityId(10));
            main = CombatMovementCalculator.MoveTo(main, CellCoord.Zero);
            var waiting = CombatPlacementCalculator.Location(CombatCommandTestData.Character(3, EntityKey.Unit_Demon,
                CellCoord.Zero, Owner.Enemy), CharacterRuntimeArea.EnemyWaiting, null, null, null);
            waiting = HeldEnvironmentEffects.AcquireFlag(waiting, Flag());
            var state = new CombatCommandState(1, 1, CombatFlowPhase.PlayerInput, main.EntityId, false,
                Array.Empty<StructureCombatFacts>(), Array.Empty<ThornFieldCellSnapshot>(), Array.Empty<CombatTimelineEntrySnapshot>());
            var i = new CombatCommandInput(1, seed.Kind, main.EntityId, default, state, new[] { main, waiting }, seed.Profiles,
                seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts);
            var characters = i.Characters.ToDictionary(c => c.EntityId);
            characters[waiting.EntityId] = CombatCommandCalculator.Hurt(waiting, 100);
            var environment = state.Environment;
            var bindings = i.EnvironmentBindings;
            var progress = state.EnvironmentProgress;
            FlagDropCalculator.ResolveDeaths(i, characters, state.Structures,
                new System.Collections.Generic.Dictionary<EntityId, CellCoord> { [waiting.EntityId] = CellCoord.Zero },
                ref environment, ref bindings, ref progress, new System.Collections.Generic.List<CombatEvent>());
            Assert.That(characters[waiting.EntityId].HeldEnvironment.Flag, Is.Null);
            Assert.That(environment.Tiles, Is.Empty);
            Assert.That(progress, Is.SameAs(state.EnvironmentProgress));
        }

        [Test]
        public void NightRecallAndRedeployment_RetainOneFlagAndPriorityWithoutReapplyingBonus()
        {
            var i = CombatCommandTestData.Input(empty: true);
            var characters = i.Characters.Select(c => HeldEnvironmentEffects.AcquireFlag(c, Flag())).ToArray();
            var turn = new CombatTurnFacts(StagePhase.Combat, participants: characters.Select(c =>
                new CombatTurnParticipant(c.EntityId, HeldEnvironmentEffects.EffectivePriority(c), c.Stats.Delay)).ToArray());
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, i.ActorId, true,
                i.State.Structures, i.State.ThornFields, i.State.Timeline, turn);
            var input = new CombatCommandInput(i.Number, CombatCommandKind.EndUnitTurn, i.ActorId, default,
                state, characters, i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
                battle: new CombatBattleFacts(4, 10, 1, 8, 8, Array.Empty<CharacterInstanceId>(), 0, 100, 0, null));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var actor = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == i.ActorId);
            Assert.That(actor.Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(HeldEnvironmentEffects.EffectivePriority(actor), Is.EqualTo(107));
            Assert.That(actor.Stats.Priority, Is.EqualTo(100));
            var redeployed = CombatPlacementCalculator.Location(actor, CharacterRuntimeArea.Field, null, new CellCoord(2, 3), Rotation4.R0);
            Assert.That(HeldEnvironmentEffects.EffectivePriority(redeployed), Is.EqualTo(107));
            Assert.That(redeployed.HeldEnvironment.Flag, Is.SameAs(actor.HeldEnvironment.Flag));
        }

        [Test]
        public void Pickup_AddsPriorityOnce_AndClearRestoresBaseWithoutClearingBadgeOrSupplies()
        {
            var input = MineEnvironmentTests.Input(Flag());
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var held = HeldEnvironmentEffects.GrantSupplies(HeldEnvironmentEffects.AcquireBadge(actor, 31), 4);
            var acquired = HeldEnvironmentEffects.AcquireFlag(held, Flag());
            Assert.That(HeldEnvironmentEffects.EffectivePriority(acquired), Is.EqualTo(actor.Stats.Priority + 7));
            Assert.That(acquired.Stats.Priority, Is.EqualTo(actor.Stats.Priority));
            Assert.That(HeldEnvironmentEffects.AcquireFlag(acquired, Flag(12)), Is.SameAs(acquired));
            var expired = HeldEnvironmentEffects.ExpireSupplies(acquired);
            Assert.That(expired.HeldEnvironment.Flag, Is.Not.Null);
            Assert.That(HeldEnvironmentEffects.EffectivePriority(expired), Is.EqualTo(HeldEnvironmentEffects.EffectivePriority(acquired)));
            var cleared = HeldEnvironmentEffects.ClearFlag(acquired);
            Assert.That(HeldEnvironmentEffects.EffectivePriority(cleared), Is.EqualTo(actor.Stats.Priority));
            Assert.That(cleared.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(cleared.HeldEnvironment.SuppliesAttackBonus, Is.EqualTo(4));
            Assert.That(HeldEnvironmentEffects.EffectivePriority(HeldEnvironmentEffects.ClearForBattleEnd(acquired)), Is.EqualTo(actor.Stats.Priority));
        }

        [Test]
        public void Arrival_ConsumesFlagAndPreservesFutureScheduledTime()
        {
            var input = MineEnvironmentTests.Input(Flag());
            var before = input.Characters.Single(c => c.EntityId == input.ActorId);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var after = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == input.ActorId);
            Assert.That(HeldEnvironmentEffects.EffectivePriority(after), Is.EqualTo(before.Stats.Priority + 7));
            Assert.That(result.FinalState.Environment.Tiles.Single().Active, Is.False);
            foreach (var participant in result.FinalState.Turn.Participants)
            {
                var previous = input.State.Turn.Participants.Single(p => p.EntityId == participant.EntityId);
                Assert.That(participant.NextActionTime, Is.EqualTo(previous.NextActionTime));
                if (participant.EntityId == before.EntityId) Assert.That(participant.Priority, Is.EqualTo(HeldEnvironmentEffects.EffectivePriority(after)));
            }
        }

        [Test]
        public void DeathBoundary_DropsOnceEvenWhenResolvedTwice()
        {
            var input = MineEnvironmentTests.Input(Flag(), MineEnvironmentTests.Mine(11, 2, 100), Flag(12, 3));
            var victim = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy);
            victim = HeldEnvironmentEffects.AcquireFlag(victim, Flag(20));
            input = MineEnvironmentTests.Copy(input, characters: input.Characters.Append(victim).ToArray());
            var projection = new MovementProjection(input);
            projection.ChangeHealth(new MovementHealthRequest(victim.EntityId, 100), false,
                new CombatDamageOrigin(new CellCoord(4, 4), null, 1));
            projection.ResolveDeathBoundary();
            projection.ResolveDeathBoundary();
            var result = projection.Complete();
            var drop = result.FinalState.Environment.Tiles.OfType<FlagSourceFact>().Single(t => t.Id > 12);
            Assert.That(drop.Cell, Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(drop.PriorityBonus, Is.EqualTo(7));
            Assert.That(drop.EntryOrdinal, Is.EqualTo(4));
            Assert.That(result.EnvironmentBindings.Final.ObjectSourceIds, Has.Member(drop.Id));
            Assert.That(result.FinalState.EnvironmentProgress.NextSourceId, Is.GreaterThan(drop.Id));
            Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == victim.EntityId).HeldEnvironment.Flag, Is.Null);
        }

        [Test]
        public void TerminalOutcome_ClearsHeldFlagWithoutCreatingDeathDrop()
        {
            var input = MineEnvironmentTests.Input(MineEnvironmentTests.Mine(damage: 100));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            input = MineEnvironmentTests.Copy(input, characters: input.Characters.Select(c => c.EntityId == actor.EntityId
                ? HeldEnvironmentEffects.AcquireFlag(c, Flag()) : c).ToArray());
            var projection = new MovementProjection(input);
            projection.ChangeHealth(new MovementHealthRequest(new EntityId(10), 100), false,
                new CombatDamageOrigin(new CellCoord(7, 6), null, 1));
            var result = projection.Complete();
            Assert.That(result.Outcome.IsStageEnded, Is.True);
            Assert.That(result.FinalState.Environment.Tiles.OfType<FlagSourceFact>(), Is.Empty);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == actor.EntityId).Final.HeldEnvironment.Flag, Is.Null);
        }
    }
}
