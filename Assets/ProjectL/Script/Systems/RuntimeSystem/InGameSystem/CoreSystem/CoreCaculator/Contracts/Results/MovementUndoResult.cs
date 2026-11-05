using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public sealed class MovementUndoResult
    {
        public EntityId ActorId => Participants[0].EntityId;
        public int MoveCommandNumber { get; }
        public CellCoord From => Participants[0].From;
        public CellCoord To => Participants[0].To;
        public Rotation4 Rotation => Participants[0].Rotation;
        public int DistanceReverted => Participants[0].DistanceReverted;
        public IReadOnlyList<MovementParticipantRestoration> Participants { get; }
        public IReadOnlyList<MovementEffectReceipt> RemovedEffects { get; }

        public MovementUndoResult(EntityId actorId, int moveCommandNumber, CellCoord from, CellCoord to,
            Rotation4 rotation, int distanceReverted)
            : this(actorId, moveCommandNumber, new[] {
                new MovementParticipantRestoration(actorId, from, to, rotation, true, distanceReverted) },
                Array.Empty<MovementEffectReceipt>())
        {
        }

        public MovementUndoResult(EntityId actorId, int moveCommandNumber,
            IReadOnlyList<MovementParticipantRestoration> participants, IReadOnlyList<MovementEffectReceipt> removedEffects)
        {
            var people = (participants ?? throw new ArgumentNullException(nameof(participants))).ToArray();
            var effects = (removedEffects ?? throw new ArgumentNullException(nameof(removedEffects))).ToArray();
            if (actorId.IsEmpty || moveCommandNumber < 1 || people.Length < 1 || people.Length > 2 || people.Any(p => p == null) ||
                people.Select(p => p.EntityId).Distinct().Count() != people.Length || people.Count(p => p.EntityId == actorId) != 1 ||
                people.Any(p => p.RestoresActionUse != (p.EntityId == actorId)) ||
                people.Select(p => p.To).Distinct().Count() != people.Length || people.Select(p => p.From).Distinct().Count() != people.Length ||
                effects.Any(e => e == null || e.Movement.ActorId != actorId || e.Movement.MoveCommandNumber != moveCommandNumber) ||
                effects.Where((e, i) => effects.Take(i).Any(e.SameContribution)).Any())
                throw new ArgumentException("취소 결과의 참여자·이동 출처·복원 권한이 일치해야 합니다.");
            MoveCommandNumber = moveCommandNumber;
            Participants = Array.AsReadOnly(people.OrderBy(p => p.EntityId == actorId ? 0 : 1).ToArray());
            RemovedEffects = Array.AsReadOnly(effects);
        }

        /// <summary>취소 결과·정산·반환 사건의 같은 의미를 검사합니다. 토큰과 효과 역산 권한은 게시 경계에서 검사합니다.</summary>
        internal static void Validate(bool accepted, CombatCommandKind kind, EntityId actor,
            MovementUndoResult undo, UnitActionUseSettlementSnapshot settlement, IReadOnlyList<CombatEvent> events,
            MovementDestination? movement, MovementPathResult path, AttackResult? attack)
        {
            if (undo == null)
            {
                if (accepted && kind == CombatCommandKind.UndoMove) throw new ArgumentException("수락한 이동 취소에는 취소 결과가 필요합니다.");
                return;
            }
            if (!accepted || kind != CombatCommandKind.UndoMove || undo.ActorId != actor ||
                movement.HasValue || path != null || attack.HasValue || settlement == null || settlement.ActorId != actor ||
                settlement.BeforeActionUse != UnitActionUse.Moved || settlement.FinalActionUse != UnitActionUse.Available ||
                events.Count(IsReturned) != undo.Participants.Count ||
                undo.RemovedEffects.Count == 0 && events.Any(e => !IsReturned(e) && !IsRecomputedEligibility(e, undo)))
                throw new ArgumentException("취소 결과는 참여자의 반환 사건 및 행동 복원 정산과 일치해야 합니다.");
            var returns = events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Returned).ToArray();
            for (int i = 0; i < undo.Participants.Count; i++)
            {
                var participant = undo.Participants[i];
                if (returns[i] is not CombatCharacterEvent returned || returned.Kind != CombatEventKind.Returned ||
                    returned.Expected.EntityId != participant.EntityId || returned.Final.EntityId != participant.EntityId ||
                    returned.Expected.Anchor != participant.From || returned.Final.Anchor != participant.To ||
                    returned.Expected.Area != CharacterRuntimeArea.Field || returned.Final.Area != CharacterRuntimeArea.Field ||
                    returned.Expected.OccupiedCells.Count != 1 || returned.Final.OccupiedCells.Count != 1 ||
                    returned.Final.OccupiedCells[0] != participant.To || returned.Final.Rotation != participant.Rotation ||
                    participant.RestoresActionUse && returned.Expected.ActionUse != UnitActionUse.Moved ||
                    !participant.RestoresActionUse && returned.Expected.Rotation != participant.Rotation ||
                    returned.Expected.StageRecord.DistanceMoved < participant.DistanceReverted)
                    throw new ArgumentException("참여자의 반환 사건이 복원 계약과 다릅니다.");
                var expectedFinal = returned.Expected.WithPlacement(returned.Expected.Area, returned.Expected.ReserveSlotIndex,
                    participant.To, participant.Rotation, new[] { participant.To });
                if (participant.RestoresActionUse)
                    expectedFinal = expectedFinal.With(actionUse: UnitActionUse.Available,
                        stageRecord: returned.Expected.StageRecord.UndoMovement(participant.DistanceReverted));
                if (undo.RemovedEffects.Count == 0 && !expectedFinal.SameValues(returned.Final) ||
                    returned.Final.ActionUse != expectedFinal.ActionUse || !returned.Final.StageRecord.SameValues(expectedFinal.StageRecord))
                    throw new ArgumentException("이동 취소는 배치와 행위자의 행동·해당 이동 거리만 복원할 수 있습니다.");
            }
        }

        /// <summary>
        /// 수명이 <see cref="StatusEffectLifetimeKind.CurrentEligibility"/>인 상태 항목만 빼고 두 유닛 사실이 같은지 봅니다.
        /// 그 상태는 이전 값 없이 되돌린 자리에서 다시 계산되므로(<c>TraitPresenceReconciler.NormalizeMovementUndo</c>) 영수증이 없습니다.
        /// 복원이 실제로 맞는지는 체크포인트를 만들 때의 완전 일치 비교(<c>MovementEffectUndo.ForwardCharactersMatch</c>)와
        /// 게시 경계의 다시 계산 대조가 지키고, 이 함수는 사건·값 모양만 봅니다.
        /// </summary>
        internal static bool SameExceptCurrentEligibility(CharacterCombatFacts left, CharacterCombatFacts right) =>
            WithoutCurrentEligibility(left).SameValues(WithoutCurrentEligibility(right));

        private static CharacterCombatFacts WithoutCurrentEligibility(CharacterCombatFacts character)
        {
            var rest = character.StatusEffects.Entries.Where(s => s.Lifetime != StatusEffectLifetimeKind.CurrentEligibility).ToArray();
            return rest.Length == character.StatusEffects.Entries.Count ? character : character.WithStatusEffects(new StatusEffectSet(rest));
        }

        private static bool IsReturned(CombatEvent e) => e is CombatCharacterEvent returned && returned.Kind == CombatEventKind.Returned;

        /// <summary>
        /// 영수증 없는 되돌림에 반환 사건 말고 붙을 수 있는 사건입니다: 참여자가 아닌 유닛의, 다시 계산되는 상태만 바뀐 상태 사건.
        /// 예를 들어 도발 보유자가 원래 자리로 돌아가면 범위에 들어오거나 나간 적의 도발당함이 다시 계산됩니다.
        /// </summary>
        private static bool IsRecomputedEligibility(CombatEvent e, MovementUndoResult undo) =>
            e is CombatTraitStateEvent state && undo.Participants.All(p => p.EntityId != state.Final.EntityId) &&
            SameExceptCurrentEligibility(state.Expected, state.Final);
    }
}
