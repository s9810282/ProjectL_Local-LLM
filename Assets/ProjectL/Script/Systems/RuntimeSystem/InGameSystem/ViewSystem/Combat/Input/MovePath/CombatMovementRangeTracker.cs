using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.MovePath
{
    /// <summary>
    /// 이동 범위 하나의 요청·응답·표시 수명을 맡습니다(경로 입력 분리 설계 RT1·RT2).
    /// 범위 요청을 게시하고, 자기 응답을 한 번만 받아 보이며, 슬롯을 아직 가졌는지와 보이는 범위가 어느 사실의 것인지를 기억하고,
    /// 자기가 만든 슬롯·표시만 지웁니다.
    /// </summary>
    /// <remarks>
    /// 언제·무엇을 보일지(이동 모드 사실·지정 사실·수명 시점·재진입 검사·슬롯 한가 R-a)는 <see cref="CombatPathInput"/>이 정하고 사실을 값으로 넘깁니다.
    /// 이 객체는 지정·이동 모드·복귀를 모릅니다. 수명 규칙 원본은 이동 모드 범위 설계 MR2~MR4, 슬롯 규칙은 <see cref="CombatPreviewSlotRule"/>입니다.
    /// </remarks>
    internal sealed class CombatMovementRangeTracker
    {
        private readonly ICombatViewBufferReadSink _input;
        private readonly ICombatViewBufferWriteSink _write;
        private readonly ICombatMovementRangeFeedback _display;
        private readonly CombatMovementRangeRequest _request = new CombatMovementRangeRequest();
        private long _generation;
        private bool _shown;
        // 보이는 범위가 어느 사실(전투·행위자·시작 칸·결과 번호)의 응답인지입니다. _shown일 때만 뜻이 있습니다.
        private CombatMovementRangeFacts _shownFacts;
        // 성공한 빈 범위의 근거는 표시 수명과 분리한다. 턴 종료 직전 LeaveMoveMode가 표시를 지워도 유지한다.
        private bool _hasAcceptedEmptyRange;
        private CombatMovementRangeFacts _acceptedEmptyRangeFacts;

        internal CombatMovementRangeTracker(ICombatViewBufferReadSink input, ICombatViewBufferWriteSink write,
            ICombatMovementRangeFeedback display)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _write = write ?? throw new ArgumentNullException(nameof(write));
            _display = display ?? throw new ArgumentNullException(nameof(display));
        }

        /// <summary>답을 기다리는 범위 요청이 있는지입니다. 경로 견적 유예와 지정 수명 검사가 읽습니다.</summary>
        internal bool IsPending => _request.IsPending;

        /// <summary>기다리는 요청도 보이는 범위도 없는지입니다.</summary>
        internal bool IsEmpty => !_shown && !_request.IsPending;

        /// <summary>현재 정체성과 같은 성공한 빈 응답인지입니다. 정체성이 바뀌면 근거도 영구히 내려놓습니다.</summary>
        internal bool HasAcceptedEmptyRange(in CombatMovementRangeFacts facts)
        {
            if (!facts.IsInitialized || !_acceptedEmptyRangeFacts.Matches(facts))
                InvalidateAcceptedRange();

            return _hasAcceptedEmptyRange;
        }

        /// <summary>전투 연결 해제·정체성 변경 때 빈 범위 근거를 지웁니다. 시각 정리는 별도입니다.</summary>
        internal void InvalidateAcceptedRange()
        {
            _hasAcceptedEmptyRange = false;
            _acceptedEmptyRangeFacts = default;
        }

        /// <summary>
        /// 보이거나 기다리는 범위가 이 사실의 것인지입니다(배경 요청 규칙 R-b). 기다리는 요청은 아직 슬롯을 갖고 있어야 합니다.
        /// 같으면 다시 요청하지 않습니다(슬롯을 잃었어도 이미 보이는 범위는 같은 것으로 봅니다).
        /// </summary>
        internal bool Matches(in CombatMovementRangeFacts facts) =>
            CombatPreviewSlotRule.IsSettled(
                answeredForFacts: !_request.IsPending && _shown && _shownFacts.Matches(facts),
                ownPending: _request.IsPending && _request.MatchesFacts(facts) && OwnsSlot);

        /// <summary>이 사실로 범위를 요청합니다. 앞 요청·보임이 있으면 호출자가 먼저 <see cref="Clear"/>합니다.</summary>
        internal void Request(in CombatMovementRangeFacts facts)
        {
            InvalidateAcceptedRange();
            _write.SetPreview(CombatViewPreviewData.MoveRange(facts.ActorId));
            _generation = _request.Begin(facts, _input.Preview);
        }

        /// <summary>
        /// 이 사실에 대한 자기 응답이면 한 번 소비하고 자기 슬롯을 비운 뒤 보입니다. 소비했으면 true입니다.
        /// 보임 상태를 먼저 켜고 표시를 부르므로, 표시가 던지면(예외는 그대로 전파) 뒤의 <see cref="Clear"/>가 표시를 지웁니다.
        /// </summary>
        internal bool TryShowAnswer(CombatPreviewResult preview, in CombatMovementRangeFacts facts)
        {
            if (!_request.TryConsume(_generation, facts, _input.HasPreview, _input.Preview, preview, out MovementRangeResult range))
                return false;

            _hasAcceptedEmptyRange = range.Succeeded && range.Destinations.Length == 0;
            _acceptedEmptyRangeFacts = facts;

            // 소비 직전 대조한 자기 슬롯만 비운다. 소비가 요청 번호를 이미 지웠으므로, 같은 번호임을 확인한 응답의 번호로 비운다.
            _write.ClearPreview(preview.RequestNumber);
            _shown = true;
            _shownFacts = facts;
            _display.ShowMovementRange(range);
            return true;
        }

        /// <summary>기다리는 요청만 내려놓습니다. 슬롯과 보이는 범위는 건드리지 않습니다(곧 슬롯을 남이 가져갈 때).</summary>
        internal void Drop()
        {
            _request.Cancel();
            _generation = 0;
        }

        /// <summary>기다리는 요청이 슬롯을 잃었으면 내려놓습니다. 슬롯을 가진 요청은 그대로 둡니다.</summary>
        internal void DropIfSlotLost()
        {
            if (_request.IsPending && !OwnsSlot)
                Drop();
        }

        /// <summary>
        /// 자기 슬롯이면 비우고, 보였으면 표시를 지웁니다. 둘 다 시도하고 실패를 모아 던집니다.
        /// 할 일이 없으면 바로 돌아갑니다(이동 모드가 아닌 동안 매 Tick 불리므로 정리 대리자를 만들지 않습니다).
        /// </summary>
        internal void Clear()
        {
            if (IsEmpty)
                return;

            // 요청을 지우면 번호가 0이 되므로 지우기 전에 잡는다. 슬롯 비우기는 Buffer가 그 번호일 때만 한다.
            int number = _request.Number;
            bool shown = _shown;
            _request.Cancel();
            _generation = 0;
            _shown = false;
            _shownFacts = default;
            CleanupErrors.RunAll("[CombatMovementRangeTracker] 이동 범위 정리 중 실패했습니다.",
                () => _write.ClearPreview(number),
                () =>
                {
                    if (shown)
                        _display.ClearMovementRange();
                });
        }

        private bool OwnsSlot => _request.OwnsPreview(_input.HasPreview, _input.Preview);
    }
}
