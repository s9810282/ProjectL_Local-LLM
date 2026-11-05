using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    internal enum CombatActionAvailabilityState
    {
        Unknown,
        Pending,
        Ready
    }

    /// <summary>
    /// 선택 아군 하나의 Core 행동 가용성 요청과 캐시 수명입니다. 전투·진영 턴·결과·선택이 같은 응답만 보관합니다.
    /// 비용/행동 규칙을 판정하지 않고, 완료된 응답은 다른 기능이 슬롯을 사용해도 유지합니다.
    /// </summary>
    internal sealed class CombatActionAvailabilityTracker
    {
        private readonly ICombatViewBufferReadSink _input;
        private readonly ICombatViewBufferWriteSink _write;
        private bool _stopped;
        private bool _hasKey;
        private long _battleInstance;
        private long _turnSequence;
        private long _queryRevision;
        private int _resultSequence;
        private EntityId _actor;
        private int _requestNumber;
        private CombatActionAvailability _answer;

        internal CombatActionAvailabilityTracker(ICombatViewBufferReadSink input, ICombatViewBufferWriteSink write)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _write = write ?? throw new ArgumentNullException(nameof(write));
        }

        internal CombatActionAvailabilityState State { get; private set; }

        /// <summary>현재 키에 맞는 Ready만 돌려줍니다. 입력이 닫혔거나 아직 응답이 없으면 null입니다.</summary>
        internal CombatActionAvailability Read(in CombatInputFacts facts) =>
            !_stopped && State == CombatActionAvailabilityState.Ready && Matches(facts)
                && _answer.SourceRevision == facts.QueryRevision && CombatInputPolicy.CanQueryActionAvailability(facts) ? _answer : null;

        /// <summary>
        /// 키/슬롯을 맞추고 자기 답을 소비한 뒤, 전경이 없는 안정된 입력에서만 배경 조회를 게시합니다.
        /// 선점당한 Pending은 같은 키라도 다시 조회하고, Ready는 반복 조회하지 않습니다.
        /// </summary>
        internal void Sync(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters,
            bool hasForeground, int answeredPreviewNumber)
        {
            if (_stopped)
                return;
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));
            if (facts.QueryRevision <= 0 || !CombatInputPolicy.CanQueryActionAvailability(facts) || !HasSelectedActor(facts.SelectedActorId, characters))
            {
                Clear();
                return;
            }

            if (!Matches(facts))
            {
                Clear();
                _hasKey = true;
                _battleInstance = facts.BattleInstanceId;
                _turnSequence = facts.ControllerTurnSequence;
                _queryRevision = facts.QueryRevision;
                _resultSequence = facts.LatestResultSequence;
                _actor = facts.SelectedActorId;
            }

            if (_requestNumber != 0 && !OwnsSlot)
                DropPending();
            Consume(facts.PreviewAnswer);
            if (hasForeground)
            {
                // 첫 Drag·공격을 막지 않습니다. Ready는 남기고 응답 대기만 내려놓습니다.
                DropPending();
                return;
            }

            if (!CombatPreviewSlotRule.ShouldRequestBackground(
                State == CombatActionAvailabilityState.Ready, _requestNumber != 0,
                _input.HasPreview, _input.HasPreview ? _input.Preview.Number : 0, answeredPreviewNumber))
                return;

            _requestNumber = _write.SetPreview(CombatViewPreviewData.ActionAvailability(_actor));
            State = CombatActionAvailabilityState.Pending;
        }

        /// <summary>새 결과·선택 무효화·입력 닫힘의 정리입니다. 자기 요청 번호만 비웁니다.</summary>
        internal void Clear()
        {
            _hasKey = false;
            _answer = null;
            DropPending();
            State = CombatActionAvailabilityState.Unknown;
        }

        /// <summary>연결 입력 정지 뒤에는 다시 조회하지 않습니다.</summary>
        internal void Stop()
        {
            _stopped = true;
            Clear();
        }

        private bool Matches(in CombatInputFacts facts) =>
            _hasKey && _battleInstance == facts.BattleInstanceId && _turnSequence == facts.ControllerTurnSequence
                && _queryRevision > 0 && _queryRevision == facts.QueryRevision && _resultSequence == facts.LatestResultSequence && _actor == facts.SelectedActorId;

        private bool OwnsSlot => _input.HasPreview && _input.Preview.Number == _requestNumber
            && _input.Preview.Data.Kind == CombatViewPreviewKind.ActionAvailability && _input.Preview.Data.EntityId == _actor;

        private void Consume(CombatPreviewResult preview)
        {
            CombatActionAvailability answer = preview?.ActionAvailability;
            if (_requestNumber == 0 || !OwnsSlot || preview == null || preview.RequestNumber != _requestNumber
                || answer == null || answer.ActorId != _actor || answer.BattleInstanceId != _battleInstance
                || answer.ControllerTurnSequence != _turnSequence || answer.SourceRevision != _queryRevision)
                return;

            int number = _requestNumber;
            _requestNumber = 0;
            _answer = answer;
            State = CombatActionAvailabilityState.Ready;
            _write.ClearPreview(number);
        }

        private void DropPending()
        {
            if (_requestNumber == 0)
                return;
            int number = _requestNumber;
            _requestNumber = 0;
            State = CombatActionAvailabilityState.Unknown;
            _write.ClearPreview(number);
        }

        private static bool HasSelectedActor(EntityId actor, IReadOnlyList<CharacterData> characters)
        {
            if (actor.IsEmpty)
                return false;
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterData character = characters[i];
                if (character.EntityId == actor && character.Owner == Owner.Player
                    && character.Area == CharacterRuntimeArea.Field && !character.Health.IsDead)
                    return true;
            }
            return false;
        }
    }
}
