using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>클립의 중간 이벤트 식별자입니다. 번호는 발사 지점이나 런타임 신호의 정수값과 관계없습니다.</summary>
    internal enum CombatClipEventId
    {
        Event = 1,
        Event2 = 2,
        Event3 = 3,
    }

    /// <summary>
    /// 클립의 시작·중간 이벤트·끝 시각을 소유하는 불변 값입니다. DB 값과 재생 커서가 같은 객체를 읽습니다.
    /// 중간 이벤트는 번호 표(<see cref="CombatClipEventIds"/>) 순서의 목록이고, 첫 항목이 기준 이벤트(필수)입니다. 선택 번호는 비어 있어도 되며 존재하는 이벤트만 보관합니다.
    /// </summary>
    internal sealed class CombatClipTimings
    {
        /// <summary>번호와 시각을 가진 중간 이벤트 하나입니다.</summary>
        internal readonly struct EventTiming
        {
            internal EventTiming(CombatClipEventId id, float time)
            {
                Id = id;
                Time = time;
            }

            internal CombatClipEventId Id { get; }
            internal float Time { get; }
        }

        private readonly ReadOnlyCollection<EventTiming> _events;

        /// <param name="events">존재하는 중간 이벤트입니다. 번호 표 순서이고 첫 항목이 기준 이벤트여야 합니다. 생성 때 복사합니다.</param>
        /// <exception cref="ArgumentOutOfRangeException">시각이나 목록이 <see cref="FindTimeError"/> 규칙을 어길 때입니다(paramName은 옛 생성자와 같은 <c>eventTime</c>, 계획 TP-3).</exception>
        internal CombatClipTimings(float startTime, float endTime, IReadOnlyList<EventTiming> events)
        {
            string error = FindTimeError(startTime, endTime, events);
            if (error != null) throw new ArgumentOutOfRangeException("eventTime", error);
            StartTime = startTime;
            EndTime = endTime;
            var copy = new EventTiming[events.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = events[i];
            _events = Array.AsReadOnly(copy);
        }

        internal float StartTime { get; }
        internal float EventTime => _events[0].Time;
        internal float EndTime { get; }

        /// <summary>존재하는 중간 이벤트입니다. 내부 배열과 변경 가능한 원소는 노출하지 않습니다.</summary>
        internal IReadOnlyList<EventTiming> Events => _events;

        /// <summary>해당 번호의 시각입니다. 정의된 선택 번호가 클립에 없으면 false입니다.</summary>
        internal bool TryGetEventTime(CombatClipEventId id, out float time)
        {
            if (!CombatClipEventIds.IsDefined(id))
                throw new ArgumentOutOfRangeException(nameof(id), id, "정의된 클립 이벤트 번호여야 합니다.");
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].Id != id) continue;
                time = _events[i].Time;
                return true;
            }
            time = default;
            return false;
        }

        /// <summary>
        /// 시각 규칙의 원본입니다. 맞으면 null, 아니면 이유입니다. 0 ≤ 시작 ≤ 기준 이벤트 ≤ (선택 이벤트들, 번호 순) ≤ 끝이어야 합니다.
        /// 검사 순서·문구는 옛 세 시각 검사와 같고(작은 묶음 설계 §3 SB2), 선택 이벤트의 이름은 번호 표의 함수 이름입니다.
        /// </summary>
        /// <param name="events">존재하는 중간 이벤트입니다. 번호 표 순서이고 첫 항목이 기준 이벤트여야 합니다(어기면 목록 오류 문구, 호출자 오류에서만 남).</param>
        internal static string FindTimeError(float startTime, float endTime, IReadOnlyList<EventTiming> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            string listError = FindListError(events);
            if (listError != null) return listError;

            float eventTime = events[0].Time;
            if (!IsFinite(startTime) || !IsFinite(eventTime) || !IsFinite(endTime))
                return "이벤트 시각이 유한한 수가 아닙니다";
            if (startTime < 0f)
                return "시작 이벤트가 0초보다 앞섭니다";
            if (eventTime < startTime)
                return "이벤트 타이밍이 시작 이벤트보다 앞섭니다";
            if (endTime < eventTime)
                return "끝 이벤트가 이벤트 타이밍보다 앞섭니다";

            // 앞 이벤트 = 앞에 있는 마지막 이벤트입니다(없는 선택 번호는 목록에 없음).
            float previous = eventTime;
            for (int i = 1; i < events.Count; i++)
            {
                string error = FindOptionalTimeError(events[i].Time, previous, endTime, CombatClipEventIds.FunctionNameOf(events[i].Id));
                if (error != null) return error;
                previous = events[i].Time;
            }
            return null;
        }

        // 목록 모양 규칙입니다. 읽기(CombatClipEvents.ReadTimings)는 늘 지키므로 호출자 오류에서만 납니다.
        private static string FindListError(IReadOnlyList<EventTiming> events)
        {
            string error = $"중간 이벤트 목록은 기준 이벤트({CombatClipEventIds.FunctionNameOf(CombatClipEventIds.Primary)})부터 번호 순이어야 합니다";
            if (events.Count == 0 || events[0].Id != CombatClipEventIds.Primary)
                return error;
            int previous = -1;
            for (int i = 0; i < events.Count; i++)
            {
                int index = CombatClipEventIds.IndexOf(events[i].Id);
                if (index <= previous)
                    return error;
                previous = index;
            }
            return null;
        }

        private static string FindOptionalTimeError(float time, float previous, float end, string name)
        {
            if (!IsFinite(time))
                return $"{name} 시각이 유한한 수가 아닙니다";
            if (time < previous)
                return $"{name}이 앞 이벤트보다 앞섭니다";
            if (time > end)
                return $"{name}이 끝 이벤트보다 뒤입니다";
            return null;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
