using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class DeadHandPreviewTests
    {
        [Test]
        public void HiddenMineDoesNotChangeVisiblePreviewButActsInLiveAttack()
        {
            var clear = DeadHandTestData.Input(active: true);
            var input = TraitTrailTestData.Environment(clear,
                MineEnvironmentTests.Mine(damage: 100, hidden: true, cell: new CellCoord(4, 2)));
            var preview = CombatCommandCalculator.Preview(input, MineEnvironmentTests.Rules, out var projection);
            var baseline = CombatCommandCalculator.Preview(clear, MineEnvironmentTests.Rules);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Sweep.Targets.Single().EnteredCells, Is.EqualTo(baseline.Sweep.Targets.Single().EnteredCells));
            var shown = preview.Targets.Span[0];
            var expected = baseline.Targets.Span[0];
            Assert.That((shown.TargetId, shown.ExpectedDamage, shown.TargetHealthAfter),
                Is.EqualTo((expected.TargetId, expected.ExpectedDamage, expected.TargetHealthAfter)));
            Assert.That(projection.Events.OfType<CombatObjectActivationEvent>(), Is.Empty);
            var decision = EnemyDecision.Attack(input.ActorId, input.Aim, default);
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            Assert.That(CombatActionAssessment.TryAssess(input, decision, MineEnvironmentTests.Rules, context,
                out var facts, out _), Is.True);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.Gaps, Is.EqualTo(CombatAssessmentGap.HiddenEnvironment));

            var live = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            Assert.That(live.Events.OfType<CombatObjectActivationEvent>().Count(), Is.EqualTo(1));
            Assert.That(live.Attack.Value.Sweep.Targets.Single().Stop, Is.EqualTo(AttackSweepStop.Interrupted));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 2).Anchor, Is.EqualTo(new CellCoord(3, 2)));
            Assert.That(input.State.Environment.Tiles.OfType<MineSourceFact>().Single().Active, Is.True);
        }

        [TestCase(4, true)]
        [TestCase(1, false)]
        public void AssessmentInspectsVisibleUnsupportedEntriesOnlyOnPossibleSweepPath(int mineX, bool partial)
        {
            var input = TraitTrailTestData.Environment(DeadHandTestData.Input(active: true),
                MineEnvironmentTests.Mine(damage: 100, cell: new CellCoord(mineX, 2)));
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            Assert.That(CombatActionAssessment.TryAssess(input, EnemyDecision.Attack(input.ActorId, input.Aim, default),
                Array.Empty<IMovementTileRule>(), context, out var facts, out _), Is.True);
            Assert.That((facts.Gaps & CombatAssessmentGap.UnsupportedEffect) != 0, Is.EqualTo(partial));
            Assert.That(facts.IsPrecise, Is.EqualTo(!partial));
            Assert.That(context.PreviewCount, Is.EqualTo(partial ? 0 : 1));
        }

        [Test]
        public void DisposingAfterFirstProjectedTargetDoesNotAlterInput()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(2, 2), new CellCoord(3, 2) }, active: true);
            var work = CombatCommandCalculator.BeginPreview(input, Array.Empty<IMovementTileRule>());
            using (work)
            {
                while (!work.IsComplete && work.Phase != "SweepTargetComplete") work.Step();
                Assert.That(work.IsComplete, Is.False);
                Assert.Throws<InvalidOperationException>(() => { _ = work.Preview; });
            }
            Assert.Throws<ObjectDisposedException>(() => work.Step());
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 3).Anchor, Is.EqualTo(new CellCoord(3, 2)));
            var rerun = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(rerun.Sweep.Targets.First().StartAnchor, Is.EqualTo(new CellCoord(3, 2)));
            Assert.That(rerun.Sweep.Targets.First().FinalAnchor, Is.EqualTo(new CellCoord(5, 2)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ThreatFieldMarksActiveSweepAsPartial(bool active)
        {
            var input = StaticTerrainIntegrationTests.Terrain(
                DeadHandTestData.Input(new[] { new CellCoord(3, 3) }, active: active),
                new StaticTerrainFact(71, new CellCoord(3, 2), StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy }));
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 10
                ? CombatCommandTestData.With(c, position: new CellCoord(7, 7), appointRange: 1) : c).ToArray());
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var builder = new EnemyThreatField.Builder(context);
            while (!builder.IsComplete) builder.Step();
            var target = input.Characters.Single(c => c.EntityId.Value == 2);
            var exposure = builder.Build().Evaluate(target, target.Anchor.Value);
            Assert.That((exposure.Gaps & CombatAssessmentGap.UnsupportedEffect) != 0, Is.EqualTo(active));
            Assert.That(exposure.Gaps.HasFlag(CombatAssessmentGap.FuturePlayerChoice), Is.True);
            Assert.That(exposure.EstimatedLossPermille, Is.EqualTo(active ? 60 : 0));
        }

        [TestCase(TargetingChoiceMode.Single)]
        [TestCase(TargetingChoiceMode.Direction)]
        [TestCase(TargetingChoiceMode.All)]
        public void ClippedAttackMaskPreservesOffBoardIntentInPreviewAndLiveResult(TargetingChoiceMode mode)
        {
            var target = new CellCoord(7, 2);
            var method = new TargetingMethodSpec(TargetingShape.ForwardRectangle, mode, TargetingSplash.None, 0, width: 3);
            var aim = mode == TargetingChoiceMode.Direction ? AttackAim.ForDirection(AttackDirection.Up)
                : mode == TargetingChoiceMode.All ? AttackAim.ForAll() : AttackAim.ForCell(target);
            var input = DeadHandTestData.Input(new[] { target }, active: true, method: method, aim: aim);
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId == input.ActorId
                ? CombatCommandTestData.With(c, position: new CellCoord(7, 1)) : c).ToArray());
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var live = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var endpoint = new CellCoord(mode == TargetingChoiceMode.Direction ? 9 : 10, 2);
            foreach (var sweep in new[] { preview.Sweep, live.Attack.Value.Sweep })
            {
                var outcome = sweep.Targets.Single();
                Assert.That(outcome.IntendedEndpoint, Is.EqualTo(endpoint));
                Assert.That(outcome.EnteredCells, Is.Empty);
                Assert.That(outcome.FinalAnchor, Is.EqualTo(target));
                Assert.That(outcome.Stop, Is.EqualTo(AttackSweepStop.Blocked));
            }
            Assert.That(preview.DesignatedCells.ToArray().All(input.State.IsInside), Is.True);
        }

        [TestCase(AttackSweepSide.Left, true)]
        [TestCase(AttackSweepSide.Right, false)]
        public void ForcedEntryThatHarmsAnUndoParticipantInvalidatesItsCheckpoint(AttackSweepSide side, bool retained)
        {
            var input = DeadHandTestData.Input(active: true, side: side, percent: 0);
            var origin = new CellCoord(4, 2);
            input = TraitTrailTestData.Environment(input, MineEnvironmentTests.Mine(damage: 1, cell: origin));
            var main = CombatCommandTestData.With(input.Characters.Single(c => c.EntityId.Value == 10),
                position: new CellCoord(4, 3)).With(actionUse: UnitActionUse.Moved,
                stageRecord: CharacterStageRecord.Empty.Add(distanceMoved: 1));
            var checkpoint = new MovementUndoCheckpoint(input.BattleInstanceId, input.State.Turn.ControllerTurnSequence,
                1, main.EntityId, origin, Rotation4.R0, new[] { origin }, 1,
                MovementUndoOriginFacts.Capture(origin, input.State, input.EnvironmentBindings, input.Souls), main);
            input = input.WithWorld(input.State.With(movementUndo: new MovementUndoState(new[] { checkpoint })),
                input.Characters.Select(c => c.EntityId == main.EntityId ? main : c).ToArray(),
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);

            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Attack.Value.Sweep.Targets.Single().EnteredCells.Contains(origin), Is.EqualTo(!retained));
            Assert.That(result.Events.OfType<CombatObjectActivationEvent>().Any(), Is.EqualTo(!retained));
            Assert.That(result.FinalState.MovementUndo.TryGet(main.EntityId, out _), Is.EqualTo(retained));
        }

    }
}
