using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>현재 사실에 이동이 소유한 제한된 역산만 적용합니다. 원래 세계나 다른 효과의 값을 덮어쓰지 않습니다.</summary>
    public static class MovementEffectUndo
    {
        public static bool TryApply(CombatCommandInput input, MovementUndoCheckpoint checkpoint, out CombatCommandTransition candidate)
        {
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
            var availability = Evaluate(input, out var current, out candidate);
            if (availability.CanUndo && checkpoint.SameValues(current)) return true;
            candidate = null;
            return false;
        }

        internal static MovementUndoAvailability Evaluate(CombatCommandInput input, out MovementUndoCheckpoint checkpoint,
            out CombatCommandTransition candidate)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            checkpoint = null; candidate = null;
            MovementUndoAvailability Fail(MovementUndoFailureReason reason) => new MovementUndoAvailability(reason);
            if (input.State.Phase == CombatFlowPhase.Finished || input.State.Turn.StagePhase != StagePhase.Combat)
                return Fail(MovementUndoFailureReason.WrongPhase);
            if (!MovementUndoPolicy.InPlayerTurn(input) || input.ProcessingKind != CombatProcessingKind.Command)
                return Fail(MovementUndoFailureReason.WrongController);
            if (input.ExpectedBattleInstanceId != input.BattleInstanceId) return Fail(MovementUndoFailureReason.StaleBattle);
            if (input.ExpectedControllerTurnSequence != input.State.Turn.ControllerTurnSequence) return Fail(MovementUndoFailureReason.StaleTurn);
            var actor = input.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
            if (actor == null || actor.Owner != Owner.Player || actor.Area != CharacterRuntimeArea.Field || actor.Health.IsDead ||
                actor.OccupiedCells.Count != 1 || !actor.Anchor.HasValue || actor.Stats.MoveMethod != MoveMethod.LiftAndPlace)
                return Fail(MovementUndoFailureReason.ActorUnavailable);
            if (!input.State.MovementUndo.TryGet(input.ActorId, out checkpoint)) return Fail(MovementUndoFailureReason.NoCheckpoint);
            if (!GuardsMatch(input, checkpoint))
                return Fail(MovementUndoFailureReason.ReturnWouldCauseEffect);
            if (checkpoint.BattleInstanceId != input.BattleInstanceId) return Fail(MovementUndoFailureReason.StaleBattle);
            if (checkpoint.ControllerTurnSequence != input.State.Turn.ControllerTurnSequence) return Fail(MovementUndoFailureReason.StaleTurn);
            if (input.ExpectedMoveCommandNumber != 0 && input.ExpectedMoveCommandNumber != checkpoint.MoveCommandNumber)
                return Fail(MovementUndoFailureReason.StaleToken);
            if (!checkpoint.Participants.All(p => input.Characters.Any(p.ExpectedAfterMove.SameValues)) || actor.ActionUse != UnitActionUse.Moved)
                return Fail(MovementUndoFailureReason.ActorChanged);
            if (BossRoarCalculator.PendingBosses(input).Count != 0) return Fail(MovementUndoFailureReason.PendingReaction);
            var participants = checkpoint.Participants.Select(p => p.EntityId).ToArray();
            foreach (var participant in checkpoint.Participants)
            {
                var destination = participant.Restoration.To;
                if (!input.State.IsInside(destination) || EnvironmentTerrainRules.BlocksVoluntaryEntry(input.State.Environment, input.EnvironmentBindings, destination) ||
                    !participant.OriginFacts.SameValues(MovementUndoOriginFacts.Capture(destination, input.State, input.EnvironmentBindings, input.Souls)))
                    return Fail(MovementUndoFailureReason.DestinationInvalid);
                if (!MovementRouteValidator.CanOccupy(input, destination, participants))
                    return Fail(MovementUndoFailureReason.DestinationOccupied);
            }
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            if (!RestoreEffects(input, checkpoint.Effects, projected, out var environment, out var removed))
                return Fail(MovementUndoFailureReason.ReturnWouldCauseEffect);
            foreach (var participant in checkpoint.Participants)
            {
                var restoration = participant.Restoration;
                var current = projected[participant.EntityId];
                var restored = current.WithPlacement(current.Area, current.ReserveSlotIndex, restoration.To, restoration.Rotation, new[] { restoration.To });
                if (restoration.RestoresActionUse)
                    restored = restored.With(actionUse: UnitActionUse.Available, stageRecord: current.StageRecord.UndoMovement(restoration.DistanceReverted));
                var normalized = PositionTileModifierCalculator.Replace(restored, PositionTileModifierCalculator.ResolveAt(restoration.To, environment));
                if (!normalized.SameValues(restored)) return Fail(MovementUndoFailureReason.ReturnWouldCauseEffect);
                projected[participant.EntityId] = restored;
            }
            if (!TraitPresenceReconciler.NormalizeMovementUndo(input, projected, checkpoint.Effects))
                return Fail(MovementUndoFailureReason.ReturnWouldCauseEffect);
            var events = checkpoint.Participants.Select(p => (CombatEvent)new CombatCharacterEvent(CombatEventKind.Returned,
                p.ExpectedAfterMove, projected[p.EntityId])).ToList();
            foreach (var current in input.Characters.Where(c => !participants.Contains(c.EntityId)))
                CombatCommandEvents.TraitState(events, current, projected[current.EntityId]);
            TraitTrailCalculator.RecordChanges(input.State.Environment, environment, events);
            var undoInput = input.Kind == CombatCommandKind.UndoMove ? input :
                input.WithIntent(CombatCommandKind.UndoMove, default, default, null, null);
            var result = new MovementUndoResult(actor.EntityId, checkpoint.MoveCommandNumber,
                checkpoint.Participants.Select(p => p.Restoration).ToArray(), removed);
            candidate = new CombatCommandTransition(undoInput, true, "", new CharacterChanges(input.Characters
                .Where(c => !c.SameValues(projected[c.EntityId])).Select(c => new CharacterStateChange(c, projected[c.EntityId])).ToArray(),
                Array.Empty<SoulCountChange>()), new SoulFieldChange(input.Souls, input.Souls), input.State.With(environment: environment),
                new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None), events: events,
                actionUseSettlement: new UnitActionUseSettlementSnapshot(actor.EntityId, UnitActionUse.Moved, UnitActionUse.Available), undoMovement: result);
            return new MovementUndoAvailability(MovementUndoFailureReason.None, checkpoint.MoveCommandNumber, checkpoint.Anchor);
        }

        internal static bool GuardsMatch(CombatCommandInput current, MovementUndoCheckpoint checkpoint) =>
            checkpoint.Effects.All(effect => effect is MovementTrailReceipt trail
                ? current.State.Environment.Tiles.Any(t => trail.CreatedSource.SameValues(t))
                : effect.MatchesGuard(current.Characters.SingleOrDefault(c => c.EntityId == effect.RecipientId)));

        internal static IReadOnlyList<MovementEffectReceipt> Capture(CombatCommandInput input, CombatCommandInput final)
        {
            var movement = new MovementEffectId(input.BattleInstanceId, input.State.Turn.ControllerTurnSequence, input.ActorId, input.Number);
            var before = input.Characters.Single(c => c.EntityId == input.ActorId);
            var after = final.Characters.Single(c => c.EntityId == input.ActorId);
            var effects = ActorEffects(movement, before, after).ToList();
            effects.AddRange(TraitPresenceReconciler.CaptureMovementEffects(movement, input.Characters, final.Characters));
            foreach (var character in input.Characters)
                effects.AddRange(ExposureEffects(movement, character, final.Characters.Single(c => c.EntityId == character.EntityId)));
            var oldIds = input.State.Environment.Tiles.Select(t => t.Id).ToHashSet();
            foreach (var source in final.State.Environment.Tiles.Where(t => !oldIds.Contains(t.Id)))
            {
                if (source is not TraitTrailFact trail || trail.Origin.OwnerInstanceId != input.ActorId.Value ||
                    trail.CreatingSide != Owner.Player || !trail.Active) return null;
                effects.Add(new MovementTrailReceipt(movement, trail));
            }
            return effects;
        }

        internal static bool RestoreEffects(CombatCommandInput input, IReadOnlyList<MovementEffectReceipt> effects,
            Dictionary<EntityId, CharacterCombatFacts> characters, out MovementEnvironmentFacts environment,
            out IReadOnlyList<MovementEffectReceipt> removed)
        {
            environment = input.State.Environment;
            var applied = new List<MovementEffectReceipt>();
            removed = applied;
            foreach (var effect in effects)
            {
                characters.TryGetValue(effect.RecipientId, out var current);
                if (!effect.MatchesGuard(current)) return false;
                switch (effect)
                {
                    case MovementExposureReceipt exposure:
                        if (current == null || !current.StatusEffects.Entries.Any(exposure.ExpectedStatus.SameValues)) continue;
                        characters[current.EntityId] = current.WithStatusEffects(WeaknessExposureRules.RemoveMovement(current.StatusEffects, exposure.Movement));
                        break;
                    case MovementAccelerationReceipt gain:
                        characters[current.EntityId] = current.With(stats: AccelerationTraitRule.UndoGain(current.Stats, gain.AddedAmount));
                        break;
                    case MovementStationaryReceipt flag:
                        characters[current.EntityId] = current.With(stats: StationaryFireTraitRule.RestoreMoved(current.Stats, flag.PreviousMoved));
                        break;
                    case MovementShieldReceipt shield:
                        var restored = current.WithStatusEffects(ShieldStatusRules.Restore(current.StatusEffects, shield.Origin, shield.PreviousStatus));
                        if (shield.PreviousDefenseBonus.HasValue)
                            restored = restored.With(traitEffects: new CharacterTraitEffects(restored.TraitEffects.AttackGrants,
                                restored.TraitEffects.Shields, shield.PreviousDefenseBonus.Value));
                        characters[current.EntityId] = restored;
                        break;
                    case MovementTrailReceipt trail:
                        var source = environment.Tiles.SingleOrDefault(t => t.Id == trail.CreatedSource.Id);
                        if (!trail.CreatedSource.SameValues(source)) return false;
                        environment = new MovementEnvironmentFacts(environment.Tiles.Select(t => t.Id == source.Id
                            ? trail.CreatedSource.WithRemainingOpposingTurnEnds(0) : t).ToArray());
                        break;
                    default:
                        return false;
                }
                applied.Add(effect);
            }
            return true;
        }

        internal static bool ForwardCharactersMatch(CombatCommandInput input, CombatCommandInput final,
            IReadOnlyList<MovementEffectReceipt> effects, int recordedDistance, CharacterCombatFacts passengerBefore = null)
        {
            var projected = final.Characters.ToDictionary(c => c.EntityId);
            if (!RestoreEffects(final, effects, projected, out _, out _)) return false;
            var before = input.Characters.Single(c => c.EntityId == input.ActorId);
            var actor = projected[input.ActorId];
            projected[input.ActorId] = actor.WithPlacement(before.Area, before.ReserveSlotIndex, before.Anchor, before.Rotation, before.OccupiedCells)
                .With(actionUse: before.ActionUse, stageRecord: actor.StageRecord.UndoMovement(recordedDistance));
            if (passengerBefore != null)
            {
                var passenger = projected[passengerBefore.EntityId];
                projected[passenger.EntityId] = passenger.WithPlacement(passenger.Area, passenger.ReserveSlotIndex,
                    passengerBefore.Anchor, passengerBefore.Rotation, passengerBefore.OccupiedCells);
            }
            return TraitPresenceReconciler.Calculate(final, final.Characters).Characters.Count == 0 &&
                TraitPresenceReconciler.NormalizeMovementUndo(final, projected, effects) &&
                input.Characters.Count == projected.Count && input.Characters.All(c => c.SameValues(projected[c.EntityId]));
        }

        internal static bool ForwardEnvironmentMatches(CombatCommandInput input, CombatCommandInput final, IReadOnlyList<MovementEffectReceipt> effects)
        {
            var created = effects.OfType<MovementTrailReceipt>().Select(e => e.CreatedSource).OrderBy(t => t.Id).ToArray();
            var expected = new MovementEnvironmentFacts(input.State.Environment.Tiles.Concat(created).ToArray());
            var progress = input.State.EnvironmentProgress;
            return final.State.Environment.SameValues(expected) &&
                created.Select(t => t.Id).SequenceEqual(Enumerable.Range(0, created.Length).Select(i => checked(progress.NextSourceId + i))) &&
                final.State.EnvironmentProgress.SameValues(new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal,
                    checked(progress.NextSourceId + created.Length), progress.RandomState)) &&
                input.State.SameGameplayValues(final.State.With(environment: input.State.Environment, environmentProgress: progress));
        }

        internal static bool ForwardEventsSupported(CombatCommandTransition transition)
        {
            var actor = transition.Input.ActorId;
            foreach (var e in transition.Events)
            {
                if (e is CombatCharacterEvent move && move.Kind == CombatEventKind.Moved && move.Final.EntityId == actor &&
                    move.MovementCause?.Agency != CombatMovementAgency.Forced &&
                    move.Expected.WithPlacement(move.Final.Area, move.Final.ReserveSlotIndex, move.Final.Anchor, move.Final.Rotation, move.Final.OccupiedCells).SameValues(move.Final)) continue;
                if (e is CombatCharacterEvent displaced && displaced.Kind == CombatEventKind.Moved &&
                    displaced.MovementCause?.Agency == CombatMovementAgency.Forced && displaced.MovementCause.TraitKey == TraitKey.PositionSwap &&
                    displaced.Expected.WithPlacement(displaced.Final.Area, displaced.Final.ReserveSlotIndex,
                        displaced.Final.Anchor, displaced.Final.Rotation, displaced.Final.OccupiedCells).SameValues(displaced.Final)) continue;
                if (e is CombatMovementSourceEvent source && source.Expected == null && source.Final is TraitTrailFact trail &&
                    trail.Origin.OwnerInstanceId == actor.Value) continue;
                if (e is CombatTraitStateEvent state && SupportsCharacterChange(transition.Input, state.Expected, state.Final)) continue;
                if (e is CombatEffectsEvent effects && effects.Categories.Count == 0 && effects.Traits.All(t =>
                    t.TraitKey == TraitKey.Acceleration && t.TraitActivationKind == TraitActivationKind.AttackGranted ||
                    (t.TraitKey == TraitKey.GroundCompaction || t.TraitKey == TraitKey.DustScreen) && t.TraitActivationKind == TraitActivationKind.TrailCreated ||
                    t.TraitKey == TraitKey.GroundCompaction && t.TraitActivationKind == TraitActivationKind.RangeChanged ||
                    (t.TraitKey == TraitKey.Leap || t.TraitKey == TraitKey.PositionSwap) && t.TraitActivationKind == TraitActivationKind.MovementChanged ||
                    t.TraitKey == TraitKey.PositionSwap && t.TraitActivationKind == TraitActivationKind.ShieldGranted ||
                    t.TraitKey == TraitKey.StationaryFire && t.TraitActivationKind == TraitActivationKind.StatusApplied ||
                    t.TraitKey == TraitKey.ReconSquad && t.TraitActivationKind == TraitActivationKind.StatusApplied ||
                    t.TraitKey == TraitKey.Taunt && t.TraitActivationKind is TraitActivationKind.StatusApplied or TraitActivationKind.StatusRemoved ||
                    (t.TraitKey == TraitKey.Cohesion || t.TraitKey == TraitKey.SoloAction) &&
                        t.TraitActivationKind is TraitActivationKind.ShieldGranted or TraitActivationKind.ShieldRemoved or TraitActivationKind.DefenseChanged)) continue;
                return false;
            }
            return true;
        }

        private static IEnumerable<MovementEffectReceipt> ActorEffects(MovementEffectId movement, CharacterCombatFacts before, CharacterCombatFacts after)
        {
            if (before.EntityId != movement.ActorId) yield break;
            int gain = AccelerationTraitRule.Held(after.Stats) - AccelerationTraitRule.Held(before.Stats);
            if (gain > 0) yield return new MovementAccelerationReceipt(movement,
                new EffectOriginKey(EffectOwnerKind.Trait, movement.ActorId.Value, (int)TraitKey.Acceleration, "main"),
                gain, AccelerationTraitRule.Held(after.Stats));
            bool oldFlag = before.Stats.TraitStatuses.ToArray().OfType<StationaryFireTraitStatusSnapshot>().SingleOrDefault()?.MovedThisOwnTurn ?? false;
            bool nextFlag = after.Stats.TraitStatuses.ToArray().OfType<StationaryFireTraitStatusSnapshot>().SingleOrDefault()?.MovedThisOwnTurn ?? false;
            if (!oldFlag && nextFlag) yield return new MovementStationaryReceipt(movement,
                new EffectOriginKey(EffectOwnerKind.Trait, movement.ActorId.Value, (int)TraitKey.StationaryFire, "main"), oldFlag, nextFlag);
        }

        private static bool SupportsCharacterChange(CombatCommandInput input, CharacterCombatFacts before, CharacterCombatFacts after)
        {
            var movement = new MovementEffectId(input.BattleInstanceId, input.State.Turn.ControllerTurnSequence, input.ActorId, input.Number);
            var effects = ActorEffects(movement, before, after).Concat(
                TraitPresenceReconciler.CaptureMovementEffects(movement, new[] { before }, new[] { after }))
                .Concat(ExposureEffects(movement, before, after)).ToArray();
            var projected = new Dictionary<EntityId, CharacterCombatFacts> { [after.EntityId] = after };
            return RestoreEffects(input, effects, projected, out _, out _) &&
                MovementUndoResult.SameExceptCurrentEligibility(projected[after.EntityId], before);
        }

        private static IEnumerable<MovementEffectReceipt> ExposureEffects(MovementEffectId movement, CharacterCombatFacts before, CharacterCombatFacts after) =>
            after.StatusEffects.Entries.OfType<WeaknessExposureStatus>().Where(s => s.Movement == movement &&
                s.Origin.Value.DefinitionKey == (int)TraitKey.ReconSquad && !before.StatusEffects.Entries.Any(s.SameValues))
                .Select(s => new MovementExposureReceipt(movement, after.EntityId, s));
    }
}
