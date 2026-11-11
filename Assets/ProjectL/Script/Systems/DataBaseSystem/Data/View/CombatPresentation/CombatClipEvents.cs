using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 전투 클립의 필수 이벤트 셋과 선택 중간 이벤트를 읽고 검사합니다. DB 생성 때 클립 묶음이 부릅니다.
    /// 이벤트를 찍은 Unity 매개변수 값(문자열·수·오브젝트)은 보지 않습니다. 함수 이름과 시각만 씁니다.
    /// 중간 이벤트의 이름·번호는 번호 표(<see cref="CombatClipEventIds"/>)가 원본이고, 클립 자체의 규칙(길이·반복·루트 곡선)은 <see cref="CombatUnitAnimationRules"/>가 원본입니다.
    /// </summary>
    internal static class CombatClipEvents
    {
        /// <summary>
        /// 필수 셋·선택 번호·시각·길이를 검사하고 전체 시각을 불변 값으로 만듭니다.
        /// 검사 순서: 함수 이름(모르는 이름·둘 이상) → 없음(시작 → 기준 이벤트 → 끝) → 시각 규칙(<see cref="CombatClipTimings.FindTimeError"/>) → 클립 길이(작은 묶음 설계 §3 SB2).
        /// </summary>
        /// <exception cref="InvalidOperationException">이벤트가 빠졌거나·겹치거나·순서·범위가 틀렸거나·모르는 함수 이름일 때입니다. 문구에 클립 이름과 이유가 있습니다.</exception>
        internal static CombatClipTimings ReadTimings(AnimationClip clip)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            float startTime = 0f;
            float endTime = 0f;
            bool hasStart = false;
            bool hasEnd = false;
            // 중간 이벤트는 번호 표의 줄마다 칸 하나입니다(계획 TP-4). DB 생성 때 클립마다 한 번이라 Tick 경로가 아닙니다.
            int count = CombatClipEventIds.Count;
            var seen = new bool[count];
            var times = new float[count];
            AnimationEvent[] events = clip.events;
            for (int i = 0; i < events.Length; i++)
            {
                AnimationEvent item = events[i];
                string function = item.functionName;
                if (function == nameof(CombatAnimationEvents.ClipStart))
                    Take(clip, function, item.time, ref hasStart, ref startTime);
                else if (function == nameof(CombatAnimationEvents.ClipEnd))
                    Take(clip, function, item.time, ref hasEnd, ref endTime);
                else if (CombatClipEventIds.TryFindByFunction(function, out CombatClipEventId id))
                {
                    int index = CombatClipEventIds.IndexOf(id);
                    Take(clip, function, item.time, ref seen[index], ref times[index]);
                }
                else
                    throw Invalid(clip, $"모르는 함수 이름의 이벤트가 있습니다: '{function}'(쓸 수 있는 이름: {FunctionNames()})");
            }

            RequirePresent(clip, hasStart, nameof(CombatAnimationEvents.ClipStart));
            CombatClipEventId primary = CombatClipEventIds.Primary;
            RequirePresent(clip, seen[CombatClipEventIds.IndexOf(primary)], CombatClipEventIds.FunctionNameOf(primary));
            RequirePresent(clip, hasEnd, nameof(CombatAnimationEvents.ClipEnd));

            int present = 0;
            for (int k = 0; k < count; k++)
                if (seen[k]) present++;
            var timings = new CombatClipTimings.EventTiming[present];
            int at = 0;
            for (int k = 0; k < count; k++)
                if (seen[k]) timings[at++] = new CombatClipTimings.EventTiming(CombatClipEventIds.All[k], times[k]);

            string error = CombatClipTimings.FindTimeError(startTime, endTime, timings);
            if (error != null)
                throw Invalid(clip, error);
            if (endTime > clip.length)
                throw Invalid(clip, $"끝 이벤트({endTime}초)가 클립 길이({clip.length}초)보다 뒤입니다");
            return new CombatClipTimings(startTime, endTime, timings);
        }

        // 쓸 수 있는 이름 목록입니다: 시작 · 번호 표 순서 · 끝. 오류 문구에서만 만듭니다.
        private static string FunctionNames()
        {
            string names = nameof(CombatAnimationEvents.ClipStart);
            IReadOnlyList<CombatClipEventId> ids = CombatClipEventIds.All;
            for (int i = 0; i < ids.Count; i++)
                names += "·" + CombatClipEventIds.FunctionNameOf(ids[i]);
            return names + "·" + nameof(CombatAnimationEvents.ClipEnd);
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
