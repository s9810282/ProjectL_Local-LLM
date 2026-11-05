using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 입력(배치 Drag·공격 조준)이 내는 유닛 효과음 창구입니다(View 3라운드 W9-2 검토). 오디오와 프리셋 번호를 함께 가져,
    /// "소리를 내면 번호도 있어야 한다"는 짝을 한 객체로 둡니다. 번호가 없으면(<see cref="CombatSoundPresets.None"/>) 내지 않는 규칙은
    /// <see cref="CombatSoundPresets.IsSound"/> 한 곳입니다.
    /// </summary>
    /// <remarks>입력 Factory가 씬 조립 때 한 번 만들어 두 입력에 넘깁니다. 두 입력 모두 필수로 받습니다(평가 C5-02).</remarks>
    internal sealed class CombatInputSounds
    {
        private readonly IAudioSystemService _audio;
        private readonly CombatSoundPresets _presets;

        internal CombatInputSounds(IAudioSystemService audio, CombatSoundPresets presets)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _presets = presets ?? throw new ArgumentNullException(nameof(presets));
        }

        /// <summary>유닛 하나의 선택·취소 소리 번호입니다. 배치 Drag는 집을 때 받아 두었다가 취소 때 같은 번호를 냅니다.</summary>
        internal int PresetOf(CharacterData unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            return _presets.Unit(unit.CharacterKey, unit.Owner);
        }

        /// <summary>번호가 있으면 효과음 칸 하나를 냅니다.</summary>
        internal void Play(UnitEvent slot, int preset)
        {
            if (CombatSoundPresets.IsSound(preset))
                _audio.Play(slot, preset);
        }
    }
}
