using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 유닛 소리 줄 목록의 칸 이름입니다(유닛 소리 설계 US1, 계획 TP-6). 칸마다 줄 번호의 뜻이 다르고,
    /// 칸 → 어느 소리(효과음 칸)인지는 View 소리 표 한 곳이 정합니다.
    /// </summary>
    internal enum CombatUnitSoundSlot
    {
        /// <summary>공격(포효·스킬 공격 포함)입니다. 번호 = UnitAttack 플레이어의 공격 이벤트 칸 번호입니다.</summary>
        Attack = 1,

        /// <summary>피격입니다. 번호 = 피격 효과음 프리셋입니다.</summary>
        Hit = 2,

        /// <summary>사망입니다. 번호 = 사망 효과음 프리셋입니다.</summary>
        Death = 3,
    }

    /// <summary>
    /// 유닛 소리 줄 하나의 입력입니다(유닛 소리 설계 US1, 계획 TP-1). "무엇(번호) + 언제(시점·접촉 고르기·지연)"를 한 줄에 듭니다.
    /// 공격·피격·사망 세 목록이 같은 형식이고, 번호의 뜻은 목록(칸)이 정합니다(설계 P2).
    /// </summary>
    [Serializable]
    internal sealed class CombatUnitSoundCueEntry
    {
        [Tooltip("낼 소리의 번호입니다. 공격 = 공격 이벤트 칸 번호, 피격·사망 = 효과음 프리셋 번호입니다. 0 이상이어야 하고, 소리를 끄려면 이 줄을 지웁니다.")]
        [SerializeField, Min(0)] private int sound;
        [Tooltip("소리를 내는 기준 시점입니다. 고를 수 있는 시점은 목록마다 다릅니다(목록 툴팁).")]
        [SerializeField] private CombatCueAnchor anchor = CombatCueAnchor.Start;
        [Tooltip(CombatCueTooltips.ContactFilter)]
        [SerializeField] private CombatContactFilter contactFilter = CombatContactFilter.Every;
        [Tooltip("기준 시점 뒤 소리까지의 초입니다. 길게 주면 그만큼 그 유닛의 다음 사건이 늦어집니다(연출이 소리까지 기다림).")]
        [SerializeField, Min(0f)] private float delay;

        // Unity 직렬화가 쓰는 기본 생성자입니다.
        public CombatUnitSoundCueEntry()
        {
        }

        /// <exception cref="ArgumentException">값이 규칙에 맞지 않을 때입니다.</exception>
        internal CombatUnitSoundCue ToCue() => new(sound, anchor, contactFilter, delay);
    }

    /// <summary>유닛 소리 줄 하나의 값입니다. 기다리지 않습니다(조립 항목, 완료 조건 아님).</summary>
    internal sealed class CombatUnitSoundCue
    {
        /// <exception cref="ArgumentOutOfRangeException">번호가 0보다 작거나, 시점·고르기가 정의되지 않았거나, 지연이 유한한 0 이상이 아닐 때입니다.</exception>
        internal CombatUnitSoundCue(int sound, CombatCueAnchor anchor, CombatContactFilter contactFilter, float delay)
        {
            // 소리 없는 줄(-1)은 뜻이 없어 받지 않습니다. 끄려면 줄을 지웁니다(투사체·피격 소리 계획 P2와 같은 규칙).
            CombatPresentationValueRules.RequireSoundPreset(sound, nameof(sound), allowNone: false);
            CombatPresentationCueRules.RequireDefined(anchor);
            ContactFilter = CombatPresentationCueRules.RequireFilter(anchor, contactFilter);
            CombatPresentationValueRules.RequireNonNegative(delay, nameof(delay));
            Sound = sound;
            Anchor = anchor;
            Delay = delay;
        }

        /// <summary>낼 소리의 번호입니다. 뜻은 칸(<see cref="CombatUnitSoundSlot"/>)이 정합니다.</summary>
        internal int Sound { get; }

        internal CombatCueAnchor Anchor { get; }

        /// <summary>접촉 시점의 고르기입니다. 접촉이 아니면 <see cref="CombatContactFilter.Every"/>입니다.</summary>
        internal CombatContactFilter ContactFilter { get; }

        internal float Delay { get; }
    }

    /// <summary>
    /// 유닛 하나의 소리 줄 묶음입니다(유닛 소리 설계 US1). 칸마다 줄 목록 하나이고, 비운 목록은 그 소리가 없습니다.
    /// 시점이 그 유닛에서 쓸 수 있는지는 외형 검사(<see cref="CombatPresentationCueRules.FindUnitSoundError"/>)가 봅니다.
    /// </summary>
    internal sealed class CombatUnitSoundsData
    {
        private static readonly IReadOnlyList<CombatUnitSoundCue> NoSounds = Array.Empty<CombatUnitSoundCue>();

        /// <param name="attack">공격 줄입니다. null이거나 비면 공격 소리가 없습니다.</param>
        /// <param name="hit">피격 줄입니다. null이거나 비면 피격 소리가 없습니다.</param>
        /// <param name="death">사망 줄입니다. null이거나 비면 사망 소리가 없습니다.</param>
        /// <exception cref="ArgumentException">목록에 빈 줄이 있을 때입니다.</exception>
        internal CombatUnitSoundsData(IReadOnlyList<CombatUnitSoundCue> attack, IReadOnlyList<CombatUnitSoundCue> hit,
            IReadOnlyList<CombatUnitSoundCue> death)
        {
            Attack = Copy(attack, nameof(attack));
            Hit = Copy(hit, nameof(hit));
            Death = Copy(death, nameof(death));
        }

        /// <summary>줄이 하나도 없는 묶음입니다.</summary>
        internal static CombatUnitSoundsData Empty { get; } = new(null, null, null);

        /// <summary>공격 줄입니다(포효·스킬 공격 포함). 번호 = 공격 이벤트 칸 번호입니다.</summary>
        internal IReadOnlyList<CombatUnitSoundCue> Attack { get; }

        /// <summary>피격 줄입니다. 번호 = 피격 효과음 프리셋입니다.</summary>
        internal IReadOnlyList<CombatUnitSoundCue> Hit { get; }

        /// <summary>사망 줄입니다. 번호 = 사망 효과음 프리셋입니다.</summary>
        internal IReadOnlyList<CombatUnitSoundCue> Death { get; }

        /// <summary>그 칸의 줄 목록입니다.</summary>
        /// <exception cref="ArgumentOutOfRangeException">정의되지 않은 칸일 때입니다.</exception>
        internal IReadOnlyList<CombatUnitSoundCue> Of(CombatUnitSoundSlot slot) => slot switch
        {
            CombatUnitSoundSlot.Attack => Attack,
            CombatUnitSoundSlot.Hit => Hit,
            CombatUnitSoundSlot.Death => Death,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "정의된 유닛 소리 칸이어야 합니다."),
        };

        /// <summary>입력 목록 셋을 값으로 만듭니다. 빈 칸(null 항목)은 문구에 목록 이름과 순번을 넣어 거절합니다.</summary>
        /// <exception cref="ArgumentException">빈 칸이 있거나 줄 값이 규칙에 맞지 않을 때입니다.</exception>
        internal static CombatUnitSoundsData Read(List<CombatUnitSoundCueEntry> attack, List<CombatUnitSoundCueEntry> hit,
            List<CombatUnitSoundCueEntry> death) =>
            new(Read(attack, nameof(attack)), Read(hit, nameof(hit)), Read(death, nameof(death)));

        private static IReadOnlyList<CombatUnitSoundCue> Read(List<CombatUnitSoundCueEntry> entries, string name)
        {
            if (entries == null || entries.Count == 0) return NoSounds;
            var cues = new CombatUnitSoundCue[entries.Count];
            for (int i = 0; i < cues.Length; i++)
                cues[i] = (entries[i] ?? throw new ArgumentException($"{name} 소리 {i}번 줄이 비어 있습니다.", name)).ToCue();
            // 생성자가 읽기 전용 사본을 만듭니다.
            return cues;
        }

        private static IReadOnlyList<CombatUnitSoundCue> Copy(IReadOnlyList<CombatUnitSoundCue> cues, string name)
        {
            if (cues == null || cues.Count == 0) return NoSounds;
            var copy = new CombatUnitSoundCue[cues.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = cues[i] ?? throw new ArgumentException($"{name} 소리 {i}번 줄이 비어 있습니다.", name);
            return Array.AsReadOnly(copy);
        }
    }
}
