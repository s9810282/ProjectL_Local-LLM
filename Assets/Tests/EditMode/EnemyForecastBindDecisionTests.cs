using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyForecastBindDecisionTests
    {
        private static readonly IMovementTileRule[] Rules = EnemyAiBenchmarkFixtures.Rules;
        private delegate EnemyScoreFacts Position(CharacterCombatFacts actor, EnemyScoreFacts facts, MovementPath path,
            StageHazardForecast hazards, EnemyThreatField threat, long progress, long followup, long entryLoss, CombatAssessmentGap entryGaps,
            long nextTurnStrike);
        private static readonly Position PositionFacts = (Position)typeof(EnemyDecisionSession)
            .GetMethod("PositionFacts", BindingFlags.NonPublic | BindingFlags.Static).CreateDelegate(typeof(Position));

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void ZeroDamageRockfallEscapeStrictlyBeatsHold(int partition)
        {
            var input = Input();
            var actor = Actor(input);
            string before = EnemyActionAssessmentTests.Encode(input);
            var forecast = EnemyActionCandidateTests.MakeForecast(input);
            var exposure = forecast.Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            Assert.That(exposure.KnownHpLoss, Is.Zero);
            Assert.That(exposure.PossibleHpLoss, Is.Zero);
            Assert.That(exposure.BindEndCount, Is.EqualTo(2));
            Assert.That(exposure.AdditionalBindAtNextOpportunity, Is.True);

            var safePath = new MovementPath(new[] { actor.Anchor.Value, actor.Anchor.Value + CellCoord.Left });
            var hold = ScoreAt(input, new EnemyScoreFacts(CombatAssessmentGap.HiddenEnvironment));
            var move = ScoreAt(input, new EnemyScoreFacts(CombatAssessmentGap.HiddenEnvironment), safePath);
            Assert.That((EnemyUtilityScorer.Score(hold), EnemyUtilityScorer.Score(move)), Is.EqualTo((-500L, 0L)));
            var fixedMove = CombatCommandCalculator.AdvancePrepared(input, EnemyDecision.Move(actor.EntityId, safePath, default), Rules);
            Assert.That(fixedMove.Accepted, Is.True, fixedMove.Reason);
            RecordActual("safe-move", fixedMove);
            var fixedHold = CombatCommandCalculator.AdvancePrepared(input, EnemyDecision.EndEnemyPhase(), Rules);
            Assert.That(fixedHold.Accepted, Is.True, fixedHold.Reason);
            RecordActual("end-side", fixedHold);

            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            EnemyUtilityDecisionTests.Drain(session, partition);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            var result = CombatCommandCalculator.AdvancePrepared(input, session.Decision, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(forecast.Evaluate(actor, session.Decision.Path.Cells, session.Decision.Path.Endpoint).PossibleBindEndCount, Is.Zero);
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [TestCase(2, 5)]
        [TestCase(3, 6)]
        public void ExistingBindThatSurvivesIsNotChargedAgain(int remainingEnds, int impactBoundary)
        {
            var input = Input(existingBindEnds: remainingEnds, impactBoundary: impactBoundary);
            string before = EnemyActionAssessmentTests.Encode(input);
            var actor = Actor(input);
            var exposure = EnemyActionCandidateTests.MakeForecast(input).Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            Assert.That(exposure.PossibleBindEndCount, Is.Positive);
            Assert.That(exposure.AdditionalBindAtNextOpportunity, Is.False);
            Assert.That(EnemyUtilityScorer.Score(ScoreAt(input, new EnemyScoreFacts(CombatAssessmentGap.HiddenEnvironment))), Is.Zero);

            var baseline = AtNextOpportunity(Input(existingBindEnds: remainingEnds, impactBoundary: 8));
            var actual = AtNextOpportunity(input);
            Assert.That(AppliedStatusRules.IsRestricted(Actor(baseline).StatusEffects, StatusEffectKey.Bind), Is.True);
            Assert.That(AppliedStatusRules.IsRestricted(Actor(actual).StatusEffects, StatusEffectKey.Bind), Is.True);
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [TestCase(5)]
        [TestCase(6)]
        public void ExpiringBindAndNewImpactChargeOneRestriction(int impactBoundary)
        {
            var input = Input(existingBindEnds: 1, impactBoundary: impactBoundary);
            string before = EnemyActionAssessmentTests.Encode(input);
            var actor = Actor(input);
            var exposure = EnemyActionCandidateTests.MakeForecast(input).Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            Assert.That(exposure.AdditionalBindAtNextOpportunity, Is.True);
            Assert.That(EnemyUtilityScorer.Score(ScoreAt(input, new EnemyScoreFacts(CombatAssessmentGap.HiddenEnvironment))), Is.EqualTo(-500));

            var baseline = AtNextOpportunity(Input(existingBindEnds: 1, impactBoundary: 8));
            var actual = AtNextOpportunity(input);
            Assert.That(AppliedStatusRules.IsRestricted(Actor(baseline).StatusEffects, StatusEffectKey.Bind), Is.False);
            var bind = Actor(actual).StatusEffects.Entries.OfType<ActionRestrictionStatus>().Single(status => status.Key == StatusEffectKey.Bind);
            Assert.That(bind.RemainingAffectedSideTurnEnds, Is.EqualTo(1));
            Assert.That(bind.Origin.Value.Kind, Is.EqualTo(EffectOwnerKind.StageGimmick));
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [TestCase(false, 1)]
        [TestCase(false, 3)]
        [TestCase(true, 1)]
        [TestCase(true, 3)]
        public void RepeatedKnownOrPossibleBindChargesOnce(bool uncertainPosition, int bindTurns)
        {
            var input = Input(bindTurns: bindTurns);
            var actor = Actor(input);
            // 경고 칸은 좌표순으로 정렬된다. 왼쪽 대표와 현재 칸이 같은 충돌의 근거로 함께 잡히게 한다.
            var warning = new RockfallGimmickState(new StageGimmickId(1), RockfallGimmickPhase.Warning, 5,
                new[] { actor.Anchor.Value + CellCoord.Left, actor.Anchor.Value, actor.Anchor.Value + CellCoord.Up });
            input = CombatCommandTestData.With(input, state: input.State.With(gimmicks: input.State.Gimmicks.Replace(warning)));
            var gaps = uncertainPosition ? StageHazardForecast.TornadoThrowGaps : CombatAssessmentGap.None;
            var exposure = EnemyActionCandidateTests.MakeForecast(input).Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value, gaps);
            Assert.That(exposure.Gaps, Is.EqualTo(gaps));
            Assert.That(exposure.PossibleBindEndCount, Is.EqualTo(bindTurns + 1));
            Assert.That(exposure.BindEndCount, Is.EqualTo(uncertainPosition ? 0 : bindTurns + 1));
            Assert.That(exposure.AdditionalBindAtNextOpportunity, Is.True);
            var facts = ScoreAt(input, new EnemyScoreFacts(CombatAssessmentGap.HiddenEnvironment), entryGaps: gaps);
            var withoutBind = new EnemyScoreFacts(facts.Gaps, actorHazardLoss: facts.ActorHazardLossPermille,
                playerThreat: facts.PlayerThreatPermille);
            Assert.That(EnemyUtilityScorer.Score(facts) - EnemyUtilityScorer.Score(withoutBind), Is.EqualTo(-500));
            Assert.That(exposure.Evidence.Count, Is.EqualTo(uncertainPosition ? 2 : 1));
        }

        [Test]
        public void PostActionBindIsTheBaseline()
        {
            var input = Input();
            var actor = Actor(input);
            var forecast = EnemyActionCandidateTests.MakeForecast(input);
            Assert.That(forecast.Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value).AdditionalBindAtNextOpportunity, Is.True);
            var facts = new EnemyScoreFacts(CombatAssessmentGap.HiddenEnvironment, precise: true,
                restrictionDelta: -500, postActionActor: WithBind(actor, 2));
            var positioned = ScoreAt(input, facts);
            Assert.That(positioned.AdditionalForecastBind, Is.False);
            Assert.That(positioned.RestrictionDeltaPermille, Is.EqualTo(-500));
            Assert.That(EnemyUtilityScorer.Score(positioned), Is.EqualTo(-500));
            Assert.That(EnemyUtilityScorer.AttackBenefit(positioned), Is.EqualTo(EnemyUtilityScorer.AttackBenefit(facts)));
            Assert.That(AppliedStatusRules.IsRestricted(actor.StatusEffects, StatusEffectKey.Bind), Is.False);
        }

        [Test]
        public void KnownDeathRemovesOnlyFutureBind()
        {
            var input = Input(damagePercent: 100);
            var actor = Actor(input);
            var forecast = EnemyActionCandidateTests.MakeForecast(input);
            var exposure = forecast.Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            Assert.That((exposure.KnownHpLoss, exposure.PossibleBindEndCount), Is.EqualTo((100, 0)));
            Assert.That(exposure.AdditionalBindAtNextOpportunity, Is.False);
            Assert.That(EnemyUtilityScorer.Score(ScoreAt(input, new EnemyScoreFacts(CombatAssessmentGap.HiddenEnvironment))), Is.EqualTo(-4000));
            var dead = actor.WithHealth(new HealthStateSnapshot(0, 100));
            Assert.That(forecast.Evaluate(dead, Array.Empty<CellCoord>(), actor.Anchor.Value).AdditionalBindAtNextOpportunity, Is.False);
        }

        [Test]
        public void TerminalAfterImpactRemovesFutureBind()
        {
            var input = Input(definitions: new StageGimmickSpec[]
            {
                new RockfallGimmickSpec(maximumHealthLossPercent: 0), new RailwayGimmickSpec(6, 2)
            });
            var main = input.Characters.Single(unit => unit.CharacterKey == EntityKey.Unit_MainCharacter);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(unit => unit.EntityId == main.EntityId
                ? CombatCommandTestData.With(unit, position: new CellCoord(1, 3))
                : unit.EntityId.Value == 1 ? CombatCommandTestData.With(unit, position: CellCoord.Zero) : unit).ToArray());
            var actor = Actor(input);
            var exposure = EnemyActionCandidateTests.MakeForecast(input).Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            Assert.That(exposure.KnownHpLoss, Is.Zero);
            Assert.That(exposure.BindEndCount, Is.EqualTo(2));
            Assert.That(exposure.AdditionalBindAtNextOpportunity, Is.False);
            Assert.That(AtNextOpportunity(input).State.Phase, Is.EqualTo(CombatFlowPhase.Finished));
        }

        [TestCase(CombatAssessmentGap.None, false)]
        [TestCase(CombatAssessmentGap.HiddenEnvironment, true)]
        public void OnlyGuaranteedTerminalSuppressesForecastBind(CombatAssessmentGap gaps, bool expectedBind)
        {
            var input = Input();
            var facts = new EnemyScoreFacts(gaps, precise: true, projectedOutcome: StageOutcome.Defeat);
            Assert.That(ScoreAt(input, facts).AdditionalForecastBind, Is.EqualTo(expectedBind));
        }

        [Test]
        public void ScorerPreservesExistingTermsAndFollowupBenefit()
        {
            var facts = new EnemyScoreFacts(CombatAssessmentGap.HiddenEnvironment, opponentHpLoss: 250, alliedHpLoss: 100,
                opponentEliminations: 1, restrictionDelta: 500);
            var original = facts.WithPosition(CombatAssessmentGap.FuturePlayerChoice, 100, 100, 3, 200);
            var bound = facts.WithPosition(CombatAssessmentGap.FuturePlayerChoice, 100, 100, 3, 200, additionalForecastBind: true);
            Assert.That(original.AdditionalForecastBind, Is.False);
            Assert.That(bound.AdditionalForecastBind, Is.True);
            Assert.That((EnemyUtilityScorer.Score(original), EnemyUtilityScorer.Score(bound)), Is.EqualTo((2260L, 1760L)), "후속 200 × 1, 위협 100 / 2(R-3)");
            Assert.That(EnemyUtilityScorer.AttackBenefit(bound), Is.EqualTo(2450));
            Assert.That(EnemyUtilityScorer.AttackBenefit(original), Is.EqualTo(EnemyUtilityScorer.AttackBenefit(bound)));
            Assert.That(bound.Gaps, Is.EqualTo(original.Gaps));
        }

        private static CombatCommandInput Input(int existingBindEnds = 0, int damagePercent = 0, int bindTurns = 1,
            long impactBoundary = 5, StageGimmickSpec[] definitions = null)
        {
            var source = EnemyHazardForecastTests.Input(definitions ?? new StageGimmickSpec[]
            {
                new RockfallGimmickSpec(maximumHealthLossPercent: damagePercent, bindAffectedSideTurns: bindTurns)
            }, warning: true).WithActor(new EntityId(2));
            var actor = Actor(source);
            var characters = source.Characters.Where(unit => unit.Owner == Owner.Player || unit.EntityId == actor.EntityId)
                .Select(unit => unit.EntityId == actor.EntityId && existingBindEnds > 0 ? WithBind(unit, existingBindEnds) : unit).ToArray();
            var profiles = source.Profiles.Select(profile =>
            {
                var definition = profile.Definition;
                return new CombatAttackProfile(new UnitSpec(definition.Key, definition.Role,
                    definition.FootprintWidth, definition.FootprintHeight, definition.FootprintLocalCells.ToArray(),
                    definition.MaxHealth, definition.MoveMethod, definition.AttackRange, definition.CategoryKey,
                    definition.AttackPower, definition.PhysicalDefense, definition.Priority, definition.Traits.ToArray(),
                    definition.OwnerEligibility, behavior: new UnitBehaviorSpec(true, false, false)), profile.Method);
            }).ToArray();
            return CombatCommandTestData.With(source, characters: characters, profiles: profiles,
                state: source.State.With(gimmicks: source.State.Gimmicks.Replace(new RockfallGimmickState(new StageGimmickId(1),
                    RockfallGimmickPhase.Warning, impactBoundary, actor.OccupiedCells))));
        }

        private static CharacterCombatFacts WithBind(CharacterCombatFacts actor, int remainingEnds) => actor.WithStatusEffects(
            AppliedStatusRules.Apply(actor.StatusEffects, new ActionRestrictionStatus(StatusEffectKey.Bind,
                new EffectOriginKey(EffectOwnerKind.Item, 77, 99, "forecast-bind-test"), remainingEnds)));

        private static EnemyScoreFacts ScoreAt(CombatCommandInput input, EnemyScoreFacts facts, MovementPath path = null,
            CombatAssessmentGap entryGaps = CombatAssessmentGap.None)
        {
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            EnemyThreatField threat = null;
            foreach (var _ in context.ThreatSteps(value => threat = value)) { }
            return PositionFacts(Actor(input), facts, path, EnemyActionCandidateTests.MakeForecast(input), threat, 0, 0, 0, entryGaps, 0);
        }

        private static CombatCommandInput AtNextOpportunity(CombatCommandInput input)
        {
            var enemyEnd = CombatTurnCalculator.EndEnemyPhase(input, Rules);
            Assert.That(enemyEnd.Accepted, Is.True, enemyEnd.Reason);
            var playerStart = CombatTurnCalculator.BeginControllerTurn(CombatCommandComposition.Project(enemyEnd), Array.Empty<IMovementTileRule>());
            Assert.That(playerStart.Accepted, Is.True, playerStart.Reason);
            var request = CombatCommandTestData.FreshCommand(CombatCommandComposition.Project(playerStart), CombatCommandKind.EndPlayerTurn);
            var playerEnd = CombatTurnCalculator.EndPlayerTurn(request, Rules);
            Assert.That(playerEnd.Accepted, Is.True, playerEnd.Reason);
            return CombatCommandComposition.Project(playerEnd).WithActor(input.ActorId);
        }

        private static CharacterCombatFacts Actor(CombatCommandInput input) =>
            input.Characters.Single(unit => unit.EntityId == input.ActorId);

        private static void RecordActual(string name, CombatCommandTransition transition)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(EnemyActionAssessmentTests.Encode(new object[]
            {
                CombatCommandComposition.Project(transition), transition.Events, transition.Outcome, transition.PathMovement
            }));
            TestContext.WriteLine(name + "=" + BitConverter.ToString(sha.ComputeHash(bytes)));
        }
    }
}
