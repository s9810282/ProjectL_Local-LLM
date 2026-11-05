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
    /// 선택 이벤트 번호는 비어 있어도 되며, 존재하는 이벤트만 번호·시각 순으로 보관합니다.
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

        /// <exception cref="ArgumentOutOfRangeException">시각이 <see cref="FindTimeError"/> 규칙을 어길 때입니다.</exception>
        internal CombatClipTimings(float startTime, float eventTime, float endTime,
            float? eventTime2 = null, float? eventTime3 = null)
        {
            string error = FindTimeError(startTime, eventTime, endTime, eventTime2, eventTime3);
            if (error != null) throw new ArgumentOutOfRangeException(nameof(eventTime), error);
            StartTime = startTime;
            EndTime = endTime;

            int count = 1 + (eventTime2.HasValue ? 1 : 0) + (eventTime3.HasValue ? 1 : 0);
            var events = new EventTiming[count];
            int index = 0;
            events[index++] = new EventTiming(CombatClipEventId.Event, eventTime);
            if (eventTime2.HasValue)
                events[index++] = new EventTiming(CombatClipEventId.Event2, eventTime2.Value);
            if (eventTime3.HasValue)
                events[index] = new EventTiming(CombatClipEventId.Event3, eventTime3.Value);
            _events = Array.AsReadOnly(events);
        }

        internal float StartTime { get; }
        internal float EventTime => _events[0].Time;
        internal float EndTime { get; }

        /// <summary>존재하는 중간 이벤트입니다. 내부 배열과 변경 가능한 원소는 노출하지 않습니다.</summary>
        internal IReadOnlyList<EventTiming> Events => _events;

        /// <summary>해당 번호의 시각입니다. 정의된 선택 번호가 클립에 없으면 false입니다.</summary>
        internal bool TryGetEventTime(CombatClipEventId id, out float time)
        {
            if (id is not (CombatClipEventId.Event or CombatClipEventId.Event2 or CombatClipEventId.Event3))
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

        /// <summary>시각 규칙의 원본입니다. 선택 번호의 빈칸을 제외하고 0 ≤ 시작 ≤ 이벤트들 ≤ 끝이어야 합니다.</summary>
        internal static string FindTimeError(float startTime, float eventTime, float endTime,
            float? eventTime2 = null, float? eventTime3 = null)
        {
            if (!IsFinite(startTime) || !IsFinite(eventTime) || !IsFinite(endTime))
                return "이벤트 시각이 유한한 수가 아닙니다";
            if (startTime < 0f)
                return "시작 이벤트가 0초보다 앞섭니다";
            if (eventTime < startTime)
                return "이벤트 타이밍이 시작 이벤트보다 앞섭니다";
            if (endTime < eventTime)
                return "끝 이벤트가 이벤트 타이밍보다 앞섭니다";

            float previous = eventTime;
            string error = FindOptionalTimeError(eventTime2, previous, endTime, nameof(CombatAnimationEvents.ClipEvent2));
            if (error != null) return error;
            if (eventTime2.HasValue) previous = eventTime2.Value;
            return FindOptionalTimeError(eventTime3, previous, endTime, nameof(CombatAnimationEvents.ClipEvent3));
        }

        private static string FindOptionalTimeError(float? time, float previous, float end, string name)
        {
            if (!time.HasValue) return null;
            if (!IsFinite(time.Value))
                return $"{name} 시각이 유한한 수가 아닙니다";
            if (time.Value < previous)
                return $"{name}이 앞 이벤트보다 앞섭니다";
            if (time.Value > end)
                return $"{name}이 끝 이벤트보다 뒤입니다";
            return null;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
