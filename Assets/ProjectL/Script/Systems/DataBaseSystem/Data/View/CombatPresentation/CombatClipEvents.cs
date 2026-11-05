using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 전투 클립의 필수 이벤트 셋과 선택 중간 이벤트를 읽고 검사합니다. DB 생성 때 클립 묶음이 부릅니다.
    /// 이벤트를 찍은 Unity 매개변수 값(문자열·수·오브젝트)은 보지 않습니다. 함수 이름과 시각만 씁니다.
    /// 클립 자체의 규칙(길이·반복·루트 곡선)은 <see cref="CombatUnitAnimationRules"/>가 원본입니다.
    /// </summary>
    internal static class CombatClipEvents
    {
        /// <summary>
        /// 기존 세 시각 읽기 입구입니다. 선택 이벤트도 검사하지만 첫 이벤트 시각만 돌려줍니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">이벤트가 빠졌거나·겹치거나·순서·범위가 틀렸거나·모르는 함수 이름일 때입니다. 문구에 클립 이름과 이유가 있습니다.</exception>
        internal static void Read(AnimationClip clip, out float startTime, out float eventTime, out float endTime)
        {
            CombatClipTimings timings = ReadTimings(clip);
            startTime = timings.StartTime;
            eventTime = timings.EventTime;
            endTime = timings.EndTime;
        }

        /// <summary>필수 셋·선택 번호·시각·길이를 검사하고 전체 시각을 불변 값으로 만듭니다.</summary>
        internal static CombatClipTimings ReadTimings(AnimationClip clip)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            float startTime = 0f;
            float eventTime = 0f;
            float endTime = 0f;
            float eventTime2 = 0f;
            float eventTime3 = 0f;
            bool hasStart = false;
            bool hasEvent = false;
            bool hasEnd = false;
            bool hasEvent2 = false;
            bool hasEvent3 = false;
            AnimationEvent[] events = clip.events;
            for (int i = 0; i < events.Length; i++)
            {
                AnimationEvent item = events[i];
                string function = item.functionName;
                if (function == nameof(CombatAnimationEvents.ClipStart))
                    Take(clip, function, item.time, ref hasStart, ref startTime);
                else if (function == nameof(CombatAnimationEvents.ClipEvent))
                    Take(clip, function, item.time, ref hasEvent, ref eventTime);
                else if (function == nameof(CombatAnimationEvents.ClipEnd))
                    Take(clip, function, item.time, ref hasEnd, ref endTime);
                else if (function == nameof(CombatAnimationEvents.ClipEvent2))
                    Take(clip, function, item.time, ref hasEvent2, ref eventTime2);
                else if (function == nameof(CombatAnimationEvents.ClipEvent3))
                    Take(clip, function, item.time, ref hasEvent3, ref eventTime3);
                else
                    throw Invalid(clip, $"모르는 함수 이름의 이벤트가 있습니다: '{function}'"
                        + $"(쓸 수 있는 이름: {nameof(CombatAnimationEvents.ClipStart)}·{nameof(CombatAnimationEvents.ClipEvent)}·"
                        + $"{nameof(CombatAnimationEvents.ClipEvent2)}·{nameof(CombatAnimationEvents.ClipEvent3)}·{nameof(CombatAnimationEvents.ClipEnd)})");
            }

            RequirePresent(clip, hasStart, nameof(CombatAnimationEvents.ClipStart));
            RequirePresent(clip, hasEvent, nameof(CombatAnimationEvents.ClipEvent));
            RequirePresent(clip, hasEnd, nameof(CombatAnimationEvents.ClipEnd));
            float? optional2 = hasEvent2 ? eventTime2 : (float?)null;
            float? optional3 = hasEvent3 ? eventTime3 : (float?)null;
            string error = CombatClipTimings.FindTimeError(startTime, eventTime, endTime, optional2, optional3);
            if (error != null)
                throw Invalid(clip, error);
            if (endTime > clip.length)
                throw Invalid(clip, $"끝 이벤트({endTime}초)가 클립 길이({clip.length}초)보다 뒤입니다");
            return new CombatClipTimings(startTime, eventTime, endTime, optional2, optional3);
        }

        private static void Take(AnimationClip clip, string function, float time, ref bool seen, ref float slot)
        {
            if (seen)
                throw Invalid(clip, $"{function} 이벤트가 둘 이상입니다");
            seen = true;
            slot = time;
        }

        private static void RequirePresent(AnimationClip clip, bool seen, string function)
        {
            if (!seen)
                throw Invalid(clip, $"{function} 이벤트가 없습니다");
        }

        private static InvalidOperationException Invalid(AnimationClip clip, string reason)
            => new($"[CombatClipEvents] 클립 이벤트 셋이 잘못되었습니다: {reason}. 클립: {clip.name}");
    }
}
