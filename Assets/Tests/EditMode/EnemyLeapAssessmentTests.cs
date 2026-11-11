using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyLeapAssessmentTests
    {
        private static readonly CellCoord Origin = new(1, 1);
        private static readonly CellCoord Crossing = new(2, 1);
        private static readonly CellCoord Endpoint = new(3, 1);
        private const CombatAssessmentGap PartialMove = CombatAssessmentGap.HiddenEnvironment | CombatAssessmentGap.UnsupportedEffect;

        [Test]
        public void IntermediateLeapMineDoesNotAddBlastExposure()
        {
            var input = TraitTrailTestData.Environment(LeapRoute(), MineEnvironmentTests.Mine(cell: Crossing));
            var rules = MineEnvironmentTests.Rules;
            var actual = RecordActual(input, rules);
            Assert.That(MovementTestData.Actor(actual).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(actual.FinalState.Environment.Tiles.Single().Active, Is.True);
            Assert.That(actual.Events.OfType<CombatObjectActivationEvent>(), Is.Empty);

            // 도약은 공백을 선언하지 않아 적 평가에서 무시된다(R-1). 경로 공백이 없으면 정밀 평가가 실제와 같은 결과를 본다.
            var facts = EnemyActionAssessmentTests.Assess(input, rules, out var projected);
            Assert.That(facts.Gaps, Is.EqualTo(CombatAssessmentGap.HiddenEnvironment));
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(projected.Characters.Single(c => c.EntityId == input.ActorId).Health.CurrentHealth, Is.EqualTo(100));
            var coverage = RouteCoverage(input, rules, out var exposed, out var unbounded);
            Assert.That(unbounded, Is.False);
            Assert.That((facts.PossibleAlliedHpLossPermille, coverage, exposed.Count),
                Is.EqualTo((0L, CombatAssessmentGap.None, 0)));
        }

        [Test]
        public void LeapSafeCrossingCanBeatHold()
        {
            var input = OnlyUsefulCrossing();
            string before = EnemyActionAssessmentTests.Encode(input);
            using var session = new EnemyDecisionSession(input, MineEnvironmentTests.Rules, EnemyDecisionSettings.Default);
            EnemyUtilityDecisionTests.Drain(session);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            Assert.That(session.Decision.Path.Cells, Is.EqualTo(new[] { CellCoord.Zero, CellCoord.Right, new CellCoord(2, 0) }));
            var actual = CombatCommandCalculator.AdvancePrepared(input, session.Decision, MineEnvironmentTests.Rules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            Assert.That(actual.Events.OfType<CombatObjectActivationEvent>(), Is.Empty);
            Assert.That(actual.FinalState.Environment.Tiles.Single().Active, Is.True);
            Assert.That(TraitCombatFixtures.Final(actual, (int)input.ActorId.Value).Anchor, Is.EqualTo(session.Decision.Path.Endpoint));
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OrdinaryTraversalAndLeapLandingRetainMineRisk(bool leap)
        {
            var input = leap ? LeapRoute() : MovementTestData.SafeRoute(Origin, Crossing, Endpoint);
            input = TraitTrailTestData.Environment(input, MineEnvironmentTests.Mine(cell: leap ? Endpoint : Crossing));
            var rules = MineEnvironmentTests.Rules;
            var actual = RecordActual(input, rules);
            Assert.That(actual.FinalState.Environment.Tiles.Single().Active, Is.False);
            Assert.That(MovementTestData.Actor(actual).Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(actual.Events.OfType<CombatObjectActivationEvent>().Single().ActivatorId, Is.EqualTo(input.ActorId));
            var facts = EnemyActionAssessmentTests.Assess(input, rules, out _);
            Assert.That(facts.Gaps, Is.EqualTo(PartialMove));
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.EqualTo(1000));

            if (!leap) return;
            var candidateInput = OnlyUsefulCrossing();
            var context = new EnemyDecisionContext(candidateInput, EnemyDecisionSettings.Default);
            using var candidates = new EnemyActionCandidates(context, candidateInput.Characters[0],
                EnemyActionCandidateTests.MakeForecast(candidateInput));
            for (int i = 0; i < 250_000 && !candidates.IsComplete; i++) candidates.Step();
            Assert.That(candidates.IsComplete, Is.True);
            Assert.That(candidates.Moves.Select(m => m.Path.Endpoint), Has.No.Member(CellCoord.Right));
            Assert.That(candidates.Moves.Select(m => m.Path.Endpoint), Has.Member(new CellCoord(2, 0)));
        }

        [Test]
        public void LeapDoesNotExemptPassengerArrival()
        {
            var route = TraitCombatFixtures.Equip(PositionSwapMovementTests.Route(), EntityKey.Unit_Akimbo,
                PositionSwapMovementTests.Spec(), new LeapTraitSpec());
            route = WithAlliedStructure(WithAlliedStructure(route, CellCoord.Zero), new CellCoord(3, 2), id: 91);
            var input = TraitTrailTestData.Environment(route,
                MineEnvironmentTests.Mine(cell: Origin), MineEnvironmentTests.Mine(id: 12, ordinal: 2, cell: Crossing));
            var rules = MineEnvironmentTests.Rules;
            var actual = RecordActual(input, rules);
            Assert.That(actual.Events.OfType<CombatObjectActivationEvent>().Single().ActivatorId, Is.EqualTo(new EntityId(20)));
            Assert.That(TraitCombatFixtures.Final(actual, 20).Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(MovementTestData.Actor(actual).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(actual.FinalState.Structures.Select(s => s.Health.CurrentHealth), Is.EqualTo(new[] { 90, 100 }));
            Assert.That(actual.FinalState.Environment.Tiles.Single(t => t.Id == 12).Active, Is.True);

            var facts = EnemyActionAssessmentTests.Assess(input, rules, out _);
            Assert.That(facts.Gaps, Is.EqualTo(PartialMove));
            // 경로가 드러낸 개체(행위자·승객·구조물 90)만 센다. 도약의 무조건 상한은 R-1에서 없어졌다.
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.EqualTo(3000));
            RouteCoverage(input, rules, out var exposed, out _);
            Assert.That(exposed, Is.EquivalentTo(new[] { input.ActorId, new EntityId(20), new EntityId(90) }));
        }

        [Test]
        public void RandomLandingRetainsPossibleMineContacts()
        {
            var route = MovementTestData.Route(LeapRoute(), Origin, Crossing, Endpoint, new CellCoord(4, 1));
            var input = TraitTrailTestData.Environment(WithAlliedStructure(route, new CellCoord(2, 2)),
                MineEnvironmentTests.Mine(cell: Crossing), TornadoEnvironmentTests.Source(cell: Endpoint));
            var rules = TornadoEnvironmentTests.Rules;
            var actual = RecordActual(input, rules);
            Assert.That(actual.PathMovement.StopReason, Is.EqualTo(MovementStopReason.ForcedRelocation));
            Assert.That(actual.Events.OfType<CombatTornadoEntryEvent>().Count(), Is.EqualTo(1));
            var facts = EnemyActionAssessmentTests.Assess(input, rules, out _);
            Assert.That(facts.Gaps, Is.EqualTo(PartialMove | CombatAssessmentGap.RandomOutcome | CombatAssessmentGap.UnknownPosition));
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.EqualTo(2000));
        }

        [Test]
        public void UnresolvedForcedArrivalRetainsPossibleMineContacts()
        {
            var input = TraitTrailTestData.Environment(WithAlliedStructure(LeapRoute(), new CellCoord(2, 2)),
                MineEnvironmentTests.Mine(cell: Crossing), new ForcedArrivalFact());
            var rules = MineEnvironmentTests.Rules.Append(new ForcedArrivalRule()).ToArray();
            var actual = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            RecordDigest(new object[] { actual.PathMovement, actual.Events, actual.Outcome, actual.FinalState.Structures,
                actual.FinalState.Environment.Tiles.OfType<MineSourceFact>().ToArray() });
            Assert.That(actual.PathMovement.StopReason, Is.EqualTo(MovementStopReason.ForcedRelocation));
            Assert.That(actual.Events.OfType<CombatObjectActivationEvent>().Single().ActivatorId, Is.EqualTo(input.ActorId));
            Assert.That(actual.FinalState.Structures.Single().Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(RouteCoverage(input, rules, out var exposed, out var unbounded), Is.EqualTo(CombatAssessmentGap.UnsupportedEffect));
            Assert.That(unbounded, Is.True);
            Assert.That(exposed, Does.Contain(new EntityId(90)));
            Assert.That(EnemyActionAssessmentTests.Assess(input, rules, out _).PossibleAlliedHpLossPermille, Is.EqualTo(3000));
        }

        [Test]
        public void NonMineIntermediateEffectsRemainVisible()
        {
            var route = LeapRoute();
            var input = TraitTrailTestData.Environment(CombatCommandTestData.With(route,
                    state: route.State.With(thornFields: new[] { new ThornFieldCellSnapshot(Crossing, 2, 7) })),
                MineEnvironmentTests.Mine(cell: Crossing),
                new StaticTerrainFact(71, Crossing, StaticTerrainKind.WeakGround, true, new[] { Owner.Player }, 2));
            var actual = RecordActual(input, TornadoEnvironmentTests.Rules);
            Assert.That(MovementTestData.Actor(actual).Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(actual.PathMovement.EnteredCells, Is.EqualTo(new[] { Crossing, Endpoint }));
            Assert.That(actual.FinalState.Environment.Tiles.OfType<StaticTerrainFact>().Single().EntryCount, Is.EqualTo(1));
            Assert.That(actual.FinalState.Environment.Tiles.OfType<MineSourceFact>().Single().Active, Is.True);
            // 도약 무시(R-1)로 정밀 평가가 돌고, 중간 칸 가시 피해(7/100)가 그 결과에 그대로 보인다.
            var facts = EnemyActionAssessmentTests.Assess(input, TornadoEnvironmentTests.Rules, out _);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.AlliedHpLossPermille, Is.EqualTo(70));
        }

        private static CombatCommandInput LeapRoute() =>
            TraitCombatFixtures.Equip(MovementTestData.SafeRoute(Origin, Crossing, Endpoint),
                EntityKey.Unit_Akimbo, new LeapTraitSpec());

        private static CombatCommandInput OnlyUsefulCrossing()
        {
            var input = EnemyUtilityDecisionTests.Approach();
            var state = new CombatCommandState(5, 1, input.State.Phase,
                input.State.Structures, input.State.ThornFields, input.State.Turn);
            input = CombatCommandTestData.With(input, state: state, characters: new[] { input.Characters[0], input.Characters[1],
                CombatCommandTestData.With(input.Characters[2], position: new CellCoord(4, 0)) });
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new LeapTraitSpec());
            return TraitTrailTestData.Environment(input,
                new MineSourceFact(11, CellCoord.Right, 1, 10, true, new[] { Owner.Enemy }));
        }

        private static CombatCommandInput WithAlliedStructure(CombatCommandInput input, CellCoord cell, int id = 90)
        {
            var structure = new StructureCombatFacts(new EntityId(id), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 1, new[] { CellCoord.Zero }, 100, false, 0),
                new HealthStateSnapshot(100, 100), cell, Rotation4.R0, new[] { cell }, Owner.Player);
            return CombatCommandTestData.With(input, state: input.State.With(
                structures: input.State.Structures.Append(structure).ToArray()));
        }

        private static CombatAssessmentGap RouteCoverage(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            out HashSet<EntityId> exposed, out bool unbounded)
        {
            exposed = new HashSet<EntityId>();
            bool foundUnbounded = false;
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var gaps = MovementEnvironmentCalculator.RouteAssessmentCoverageSteps(input, actor, rules, exposed,
                () => foundUnbounded = true).Aggregate(CombatAssessmentGap.None, (all, gap) => all | gap);
            unbounded = foundUnbounded;
            return gaps;
        }

        private static CombatCommandTransition RecordActual(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            string before = EnemyActionAssessmentTests.Encode(input);
            var result = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
            RecordDigest(new object[] {
                CombatCommandComposition.Project(result), result.Events, result.Outcome, result.PathMovement });
            return result;
        }

        private static void RecordDigest(object[] facts)
        {
            using var sha = SHA256.Create();
            string snapshot = EnemyActionAssessmentTests.Encode(facts);
            TestContext.WriteLine("actual=" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(snapshot))));
        }

        private sealed class ForcedArrivalFact : MovementTileFact
        {
            internal ForcedArrivalFact() : base(81, Endpoint, "leap-forced-arrival", 0, true, new[] { Owner.Player }) { }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => this;
        }

        private sealed class ForcedArrivalRule : IMovementTileRule
        {
            public string Key => "leap-forced-arrival";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput input) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) =>
                input.Phase == MovementEffectPhase.Hazard
                    ? new MovementStepEffects(forcedMovement: new[] { new MovementForcedMoveRequest(input.MoverId, Rotation4.R270, 1) })
                    : new MovementStepEffects();
        }
    }
}
