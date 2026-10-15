using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>실제 턴 사실을 입력으로 받아 다음 턴·밤 정산과 조회값을 계산한다.</summary>
    public static class CombatTurnCalculator
    {
        internal static CombatCommandTransition EnterCombat(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input.State.Phase == CombatFlowPhase.Finished) return CombatCommandCalculator.Reject(input, "StageEnded");
            if (input.State.Turn.StagePhase != StagePhase.Deployment) return CombatCommandCalculator.Reject(input, "WrongPhase");
            var main = input.Characters.Single(c => c.OwnershipRole == Contracts.Roster.CharacterOwnershipRole.Main);
            if (main.Health.IsDead || main.Area != CharacterRuntimeArea.Field) return CombatCommandCalculator.Reject(input, "InvalidMainCharacter");
            if (input.State.Structures.Count(s => s.Owner == Owner.Enemy && s.Definition.Key == EntityKey.Structure_Headquarters && !s.Health.IsDead) != 1)
                return CombatCommandCalculator.Reject(input, "InvalidEnemyHeadquarters");
            RequiredBattle(input);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var projection = MovementProjection.ForArrival(input, projected, events);
            if (!CombatPlacementCalculator.RedeployEnemies(input, projected, projection, rules)) return CombatCommandCalculator.Reject(input, "EnemyRedeploymentBlocked");
            if (projection.Terminal) return projection.Complete();
            var participants = new List<CombatTurnParticipant>();
            foreach (var actor in projected.Values.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).ToArray())
            {
                if (actor.OccupiedCells.Count != 1) return CombatCommandCalculator.Reject(input, "DeferredMultiCellUnit");
                participants.Add(new CombatTurnParticipant(actor.EntityId, HeldEnvironmentEffects.EffectivePriority(actor), actor.Stats.Delay));
                projected[actor.EntityId] = ResetMovement(actor, actor.Stats, 0);
            }
            var old = input.State.Turn;
            var turn = new CombatTurnFacts(StagePhase.Combat, old.PreparationIndex, true,
                completedUnitTurnCount: old.CompletedUnitTurnCount,
                participants: participants, enemyOrigins: old.EnemyOrigins, gimmicks: InitialGimmicks(input));
            events.Add(new CombatTurnEvent(CombatEventKind.CombatEntered, EntityId.Empty, old, turn));
            return projection.Complete(state: State(projection.CurrentInput().State, turn, CombatFlowPhase.Automatic, isNight: true));
        }

        internal static CombatCommandTransition EndTurn(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            var reason = CombatMovementCalculator.Actor(input, out var actor, requireMovement: false);
            if (reason != MovementFailureReason.None) return CombatCommandCalculator.Reject(input, reason.ToString());
            var battle = RequiredBattle(input);
            var before = input.State.Turn;
            if (before.StagePhase != StagePhase.Combat) return CombatCommandCalculator.Reject(input, "WrongPhase");
            var participant = before.Participants.Single(p => p.EntityId == actor.EntityId);
            var engine = new CombatEffectEngine();
            var effect = engine.PlanTurnEnded(CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor)));
            var fields = input.State.ThornFields.ToDictionary(f => f.Cell);
            var settled = CombatMovementCalculator.SealPendingMovement(actor);
            settled = CombatMovementCalculator.CopyMovement(settled, settled.Stats, settled.Anchor,
                settled.CommittedMoveDistance, 0, 0, 0, settled.MovementCheckpointAnchor);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            projected[actor.EntityId] = settled;
            var counts = input.SoulCounts.ToDictionary(p => p.Key, p => p.Value);
            var souls = new SoulEffectCalculator(input.Artifacts);
            var soulField = input.Souls;
            var consumed = new List<long>();
            if (!settled.Health.IsDead)
            {
                projected[actor.EntityId] = CombatCommandCalculator.Copy(settled, settled.Health,
                    CombatCommandEffects.With(CombatCommandEffects.Apply(settled.Stats, effect), ap: settled.Stats.MaxActionCost));
                CombatCommandEvents.Effects(events, effect);
            }
            var participants = before.Participants.Select(p => p.EntityId == actor.EntityId
                ? new CombatTurnParticipant(p.EntityId, p.Priority, p.Delay,
                    checked(before.CurrentTime + CombatTimelineScheduler.CalculateActionInterval(p.Delay, p.Priority)), false,
                    checked(p.CompletedTurnCount + 1)) : p).ToArray();
            long nightTime = checked(before.CurrentNightTime + actor.Stats.Delay);
            bool endsNight = nightTime >= battle.NightLength;
            var turn = new CombatTurnFacts(endsNight ? StagePhase.Deployment : StagePhase.Combat,
                checked(before.PreparationIndex + (endsNight ? 1 : 0)), before.MainPlacementLocked,
                before.CurrentTime, nightTime, checked(before.CompletedUnitTurnCount + 1),
                endsNight ? Array.Empty<CombatTurnParticipant>() : participants, before.EnemyOrigins, before.Gimmicks);
            fields = endsNight ? new Dictionary<CellCoord, ThornFieldCellSnapshot>() : fields.Values.Where(f => f.RemainingUnitTurns > 1)
                .ToDictionary(f => f.Cell, f => new ThornFieldCellSnapshot(f.Cell, f.RemainingUnitTurns - 1, f.ThornDamage));
            events.Add(new CombatTurnEvent(CombatEventKind.TurnEnded, actor.EntityId, before, turn));
            var projection = new MovementProjection(input, State(input.State, turn, CombatFlowPhase.Automatic),
                projected, input.State.Structures.ToDictionary(s => s.EntityId), fields, counts, soulField, consumed,
                events, new Dictionary<EntityId, CellCoord>(), souls);
            TornadoCalculator.ResolveDue(projection, rules);
            if (projection.Terminal) return projection.Complete(countdown: true);
            if (endsNight)
            {
                foreach (var c in projected.Values.OrderBy(c => c.EntityId).ToArray())
                {
                    var expired = HeldEnvironmentEffects.ExpireSupplies(c);
                    if (ReferenceEquals(c, expired)) continue;
                    projected[c.EntityId] = expired;
                    events.Add(new CombatHeldEnvironmentEvent(c, expired));
                }
                var beforeRecall = projected.ToDictionary(p => p.Key, p => p.Value);
                CombatPlacementCalculator.RecallForNight(input, projected);
                events.Add(new CombatTurnEvent(CombatEventKind.NightEnded, EntityId.Empty, before, turn));
                foreach (var c in projected.Values.OrderBy(c => c.EntityId))
                    if (beforeRecall[c.EntityId].Area != c.Area)
                    {
                        PositionTileModifierCalculator.RecordNormalization(events, beforeRecall[c.EntityId], c);
                        events.Add(new CombatCharacterEvent(CombatEventKind.Returned, beforeRecall[c.EntityId], c));
                    }
            }
            var finalState = State(projection.CurrentInput().State, turn, endsNight ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic,
                fields: fields.Values.ToArray(), isNight: !endsNight);
            return projection.Complete(state: finalState, countdown: true);
        }

        internal static CombatCommandTransition BeginNext(CombatCommandInput input)
        {
            var before = input.State.Turn;
            if (before.StagePhase != StagePhase.Combat || !input.State.ActiveActorId.IsEmpty)
                throw new InvalidOperationException("다음 턴을 시작할 상태가 아닙니다.");
            var battle = RequiredBattle(input);
            var participant = Ordered(before.Participants, battle.TimelineSeed).FirstOrDefault();
            if (participant.EntityId.IsEmpty) throw new InvalidOperationException("진행할 타임라인 참가자가 없습니다.");
            var actor = input.Characters.Single(c => c.EntityId == participant.EntityId && c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead);
            var effect = new CombatEffectEngine().PlanTurnStarted(CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor)));
            var stats = CombatCommandEffects.With(CombatCommandEffects.Apply(actor.Stats, effect),
                ap: actor.Stats.MaxActionCost, remainingMoveRange: actor.Stats.MoveRange);
            var final = ResetMovement(actor, stats, 0);
            var turn = new CombatTurnFacts(StagePhase.Combat, before.PreparationIndex, before.MainPlacementLocked,
                Math.Max(before.CurrentTime, participant.NextActionTime), before.CurrentNightTime, before.CompletedUnitTurnCount,
                before.Participants, before.EnemyOrigins, before.Gimmicks);
            var state = State(input.State, turn, actor.Owner == Owner.Player ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic,
                actor.EntityId, isNight: true);
            var events = new List<CombatEvent> { new CombatTurnEvent(CombatEventKind.TurnStarted, actor.EntityId, before, turn) };
            CombatCommandEvents.Effects(events, effect);
            return Changed(input, new Dictionary<EntityId, CharacterCombatFacts> { [actor.EntityId] = final }, state, events);
        }

        public static CombatTimelineEntrySnapshot[] Forecast(CombatCommandInput input, int count = 10)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (input.State.Phase == CombatFlowPhase.Finished || input.State.Turn.StagePhase != StagePhase.Combat) return Array.Empty<CombatTimelineEntrySnapshot>();
            var battle = RequiredBattle(input);
            var participants = input.State.Turn.Participants.ToList();
            var gimmicks = input.State.Turn.Gimmicks.ToArray();
            var tornados = input.State.Environment.Tiles.OfType<TornadoSourceFact>()
                .Where(t => t.Active && t.KnownTo.Contains(Owner.Player)).OrderBy(t => t.Id).ToArray();
            var result = new List<CombatTimelineEntrySnapshot>();
            long time = input.State.Turn.CurrentTime, night = input.State.Turn.CurrentNightTime;
            long initialCompleted = input.State.Turn.CompletedUnitTurnCount, completed = initialCompleted;
            EntityId active = input.State.ActiveActorId;
            if (active.IsEmpty) { AppendDueTornados(); AppendDueGimmicks(); }
            while (result.Count < count && participants.Count != 0)
            {
                var next = !active.IsEmpty ? participants.Single(p => p.EntityId == active) : Ordered(participants, battle.TimelineSeed).First();
                time = Math.Max(time, next.NextActionTime);
                night = checked(night + next.Delay);
                result.Add(CombatTimelineEntrySnapshot.ForUnit(next.EntityId, next.Priority, time,
                    next.EntityId == active, night, night >= battle.NightLength));
                completed = checked(completed + 1);
                AppendDueTornados();
                if (night >= battle.NightLength)
                {
                    if (result.Count < count) result.Add(CombatTimelineEntrySnapshot.ForNightEnd(time, battle.NightLength));
                    break;
                }
                int index = participants.FindIndex(p => p.EntityId == next.EntityId);
                participants[index] = new CombatTurnParticipant(next.EntityId, next.Priority, next.Delay,
                    checked(time + CombatTimelineScheduler.CalculateActionInterval(next.Delay, next.Priority)), false,
                    checked(next.CompletedTurnCount + 1));
                active = EntityId.Empty;
                AppendDueGimmicks();
            }
            return result.ToArray();

            void AppendDueTornados()
            {
                for (int index = 0; index < tornados.Length && result.Count < count; index++)
                    while (TornadoCalculator.IsDue(completed, tornados[index].NextDueCompletedTurn) && result.Count < count)
                    {
                        var due = tornados[index];
                        result.Add(CombatTimelineEntrySnapshot.ForTornado(due.Id, time,
                            Math.Max(0, due.NextDueCompletedTurn - initialCompleted)));
                        // 예측은 다음 시점만 진행하며 목적지 선택이나 실제 난수 상태에 접근하지 않는다.
                        tornados[index] = due.AfterActivation(due.Cell);
                    }
            }

            void AppendDueGimmicks()
            {
                while (result.Count < count)
                {
                    var due = gimmicks.Where(g => g.NextActivationTurn <= completed).OrderBy(g => g.Id.Value).FirstOrDefault();
                    if (due.Id.IsEmpty) break;
                    if (battle.GimmickDefinitions[due.Id.Value - 1] is not Contracts.Commands.StageGimmicks.RailwayGimmickSpec definition)
                        throw new InvalidOperationException("예측할 기믹 정의가 없습니다.");
                    result.Add(CombatTimelineEntrySnapshot.ForStageGimmick(due.Id, StageGimmickKey.Railway,
                        time, Math.Max(0, due.NextActivationTurn - initialCompleted)));
                    int index = Array.FindIndex(gimmicks, g => g.Id == due.Id);
                    gimmicks[index] = new CombatGimmickFacts(due.Id, checked(due.NextActivationTurn + definition.RepeatIntervalUnitTurns), due.ActivationCount);
                }
            }
        }

        private static IEnumerable<CombatTurnParticipant> Ordered(IEnumerable<CombatTurnParticipant> values, int seed) =>
            values.OrderBy(p => p.OpeningTurnPending ? 0 : 1).ThenBy(p => p.OpeningTurnPending ? -(long)p.Priority : p.NextActionTime)
                .ThenBy(p => CombatTimelineScheduler.CalculateTieKey(seed, p.EntityId, p.CompletedTurnCount)).ThenBy(p => p.EntityId.Value);
        internal static CombatBattleFacts RequiredBattle(CombatCommandInput input) => input.Battle ??
            throw new InvalidOperationException("전투 설정·드로우 Buffer 사실이 필요합니다.");
        internal static CombatCommandState State(CombatCommandState source, CombatTurnFacts turn, CombatFlowPhase phase,
            EntityId active = default, ThornFieldCellSnapshot[] fields = null, bool? isNight = null) =>
            new(source.BoardWidth, source.BoardHeight, phase, active, isNight ?? source.IsNight, source.Structures,
                fields ?? source.ThornFields.ToArray(), turn.Participants.Select(p => CombatTimelineEntrySnapshot.ForUnit(p.EntityId,
                    p.Priority, p.NextActionTime, p.EntityId == active)).ToArray(), turn, source.Environment,
                source.EnvironmentProgress, source.WallBlocking);
        internal static CharacterCombatFacts ResetMovement(CharacterCombatFacts c, UnitStatsSnapshot stats, int committed) =>
            new(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole, c.Area, c.Health, stats,
                c.ReserveSlotIndex, c.Anchor, c.Rotation, c.OccupiedCells, c.Anchor ?? c.MovementCheckpointAnchor,
                committed, 0, c.TemporaryAttackBonus, c.TemporaryMaxHealthBonus,
                0, 0, c.PositionModifier, c.HeldEnvironment);
        internal static CombatCommandTransition Changed(CombatCommandInput input,
            IReadOnlyDictionary<EntityId, CharacterCombatFacts> projected, CombatCommandState state, IReadOnlyList<CombatEvent> events = null)
        {
            var main = projected.Values.SingleOrDefault(c => c.OwnershipRole == Contracts.Roster.CharacterOwnershipRole.Main);
            return new(input, true, "", new CharacterChanges(input.Characters.Where(c => projected.ContainsKey(c.EntityId) &&
                !ReferenceEquals(c, projected[c.EntityId])).Select(c => new CharacterStateChange(c, projected[c.EntityId])).ToArray(),
                Array.Empty<Contracts.Roster.SoulCountChange>(), main == null ? null : PositionTileModifierCalculator.MainHealthChange(input, main)),
                new SoulFieldChange(input.Souls, input.Souls), state,
                new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None), events: events);
        }
        internal static CombatGimmickFacts[] InitialGimmicks(CombatCommandInput input) => RequiredBattle(input).GimmickDefinitions
            .Select((g, index) => g is Contracts.Commands.StageGimmicks.RailwayGimmickSpec r
                ? new CombatGimmickFacts(new StageGimmickId(index + 1), checked(input.State.Turn.CompletedUnitTurnCount + r.FirstActivationAfterUnitTurns))
                : throw new InvalidOperationException("지원되지 않는 기믹 정의입니다.")).ToArray();
    }
}
