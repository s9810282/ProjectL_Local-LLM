using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 동작 전환 하나의 블렌드 입력입니다(클립 칸 다시 짜기 설계 CS7, 결정 Q-A1). 끄면 지금처럼 바로 바꿉니다.
    /// 섞기는 클립 채널(<c>UnitClipChannel</c>)이 합니다. 초는 재생 배율(Anim Speed)과 상관없는 실제 초입니다(설계 P12).
    /// </summary>
    [Serializable]
    internal sealed class CombatClipBlendInput
    {
        [Tooltip("켜면 이 전환을 섞습니다. 끄면 바로 바꿉니다(지금과 같음).")]
        [SerializeField] private bool enabled;
        [Tooltip("섞는 시간(초)입니다. 켰으면 0보다 커야 합니다. 재생 배율(Anim Speed)은 걸리지 않습니다.")]
        [SerializeField, Min(0f)] private float seconds = 0.1f;

        // Unity 직렬화가 쓰는 기본 생성자입니다.
        public CombatClipBlendInput()
        {
        }

        internal CombatClipBlend ToData() => new(enabled, seconds);
    }

    /// <summary>동작 전환 하나의 블렌드 값입니다. 꺼져 있으면 초는 0입니다.</summary>
    internal readonly struct CombatClipBlend
    {
        /// <exception cref="ArgumentOutOfRangeException">켰는데 초가 유한한 양수가 아닐 때입니다.</exception>
        internal CombatClipBlend(bool enabled, float seconds)
        {
            if (enabled)
                CombatPresentationValueRules.RequirePositive(seconds, nameof(seconds));
            Enabled = enabled;
            Seconds = enabled ? seconds : 0f;
        }

        internal bool Enabled { get; }
        internal float Seconds { get; }

        /// <summary>바로 바꾸는 값입니다.</summary>
        internal static CombatClipBlend Off => default;
    }
}
