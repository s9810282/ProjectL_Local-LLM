using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PillarPlatformPublicationTests
    {
        private static readonly CellCoord BoardingCell = new CellCoord(1, 1);
        private static readonly CellCoord SecondCell = new CellCoord(2, 1);
        private static readonly CellCoord RouteEnd = new CellCoord(1, 3);
        private static readonly CellCoord FilledCell = new CellCoord(1, 2);

        [TestCase(false)]
        [TestCase(true)]
        public void ObstacleKnockbackPublishesBlockedEntryAndPlatformContinues(bool pillar)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new ObstacleOnPlatformSource(pillar));
            battle.Submit(battle.Attack(battle.Actor, new CellCoord(0, 2)));
            var state = battle.Buffers.CombatStateReadSink;
            var obstacle = state.Current.CommandState.Structures.Single(s => s.ObstacleDurability != null);
            Assert.That(obstacle.Anchor, Is.EqualTo(new CellCoord(0, 2)));
            Assert.That(obstacle.ObstacleDurability.RemainingHits, Is.EqualTo(2));
            battle.Buffers.CombatResultReadSink.TryGetLatest(out var attack);
            Assert.That(attack.Events.OfType<CombatStructureEvent>().Any(e => e.MovementCause != null), Is.False);
            Assert.That(attack.Events.OfType<CombatFallEvent>(), Is.Empty);
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            battle.Buffers.CombatResultReadSink.TryGetLatest(out var prelude);
            Assert.That(prelude.Events.OfType<CombatPlatformMotionEvent>().Single().StopReason,
                Is.EqualTo(PlatformMotionStopReason.BudgetConsumed));
            Assert.That(state.Current.ReadPlatforms().Single().Cell, Is.EqualTo(new CellCoord(1, 3)));
            Assert.That(state.Current.CommandState.Structures.Single(s => s.ObstacleDurability != null).Anchor, Is.EqualTo(obstacle.Anchor));
            Assert.That(state.Current.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingEnemy));
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            Assert.That(state.Current.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Enemy));
        }

        private sealed class ObstacleOnPlatformSource : IBattleInitializationSource
        {
            private readonly bool _pillar;
            internal ObstacleOnPlatformSource(bool pillar) => _pillar = pillar;
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var obstacle = new InitialObstaclePlacementSpec(new StructureSpec(_pillar ? EntityKey.Structure_Pillar : EntityKey.Structure_PowderBarrel,
                    1, 1, new[] { CellCoord.Zero }, 1, false, 0), _pillar ? ObstacleContentKind.Pillar : ObstacleContentKind.PowderBarrel,
                    new CellCoord(0, 2), Rotation4.R0, 3, 0);
                var source = new PullBattleSource(new KnockbackTraitSpec(1), target: new CellCoord(7, 5)) { Obstacle = obstacle }
                    .Read(database, chapterKey, stageKey);
                var platform = new MovingPlatformFact(new MovingPlatformSpec(100, 1, PlatformAxis.Horizontal,
                    new CellCoord(0, 3), new CellCoord(1, 3), new CellCoord(0, 3), 1));
                var environment = new MovementEnvironmentFacts(new MovementTileFact[] {
                    Terrain(71, new CellCoord(0, 3)), Terrain(72, new CellCoord(1, 3)), platform });
                return new BattleInitialization(source.Stage, source.MainAnchor, source.MainRotation, source.Units,
                    source.Structures, source.Categories, source.AttackProfiles, environment,
                    new EnvironmentSemanticBindings(new long[] { 71, 72, 100 }, Array.Empty<long>(), Array.Empty<EntityId>()),
                    clearRewards: source.ClearRewards, obstaclePlacements: new[] { obstacle });
            }
        }

        [Test]
        public void ForecastUsesThePreviewSlotAndDoesNotAdvanceBattle()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new PlatformBattleSource(moves: true));
            var state = battle.Buffers.CombatStateReadSink;
            var requests = battle.Buffers.CombatViewWriteSink;
            var beforeBoarding = ReadForecast();
            battle.Move(battle.Actor, BoardingCell);
            Assert.That(state.Current.CommandState.Turn.ControllerTurnSequence,
                Is.EqualTo(beforeBoarding.Source.ControllerTurnSequence));
            Assert.That(beforeBoarding.Source.Matches(state.BattleInstanceId, state.QueryRevision, state.Current.CommandState), Is.False);
            var pending = ReadForecast();
            Assert.That(pending.Coverage, Is.EqualTo(PlatformForecastCoverage.KnownInformationConditional));
            Assert.That(pending.Paths.Single().Steps.SelectMany(step => step.Passengers).Count(), Is.EqualTo(2));
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(pending.Source.Matches(state.BattleInstanceId, state.QueryRevision, state.Current.CommandState), Is.False);
            Assert.That(state.TryGetLatestPreview(out _), Is.False);
            var before = battle.CaptureGameplay();
            var forecast = ReadForecast();
            Assert.That(ReadForecast().Paths.Single().Steps.Select(step => step.To),
                Is.EqualTo(forecast.Paths.Single().Steps.Select(step => step.To)));
            Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
            Assert.That(state.Current.ReadPlatforms().Single().TravelSign, Is.EqualTo(1));
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            battle.Buffers.CombatResultReadSink.TryGetLatest(out var result);
            var movement = result.Events.OfType<CombatPlatformMotionEvent>().Single();
            Assert.That(forecast.Paths.Single().Steps.Select(step => (step.To, step.TravelSign)),
                Is.EqualTo(movement.Steps.Select(step => (step.Final.Cell, step.Final.TravelSign))));
            Assert.That(state.Current.ReadPlatforms().Single().TravelSign, Is.EqualTo(movement.Final.TravelSign));
            Assert.That(state.TryGetLatestPreview(out _), Is.False);

            PlatformForecastResult ReadForecast()
            {
                int number = requests.SetPreview(CombatViewPreviewData.PlatformForecast());
                Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
                Assert.That(state.TryGetLatestPreview(out var preview), Is.True);
                Assert.That(preview.RequestNumber, Is.EqualTo(number));
                Assert.That(preview.PlatformForecast, Is.Not.Null);
                return preview.PlatformForecast;
            }
        }

        [Test]
        public void CancelledOrObsoleteForecastCannotPublish()
        {
            using var battle = Create();
            var requests = battle.Buffers.CombatViewWriteSink;
            var state = battle.Buffers.CombatStateReadSink;
            int number = requests.SetPreview(CombatViewPreviewData.PlatformForecast());
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(state.TryGetLatestPreview(out var previous), Is.True);
            Assert.That(previous.PlatformForecast.Source.Matches(state.BattleInstanceId + 1, state.QueryRevision,
                state.Current.CommandState), Is.False);
            int replacement = requests.SetPreview(CombatViewPreviewData.PlatformForecast());
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.PublishPreview(previous));
            Assert.That(requests.ClearPreview(replacement), Is.True);
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.False);
            Assert.That(state.AnsweredPreviewNumber, Is.EqualTo(number));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ForecastDiscardsARequestChangedDuringSimulation(bool replace)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new PlatformBattleSource(moves: true, previewProbe: true));
            battle.Move(battle.Actor, BoardingCell);
            var requests = battle.Buffers.CombatViewWriteSink;
            var state = battle.Buffers.CombatStateReadSink;
            int answered = state.AnsweredPreviewNumber;
            int number = requests.SetPreview(CombatViewPreviewData.PlatformForecast());
            var rule = new ForecastProbeRule(() =>
            {
                if (replace) requests.SetPreview(CombatViewPreviewData.PlatformForecast());
                else requests.ClearPreview(number);
            });
            var service = new CombatPreviewService(battle.Publication().Assembler, new[] { rule });
            var builder = new SystemBuilderHelper();
            builder.AddService<IBufferQuery>(battle.Buffers);
            service.PostBindService(builder.BuildSystemReader().ServiceStoreReader);
            var before = battle.CaptureGameplay();

            Assert.That(service.ProcessLatestPreview(), Is.False);
            Assert.That(rule.Triggered, Is.True);
            Assert.That(state.AnsweredPreviewNumber, Is.EqualTo(answered));
            Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
        }

        private sealed class ForecastProbeRule : IMovementTileRule
        {
            private readonly Action _change;
            internal bool Triggered { get; private set; }
            public string Key => "forecast-probe";
            internal ForecastProbeRule(Action change) { _change = change; }
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
            {
                if (!Triggered)
                {
                    Triggered = true;
                    _change();
                }
                return new MovementStepEffects();
            }
        }

        private sealed class ForecastProbeTile : MovementTileFact
        {
            internal ForecastProbeTile() : this(new[] { Owner.Player, Owner.Enemy }) { }
            private ForecastProbeTile(IReadOnlyList<Owner> known) : base(110, FilledCell, "forecast-probe", 0, true, known) { }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => new ForecastProbeTile(knownTo);
        }

        [TestCase("valid")]
        [TestCase("missing-step")]
        [TestCase("missing-motion")]
        [TestCase("duplicate-boundary")]
        [TestCase("route")]
        [TestCase("rider-teleport")]
        [TestCase("rng")]
        [TestCase("boundary-count")]
        public void PlatformPreludePublishesSeparatelyAndRejectsForgedMotion(string fault)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new PlatformBattleSource(moves: true));
            battle.Move(battle.Actor, BoardingCell);
            battle.Submit(battle.EndPlayerTurn());
            var parts = battle.Publication();
            var state = battle.Buffers.CombatStateReadSink;
            var input = parts.Assembler.AssembleDecision(new CombatActionRequest(0, CombatCommandKind.StartControllerTurn,
                default, default, processingKind: CombatProcessingKind.Advance,
                expectedBattleInstanceId: state.BattleInstanceId,
                expectedControllerTurnSequence: state.Current.CommandState.Turn.ControllerTurnSequence), out var preparedSource);
            Assert.That(parts.Assembler.IsCurrent(preparedSource), Is.True);
            var valid = CombatCommandCalculator.AdvancePrepared(input, null, Array.Empty<IMovementTileRule>());
            var events = valid.Events.ToList();
            var final = valid.FinalState;
            var characters = valid.Characters;
            var motion = events.OfType<CombatPlatformMotionEvent>().Single();
            switch (fault)
            {
                case "missing-step": events.Remove(motion.Steps[0]); break;
                case "missing-motion": events.Remove(motion); break;
                case "duplicate-boundary": events.Add(events.OfType<CombatPlatformBoundaryEvent>().Single()); break;
                case "route":
                    var changed = new MovingPlatformFact(new MovingPlatformSpec(100, 2, PlatformAxis.Horizontal,
                        BoardingCell, new CellCoord(1, 4), motion.Final.Cell, motion.Final.TravelSign));
                    final = final.With(environment: new MovementEnvironmentFacts(final.Environment.Tiles.Select(t => t.Id == 100 ? changed : t).ToArray()));
                    break;
                case "rider-teleport":
                    var step = events.OfType<CombatCharacterEvent>().First(e => e.Kind == CombatEventKind.Moved);
                    events[events.IndexOf(step)] = new CombatCharacterEvent(step.Kind, step.Expected,
                        step.Final.WithPlacement(step.Final.Area, step.Final.ReserveSlotIndex, new CellCoord(6, 6),
                            step.Final.Rotation, new[] { new CellCoord(6, 6) }), null, null,
                        CombatDamageCause.NumericDamage, new CombatMovementCause(CombatMovementAgency.Forced));
                    break;
                case "rng":
                    final = final.With(environmentProgress: new EnvironmentProgressFacts(final.EnvironmentProgress.NextObjectEntryOrdinal,
                        final.EnvironmentProgress.NextSourceId, final.EnvironmentProgress.RandomState + 1));
                    break;
                case "boundary-count": final = final.With(lastPlatformBoundaryCount: 0); break;
            }
            var proposal = new CombatCommandTransition(valid.Input, valid.Accepted, valid.Reason, characters,
                valid.Field, final, valid.Outcome, valid.Attack, valid.ConsumedSoulIds, movement: valid.Movement,
                events: events, environmentBindings: valid.EnvironmentBindings, actionUseSettlement: valid.ActionUseSettlement,
                movementEffectUses: valid.MovementEffectUses, healthExecutions: valid.HealthExecutions,
                healthClaims: valid.HealthClaims, rewardChange: valid.RewardChange, rewardSettlement: valid.RewardSettlement);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            if (fault != "valid")
            {
                Assert.Throws<InvalidOperationException>(() => parts.Publisher.Publish(proposal));
                before.AssertUnchanged(battle.Buffers);
            }
            battle.Buffers.CombatResultReadSink.TryGetLatest(out var previous);
            Assert.That(parts.Publisher.Publish(valid), Is.True);
            Assert.That(parts.Assembler.IsCurrent(preparedSource), Is.False);
            Assert.That(state.Current.CommandState.Turn.ControllerTurnSequence, Is.EqualTo(input.State.Turn.ControllerTurnSequence));
            Assert.That(state.Current.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingEnemy));
            Assert.That(battle.Actor.Anchor, Is.EqualTo(RouteEnd));
            battle.Buffers.CombatResultReadSink.TryGetLatest(out var prelude);
            Assert.That(prelude.Sequence, Is.EqualTo(previous.Sequence + 1));
            Assert.That(prelude.Events.Any(e => e.Kind == CombatEventKind.ControllerTurnStarted), Is.False);
            var published = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => parts.Publisher.Publish(valid));
            published.AssertUnchanged(battle.Buffers);
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            battle.Buffers.CombatResultReadSink.TryGetLatest(out var begun);
            Assert.That(begun.Sequence, Is.EqualTo(prelude.Sequence + 1));
            Assert.That(begun.Events.Count(e => e.Kind == CombatEventKind.ControllerTurnStarted), Is.EqualTo(1));
            Assert.That(begun.Events.OfType<CombatPlatformMotionEvent>(), Is.Empty);
            Assert.That(state.Current.CommandState.Turn.ControllerTurnSequence, Is.EqualTo(input.State.Turn.ControllerTurnSequence + 1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PreparedPlatformAndFilledCliffArePublishedThroughNormalMovement(bool disembark)
        {
            using var battle = Create();
            var state = battle.Buffers.CombatStateReadSink;
            var platform = state.Current.CommandState.Environment.Tiles.OfType<MovingPlatformFact>().Single();
            Assert.That(platform.Cell, Is.EqualTo(BoardingCell));
            Assert.That(platform.OccupiedCells, Is.EqualTo(new[] { BoardingCell, SecondCell }));
            Assert.That(state.Current.EnvironmentBindings.TerrainSourceIds, Does.Contain(platform.Id));
            var origin = battle.Actor.Anchor.Value;
            var route = disembark ? new[] { origin, BoardingCell, FilledCell } : new[] { origin, BoardingCell };
            battle.Submit(battle.PreviewMove(battle.Actor, route));
            Assert.That(battle.Actor.Anchor, Is.EqualTo(route.Last()));
            var environment = state.Current.CommandState.Environment;
            Assert.That(platform.SameValues(environment.Tiles.Single(tile => tile.Id == platform.Id)), Is.True);
            Assert.That(environment.Tiles.OfType<StaticTerrainFact>().Single(tile => tile.Cell == FilledCell).Kind,
                Is.EqualTo(StaticTerrainKind.FilledCliff));
        }

        [TestCase("move")]
        [TestCase("reverse")]
        [TestCase("remove")]
        [TestCase("add")]
        [TestCase("fill")]
        [TestCase("promote-highground")]
        [TestCase("promote-sandstorm")]
        public void PublicationRejectsTerrainAndPlatformChangesWithoutWritingBuffers(string edit)
        {
            using var battle = Create();
            var services = battle.Publication();
            var state = battle.Buffers.CombatStateReadSink;
            int number = battle.Buffers.CombatViewWriteSink.Submit(battle.EndPlayerTurn());
            var input = services.Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.EndPlayerTurn,
                default, default,
                expectedBattleInstanceId: state.BattleInstanceId,
                expectedControllerTurnSequence: state.Current.CommandState.Turn.ControllerTurnSequence));
            var valid = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(valid.Accepted, Is.True, valid.Reason);
            var tiles = valid.FinalState.Environment.Tiles.ToList();
            int index = tiles.FindIndex(tile => tile is MovingPlatformFact);
            var platform = (MovingPlatformFact)tiles[index];
            switch (edit)
            {
                case "move":
                    tiles[index] = new MovingPlatformFact(platform.Spec, RouteEnd, platform.TravelSign);
                    break;
                case "reverse":
                    tiles[index] = new MovingPlatformFact(platform.Spec, platform.Cell, -platform.TravelSign);
                    break;
                case "remove":
                    tiles.RemoveAt(index);
                    break;
                case "add":
                    tiles.Add(new MovingPlatformFact(new MovingPlatformSpec(valid.FinalState.EnvironmentProgress.NextSourceId,
                        1, PlatformAxis.Horizontal, BoardingCell, RouteEnd, RouteEnd, 1)));
                    break;
                case "fill":
                    int cliff = tiles.FindIndex(tile => tile is StaticTerrainFact terrain && terrain.Cell == BoardingCell);
                    tiles[cliff] = Terrain(tiles[cliff].Id, BoardingCell, StaticTerrainKind.FilledCliff);
                    break;
                case "promote-highground":
                case "promote-sandstorm":
                    int terrainIndex = tiles.FindIndex(tile => tile.Id == (edit == "promote-highground" ? 4 : 5));
                    tiles[terrainIndex] = Terrain(tiles[terrainIndex].Id, tiles[terrainIndex].Cell, StaticTerrainKind.Wall);
                    break;
            }

            var environment = new MovementEnvironmentFacts(tiles);
            var final = valid.FinalState.With(environment: environment,
                environmentProgress: edit == "add" ? EnvironmentProgressFacts.Initial(environment) : null);
            var proposal = new CombatCommandTransition(valid.Input, valid.Accepted, valid.Reason, valid.Characters,
                valid.Field, final, valid.Outcome, valid.Attack, valid.ConsumedSoulIds, movement: valid.Movement,
                events: valid.Events, actionUseSettlement: valid.ActionUseSettlement, movementEffectUses: valid.MovementEffectUses,
                healthExecutions: valid.HealthExecutions, healthClaims: valid.HealthClaims,
                rewardChange: valid.RewardChange, rewardSettlement: valid.RewardSettlement);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            var error = Assert.Throws<InvalidOperationException>(() => services.Publisher.Publish(proposal));
            Assert.That(error.Message, Does.Contain(edit.StartsWith("promote-") ? "벽" : edit == "fill" ? "지형" : "정지 플랫폼"));
            before.AssertUnchanged(battle.Buffers);
            Assert.That(services.Publisher.Publish(valid), Is.True);
            Assert.That(state.Current.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingEnemy));
        }

        [TestCase("valid")]
        [TestCase("missing-fall")]
        [TestCase("missing-terrain")]
        [TestCase("forged-fill")]
        [TestCase("reused-id")]
        [TestCase("gapped-id")]
        [TestCase("old-wall")]
        [TestCase("bindings")]
        [TestCase("final-mismatch")]
        [TestCase("terrain-order")]
        public void PillarChainPublishesAtomicallyAndRejectsForgedTerrain(string fault)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new PillarBattleSource());
            var parts = battle.Publication();
            var valid = CombatCommandCalculator.Calculate(parts.Attack(battle.Actor, new CellCoord(0, 2)), Array.Empty<IMovementTileRule>());
            Assert.That(valid.Accepted, Is.True, valid.Reason);
            Assert.That(valid.Events.OfType<CombatPillarFallEvent>().Count(), Is.EqualTo(2));
            var events = valid.Events.ToList();
            var final = valid.FinalState;
            var bindings = valid.EnvironmentBindings;
            var wall = events.OfType<CombatPillarTerrainEvent>().Single(e => e.Expected == null);
            var fill = events.OfType<CombatPillarTerrainEvent>().Single(e => e.Expected != null);
            var tiles = final.Environment.Tiles.ToList();
            switch (fault)
            {
                case "missing-fall":
                    events.Remove(wall.Fall);
                    break;
                case "missing-terrain":
                    events.Remove(wall);
                    break;
                case "forged-fill":
                    var fakeCliff = Terrain(wall.Final.Id, wall.Final.Cell);
                    var fakeFill = fakeCliff.FillCliff();
                    events[events.IndexOf(wall)] = new CombatPillarTerrainEvent(wall.Fall, fakeCliff, fakeFill);
                    tiles[tiles.FindIndex(t => t.Id == wall.Final.Id)] = fakeFill;
                    break;
                case "reused-id":
                case "gapped-id":
                    long id = fault == "reused-id" ? fill.Final.Id : final.EnvironmentProgress.NextSourceId;
                    var changedId = Terrain(id, wall.Final.Cell, StaticTerrainKind.Wall);
                    events[events.IndexOf(wall)] = new CombatPillarTerrainEvent(wall.Fall, null, changedId);
                    if (fault == "reused-id") tiles.RemoveAll(t => t.Id == id);
                    tiles[tiles.FindIndex(t => t.Id == wall.Final.Id)] = changedId;
                    if (fault == "gapped-id")
                        final = final.With(environmentProgress: new EnvironmentProgressFacts(final.EnvironmentProgress.NextObjectEntryOrdinal,
                            id + 1, final.EnvironmentProgress.RandomState));
                    break;
                case "old-wall":
                    tiles[tiles.FindIndex(t => t.Id == 72)] = Terrain(72, new CellCoord(1, 3), StaticTerrainKind.Wall);
                    break;
                case "bindings":
                    bindings = null;
                    break;
                case "final-mismatch":
                    tiles.RemoveAll(t => t.Id == wall.Final.Id);
                    break;
                case "terrain-order":
                    int wallIndex = events.IndexOf(wall), fillIndex = events.IndexOf(fill);
                    (events[wallIndex], events[fillIndex]) = (events[fillIndex], events[wallIndex]);
                    break;
            }
            final = final.With(environment: new MovementEnvironmentFacts(tiles));
            var proposal = new CombatCommandTransition(valid.Input, valid.Accepted, valid.Reason, valid.Characters,
                valid.Field, final, valid.Outcome, valid.Attack, valid.ConsumedSoulIds, movement: valid.Movement,
                events: events, environmentBindings: bindings, actionUseSettlement: valid.ActionUseSettlement,
                movementEffectUses: valid.MovementEffectUses, healthExecutions: valid.HealthExecutions,
                healthClaims: valid.HealthClaims, rewardChange: valid.RewardChange, rewardSettlement: valid.RewardSettlement);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            if (fault != "valid")
            {
                Assert.Throws<InvalidOperationException>(() => parts.Publisher.Publish(proposal));
                before.AssertUnchanged(battle.Buffers);
            }
            Assert.That(parts.Publisher.Publish(valid), Is.True);
            var published = battle.Buffers.CombatStateReadSink.Current;
            Assert.That(published.CommandState.Environment.Tiles.OfType<StaticTerrainFact>()
                .Single(t => t.Id == 71).Kind, Is.EqualTo(StaticTerrainKind.FilledCliff));
            Assert.That(published.CommandState.Environment.Tiles.Single(t => t.Id == wall.Final.Id).Cell,
                Is.EqualTo(new CellCoord(0, 4)));
            Assert.That(published.EnvironmentBindings.TerrainSourceIds, Does.Contain(wall.Final.Id));
            Assert.That(published.CommandState.EnvironmentProgress.NextSourceId, Is.EqualTo(74));
            var after = new CoreIntegrationSnapshot(battle.Buffers);
            parts.Publisher.Publish(valid);
            after.AssertUnchanged(battle.Buffers);
        }

        private sealed class PillarBattleSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var source = new PullBattleSource(new PullTraitSpec(1), target: new CellCoord(7, 5)).Read(database, chapterKey, stageKey);
                var definition = new StructureSpec(EntityKey.Structure_Pillar, 1, 1, new[] { CellCoord.Zero }, 1, false, 0);
                var tiles = new MovementTileFact[]
                {
                    Terrain(71, new CellCoord(0, 5)),
                    Terrain(72, new CellCoord(0, 3), StaticTerrainKind.Wall)
                };
                return new BattleInitialization(source.Stage, source.MainAnchor, source.MainRotation,
                    source.Units, source.Structures, source.Categories, source.AttackProfiles, new MovementEnvironmentFacts(tiles),
                    new EnvironmentSemanticBindings(new long[] { 71, 72 }, Array.Empty<long>(), Array.Empty<EntityId>()),
                    clearRewards: source.ClearRewards,
                    obstaclePlacements: new[]
                    {
                        new InitialObstaclePlacementSpec(definition, ObstacleContentKind.Pillar, new CellCoord(0, 2), Rotation4.R0, 1, 0),
                        new InitialObstaclePlacementSpec(definition, ObstacleContentKind.Pillar, new CellCoord(0, 4), Rotation4.R0, 9, 0)
                    });
            }
        }

        private static ConfiguredMovementBattle Create() =>
            new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new PlatformBattleSource());

        private static StaticTerrainFact Terrain(long id, CellCoord cell, StaticTerrainKind kind = StaticTerrainKind.Cliff) =>
            new StaticTerrainFact(id, cell, kind, true, new[] { Owner.Player, Owner.Enemy });

        private sealed class PlatformBattleSource : IBattleInitializationSource
        {
            private readonly bool _moves;
            private readonly bool _previewProbe;
            internal PlatformBattleSource(bool moves = false, bool previewProbe = false)
            {
                _moves = moves;
                _previewProbe = previewProbe;
            }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var source = new ConfiguredMovementBattle.EncounterSource(new CellCoord(7, 5)).Read(database, chapterKey, stageKey);
                var platform = new MovingPlatformFact(new MovingPlatformSpec(100, 2, PlatformAxis.Horizontal,
                    BoardingCell, RouteEnd, BoardingCell, 1));
                var tiles = new MovementTileFact[] { Terrain(1, BoardingCell), Terrain(2, SecondCell),
                    Terrain(3, FilledCell, StaticTerrainKind.FilledCliff), platform,
                    Terrain(4, new CellCoord(4, 3), StaticTerrainKind.HighGround),
                    Terrain(5, new CellCoord(4, 4), StaticTerrainKind.Sandstorm) };
                if (_moves)
                    tiles = Enumerable.Range(1, 3).SelectMany(y => Enumerable.Range(1, 2).Select(x =>
                        (MovementTileFact)Terrain(y * 10 + x, new CellCoord(x, y)))).Append(platform).ToArray();
                if (_previewProbe) tiles = tiles.Append(new ForecastProbeTile()).ToArray();
                return new BattleInitialization(source.Stage, source.MainAnchor, source.MainRotation,
                    source.Units, source.Structures, source.Categories, source.AttackProfiles,
                    new MovementEnvironmentFacts(tiles), new EnvironmentSemanticBindings(
                        tiles.Where(tile => tile is not ForecastProbeTile).Select(tile => tile.Id).ToArray(),
                        Array.Empty<long>(), Array.Empty<EntityId>()),
                    clearRewards: source.ClearRewards);
            }
        }
    }
}
