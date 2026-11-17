using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 결과 재생 사건의 효과음·배경음 호출입니다(Presenter 분리 설계 S5, 결정 Q-S2 A). 무엇을 낼지는 소리 표(<see cref="CombatEventSounds"/>) 한 곳이 답하고,
    /// 이 클래스는 그 단서를 AudioSystem에 냅니다(조각·조립 설계 BP8, 결정 Q-B4 A). 언제 내는지는 시계를 가진 쪽이 정합니다
    /// (틀의 사실 반영 전, 사망 조립의 소리 조각, 공격 연출 콜백, 걸음 레인 콜백).
    /// 프리셋과 유닛 소리 줄(공격·피격·사망)은 View DB에서 고르는 <see cref="CombatSoundPresets"/>가 답하고, AudioSystem에는 인덱스만 넘깁니다.
    /// 오디오·프리셋은 필수입니다(View 3라운드 W9-3).
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

        /// <summary>
        /// 사건 하나의 소리를 지금 냅니다. 유닛 소리(공격·스킬·포효·사망)는 그 유닛의 줄을 모두 이 순간에 냅니다 — 시계가 없는 길(연출 없이 지나가는 공격·반응 묶음 없는 사망,
        /// 포효 정면 공격의 타격 콜백, 전용 재생 경로)이라 줄의 시점·지연은 쓰지 않습니다(유닛 소리 계획 TP-5). 사망 소리는 보인 사실에 아직 사망이 반영되지 않았을 때만 냅니다(반영 전에 불러야 함).
        /// </summary>
        internal void Play(CombatEvent e)
        {
            CombatUnitSoundSet unit = UnitSoundsOf(e);
            if (unit != null)
                PlayNow(unit);
            else
                Play(CombatEventSounds.For(e, _presets, _findCharacter));
        }

        /// <summary>사건 하나의 유닛 소리 줄입니다(소리 표 <see cref="CombatEventSounds.UnitSoundsOf"/>). 유닛 소리가 없는 사건이거나 줄이 없으면 null입니다.</summary>
        internal CombatUnitSoundSet UnitSoundsOf(CombatEvent e) => CombatEventSounds.UnitSoundsOf(e, _presets, _findCharacter);

        /// <summary>
        /// 사망 조립(반응·추락·자폭 깜빡임)이 쓸 사망 소리 줄입니다. "보인 사실에 아직 사망이 반영 전"인지를 지금(조립 때) 한 번 판단하고,
        /// 반영 전이면 판단을 끝낸 줄(<see cref="CombatUnitSoundSet.Decided"/>), 이미 사망으로 보였으면 null입니다. 조립은 시작 바로 뒤에 사망을 반영하므로
        /// 낼 때 판단하면 End·지연 줄이 조용히 빠집니다(묶음 끝 검토 M1). 시계 없는 길(<see cref="PlayNow"/>)은 지금처럼 낼 때 판단합니다.
        /// </summary>
        internal CombatUnitSoundSet DeathSoundsForComposition(CombatEvent death)
        {
            CombatUnitSoundSet set = UnitSoundsOf(death);
            if (set?.Death == null) return set;
            return CombatEventSounds.IsUnshownDeath(set.Death, _findCharacter) ? set.Decided() : null;
        }

        /// <summary>맞은 유닛의 피격 소리 줄입니다(유닛 소리 계획 TP-8). 보인 사실에 없거나 줄이 없으면 null입니다.</summary>
        internal CombatUnitSoundSet HitSoundsOf(EntityId subject)
        {
            CharacterCombatFacts unit = _findCharacter(subject);
            return unit == null ? null : CombatUnitSoundSet.Of(CombatUnitSoundSlot.Hit, _presets.UnitSounds(unit.CharacterKey));
        }

        /// <summary>유닛 소리 줄을 모두 지금 냅니다(시점·지연은 쓰지 않음, 유닛 소리 계획 TP-5). 줄이 없으면(null) 아무것도 하지 않습니다.</summary>
        internal void PlayNow(CombatUnitSoundSet set)
        {
            if (set == null) return;
            for (int i = 0; i < set.Rows.Count; i++)
                Play(CueOf(set, set.Rows[i]));
        }

        /// <summary>
        /// 유닛 소리 줄 하나의 단서입니다(소리 표 <see cref="CombatEventSounds.UnitSound"/>). 판단을 끝내지 않은 사망 줄(<see cref="CombatUnitSoundSet.Death"/>가 있음 — 시계 없는 길)은
        /// 보인 사실에 아직 사망이 반영되지 않았을 때만 있으므로 반영 전에 물어야 합니다. 소리 조각이 시작하는 순간에 부릅니다.
        /// </summary>
        internal CombatSoundCue CueOf(CombatUnitSoundSet set, CombatUnitSoundCue row)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (set.Death != null && !CombatEventSounds.IsUnshownDeath(set.Death, _findCharacter))
                return CombatSoundCue.None;
            return CombatEventSounds.UnitSound(set.Slot, row.Sound);
        }

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
                case CombatSoundCueKind.UnitAttack:
                    _audio.PlayUnitAttack(cue.AttackSlotIndex);
                    break;
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
