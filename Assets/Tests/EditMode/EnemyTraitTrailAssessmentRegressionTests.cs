using System;
using System.Linq;
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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyTraitTrailAssessmentRegressionTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static readonly CellCoord Crossing = new CellCoord(1, 0);
        private static readonly CellCoord Endpoint = new CellCoord(2, 0);

        [TestCase(TraitKey.GroundCompaction, false)]
        [TestCase(TraitKey.GroundCompaction, true)]
        [TestCase(TraitKey.DustScreen, false)]
        [TestCase(TraitKey.DustScreen, true)]
        public void MoveAcrossPassiveTrailKeepsPreciseProjection(TraitKey trait, bool atEndpoint)
        {
            var input = MovementTestData.Route(Approach(), CellCoord.Zero, Crossing, Endpoint);
            input = TraitTrailTestData.Environment(input, Trail(trait, atEndpoint ? Endpoint : Crossing));
            string before = EnemyActionAssessmentTests.Encode(input);

            var actual = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            var actualActor = MovementTestData.Actor(actual);
            Assert.That(actualActor.Anchor, Is.EqualTo(Endpoint));
            Assert.That(actualActor.Health.CurrentHealth, Is.EqualTo(100));

            var facts = EnemyActionAssessmentTests.Assess(input, NoRules, out var moved);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.Gaps & ~CombatAssessmentGap.HiddenEnvironment, Is.EqualTo(CombatAssessmentGap.None));
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.Zero);
            Assert.That(moved, Is.Not.Null);
            var projectedActor = moved.Characters.Single(c => c.EntityId == input.ActorId);
            Assert.That(projectedActor.Anchor, Is.EqualTo(actualActor.Anchor));
            Assert.That(projectedActor.Health, Is.EqualTo(actualActor.Health));
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [TestCase(TraitKey.GroundCompaction)]
        [TestCase(TraitKey.DustScreen)]
        public void KnockbackIntoPassiveTrailKeepsAttackBenefit(TraitKey trait)
        {
            var targetCell = new CellCoord(2, 1);
            var landing = new CellCoord(3, 1);
            var input = EnemyMovementPathTests.Input(targetCell: targetCell);
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == input.ActorId
                    ? c.With(stats: c.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(1) }))
                    : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Demon
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Demon, 10, CategoryKey.Main,
                        new KnockbackTraitSpec(1)), p.Method) : p).ToArray());
            input = input.WithIntent(CombatCommandKind.Attack, AttackAim.ForCell(targetCell), default, null, null);
            input = TraitTrailTestData.Environment(input, Trail(trait, landing));

            var actual = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            var target = TraitCombatFixtures.Final(actual, 2);
            Assert.That(target.Anchor, Is.EqualTo(landing));
            Assert.That(target.Health.CurrentHealth, Is.LessThan(100));

            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            Assert.That(CombatActionAssessment.TryAssess(input,
                EnemyDecision.Attack(input.ActorId, input.Aim, default), NoRules, context, out var facts, out _), Is.True);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.Gaps & ~CombatAssessmentGap.HiddenEnvironment, Is.EqualTo(CombatAssessmentGap.None));
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.Zero);
            Assert.That(facts.OpponentHpLossPermille, Is.EqualTo((100 - target.Health.CurrentHealth) * 10L));
            Assert.That(EnemyUtilityScorer.AttackBenefit(facts), Is.Positive);
            Assert.That(context.PreviewCount, Is.EqualTo(1));
        }

        [TestCase(TraitKey.GroundCompaction)]
        [TestCase(TraitKey.DustScreen)]
        public void UtilityAdvancesThroughPassiveTrailInsteadOfHolding(TraitKey trait)
        {
            // 한 칸 높이 통로에서 공격 위치에 도달하려면 공개 흔적을 반드시 지나야 한다.
            var input = TraitTrailTestData.Environment(Approach(), Trail(trait, Crossing));
            using var session = new EnemyDecisionSession(input, NoRules, EnemyDecisionSettings.Default);
            EnemyUtilityDecisionTests.Drain(session);

            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            Assert.That(session.Decision.Path.Cells, Is.EqualTo(new[] { CellCoord.Zero, Crossing, Endpoint }));
            var actual = CombatCommandCalculator.AdvancePrepared(input, session.Decision, NoRules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            Assert.That(TraitCombatFixtures.Final(actual, (int)input.ActorId.Value).Anchor, Is.EqualTo(Endpoint));
        }

        private static CombatCommandInput Approach()
        {
            var input = EnemyUtilityDecisionTests.Approach();
            return CombatCommandTestData.With(input,
                state: new CombatCommandState(5, 1, input.State.Phase,
                    input.State.Structures, input.State.Turn),
                characters: new[]
                {
                    input.Characters[0], input.Characters[1],
                    CombatCommandTestData.With(input.Characters[2], position: new CellCoord(4, 0))
                });
        }

        private static MovementTileFact Trail(TraitKey trait, CellCoord cell) =>
            TraitTrailTestData.Trail(71, cell, trait,
                movement: trait == TraitKey.GroundCompaction ? 1 : 0,
                range: trait == TraitKey.DustScreen ? 1 : 0,
                percent: trait == TraitKey.DustScreen ? 20 : 0);
    }
}
