using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    public static class MovementUndoPolicy
    {
        /// <summary>기존 역산 소유자가 만든 후보와 게시 사실을 비교합니다. Buffer에 별도 효과 산식을 만들지 않습니다.</summary>
        public static void ValidatePublication(CombatCommandTransition transition, IReadOnlyList<CharacterCombatFacts> published)
        {
            PositionSwapShields.ValidateMove(transition, published);
            var reconciled = Reconcile(transition);
            if (!reconciled.FinalState.MovementUndo.SameValues(transition.FinalState.MovementUndo) ||
                !SameUses(reconciled.MovementEffectUses, transition.MovementEffectUses))
                throw new InvalidOperationException("이동 취소 기록이 명령의 확정 사실과 다릅니다.");
            if (!transition.Accepted || transition.Input.Kind != CombatCommandKind.UndoMove) return;
            var input = transition.Input;
            var result = transition.UndoMovement;
            if (!input.State.MovementUndo.TryGet(input.ActorId, out var checkpoint) ||
                input.ExpectedMoveCommandNumber != checkpoint.MoveCommandNumber ||
                !MovementEffectUndo.TryApply(input, checkpoint, out var expected))
                throw new InvalidOperationException("이동 취소 원본이나 제한된 역산 후보가 유효하지 않습니다.");
            var expectedFinal = CombatCommandComposition.Project(expected);
            if (result == null || result.MoveCommandNumber != checkpoint.MoveCommandNumber ||
                result.Participants.Count != expected.UndoMovement.Participants.Count ||
                !result.Participants.Zip(expected.UndoMovement.Participants, (a, b) => a.SameValues(b)).All(same => same) ||
                result.RemovedEffects.Count != expected.UndoMovement.RemovedEffects.Count ||
                !result.RemovedEffects.Zip(expected.UndoMovement.RemovedEffects, (a, b) => a.SameValues(b)).All(same => same) ||
                published.Count != expectedFinal.Characters.Count || !published.All(c => expectedFinal.Characters.Any(c.SameValues)) ||
                transition.Characters.Created.Count != 0 || transition.Characters.Owned.Count != 0 || transition.Characters.Souls.Count != 0 ||
                transition.Characters.Main != null || !transition.FinalState.SameGameplayValues(expected.FinalState) ||
                !transition.Field.Final.SameValues(input.Souls) || transition.EnvironmentBindings != null || transition.InventoryChange != null ||
                transition.RewardChange != null || transition.RewardSettlement != null || transition.Completion != null || transition.ConsumedSoulIds.Count != 0 ||
                !SameUndoEvents(expected.Events, transition.Events))
                throw new InvalidOperationException("이동 취소 결과가 현재 원본에서 계산한 제한된 복원과 다릅니다.");
        }

        /// <summary>반응까지 끝난 한 명령의 사실에서 취소 메타데이터만 결정한다.</summary>
        public static CombatCommandTransition Reconcile(CombatCommandTransition resolved)
        {
            if (resolved == null) throw new ArgumentNullException(nameof(resolved));
            if (!resolved.Accepted) return resolved;
            using var sample = CoreProfiling.Sample(CoreProfileSection.MovementUndoReconcile);
            var input = resolved.Input;
            var final = CombatCommandComposition.Project(resolved);
            var evidence = CombatChangeEvidenceReader.ReadAll(resolved);
            var numericUses = CombatHealthExecution.CollectMovementUses(resolved.HealthExecutions);
            ValidateNumericUses(resolved, numericUses);
            var stream = resolved.Events.ToArray();
            var uses = CaptureParticipantUses(resolved).Concat(CaptureTrailActionUses(resolved)).Concat(numericUses)
                .OrderBy(u => Array.IndexOf(stream, u.Execution)).ToArray();
            // 실제 수신 계산의 선택 근거와 실행 사건을 함께 검사합니다. 선언만으로 다른 이동을 잠글 수 없습니다.
            if (resolved.MovementEffectUses.Any(u => !uses.Any(u.SameValues)))
                throw new InvalidOperationException("이동 효과 사용에 대응하는 확정 실행·출처 근거가 없습니다.");
            if (!SameUses(uses, resolved.MovementEffectUses)) resolved = resolved.WithMovementEffectUses(uses);
            var records = input.State.MovementUndo;
            foreach (var change in evidence) records = records.WithoutAffected(change);
            if (uses.Length != 0) records = records.WithoutAffected(new CombatChangeEvidence(CombatChangeEvidenceKind.Action,
                Array.Empty<EntityId>(), Array.Empty<CellCoord>(), uses.Where(u => u.Movement.HasValue).Select(u => u.Movement.Value).ToArray()));
            if (!InPlayerTurn(final) ||
                final.State.Turn.ControllerTurnSequence != input.State.Turn.ControllerTurnSequence)
                records = MovementUndoState.Empty;
            var retained = records.Checkpoints.Where(c => c.BattleInstanceId == input.BattleInstanceId &&
                c.ControllerTurnSequence == final.State.Turn.ControllerTurnSequence &&
                MovementEffectUndo.GuardsMatch(final, c) && c.Participants.All(p => final.Characters.Any(a => p.ExpectedAfterMove.SameValues(a)) &&
                    p.OriginFacts.SameValues(MovementUndoOriginFacts.Capture(p.Restoration.To, final.State, final.EnvironmentBindings, final.Souls)))).ToList();
            var created = CreateCheckpoint(resolved, final, evidence);
            if (created != null && !uses.Any(u => u.Movement == created.Movement ||
                u.SourceId.HasValue && created.Effects.OfType<MovementTrailReceipt>().Any(t => t.CreatedSource.Id == u.SourceId.Value))) retained.Add(created);
            var next = retained.Count == 0 ? MovementUndoState.Empty : new MovementUndoState(retained);
            if (next.SameValues(input.State.MovementUndo)) next = input.State.MovementUndo;
            return ReferenceEquals(resolved.FinalState.MovementUndo, next) ? resolved :
                resolved.WithFinalState(resolved.FinalState.With(movementUndo: next));
        }

        internal static bool InPlayerTurn(CombatCommandInput input) => input.State.Phase == CombatFlowPhase.PlayerInput &&
            input.State.Turn.StagePhase == StagePhase.Combat && input.State.Turn.ControllerPhase == CombatControllerPhase.Player;

        private static bool SameUses(IReadOnlyList<MovementEffectUse> a, IReadOnlyList<MovementEffectUse> b) =>
            a.Count == b.Count && a.Zip(b, (x, y) => x.SameValues(y)).All(same => same);

        private static IReadOnlyList<MovementEffectUse> CaptureParticipantUses(CombatCommandTransition transition)
        {
            var input = transition.Input;
            var uses = new List<MovementEffectUse>();
            foreach (var execution in transition.Events)
            {
                EntityId actor = execution switch
                {
                    CombatCharacterEvent moved when moved.Kind == CombatEventKind.Moved &&
                        moved.MovementCause?.Agency != CombatMovementAgency.Forced && moved.Expected.Anchor != moved.Final.Anchor => moved.Final.EntityId,
                    CombatAttackEvent attack when attack.Attack.Succeeded => attack.Attack.AttackerId,
                    CombatPlayerSkillAttackEvent skill => skill.Attack.ActorId,
                    CombatRoarEvent roar when roar.Attack.HasValue && roar.Attack.Value.Succeeded => roar.BossId,
                    _ => EntityId.Empty
                };
                if (actor.IsEmpty) continue;
                foreach (var checkpoint in input.State.MovementUndo.Checkpoints)
                {
                    if (checkpoint.BattleInstanceId != input.BattleInstanceId ||
                        checkpoint.ControllerTurnSequence != input.State.Turn.ControllerTurnSequence ||
                        !checkpoint.Participants.Any(p => p.EntityId == actor)) continue;
                    if (!input.Characters.Any(c => c.EntityId == actor && c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead))
                        throw new InvalidOperationException("실행한 참여자의 원본 사실이 없습니다.");
                    uses.Add(new MovementEffectUse(checkpoint.Movement, null, actor, MovementEffectUseKind.ParticipantAction, execution));
                }
            }
            return uses;
        }

        private static MovementUndoCheckpoint CreateCheckpoint(CombatCommandTransition transition, CombatCommandInput final,
            IReadOnlyList<CombatChangeEvidence> evidence)
        {
            var input = transition.Input;
            var path = transition.PathMovement;
            if (input.Kind != CombatCommandKind.Move || input.ProcessingKind != CombatProcessingKind.Command || !InPlayerTurn(input) || !InPlayerTurn(final) ||
                input.BattleInstanceId < 1 || input.State.Turn.ControllerTurnSequence < 1 || input.Path == null ||
                path == null || path.DistanceSpent < 1 || path.StopReason is not (MovementStopReason.Completed or MovementStopReason.NextStepInvalid) ||
                !path.RequestedPath.Cells.SequenceEqual(input.Path.Cells) ||
                !MovementEffectUndo.ForwardEventsSupported(transition)) return null;
            var before = input.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
            var after = final.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
            if (before == null || after == null || before.Owner != Owner.Player || before.Health.IsDead || after.Health.IsDead ||
                before.Area != CharacterRuntimeArea.Field || after.Area != CharacterRuntimeArea.Field || before.OccupiedCells.Count != 1 ||
                after.OccupiedCells.Count != 1 || before.Anchor != input.Path.Origin || !before.Rotation.HasValue ||
                after.Anchor != path.FinalStop || before.ActionUse != UnitActionUse.Available || after.ActionUse != UnitActionUse.Moved ||
                transition.ActionUseSettlement == null || transition.ActionUseSettlement.ActorId != before.EntityId ||
                transition.ActionUseSettlement.BeforeActionUse != UnitActionUse.Available || transition.ActionUseSettlement.FinalActionUse != UnitActionUse.Moved)
                return null;
            var swap = transition.Events.OfType<CombatCharacterEvent>().SingleOrDefault(e => e.Kind == CombatEventKind.Moved &&
                e.MovementCause?.Agency == CombatMovementAgency.Forced && e.MovementCause.TraitKey == TraitKey.PositionSwap);
            var passengerBefore = swap == null ? null : input.Characters.SingleOrDefault(c => c.EntityId == swap.Final.EntityId);
            if (swap != null && (!PositionSwapTraitRule.AllowsExchange(before, passengerBefore) ||
                passengerBefore.Anchor != input.Path.Endpoint || path.FinalStop != input.Path.Endpoint ||
                swap.Expected.Anchor != passengerBefore.Anchor || swap.Final.Anchor != before.Anchor ||
                swap.Expected.Rotation != swap.Final.Rotation || swap.Final.ActionUse != passengerBefore.ActionUse ||
                !swap.Final.StageRecord.SameValues(passengerBefore.StageRecord))) return null;
            int recorded = before.OwnedId.HasValue ? path.DistanceSpent : 0;
            var effects = MovementEffectUndo.Capture(input, final);
            if (effects == null || !after.StageRecord.SameValues(before.StageRecord.Add(distanceMoved: recorded)) ||
                !MovementEffectUndo.ForwardCharactersMatch(input, final, effects, recorded, passengerBefore)) return null;
            if (!MovementEffectUndo.ForwardEnvironmentMatches(input, final, effects) || !input.EnvironmentBindings.SameValues(final.EnvironmentBindings) ||
                !input.Souls.SameValues(final.Souls) || !input.Main.SameValues(final.Main) || !input.ItemInventory.SameValues(final.ItemInventory) ||
                input.GoldBalance != final.GoldBalance || input.FameBalance != final.FameBalance ||
                input.SoulCounts.Count != final.SoulCounts.Count || input.SoulCounts.Any(p => !final.SoulCounts.TryGetValue(p.Key, out var count) || p.Value != count) ||
                input.Owned.Count != final.Owned.Count || input.Owned.Any(o => !final.Owned.Any(n => o.SameValues(n))) ||
                input.Characters.Count != final.Characters.Count) return null;
            var replay = input.Characters.ToDictionary(c => c.EntityId);
            replay[before.EntityId] = before.With(actionUse: UnitActionUse.Moved);
            int steps = 0;
            foreach (var e in transition.Events)
            {
                CharacterCombatFacts expected = e is CombatCharacterEvent move ? move.Expected : (e as CombatTraitStateEvent)?.Expected;
                CharacterCombatFacts next = e is CombatCharacterEvent movement ? movement.Final : (e as CombatTraitStateEvent)?.Final;
                if (expected == null) continue;
                if (!replay.TryGetValue(expected.EntityId, out var prior) || !prior.SameValues(expected)) return null;
                if (e is CombatCharacterEvent step && step.Final.EntityId == before.EntityId)
                {
                    if (steps >= path.EnteredCells.Count || step.Expected.Anchor != input.Path.Cells[steps] ||
                        step.Final.Anchor != path.EnteredCells[steps]) return null;
                    steps++;
                }
                replay[next.EntityId] = next;
            }
            if (steps != path.DistanceSpent || path.FinalStop != path.EnteredCells.Last() ||
                !replay[before.EntityId].With(stageRecord: after.StageRecord).SameValues(after)) return null;
            var participants = new List<MovementUndoParticipant> {
                new MovementUndoParticipant(after, MovementUndoOriginFacts.Capture(before.Anchor.Value, input.State, input.EnvironmentBindings, input.Souls),
                    before.Rotation.Value, true, recorded) };
            if (passengerBefore != null)
                participants.Add(new MovementUndoParticipant(final.Characters.Single(c => c.EntityId == passengerBefore.EntityId),
                    MovementUndoOriginFacts.Capture(passengerBefore.Anchor.Value, input.State, input.EnvironmentBindings, input.Souls), passengerBefore.Rotation.Value));
            return new MovementUndoCheckpoint(new MovementEffectId(input.BattleInstanceId, input.State.Turn.ControllerTurnSequence,
                before.EntityId, input.Number), participants, effects);
        }

        private static bool SameUndoEvents(IReadOnlyList<CombatEvent> a, IReadOnlyList<CombatEvent> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                bool same = (a[i], b[i]) switch
                {
                    (CombatCharacterEvent x, CombatCharacterEvent y) => x.Kind == y.Kind && x.Expected.SameValues(y.Expected) && x.Final.SameValues(y.Final),
                    (CombatTraitStateEvent x, CombatTraitStateEvent y) => x.Expected.SameValues(y.Expected) && x.Final.SameValues(y.Final),
                    (CombatMovementSourceEvent x, CombatMovementSourceEvent y) => (x.Expected == null ? y.Expected == null : x.Expected.SameValues(y.Expected)) && x.Final.SameValues(y.Final),
                    (CombatEffectsEvent x, CombatEffectsEvent y) => x.Categories.Count == 0 && y.Categories.Count == 0 &&
                        x.Traits.Count == y.Traits.Count && x.Traits.Zip(y.Traits, (p, q) => p.SourceUnitId == q.SourceUnitId && p.TraitKey == q.TraitKey &&
                            p.TraitActivationKind == q.TraitActivationKind && p.Magnitude == q.Magnitude &&
                            p.TargetIds.SequenceEqual(q.TargetIds) && p.TargetCells.SequenceEqual(q.TargetCells)).All(value => value),
                    _ => false
                };
                if (!same) return false;
            }
            return true;
        }

        internal static MovementEffectId? TrailMovement(CombatCommandInput input, long sourceId) =>
            input.State.MovementUndo.Checkpoints.FirstOrDefault(c => c.Effects.OfType<MovementTrailReceipt>().Any(e => e.CreatedSource.Id == sourceId))?.Movement;

        private static IEnumerable<MovementEffectUse> CaptureTrailActionUses(CombatCommandTransition transition)
        {
            var input = transition.Input;
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var environment = input.State.Environment.Tiles.ToDictionary(t => t.Id);
            bool departed = false;
            foreach (var execution in transition.Events)
            {
                // 자동 진행 합성 뒤에도 같은 출발 근거를 얻도록 요청 종류·행위자 대신 실제 이동 사건을 읽습니다.
                if (!departed && execution is CombatCharacterEvent step &&
                    step.Kind == CombatEventKind.Moved && step.MovementCause?.Agency != CombatMovementAgency.Forced)
                {
                    departed = true;
                    var actor = step.Expected;
                    foreach (var trail in GroundCompactionTraitRule.SourcesAt(actor.Anchor.Value,
                        new MovementEnvironmentFacts(environment.Values.ToArray())))
                        yield return new MovementEffectUse(TrailMovement(input, trail.Id), trail.Origin, actor.EntityId,
                            MovementEffectUseKind.GroundDeparture, execution, sourceId: trail.Id);
                }
                var attack = execution is CombatAttackEvent attacked ? attacked.Attack :
                    execution is CombatRoarEvent roar ? roar.Attack : null;
                if (attack.HasValue && attack.Value.Succeeded && characters.TryGetValue(attack.Value.AttackerId, out var attacker) && attacker.Anchor.HasValue)
                    foreach (var trail in DustScreenTraitRule.RangeSourcesAt(attacker.Anchor.Value,
                        new MovementEnvironmentFacts(environment.Values.ToArray()), attacker.Owner))
                        yield return new MovementEffectUse(TrailMovement(input, trail.Id), trail.Origin, attacker.EntityId,
                            MovementEffectUseKind.DustAttackRange, execution, sourceId: trail.Id);
                if (execution is CombatCharacterEvent character) characters[character.Final.EntityId] = character.Final;
                else if (execution is CombatTraitStateEvent state) characters[state.Final.EntityId] = state.Final;
                else if (execution is CombatMovementSourceEvent source) environment[source.Final.Id] = source.Final;
            }
        }

        internal static bool MatchesDamage(CombatEvent execution, CombatDamageOrigin origin, EntityId recipient) => execution switch
        {
            CombatCharacterEvent character => character.Final.EntityId == recipient && character.DamageCause == CombatDamageCause.NumericDamage &&
                character.DamageOrigins.Any(o => SameOrigin(o, origin)),
            CombatTraitStateEvent state => state.Final.EntityId == recipient && SameOrigin(state.Origin, origin),
            CombatHeldEnvironmentEvent held => held.CharacterId == recipient && SameOrigin(held.Origin, origin),
            CombatEffectsEvent effects => effects.Traits.Any(t => t.TargetIds.Contains(recipient) && SameOrigin(t.ResultOrigin, origin) &&
                t.TraitActivationKind is TraitActivationKind.DamageModified or TraitActivationKind.ShieldAbsorbed),
            CombatItemEvent item => item.HolderId == recipient && item.Stage == ItemEventStage.BeforeDamage && item.DamageKind.HasValue,
            _ => false
        };

        private static bool SameOrigin(CombatDamageOrigin a, CombatDamageOrigin b) => a != null && b != null &&
            a.AttackGroup == b.AttackGroup && a.CreditedCharacterId == b.CreditedCharacterId && a.PhysicalCell == b.PhysicalCell;

        private static void ValidateNumericUses(CombatCommandTransition transition, IReadOnlyList<MovementEffectUse> uses)
        {
            var characters = transition.Input.Characters.ToDictionary(c => c.EntityId);
            var environment = transition.Input.State.Environment.Tiles.ToDictionary(t => t.Id);
            foreach (var execution in transition.Events)
            {
                foreach (var use in uses.Where(u => ReferenceEquals(u.Execution, execution)))
                {
                    if (use.NumericInput.GetValueOrDefault() <= 0 || use.DamageOrigin == null || !use.DamageKind.HasValue ||
                        use.ConsumerBefore == null || !characters.TryGetValue(use.ConsumerId, out var current) || current.Area != CharacterRuntimeArea.Field ||
                        use.ConsumerBefore.Area != CharacterRuntimeArea.Field || use.ConsumerBefore.Health.IsDead || use.ConsumerBefore.Owner != current.Owner ||
                        !use.Source.HasValue)
                        throw new InvalidOperationException("수치 효과 사용의 실행 입력이 현재 사건 문맥과 다릅니다.");
                    if (use.Kind == MovementEffectUseKind.ExposureDamage)
                    {
                        var selected = WeaknessExposureRules.Evaluate(current.StatusEffects).UsedSources.Where(s => s.Movement.HasValue).ToArray();
                        var related = uses.Where(u => ReferenceEquals(u.Execution, execution) && u.Kind == use.Kind &&
                            u.ConsumerId == use.ConsumerId && SameOrigin(u.DamageOrigin, use.DamageOrigin)).ToArray();
                        if (!MatchesDamage(execution, use.DamageOrigin, use.ConsumerId) || use.SourceId.HasValue ||
                            !selected.Any(s => s.Movement == use.Movement && s.Origin == use.Source && s.IncreasePercent == use.CapturedAmount) ||
                            selected.Length != related.Length || selected.Any(s => !related.Any(u => u.Movement == s.Movement &&
                                u.Source == s.Origin && u.CapturedAmount == s.IncreasePercent)))
                            throw new InvalidOperationException("노출 사용은 실제 수신자의 양수 동률 이동 기여분 모두와 일치해야 합니다.");
                    }
                    else if (use.Kind == MovementEffectUseKind.DustDefense)
                    {
                        // 끌어오기 같은 선행 이동의 중간 위치는 Core 피해 실행 근거가 포착한 실제 수신 사실을 사용합니다.
                        var selected = DustScreenTraitRule.DefenseSourcesAt(use.ConsumerBefore, new MovementEnvironmentFacts(environment.Values.ToArray()));
                        if (!CombatDamageKinds.IsAttack(use.DamageKind.Value) ||
                            !MatchesDamage(execution, use.DamageOrigin, use.ConsumerId) || !use.SourceId.HasValue ||
                            !selected.Any(t => t.Id == use.SourceId.Value && t.Origin == use.Source.Value) ||
                            use.Movement != TrailMovement(transition.Input, use.SourceId.Value) ||
                            !selected.Select(t => t.Id).OrderBy(id => id).SequenceEqual(uses.Where(u =>
                                ReferenceEquals(u.Execution, execution) && u.Kind == use.Kind && u.ConsumerId == use.ConsumerId &&
                                SameOrigin(u.DamageOrigin, use.DamageOrigin)).Select(u => u.SourceId.Value).OrderBy(id => id)))
                            throw new InvalidOperationException("연막 사용은 실제 수신자의 양수 동률 출처 모두와 일치해야 합니다.");
                    }
                    else if (use.Kind == MovementEffectUseKind.AccelerationDamage)
                    {
                        if (!use.Movement.HasValue || !transition.Input.State.MovementUndo.TryGet(use.ConsumerId, out var checkpoint) ||
                            checkpoint.Movement != use.Movement.Value || !checkpoint.Effects.Any(e => e is MovementAccelerationReceipt) ||
                            use.Source.Value.Kind != EffectOwnerKind.Trait || use.Source.Value.DefinitionKey != (int)TraitKey.Acceleration ||
                            use.Source.Value.OwnerInstanceId != use.ConsumerId.Value || use.DamageOrigin.CreditedCharacterId != use.ConsumerId ||
                            use.CapturedAmount <= 0 || use.CapturedAmount > AccelerationTraitRule.Held(use.ConsumerBefore.Stats) ||
                            use.CapturedAmount > AccelerationTraitRule.Held(current.Stats) ||
                            !characters.Keys.Any(id => MatchesDamage(execution, use.DamageOrigin, id)))
                            throw new InvalidOperationException("가속 사용은 실제 공격 범위가 포착한 보유 기여분이어야 합니다.");
                    }
                }
                if (execution is CombatCharacterEvent character) characters[character.Final.EntityId] = character.Final;
                else if (execution is CombatTraitStateEvent state) characters[state.Final.EntityId] = state.Final;
                else if (execution is CombatMovementSourceEvent source) environment[source.Final.Id] = source.Final;
            }
        }
    }
}
