using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyHazardForecastTests
    {
        [Test]
        public void MainDeathElsewhereStopsLaterActorHazards()
        {
            var input = Input(new StageGimmickSpec[] { new RockfallGimmickSpec(), new RailwayGimmickSpec(5, 2) }, warning: true)
                .WithActor(new EntityId(2));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 10
                ? CombatCommandTestData.With(c, position: new CellCoord(1, 3))
                : c.EntityId.Value == 1 ? CombatCommandTestData.With(c, position: new CellCoord(0, 0)) : c).ToArray());
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            input = CombatCommandTestData.With(input, state: input.State.With(gimmicks: input.State.Gimmicks.Replace(
                new RockfallGimmickState(new StageGimmickId(1), RockfallGimmickPhase.Warning, 5, actor.OccupiedCells))));
            var boundary = CombatCommandTestData.With(input, state: input.State.With(turn: Turn(5)));
            var live = StageGimmickComposition.Default.TryAdvance(boundary, new StageGimmickBoundary(Owner.Enemy, 5));
            Assert.That(live.Outcome.Outcome, Is.EqualTo(StageOutcome.Defeat));
            Assert.That(TraitCombatFixtures.Final(live, 2).Health.CurrentHealth, Is.EqualTo(actor.Health.CurrentHealth));
            using var builder = new StageHazardForecast.Builder(input);
            var exposure = Finish(builder).Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            Assert.That(exposure.KnownHpLoss, Is.Zero);
            Assert.That(exposure.BindEndCount, Is.Zero);
        }

        [Test]
        public void SameRailwayBatchKeepsActorDeathBeforeTerminalStop()
        {
            var input = Input(new StageGimmickSpec[] { new RailwayGimmickSpec(5, 2) },
                rails: new[] { new CellCoord(1, 3), new CellCoord(4, 3) }).WithActor(new EntityId(2));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 10
                ? CombatCommandTestData.With(c, position: new CellCoord(1, 3))
                : c.EntityId.Value == 1 ? CombatCommandTestData.With(c, position: new CellCoord(0, 0)) : c).ToArray());
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            using var builder = new StageHazardForecast.Builder(input);
            var risk = Finish(builder).Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            Assert.That(risk.KnownHpLoss, Is.EqualTo(actor.Health.CurrentHealth));
            Assert.That(risk.Evidence.Single().Key, Is.EqualTo(StageGimmickKey.Railway));
        }

        [Test]
        public void PredictedBindCombinesWithDisarmToMarkUncoveredHorizon()
        {
            var input = Input(new StageGimmickSpec[] { new RockfallGimmickSpec(bindAffectedSideTurns: 2) }, warning: true)
                .WithActor(new EntityId(2));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId).WithStatusEffects(
                new StatusEffectSet(new AppliedStatus[] { new ActionRestrictionStatus(StatusEffectKey.Disarm,
                    new EffectOriginKey(EffectOwnerKind.Item, 2, 999, "status"), 3) }));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(),
                state: input.State.With(gimmicks: input.State.Gimmicks.Replace(new RockfallGimmickState(new StageGimmickId(1),
                    RockfallGimmickPhase.Warning, 5, actor.OccupiedCells))));
            Assert.That(CombatActionAdmission.IsAttackRestricted(actor), Is.True);
            Assert.That(CombatActionAdmission.IsMovementRestricted(actor), Is.False);
            using var builder = new StageHazardForecast.Builder(input);
            var exposure = Finish(builder).Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            Assert.That(exposure.BindEndCount, Is.EqualTo(3));
            Assert.That((exposure.Gaps & (CombatAssessmentGap.UnsupportedEffect | CombatAssessmentGap.FutureWarning)),
                Is.EqualTo(CombatAssessmentGap.UnsupportedEffect | CombatAssessmentGap.FutureWarning));
        }

        [Test]
        public void ForecastUsesRegistryOrderAndKeepsUnpublishedHazardsUncertain()
        {
            var definitions = new StageGimmickSpec[] {
                new RockfallGimmickSpec(), new HaboobGimmickSpec(initialAnchor: new CellCoord(3, 3)),
                new RailwayGimmickSpec(5, 2), new TornadoGimmickSpec(new[] {
                    new TornadoMemberSpec(71, new[] { Owner.Enemy, Owner.Player }, new CellCoord(2, 2), 1) }) };
            var input = Input(definitions, warning: true);
            using var builder = new StageHazardForecast.Builder(input);
            var forecast = Finish(builder);
            Assert.That(forecast.Facts.Where(f => f.BoundaryCount == 5).Select(f => f.Key).Distinct(),
                Is.EqualTo(new[] { StageGimmickKey.Tornado, StageGimmickKey.Railway, StageGimmickKey.Haboob, StageGimmickKey.Rockfall }));
            Assert.That(forecast.Facts.Any(f => f.Key == StageGimmickKey.Tornado && f.BoundaryCount == 5 &&
                !f.Cell.HasValue && f.Gaps == CombatAssessmentGap.FutureWarning), Is.True);
            Assert.That(forecast.Facts.Any(f => f.Key == StageGimmickKey.Haboob && f.BoundaryCount == 5 &&
                f.Cell.HasValue && f.Gaps == CombatAssessmentGap.None), Is.True);
            Assert.That(forecast.Facts.Any(f => f.Key == StageGimmickKey.Haboob && f.BoundaryCount == 6 &&
                !f.Cell.HasValue && f.Gaps == CombatAssessmentGap.FutureWarning), Is.True);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var risk = forecast.Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            Assert.That(risk.PossibleHpLoss, Is.EqualTo(actor.Health.CurrentHealth));
            Assert.That(risk.KnownHpLoss, Is.EqualTo(actor.Health.CurrentHealth));
            Assert.That((risk.Gaps & CombatAssessmentGap.UnknownPosition) != 0, Is.False);
        }

        [Test]
        public void IndividualActionsDoNotAdvanceBoundary()
        {
            var input = EnemyAiBenchmarkFixtures.Build("R8", 1);
            using var before = new StageHazardForecast.Builder(input);
            var expected = Finish(before);
            var transition = CombatCommandCalculator.Advance(input, EnemyAiBenchmarkFixtures.Rules);
            Assert.That(transition.Accepted, Is.True);
            Assert.That(transition.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(input.State.Turn.CompletedControllerTurnCount));
            using var after = new StageHazardForecast.Builder(CombatCommandComposition.Project(transition));
            var actual = Finish(after);
            Assert.That(actual.LastBoundaryCount, Is.EqualTo(expected.LastBoundaryCount));
            Assert.That(EncodeFacts(actual), Is.EqualTo(EncodeFacts(expected)));
        }

        [Test]
        public void RailForecastMatchesLiveFixedOccupancyAndStopsLaterKnownEffects()
        {
            var spec = new RailwayGimmickSpec(5, 1);
            var input = Input(new StageGimmickSpec[] { new RockfallGimmickSpec(), spec }, warning: true);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            using var builder = new StageHazardForecast.Builder(input);
            var risk = Finish(builder).Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            var boundary = CombatCommandTestData.With(input, state: input.State.With(turn: Turn(5)));
            var actual = new RailwayGimmickRule().TryAdvance(boundary, spec, new StageGimmickId(2),
                new StageGimmickBoundary(Owner.Enemy, 5));
            Assert.That(TraitCombatFixtures.Final(actual, (int)actor.EntityId.Value).Health.CurrentHealth,
                Is.EqualTo(actor.Health.CurrentHealth - risk.KnownHpLoss));
            Assert.That(risk.Evidence.Select(e => e.Key), Is.EqualTo(new[] { StageGimmickKey.Railway }));
        }

        [Test]
        public void CancelledBuilderDoesNotCompleteOrMutateSource()
        {
            var input = EnemyAiBenchmarkFixtures.Build("R8", 1);
            string original = EnemyActionAssessmentTests.Encode(input);
            using var builder = new StageHazardForecast.Builder(input);
            for (int i = 0; i < 4; i++) builder.Step();
            Assert.That(builder.IsComplete, Is.False);
            builder.Dispose();
            Assert.Throws<ObjectDisposedException>(() => builder.Build());
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(original));
        }

        [TestCase(4, false)]
        [TestCase(5, true)]
        [TestCase(6, false)]
        [TestCase(7, true)]
        public void RailwayScheduleMatchesExecution(long boundary, bool due)
        {
            Assert.That(RailwayGimmickRule.IsDue(new RailwayGimmickSpec(5, 2), boundary), Is.EqualTo(due));
        }

        [Test]
        public void SharedRockfallHealthAndBindArithmetic()
        {
            var spec = new RockfallGimmickSpec(maximumHealthLossPercent: 25, bindAffectedSideTurns: 2);
            Assert.That(RockfallGimmickRule.HealthLoss(125, spec), Is.EqualTo(31));
            Assert.That(RockfallGimmickRule.BindEndCount(Owner.Enemy, Owner.Enemy, spec), Is.EqualTo(3));
            Assert.That(RockfallGimmickRule.BindEndCount(Owner.Player, Owner.Enemy, spec), Is.EqualTo(2));
        }

        [TestCase(5)]
        [TestCase(6)]
        public void RailEndpointDueBeforeNextEnemyActionIsLethal(int first)
        {
            var input = Input(new[] { new RailwayGimmickSpec(first, 2) });
            using var builder = new StageHazardForecast.Builder(input);
            var forecast = Finish(builder);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var exposure = forecast.Evaluate(actor, Array.Empty<CellCoord>(), new CellCoord(1, 3));
            Assert.That(exposure.KnownHpLoss, Is.EqualTo(actor.Health.CurrentHealth));
            Assert.That(input.State.Turn.CompletedControllerTurnCount, Is.EqualTo(4));
        }

        [Test]
        public void RailCrossingBeforeBoundaryIsNotInstantDeath()
        {
            var input = Input(new[] { new RailwayGimmickSpec(5, 2) });
            using var builder = new StageHazardForecast.Builder(input);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var exposure = Finish(builder).Evaluate(actor,
                new[] { new CellCoord(0, 3), new CellCoord(1, 3), new CellCoord(2, 3) }, new CellCoord(2, 3));
            Assert.That(exposure.KnownHpLoss, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(25)]
        public void KnownRockfallMatchesLiveFixedOccupancy(int percent)
        {
            var spec = new RockfallGimmickSpec(maximumHealthLossPercent: percent, bindAffectedSideTurns: 2);
            var input = Input(new[] { spec }, warning: true).WithActor(new EntityId(2));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            input = CombatCommandTestData.With(input, state: input.State.With(gimmicks: input.State.Gimmicks.Replace(
                new RockfallGimmickState(new StageGimmickId(1), RockfallGimmickPhase.Warning, 5, actor.OccupiedCells))));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == actor.EntityId
                ? c.WithHealth(new HealthStateSnapshot(125, 125)) : c).ToArray());
            actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            using var builder = new StageHazardForecast.Builder(input);
            var exposure = Finish(builder).Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            Assert.That(exposure.KnownHpLoss, Is.EqualTo(percent == 0 ? 0 : 31));
            Assert.That(exposure.BindEndCount, Is.EqualTo(3));
            var atBoundary = CombatCommandTestData.With(input, state: input.State.With(turn: Turn(5)));
            var actual = new RockfallGimmickRule().TryAdvance(atBoundary, spec, new StageGimmickId(1),
                new StageGimmickBoundary(Owner.Enemy, 5));
            var after = MovementTestData.After(actual).Characters.Single(c => c.EntityId == actor.EntityId);
            Assert.That(actor.Health.CurrentHealth - after.Health.CurrentHealth, Is.EqualTo(exposure.KnownHpLoss));
        }

        [Test]
        public void UnknownFutureWarningDoesNotRevealRandomCells()
        {
            var input = Input(new[] { new RockfallGimmickSpec(cooldownControllerTurns: 1, warningControllerTurns: 1) });
            string before = EnemyActionAssessmentTests.Encode(input);
            using var builder = new StageHazardForecast.Builder(input);
            var forecast = Finish(builder);
            Assert.That(forecast.Facts.Any(f => f.Cell == null && (f.Gaps & CombatAssessmentGap.FutureWarning) != 0), Is.True);
            Assert.That(forecast.Facts.All(f => f.Cell == null), Is.True);
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
            var varied = CombatCommandTestData.With(input, state: input.State.With(environmentProgress:
                new EnvironmentProgressFacts(input.State.EnvironmentProgress.NextObjectEntryOrdinal,
                    input.State.EnvironmentProgress.NextSourceId, 9191)));
            using var other = new StageHazardForecast.Builder(varied);
            Assert.That(EncodeFacts(Finish(other)), Is.EqualTo(EncodeFacts(forecast)));
        }

        [Test]
        public void HazardLossClampedAfterDeathAndTerminalInputStopsForecast()
        {
            var input = Input(new StageGimmickSpec[] { new RockfallGimmickSpec(maximumHealthLossPercent: 100), new RailwayGimmickSpec(5, 1) }, warning: true);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            using var builder = new StageHazardForecast.Builder(input);
            var exposure = Finish(builder).Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            Assert.That(exposure.KnownHpLoss, Is.EqualTo(actor.Health.CurrentHealth));
            Assert.That(exposure.BindEndCount, Is.Zero);
            var terminal = CombatCommandTestData.With(input, state: input.State.With(phase: CombatFlowPhase.Finished));
            using var stopped = new StageHazardForecast.Builder(terminal);
            Assert.That(Finish(stopped).Facts, Is.Empty);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void ResumePartitionsProduceSameFacts(int partition)
        {
            var input = Input(new StageGimmickSpec[] { new RockfallGimmickSpec(), new RailwayGimmickSpec(5, 1) }, warning: true);
            using var whole = new StageHazardForecast.Builder(input);
            using var sliced = new StageHazardForecast.Builder(input);
            Assert.That(sliced.WorkVersion, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => sliced.Build());
            while (!sliced.IsComplete) for (int i = 0; i < partition && !sliced.IsComplete; i++) sliced.Step();
            Assert.That(EncodeFacts(sliced.Build()), Is.EqualTo(EncodeFacts(Finish(whole))));
            Assert.That(sliced.WorkVersion, Is.GreaterThan(sliced.Build().Facts.Count));
            sliced.Dispose();
            Assert.Throws<ObjectDisposedException>(() => sliced.Step());
        }

        internal static string EncodeFacts(StageHazardForecast forecast) => EnemyActionAssessmentTests.Encode(forecast.Facts
            .Select(f => new object[] { f.Id, f.Key, f.BoundaryCount, f.Cell, f.InstantDefeat, f.Rockfall, f.Gaps,
                f.TornadoSourceId, f.HaboobRegion, f.HaboobPhase, f.HaboobNextBoundaryCount }).ToArray());

        internal static StageHazardForecast Finish(StageHazardForecast.Builder builder)
        {
            int steps = 0;
            while (!builder.IsComplete) { builder.Step(); Assert.That(++steps, Is.LessThan(100000)); }
            return builder.Build();
        }

        internal static CombatTurnFacts Turn(long count) => new(StagePhase.Combat,
            controllerPhase: CombatControllerPhase.Enemy, controllerTurnSequence: 1,
            completedPlayerTurnCount: 2, completedControllerTurnCount: count);

        internal static CombatCommandInput Input(StageGimmickSpec[] definitions, bool warning = false,
            CellCoord[] rails = null)
        {
            var source = CombatCommandTestData.Input();
            var state = source.State.With(phase: CombatFlowPhase.Automatic, turn: Turn(4));
            state = StageGimmickComposition.Initialize(state, definitions);
            for (int i = 0; i < definitions.Length; i++)
                if (definitions[i] is RockfallGimmickSpec && warning)
                    state = state.With(gimmicks: state.Gimmicks.Replace(new RockfallGimmickState(new StageGimmickId(i + 1),
                        RockfallGimmickPhase.Warning, 5, new[] { new CellCoord(1, 3) })));
            var battle = new CombatBattleFacts(8, 42, 2, 2, Array.Empty<CharacterInstanceId>(), 0,
                rails ?? new[] { new CellCoord(1, 3) }, definitions);
            return new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, source.ActorId, source.Aim, state, source.Characters,
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts, battle: battle,
                processingKind: CombatProcessingKind.Advance, battleInstanceId: 1, expectedBattleInstanceId: 1, expectedControllerTurnSequence: 1);
        }
    }
}
