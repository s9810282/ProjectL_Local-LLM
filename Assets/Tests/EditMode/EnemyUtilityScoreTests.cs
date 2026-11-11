using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyUtilityScoreTests
    {
        [Test]
        public void LockedWeightsProduceExactFixedPointValues()
        {
            const CombatAssessmentGap complete = CombatAssessmentGap.None;
            Assert.That(EnemyUtilityScorer.Score(new EnemyScoreFacts(complete, opponentHpLoss: 250)), Is.EqualTo(250L));
            Assert.That(EnemyUtilityScorer.Score(new EnemyScoreFacts(complete, opponentHpLoss: 250, opponentEliminations: 1)), Is.EqualTo(2250L));
            Assert.That(EnemyUtilityScorer.Score(new EnemyScoreFacts(complete, alliedHpLoss: 250)), Is.EqualTo(-750L));
            Assert.That(EnemyUtilityScorer.Score(new EnemyScoreFacts(complete, alliedEliminations: 1)), Is.EqualTo(-4000L));
            Assert.That(EnemyUtilityScorer.Score(new EnemyScoreFacts(complete, actorHazardLoss: 1000)), Is.EqualTo(-4000L));
            Assert.That(EnemyUtilityScorer.Score(new EnemyScoreFacts(complete, playerThreat: 250)), Is.EqualTo(-125L));
            Assert.That(EnemyUtilityScorer.Score(new EnemyScoreFacts(complete, playerThreat: 250, playerThreatLethal: true)), Is.EqualTo(-500L));
            Assert.That(EnemyUtilityScorer.Score(new EnemyScoreFacts(complete, nextTurnStrike: 300)), Is.EqualTo(150L));
            Assert.That(EnemyUtilityScorer.Score(new EnemyScoreFacts(complete, engagementProgress: 3)), Is.EqualTo(60L));
            Assert.That(EnemyUtilityScorer.Score(new EnemyScoreFacts(complete, engagementProgress: 101)), Is.EqualTo(100L));
            Assert.That(EnemyUtilityScorer.Score(new EnemyScoreFacts(complete, subsequentAttackBenefit: 200)), Is.EqualTo(200L));
            Assert.That(EnemyUtilityScorer.Score(new EnemyScoreFacts(complete, restrictionDelta: 500)), Is.EqualTo(500L));
        }

        [Test]
        public void EscapeAndSafeProgressMustStrictlyBeatTheirHold()
        {
            const CombatAssessmentGap complete = CombatAssessmentGap.None;
            var riskyHold = new EnemyScoreFacts(complete, actorHazardLoss: 1000);
            var escape = new EnemyScoreFacts(complete, actorHazardLoss: 250);
            var safeHold = new EnemyScoreFacts(complete);
            Assert.That(EnemyUtilityScorer.Score(escape), Is.EqualTo(-1000L));
            Assert.That(EnemyUtilityScorer.BeatsHold(escape, riskyHold), Is.True);
            Assert.That(EnemyUtilityScorer.BeatsHold(new EnemyScoreFacts(complete, engagementProgress: 1), safeHold), Is.True);
            Assert.That(EnemyUtilityScorer.BeatsHold(safeHold, safeHold), Is.False);
        }

        [Test]
        public void HealthLossCapsOverkillAndArithmeticIsChecked()
        {
            Assert.That(EnemyUtilityScorer.LossPermille(900, 25, 100), Is.EqualTo(250L));
            Assert.That(EnemyUtilityScorer.LossPermille(31, 125, 125), Is.EqualTo(248L));
            Assert.Throws<OverflowException>(() =>
                EnemyUtilityScorer.Score(new EnemyScoreFacts(CombatAssessmentGap.None, alliedHpLoss: long.MaxValue)));
        }

        [Test]
        public void ObservableUnsupportedAlliedDamageKeepsItsUpperBound()
        {
            var uncertain = new EnemyScoreFacts(CombatAssessmentGap.HiddenEnvironment | CombatAssessmentGap.ActualOnlyReaction,
                opponentHpLoss: 500, alliedHpLoss: 100, possibleAlliedHpLoss: 1000);
            var hold = new EnemyScoreFacts(CombatAssessmentGap.HiddenEnvironment);
            Assert.That(EnemyUtilityScorer.Score(uncertain), Is.EqualTo(-2500L));
            Assert.That(EnemyUtilityScorer.BeatsHold(uncertain, hold), Is.False);
        }

        [Test]
        public void EqualScoresUseActionAimAndFullRouteOrder()
        {
            var actor = new EntityId(1);
            var attack = EnemyDecision.Attack(actor, AttackAim.ForCell(new CellCoord(2, 0)), default);
            var laterAim = EnemyDecision.Attack(actor, AttackAim.ForCell(new CellCoord(0, 1)), default);
            var end = CellCoord.Right + CellCoord.Up;
            var earlierPath = EnemyDecision.Move(actor, new MovementPath(new[] { CellCoord.Zero, CellCoord.Right, end }), default);
            var laterPath = EnemyDecision.Move(actor, new MovementPath(new[] { CellCoord.Zero, CellCoord.Up, end }), default);
            Assert.That(EnemyUtilityScorer.CompareIntent(attack, earlierPath), Is.LessThan(0));
            Assert.That(EnemyUtilityScorer.CompareIntent(attack, laterAim), Is.LessThan(0));
            Assert.That(EnemyUtilityScorer.CompareIntent(earlierPath, laterPath), Is.LessThan(0));
        }

        [Test]
        public void CertifiedSuccessOutranksSafeHoldEvenWithSacrifice()
        {
            var victory = new EnemyScoreFacts(CombatAssessmentGap.None, precise: true, alliedEliminations: 1,
                projectedOutcome: StageOutcome.Defeat);
            var hold = new EnemyScoreFacts(CombatAssessmentGap.None);
            Assert.That(EnemyUtilityScorer.BeatsHold(victory, hold), Is.True);
        }

        [TestCase(StageOutcome.Defeat, CombatAssessmentGap.None, 1)]
        [TestCase(StageOutcome.Victory, CombatAssessmentGap.None, -1)]
        [TestCase(StageOutcome.Defeat, CombatAssessmentGap.HiddenEnvironment, 0)]
        [TestCase(StageOutcome.Defeat, CombatAssessmentGap.RandomOutcome, 0)]
        public void TerminalTierRequiresCompleteCoverageAndUsesPlayerOutcome(StageOutcome outcome, CombatAssessmentGap gap, int expected)
        {
            var facts = new EnemyScoreFacts(gap, precise: true, projectedOutcome: outcome);
            Assert.That(EnemyUtilityScorer.Rank(facts).tier, Is.EqualTo(expected));
        }
    }
}
