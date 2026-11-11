using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    public sealed class ControllerTurnCountTests
    {
        [Test]
        public void CountIsPartOfTurnEqualityAndCommandStateCopies()
        {
            var initial = new CombatTurnFacts(StagePhase.Deployment);
            var counted = new CombatTurnFacts(StagePhase.Deployment, completedControllerTurnCount: 16);
            Assert.That(initial.CompletedControllerTurnCount, Is.Zero);
            Assert.That(initial.SameValues(counted), Is.False);
            Assert.That(counted.SameValues(new CombatTurnFacts(StagePhase.Deployment, completedControllerTurnCount: 16)), Is.True);
            Assert.Throws<ArgumentException>(() => new CombatTurnFacts(StagePhase.Deployment, completedControllerTurnCount: -1));
            var state = CombatCommandTestData.Input().State.With(turn: counted, phase: CombatFlowPhase.PlayerInput);
            Assert.That(state.With().Turn.CompletedControllerTurnCount, Is.EqualTo(16));
            Assert.That(state.SameValues(state.With(turn: initial)), Is.False);
        }

        [Test]
        public void BothEndsIncrementButStartAndRejectedDuplicatePreserve()
        {
            var input = EnemyControllerPhaseTests.Input(CombatCommandTestData.Input(empty: true), CombatControllerPhase.Player);
            var end = CombatTurnCalculator.EndPlayerTurn(input, Array.Empty<IMovementTileRule>());
            Assert.That(end.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(1));
            var after = CombatCommandComposition.Project(end);
            var duplicate = CombatTurnCalculator.EndPlayerTurn(after, Array.Empty<IMovementTileRule>());
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(duplicate.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(1));
            var start = CombatTurnCalculator.BeginControllerTurn(after, Array.Empty<IMovementTileRule>());
            Assert.That(start.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(1));
            var enemyEnd = CombatTurnCalculator.EndEnemyPhase(CombatCommandComposition.Project(start), Array.Empty<IMovementTileRule>());
            Assert.That(enemyEnd.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(2));
            Assert.That(enemyEnd.FinalState.Turn.CompletedPlayerTurnCount, Is.EqualTo(1));
            var merged = CombatCommandComposition.Merge(input, new[] { end, start, enemyEnd });
            Assert.That(merged.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(2));
            Assert.That(merged.Events.OfType<CombatGimmickEvent>(), Is.Empty);
        }

        [Test]
        public void StalePlayerEndPreservesCount()
        {
            var input = EnemyControllerPhaseTests.Input(CombatCommandTestData.Input(empty: true),
                CombatControllerPhase.Player, expectedSequence: 9);
            var result = CombatTurnCalculator.EndPlayerTurn(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("StaleTurn"));
            Assert.That(result.FinalState.Turn.CompletedControllerTurnCount, Is.Zero);
        }
    }
}
