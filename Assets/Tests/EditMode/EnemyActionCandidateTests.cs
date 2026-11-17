using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyActionCandidateTests
    {
        [TestCase("N5")]
        [TestCase("A8")]
        [TestCase("W8")]
        public void ShortlistsKeepLegalDistinctIntentsAndDoNotChargePreviews(string fixture)
        {
            var input = EnemyAiBenchmarkFixtures.Build(fixture, 1);
            var actor = input.Characters.Single(unit => unit.EntityId == new EntityId(11));
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            var (attacks, moves) = Finish(context, actor);
            Assert.That(attacks.Count, Is.LessThanOrEqualTo(4));
            Assert.That(moves.Count, Is.InRange(1, EnemyDecisionSettings.Default.MoveCandidateLimit));
            Assert.That(context.PreviewCount, Is.Zero);
            Assert.That(attacks.Distinct().Count(), Is.EqualTo(attacks.Count));
            Assert.That(moves.Select(move => move.Path.Endpoint), Is.Unique);
            foreach (var move in moves)
            {
                var current = CombatCommandComposition.Intent(input.WithActor(actor.EntityId), CombatCommandKind.Move, path: move.Path);
                Assert.That(MovementRouteValidator.Validate(current, move.Path, out _, current.State.Environment.VisibleTo(actor.Owner)), Is.EqualTo(MovementFailureReason.None));
            }
            if (fixture == "N5") Assert.That(attacks, Is.Not.Empty);
        }

        [Test]
        public void FarPursuitKeepsExistingOptimizedRoute()
        {
            var input = EnemyAiBenchmarkFixtures.Build("W8", 1);
            var actor = input.Characters.Single(unit => unit.EntityId == new EntityId(11));
            var current = CombatCommandComposition.Intent(input.WithActor(actor.EntityId), CombatCommandKind.Attack);
            var policy = new EnemyAiPolicy(new HostilityPolicy(), Array.Empty<IMovementTileRule>());
            Assert.That(policy.TrySelectPursuitTarget(current, out var target), Is.True);
            Assert.That(EnemyMovementPathfinder.TryFind(current, target, out var expected), Is.True);
            var (_, moves) = Finish(new EnemyDecisionContext(input, EnemyDecisionSettings.Default), actor);
            Assert.That(moves.Any(move => move.Path.Cells.SequenceEqual(expected.Cells)), Is.True);
        }

        [Test]
        public void KnownBoundaryLossReservesSafeEndpoint()
        {
            var input = EnemyAiBenchmarkFixtures.Build("R8", 1);
            var actor = input.Characters.Single(unit => unit.EntityId == new EntityId(11));
            var forecast = MakeForecast(input);
            Assert.That(forecast.Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value).KnownHpLoss, Is.Positive);
            var (_, moves) = Finish(new EnemyDecisionContext(input, EnemyDecisionSettings.Default), actor, forecast);
            Assert.That(moves.Any(move => forecast.Evaluate(actor, move.Path.Cells, move.Path.Endpoint).KnownHpLoss == 0), Is.True);
        }

        [Test]
        public void MovedActorHasNoSecondMoveCandidate()
        {
            var original = EnemyAiBenchmarkFixtures.Build("N5", 1);
            var actor = original.Characters.Single(unit => unit.EntityId == new EntityId(11)).With(actionUse: UnitActionUse.Moved);
            var input = CombatCommandTestData.With(original,
                characters: original.Characters.Select(unit => unit.EntityId == actor.EntityId ? actor : unit).ToArray());
            var (_, moves) = Finish(new EnemyDecisionContext(input, EnemyDecisionSettings.Default), actor);
            Assert.That(moves, Is.Empty);
        }

        [Test]
        public void EscapeCanMoveAwayFromTarget()
        {
            var goal = new CellCoord(0, 1);
            var input = EscapeInput(goal);
            var actor = input.Characters[0];
            var (_, moves) = Finish(new EnemyDecisionContext(input, EnemyDecisionSettings.Default), actor);
            Assert.That(moves.Any(move => move.Path.Endpoint == goal), Is.True);
            Assert.That(CombatMovementCalculator.Distance(goal, input.Characters[1].Anchor.Value),
                Is.GreaterThan(CombatMovementCalculator.Distance(actor.Anchor.Value, input.Characters[1].Anchor.Value)));
        }

        [TestCase(1, true)]
        [TestCase(256, false)]
        public void SaferLongerRouteWithinAllowanceAndRefinementCap(int limit, bool truncated)
        {
            var goal = new CellCoord(3, 1);
            var input = EscapeInput(goal);
            var actor = input.Characters[0];
            var context = new EnemyDecisionContext(input, new EnemyDecisionSettings(alternateRouteNodeLimit: limit));
            using var work = new EnemyActionCandidates(context, actor, MakeForecast(input));
            Drain(work);
            var escape = work.Moves.Single(move => move.Path.Endpoint == goal);
            Assert.That(work.RefinementTruncated, Is.EqualTo(truncated));
            Assert.That(work.RefinementExpandedNodes, Is.InRange(1, limit));
            Assert.That(work.MandatoryExpandedNodes, Is.GreaterThan(1));
            Assert.That(escape.Path.StepCount, Is.EqualTo(truncated ? 2 : 4));
            Assert.That(escape.Path.Cells.Contains(new CellCoord(2, 1)), Is.EqualTo(truncated));
        }

        [Test]
        public void CapRetainsSaferGoalAlreadyAdmittedByLastExpansion()
        {
            var goal = new CellCoord(3, 1);
            var input = EscapeInput(goal);
            var actor = input.Characters[0];
            var forecast = MakeForecast(input);
            using var full = new EnemyActionCandidates(new EnemyDecisionContext(input, EnemyDecisionSettings.Default), actor, forecast);
            Drain(full);
            int cap = full.RefinementExpandedNodes - 1;
            using var capped = new EnemyActionCandidates(new EnemyDecisionContext(input,
                new EnemyDecisionSettings(alternateRouteNodeLimit: cap)), actor, forecast);
            Drain(capped);
            Assert.That(capped.RefinementTruncated, Is.True);
            Assert.That(capped.RefinementExpandedNodes, Is.EqualTo(cap));
            Assert.That(capped.Moves.Single(move => move.Path.Endpoint == goal).Path.StepCount, Is.EqualTo(4));
        }

        [Test]
        public void TauntObligationBeamHoldsOnlyTauntRoute()
        {
            // R-4 예약 규칙: 도발 의무는 다양성이 아니라 규칙이다. 빔에는 도발 경로만 둔다(위험 피하기는 결정 세션이 어차피 뺀다).
            var goal = new CellCoord(0, 1);
            var input = TraitCombatFixtures.Equip(EscapeInput(goal), EntityKey.Unit_Akimbo, new TauntTraitSpec(8, 10));
            var actor = input.Characters[0];
            using var work = new EnemyActionCandidates(
                new EnemyDecisionContext(input, new EnemyDecisionSettings(moveCandidateLimit: 2)), actor, MakeForecast(input));
            Drain(work);
            Assert.That(work.RequiredTauntSource, Is.EqualTo(new EntityId(2)));
            Assert.That(work.Attacks, Is.Empty);
            Assert.That(work.Moves.Count, Is.EqualTo(1));
            Assert.That(work.Moves.All(move => move.TauntSourceId == work.RequiredTauntSource), Is.True);
            Assert.That(work.Moves.Any(move => move.Path.Endpoint == goal), Is.False);
        }

        [Test]
        public void LeapMayCrossButNotLandOnKnownMine()
        {
            var goal = new CellCoord(3, 1);
            var mine = new CellCoord(2, 1);
            var source = EscapeInput(goal, MineEnvironmentTests.Mine(77, 1, hidden: false, cell: mine));
            var input = TraitCombatFixtures.Equip(source, EntityKey.Unit_Demon, new LeapTraitSpec());
            var actor = input.Characters[0];
            var (_, moves) = Finish(new EnemyDecisionContext(input, EnemyDecisionSettings.Default), actor);
            Assert.That(moves.Select(move => move.Path.Endpoint).Contains(mine), Is.False);
            var escape = moves.Single(move => move.Path.Endpoint == goal);
            Assert.That(escape.Path.Cells.Contains(mine), Is.True);
        }

        [TestCase(1, false, true)]
        [TestCase(1, true, false)]
        [TestCase(2, false, false)]
        public void WeakGroundRiderCoarseAssessmentMatchesLandingAndPassThrough(int remaining, bool passesThrough, bool falls)
        {
            var ground = new CellCoord(2, 1);
            var input = RiderOnWeakGround(ground, remaining);
            var actor = input.Characters[0];
            var path = new MovementPath(passesThrough
                ? new[] { actor.Anchor.Value, ground, new CellCoord(3, 1) }
                : new[] { actor.Anchor.Value, ground });
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var candidates = new EnemyActionCandidates(context, actor, MakeForecast(input));
            Drain(candidates);
            var decision = EnemyDecision.Move(actor.EntityId, path, default);
            Assert.That(CombatActionAssessment.TryAssess(input, decision, Array.Empty<IMovementTileRule>(),
                context, out var assessment, out _), Is.True);
            // 기수의 도약은 공백을 선언하지 않아 적 평가에서 무시되고(R-1) 정밀 평가가 실제 붕괴·추락을 본다.
            Assert.That(assessment.IsPrecise, Is.True);
            Assert.That(assessment.PostActionActor.Health.IsDead, Is.EqualTo(falls));

            var actual = CombatCommandCalculator.AdvancePrepared(input, decision, Array.Empty<IMovementTileRule>());
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            Assert.That(actual.Events.OfType<CombatFallEvent>().Any(), Is.EqualTo(falls));
            Assert.That(actual.FinalState.Environment.Tiles.OfType<StaticTerrainFact>().Single().Kind,
                Is.EqualTo(remaining == 1 ? StaticTerrainKind.Cliff : StaticTerrainKind.WeakGround));

            long estimatedLoss = -1;
            foreach (var _ in candidates.EntryEstimateSteps(path, (loss, _) => estimatedLoss = loss)) { }
            Assert.That(estimatedLoss, Is.EqualTo(falls ? actor.Health.CurrentHealth : 0));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void WeakGroundRiderDecisionAvoidsLethalStopAndCanCross(bool utility, bool crossesGround)
        {
            var ground = new CellCoord(crossesGround ? 2 : 3, 1);
            var input = RiderOnWeakGround(ground, remaining: 1);
            var rules = Array.Empty<IMovementTileRule>();
            var result = utility ? CombatCommandCalculator.AdvanceUtility(input, rules)
                : CombatCommandCalculator.Advance(input, rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(result.Events.OfType<CombatFallEvent>(), Is.Empty);
            var rider = MovementTestData.Actor(result);
            Assert.That(rider.Health.IsDead, Is.False);
            Assert.That(rider.Anchor, Is.Not.EqualTo(ground));
            if (crossesGround)
            {
                Assert.That(result.PathMovement.EnteredCells, Does.Contain(ground));
                var projected = CombatCommandComposition.Project(result);
                Assert.That(EnemyMovementPathfinder.CanAttackFrom(projected, rider, rider.Anchor.Value,
                    input.Characters[1].EntityId), Is.True);
                Assert.That(result.FinalState.Environment.Tiles.OfType<StaticTerrainFact>().Single().Kind,
                    Is.EqualTo(StaticTerrainKind.Cliff));
            }
        }

        internal static CombatCommandInput RiderOnWeakGround(CellCoord ground, int remaining)
        {
            var input = EnemyMovementPathTests.Input(CategoryKey.Rider, targetCell: new CellCoord(4, 1));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new LeapTraitSpec());
            return CliffFallTests.WithEnvironment(input, new StaticTerrainFact(71, ground, StaticTerrainKind.WeakGround,
                true, new[] { Owner.Player, Owner.Enemy }, entryThreshold: 2, entryCount: 2 - remaining));
        }

        [Test]
        public void KnownSurvivingMovePrecedesPossibleDeathAndKnownDeath()
        {
            var knownSurvival = new HazardExposure(20, 20, 0, 0, CombatAssessmentGap.None, Array.Empty<HazardCellFact>());
            var possibleDeath = new HazardExposure(0, 100, 0, 0, CombatAssessmentGap.UnknownPosition, Array.Empty<HazardCellFact>());
            var knownDeath = new HazardExposure(100, 100, 0, 0, CombatAssessmentGap.None, Array.Empty<HazardCellFact>());
            var surviving = EnemyActionCandidates.HazardRank(knownSurvival, 100);
            var possible = EnemyActionCandidates.HazardRank(possibleDeath, 100);
            var terminal = EnemyActionCandidates.HazardRank(knownDeath, 100);
            Assert.That(surviving.CompareTo(possible), Is.LessThan(0));
            Assert.That(possible.CompareTo(terminal), Is.LessThan(0));
        }

        [Test]
        public void DecisionReusesThreatFieldAcrossRequests()
        {
            var context = new EnemyDecisionContext(EnemyAiBenchmarkFixtures.Build("N5", 1), EnemyDecisionSettings.Default);
            EnemyDecisionContext.ObservedGeometry geometry = null, reused = null;
            foreach (var _ in context.GeometrySteps(value => geometry = value)) { }
            EnemyThreatField first = null, second = null;
            foreach (var _ in context.ThreatSteps(value => first = value)) { }
            foreach (var _ in context.GeometrySteps(value => reused = value))
                Assert.Fail("같은 결정에서 공개 기하를 다시 조립했습니다.");
            Assert.That(geometry, Is.SameAs(reused));
            int secondSteps = 0;
            foreach (var _ in context.ThreatSteps(value => second = value)) secondSteps++;
            Assert.That(first, Is.SameAs(second));
            Assert.That(secondSteps, Is.Zero);
        }

        private static CombatCommandInput EscapeInput(CellCoord goal, MovementTileFact tile = null)
        {
            var source = EnemyMovementPathTests.Input(
                environment: new MovementEnvironmentFacts(new[] { tile ?? new EntryProbe(new CellCoord(2, 1), new[] { Owner.Enemy }) }),
                actorCell: new CellCoord(1, 1), targetCell: new CellCoord(4, 1));
            var actor = source.Characters[0].With(stats: source.Characters[0].Stats.With(moveRange: 4));
            var units = source.Characters.Select(unit => unit.EntityId == actor.EntityId ? actor : unit).ToArray();
            var definitions = new StageGimmickSpec[] { new RailwayGimmickSpec(1, 5) };
            var rails = Enumerable.Range(0, 25).Select(i => new CellCoord(i % 5, i / 5))
                .Where(cell => cell != goal && !units.Skip(1).Any(unit => unit.Anchor == cell)).ToArray();
            var observed = tile == null ? EnvironmentDamageTests.WithDamageTiles(source.State, (new CellCoord(2, 1), actor.Health.CurrentHealth)) : source.State;
            var state = StageGimmickComposition.Initialize(observed, definitions);
            var battle = new CombatBattleFacts(10, 1, 0, 0, Array.Empty<CharacterInstanceId>(), 0, rails, definitions);
            return new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, state, units, source.Profiles,
                source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts, battle: battle,
                processingKind: CombatProcessingKind.Advance);
        }

        private sealed class EntryProbe : MovementTileFact
        {
            internal EntryProbe(CellCoord cell, IReadOnlyList<Owner> knownTo) : base(77, cell, "phase-5-visible-entry", 0, true, knownTo) { }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => new EntryProbe(Cell, knownTo);
        }

        internal static StageHazardForecast MakeForecast(CombatCommandInput input)
        {
            using var builder = new StageHazardForecast.Builder(input);
            return EnemyHazardForecastTests.Finish(builder);
        }

        private static (IReadOnlyList<EnemyDecision> attacks, IReadOnlyList<EnemyDecision> moves) Finish(
            EnemyDecisionContext context, CharacterCombatFacts actor, StageHazardForecast forecast = null)
        {
            using var work = new EnemyActionCandidates(context, actor, forecast ?? MakeForecast(context.Input));
            Assert.Throws<InvalidOperationException>(() => { _ = work.Attacks; });
            Drain(work);
            Assert.That(work.RouteReconstructionCount, Is.LessThanOrEqualTo(work.Moves.Count));
            return (work.Attacks, work.Moves);
        }

        private static void Drain(EnemyActionCandidates work)
        {
            for (int i = 0; i < 250_000 && !work.IsComplete; i++) work.Step();
            Assert.That(work.IsComplete, Is.True);
        }
    }
}
