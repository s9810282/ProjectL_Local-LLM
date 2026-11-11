using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyControllerPhaseTests
    {
        [TestCase(CombatControllerPhase.StartingPlayer, Owner.Player)]
        [TestCase(CombatControllerPhase.StartingEnemy, Owner.Enemy)]
        public void Start_ResetsOnlyLivingFieldUnitsOfOwner(CombatControllerPhase phase, Owner owner)
        {
            var source = CombatCommandTestData.Input();
            var characters = source.Characters.Select(c => c.With(actionUse: UnitActionUse.Attacked)).ToArray();
            var input = Input(source, phase, characters: characters);
            var result = CombatTurnCalculator.BeginControllerTurn(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = CombatCommandComposition.Project(result);
            foreach (var actor in final.Characters)
                Assert.That(actor.ActionUse, Is.EqualTo(actor.Owner == owner ? UnitActionUse.Available : UnitActionUse.Attacked));
            Assert.That(result.FinalState.Turn.ControllerTurnSequence, Is.EqualTo(12));
            Assert.That(result.Events.OfType<CombatTurnEvent>().Single().Controller, Is.EqualTo(owner));
        }

        [Test]
        public void DeferredUnit_BecomesAvailableOnlyAtOwnerStart()
        {
            var input = Input(CombatCommandTestData.Input(), CombatControllerPhase.StartingEnemy,
                characters: CombatCommandTestData.Input().Characters.Select(c => c.With(actionUse: UnitActionUse.Deferred)).ToArray());
            var result = CombatTurnCalculator.BeginControllerTurn(input, Array.Empty<IMovementTileRule>());
            var final = CombatCommandComposition.Project(result);
            Assert.That(final.Characters.Where(c => c.Owner == Owner.Player).All(c => c.ActionUse == UnitActionUse.Deferred), Is.True);
            Assert.That(final.Characters.Where(c => c.Owner == Owner.Enemy).All(c => c.ActionUse == UnitActionUse.Available), Is.True);
        }

        [Test]
        public void ManualEnd_IncrementsOnceAndDuplicateRejects()
        {
            var input = Input(CombatCommandTestData.Input(), CombatControllerPhase.Player);
            var result = CombatTurnCalculator.EndPlayerTurn(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Turn.CompletedPlayerTurnCount, Is.EqualTo(1));
            Assert.That(result.FinalState.Turn.ControllerTurnSequence, Is.EqualTo(11));
            Assert.That(CombatTurnCalculator.EndPlayerTurn(CombatCommandComposition.Project(result), Array.Empty<IMovementTileRule>()).Accepted, Is.False);
        }

        [Test]
        public void StaleEndCannotEndLaterPlayerTurn()
        {
            var input = Input(CombatCommandTestData.Input(), CombatControllerPhase.Player, expectedSequence: 9);
            var result = CombatTurnCalculator.EndPlayerTurn(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("StaleTurn"));
            Assert.That(result.Characters.Characters, Is.Empty);
        }

        [Test]
        public void PlayerPhaseCannotBeStartedAgainOrAutomaticallyEnded()
        {
            var input = Input(CombatCommandTestData.Input(), CombatControllerPhase.Player);
            Assert.That(CombatTurnCalculator.BeginControllerTurn(input, Array.Empty<IMovementTileRule>()).Accepted, Is.False);
            Assert.That(input.State.Turn.CompletedPlayerTurnCount, Is.Zero);
        }

        internal static CombatCommandInput Input(CombatCommandInput source, CombatControllerPhase phase,
            long completed = 0, long baseline = 0, long sequence = 11, long? expectedSequence = null,
            CharacterCombatFacts[] characters = null)
        {
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: phase,
                controllerTurnSequence: sequence, completedPlayerTurnCount: completed);
            // 적 진영 턴 중간이면 적 진영 시작이 이미 적 예산을 채운 상태다(적 공용 코스트 설계 §3 1번).
            var state = source.State.With(turn: turn,                phase: phase == CombatControllerPhase.Player ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic);
            bool player = phase == CombatControllerPhase.Player;
            return new CombatCommandInput(player ? 1 : 0, player ? CombatCommandKind.EndPlayerTurn : CombatCommandKind.StartControllerTurn,
                EntityId.Empty, default, state, characters ?? source.Characters.ToArray(), source.Profiles, source.Main,
                source.Owned, source.SoulCounts, source.Souls, source.Artifacts, battle: source.Battle,
                processingKind: player ? CombatProcessingKind.Command : CombatProcessingKind.Advance,
                battleInstanceId: 17, environmentBindings: source.EnvironmentBindings,
                obstacleContents: source.ObstacleContents,
                expectedBattleInstanceId: 17, expectedControllerTurnSequence: expectedSequence ?? sequence);
        }
    }
}
