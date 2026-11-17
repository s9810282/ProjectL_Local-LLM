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
    public sealed class GimmickEvidenceOrderingTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ChainContactUsesMovementOnlyIfItPrecedesTheContact(bool movedEarlier)
        {
            var before = LavaChainGimmickTests.Warned(new CellCoord(2, 1));
            var actor = before.Characters.Single(c => c.EntityId == before.ActorId);
            var moved = actor.WithPlacement(actor.Area, actor.ReserveSlotIndex, new CellCoord(2, 2), actor.Rotation,
                new[] { new CellCoord(2, 2) });
            var after = CombatCommandTestData.With(before,
                characters: before.Characters.Select(c => c.EntityId == actor.EntityId ? moved : c).ToArray());
            var result = LavaChainGimmickTests.At(after, 3, StageGimmickBoundaryPhase.Scheduled);
            var original = CombatCommandTestData.With(result.Input, characters: before.Characters.ToArray());
            var movement = new CombatCharacterEvent(CombatEventKind.Moved, actor, moved, null, null,
                CombatDamageCause.NumericDamage, new CombatMovementCause(CombatMovementAgency.Forced));
            var proposal = WithPrecedingEvent(result, original, movement, movedEarlier);

            if (movedEarlier) Assert.DoesNotThrow(() => LavaChainGimmickEvidenceValidator.Validate(proposal));
            else Assert.Throws<InvalidOperationException>(() => LavaChainGimmickEvidenceValidator.Validate(proposal));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MeteorCancellationUsesWallOnlyIfItPrecedesTheImpact(bool wallEarlier)
        {
            var cell = new CellCoord(0, 7);
            var wall = new StaticTerrainFact(1, cell, StaticTerrainKind.Wall, false, new[] { Owner.Player, Owner.Enemy });
            var active = new StaticTerrainFact(1, cell, StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy });
            var source = CombatCommandTestData.Input();
            var initial = new MeteorGimmickState(new StageGimmickId(1), 4,
                new[] { new MeteorFootprint(4, 1, new[] { cell }) }, Array.Empty<MeteorFlameArea>());
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] { active });
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.StartingEnemy,
                controllerTurnSequence: 1, completedPlayerTurnCount: 2, completedControllerTurnCount: 4);
            var state = source.State.With(gimmicks: new StageGimmickStateSet(new StageGimmickState[] { initial }),
                environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment),
                turn: turn, phase: CombatFlowPhase.Automatic);
            var battle = new CombatBattleFacts(8, 42, 2, 2, Array.Empty<CharacterInstanceId>(), 0,
                gimmicks: new StageGimmickSpec[] { new MeteorGimmickSpec(4, 2, 1, 1, 1, 0, 3, 4) });
            var input = new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, state,
                source.Characters, source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls,
                source.Artifacts, source.TargetCell, source.Rotation, battle, source.ProcessingKind,
                environmentBindings: new EnvironmentSemanticBindings(new[] { 1L }, Array.Empty<long>(), Array.Empty<EntityId>()));
            var result = CombatGimmickCalculator.TryAdvance(input,
                new StageGimmickBoundary(Owner.Player, 4, StageGimmickBoundaryPhase.Scheduled), Array.Empty<IMovementTileRule>());
            Assert.That(result.Events.OfType<CombatMeteorImpactEvent>().Single().Cancelled, Is.True);
            var original = CombatCommandTestData.With(input, state: state.With(
                environment: new MovementEnvironmentFacts(new MovementTileFact[] { wall })));
            var proposal = WithPrecedingEvent(result, original, new CombatMovementSourceEvent(wall, active), wallEarlier);

            if (wallEarlier) Assert.DoesNotThrow(() => MeteorGimmickEvidenceValidator.Validate(proposal));
            else Assert.Throws<InvalidOperationException>(() => MeteorGimmickEvidenceValidator.Validate(proposal));
        }

        private static CombatCommandTransition WithPrecedingEvent(CombatCommandTransition result,
            CombatCommandInput original, CombatEvent occurrence, bool before)
        {
            var turn = new CombatTurnEvent(CombatEventKind.PlayerTurnEnded, EntityId.Empty, original.State.Turn, result.FinalState.Turn);
            var events = before ? new CombatEvent[] { turn, occurrence }.Concat(result.Events) :
                new CombatEvent[] { turn }.Concat(result.Events).Append(occurrence);
            return new CombatCommandTransition(original, result.Accepted, result.Reason, result.Characters, result.Field,
                result.FinalState, result.Outcome, events: events.ToArray(), healthExecutions: result.HealthExecutions,
                healthClaims: result.HealthClaims);
        }
    }
}
