using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Framework
{
    public sealed class MovementPublicationTests
    {
        [Test]
        public void StaleObjectOrdinalProgress_RejectsWithoutAnyPublication()
        {
            var b = CharacterBattleSettlementTests.Create();
            var i = CombatCommandPublicationTests.Input(b);
            var s = i.State;
            var staleState = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId,
                s.IsNight, s.Structures, s.ThornFields, s.Timeline, s.Turn, s.Environment,
                new EnvironmentProgressFacts(s.EnvironmentProgress.NextObjectEntryOrdinal + 1));
            var stale = new CombatCommandInput(i.Number, i.Kind, i.ActorId, i.Aim, staleState, i.Characters,
                i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts);
            var transition = new CombatCommandTransition(stale, true, "",
                CharacterSettlementCalculator.SetHealth(i.Characters.Single(), i.Main, 4),
                new SoulFieldChange(i.Souls, i.Souls), staleState,
                new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None));
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(transition));
            Assert.That(b.CharacterReadSink.Characters.Single().Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(b.CombatStateReadSink.Current.CommandState.EnvironmentProgress.NextObjectEntryOrdinal,
                Is.EqualTo(s.EnvironmentProgress.NextObjectEntryOrdinal));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.Zero);
        }

        internal static CombatViewCommandData PreviewedMove(IBufferQuery b, ICombatProcessingService service,
            EntityId actor, params CellCoord[] cells)
        {
            var path = new MovementPath(cells);
            b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Move(actor, path));
            Assert.That(service.ProcessLatestPreview(), Is.True);
            Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.Move.Value.CanMove, Is.True, preview.Move.Value.FailureReason.ToString());
            return CombatViewCommandData.Move(actor, path, preview.MovementContext);
        }

        [Test]
        public void HighGround_PublishesActualCostAndRefundsRecordedCostAfterTerrainChanges()
        {
            var b = CombatPreviewPublicationTests.Create(out var service);
            var actor = Main(b);
            var destination = actor.Anchor.Value + CellCoord.Up;
            PrepareTerrain(b, new StaticTerrainFact(71, destination, StaticTerrainKind.HighGround,
                true, new[] { Owner.Player }));
            var command = PreviewedMove(b, service, actor.EntityId, actor.Anchor.Value, destination);
            b.CombatStateReadSink.TryGetLatestPreview(out var preview);
            Assert.That(preview.Move.Value.QuotedActionCost, Is.EqualTo(3));
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(4));
            b.CombatViewWriteSink.Submit(command);
            Assert.That(service.ProcessNextCommand(), Is.True);
            b.CombatResultReadSink.TryGetLatest(out var moved);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(moved.PathMovement.ActionCostSpent, Is.EqualTo(3));
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(1));
            Assert.That(Main(b).PendingMoveActionCost, Is.EqualTo(3));
            Assert.That(Main(b).PendingMoveDistance, Is.EqualTo(1));
            Assert.That(Main(b).AttackRefundEligibleMoveCost, Is.EqualTo(3));
            SetEnvironment(b, MovementEnvironmentFacts.Empty);
            b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ReturnToCheckpoint(actor.EntityId));
            Assert.That(service.ProcessLatestPreview(), Is.True);
            b.CombatStateReadSink.TryGetLatestPreview(out var returnedPreview);
            b.CombatViewWriteSink.Submit(CombatViewCommandData.ReturnToCheckpoint(actor.EntityId,
                returnedPreview.MovementContext));
            Assert.That(service.ProcessNextCommand(), Is.True);
            b.CombatResultReadSink.TryGetLatest(out var returned);
            Assert.That(returned.Accepted, Is.True, returned.Reason);
            Assert.That(Main(b).Anchor, Is.EqualTo(actor.Anchor));
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(4));
            Assert.That(Main(b).PendingMoveActionCost, Is.Zero);
            Assert.That(Main(b).PendingMoveDistance, Is.Zero);
            Assert.That(Main(b).AttackRefundEligibleMoveCost, Is.Zero);
        }

        [Test]
        public void HighGround_OriginChangeRejectsStalePreviewWithoutChargingMovement()
        {
            var b = CombatPreviewPublicationTests.Create(out var service);
            var actor = Main(b);
            var destination = actor.Anchor.Value + CellCoord.Up;
            var origin = new StaticTerrainFact(71, actor.Anchor.Value, StaticTerrainKind.HighGround,
                true, new[] { Owner.Player });
            var high = new StaticTerrainFact(72, destination, StaticTerrainKind.HighGround,
                true, new[] { Owner.Player });
            PrepareTerrain(b, origin, high);
            var command = PreviewedMove(b, service, actor.EntityId, actor.Anchor.Value, destination);
            b.CombatStateReadSink.TryGetLatestPreview(out var preview);
            Assert.That(preview.Move.Value.QuotedActionCost, Is.EqualTo(1));
            SetEnvironment(b, new MovementEnvironmentFacts(new MovementTileFact[] { high }));
            b.CombatViewWriteSink.Submit(command);
            Assert.That(service.ProcessNextCommand(), Is.True);
            b.CombatResultReadSink.TryGetLatest(out var result);
            Assert.That(result.Reason, Is.EqualTo("StaleContext"));
            Assert.That(result.Events, Is.Empty);
            Assert.That(Main(b).Anchor, Is.EqualTo(actor.Anchor));
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(4));
            Assert.That(Main(b).PendingMoveActionCost, Is.Zero);
            Assert.That(Main(b).PendingMoveDistance, Is.Zero);
        }

        [Test]
        public void HighGround_HiddenChangeKeepsPreviewValidAndChargesAuthoritativeCost()
        {
            var b = CombatPreviewPublicationTests.Create(out var service);
            var actor = Main(b);
            var destination = actor.Anchor.Value + CellCoord.Up;
            PrepareTerrain(b, new StaticTerrainFact(71, destination, StaticTerrainKind.HighGround,
                false, new[] { Owner.Enemy }));
            var command = PreviewedMove(b, service, actor.EntityId, actor.Anchor.Value, destination);
            b.CombatStateReadSink.TryGetLatestPreview(out var preview);
            Assert.That(preview.Move.Value.QuotedActionCost, Is.EqualTo(1));
            Assert.That(preview.MovementContext.VisibleEnvironment.Tiles, Is.Empty);
            Assert.That(preview.MovementContext.VisibleTerrainBindings.TerrainSourceIds, Is.Empty);
            SetEnvironment(b, new MovementEnvironmentFacts(new MovementTileFact[] {
                new StaticTerrainFact(71, destination, StaticTerrainKind.HighGround, true, new[] { Owner.Enemy }) }));
            b.CombatViewWriteSink.Submit(command);
            Assert.That(service.ProcessNextCommand(), Is.True);
            b.CombatResultReadSink.TryGetLatest(out var moved);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(moved.PathMovement.ActionCostSpent, Is.EqualTo(3));
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(1));
        }

        private static void PrepareTerrain(IBufferQuery b, params StaticTerrainFact[] terrain)
        {
            var actor = Main(b);
            var state = b.CombatStateReadSink.Current.CommandState;
            var environment = new MovementEnvironmentFacts(terrain.Cast<MovementTileFact>().ToArray());
            var bindings = new EnvironmentSemanticBindings(terrain.Select(t => t.Id).ToArray(),
                Array.Empty<long>(), Array.Empty<EntityId>());
            var combat = new CombatPreparation(8, 6, 4, 100, 73, CombatFlowPhase.PlayerInput,
                Array.Empty<CharacterInstanceId>(), environment: environment, environmentBindings: bindings);
            b.BattleWriteSink.EndBattle();
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { actor }, combat,
                state.Structures, commandState: State(state, environment)));
        }

        [Test]
        public void Preview_IsPureAndMatchesKnownExecution_FromComposedEnvironment()
        {
            var b = CombatPreviewPublicationTests.Create(out var service, new IMovementTileRule[] { new ThornMovementTileRule(), new PublicationRule() });
            var actor = Main(b);
            var entry = actor.Anchor.Value + CellCoord.Up;
            var stop = entry + CellCoord.Right;
            SetEnvironment(b, new MovementEnvironmentFacts(new MovementTileFact[] {
                new PublicationTile(8, entry, false), new PublicationTile(9, entry, true),
                new DestinationTileModifierFact(7, stop, true, new[] { Owner.Player }, 2, 5) }));
            var sequence = b.CombatStateReadSink.LatestResultSequence;
            var path = new MovementPath(new[] { actor.Anchor.Value, entry, stop });
            var command = PreviewedMove(b, service, actor.EntityId, path.Cells.ToArray());
            b.CombatStateReadSink.TryGetLatestPreview(out var preview);
            Assert.That(Main(b).Health, Is.EqualTo(actor.Health));
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(4));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
            Assert.That(b.CombatStateReadSink.Current.Environment.Tiles.All(t => t.Active), Is.True);
            Assert.That(preview.Move.Value.ProjectedActor.Health, Is.EqualTo(new HealthStateSnapshot(7, 15)));
            Assert.That(preview.Move.Value.ProjectedActor.Stats.CurrentActionCost, Is.EqualTo(2));
            Assert.That(preview.Move.Value.ProjectedActor.PositionModifier.SourceIds, Is.EqualTo(new long[] { 7 }));
            Assert.That(preview.Move.Value.CanReturn, Is.False);
            int number = b.CombatViewWriteSink.Submit(command);
            Assert.That(service.ProcessNextCommand(), Is.True);
            Assert.That(service.ProcessNextCommand(), Is.False);
            b.CombatResultReadSink.TryGetLatest(out var result);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(Main(b).Health, Is.EqualTo(preview.Move.Value.ProjectedActor.Health));
            Assert.That(Main(b).Stats.EffectiveAttackPower, Is.EqualTo(preview.Move.Value.ProjectedActor.Stats.EffectiveAttackPower));
            Assert.That(Main(b).PositionModifier.SameValues(preview.Move.Value.ProjectedActor.PositionModifier), Is.True);
            Assert.That(result.PathMovement.EnteredCells, Is.EqualTo(preview.Move.Value.PathMovement.EnteredCells));
            Assert.That(result.Events.Select(e => e.Kind), Is.EqualTo(preview.Move.Value.PublicEffects.Select(e => e.Kind)));
            Assert.That(b.CombatStateReadSink.Current.Environment.Tiles.Where(t => t.Id != 7).All(t => !t.Active), Is.True);
        }

        [Test]
        public void UnknownMine_DoesNotLeakThroughPreview_OrCauseHiddenOnlyStaleRejection()
        {
            var rules = new IMovementTileRule[] { new ThornMovementTileRule(), new PublicationRule() };
            var b = CombatPreviewPublicationTests.Create(out var service, rules);
            var actor = Main(b);
            var entry = actor.Anchor.Value + CellCoord.Up;
            var path = new MovementPath(new[] { actor.Anchor.Value, entry, entry + CellCoord.Right });
            PreviewedMove(b, service, actor.EntityId, path.Cells.ToArray());
            b.CombatStateReadSink.TryGetLatestPreview(out var clear);
            SetEnvironment(b, new MovementEnvironmentFacts(new MovementTileFact[] {
                new PublicationTile(80, entry, false, knownTo: Array.Empty<Owner>()) }));
            var hiddenCommand = PreviewedMove(b, service, actor.EntityId, path.Cells.ToArray());
            b.CombatStateReadSink.TryGetLatestPreview(out var hidden);
            Assert.That(MovementContextCalculator.SameValues(clear.MovementContext, hidden.MovementContext), Is.True);
            Assert.That(hidden.MovementContext.VisibleEnvironment.Tiles, Is.Empty);
            Assert.That(ProjectL.Systems.RuntimeSystem.BufferSystems.Character.CharacterCombatFactsComparer.SameFacts(
                hidden.Move.Value.ProjectedActor, clear.Move.Value.ProjectedActor), Is.True);
            Assert.That(hidden.Move.Value.CanMove, Is.EqualTo(clear.Move.Value.CanMove));
            Assert.That(hidden.Move.Value.FailureReason, Is.EqualTo(clear.Move.Value.FailureReason));
            Assert.That(hidden.Move.Value.ValidStepCount, Is.EqualTo(clear.Move.Value.ValidStepCount));
            Assert.That(hidden.Move.Value.ProjectedActor.Health, Is.EqualTo(clear.Move.Value.ProjectedActor.Health));
            Assert.That(hidden.Move.Value.ProjectedActor.Stats.CurrentActionCost, Is.EqualTo(clear.Move.Value.ProjectedActor.Stats.CurrentActionCost));
            Assert.That(hidden.Move.Value.QuotedActionCost, Is.EqualTo(clear.Move.Value.QuotedActionCost));
            Assert.That(hidden.Move.Value.QuotedDistance, Is.EqualTo(clear.Move.Value.QuotedDistance));
            Assert.That(hidden.Move.Value.PathMovement.EnteredCells, Is.EqualTo(clear.Move.Value.PathMovement.EnteredCells));
            Assert.That(hidden.Move.Value.PathMovement.FinalStop, Is.EqualTo(clear.Move.Value.PathMovement.FinalStop));
            Assert.That(hidden.Move.Value.PathMovement.ReturnBarrierOccurred, Is.EqualTo(clear.Move.Value.PathMovement.ReturnBarrierOccurred));
            Assert.That(hidden.Move.Value.CanReturn, Is.EqualTo(clear.Move.Value.CanReturn));
            Assert.That(hidden.Move.Value.PublicEffects.Select(e => e.Kind), Is.EqualTo(clear.Move.Value.PublicEffects.Select(e => e.Kind)));
            SetEnvironment(b, new MovementEnvironmentFacts(new MovementTileFact[] {
                new PublicationTile(80, entry, false) }));
            b.CombatViewWriteSink.Submit(hiddenCommand);
            Assert.That(service.ProcessNextCommand(), Is.True);
            b.CombatResultReadSink.TryGetLatest(out var discovered);
            Assert.That(discovered.Reason, Is.EqualTo("StaleContext"));
            Assert.That(discovered.Events, Is.Empty);
            Assert.That(Main(b).Anchor, Is.EqualTo(actor.Anchor));
            SetEnvironment(b, new MovementEnvironmentFacts(new MovementTileFact[] {
                new PublicationTile(81, path.Endpoint, false, knownTo: Array.Empty<Owner>()) }));
            b.CombatViewWriteSink.Submit(hiddenCommand);
            Assert.That(service.ProcessNextCommand(), Is.True);
            b.CombatResultReadSink.TryGetLatest(out var result);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(result.PathMovement.ReturnBarrierOccurred, Is.True);
            Assert.That(Main(b).PendingMoveActionCost, Is.Zero);
            Assert.That(b.CombatStateReadSink.Current.Environment.Tiles.Single().Active, Is.False);
        }

        [Test]
        public void DirectHealthChange_InvalidatesContext_WithoutResultSequenceChange()
        {
            var b = CombatPreviewPublicationTests.Create(out var service);
            var actor = Main(b);
            var command = PreviewedMove(b, service, actor.EntityId, actor.Anchor.Value, actor.Anchor.Value + CellCoord.Up);
            var sequence = b.CombatStateReadSink.LatestResultSequence;
            service.ApplyHealth(actor.EntityId, 6);
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
            b.CombatViewWriteSink.Submit(command);
            Assert.That(service.ProcessNextCommand(), Is.True);
            b.CombatResultReadSink.TryGetLatest(out var result);
            Assert.That(result.Reason, Is.EqualTo("StaleContext"));
            Assert.That(result.Events, Is.Empty);
            Assert.That(Main(b).Anchor, Is.EqualTo(actor.Anchor));
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(6));
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(4));
            Assert.That(service.ProcessNextCommand(), Is.False);
        }

        [Test]
        public void ReusedActorIdInNewBattle_RejectsOldContext_AndPreparationOverflowIsAtomic()
        {
            var b = CombatPreviewPublicationTests.Create(out var service);
            var actor = Main(b);
            var command = PreviewedMove(b, service, actor.EntityId, actor.Anchor.Value, actor.Anchor.Value + CellCoord.Up);
            var state = b.CombatStateReadSink.Current.CommandState;
            var previous = b.CombatStateReadSink.BattleInstanceId;
            b.BattleWriteSink.EndBattle();
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { actor }, CharacterBufferTestData.Combat(), state.Structures, commandState: state));
            Assert.That(b.CombatStateReadSink.BattleInstanceId, Is.EqualTo(previous + 1));
            b.CombatViewWriteSink.Submit(command);
            service.ProcessNextCommand();
            b.CombatResultReadSink.TryGetLatest(out var result);
            Assert.That(result.Reason, Is.EqualTo("StaleContext"));
            Assert.That(Main(b).Anchor, Is.EqualTo(actor.Anchor));
            b.BattleWriteSink.EndBattle();
            typeof(ProjectL.Systems.RuntimeSystem.BufferSystems.Combat.CombatStateBuffer)
                .GetField("<BattleInstanceId>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(b.CombatStateReadSink, long.MaxValue);
            Assert.Throws<OverflowException>(() => b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { actor },
                CharacterBufferTestData.Combat(), state.Structures, commandState: state)));
            Assert.That(b.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(b.CharacterReadSink.Characters, Is.Empty);
        }

        [Test]
        public void BookkeepingAndUnrelatedLiveBlockerHealth_DoNotInvalidate_AndReturnDoesNotAcquire()
        {
            var b = CombatPreviewPublicationTests.Create(out var service);
            var actor = Main(b);
            var command = PreviewedMove(b, service, actor.EntityId, actor.Anchor.Value, actor.Anchor.Value + CellCoord.Up);
            b.CombatStateReadSink.TryGetLatestPreview(out var old);
            int newest = b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(actor.EntityId));
            service.ProcessLatestPreview();
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.PublishPreview(old));
            Assert.That(b.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(newest));
            b.CombatViewWriteSink.AcknowledgePresented(1);
            var input = CombatCommandPublicationTests.Input(b);
            var hq = input.State.Structures.Single();
            var changed = new StructureCombatFacts(hq.EntityId, hq.Definition, new HealthStateSnapshot(19, 20), hq.Anchor,
                hq.Rotation, hq.OccupiedCells, hq.Owner);
            var s = input.State;
            b.BattleWriteSink.Apply(new CombatCommandTransition(input, true, "", Empty(), new SoulFieldChange(input.Souls, input.Souls),
                new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight, new[] { changed },
                    s.ThornFields, s.Timeline, s.Turn, s.Environment), InProgress()));
            b.CombatViewWriteSink.Submit(command);
            Assert.That(service.ProcessNextCommand(), Is.True);
            b.CombatResultReadSink.TryGetLatest(out var moved);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(service.ProcessNextCommand(), Is.False);
            SetEnvironment(b, new MovementEnvironmentFacts(new MovementTileFact[] {
                new DestinationTileModifierFact(7, actor.Anchor.Value, true, new[] { Owner.Player }, 2, 5),
                new PublicationTile(80, actor.Anchor.Value, false) }));
            b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ReturnToCheckpoint(actor.EntityId));
            service.ProcessLatestPreview();
            b.CombatStateReadSink.TryGetLatestPreview(out var returnedPreview);
            Assert.That(returnedPreview.CheckpointReturn.CanReturn, Is.True);
            Assert.That(returnedPreview.MovementContext.Thorns, Is.Empty);
            Assert.That(returnedPreview.MovementContext.Souls, Is.Empty);
            Assert.That(returnedPreview.MovementContext.VisibleEnvironment.Tiles.Select(t => t.Id), Is.EqualTo(new long[] { 7 }));
            b.CombatViewWriteSink.Submit(CombatViewCommandData.ReturnToCheckpoint(actor.EntityId, returnedPreview.MovementContext));
            service.ProcessNextCommand();
            b.CombatResultReadSink.TryGetLatest(out var returned);
            Assert.That(returned.Accepted, Is.True, returned.Reason);
            Assert.That(Main(b).Anchor, Is.EqualTo(actor.Anchor));
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(4));
            Assert.That(Main(b).Health, Is.EqualTo(new HealthStateSnapshot(7, 15)));
            Assert.That(Main(b).PositionModifier.SourceIds, Is.EqualTo(new long[] { 7 }));
            Assert.That(b.CombatStateReadSink.Current.Environment.Tiles.Single(t => t.Id == 80).Active, Is.True);
            Assert.That(returned.Events.Select(e => e.Kind), Is.EqualTo(new[] { CombatEventKind.Returned, CombatEventKind.HealthNormalized }));
        }

        private static void SetEnvironment(IBufferQuery b, MovementEnvironmentFacts environment)
        {
            var source = CombatCommandPublicationTests.Input(b);
            // 픽스처의 외부 환경 게시가 View 명령 번호를 미리 소비하지 않도록 Advance 번호를 쓴다.
            var input = new CombatCommandInput(0, source.Kind, source.ActorId, source.Aim, source.State,
                source.Characters, source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls,
                source.Artifacts, processingKind: CombatProcessingKind.Advance,
                environmentBindings: source.EnvironmentBindings);
            b.BattleWriteSink.Apply(new CombatCommandTransition(input, true, "", Empty(), new SoulFieldChange(input.Souls, input.Souls),
                State(input.State, environment), InProgress()));
        }
        [Test]
        public void UnchangedOccupantMutation_RejectsPreparedRouteWithoutPartialPublication()
        {
            var b = CombatPreviewPublicationTests.Create(out _);
            var actor = Main(b);
            var enemy = CharacterBufferTestData.EnemyFacts(new EntityId(50));
            enemy = new CharacterCombatFacts(enemy.EntityId, null, enemy.CharacterKey, enemy.Owner, null,
                CharacterRuntimeArea.Field, enemy.Health, enemy.Stats, null, new CellCoord(6, 4), Rotation4.R0,
                new[] { new CellCoord(6, 4) }, new CellCoord(6, 4), 0, 0);
            var state = b.CombatStateReadSink.Current.CommandState;
            b.BattleWriteSink.EndBattle();
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { actor, enemy }, CharacterBufferTestData.Combat(),
                state.Structures, commandState: state));
            var input = RouteInput(b, new MovementPath(new[] { actor.Anchor.Value, actor.Anchor.Value + CellCoord.Up }));
            var proposal = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(proposal.Accepted, Is.True, proposal.Reason);
            Assert.That(proposal.Characters.Characters.Any(c => c.Final.EntityId == enemy.EntityId), Is.False);
            b.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(enemy,
                CharacterBufferTestData.WithHealth(enemy, 4)) }, Array.Empty<SoulCountChange>()));
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(proposal));
            Assert.That(b.CharacterReadSink.Characters.Single(c => c.EntityId == actor.EntityId).Anchor, Is.EqualTo(actor.Anchor));
            Assert.That(b.CharacterReadSink.Characters.Single(c => c.EntityId == actor.EntityId).Stats.CurrentActionCost, Is.EqualTo(4));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.Zero);
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.Zero);
            Assert.That(b.CombatStateReadSink.SoulField.SameValues(input.Souls), Is.True);
        }

        private static CombatCommandInput RouteInput(IBufferQuery b, MovementPath path)
        {
            var actor = b.CharacterReadSink.Characters.Single(c => c.Owner == Owner.Player).ToFacts();
            b.CharacterReadSink.TryGetMainState(out var main);
            var definition = CharacterBattlePreparationTests.Initialization().Units.Single(u => u.Key == actor.CharacterKey);
            return new CombatCommandInput(1, CombatCommandKind.Move, actor.EntityId, default,
                b.CombatStateReadSink.Current.CommandState, b.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray(),
                new[] { new CombatAttackProfile(definition, new TargetingMethodSpec(TargetingShape.RadialSquare,
                    TargetingChoiceMode.Single, TargetingSplash.None, 0)) }, main, b.CharacterReadSink.Owned,
                b.CharacterReadSink.Owned.ToDictionary(o => o.Id, o => b.CharacterReadSink.GetSoulCount(o.Id)),
                b.CombatStateReadSink.SoulField, default, path: path);
        }

        [TestCase(5, 15, 7, false)]
        [TestCase(-4, 6, 6, false)]
        [TestCase(5, 15, 7, true)]
        public void RouteAndReturn_PublishLedgerAndOrderedEventsOncePerCommand(int positionMax, int finalMax, int finalHp, bool entryEffects)
        {
            var b = CharacterBattleSettlementTests.Create();
            var field = b.CombatStateReadSink.SoulField;
            b.BattleWriteSink.Apply(CharacterSettlementCalculator.EndTurn(Main(b),
                new SoulActionChanges(Empty(), new SoulFieldChange(field, field), Array.Empty<long>())));
            var actor = Main(b);
            b.BattleWriteSink.EndBattle();
            var tiles = new List<MovementTileFact> {
                new DestinationTileModifierFact(7, actor.Anchor.Value + CellCoord.Up, true,
                    new[] { Owner.Player }, 2, positionMax) };
            if (entryEffects)
            {
                tiles.Add(new PublicationTile(8, actor.Anchor.Value + CellCoord.Right, false));
                tiles.Add(new PublicationTile(9, actor.Anchor.Value + CellCoord.Right, true));
            }
            var environment = new MovementEnvironmentFacts(tiles);
            var rules = Array.AsReadOnly(new IMovementTileRule[] { new PublicationRule() });
            var state = new CombatCommandState(8, 6, CombatFlowPhase.PlayerInput, actor.EntityId, false,
                Array.Empty<StructureCombatFacts>(), Array.Empty<ThornFieldCellSnapshot>(),
                new[] { CombatTimelineEntrySnapshot.ForUnit(actor.EntityId, actor.Stats.Priority, 0, true) },
                new CombatTurnFacts(StagePhase.Combat), environment);
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { actor },
                new CombatPreparation(8, 6, 4, 100, 73, CombatFlowPhase.PlayerInput,
                    Array.Empty<CharacterInstanceId>(), environment: environment), commandState: state));
            var source = CombatCommandPublicationTests.Input(b);
            var definition = CharacterBattlePreparationTests.Initialization().Units.Single(u => u.Key == actor.CharacterKey);
            var profiles = new[] { new CombatAttackProfile(definition,
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)) };
            var from = actor.Anchor.Value;
            var path = new MovementPath(new[] { from, from + CellCoord.Right,
                from + CellCoord.Right + CellCoord.Up, from + CellCoord.Up });
            var input = new CombatCommandInput(1, CombatCommandKind.Move, actor.EntityId, default, source.State,
                source.Characters, profiles, source.Main, source.Owned, source.SoulCounts, source.Souls,
                source.Artifacts, path: path);
            var moved = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(Main(b).PendingMoveDistance, Is.Zero);
            b.BattleWriteSink.Apply(moved);
            Assert.That(Main(b).PendingMoveDistance, Is.EqualTo(entryEffects ? 0 : 3));
            Assert.That(Main(b).PendingMoveActionCost, Is.EqualTo(entryEffects ? 0 : 3));
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(1));
            Assert.That(Main(b).Stats.EffectiveAttackPower, Is.EqualTo(actor.Stats.EffectiveAttackPower + 2));
            Assert.That(Main(b).Stats.PositionAttackBonus, Is.EqualTo(2));
            Assert.That(Main(b).PositionModifier.SourceIds, Is.EqualTo(new long[] { 7 }));
            Assert.That(Main(b).Health, Is.EqualTo(new HealthStateSnapshot(finalHp, finalMax)));
            b.CharacterReadSink.TryGetMainState(out var movedMain);
            Assert.That(movedMain.CurrentHealth, Is.EqualTo(finalHp));
            Assert.That(movedMain.PersistentMaxHealth, Is.EqualTo(10));
            Assert.That(b.CombatStateReadSink.Current.Environment.Tiles.Single(t => t.Id == 7).SameValues(tiles[0]), Is.True);
            Assert.That(moved.PathMovement.ReturnBarrierOccurred, Is.EqualTo(entryEffects));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var first), Is.True);
            Assert.That(first.PathMovement.DistanceSpent, Is.EqualTo(3));
            Assert.That(first.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved)
                .Select(e => e.Final.Anchor.Value), Is.EqualTo(path.Cells.Skip(1)));

            var current = CombatCommandPublicationTests.Input(b, 2);
            var returnInput = new CombatCommandInput(2, CombatCommandKind.ReturnToCheckpoint, actor.EntityId, default,
                current.State, current.Characters, current.Profiles, current.Main, current.Owned,
                current.SoulCounts, current.Souls, current.Artifacts);
            var returned = CombatCommandCalculator.Calculate(returnInput, rules);
            if (entryEffects)
            {
                Assert.That(returned.Accepted, Is.False);
                Assert.That(Main(b).CommittedMoveDistance, Is.EqualTo(3));
                Assert.That(Main(b).MovementCheckpointAnchor, Is.EqualTo(path.Endpoint));
                Assert.That(Main(b).AttackRefundEligibleMoveCost, Is.EqualTo(3));
                Assert.That(b.CombatStateReadSink.Current.Environment.Tiles.Where(t => t.Id != 7).All(t => !t.Active), Is.True);
                Assert.That(environment.Tiles.All(t => t.Active), Is.True);
                Assert.That(first.Events.Count(e => e.Kind == CombatEventKind.MovementSourceChanged), Is.EqualTo(2));
                return;
            }
            Assert.That(returned.Accepted, Is.True, returned.Reason);
            b.BattleWriteSink.Apply(returned);
            Assert.That(Main(b).Anchor, Is.EqualTo(from));
            Assert.That(Main(b).PendingMoveDistance, Is.Zero);
            Assert.That(Main(b).PendingMoveActionCost, Is.Zero);
            Assert.That(Main(b).AttackRefundEligibleMoveCost, Is.Zero);
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(4));
            Assert.That(Main(b).Stats.RemainingMoveRange, Is.EqualTo(3));
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(finalHp));
            Assert.That(Main(b).Health.MaxHealth, Is.EqualTo(10));
            Assert.That(Main(b).Stats.EffectiveAttackPower, Is.EqualTo(actor.Stats.EffectiveAttackPower));
            Assert.That(Main(b).PositionModifier.SameValues(PositionTileModifier.None), Is.True);
            Assert.That(b.CombatStateReadSink.Current.Environment.SameValues(environment), Is.True);
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(2));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(2));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var final), Is.True);
            Assert.That(final.ReturnMovement.DistanceRefunded, Is.EqualTo(3));
            Assert.That(final.Events.Select(e => e.Kind),
                Is.EqualTo(new[] { CombatEventKind.Returned, CombatEventKind.HealthNormalized }));
        }

        [TestCase(5, 18)]
        [TestCase(-2, 11)]
        public void PositionModifier_RoundTripsWithoutChangingTemporaryBonuses(int positionMaxHealth, int maxHealth)
        {
            var b = CharacterBattleSettlementTests.Create();
            var original = Main(b);
            b.CharacterReadSink.TryGetMainState(out var main);
            var withBonuses = CharacterBufferTestData.MainFacts(original.OwnedId.Value, main,
                temporaryAttack: 2, temporaryMaxHealth: 3);
            var modified = new CharacterCombatFacts(withBonuses.EntityId, withBonuses.OwnedId, withBonuses.CharacterKey,
                withBonuses.Owner, withBonuses.OwnershipRole, withBonuses.Area,
                new HealthStateSnapshot(7, maxHealth), withBonuses.Stats, withBonuses.ReserveSlotIndex,
                withBonuses.Anchor, withBonuses.Rotation, withBonuses.OccupiedCells, withBonuses.MovementCheckpointAnchor,
                0, 0, withBonuses.TemporaryAttackBonus, withBonuses.TemporaryMaxHealthBonus, positionModifier:
                new PositionTileModifier(original.Anchor, new long[] { 7 }, 0, positionMaxHealth));
            b.BattleWriteSink.Apply(Change(original, modified));
            b.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(Main(b), main, 6));
            var stored = Main(b);
            Assert.That(stored.PositionModifier.MaxHealthBonus, Is.EqualTo(positionMaxHealth));
            Assert.That(stored.TemporaryAttackBonus, Is.EqualTo(2));
            Assert.That(stored.TemporaryMaxHealthBonus, Is.EqualTo(3));
            Assert.That(stored.Health, Is.EqualTo(new HealthStateSnapshot(6, maxHealth)));
            Assert.That(stored.Stats.AttackPower, Is.EqualTo(original.Stats.AttackPower));
            Assert.That(stored.Stats.ArtifactAttackBonus, Is.EqualTo(2));
        }

        [TestCase("distance")]
        [TestCase("refund")]
        [TestCase("source")]
        public void NewLedgerFacts_RoundTripAndRejectStaleExpectedValues(string changed)
        {
            var b = CharacterBattleSettlementTests.Create();
            var initial = Main(b);
            var modifier = new PositionTileModifier(initial.Anchor, new long[] { 7 }, 0, 0);
            var seeded = Ledger(initial, 2, 3, modifier);
            b.BattleWriteSink.Apply(Change(initial, seeded));
            var view = b.CharacterReadSink.Characters.Single();
            Assert.That(view.PendingMoveDistance, Is.EqualTo(2));
            Assert.That(view.AttackRefundEligibleMoveCost, Is.EqualTo(3));
            Assert.That(view.PositionModifier.SameValues(modifier), Is.True);

            var input = CombatCommandPublicationTests.Input(b);
            var stale = Main(b);
            var current = Ledger(stale, changed == "distance" ? 3 : 2, changed == "refund" ? 4 : 3,
                changed == "source" ? new PositionTileModifier(stale.Anchor, new long[] { 8 }, 0, 0) : modifier);
            b.BattleWriteSink.Apply(Change(stale, current));
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] { Tile(true) });
            var proposal = new CombatCommandTransition(input, true, "",
                CharacterSettlementCalculator.SetHealth(stale, input.Main, 4),
                new SoulFieldChange(input.Souls, input.Souls), State(input.State, environment), InProgress());

            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(proposal));
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(Main(b).PendingMoveDistance, Is.EqualTo(current.PendingMoveDistance));
            Assert.That(Main(b).AttackRefundEligibleMoveCost, Is.EqualTo(current.AttackRefundEligibleMoveCost));
            Assert.That(Main(b).PositionModifier.SameValues(current.PositionModifier), Is.True);
            Assert.That(b.CombatStateReadSink.Current.Environment.Tiles, Is.Empty);
            b.CharacterReadSink.TryGetMainState(out var persistent);
            Assert.That(persistent.CurrentHealth, Is.EqualTo(7));
            Assert.That(b.CombatStateReadSink.SoulField.SameValues(input.Souls), Is.True);
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.Zero);
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.Zero);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out _), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EnvironmentPreparation_PublicationAndStaleValidationAreAtomic(bool visibilityChange)
        {
            var b = CharacterBattleSettlementTests.Create();
            b.BattleWriteSink.EndBattle();
            b.CharacterReadSink.TryGetMainState(out var main);
            var definition = CharacterBattlePreparationTests.Initialization();
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] { Tile(true) });
            var initialization = new BattleInitialization(definition.Stage, definition.MainAnchor, definition.MainRotation,
                definition.Units, definition.Structures, definition.Categories, definition.AttackProfiles,
                definition.Economy, environment);
            var preparation = BattlePreparationCalculator.Prepare(initialization, b.CharacterReadSink.Owned, main,
                b.CharacterReadSink.Owned.ToDictionary(o => o.Id, o => b.CharacterReadSink.GetSoulCount(o.Id)),
                new InitialDrawPlan(Array.Empty<CharacterInstanceId>()), Array.Empty<CharacterCombatFacts>());
            Assert.That(preparation.Combat.Environment.SameValues(environment), Is.True);
            b.BattleWriteSink.PrepareBattle(preparation);
            var input = CombatCommandPublicationTests.Input(b);
            Assert.That(input.State.Environment.SameValues(environment), Is.True);
            var changed = new MovementEnvironmentFacts(new MovementTileFact[]
            {
                new DestinationTileModifierFact(7, new CellCoord(3, 3), visibilityChange,
                    visibilityChange ? new[] { Owner.Enemy } : new[] { Owner.Player }, 0, 0)
            });
            var state = State(input.State, changed);
            b.BattleWriteSink.Apply(new CombatCommandTransition(input, true, "", Empty(),
                new SoulFieldChange(input.Souls, input.Souls), state, InProgress()));
            Assert.That(b.CombatStateReadSink.Current.Environment.SameValues(changed), Is.True);

            var stale = new CombatCommandInput(2, input.Kind, input.ActorId, input.Aim, input.State,
                input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts);
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(new CombatCommandTransition(stale,
                true, "", CharacterSettlementCalculator.SetHealth(Main(b), main, 4),
                new SoulFieldChange(input.Souls, input.Souls), state, InProgress())));
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(b.CombatStateReadSink.Current.Environment.SameValues(changed), Is.True);
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(1));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Sequence, Is.EqualTo(1));
        }

        private static DestinationTileModifierFact Tile(bool active) =>
            new(7, new CellCoord(3, 3), active, new[] { Owner.Player }, 0, 0);

        private sealed class PublicationTile : MovementTileFact
        {
            internal bool Heal { get; }
            internal PublicationTile(long id, CellCoord cell, bool heal, bool active = true, IReadOnlyList<Owner> knownTo = null)
                : base(id, cell, "publication-fixture", 0, active, knownTo ?? new[] { Owner.Player }) { Heal = heal; }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other) &&
                other is PublicationTile tile && tile.Heal == Heal;
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => new PublicationTile(Id, Cell, Heal, Active, knownTo);
        }
        private sealed class PublicationRule : IMovementTileRule
        {
            public string Key => "publication-fixture";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact source)
            {
                var tile = (PublicationTile)source;
                if (tile.Heal)
                {
                    Assert.That(input.Health.CurrentHealth, Is.EqualTo(5));
                    Assert.That(input.SourcesAtCell.Single(t => t.Id == 8).Active, Is.False);
                }
                bool apply = tile.Heal ? input.Phase == MovementEffectPhase.Recovery : input.Phase == MovementEffectPhase.Hazard;
                if (!apply) return new MovementStepEffects();
                return new MovementStepEffects(
                    damage: tile.Heal ? null : new[] { new MovementHealthRequest(input.MoverId, 2) },
                    healing: tile.Heal ? new[] { new MovementHealthRequest(input.MoverId, 2) } : null,
                    expectedSource: tile, finalSource: new PublicationTile(tile.Id, tile.Cell, tile.Heal, false, tile.KnownTo));
            }
        }
        private static CharacterCombatFacts Main(IBufferQuery b) => b.CharacterReadSink.Characters.Single().ToFacts();
        private static CharacterChanges Empty() => new(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>());
        private static StageOutcomeSnapshot InProgress() => new(StageOutcome.InProgress, StageOutcomeReason.None);
        private static CharacterChanges Change(CharacterCombatFacts before, CharacterCombatFacts after) =>
            new(new[] { new CharacterStateChange(before, after) }, Array.Empty<SoulCountChange>());
        private static CharacterCombatFacts Ledger(CharacterCombatFacts c, int distance, int refund, PositionTileModifier modifier) =>
            new(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole, c.Area, c.Health, c.Stats,
                c.ReserveSlotIndex, c.Anchor, c.Rotation, c.OccupiedCells, c.MovementCheckpointAnchor,
                c.CommittedMoveDistance, c.PendingMoveActionCost, c.TemporaryAttackBonus, c.TemporaryMaxHealthBonus,
                distance, refund, modifier);
        private static CombatCommandState State(CombatCommandState s, MovementEnvironmentFacts environment) =>
            new(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight, s.Structures,
                s.ThornFields, s.Timeline, s.Turn, environment,
                new EnvironmentProgressFacts(s.EnvironmentProgress.NextObjectEntryOrdinal,
                    Math.Max(s.EnvironmentProgress.NextSourceId, checked(environment.Tiles.Select(t => t.Id).DefaultIfEmpty(0).Max() + 1)),
                    s.EnvironmentProgress.RandomState));
    }
}
