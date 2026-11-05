using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 결과 재생 사건의 효과음·배경음 호출입니다(Presenter 분리 설계 S5, 결정 Q-S2 A). 무엇을 낼지는 소리 표(<see cref="CombatEventSounds"/>) 한 곳이 답하고,
    /// 이 클래스는 그 단서를 AudioSystem에 냅니다(조각·조립 설계 BP8, 결정 Q-B4 A). 언제 내는지는 시계를 가진 쪽이 정합니다
    /// (틀의 사실 반영 전, 사망 조립의 소리 조각, 공격 연출 콜백, 걸음 레인 콜백). 값은 그대로입니다(리팩토링 R4 — Audio 로직은 이 작업 범위가 아님).
    /// 프리셋 번호는 View DB에서 고르는 <see cref="CombatSoundPresets"/>가 답하고(View 3라운드 W9-2), AudioSystem에는 인덱스만 넘깁니다.
    /// 오디오·프리셋은 필수입니다(View 3라운드 W9-3). 시험 조립은 아무것도 하지 않는 가짜 오디오를 넘깁니다.
    /// </summary>
    internal sealed class CombatEventAudio
    {
        private readonly IAudioSystemService _audio;
        private readonly CombatSoundPresets _presets;
        private readonly Func<EntityId, CharacterCombatFacts> _findCharacter;

        /// <param name="audio">효과음·배경음 서비스입니다(필수).</param>
        /// <param name="presets">칸에 넣을 프리셋 번호를 답합니다(필수).</param>
        /// <param name="findCharacter">프리셋을 고를 유닛을 지금 보인 사실에서 찾습니다(Presenter 분리 설계 S1의 <see cref="CombatShownFacts.FindCharacter"/>).</param>
        internal CombatEventAudio(IAudioSystemService audio, CombatSoundPresets presets, Func<EntityId, CharacterCombatFacts> findCharacter)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _presets = presets ?? throw new ArgumentNullException(nameof(presets));
            _findCharacter = findCharacter ?? throw new ArgumentNullException(nameof(findCharacter));
        }

        /// <summary>사건 하나의 소리를 냅니다. 사망 소리는 보인 사실에 아직 사망이 반영되지 않았을 때만 냅니다(반영 전에 불러야 함).</summary>
        internal void Play(CombatEvent e) => Play(CueOf(e));

        /// <summary>
        /// 사건 하나가 낼 소리 단서입니다(소리 표 <see cref="CombatEventSounds.For"/>). 사망 소리는 보인 사실에 아직 사망이 반영되지 않았을 때만 있으므로
        /// 반영 전에 물어야 합니다. 소리 조각이 시작하는 순간에 부릅니다.
        /// </summary>
        internal CombatSoundCue CueOf(CombatEvent e) => CombatEventSounds.For(e, _presets, _findCharacter);

        /// <summary>걸음 소리입니다. 걸음 레인이 걸음 시작에 부릅니다.</summary>
        internal void PlayMovement(CharacterCombatFacts unit) => Play(CombatEventSounds.Movement(unit, _presets));

        /// <summary>
        /// 소리 표가 고른 단서 하나를 AudioSystem에 냅니다. 소리 없음이면 아무것도 하지 않습니다. 사건 소리·걸음 소리·투사체·피격 소리가 모두 이 입구를 지납니다
        /// (투사체·피격 소리 묶음 끝 검토 낮음 4). 단서는 소리 표(<see cref="CombatEventSounds"/>)의 함수로 만듭니다.
        /// </summary>
        internal void Play(CombatSoundCue cue)
        {
            switch (cue.Kind)
            {
                case CombatSoundCueKind.Effect:
                    if (cue.UnitSlot.HasValue) _audio.Play(cue.UnitSlot.Value, cue.Preset);
                    else _audio.Play(cue.Slot, cue.Preset);
                    break;
                case CombatSoundCueKind.EffectWithoutPreset:
                    if (cue.UnitSlot.HasValue) _audio.Play(cue.UnitSlot.Value);
                    else _audio.Play(cue.Slot);
                    break;
                case CombatSoundCueKind.BackgroundPreset:
                    if (cue.PlayStageChanger) _audio.Play(AmbgmEvent.StageChanger);
                    _audio.SetPreset(AmbgmEvent.BGM, cue.Preset);
                    break;
            }
        }
    }
}
