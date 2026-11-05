using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 전투 사건 클립 하나와 그 이벤트 시각입니다(클립 설계 AC1·AC5). 시각은 클립 자신의 애니메이션 이벤트에서 읽은 값이고
    /// (<see cref="CombatClipEvents.ReadTimings"/>), 재생 길이는 끝 이벤트 시각입니다(결정 Q-AC4 A). 재생 중의 진행(커서)은 재생 단위가 이 시각으로 만듭니다.
    /// </summary>
    /// <remarks>
    /// 사건 클립은 <b>주 클립</b>(몸, 이벤트 셋을 찍음) 하나와 <b>부속 클립</b>(무기 등) 0개 이상입니다(설계 6차 AC16).
    /// 부속은 주 클립과 같은 시각으로만 돌고 이벤트를 찍지 않습니다. 시점과 재생 길이의 원본은 주 클립 하나입니다.
    /// </remarks>
    internal sealed class CombatClipData
    {
        /// <param name="companions">같이 재생할 부속 클립입니다. 없으면 null 또는 빈 목록입니다. 생성 때 복사합니다.</param>
        /// <exception cref="ArgumentOutOfRangeException">세 시각이 <see cref="FindTimeError"/>의 규칙에 맞지 않을 때입니다.</exception>
        /// <exception cref="ArgumentException">부속 목록이 <see cref="FindCompanionError"/>의 규칙에 맞지 않을 때입니다.</exception>
        internal CombatClipData(AnimationClip clip, float startTime, float eventTime, float endTime,
            IReadOnlyList<AnimationClip> companions = null)
            : this(clip, new CombatClipTimings(startTime, eventTime, endTime), companions)
        {
        }

        /// <summary>검사된 불변 시각을 보관합니다. 재생 커서는 이 시각 객체를 공유합니다.</summary>
        internal CombatClipData(AnimationClip clip, CombatClipTimings timings, IReadOnlyList<AnimationClip> companions = null)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            Timings = timings ?? throw new ArgumentNullException(nameof(timings));
            string companionError = FindCompanionError(clip, companions);
            if (companionError != null) throw new ArgumentException(companionError, nameof(companions));
            Clip = clip;
            Companions = Copy(companions);
        }

        /// <summary>주 클립입니다. 이벤트 셋을 찍은 클립이며 시각은 이 클립에서 읽었습니다.</summary>
        internal AnimationClip Clip { get; }

        /// <summary>이 클립의 시각 원본입니다. 선택 이벤트를 포함하며 생성 뒤 바뀌지 않습니다.</summary>
        internal CombatClipTimings Timings { get; }

        /// <summary>주 클립과 같은 시각으로 같이 재생할 부속 클립입니다(설계 6차 AC16). 없으면 빈 목록입니다.</summary>
        internal IReadOnlyList<AnimationClip> Companions { get; }

        /// <summary>시작 이벤트(<see cref="CombatAnimationEvents.ClipStart"/>) 시각(초)입니다.</summary>
        internal float StartTime => Timings.StartTime;

        /// <summary>이벤트 타이밍(<see cref="CombatAnimationEvents.ClipEvent"/>) 시각(초)입니다. 이펙트·소리·피격 같은 연출의 기준 시점입니다.</summary>
        internal float EventTime => Timings.EventTime;

        /// <summary>끝 이벤트(<see cref="CombatAnimationEvents.ClipEnd"/>) 시각(초)이며 재생 길이입니다.</summary>
        internal float EndTime => Timings.EndTime;

        /// <summary>
        /// 기존 세 시각 검사 입구입니다. 규칙은 <see cref="CombatClipTimings.FindTimeError"/> 하나를 사용합니다.
        /// 맞으면 null, 아니면 이유입니다. 클립 길이는 <see cref="CombatClipEvents.ReadTimings"/>가 따로 봅니다.
        /// 운영 생성자와 커서는 검사된 <see cref="CombatClipTimings"/>를 공유하고 이 입구는 기존 호출에 남깁니다.
        /// </summary>
        internal static string FindTimeError(float startTime, float eventTime, float endTime) =>
            CombatClipTimings.FindTimeError(startTime, eventTime, endTime);

        /// <summary>
        /// 부속 목록 모양 규칙의 원본입니다(설계 6차 AC18, 계획 P14). 빈 칸이 없고, 같은 클립이 두 번 없고, 주 클립과 같은 클립이 없어야 합니다.
        /// 목록이 null이거나 비면 맞습니다. 맞으면 null, 아니면 이유입니다. 이 값의 생성자와 클립 규칙(<see cref="CombatUnitAnimationRules.RequireCompanionClips"/>)이 부릅니다.
        /// 부속 클립 하나하나의 규칙(길이·반복·이벤트·곡선 겹침)은 클립 규칙이 원본입니다.
        /// </summary>
        internal static string FindCompanionError(AnimationClip clip, IReadOnlyList<AnimationClip> companions)
        {
            if (companions == null)
                return null;
            for (int i = 0; i < companions.Count; i++)
            {
                AnimationClip companion = companions[i];
                if (companion == null)
                    return $"부속 클립 {i}번 칸이 비어 있습니다";
                if (companion == clip)
                    return $"부속 클립 {i}번 칸이 주 클립과 같습니다: {companion.name}";
                for (int j = 0; j < i; j++)
                {
                    if (companions[j] == companion)
                        return $"부속 클립 {companion.name}이(가) 두 번 들어 있습니다({j}번·{i}번 칸)";
                }
            }

            return null;
        }

        private static IReadOnlyList<AnimationClip> Copy(IReadOnlyList<AnimationClip> companions)
        {
            if (companions == null || companions.Count == 0)
                return Array.Empty<AnimationClip>();
            var copy = new AnimationClip[companions.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = companions[i];
            return copy;
        }
    }
}
