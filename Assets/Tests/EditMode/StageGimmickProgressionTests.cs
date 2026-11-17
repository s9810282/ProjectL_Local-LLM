using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class StageGimmickProgressionTests
    {
        [Test]
        public void DefinitionRegistryIsTheSameWithAndWithoutMovementRules()
        {
            var definitions = new StageGimmickSpec[] {
                new RailwayGimmickSpec(1, 1), new HaboobGimmickSpec(), new RockfallGimmickSpec(),
                new TornadoGimmickSpec(new[] { new TornadoMemberSpec(31, new[] { Owner.Player, Owner.Enemy }) }) };
            Assert.DoesNotThrow(() => StageGimmickComposition.Default.ValidateDefinitions(definitions));
            Assert.DoesNotThrow(() => StageGimmickComposition.WithMovementRules(TornadoEnvironmentTests.Rules).ValidateDefinitions(definitions));
        }

        [Test]
        public void RulelessScheduledEntryReportsRequiredMovementRulesBeforeAdvancingTornado()
        {
            var input = TornadoGimmickResolutionTests.Input(count: 1);
            var error = Assert.Throws<InvalidOperationException>(() => CombatGimmickCalculator.TryAdvance(input,
                new StageGimmickBoundary(Owner.Player, 1)));
            Assert.That(error.Message, Does.Contain(nameof(IMovementTileRule)));
        }

        [Test]
        public void ExplicitMovementRuleEntryRejectsNullInsteadOfUsingAnUnboundHandler()
        {
            var input = TornadoGimmickResolutionTests.Input(count: 1);
            Assert.Throws<ArgumentNullException>(() => CombatGimmickCalculator.TryAdvance(input,
                new StageGimmickBoundary(Owner.Player, 1), null));
        }

        [Test]
        public void RegistryRejectsDuplicateAndMissingHandlers()
        {
            Assert.Throws<InvalidOperationException>(() => new StageGimmickComposition(
                new IStageGimmickRule[] { new RailwayGimmickRule(), new RailwayGimmickRule() }));
            Assert.Throws<InvalidOperationException>(() => new StageGimmickComposition(new IStageGimmickRule[] {
                new RailwayGimmickRule() }).ValidateDefinitions(new StageGimmickSpec[] { new RockfallGimmickSpec() }));
            Assert.Throws<InvalidOperationException>(() => new StageGimmickComposition(new IStageGimmickRule[] {
                new RailwayGimmickRule() }).ValidateDefinitions(new StageGimmickSpec[] { new HaboobGimmickSpec() }));
        }

        [TestCase(Owner.Player)]
        [TestCase(Owner.Enemy)]
        public void ProductionEndPassesOneBoundaryWithoutIncrementingAgain(Owner owner)
        {
            var input = CombatCommandTestData.Input();
            var battle = new CombatBattleFacts(8, 42, 2, 2, Array.Empty<CharacterInstanceId>(), 0,
                new[] { CellCoord.Zero }, new StageGimmickSpec[] { new RailwayGimmickSpec(1, 1) });
            var state = input.State.With(phase: owner == Owner.Player ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic,
                turn: new CombatTurnFacts(StagePhase.Combat, controllerPhase: owner == Owner.Player ? CombatControllerPhase.Player : CombatControllerPhase.Enemy,
                    controllerTurnSequence: 1));
            input = new CombatCommandInput(1, owner == Owner.Player ? CombatCommandKind.EndPlayerTurn : CombatCommandKind.CompleteEnemyTurn,
                input.ActorId, default, state, input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls,
                input.Artifacts, battle: battle, battleInstanceId: 1, expectedBattleInstanceId: 1, expectedControllerTurnSequence: 1);
            var result = owner == Owner.Player ? CombatTurnCalculator.EndPlayerTurn(input, Array.Empty<IMovementTileRule>()) :
                CombatTurnCalculator.EndEnemyPhase(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatGimmickEvent>().Count(), Is.EqualTo(1));
            var projected = CombatCommandComposition.Project(result);
            Assert.Throws<InvalidOperationException>(() => CombatGimmickCalculator.TryAdvance(projected, new StageGimmickBoundary(owner, 2)));
            Assert.DoesNotThrow(() => CombatGimmickCalculator.TryAdvance(projected,
                new StageGimmickBoundary(owner, result.FinalState.Turn.CompletedControllerTurnCount)));
        }

        [Test]
        public void TurnCopyAndAttackCompositionPreserveWarningFacts()
        {
            var input = CombatCommandTestData.Input();
            var facts = new StageGimmickStateSet(new StageGimmickState[] {
                new RockfallGimmickState(new StageGimmickId(1), RockfallGimmickPhase.Warning, 4, new[] { new CellCoord(2, 2) }) });
            var state = input.State.With(gimmicks: facts);
            input = input.WithWorld(state, input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var attack = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(attack.FinalState.Gimmicks.SameValues(facts), Is.True);
            Assert.That(CombatCommandComposition.Project(attack).State.Gimmicks.SameValues(facts), Is.True);
            Assert.That(CombatTurnCalculator.State(state, state.Turn, state.Phase).Gimmicks.SameValues(facts), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompositionProjectsInRegistrationOrderAndStopsAfterTerminal(bool terminal)
        {
            var source = CombatCommandTestData.Input();
            var turn = source.State.Turn;
            var state = source.State.With(turn: new CombatTurnFacts(turn.StagePhase, controllerPhase: turn.ControllerPhase,
                controllerTurnSequence: turn.ControllerTurnSequence, completedControllerTurnCount: 1));
            var battle = new CombatBattleFacts(8, 42, 2, 2, Array.Empty<CharacterInstanceId>(), 0,
                new[] { terminal ? new CellCoord(7, 7) : new CellCoord(1, 3) },
                new StageGimmickSpec[] { new HaboobGimmickSpec(), new RailwayGimmickSpec(1, 1) });
            var input = new CombatCommandInput(1, source.Kind, source.ActorId, source.Aim, state, source.Characters,
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts, battle: battle);
            var observer = new ObservingRule();
            var composition = new StageGimmickComposition(new IStageGimmickRule[] { new RailwayGimmickRule(), observer });
            var result = composition.TryAdvance(input, new StageGimmickBoundary(Owner.Player, 1));
            Assert.That(result.Outcome.IsStageEnded, Is.EqualTo(terminal));
            Assert.That(observer.Input != null, Is.EqualTo(!terminal));
            if (!terminal)
            {
                Assert.That(observer.Input.Characters.Single(c => c.EntityId == source.ActorId).Health.IsDead, Is.True);
                Assert.That(observer.Id, Is.EqualTo(new StageGimmickId(1)));
                Assert.That(observer.Boundary.Value.CompletedControllerTurnCount, Is.EqualTo(result.FinalState.Turn.CompletedControllerTurnCount));
            }
        }

        [Test]
        public void StateCollectionsFreezeInputsAndRejectInvalidIdentityOrGeometry()
        {
            var cells = new[] { CellCoord.Zero };
            var warning = new RockfallGimmickState(new StageGimmickId(1), RockfallGimmickPhase.Warning, 4, cells);
            var entries = new StageGimmickState[] { warning };
            var facts = new StageGimmickStateSet(entries);
            var spec = new RockfallGimmickSpec(footprintOffsets: cells);
            cells[0] = new CellCoord(3, 3);
            entries[0] = null;
            Assert.That(warning.WarnedCells.Single(), Is.EqualTo(CellCoord.Zero));
            Assert.That(spec.FootprintOffsets.Single(), Is.EqualTo(CellCoord.Zero));
            Assert.That(facts.Get(warning.Id), Is.SameAs(warning));
            Assert.Throws<ArgumentException>(() => new StageGimmickStateSet(new StageGimmickState[] { warning, warning }));
            Assert.Throws<InvalidOperationException>(() => facts.Replace(new RockfallGimmickState(new StageGimmickId(2),
                RockfallGimmickPhase.Warning, 4, Array.Empty<CellCoord>())));
            Assert.Throws<InvalidOperationException>(() => facts.Replace(new HaboobGimmickState(warning.Id,
                HaboobGimmickPhase.Active, 4, new[] { CellCoord.Zero })));
            Assert.Throws<ArgumentException>(() => new RockfallGimmickState(warning.Id, RockfallGimmickPhase.Cooldown, 4, cells));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HaboobGimmickSpec(2, 3));
            Assert.Throws<ArgumentException>(() => new HaboobGimmickSpec(footprintOffsets: cells));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RockfallGimmickSpec(maximumHealthLossPercent: 101));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RockfallGimmickState(warning.Id, RockfallGimmickPhase.Warning, 0, cells));
            Assert.That(facts.Without(warning.Id).SameValues(StageGimmickStateSet.Empty), Is.True);
        }

        private sealed class ObservingRule : IStageGimmickRule
        {
            public System.Collections.Generic.IEnumerator<HazardCellFact?> EnumerateForecast(CombatCommandInput input,
                StageGimmickSpec definition, StageGimmickId id, long lastBoundaryCount)
            {
                yield break;
            }

            public StageGimmickKey Key => StageGimmickKey.Haboob;
            public bool RunsAt(StageGimmickBoundaryPhase phase) => phase == StageGimmickBoundaryPhase.Scheduled;
            public StageGimmickState Initialize(CombatCommandState initialState, StageGimmickSpec definition,
                StageGimmickId id, ref uint randomState) => throw new NotSupportedException();

            internal CombatCommandInput Input { get; private set; }
            internal StageGimmickId Id { get; private set; }
            internal StageGimmickBoundary? Boundary { get; private set; }

            public CombatCommandTransition TryAdvance(CombatCommandInput input, StageGimmickSpec definition,
                StageGimmickId id, StageGimmickBoundary? boundary)
            {
                Input = input;
                Id = id;
                Boundary = boundary;
                return null;
            }
        }
    }
}
