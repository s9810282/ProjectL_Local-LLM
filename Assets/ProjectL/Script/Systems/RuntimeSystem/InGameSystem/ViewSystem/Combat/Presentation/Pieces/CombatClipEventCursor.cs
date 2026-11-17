using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>전투 클립 이벤트의 종류입니다. 기존 정수값을 유지하며 재생 순서는 시각 값이 정합니다.</summary>
    internal enum CombatClipEventKind
    {
        /// <summary>시작 이벤트(<see cref="CombatAnimationEvents.ClipStart"/>)입니다.</summary>
        Start = 1,

        /// <summary>
        /// 중간 이벤트(기준 이벤트 <see cref="CombatAnimationEvents.ClipEvent"/>와 선택 이벤트)입니다. 어느 이벤트인지는 커서가 함께 내는 순번
        /// (<see cref="CombatClipTimings.Events"/>의 칸)입니다(평가 C2-04, 작은 묶음 설계 SB2 — 번호마다 종류를 두지 않음).
        /// </summary>
        Event = 2,

        /// <summary>끝 이벤트(<see cref="CombatAnimationEvents.ClipEnd"/>)입니다. 재생의 끝입니다.</summary>
        End = 3,
    }

    /// <summary>
    /// 클립 재생 시각을 따라 존재하는 이벤트를 한 번씩 내는 커서입니다. Unity 애니메이션 이벤트 대신 전투 재생이 이 커서로 시점을 냅니다.
    /// 지금 시각까지 아직 내지 않은 이벤트를 앞에서부터 하나씩 내므로, 구간은 (이전 시각, 지금 시각]이고 0초 이벤트는 처음 부를 때 나옵니다.
    /// </summary>
    /// <remarks>
    /// 재생 하나에 하나를 만듭니다. <see cref="TryNext"/>는 할당하지 않습니다(Tick 경로). 진행 상태를 가지므로 값 형식으로 두지 않았습니다
    /// (읽기 전용 필드에 담긴 값 형식은 복사본만 바뀌어 같은 이벤트를 계속 내는 함정이 있음, 원칙 검토 S5).
    /// </remarks>
    internal sealed class CombatClipEventCursor
    {
        private readonly CombatClipTimings _timings;
        // 다음 항목입니다. 0은 시작, 중간 이벤트 목록 뒤의 마지막은 끝입니다.
        private int _next;

        /// <summary>DB가 검사한 시각을 공유하며 진행 위치만 소유합니다.</summary>
        internal CombatClipEventCursor(CombatClipTimings timings)
        {
            _timings = timings ?? throw new ArgumentNullException(nameof(timings));
        }

        /// <summary>끝 이벤트를 냈는지입니다. 낸 뒤에는 더 내지 않습니다.</summary>
        internal bool IsFinished => _next > _timings.Events.Count + 1;

        /// <summary>
        /// <paramref name="time"/>(재생 시각, 초)까지 아직 내지 않은 다음 이벤트를 하나 냅니다. 없으면 false입니다.
        /// 중간 이벤트는 번호가 아니라 시각 순(<see cref="CombatClipTimings.EventsInTimeOrder"/>)으로 냅니다(Event2가 Event1보다 앞설 수 있음, 11-17 사용자).
        /// 한 시각에 여러 이벤트가 걸리면 시작 → 존재하는 중간 이벤트들(시각 순, 같은 시각은 번호 순) → 끝 순으로 하나씩 냅니다.
        /// </summary>
        /// <param name="eventIndex">중간 이벤트면 <see cref="CombatClipTimings.Events"/>의 칸, 시작·끝이면 -1입니다(계획 TP-6). 받는 쪽이 이 칸으로 바로 고릅니다.</param>
        internal bool TryNext(float time, out CombatClipEventKind kind, out int eventIndex)
        {
            if (float.IsNaN(time)) throw new ArgumentOutOfRangeException(nameof(time), "재생 시각이 수가 아닙니다.");
            kind = default;
            eventIndex = -1;
            if (IsFinished)
                return false;
            int eventCount = _timings.Events.Count;
            int slot = _next == 0 || _next == eventCount + 1 ? -1 : _timings.EventsInTimeOrder[_next - 1];
            float nextTime = _next == 0 ? _timings.StartTime
                : _next == eventCount + 1 ? _timings.EndTime : _timings.Events[slot].Time;
            if (time < nextTime)
                return false;
            if (_next == 0)
                kind = CombatClipEventKind.Start;
            else if (_next == eventCount + 1)
                kind = CombatClipEventKind.End;
            else
            {
                kind = CombatClipEventKind.Event;
                eventIndex = slot;
            }
            _next++;
            return true;
        }
    }
}
