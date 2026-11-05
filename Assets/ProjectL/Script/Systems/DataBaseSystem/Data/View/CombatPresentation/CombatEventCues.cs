using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 사건 칸 하나(공격·피격·사망, 사실 사건·토네이도)의 연출 큐 묶음입니다(범용 연출 구조 설계 G2, 이펙트 창구 확장 설계 EC3). 큐 하나가 조립 항목 하나가 됩니다.
    /// 소리 시점은 공격·사망 칸에 늘 있고(공격은 추가 시점 선택), 플래시는 피격 칸에만 하나입니다. 없는 소리 시점은 빈 목록입니다.
    /// 프리셋을 가진 소리 큐 목록은 피격 칸에서만 받습니다(투사체·피격 소리 설계 PS4). 없으면 빈 목록입니다.
    /// 사실 사건·토네이도 칸은 이펙트·흔들림만 갖습니다(<see cref="CombatVisualCueInput"/>).
    /// </summary>
    internal sealed class CombatEventCues
    {
        private static readonly IReadOnlyList<CombatEffectCue> NoEffects = Array.Empty<CombatEffectCue>();
        private static readonly IReadOnlyList<CombatShakeCue> NoShakes = Array.Empty<CombatShakeCue>();
        private static readonly IReadOnlyList<CombatPresetSoundCue> NoPresetSounds = Array.Empty<CombatPresetSoundCue>();
        private static readonly IReadOnlyList<CombatSoundTimingCue> NoSoundTimings = Array.Empty<CombatSoundTimingCue>();

        internal CombatEventCues(IReadOnlyList<CombatEffectCue> effects, IReadOnlyList<CombatShakeCue> shakes, CombatSoundTimingCue sound,
            CombatFlashCue flash, IReadOnlyList<CombatPresetSoundCue> presetSounds = null,
            IReadOnlyList<CombatSoundTimingCue> additionalSounds = null)
        {
            Effects = Copy(effects, NoEffects, nameof(effects));
            Shakes = Copy(shakes, NoShakes, nameof(shakes));
            SoundTimings = ReadSoundTimings(sound, additionalSounds);
            Flash = flash;
            PresetSounds = Copy(presetSounds, NoPresetSounds, nameof(presetSounds));
        }

        /// <summary>이펙트 큐입니다. 목록 순서대로 항목이 됩니다.</summary>
        internal IReadOnlyList<CombatEffectCue> Effects { get; }

        /// <summary>카메라 흔들림 큐입니다.</summary>
        internal IReadOnlyList<CombatShakeCue> Shakes { get; }

        /// <summary>첫 사건 소리 시점의 호환 읽기입니다. 원본은 SoundTimings이며, 시점이 없으면 null입니다.</summary>
        internal CombatSoundTimingCue Sound => SoundTimings.Count == 0 ? null : SoundTimings[0];

        /// <summary>기존 한 소리와 추가 입력을 합친 사건 소리 시점의 유일한 목록입니다.</summary>
        internal IReadOnlyList<CombatSoundTimingCue> SoundTimings { get; }

        /// <summary>피격 플래시입니다. 피격 칸에만 있고, 다른 칸은 null입니다.</summary>
        internal CombatFlashCue Flash { get; }

        /// <summary>프리셋을 가진 소리 큐입니다. 피격 칸에서만 받고, 목록 순서대로 항목이 됩니다(큐 붙이기가 효과음 피격 슬롯으로 냄).</summary>
        internal IReadOnlyList<CombatPresetSoundCue> PresetSounds { get; }

        /// <summary>
        /// 행동 끝(<see cref="CombatEffectEnd.UnitAction"/>)으로 끝나는 이펙트가 있는지입니다(이펙트 끝 규칙 설계 EL11). 줄 검사와 View 특성 처리기(행동 묶음을 만들지)가
        /// 함께 묻는 한 곳입니다(묶음 끝 검토 L3).
        /// </summary>
        internal bool EndsWithAction
        {
            get
            {
                for (int i = 0; i < Effects.Count; i++)
                    if (Effects[i].End == CombatEffectEnd.UnitAction)
                        return true;
                return false;
            }
        }

        /// <summary>큐가 하나도 없는지입니다(이펙트·흔들림·소리·플래시 모두 없음).</summary>
        internal bool IsEmpty => Effects.Count == 0 && Shakes.Count == 0 && SoundTimings.Count == 0 && Flash == null && PresetSounds.Count == 0;

        private static IReadOnlyList<CombatSoundTimingCue> ReadSoundTimings(CombatSoundTimingCue sound,
            IReadOnlyList<CombatSoundTimingCue> additional)
        {
            int count = additional?.Count ?? 0;
            int first = sound != null ? 1 : 0;
            if (count + first == 0) return NoSoundTimings;
            var copy = new CombatSoundTimingCue[count + first];
            if (sound != null) copy[0] = sound;
            for (int i = 0; i < count; i++)
                copy[i + first] = additional[i] ?? throw new ArgumentException($"추가 소리 {i}번 칸이 비어 있습니다.", nameof(additional));
            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source, IReadOnlyList<T> empty, string name) where T : class
        {
            if (source == null || source.Count == 0) return empty;
            var copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i] ?? throw new ArgumentException("큐 목록에 빈 항목이 있습니다.", name);
            return copy;
        }

        internal static IReadOnlyList<CombatEffectCue> Read(List<CombatEffectCueEntry> entries)
        {
            if (entries == null || entries.Count == 0) return NoEffects;
            var cues = new CombatEffectCue[entries.Count];
            for (int i = 0; i < entries.Count; i++)
                cues[i] = (entries[i] ?? throw new ArgumentException("이펙트 큐 목록에 빈 항목이 있습니다.", nameof(entries))).ToCue();
            return cues;
        }

        internal static IReadOnlyList<CombatShakeCue> Read(List<CombatShakeCueEntry> entries)
        {
            if (entries == null || entries.Count == 0) return NoShakes;
            var cues = new CombatShakeCue[entries.Count];
            for (int i = 0; i < entries.Count; i++)
                cues[i] = (entries[i] ?? throw new ArgumentException("흔들림 큐 목록에 빈 항목이 있습니다.", nameof(entries))).ToCue();
            return cues;
        }

        internal static IReadOnlyList<CombatPresetSoundCue> Read(List<CombatPresetSoundCueEntry> entries)
        {
            if (entries == null || entries.Count == 0) return NoPresetSounds;
            var cues = new CombatPresetSoundCue[entries.Count];
            for (int i = 0; i < entries.Count; i++)
                cues[i] = (entries[i] ?? throw new ArgumentException("소리 큐 목록에 빈 항목이 있습니다.", nameof(entries))).ToCue();
            return cues;
        }

        internal static IReadOnlyList<CombatSoundTimingCue> Read(List<CombatSoundTimingCueEntry> entries)
        {
            if (entries == null || entries.Count == 0) return NoSoundTimings;
            var cues = new CombatSoundTimingCue[entries.Count];
            for (int i = 0; i < cues.Length; i++)
                cues[i] = (entries[i] ?? throw new ArgumentException($"추가 소리 {i}번 칸이 비어 있습니다.", nameof(entries))).ToCue();
            return cues;
        }
    }

    /// <summary>공격 칸의 큐 입력입니다(이펙트·흔들림·소리 시점). 직선·곡사·근접 모두 이 형식이고, 고를 수 있는 시점·자리는 종류마다 규칙이 다릅니다.</summary>
    [Serializable]
    internal sealed class CombatAttackCueInput
    {
        [SerializeField] private List<CombatEffectCueEntry> effects = new();
        [SerializeField] private List<CombatShakeCueEntry> shakes = new();
        [Tooltip("공격 소리를 내는 시점입니다. 기본은 동작 기준 순간(Cue)입니다. 낼지는 소리 프리셋이 정합니다.")]
        [SerializeField] private CombatSoundTimingCueEntry sound = new(CombatCueAnchor.Cue);
        [Tooltip("같은 공격 소리를 추가로 낼 시점입니다. 비워도 됩니다. Cue2·3은 해당 이벤트가 있는 원거리 공격 클립에서만 됩니다.")]
        [SerializeField] private List<CombatSoundTimingCueEntry> additionalSounds = new();

        /// <summary>값으로 만듭니다. 규칙(공격 종류마다 다름)은 공격 값 객체 생성자가 봅니다.</summary>
        /// <exception cref="ArgumentException">칸이 비었거나 큐 값이 맞지 않을 때입니다.</exception>
        internal CombatEventCues ToCues() =>
            new(CombatEventCues.Read(effects), CombatEventCues.Read(shakes),
                (sound ?? throw new ArgumentException("공격 소리 시점 칸이 없습니다.", nameof(sound))).ToCue(), null,
                additionalSounds: CombatEventCues.Read(additionalSounds));
    }

    /// <summary>피격 칸의 큐 입력입니다(이펙트·흔들림·플래시·소리).</summary>
    [Serializable]
    internal sealed class CombatHitCueInput
    {
        [SerializeField] private List<CombatEffectCueEntry> effects = new();
        [SerializeField] private List<CombatShakeCueEntry> shakes = new();
        [SerializeField] private CombatFlashCueEntry flash = new();
        [Tooltip("피격 소리입니다. 항목마다 프리셋·시점·지연·자리를 정합니다(이펙트 큐와 같은 꼴). 비우면 피격 소리가 없습니다. "
            + "시점 Cue는 피격 클립이 있는 유닛만 됩니다.")]
        [SerializeField] private List<CombatPresetSoundCueEntry> presetSounds = new();

        /// <summary>값으로 만듭니다. 규칙은 반응 값 객체 생성자가 봅니다.</summary>
        /// <exception cref="ArgumentException">칸이 비었거나 큐 값이 맞지 않을 때입니다.</exception>
        internal CombatEventCues ToCues() =>
            new(CombatEventCues.Read(effects), CombatEventCues.Read(shakes), null,
                (flash ?? throw new ArgumentException("피격 플래시 칸이 없습니다.", nameof(flash))).ToCue(),
                CombatEventCues.Read(presetSounds));
    }

    /// <summary>사망 칸의 큐 입력입니다(이펙트·흔들림·소리 시점).</summary>
    [Serializable]
    internal sealed class CombatDeathCueInput
    {
        [SerializeField] private List<CombatEffectCueEntry> effects = new();
        [SerializeField] private List<CombatShakeCueEntry> shakes = new();
        [Tooltip("사망 소리를 내는 시점입니다. 기본은 사망 반응 시작(Start)입니다. 낼지는 소리 프리셋이 정합니다.")]
        [SerializeField] private CombatSoundTimingCueEntry sound = new(CombatCueAnchor.Start);

        /// <summary>값으로 만듭니다. 규칙은 반응 값 객체 생성자가 봅니다.</summary>
        /// <exception cref="ArgumentException">칸이 비었거나 큐 값이 맞지 않을 때입니다.</exception>
        internal CombatEventCues ToCues() =>
            new(CombatEventCues.Read(effects), CombatEventCues.Read(shakes),
                (sound ?? throw new ArgumentException("사망 소리 시점 칸이 없습니다.", nameof(sound))).ToCue(), null);
    }

    /// <summary>
    /// 이펙트·흔들림만 받는 큐 입력입니다(이펙트 창구 확장 설계 EC4, 계획 P5). 사실 사건(오브젝트·장애물·추락·약한 지반·아이템)과 토네이도 칸이 씁니다.
    /// 이 사건들의 소리는 사건 소리 표가 사건 순간에 내므로 소리·플래시 칸을 두지 않습니다. 칸마다 고를 수 있는 시점·자리는 큐 규칙이 봅니다.
    /// </summary>
    [Serializable]
    internal sealed class CombatVisualCueInput
    {
        [SerializeField] private List<CombatEffectCueEntry> effects = new();
        [SerializeField] private List<CombatShakeCueEntry> shakes = new();

        /// <summary>값으로 만듭니다. 규칙은 이 입력을 가진 값(외형 Data·설정 Data)이 큐 칸을 정해 봅니다.</summary>
        /// <exception cref="ArgumentException">큐 값이 맞지 않을 때입니다.</exception>
        internal CombatEventCues ToCues() => new(CombatEventCues.Read(effects), CombatEventCues.Read(shakes), null, null);
    }
}
